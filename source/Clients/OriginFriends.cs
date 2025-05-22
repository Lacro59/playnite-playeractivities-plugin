using CommonPluginsShared;
using CommonPluginsStores;
using CommonPluginsStores.Models;
using CommonPluginsStores.Origin;
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
    /// Client for retrieving Origin (EA) friends and their game statistics.
    /// </summary>
    public class OriginFriends : GenericFriends
    {
        private static readonly Lazy<OriginApi> originApi = new Lazy<OriginApi>(() => new OriginApi(PluginDatabase.PluginName));
        internal static OriginApi OriginApi => originApi.Value;

        internal override StoreApi StoreApi => OriginApi;


        public OriginFriends() : base("EA") 
        {
        }

        /// <summary>
        /// Retrieve the list of Origin friends including the current user with stats and games.
        /// </summary>
        public override List<PlayerFriend> GetFriends()
        {
            var friends = new List<PlayerFriend>();

            if (!OriginApi.IsUserLoggedIn)
            {
                ShowNotificationPluginNoAuthenticate(
                    string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName),
                    ExternalPlugin.OriginLibrary
                );
                return friends;
            }

            try
            {
                // Add current user
                var CurrentUser = OriginApi.CurrentAccountInfos;
                var CurrentGamesInfos = OriginApi.CurrentGamesInfos;

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

                // Add friends
                var CurrentFriendsInfos = OriginApi.CurrentFriendsInfos;
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

                    var friendGamesInfos = OriginApi.GetAccountGamesInfos(friend) ?? new ObservableCollection<AccountGameInfos>();

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
                            GamesOwned = friendGamesInfos.Count,
                            Achievements = friendGamesInfos.Sum(x => x.AchievementsUnlocked),
                            Playtime = friendGamesInfos.Sum(x => x.Playtime)
                        },
                        Games = friendGamesInfos.Select(x => new PlayerGame
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
        /// Retrieve or update a single Origin friend’s data and statistics.
        /// </summary>
        public override PlayerFriend GetFriends(PlayerFriend pf)
        {
            if (!OriginApi.IsUserLoggedIn)
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

                var friendGamesInfos = OriginApi.GetAccountGamesInfos(accountInfos) ?? new ObservableCollection<AccountGameInfos>();

                pf.Stats = new PlayerStats
                {
                    GamesOwned = friendGamesInfos.Count,
                    Achievements = friendGamesInfos.Sum(x => x.AchievementsUnlocked),
                    Playtime = friendGamesInfos.Sum(x => x.Playtime)
                };
                pf.Games = friendGamesInfos.Select(x => new PlayerGame
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