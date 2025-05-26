using CommonPluginsStores.Steam;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Client for retrieving Steam friends and their game statistics.
    /// </summary>
    public class SteamFriends : GenericFriends
    {
        #region Properties

        /// <summary>
        /// Instance of SteamApi used for accessing Steam account and friends data.
        /// </summary>
        private SteamApi SteamApi => PlayerActivities.SteamApi;

        # endregion

        public SteamFriends() : base("Steam")
        {
            StoreApi = SteamApi;
        }
    }
}