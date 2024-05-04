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

namespace PlayerActivities.Clients
{
    public class SteamFriends : GenericFriends
    {
        private SteamApi SteamApi => PlayerActivities.SteamApi;
        internal override StoreApi StoreApi => SteamApi;

        private static string UrlProfil => @"https://steamcommunity.com/my/profile";
        private static string UrlProfilById => @"https://steamcommunity.com/profiles/{0}/friends";
        private static string UrlProfilByName => @"https://steamcommunity.com/id/{0}/friends";
        private static string UrlAch => @"/stats/{0}/?tab=achievements";
        private static string UrlStoreGame => @"https://store.steampowered.com/app/{0}";

        private static string SteamId { get; set; } = string.Empty;
        private static string SteamUser { get; set; } = string.Empty;


        public SteamFriends() : base("Steam")
        {
            if (File.Exists(PluginDatabase.Paths.PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json"))
            {
                dynamic SteamConfig = Serialization.FromJsonFile<dynamic>(PluginDatabase.Paths.PluginUserDataPath + "\\..\\CB91DFC9-B977-43BF-8E70-55F46E410FAB\\config.json");
                SteamId = (string)SteamConfig["UserId"];
                SteamUser = SteamApi.GetSteamUsers()?.First()?.PersonaName;
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
                        Logger.Warn("No friends find");
                        return Friends;
                    }

                    // us
                    PlayerFriends playerFriendsUs = GetPlayerFriends(url.Replace("/friends", string.Empty));
                    playerFriendsUs.IsUser = true;
                    Friends.Add(playerFriendsUs);


                    IHtmlCollection<IElement> els = htmlDocument.QuerySelectorAll("a.selectable_overlay");
                    PluginDatabase.FriendsDataLoading.FriendCount = els.Count();
                    
                    foreach (IElement el in els)
                    {
                        if (PluginDatabase.FriendsDataIsCanceled)
                        {
                            return Friends;
                        }

                        string linkFriends = el.GetAttribute("href");
                        Friends.Add(GetPlayerFriends(linkFriends, playerFriendsUs));
                        PluginDatabase.FriendsDataLoading.ActualCount += 1;
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            }
            else
            {
                ShowNotificationPluginNoAuthenticate(string.Format(ResourceProvider.GetString("LOCCommonPluginNoAuthenticate"), ClientName), ExternalPlugin.SteamLibrary);
            }

            return Friends;
        }

        private PlayerFriends GetPlayerFriends(string link, PlayerFriends playerFriendsUs = null)
        {
            PlayerFriends playerFriends = new PlayerFriends();

            try
            {
                HtmlParser parser = new HtmlParser();
                List<HttpCookie> cookies = GetCookies();

                //string webData = Web.DownloadStringData(link, cookies).GetAwaiter().GetResult();
                string webData = string.Empty;
                using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                {
                    WebViewOffscreen.NavigateAndWait(link);
                    webData = WebViewOffscreen.GetPageSource();
                }
                IHtmlDocument htmlDocument = parser.Parse(webData);

                IHtmlCollection<IElement> avatars = htmlDocument.QuerySelectorAll("div.playerAvatarAutoSizeInner img");
                string avatar = avatars[0].GetAttribute("src");
                if (avatars.Count() > 1)
                {
                    avatar = avatars[1].GetAttribute("src");
                }
                string pseudo = htmlDocument.QuerySelector("span.actual_persona_name").InnerHtml;

                PluginDatabase.FriendsDataLoading.FriendName = pseudo;

                string gamesOwnedUrl = string.Empty;
                int gamesOwned = 0;
                double hoursPlayed = 0;

                IHtmlCollection<IElement> items = htmlDocument.QuerySelectorAll("div.profile_item_links div");
                foreach (IElement item in items)
                {
                    IElement a = item.QuerySelector("a");
                    if (a?.GetAttribute("href")?.Contains("games", StringComparison.InvariantCultureIgnoreCase) ?? false)
                    {
                        gamesOwnedUrl = a.GetAttribute("href");
                        _ = int.TryParse(a.QuerySelector("span.profile_count_link_total").InnerHtml?.Replace(",", string.Empty)?.Replace(".", string.Empty)?.Trim(), out gamesOwned);
                        break;
                    }
                }

                IElement itemAch = htmlDocument.QuerySelector("div.achievement_showcase div.showcase_stat div.value");
                _ = int.TryParse(itemAch?.InnerHtml?.Replace(",", string.Empty)?.Replace(".", string.Empty)?.Trim(), out int achievements);

                bool UsedWebAchdata = achievements == 0;

                IElement itemCompleted = htmlDocument.QuerySelector("div.achievement_showcase a.showcase_stat div.value");
                _ = int.TryParse(itemCompleted?.InnerHtml?.Trim(), out int GamesCompleted);

                List<PlayerGames> Games = new List<PlayerGames>();
                if (!gamesOwnedUrl.IsNullOrEmpty())
                {
                    //webData = Web.DownloadStringData(gamesOwnedUrl, cookies).GetAwaiter().GetResult();
                    using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                    {
                        WebViewOffscreen.NavigateAndWait(gamesOwnedUrl);
                        webData = WebViewOffscreen.GetPageSource();
                    }
                    
                    string JsonDataString = Tools.GetJsonInString(webData, "rgGames = ", "var rgChangingGames = ", "}];");
                    _ = Serialization.TryFromJson(JsonDataString, out List<FriendsApps> FriendsAppsAll);

                    if (FriendsAppsAll != null)
                    {
                        using (IWebView WebViewOffscreen = API.Instance.WebViews.CreateOffscreenView())
                        {
                            FriendsAppsAll.ForEach(x =>
                            {
                                bool IsCommun = false;
                                if (playerFriendsUs != null)
                                {
                                    IsCommun = playerFriendsUs.Games?.Where(y => y.Id.IsEqual(x.appid.ToString()))?.Count() != 0;
                                }

                                PlayerGames playerGames = new PlayerGames
                                {
                                    Id = x.appid.ToString(),
                                    Name = x.name,
                                    Link = string.Format(UrlStoreGame, x.appid),
                                    IsCommun = IsCommun
                                };

                                if (!x.hours_forever.IsNullOrEmpty())
                                {
                                    if (Regex.IsMatch(x.hours_forever, @"\d*,\d{3}"))
                                    {
                                        x.hours_forever = x.hours_forever.Replace(",", string.Empty);
                                    }
                                    if (Regex.IsMatch(x.hours_forever, @"\d*.\d{3}"))
                                    {
                                        x.hours_forever = x.hours_forever.Replace(".", string.Empty);
                                    }

                                    _ = double.TryParse(x.hours_forever
                                        .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                                        .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator), out double hours_forever);
                                    hoursPlayed += hours_forever;

                                    playerGames.Playtime = (long)(hours_forever * 3600);
                                }

                                if (x.availStatLinks.achievements && UsedWebAchdata)
                                {
                                    string urlPlayerAch = link + string.Format(UrlAch, x.appid);
                                    WebViewOffscreen.NavigateAndWait(urlPlayerAch);
                                    webData = WebViewOffscreen.GetPageSource();

                                    htmlDocument = parser.Parse(webData);
                                    IHtmlCollection<IElement> data = htmlDocument.QuerySelectorAll("#topSummaryAchievements div");
                                    if (data.Count() > 0 && data[0] != null)
                                    {
                                        Regex regex = new Regex(@"(\d+)");
                                        _ = int.TryParse(regex.Matches(data[0].InnerHtml)?[0]?.Value?.Trim(), out int achCount);
                                        achievements += achCount;

                                        playerGames.Achievements = achCount;
                                    }
                                }

                                Games.Add(playerGames);
                            });
                        }
                    }
                }


                playerFriends.ClientName = ClientName;
                playerFriends.FriendId = 0;
                playerFriends.FriendPseudo = pseudo;
                playerFriends.FriendsAvatar = avatar;
                playerFriends.FriendsLink = link;
                playerFriends.Stats = new PlayerStats
                {
                    GamesOwned = gamesOwned,
                    GamesCompleted = GamesCompleted,
                    Achievements = achievements,
                    Playtime = (long)(hoursPlayed * 3600)
                };
                playerFriends.Games = Games;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return playerFriends;
        }


        private bool IsConnected()
        {
            if (CachedIsConnectedResult == null)
            {
                if (SteamApi.IsConfigured())
                {
                    CachedIsConnectedResult = SteamApi.IsUserLoggedIn;
                }
            }

            return CachedIsConnectedResult ?? false;
        }
    }
}
