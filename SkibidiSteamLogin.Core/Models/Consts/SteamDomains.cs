namespace SkibidiSteamLogin.Core.Models.Consts
{
    public static class SteamDomains
    {
        public const string Community = "steamcommunity.com";
        public const string Store = "store.steampowered.com";
        public const string Help = "help.steampowered.com";
        public const string Checkout = "checkout.steampowered";
        public const string Tv = "steam.tv";

        public static List<string> All => [Community, Store, Help, Checkout, Tv];
    }
}