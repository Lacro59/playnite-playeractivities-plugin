using CommonPluginsStores.Gog;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Implementation of GenericFriends for GOG platform.
    /// Retrieves friends and their game statistics using GogApi.
    /// </summary>
    public class GogFriends : GenericFriends
    {
        #region Properties

        /// <summary>
        /// Instance of GogApi used for accessing GOG account and friends data.
        /// </summary>
        private GogApi GogApi => PlayerActivities.GogApi;

        #endregion

        /// <summary>
        /// Default constructor passes client name "GOG" to base.
        /// </summary>
        public GogFriends() : base("GOG")
        {
            StoreApi = GogApi;
        }
    }
}