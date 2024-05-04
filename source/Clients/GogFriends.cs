using CommonPluginsShared;
using CommonPluginsStores;
using CommonPluginsStores.Gog;
using CommonPluginsStores.Models;
using PlayerActivities.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Clients
{
    public class GogFriends : GenericFriends
    {
        private static readonly Lazy<GogApi> gogApi = new Lazy<GogApi>(() => new GogApi(PluginDatabase.PluginName));
        internal static GogApi GogApi => gogApi.Value;

        internal override StoreApi StoreApi => GogApi;


        public GogFriends() : base("GOG")
        {

        }


        public override List<PlayerFriends> GetFriends()
        {
            List<PlayerFriends> Friends = new List<PlayerFriends>();

            if (GogApi.IsUserLoggedIn)
            {
                try
                {
                    AccountInfos CurrentUser = GogApi.CurrentAccountInfos;
                    ObservableCollection<AccountGameInfos> CurrentGamesInfos = GogApi.CurrentGamesInfos;

                    PlayerFriends playerFriendsUs = new PlayerFriends
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


                    ObservableCollection<AccountInfos> CurrentFriendsInfos = GogApi.CurrentFriendsInfos;
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
                        ObservableCollection<AccountGameInfos> FriendGamesInfos = GogApi.GetAccountGamesInfos(y);

                        PlayerFriends playerFriends = new PlayerFriends
                        {
                            ClientName = ClientName,
                            FriendId = y.UserId,
                            FriendPseudo = y.Pseudo,
                            FriendsAvatar = y.Avatar,
                            FriendsLink = y.Link,
                            AcceptedAt = y.DateAdded,
                            IsUser = true,
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
                ShowNotificationPluginNoAuthenticate(string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName), ExternalPlugin.GogLibrary);
            }

            return Friends;
        }
    }
}
