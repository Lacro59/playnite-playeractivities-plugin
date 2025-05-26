using CommonPluginsStores.Origin;
using System;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Client for retrieving Origin (EA) friends and their game statistics.
    /// </summary>
    public class OriginFriends : GenericFriends
    {
        #region Properties

        /// <summary>
        /// Instance of OriginApi used for accessing Origin/EA account and friends data.
        /// </summary>
        private static readonly Lazy<OriginApi> originApi = new Lazy<OriginApi>(() => new OriginApi(PluginDatabase.PluginName));

        internal static OriginApi OriginApi => originApi.Value;

        # endregion

        public OriginFriends() : base("EA")
        {
            StoreApi = OriginApi;
        }
    }
}