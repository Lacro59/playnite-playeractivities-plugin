using CommonPluginsShared;
using CommonPluginsStores;
using CommonPluginsStores.Epic;
using CommonPluginsStores.Gog;
using CommonPluginsStores.Models;
using CommonPluginsStores.Steam;
using PlayerActivities.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Implementation of GenericFriends for Epic platform.
    /// Retrieves friends and their game statistics using EpicApi.
    /// </summary>
    public class EpicFriends : GenericFriends
    {
        private EpicApi EpicApi => PlayerActivities.EpicApi;

        internal override StoreApi StoreApi => EpicApi;


        /// <summary>
        /// Default constructor passes client name "Epic" to base.
        /// </summary>
        public EpicFriends() : base("Epic")
        {
        }

        /// <summary>
        /// Retrieves list of friends including current user with their game stats.
        /// </summary>
        /// <returns>List of PlayerFriend instances.</returns>
        public override List<PlayerFriend> GetFriends()
        {
            var friends = new List<PlayerFriend>();

            if (!EpicApi.IsUserLoggedIn)
            {
                ShowNotificationPluginNoAuthenticate(
                    string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName),
                    ExternalPlugin.PlayerActivities
                );
                return friends;
            }

            try
            {
                var CurrentUser = EpicApi.CurrentAccountInfos;
                var CurrentGamesInfos = EpicApi.CurrentGamesInfos;

                // Add current user as friend with stats and game details
                var playerFriendsUs = new PlayerFriend
                {
                    ClientName = ClientName,
                    FriendId = CurrentUser.UserId,
                    FriendPseudo = CurrentUser.Pseudo,
                    FriendsAvatar = CurrentUser.Avatar,
                    FriendsLink = CurrentUser.Link,
                    IsUser = true,
                    Stats = new PlayerStats
                    {
                        GamesOwned = CurrentGamesInfos.Count,
                        Achievements = CurrentGamesInfos.Sum(x => x.AchievementsUnlocked),
                        Playtime = CurrentGamesInfos.Sum(x => x.Playtime)
                    },
                    Games = CurrentGamesInfos.Select(x => new PlayerGame
                    {
                        Achievements = x.AchievementsUnlocked,
                        Playtime = x.Playtime,
                        Id = x.Id,
                        IsCommun = false,
                        Link = x.Link,
                        Name = x.Name
                    }).ToList()
                };
                friends.Add(playerFriendsUs);

                var CurrentFriendsInfos = EpicApi.CurrentFriendsInfos;
                if (CurrentFriendsInfos == null)
                {
                    return friends;
                }

                PluginDatabase.FriendsDataLoading.FriendCount = CurrentFriendsInfos.Count;

                // Enumerate friends and add with stats and games
                CurrentFriendsInfos.ForEach(y =>
                {
                    if (PluginDatabase.FriendsDataIsCanceled)
                    {
                        return;
                    }

                    PluginDatabase.FriendsDataLoading.FriendName = y.Pseudo;
                    var friendGamesInfos = EpicApi.GetAccountGamesInfos(y);

                    var playerFriends = new PlayerFriend
                    {
                        ClientName = ClientName,
                        FriendId = y.UserId,
                        FriendPseudo = y.Pseudo,
                        FriendsAvatar = y.Avatar,
                        FriendsLink = y.Link,
                        AcceptedAt = y.DateAdded,
                        IsUser = false,
                        Stats = new PlayerStats
                        {
                            GamesOwned = friendGamesInfos?.Count ?? 0,
                            Achievements = friendGamesInfos?.Sum(x => x.AchievementsUnlocked) ?? 0,
                            Playtime = friendGamesInfos?.Sum(x => x.Playtime) ?? 0
                        },
                        Games = friendGamesInfos?.Select(x => new PlayerGame
                        {
                            Achievements = x.AchievementsUnlocked,
                            Playtime = x.Playtime,
                            Id = x.Id,
                            IsCommun = x.IsCommun,
                            Link = x.Link,
                            Name = x.Name
                        }).ToList() ?? new List<PlayerGame>()
                    };

                    PluginDatabase.FriendsDataLoading.ActualCount += 1;
                    friends.Add(playerFriends);
                });
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
        public override PlayerFriend GetFriends(PlayerFriend pf)
        {
            if (!EpicApi.IsUserLoggedIn)
            {
                ShowNotificationPluginNoAuthenticate(
                    string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName),
                    ExternalPlugin.PlayerActivities
                );
                return pf;
            }

            try
            {
                var accountInfos = new AccountInfos
                {
                    UserId = pf.FriendId,
                    Pseudo = pf.FriendPseudo,
                    IsCurrent = pf.IsUser
                };

                var FriendGamesInfos = EpicApi.GetAccountGamesInfos(accountInfos);

                pf.Stats = new PlayerStats
                {
                    GamesOwned = FriendGamesInfos?.Count ?? 0,
                    Achievements = FriendGamesInfos?.Sum(x => x.AchievementsUnlocked) ?? 0,
                    Playtime = FriendGamesInfos?.Sum(x => x.Playtime) ?? 0
                };
                pf.Games = FriendGamesInfos?.Select(x => new PlayerGame
                {
                    Achievements = x.AchievementsUnlocked,
                    Playtime = x.Playtime,
                    Id = x.Id,
                    IsCommun = x.IsCommun,
                    Link = x.Link,
                    Name = x.Name
                }).ToList() ?? new List<PlayerGame>();

                pf.LastUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return pf;
        }
    }
}