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
        private const string UrlPersonas = @"https://gateway.ea.com/proxy/identity/pids/{0}/personas?namespaceName=cem_ea_id";

        private string UrlApiFriends = @"https://friends.gs.ea.com/friends/2/users/{0}/friends?start=0&end=100&names=true";

        private string UrlEncode = @"https://api1.origin.com/gifting/idobfuscate/users/{0}/encodePair";
        private string UrlAvatar = @"https://api1.origin.com/avatar/user/{0}/avatars?size=2";

        private const string UrlProfileUser = @"https://www.origin.com/profile/user/{0}";
        private const string UrlProfileAchievements = UrlProfileUser + @"/achievements";
        private const string UrlProfileGames = UrlProfileUser + @"/games";


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
                    long userId = OriginAPI.GetAccountInfo(OriginAPI.GetAccessToken()).pid.pidId;

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

                    using (WebClient webClient = new WebClient { Encoding = Encoding.UTF8 }) 
                    {
                        webClient.Headers.Add("AuthToken", accessToken);
                        //webClient.Headers.Add("accept", "application/vnd.origin.v3+json; x-cache/force-write");
                        webClient.Headers.Add("accept", "application/json");

                        if (FriendsAll?.entries?.Count > 0)
                        {
                            FriendsAll.entries.ForEach(x => 
                            {
                                string FriendId = x.userId.ToString();
                                string FriendPseudo = x.displayName;

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


                                Friends.Add(new PlayerFriends
                                {
                                    ClientName = ClientName,
                                    FriendId = FriendId,
                                    FriendPseudo = FriendPseudo,
                                    FriendsAvatar = FriendsAvatar,
                                    FriendsLink = string.Format(UrlProfileUser, encoded.id)
                                });
                            });
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
    }
}
