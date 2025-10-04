using CommonPluginsStores.Ea;
using System;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Client for retrieving Origin (EA) friends and their game statistics.
    /// </summary>
    public class EaFriends : GenericFriends
    {
        #region Properties

        /// <summary>
        /// Instance of EaApi used for accessing Origin/EA account and friends data.
        /// </summary>
        private static readonly Lazy<EaApi> eaApi = new Lazy<EaApi>(() => new EaApi(PluginDatabase.PluginName));

        internal static EaApi EaApi => eaApi.Value;

        #endregion

        public EaFriends() : base("EA")
        {
            StoreApi = EaApi;
        }
    }
}