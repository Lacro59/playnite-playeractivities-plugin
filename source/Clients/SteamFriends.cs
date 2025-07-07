using CommonPluginsStores.Steam;

namespace PlayerActivities.Clients
{
    /// <summary>
    /// Client for retrieving Steam friends and their game statistics.
    /// </summary>
    public class SteamFriends : GenericFriends
    {
        public SteamFriends() : base("Steam")
        {
            StoreApi = PlayerActivities.SteamApi;
        }
    }
}