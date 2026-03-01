namespace SkibidiSteamLogin.Core.Models.Consts
{
    internal static class HttpHeaderConstants
    {
        // Client identification
        internal const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0";
        internal const string AcceptLanguage = "pl-PL,pl;q=0.9,en-US;q=0.8,en;q=0.7";
        internal const string AcceptLanguageFinalize = "pl,en;q=0.9,en-GB;q=0.8,en-US;q=0.7";

        // Response formats
        internal const string AcceptHtml = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";
        internal const string AcceptJson = "application/json, text/plain, */*";
        internal const string AcceptEncoding = "gzip, deflate, br, zstd";

        // Headers required by Steam login endpoint for browser emulation
        internal const string FinalizeHost = "login.steampowered.com";
        internal const string FinalizeOrigin = "https://steamcommunity.com";
        internal const string FinalizeReferer = "https://steamcommunity.com/";
        internal const string SecChUa = "\"Microsoft Edge\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"";
        internal const string SecChUaMobile = "?0";
        internal const string SecChUaPlatform = "\"Windows\"";
    }
}
