using CommonPluginsStores.Ea;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Client for retrieving EA friends and their game statistics.
    /// </summary>
    public class EaFriends : GenericFriends
    {
        public EaFriends() : base("EA")
        {
            StoreApi = PlayerActivities.EaApi;
        }
    }
}
