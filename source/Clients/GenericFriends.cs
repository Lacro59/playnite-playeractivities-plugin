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
        internal string FileCookies { get; }

        internal CookiesTools CookiesTools { get; }

        #endregion


        /// <summary>
        /// Constructor initializes client name and cookie file path.
        /// </summary>
        /// <param name="clientName">Client identifier string.</param>
        public GenericFriends(string clientName)
        {
            ClientName = clientName;
            FileCookies = Path.Combine(
                PluginDatabase.Paths.PluginUserDataPath,
                CommonPlayniteShared.Common.Paths.GetSafePathName($"{ClientName.RemoveWhiteSpace()}_Cookies.dat"));

            CookiesTools = new CookiesTools(
                "PlayerActivities",
                ClientName,
                FileCookies,
                null);
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
        /// Read the last identified cookies stored.
        /// </summary>
        /// <returns></returns>
        internal virtual List<HttpCookie> GetStoredCookies() => CookiesTools.GetStoredCookies();

        /// <summary>
        /// Save the last identified cookies stored.
        /// </summary>
        /// <param name="httpCookies"></param>
        internal virtual bool SetStoredCookies(List<HttpCookie> httpCookies) => CookiesTools.SetStoredCookies(httpCookies);

        #endregion
    }
}