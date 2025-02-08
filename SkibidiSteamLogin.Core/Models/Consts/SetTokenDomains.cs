namespace SkibidiSteamLogin.Core.Models.Consts
{
    public static class SetTokenDomains
    {
        public const string SteamCommunityDomain = "steamcommunity.com";
        public const string StoreSteamPoweredDomain = "store.steampowered.com";
        public const string HelpSteamPoweredDomain = "help.steampowered.com";
        public const string CheckoutSteamPoweredDomain = "checkout.steampowered";
        public const string SteamTv = "steam.tv";

        public static List<string> AllDomains { get => [SteamCommunityDomain, StoreSteamPoweredDomain, HelpSteamPoweredDomain, CheckoutSteamPoweredDomain, SteamTv]; }
    }
}