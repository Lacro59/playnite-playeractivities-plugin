using CommonPluginsShared;
using PlayerActivities.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using static CommonPluginsShared.PlayniteTools;
using CommonPluginsStores;
using CommonPluginsStores.Steam;
using CommonPluginsStores.Models;
using System.Collections.ObjectModel;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Client for retrieving Steam friends and their game statistics.
    /// </summary>
    public class SteamFriends : GenericFriends
    {
        private SteamApi SteamApi => PlayerActivities.SteamApi;
        internal override StoreApi StoreApi => SteamApi;
        

        public SteamFriends() : base("Steam") 
        {
        }

        /// <summary>
        /// Retrieve the list of Steam friends including the current user with stats and games.
        /// </summary>
        public override List<PlayerFriend> GetFriends()
        {
            var friends = new List<PlayerFriend>();

            if (!SteamApi.IsUserLoggedIn)
            {
                ShowNotificationPluginNoAuthenticate(
                    string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName),
                    ExternalPlugin.PlayerActivities
                );
                return friends;
            }

            try
            {
                // Current user
                var CurrentUser = SteamApi.CurrentAccountInfos;
                var CurrentGamesInfos = SteamApi.CurrentGamesInfos ?? new ObservableCollection<AccountGameInfos>();

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

                // Friends
                var CurrentFriendsInfos = SteamApi.CurrentFriendsInfos;
                if (CurrentFriendsInfos == null)
                    return friends;

                PluginDatabase.FriendsDataLoading.FriendCount = CurrentFriendsInfos.Count;

                foreach (var friend in CurrentFriendsInfos)
                {
                    if (PluginDatabase.FriendsDataIsCanceled)
                    {
                        return friends;
                    }

                    PluginDatabase.FriendsDataLoading.FriendName = friend.Pseudo;

                    var FriendGamesInfos = SteamApi.GetAccountGamesInfos(friend) ?? new ObservableCollection<AccountGameInfos>();

                    var playerFriend = new PlayerFriend
                    {
                        ClientName = ClientName,
                        FriendId = friend.UserId,
                        FriendPseudo = friend.Pseudo,
                        FriendsAvatar = friend.Avatar,
                        FriendsLink = friend.Link,
                        AcceptedAt = friend.DateAdded,
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
        /// Retrieve or update a single Steam friend’s data and statistics.
        /// </summary>
        public override PlayerFriend GetFriends(PlayerFriend pf)
        {
            if (!SteamApi.IsUserLoggedIn)
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

                var FriendGamesInfos = SteamApi.GetAccountGamesInfos(accountInfos) ?? new ObservableCollection<AccountGameInfos>();

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