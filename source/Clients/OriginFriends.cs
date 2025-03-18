using CommonPluginsShared;
using PlayerActivities.Models;
using System;
using System.Collections.Generic;
using static CommonPluginsShared.PlayniteTools;
using CommonPluginsStores.Origin;
using CommonPluginsStores.Models;
using System.Collections.ObjectModel;
using System.Linq;
using Playnite.SDK;
using CommonPluginsStores;

namespace PlayerActivities.Clients
{
    public class OriginFriends : GenericFriends
    {
        private static readonly Lazy<OriginApi> originApi = new Lazy<OriginApi>(() => new OriginApi(PluginDatabase.PluginName));
        internal static OriginApi OriginApi => originApi.Value;

        internal override StoreApi StoreApi => OriginApi;


        public OriginFriends() : base("Origin")
        {

        }


        public override List<PlayerFriends> GetFriends()
        {
            List<PlayerFriends> Friends = new List<PlayerFriends>();

            if (OriginApi.IsUserLoggedIn)
            {
                try
                {
                    AccountInfos CurrentUser = OriginApi.CurrentAccountInfos;
                    ObservableCollection<AccountGameInfos> CurrentGamesInfos = OriginApi.CurrentGamesInfos;

                    PlayerFriends playerFriendsUs = new PlayerFriends
                    {
                        ClientName = ClientName,
                        FriendId = long.Parse(CurrentUser.UserId),
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
                        Games = CurrentGamesInfos.Select(x => new PlayerGames
                        {
                            Achievements = x.AchievementsUnlocked,
                            Playtime = x.Playtime,
                            Id = x.Id,
                            IsCommun = false,
                            Link = x.Link,
                            Name = x.Name
                        }).ToList()
                    };
                    Friends.Add(playerFriendsUs);


                    ObservableCollection<AccountInfos> CurrentFriendsInfos = OriginApi.CurrentFriendsInfos;
                    if (CurrentFriendsInfos == null)
                    {
                        return Friends;
                    }

                    PluginDatabase.FriendsDataLoading.FriendCount = CurrentFriendsInfos.Count;

                    CurrentFriendsInfos.ForEach(y =>
                    {
                        if (PluginDatabase.FriendsDataIsCanceled)
                        {
                            return;
                        }

                        PluginDatabase.FriendsDataLoading.FriendName = y.Pseudo;
                        ObservableCollection<AccountGameInfos> FriendGamesInfos = OriginApi.GetAccountGamesInfos(y);

                        PlayerFriends playerFriends = new PlayerFriends
                        {
                            ClientName = ClientName,
                            FriendId = long.Parse(y.UserId),
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
                            Games = FriendGamesInfos.Select(x => new PlayerGames
                            {
                                Achievements = x.AchievementsUnlocked,
                                Playtime = x.Playtime,
                                Id = x.Id,
                                IsCommun = false,
                                Link = x.Link,
                                Name = x.Name
                            }).ToList()
                        };

                        PluginDatabase.FriendsDataLoading.ActualCount += 1;
                        Friends.Add(playerFriends);
                    });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName), ExternalPlugin.OriginLibrary);
            }

            return Friends;
        }
    }
}
