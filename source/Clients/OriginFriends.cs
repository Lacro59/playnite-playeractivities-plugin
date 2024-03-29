﻿using CommonPluginsShared;
using PlayerActivities.Models;
using System;
using System.Collections.Generic;
using static CommonPluginsShared.PlayniteTools;
using CommonPluginsStores.Origin;
using CommonPluginsStores.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace PlayerActivities.Clients
{
    public class OriginFriends : GenericFriends
    {
        protected static OriginApi _originApi;
        internal static OriginApi originApi
        {
            get
            {
                if (_originApi == null)
                {
                    _originApi = new OriginApi(PluginDatabase.PluginName);
                }
                return _originApi;
            }

            set => _originApi = value;
        }


        public OriginFriends() : base("Origin")
        {

        }


        public override List<PlayerFriends> GetFriends()
        {
            List<PlayerFriends> Friends = new List<PlayerFriends>();

            if (originApi.IsUserLoggedIn)
            {
                try
                {
                    AccountInfos CurrentUser = originApi.CurrentAccountInfos;
                    ObservableCollection<AccountGameInfos> CurrentGamesInfos = originApi.CurrentGamesInfos;

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


                    ObservableCollection<AccountInfos> CurrentFriendsInfos = originApi.CurrentFriendsInfos;
                    if (CurrentFriendsInfos == null)
                    {
                        return Friends;
                    }

                    PluginDatabase.friendsDataLoading.FriendCount = CurrentFriendsInfos.Count;

                    CurrentFriendsInfos.ForEach(y =>
                    {
                        if (PluginDatabase.FriendsDataIsCanceled)
                        {
                            return;
                        }

                        PluginDatabase.friendsDataLoading.FriendName = y.Pseudo;
                        ObservableCollection<AccountGameInfos> FriendGamesInfos = originApi.GetAccountGamesInfos(y);

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

                        PluginDatabase.friendsDataLoading.ActualCount += 1;
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
                ShowNotificationPluginNoAuthenticate(string.Format(resources.GetString("LOCCommonPluginNoAuthenticate"), ClientName), ExternalPlugin.OriginLibrary);
            }

            return Friends;
        }
    }
}
