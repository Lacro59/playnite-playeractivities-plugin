using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Gog;
using PlayerActivities.Models;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Plugins;
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
        internal static ILogger Logger => LogManager.GetLogger();

        internal static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        internal virtual CommonPluginsStores.StoreApi StoreApi { get; set; }

        protected bool? CachedIsConnectedResult { get; set; }

        protected string ClientName { get; }

        internal string CookiesPath { get; }


        public GenericFriends(string clientName)
        {
            ClientName = clientName;
            CookiesPath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientName}.json"));
        }


        public abstract List<PlayerFriend> GetFriends();

        public abstract PlayerFriend GetFriends(PlayerFriend pf);


        #region Errors
        public virtual void ShowNotificationPluginNoAuthenticate(string Message, ExternalPlugin PluginSource)
        {
            API.Instance.Notifications.Add(new NotificationMessage(
                $"{PluginDatabase.PluginName}-{ClientName.RemoveWhiteSpace()}-noauthenticate",
                $"{PluginDatabase.PluginName}\r\n{Message}",
                NotificationType.Error,
                () =>
                {
                    try
                    {
                        Plugin plugin = API.Instance.Addons.Plugins.Find(x => x.Id == PlayniteTools.GetPluginId(PluginSource));
                        if (plugin != null)
                        {
                            StoreApi.ResetIsUserLoggedIn();
                            _ = plugin.OpenSettingsView();
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
            if (File.Exists(CookiesPath))
            {
                try
                {
                    return Serialization.FromJson<List<HttpCookie>>(
                        Encryption.DecryptFromFile(
                            CookiesPath,
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
            FileSystem.CreateDirectory(Path.GetDirectoryName(CookiesPath));
            Encryption.EncryptToFile(
                CookiesPath,
                Serialization.ToJson(httpCookies),
                Encoding.UTF8,
                WindowsIdentity.GetCurrent().User.Value);
        }
        #endregion
    }
}
