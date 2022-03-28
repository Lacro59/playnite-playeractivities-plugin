using CommonPlayniteShared.PluginLibrary.GogLibrary.Models;
using CommonPlayniteShared.PluginLibrary.Services.GogLibrary;
using CommonPluginsShared;
using PlayerActivities.Models;
using PlayerActivities.Models.Gog;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Clients
{
    public class GogFriends : GenericFriends
    {
        protected static GogAccountClient _GogAPI;
        internal static GogAccountClient GogAPI
        {
            get
            {
                if (_GogAPI == null)
                {
                    _GogAPI = new GogAccountClient(WebViewOffscreen);
                }
                return _GogAPI;
            }

            set
            {
                _GogAPI = value;
            }
        }


        protected static AccountBasicRespose _AccountInfo;

        internal static AccountBasicRespose AccountInfo
        {
            get
            {
                if (_AccountInfo == null)
                {
                    _AccountInfo = GogAPI.GetAccountInfo();
                }
                return _AccountInfo;
            }

            set
            {
                _AccountInfo = value;
            }
        }


        private string UrlFriends = @"https://embed.gog.com/users/info/{0}?expand=friendStatus";
        private string UrlProfileFriend = @"https://www.gog.com/u/{0}/friends";

        public GogFriends() : base("GOG")
        {

        }


        public override List<PlayerFriends> GetFriends()
        {
            List<PlayerFriends> Friends = new List<PlayerFriends>();

            // TODO Can be improved
            GogAPI.GetIsUserLoggedIn();
            if (GogAPI.GetIsUserLoggedIn())
            {
                try
                {
                    string AccessToken = AccountInfo.accessToken;
                    string UserId = AccountInfo.userId;
                    string UserName = AccountInfo.username;

                    List<HttpCookie> cookies = GetCookies();
                    string ResultWeb = Web.DownloadStringData(string.Format(UrlProfileFriend, UserName), cookies).GetAwaiter().GetResult();

                    // List data friends
                    string JsonDataString = Tools.GetJsonInString(ResultWeb, "window.profilesData.profileUserFriends = ", "window.profilesData.currentUserFriends = ", "}}];");
                    Serialization.TryFromJson(JsonDataString, out List<ProfileUserFriends> profileUserFriends);

                    if (JsonDataString.IsNullOrEmpty())
                    {
                        using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                        {
                            WebViewOffscreen.NavigateAndWait(UrlProfileFriend);
                            WebViewOffscreen.GetPageSource();
                            cookies = WebViewOffscreen.GetCookies()?.Where(x => x?.Domain?.Contains("gog") ?? false)?.ToList() ?? new List<HttpCookie>();
                        }

                        SetCookies(cookies);
                        cookies = GetCookies();
                        ResultWeb = Web.DownloadStringData(string.Format(UrlProfileFriend, UserName), cookies).GetAwaiter().GetResult();
                        JsonDataString = Tools.GetJsonInString(ResultWeb, "window.profilesData.profileUserFriends = ", "window.profilesData.currentUserFriends = ", "}}];");
                    }

                    if (JsonDataString.IsNullOrEmpty())
                    {
                        ShowNotificationPluginNoAuthenticate(string.Format(resources.GetString("LOCCommonPluginNoAuthenticate"), ClientName), ExternalPlugin.GogLibrary);
                    }

                    // data user
                    JsonDataString = Tools.GetJsonInString(ResultWeb, "window.profilesData.currentUser = ", "window.profilesData.profileUser = ", "]}};");
                    Serialization.TryFromJson(JsonDataString, out ProfileUser profileUser);

                    // set data
                    if (profileUserFriends != null && profileUser != null)
                    {
                        Friends.Add(new PlayerFriends
                        {
                            ClientName = ClientName,
                            FriendId = profileUser.userId,
                            FriendPseudo = profileUser.username,
                            FriendsAvatar = profileUser.avatar.Replace("\\", string.Empty),
                            FriendsLink = string.Format(UrlProfileFriend, profileUser.username),
                            Stats = new PlayerStats
                            {
                                GamesOwned = profileUser.stats.games_owned,
                                Achievements = profileUser.stats.achievements,
                                HoursPlayed = profileUser.stats.hours_played
                            }
                        });

                        profileUserFriends.ForEach(x =>
                        {
                            DateTime.TryParse(x.date_accepted.date, out DateTime dt);

                            Friends.Add(new PlayerFriends
                            {
                                ClientName = ClientName,
                                FriendId = x.user.id,
                                FriendPseudo = x.user.username,
                                FriendsAvatar = x.user.avatar.Replace("\\", string.Empty),
                                FriendsLink = string.Format(UrlProfileFriend, x.user.username),
                                AcceptedAt = dt,
                                Stats = new PlayerStats
                                {
                                    GamesOwned = x.stats.games_owned,
                                    Achievements = x.stats.achievements,
                                    HoursPlayed = Math.Round((double)x.stats.hours_played, 2)
                                }
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(string.Format(resources.GetString("LOCCommonPluginNoAuthenticate"), ClientName), ExternalPlugin.GogLibrary);
            }

            return Friends;
        }
    }
}
