using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Models;
using CommonPlayniteShared.PluginLibrary.OriginLibrary.Services;
using CommonPluginsShared;
using PlayerActivities.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using PlayerActivities.Models.Origin;
using static CommonPluginsShared.PlayniteTools;
using CommonPluginsShared.Extensions;

namespace PlayerActivities.Clients
{
    public class OriginFriends : GenericFriends
    {
        private string UrlApiFriends = @"https://friends.gs.ea.com/friends/2/users/{0}/friends?start=0&end=100&names=true";
        private string UrlApiUserInfos = @"https://api2.origin.com/atom/users?userIds={0}";

        private string UrlEncode = @"https://api1.origin.com/gifting/idobfuscate/users/{0}/encodePair";
        private string UrlAvatar = @"https://api1.origin.com/avatar/user/{0}/avatars?size=2";
        private string UrlGames = @"https://api3.origin.com/atom/users/{0}/other/{1}/games";
        private string UrlAchievements = @"https://achievements.gameservices.ea.com/achievements/personas/{0}/all?lang={1}&metadata=true";
        private string UrlAchGames = @"https://achievements.gameservices.ea.com/achievements/personas/{0}/{1}/all?lang={2}&metadata=true&fullset=true";

        private const string UrlProfileUser = @"https://www.origin.com/profile/user/{0}";


        protected static OriginAccountClient _OriginAPI;
        internal static OriginAccountClient OriginAPI
        {
            get
            {
                if (_OriginAPI == null)
                {
                    _OriginAPI = new OriginAccountClient(WebViewOffscreen);
                }
                return _OriginAPI;
            }

            set
            {
                _OriginAPI = value;
            }
        }

        private AuthTokenResponse _token;
        private AuthTokenResponse token
        {
            get
            {
                if (_token == null)
                {
                    _token = OriginAPI.GetAccessToken();
                }
                return _token;
            }
        }


        public OriginFriends() : base("Origin")
        {

        }


        public override List<PlayerFriends> GetFriends()
        {
            List<PlayerFriends> Friends = new List<PlayerFriends>();

            if (OriginAPI.GetIsUserLoggedIn())
            {
                try
                {
                    // Get informations from Origin plugin.
                    string accessToken = token.access_token;
                    AccountInfoResponse account = OriginAPI.GetAccountInfo(OriginAPI.GetAccessToken());
                    long userId = account.pid.pidId;

                    string Url = string.Format(UrlApiFriends, userId);
                    Friends FriendsAll = null;
                    using (WebClient webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        try
                        {
                            webClient.Headers.Add("X-AuthToken", accessToken);
                            webClient.Headers.Add("X-Api-Version", "2");
                            webClient.Headers.Add("X-Application-Key", "Origin");
                            webClient.Headers.Add("accept", "application/vnd.origin.v3+json; x-cache/force-write");

                            string DownloadString = webClient.DownloadString(Url);
                            FriendsAll = Serialization.FromJson<Friends>(DownloadString);
                        }
                        catch (WebException ex)
                        {
                            if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                            {
                                HttpWebResponse resp = (HttpWebResponse)ex.Response;
                                switch (resp.StatusCode)
                                {
                                    case HttpStatusCode.NotFound: // HTTP 404
                                        break;
                                    default:
                                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                                        break;
                                }

                                return Friends;
                            }
                        }
                    }


                    string userPseudo = string.Empty;
                    long personaId = 0;
                    using (WebClient webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        try
                        {
                            webClient.Headers.Add("AuthToken", accessToken);
                            webClient.Headers.Add("accept", "application/json");
                            Url = string.Format(UrlApiUserInfos, userId);
                            string DownloadString = webClient.DownloadString(Url);
                            UsersInfos usersInfos = Serialization.FromJson<UsersInfos>(DownloadString);

                            userPseudo = usersInfos?.users?.First()?.eaId;
                            long.TryParse(usersInfos?.users?.First()?.personaId, out personaId);
                        }
                        catch (WebException ex)
                        {
                            if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                            {
                                HttpWebResponse resp = (HttpWebResponse)ex.Response;
                                switch (resp.StatusCode)
                                {
                                    case HttpStatusCode.NotFound: // HTTP 404
                                        break;
                                    default:
                                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                                        break;
                                }

                                return Friends;
                            }
                        }
                    }

                    PlayerFriends playerFriends = null;
                    using (WebClient webClient = new WebClient { Encoding = Encoding.UTF8 })
                    {
                        webClient.Headers.Add("AuthToken", accessToken);
                        //webClient.Headers.Add("accept", "application/vnd.origin.v3+json; x-cache/force-write");
                        webClient.Headers.Add("accept", "application/json");

                        if (FriendsAll?.entries?.Count > 0)
                        {
                            FriendsAll.entries.ForEach(x =>
                            {
                                playerFriends = GetPlayerFriends(x, userId, accessToken);
                                if (playerFriends != null)
                                {
                                    Friends.Add(playerFriends);
                                }
                            });
                        }

                        // Us
                        Entry entry = new Entry
                        {
                            userId = userId,
                            displayName = userPseudo,
                            personaId = personaId
                        };
                        playerFriends = GetPlayerFriends(entry, userId, accessToken);
                        if (playerFriends != null)
                        {
                            Friends.Add(playerFriends);
                        }
                    }
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


        private PlayerFriends GetPlayerFriends(Entry entry, long userId, string accessToken)
        {
            PlayerFriends playerFriends = null;
            try
            {
                using (WebClient webClient = new WebClient { Encoding = Encoding.UTF8 })
                {
                    webClient.Headers.Add("AuthToken", accessToken);
                    webClient.Headers.Add("accept", "application/json");

                    string FriendId = entry.userId.ToString();
                    string FriendPseudo = entry.displayName;

                    // Encoded profile userid
                    string NewUrl = string.Format(UrlEncode, FriendId);
                    string DownloadString = webClient.DownloadString(NewUrl);
                    Encoded encoded = Serialization.FromJson<Encoded>(DownloadString);

                    // Avatar
                    webClient.Headers.Add("accept", "application/json");
                    NewUrl = string.Format(UrlAvatar, FriendId);
                    DownloadString = webClient.DownloadString(NewUrl);
                    AvatarResponse avatarResponse = Serialization.FromJson<AvatarResponse>(DownloadString);

                    string FriendsAvatar = avatarResponse?.users?.First()?.avatar?.link;

                    // Games
                    OriginProductInfos originProductInfos = null;
                    try
                    {
                        webClient.Headers.Add("accept", "application/json");
                        NewUrl = string.Format(UrlGames, userId, FriendId);
                        DownloadString = webClient.DownloadString(NewUrl);
                        Serialization.TryFromJson<OriginProductInfos>(DownloadString, out originProductInfos);
                    }
                    // No data
                    catch (Exception ex) { }

                    int GamesOwned = originProductInfos?.productInfos?.Count ?? 0;

                    // Achievements
                    dynamic originAchievements = null;
                    int Achievements = 0;
                    try
                    {
                        //webClient.Headers.Add("accept", "application/json");
                        //NewUrl = string.Format(UrlAchievements, entry.personaId, CodeLang.GetOriginLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language));
                        //DownloadString = webClient.DownloadString(NewUrl);
                        //Serialization.TryFromJson<dynamic>(DownloadString, out originAchievements);
                        //
                        //foreach(var el in originAchievements)
                        //{
                        //    var achs = originAchievements[el.Path]["achievements"];
                        //    foreach (var ach in achs)
                        //    {
                        //        Achievements++;
                        //    }
                        //}

                        originProductInfos?.productInfos?.ForEach(x => 
                        {
                            try
                            {
                                string achId = x?.softwares?.softwareList?.First().achievementSetOverride;

                                webClient.Headers.Add("accept", "application/json");
                                NewUrl = string.Format(UrlAchGames, entry.personaId, achId, CodeLang.GetOriginLang(PluginDatabase.PlayniteApi.ApplicationSettings.Language));
                                DownloadString = webClient.DownloadString(NewUrl);
                                Serialization.TryFromJson<dynamic>(DownloadString, out originAchievements);

                                foreach (var item in originAchievements["achievements"])
                                {
                                    if ((string)item.Value["state"]["a_st"] != "ACTIVE")
                                    {
                                        Achievements++;
                                    }
                                }
                            }
                            // No data
                            catch (Exception ex) { }
                        });

                    }
                    // No data
                    catch (Exception ex) { }


                    playerFriends = new PlayerFriends
                    {
                        ClientName = ClientName,
                        FriendId = FriendId,
                        FriendPseudo = FriendPseudo,
                        FriendsAvatar = FriendsAvatar,
                        FriendsLink = string.Format(UrlProfileUser, encoded.id),
                        Stats = new PlayerStats
                        {
                            GamesOwned = GamesOwned,
                            Achievements = Achievements
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return playerFriends;
        }
    }
}
