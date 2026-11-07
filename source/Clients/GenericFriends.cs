using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Models;
using CommonPluginsStores.Models.Enumerations;
using PlayerActivities.Models;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Abstract base class for managing friends from different client APIs.
    /// Handles cookie storage, connection caching, and notification for authentication errors.
    /// </summary>
    public abstract class GenericFriends
    {
        #region Properties

        /// <summary>
        /// Skip re-fetching achievements for a game when the last fetch is newer than this TTL.
        /// </summary>
        public static readonly TimeSpan AchievementsCacheTtl = TimeSpan.FromHours(12);

        protected ILogger Logger => LogManager.GetLogger();

        protected static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        /// <summary>
        /// The store API associated with the client, to be set by derived classes.
        /// </summary>
        protected CommonPluginsStores.StoreApi StoreApi { get; set; }

        protected bool IsUserLoggedIn => StoreApi?.IsUserLoggedIn ?? false;

        /// <summary>
        /// Name of the client this class represents (e.g., Steam, Epic).
        /// </summary>
        protected string ClientName { get; }

        #endregion

        /// <summary>
        /// Constructor initializes client name and cookie file path.
        /// </summary>
        /// <param name="clientName">Client identifier string.</param>
        public GenericFriends(string clientName)
        {
            ClientName = clientName ?? throw new ArgumentNullException(nameof(clientName), "Client name cannot be null.");
        }

        #region Methods

        /// <summary>
        /// Retrieves list of friends including current user with their game stats.
        /// Bulk path loads owned games and playtime only; achievements are deferred.
        /// </summary>
        /// <returns>List of PlayerFriend instances.</returns>
        public virtual List<PlayerFriend> GetFriends()
        {
            var friends = new List<PlayerFriend>();

            if (!EnsureAuthenticated())
            {
                return friends;
            }

            try
            {
                var currentUser = StoreApi.CurrentAccountInfos;
                var currentGamesInfos = StoreApi.CurrentGamesInfos;

                var playerFriendsUs = BuildPlayerFriend(currentUser, currentGamesInfos);
                friends.Add(playerFriendsUs);

                var currentFriendsInfos = StoreApi.CurrentFriendsInfos;
                if (currentFriendsInfos == null)
                {
                    return friends;
                }

                PluginDatabase.FriendsDataLoading.FriendCount = currentFriendsInfos.Count;

                // Enumerate friends and add with stats and games
                foreach (var friend in currentFriendsInfos)
                {
                    if (PluginDatabase.FriendsDataIsCanceled)
                    {
                        break;
                    }

                    PluginDatabase.FriendsDataLoading.FriendName = friend.Pseudo;

                    var playerFriend = BuildPlayerFriend(friend);
                    PluginDatabase.FriendsDataLoading.ActualCount++;
                    friends.Add(playerFriend);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return friends;
        }

        /// <summary>
        /// Updates detailed information for a single PlayerFriend, including full-library achievement enrichment.
        /// </summary>
        /// <param name="pf">PlayerFriend instance to update.</param>
        /// <returns>Updated PlayerFriend.</returns>
        public virtual PlayerFriend GetFriends(PlayerFriend pf)
        {
            if (!EnsureAuthenticated())
            {
                return pf;
            }

            try
            {
                // Single-friend refresh (PaView Button_Click) must invalidate the 12h achievements cache.
                PlayerFriend updatedFriend = BuildPlayerFriend(pf);
                Logger.Info($"[GenericFriends] GetFriends(single) client={ClientName} friend={updatedFriend?.FriendPseudo} achievementsCache=invalidated");
                updatedFriend = EnrichFriendAchievements(updatedFriend);
                updatedFriend.LastUpdate = DateTime.Now;
                return updatedFriend;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return pf;
        }

        /// <summary>
        /// Loads achievement unlock counts for every owned game (full library). Used after bulk refresh or on demand.
        /// </summary>
        /// <param name="pf">Friend whose games should be enriched.</param>
        /// <param name="cancellationToken">Cancels the per-game loop (e.g. friend selection changed).</param>
        /// <returns>The same instance with <see cref="PlayerGame.Achievements"/> filled when possible.</returns>
        public PlayerFriend EnrichFriendAchievements(PlayerFriend pf, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (pf?.Games == null || pf.Games.Count == 0)
            {
                return pf;
            }

            if (!EnsureAuthenticated())
            {
                return pf;
            }

            int pending = pf.Games.Count(g => g != null && NeedsAchievementsFetch(g));
            if (pending == 0)
            {
                StampLegacyAchievementsCache(pf);
                RefreshFriendAchievementStats(pf);
                Logger.Info($"[GenericFriends] EnrichFriendAchievements skip client={ClientName} friend={pf.FriendPseudo} reason=cache-fresh totalGames={pf.Games.Count}");
                return pf;
            }

            Logger.Info($"[GenericFriends] EnrichFriendAchievements start client={ClientName} friend={pf.FriendPseudo} pendingGames={pending} totalGames={pf.Games.Count}");

            AccountInfos accountInfos = new AccountInfos
            {
                UserId = pf.FriendId,
                ClientId = pf.ClientId,
                Pseudo = pf.FriendPseudo,
                Avatar = pf.FriendsAvatar,
                Link = pf.FriendsLink,
                IsCurrent = pf.IsUser,
                DateAdded = pf.AcceptedAt
            };

            int loaded = 0;
            int skippedFresh = 0;
            foreach (PlayerGame game in pf.Games)
            {
                if (cancellationToken.IsCancellationRequested || PluginDatabase.FriendsDataIsCanceled)
                {
                    Logger.Info($"[GenericFriends] EnrichFriendAchievements canceled client={ClientName} friend={pf.FriendPseudo} gamesEnriched={loaded}/{pending}");
                    break;
                }

                if (game == null)
                {
                    continue;
                }

                if (!NeedsAchievementsFetch(game))
                {
                    if (game.AchievementsLoaded && !game.AchievementsFetchedAt.HasValue)
                    {
                        game.AchievementsFetchedAt = DateTime.UtcNow;
                    }

                    skippedFresh++;
                    continue;
                }

                try
                {
                    ObservableCollection<GameAchievement> achievements = StoreApi.GetAchievements(game.Id, accountInfos);
                    game.Achievements = achievements?.Count(a => a != null && a.DateUnlocked != null) ?? 0;
                    game.AchievementsLoaded = true;
                    game.AchievementsFetchedAt = DateTime.UtcNow;
                    loaded++;
                }
                catch (Exception ex)
                {
                    Common.LogError(
                        ex,
                        false,
                        $"[GenericFriends] EnrichFriendAchievements failed client={ClientName} friend={pf.FriendPseudo} gameId={game.Id}",
                        false,
                        PluginDatabase.PluginName);
                    game.AchievementsLoaded = true;
                    game.AchievementsFetchedAt = DateTime.UtcNow;
                }
            }

            RefreshFriendAchievementStats(pf);

            Logger.Info(
                $"[GenericFriends] EnrichFriendAchievements done client={ClientName} friend={pf.FriendPseudo} gamesEnriched={loaded} skippedFresh={skippedFresh} totalGames={pf.Games.Count} statsAchievements={pf.Stats?.Achievements ?? 0} canceled={cancellationToken.IsCancellationRequested}");
            return pf;
        }

        /// <summary>
        /// Returns true when achievements for <paramref name="game"/> should be fetched (missing or older than TTL).
        /// </summary>
        public static bool NeedsAchievementsFetch(PlayerGame game)
        {
            if (game == null)
            {
                return false;
            }

            if (!game.AchievementsLoaded)
            {
                return true;
            }

            if (!game.AchievementsFetchedAt.HasValue)
            {
                // Legacy rows: treat as fresh once, stamp on skip path.
                return false;
            }

            return DateTime.UtcNow - game.AchievementsFetchedAt.Value >= AchievementsCacheTtl;
        }

        private static void StampLegacyAchievementsCache(PlayerFriend pf)
        {
            if (pf?.Games == null)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            foreach (PlayerGame game in pf.Games)
            {
                if (game != null && game.AchievementsLoaded && !game.AchievementsFetchedAt.HasValue)
                {
                    game.AchievementsFetchedAt = now;
                }
            }
        }

        private static void RefreshFriendAchievementStats(PlayerFriend pf)
        {
            if (pf == null)
            {
                return;
            }

            int sum = pf.Games?.Sum(g => g?.Achievements ?? 0) ?? 0;
            if (pf.Stats == null)
            {
                pf.Stats = new PlayerStats { Achievements = sum };
            }
            else
            {
                PlayerStats stats = pf.Stats;
                stats.Achievements = sum;
                pf.Stats = stats;
            }
        }

        /// <summary>
        /// Creates the friends client for a store display name (Steam, GOG, EA, Epic).
        /// </summary>
        /// <param name="clientName">Client name from <see cref="PlayerFriend.ClientName"/>.</param>
        /// <returns>Client instance, or null if unknown.</returns>
        public static GenericFriends CreateClient(string clientName)
        {
            if (clientName.IsNullOrEmpty())
            {
                return null;
            }

            if (clientName.IsEqual("Steam"))
            {
                return new SteamFriends();
            }

            if (clientName.IsEqual("GOG"))
            {
                return new GogFriends();
            }

            if (clientName.IsEqual("EA"))
            {
                return new EaFriends();
            }

            if (clientName.IsEqual("Epic"))
            {
                return new EpicFriends();
            }

            return null;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Builds a <see cref="PlayerFriend"/> instance using provided account information and optional game data.
        /// </summary>
        /// <param name="account">
        /// The <see cref="AccountInfos"/> object containing identity and metadata of the user or friend.
        /// </param>
        /// <param name="games">
        /// An optional collection of <see cref="AccountGameInfos"/> representing the user's or friend's games.
        /// If null, the method retrieves the game data using the <see cref="StoreApi"/>.
        /// </param>
        /// <returns>
        /// A fully populated <see cref="PlayerFriend"/> instance containing identity, game statistics,
        /// game list, and metadata.
        /// </returns>
        protected PlayerFriend BuildPlayerFriend(AccountInfos account, IEnumerable<AccountGameInfos> games = null)
        {
            games = games ?? StoreApi.GetAccountGamesInfos(account);

            return new PlayerFriend
            {
                ClientName = ClientName,
                FriendId = account.UserId,
                ClientId = account.ClientId,
                FriendPseudo = account.Pseudo.FixUtf8Mojibake(),
                FriendsAvatar = account.Avatar,
                FriendsLink = account.Link,
                IsUser = account.IsCurrent,
                // AccountInfos.IsPrivate defaults to true — only trust an explicit Private status from the store.
                IsPrivate = account.AccountStatus == AccountStatus.Private,
                AcceptedAt = account.DateAdded,
                Stats = new PlayerStats
                {
                    GamesOwned = games?.Count() ?? 0,
                    Achievements = games?.Sum(x => x.AchievementsUnlocked) ?? 0,
                    Playtime = games?.Sum(x => x.Playtime) ?? 0
                },
                Games = games?.Select(x => new PlayerGame
                {
                    Achievements = x.AchievementsUnlocked,
                    AchievementsLoaded = x.Achievements != null,
                    Playtime = x.Playtime,
                    Id = x.Id,
                    IsCommun = x.IsCommun,
                    Link = x.Link,
                    Name = x.Name
                }).ToList() ?? new List<PlayerGame>(),
                LastUpdate = DateTime.Now
            };
        }

        /// <summary>
        /// Builds a <see cref="PlayerFriend"/> instance using provided account information and optional game data.
        /// </summary>
        /// <param name="pf">
        /// The existing <see cref="PlayerFriend"/> instance containing identity and metadata information.
        /// </param>
        /// <param name="games">
        /// An optional collection of <see cref="AccountGameInfos"/> representing the user's or friend's games.
        /// If null, the method retrieves the game data using the <see cref="StoreApi"/>.
        /// </param>
        /// <returns>
        /// A fully populated <see cref="PlayerFriend"/> instance containing identity, game statistics,
        /// game list, and metadata.
        /// </returns>
        protected PlayerFriend BuildPlayerFriend(PlayerFriend pf, IEnumerable<AccountGameInfos> games = null)
        {
            var accountInfos = new AccountInfos
            {
                UserId = pf.FriendId,
                ClientId = pf.ClientId,
                Pseudo = pf.FriendPseudo,
                Avatar = pf.FriendsAvatar,
                Link = pf.FriendsLink,
                IsCurrent = pf.IsUser,
                IsPrivate = pf.IsPrivate,
                AccountStatus = pf.IsPrivate ? AccountStatus.Private : AccountStatus.Public,
                DateAdded = pf.AcceptedAt
            };

            return BuildPlayerFriend(accountInfos, games);
        }

        /// <summary>
        /// Verifies whether the user is authenticated. If not, shows a notification and returns false.
        /// </summary>
        /// <returns><c>true</c> if authenticated; otherwise <c>false</c>.</returns>
        protected bool EnsureAuthenticated()
        {
            if (!IsUserLoggedIn)
            {
                ShowNotificationPluginNoAuthenticate(
                    string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName),
                    ClientName.IsEqual("EA") ? ExternalPlugin.OriginLibrary : ExternalPlugin.PlayerActivities
                );
                return false;
            }
            return true;
        }

        #endregion

        #region Errors

        /// <summary>
        /// Shows a notification error when plugin authentication is missing or failed.
        /// Offers to open the plugin settings for re-authentication.
        /// </summary>
        /// <param name="message">Error message to display.</param>
        /// <param name="pluginSource">Source plugin reference.</param>
        private void ShowNotificationPluginNoAuthenticate(string message, ExternalPlugin pluginSource)
        {
            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-noauthenticate",
                $"{PluginDatabase.PluginName}\r\n{message}",
                NotificationType.Error,
                () =>
                {
                    try
                    {
                        Plugin plugin = API.Instance.Addons.Plugins.Find(x => x.Id == PlayniteTools.GetPluginId(pluginSource));
                        if (plugin != null)
                        {
                            StoreApi.ResetIsUserLoggedIn();
                            _ = plugin.OpenSettingsView();
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }
            ));
        }

        #endregion
    }
}
