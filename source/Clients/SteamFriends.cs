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

namespace PlayerActivities.Clients
{
    public class SteamFriends : GenericFriends
    {
        protected static SteamApi _steamApi;
        internal static SteamApi steamApi
        {
            get
            {
                if (_steamApi == null)
                {
                    _steamApi = new SteamApi();
                }
                return _steamApi;
            }

            set
            {
                _steamApi = value;
            }
        }


        private const string UrlProfil = @"https://steamcommunity.com/my/profile";
        private const string UrlProfilById = @"https://steamcommunity.com/profiles/{0}/friends";
        private const string UrlProfilByName = @"https://steamcommunity.com/id/{0}/friends";

        private static string SteamId { get; set; } = string.Empty;
        private static string SteamUser { get; set; } = string.Empty;


        public SteamFriends() : base("Steam")
        {
            if (File.Exists(PluginDatabase.Paths.PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"))
            {
                dynamic SteamConfig = Serialization.FromJsonFile<dynamic>(PluginDatabase.Paths.PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json");
                SteamId = (string)SteamConfig["UserId"];
                SteamUser = steamApi.GetSteamUsers()?.First()?.PersonaName;
            }
        }


        public override List<PlayerFriends> GetFriends()
        {
            List<PlayerFriends> Friends = new List<PlayerFriends>();

            if (IsConnected())
            {
                try 
                {
                    HtmlParser parser = new HtmlParser();
                    List<HttpCookie> cookies = GetCookies();

                    string url = string.Format(UrlProfilById, SteamId);
                    string webData = Web.DownloadStringData(url, cookies).GetAwaiter().GetResult();

                    IHtmlDocument htmlDocument = parser.Parse(webData);
                    IElement avatarElement = htmlDocument.QuerySelector("div.friends_header_avatar img");
                    if (avatarElement == null)
                    {
                        url = string.Format(UrlProfilByName, SteamUser);
                        webData = Web.DownloadStringData(url, cookies).GetAwaiter().GetResult();
                    }

                    avatarElement = htmlDocument.QuerySelector("div.friends_header_avatar img");
                    if (avatarElement == null)
                    {
                        logger.Warn("No friends find");
                        return Friends;
                    }

                    // us
                    Friends.Add(GetPlayerFriends(url.Replace("/friends", string.Empty)));


                    IHtmlCollection<IElement> els = htmlDocument.QuerySelectorAll("a.selectable_overlay");
                    foreach (IElement el in els)
                    {
                        string linkFriends = el.GetAttribute("href");
                        Friends.Add(GetPlayerFriends(linkFriends));


                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(string.Format(resources.GetString("LOCCommonPluginNoAuthenticate"), ClientName), ExternalPlugin.SteamLibrary);
            }

            return Friends;
        }

        private PlayerFriends GetPlayerFriends(string link)
        {
            PlayerFriends playerFriends = new PlayerFriends();

            try
            {
                HtmlParser parser = new HtmlParser();
                List<HttpCookie> cookies = GetCookies();

                string webData = Web.DownloadStringData(link, cookies).GetAwaiter().GetResult();
                IHtmlDocument htmlDocument = parser.Parse(webData);

                var avatars = htmlDocument.QuerySelectorAll("div.playerAvatarAutoSizeInner img");
                string avatar = avatars[0].GetAttribute("src");
                if (avatars.Count() > 1)
                {
                    avatar = avatars[1].GetAttribute("src");
                }
                string pseudo = htmlDocument.QuerySelector("span.actual_persona_name").InnerHtml;

                int gamesOwned = 0;
                string gamesOwnedUrl = string.Empty;
                int achievements = 0;
                double hoursPlayed = 0;

                IHtmlCollection<IElement> items = htmlDocument.QuerySelectorAll("div.profile_item_links div");
                foreach (IElement item in items)
                {
                    IElement a = item.QuerySelector("a");                    
                    if (a?.GetAttribute("href")?.Contains("games", StringComparison.InvariantCultureIgnoreCase) ?? false)
                    {
                        gamesOwnedUrl = a.GetAttribute("href");
                        int.TryParse(a.QuerySelector("span.profile_count_link_total").InnerHtml.Trim(), out gamesOwned);
                        break;
                    }
                }

                IElement itemAch = htmlDocument.QuerySelector("div.achievement_showcase div.showcase_stat div.value");
                int.TryParse(itemAch?.InnerHtml?.Replace(",", string.Empty)?.Replace(".", string.Empty)?.Trim(), out achievements);

                if (!gamesOwnedUrl.IsNullOrEmpty())
                {
                    //webData = Web.DownloadStringData(gamesOwnedUrl, cookies).GetAwaiter().GetResult();
                    using (var WebViewOffscreen = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView())
                    {
                        WebViewOffscreen.NavigateAndWait(gamesOwnedUrl);
                        webData = WebViewOffscreen.GetPageSource();
                    }

                    string JsonDataString = Tools.GetJsonInString(webData, "rgGames = ", "var rgChangingGames = ", "}}];");
                    Serialization.TryFromJson(JsonDataString, out List<FriendsApps> FriendsAppsAll);
                    FriendsAppsAll.ForEach(x => 
                    {
                        if (!x.hours_forever.IsNullOrEmpty())
                        {
                            double.TryParse(x.hours_forever
                                .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator), out double hours_forever);
                            hoursPlayed += hours_forever;
                        }
                    });
                }


                playerFriends.ClientName = ClientName;
                playerFriends.FriendId = string.Empty;
                playerFriends.FriendPseudo = pseudo;
                playerFriends.FriendsAvatar = avatar;
                playerFriends.FriendsLink = link;
                playerFriends.Stats = new PlayerStats
                {
                    GamesOwned = gamesOwned,
                    Achievements = achievements,
                    HoursPlayed = hoursPlayed
                };
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return playerFriends;
        }


        private bool IsConnected()
        {
            try 
            { 
                string ProfileById = $"https://steamcommunity.com/profiles/{SteamId}";
                string ProfileByName = $"https://steamcommunity.com/id/{SteamUser}";

                return IsProfileConnected(ProfileById) || IsProfileConnected(ProfileByName);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return false;
            }
        }

        private bool IsProfileConnected(string profilePageUrl)
        {
            try
            {
                List<HttpCookie> cookies = GetCookies();
                string ResultWeb = Web.DownloadStringData(profilePageUrl, cookies).GetAwaiter().GetResult();

                //this finds the Games link on the right side of the profile page. If that's public then so are achievements.
                IHtmlDocument HtmlDoc = new HtmlParser().Parse(ResultWeb);
                AngleSharp.Dom.IElement gamesPageLink = HtmlDoc.QuerySelector(@".profile_item_links a[href$=""/games/?tab=all""]");
                if (gamesPageLink != null)
                {
                    return true;
                }

                using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                {
                    WebViewOffscreen.NavigateAndWait(profilePageUrl);
                    ResultWeb = WebViewOffscreen.GetPageSource();
                    cookies = WebViewOffscreen.GetCookies()?.Where(x => x?.Domain?.Contains("steam") ?? false)?.ToList() ?? new List<HttpCookie>();
                }

                //this finds the Games link on the right side of the profile page. If that's public then so are achievements.
                HtmlDoc = new HtmlParser().Parse(ResultWeb);
                gamesPageLink = HtmlDoc.QuerySelector(@".profile_item_links a[href$=""/games/?tab=all""]");
                if (gamesPageLink != null)
                {
                    SetCookies(cookies);
                    return true;
                }

                return false;
            }
            catch (WebException ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
                return false;
            }
        }
    }
}
