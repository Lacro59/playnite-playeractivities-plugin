using CommonPluginsShared;
using CommonPluginsStores;
using CommonPluginsStores.Gog;
using CommonPluginsStores.Models;
using CommonPluginsStores.Origin;
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
    /// Implementation of GenericFriends for GOG platform.
    /// Retrieves friends and their game statistics using GogApi.
    /// </summary>
    public class GogFriends : GenericFriends
    {
        private GogApi GogApi => PlayerActivities.GogApi;

        internal override StoreApi StoreApi => GogApi;


        /// <summary>
        /// Default constructor passes client name "GOG" to base.
        /// </summary>
        public GogFriends() : base("GOG")
        {
        }

        /// <summary>
        /// Retrieves list of friends including current user with their game stats.
        /// </summary>
        /// <returns>List of PlayerFriend instances.</returns>
        public override List<PlayerFriend> GetFriends()
        {
            var friends = new List<PlayerFriend>();

            if (!GogApi.IsUserLoggedIn)
            {
                ShowNotificationPluginNoAuthenticate(
                    string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName),
                    ExternalPlugin.OriginLibrary
                );
                return friends;
            }

            try
            {
                AccountInfos CurrentUser = GogApi.CurrentAccountInfos;
                ObservableCollection<AccountGameInfos> CurrentGamesInfos = GogApi.CurrentGamesInfos;

                // Add current user as a friend with stats and games info
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

                ObservableCollection<AccountInfos> CurrentFriendsInfos = GogApi.CurrentFriendsInfos;
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
                    ObservableCollection<AccountGameInfos> FriendGamesInfos = GogApi.GetAccountGamesInfos(y);

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
                            GamesOwned = FriendGamesInfos.Count,
                            Achievements = FriendGamesInfos.Sum(x => x.AchievementsUnlocked),
                            Playtime = FriendGamesInfos.Sum(x => x.Playtime)
                        },
                        Games = FriendGamesInfos.Select(x => new PlayerGame
                        {
                            Achievements = x.AchievementsUnlocked,
                            Playtime = x.Playtime,
                            Id = x.Id,
                            IsCommun = x.IsCommun,
                            Link = x.Link,
                            Name = x.Name
                        }).ToList()
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
        /// Updates or retrieves detailed info for a single PlayerFriend.
        /// </summary>
        /// <param name="pf">PlayerFriend to update.</param>
        /// <returns>Updated PlayerFriend instance.</returns>
        public override PlayerFriend GetFriends(PlayerFriend pf)
        {
            if (!GogApi.IsUserLoggedIn)
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

                ObservableCollection<AccountGameInfos> FriendGamesInfos = GogApi.GetAccountGamesInfos(accountInfos);

                pf.Stats = new PlayerStats
                {
                    GamesOwned = FriendGamesInfos.Count,
                    Achievements = FriendGamesInfos.Sum(x => x.AchievementsUnlocked),
                    Playtime = FriendGamesInfos.Sum(x => x.Playtime)
                };
                pf.Games = FriendGamesInfos.Select(x => new PlayerGame
                {
                    Achievements = x.AchievementsUnlocked,
                    Playtime = x.Playtime,
                    Id = x.Id,
                    IsCommun = x.IsCommun,
                    Link = x.Link,
                    Name = x.Name
                }).ToList();

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