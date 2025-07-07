using CommonPluginsStores.Gog;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Implementation of GenericFriends for GOG platform.
    /// Retrieves friends and their game statistics using GogApi.
    /// </summary>
    public class GogFriends : GenericFriends
    {
        /// <summary>
        /// Default constructor passes client name "GOG" to base.
        /// </summary>
        public GogFriends() : base("GOG")
        {
            StoreApi = PlayerActivities.GogApi;
        }
    }
}