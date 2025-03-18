using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using PlayerActivities.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static CommonPluginsShared.PlayniteTools;
using CommonPluginsStores;
using System.Globalization;
using PlayerActivities.Models.Steam;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Steam;
using CommonPluginsStores.Models;
using System.Collections.ObjectModel;

namespace PlayerActivities.Clients
{
    public class SteamFriends : GenericFriends
    {
        private SteamApi SteamApi => PlayerActivities.SteamApi;
        internal override StoreApi StoreApi => SteamApi;


        public SteamFriends() : base("Steam")
        {

        }


        public override List<PlayerFriends> GetFriends()
        {
            List<PlayerFriends> Friends = new List<PlayerFriends>();

            if (SteamApi.IsUserLoggedIn)
            {
                try
                {
                    AccountInfos CurrentUser = SteamApi.CurrentAccountInfos;
                    ObservableCollection<AccountGameInfos> CurrentGamesInfos = SteamApi.CurrentGamesInfos;

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


                    ObservableCollection<AccountInfos> CurrentFriendsInfos = SteamApi.CurrentFriendsInfos;
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
                        ObservableCollection<AccountGameInfos> FriendGamesInfos = SteamApi.GetAccountGamesInfos(y);

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
                ShowNotificationPluginNoAuthenticate(string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName), ExternalPlugin.GogLibrary);
            }

            return Friends;
        }
    }
}
