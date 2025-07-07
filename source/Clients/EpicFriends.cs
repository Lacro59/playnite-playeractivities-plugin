using CommonPluginsStores.Epic;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Implementation of GenericFriends for Epic platform.
    /// Retrieves friends and their game statistics using EpicApi.
    /// </summary>
    public class EpicFriends : GenericFriends
    {
        /// <summary>
        /// Default constructor passes client name "Epic" to base.
        /// </summary>
        public EpicFriends() : base("Epic")
        {
            StoreApi = PlayerActivities.EpicApi;
        }
    }
}