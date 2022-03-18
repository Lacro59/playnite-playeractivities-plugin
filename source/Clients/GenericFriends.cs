using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using PlayerActivities.Models;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Clients
{
    public abstract class GenericFriends
    {
        internal static readonly ILogger logger = LogManager.GetLogger();
        internal static readonly IResourceProvider resources = new ResourceProvider();


        protected static IWebView _WebViewOffscreen;
        internal static IWebView WebViewOffscreen
        {
            get
            {
                if (_WebViewOffscreen == null)
                {
                    _WebViewOffscreen = PluginDatabase.PlayniteApi.WebViews.CreateOffscreenView();
                }
                return _WebViewOffscreen;
            }

            set
            {
                _WebViewOffscreen = value;
            }
        }


        private static PlayerActivitiesDatabase PluginDatabase = PlayerActivities.PluginDatabase;

        protected string ClientName { get; }

        internal readonly string cookiesPath;


        public GenericFriends(string ClientName)
        {
            this.ClientName = ClientName;

            cookiesPath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientName}.json"));
        }


        public abstract List<PlayerFriends> GetFriends();


        #region Errors
        public virtual void ShowNotificationPluginNoAuthenticate(string Message, ExternalPlugin PluginSource)
        {
            PluginDatabase.PlayniteApi.Notifications.Add(new NotificationMessage(
                $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-noauthenticate",
                $"{PluginDatabase.PluginName}\r\n{Message}",
                NotificationType.Error,
                () =>
                {
                    try
                    {
                        var plugin = API.Instance.Addons.Plugins.Find(x => x.Id == PlayniteTools.GetPluginId(PluginSource));
                        if (plugin != null)
                        {
                            plugin.OpenSettingsView();
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }
            ));
        }
        #endregion


        #region Cookies
        internal List<HttpCookie> GetCookies()
        {
            if (File.Exists(cookiesPath))
            {
                try
                {
                    return Serialization.FromJson<List<HttpCookie>>(
                        Encryption.DecryptFromFile(
                            cookiesPath,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "Failed to load saved cookies");
                }
            }

            return null;
        }

        internal void SetCookies(List<HttpCookie> httpCookies)
        {
            FileSystem.CreateDirectory(Path.GetDirectoryName(cookiesPath));
            Encryption.EncryptToFile(
                cookiesPath,
                Serialization.ToJson(httpCookies),
                Encoding.UTF8,
                WindowsIdentity.GetCurrent().User.Value);
        }
        #endregion
    }
}
