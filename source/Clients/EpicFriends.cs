using CommonPluginsStores.Epic;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Implementation of GenericFriends for Epic platform.
    /// Retrieves friends and their game statistics using EpicApi.
    /// </summary>
    public class EpicFriends : GenericFriends
    {
        #region Properties

        /// <summary>
        /// Instance of EpicApi used for accessing Epic account and friends data.
        /// </summary>
        private EpicApi EpicApi => PlayerActivities.EpicApi;

        #endregion

        /// <summary>
        /// Default constructor passes client name "Epic" to base.
        /// </summary>
        public EpicFriends() : base("Epic")
        {
            StoreApi = EpicApi;
        }
    }
}