using CommonPluginsShared;
using CommonPluginsStores;
using CommonPluginsStores.Epic;
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
    public class EpicFriends : GenericFriends
    {
        private EpicApi EpicApi => PlayerActivities.EpicApi;
        internal override StoreApi StoreApi => EpicApi;


        public EpicFriends() : base("Epic")
        {

        }


        public override List<PlayerFriend> GetFriends()
        {
            List<PlayerFriend> Friends = new List<PlayerFriend>();

            if (EpicApi.IsUserLoggedIn)
            {
                try
                {
                    AccountInfos CurrentUser = EpicApi.CurrentAccountInfos;
                    ObservableCollection<AccountGameInfos> CurrentGamesInfos = EpicApi.CurrentGamesInfos;

                    PlayerFriend playerFriendsUs = new PlayerFriend
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
                    Friends.Add(playerFriendsUs);


                    ObservableCollection<AccountInfos> CurrentFriendsInfos = EpicApi.CurrentFriendsInfos;
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
                        ObservableCollection<AccountGameInfos> friendGamesInfos = EpicApi.GetAccountGamesInfos(y);

                        PlayerFriend playerFriends = new PlayerFriend
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
                ShowNotificationPluginNoAuthenticate(string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName), ExternalPlugin.PlayerActivities);
            }

            return Friends;
        }

        public override PlayerFriend GetFriends(PlayerFriend pf)
        {
            if (EpicApi.IsUserLoggedIn)
            {
                try
                {
                    AccountInfos accountInfos = new AccountInfos
                    {
                        UserId = pf.FriendId,
                        Pseudo = pf.FriendPseudo,
                        IsCurrent = pf.IsUser
                    };

                    ObservableCollection<AccountGameInfos> FriendGamesInfos = EpicApi.GetAccountGamesInfos(accountInfos);

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
                    pf.LastRefresh = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName), ExternalPlugin.PlayerActivities);
            }

            return pf;
        }
    }
}
