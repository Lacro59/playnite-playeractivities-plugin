using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Models;
using PlayerActivities.Models;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Updates detailed information for a single PlayerFriend.
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
                var updatedFriend = BuildPlayerFriend(pf);
                updatedFriend.LastUpdate = DateTime.Now;
                return updatedFriend;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return pf;
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
                FriendPseudo = account.Pseudo,
                FriendsAvatar = account.Avatar,
                FriendsLink = account.Link,
                IsUser = account.IsCurrent,
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
                Pseudo = pf.FriendPseudo,
                Avatar = pf.FriendsAvatar,
                Link = pf.FriendsLink,
                IsCurrent = pf.IsUser,
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
                    ExternalPlugin.PlayerActivities
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