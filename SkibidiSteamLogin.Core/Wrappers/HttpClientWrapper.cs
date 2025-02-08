using System.Net;
using Newtonsoft.Json;
using SkibidiSteamLogin.Core.Interfaces;
using SkibidiSteamLogin.Core.Mapping;
using SkibidiSteamLogin.Core.Models.SteamResponses;
using SkibidiSteamLogin.Core.Models.Consts;
using SkibidiSteamLogin.Core.Models.Internals;
using SkibidiSteamLogin.Core.Models.Externals;

namespace SkibidiSteamLogin.Core.Wrappers
{
    internal class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;

        public HttpClientWrapper()
        {
            _cookieContainer = new CookieContainer();

            var httpHandler = new HttpClientHandler()
            { 
                CookieContainer = _cookieContainer
            };
            httpHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            _httpClient = new HttpClient(httpHandler);
        }

        public async Task<HttpResult> StartSessionAsync()
        {
            var result = await _httpClient.GetAsync(Endpoints.SteamCommunityUrlBase);

            return result.ToHttpResult();
        }

        public async Task<HttpDataResult<RsaData>> GetRsaDataAsync(string username)
        {
            var result = await _httpClient.GetAsync(Endpoints.SteamPoweredUrlBase + Endpoints.GetRsa + "?account_name=" + username);

            RsaData data = null;

            if (result.IsSuccessStatusCode)
            {
                var str = await result.Content.ReadAsStringAsync();
                var response = JsonConvert.DeserializeObject<SteamResponseWrapper<RsaData>>(str);

                data = response.Data;
            }

            return result.ToHttpDataResult(data);
        }

        public async Task<HttpDataResult<SteamLoginResponse>> LoginAsync(string username, string encryptedPassword, long timestamp)
        {
            var uri = new Uri(Endpoints.SteamPoweredUrlBase + Endpoints.CredentialsSessionStart + $"?account_name={username}&encrypted_password={Uri.EscapeDataString(encryptedPassword)}&encryption_timestamp={timestamp}");

            var msg  = new HttpRequestMessage(HttpMethod.Post, uri);

            ApplyHeaders(msg);

            var result = await _httpClient.SendAsync(msg);

            var resonseString = await result.Content.ReadAsStringAsync();

            var responseData = JsonConvert.DeserializeObject<SteamResponseWrapper<SteamLoginResponse>>(resonseString);

            return result.ToHttpDataResult(responseData.Data);
        }

        public async Task<HttpResult> EnterSteamGuardCodeAsync(SteamGuardRequest steamGuardRequest)
        {
            var queryParams = $"?client_id={steamGuardRequest.ClientId}&steamid={steamGuardRequest.SteamId}&code={steamGuardRequest.Code}&code_type={(byte)steamGuardRequest.CodeType}";
            
            var uri = new Uri(Endpoints.SteamPoweredUrlBase + Endpoints.CredentialsSessionUpdateWithGuardCode + queryParams);

            var msg = new HttpRequestMessage(HttpMethod.Post, uri);

            ApplyHeaders(msg);

            var result = await _httpClient.SendAsync(msg);

            var resonseString = await result.Content.ReadAsStringAsync();

            return result.ToHttpResult();
        }

        public async Task<HttpDataResult<string>> PollAuthSessionStatusAsync(string clientId, string requestId)
        {
            var queryParams = $"?client_id={clientId}&request_id={Uri.EscapeDataString(requestId)}";

            var uri = new Uri(Endpoints.SteamPoweredUrlBase + Endpoints.PollAuthSessionStatus + queryParams);

            var msg = new HttpRequestMessage(HttpMethod.Post, uri);

            ApplyHeaders(msg, false);

            var result = await _httpClient.SendAsync(msg);

            var resonseString = await result.Content.ReadAsStringAsync();

            var responseData = JsonConvert.DeserializeObject<SteamResponseWrapper<PollStatusResponse>>(resonseString);

            return result.ToHttpDataResult(responseData.Data.RefreshToken);
        }

        public async Task<HttpDataResult<FinalizeLoginResult>> FinalizeLoginAsync(string token)
        {
            var uri = new Uri(Endpoints.SteamLoginUrlBase + Endpoints.FinalizeLogin);

            var cookies = GetCookies();
            var sessionId = cookies.First(x => x.Name.Equals("sessionid")).Value;

            var boundary = "----WebKitFormBoundarysMZXRB5xhtSNbrDh";
            var multipartContent = new MultipartFormDataContent(boundary)
            {
                { new StringContent(token), "nonce" },
                { new StringContent(sessionId), "sessionid" },
                { new StringContent("https://steamcommunity.com/login/home/?goto="), "redir" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = multipartContent
            };

            ApplyHeadersFinalize(request);

            var result = await _httpClient.SendAsync(request);

            var resonseString = await result.Content.ReadAsStringAsync();

            var responseData = JsonConvert.DeserializeObject<FinalizeLoginResult>(resonseString);

            return result.ToHttpDataResult(responseData);
        }

        public async Task<HttpResult> SetToken(string steamId, string auth, string nonce, string url)
        {
            var uri = new Uri(url);

            var cookies = GetCookies();
            var sessionId = cookies.First(x => x.Name.Equals("sessionid")).Value;

            var boundary = "----WebKitFormBoundarysMZXRB5xhtSNbrDh";
            var multipartContent = new MultipartFormDataContent(boundary)
            {
                { new StringContent(nonce), "nonce" },
                { new StringContent(auth), "auth" },
                { new StringContent(steamId), "steamID" },
            };

            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = multipartContent
            };

            var result = await _httpClient.SendAsync(request);

            return result.ToHttpResult();
        }

        private void ApplyHeaders(HttpRequestMessage httpRequestMessage, bool encoded = true)
        {
            httpRequestMessage.Headers.TryAddWithoutValidation("Accept-Language", "pl-PL,pl;q=0.9,en-US;q=0.8,en;q=0.7");
            httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0");
            
            if (encoded)
            {
                httpRequestMessage.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                httpRequestMessage.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br, zstd");
                httpRequestMessage.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
            }
        }

        private void ApplyHeadersFinalize(HttpRequestMessage httpRequestMessage)
        {
            httpRequestMessage.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
            httpRequestMessage.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br, zstd");
            httpRequestMessage.Headers.TryAddWithoutValidation("Accept-Language", "pl,en;q=0.9,en-GB;q=0.8,en-US;q=0.7");
            httpRequestMessage.Headers.TryAddWithoutValidation("Connection", "keep-alive");
            httpRequestMessage.Headers.TryAddWithoutValidation("Host", "login.steampowered.com");
            httpRequestMessage.Headers.TryAddWithoutValidation("Origin", "https://steamcommunity.com");
            httpRequestMessage.Headers.TryAddWithoutValidation("Referer", "https://steamcommunity.com/");
            httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Ch-Ua", "\"Microsoft Edge\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"");
            httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Ch-Ua-Mobile", "?0");
            httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Ch-Ua-Platform", "\"Windows\"");
            httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
            httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
            httpRequestMessage.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");
            httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0");
        }

        public CookieCollection GetCookies()
        {
            return _cookieContainer.GetAllCookies();
        }

        public void SetCookies(CookieCollection cookieCollection)
        {
            throw new NotImplementedException();
        }
    }
}
