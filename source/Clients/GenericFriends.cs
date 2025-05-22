using CommonPlayniteShared.Common;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using PlayerActivities.Models;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Text;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Abstract base class for managing friends from different client APIs.
    /// Handles cookie storage, connection caching, and notification for authentication errors.
    /// </summary>
    public abstract class GenericFriends
    {
        #region Properties

        internal static ILogger Logger => LogManager.GetLogger();

        internal static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        /// <summary>
        /// The store API associated with the client, to be set by derived classes.
        /// </summary>
        internal virtual CommonPluginsStores.StoreApi StoreApi { get; set; }

        /// <summary>
        /// Cached nullable boolean to store connection state result to avoid repeated checks.
        /// </summary>
        protected bool? CachedIsConnectedResult { get; set; }

        /// <summary>
        /// Name of the client this class represents (e.g., Steam, Epic).
        /// </summary>
        protected string ClientName { get; }

        /// <summary>
        /// Path to store encrypted cookies for the client.
        /// </summary>
        internal string CookiesPath { get; }

        #endregion


        /// <summary>
        /// Constructor initializes client name and cookie file path.
        /// </summary>
        /// <param name="clientName">Client identifier string.</param>
        public GenericFriends(string clientName)
        {
            ClientName = clientName;
            CookiesPath = Path.Combine(
                PluginDatabase.Paths.PluginUserDataPath,
                CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientName}.json"));
        }

        /// <summary>
        /// Abstract method to retrieve all friends from the client.
        /// </summary>
        /// <returns>List of PlayerFriend objects.</returns>
        public abstract List<PlayerFriend> GetFriends();

        /// <summary>
        /// Abstract method to update or get details for a single friend.
        /// </summary>
        /// <param name="pf">PlayerFriend object to update.</param>
        /// <returns>Updated PlayerFriend object.</returns>
        public abstract PlayerFriend GetFriends(PlayerFriend pf);


        #region Errors

        /// <summary>
        /// Shows a notification error when plugin authentication is missing or failed.
        /// Offers to open the plugin settings for re-authentication.
        /// </summary>
        /// <param name="Message">Error message to display.</param>
        /// <param name="PluginSource">Source plugin reference.</param>
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

        /// <summary>
        /// Loads and decrypts cookies from the persistent storage.
        /// </summary>
        /// <returns>List of HttpCookie objects if available; otherwise null.</returns>
        internal List<HttpCookie> GetCookies()
        {
            if (File.Exists(CookiesPath))
            {
                try
                {
                    // Decrypt cookie file using current Windows user SID as key
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

        /// <summary>
        /// Encrypts and saves cookies to persistent storage.
        /// Ensures directory exists before saving.
        /// </summary>
        /// <param name="httpCookies">List of HttpCookie to save.</param>
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