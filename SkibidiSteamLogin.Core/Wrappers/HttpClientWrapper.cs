using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkibidiSteamLogin.Core.Interfaces;
using SkibidiSteamLogin.Core.Mapping;
using SkibidiSteamLogin.Core.Models.SteamResponses;
using SkibidiSteamLogin.Core.Models.Consts;
using SkibidiSteamLogin.Core.Models.Internals;
using SkibidiSteamLogin.Core.Models.Externals;

namespace SkibidiSteamLogin.Core.Wrappers
{
    internal class HttpClientWrapper : IHttpClientWrapper, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _httpClientHandler;
        private readonly CookieContainer _cookieContainer;
        private readonly ILogger<HttpClientWrapper> _logger;
        private bool _disposed;

        public HttpClientWrapper(ILogger<HttpClientWrapper> logger)
        {
            _logger = logger;
            _cookieContainer = new CookieContainer();
            _httpClientHandler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer
            };
            _httpClient = new HttpClient(_httpClientHandler);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", HttpHeaderConstants.UserAgent);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", HttpHeaderConstants.AcceptLanguage);
        }

        public async Task<HttpResult> StartSessionAsync()
        {
            _logger.LogDebug("Starting Steam session.");
            var result = await _httpClient.GetAsync(Endpoints.SteamCommunityUrlBase);
            return result.ToHttpResult();
        }

        public async Task<HttpDataResult<RsaData>> GetRsaDataAsync(string username)
        {
            _logger.LogDebug("Fetching RSA data for user {Username}.", username);

            var response = await _httpClient.GetAsync(
                Endpoints.SteamPoweredUrlBase + Endpoints.GetRsa + "?account_name=" + username);

            return await SendAndDeserializeAsync<SteamResponseWrapper<RsaData>, RsaData>(
                response, wrapper => wrapper.Data, "Fetch RSA data");
        }

        public async Task<HttpDataResult<SteamLoginResponse>> LoginAsync(string username, string encryptedPassword, long timestamp)
        {
            _logger.LogDebug("Sending login request for user {Username}.", username);
            var uri = new Uri(Endpoints.SteamPoweredUrlBase + Endpoints.CredentialsSessionStart);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["account_name"] = username,
                ["encrypted_password"] = encryptedPassword,
                ["encryption_timestamp"] = timestamp.ToString()
            });

            var msg = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
            ApplyHeaders(msg);

            var response = await _httpClient.SendAsync(msg);

            return await SendAndDeserializeAsync<SteamResponseWrapper<SteamLoginResponse>, SteamLoginResponse>(
                response, wrapper => wrapper.Data, "Login request");
        }

        public async Task<HttpResult> EnterSteamGuardCodeAsync(SteamGuardRequest steamGuardRequest)
        {
            _logger.LogDebug("Submitting Steam Guard code.");
            var uri = new Uri(Endpoints.SteamPoweredUrlBase + Endpoints.CredentialsSessionUpdateWithGuardCode);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = steamGuardRequest.ClientId,
                ["steamid"] = steamGuardRequest.SteamId,
                ["code"] = steamGuardRequest.Code,
                ["code_type"] = ((byte)steamGuardRequest.CodeType).ToString()
            });

            var msg = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
            ApplyHeaders(msg);

            var result = await _httpClient.SendAsync(msg);

            if (!result.IsSuccessStatusCode)
                _logger.LogWarning("Steam Guard code submission failed. Status: {StatusCode}", result.StatusCode);

            return result.ToHttpResult();
        }

        public async Task<HttpDataResult<string>> PollAuthSessionStatusAsync(string clientId, string requestId)
        {
            _logger.LogDebug("Polling auth session status.");
            var uri = new Uri(Endpoints.SteamPoweredUrlBase + Endpoints.PollAuthSessionStatus);

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["request_id"] = requestId
            });

            var msg = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
            ApplyHeaders(msg);

            var response = await _httpClient.SendAsync(msg);

            return await SendAndDeserializeAsync<SteamResponseWrapper<PollStatusResponse>, string>(
                response, wrapper => wrapper.Data?.RefreshToken, "Poll auth session status");
        }

        public async Task<HttpDataResult<FinalizeLoginResult>> FinalizeLoginAsync(string token)
        {
            _logger.LogDebug("Finalizing login.");
            var uri = new Uri(Endpoints.SteamLoginUrlBase + Endpoints.FinalizeLogin);

            var cookies = GetCookies();
            var sessionId = cookies.FirstOrDefault(x => x.Name.Equals("sessionid"))?.Value;

            var multipartContent = new MultipartFormDataContent
            {
                { new StringContent(token), "nonce" },
                { new StringContent(sessionId ?? string.Empty), "sessionid" },
                { new StringContent("https://steamcommunity.com/login/home/?goto="), "redir" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = multipartContent
            };

            ApplyFinalizeHeaders(request);

            var response = await _httpClient.SendAsync(request);

            return await SendAndDeserializeAsync<FinalizeLoginResult, FinalizeLoginResult>(
                response, data => data, "Finalize login");
        }

        public async Task<HttpResult> SetTokenAsync(string steamId, string auth, string nonce, string url)
        {
            _logger.LogDebug("Setting token for {Url}.", url);
            var uri = new Uri(url);

            var multipartContent = new MultipartFormDataContent
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

            if (!result.IsSuccessStatusCode)
                _logger.LogWarning("Set token failed for {Url}. Status: {StatusCode}", url, result.StatusCode);

            return result.ToHttpResult();
        }

        private static void ApplyHeaders(HttpRequestMessage httpRequestMessage)
        {
            httpRequestMessage.Headers.TryAddWithoutValidation("Accept", HttpHeaderConstants.AcceptHtml);
            httpRequestMessage.Headers.TryAddWithoutValidation("Accept-Encoding", HttpHeaderConstants.AcceptEncoding);
        }

        /// <summary>
        /// Steam's login finalization endpoint requires browser-emulating headers.
        /// Without them the server returns 403.
        /// </summary>
        private static void ApplyFinalizeHeaders(HttpRequestMessage request)
        {
            var h = request.Headers;
            h.TryAddWithoutValidation("Accept", HttpHeaderConstants.AcceptJson);
            h.TryAddWithoutValidation("Accept-Encoding", HttpHeaderConstants.AcceptEncoding);
            h.TryAddWithoutValidation("Accept-Language", HttpHeaderConstants.AcceptLanguageFinalize);
            h.TryAddWithoutValidation("Connection", "keep-alive");
            h.TryAddWithoutValidation("Host", HttpHeaderConstants.FinalizeHost);
            h.TryAddWithoutValidation("Origin", HttpHeaderConstants.FinalizeOrigin);
            h.TryAddWithoutValidation("Referer", HttpHeaderConstants.FinalizeReferer);
            h.TryAddWithoutValidation("Sec-Ch-Ua", HttpHeaderConstants.SecChUa);
            h.TryAddWithoutValidation("Sec-Ch-Ua-Mobile", HttpHeaderConstants.SecChUaMobile);
            h.TryAddWithoutValidation("Sec-Ch-Ua-Platform", HttpHeaderConstants.SecChUaPlatform);
            h.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
            h.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
            h.TryAddWithoutValidation("Sec-Fetch-Site", "cross-site");
        }

        private async Task<HttpDataResult<TResult>> SendAndDeserializeAsync<TResponse, TResult>(
            HttpResponseMessage response,
            Func<TResponse, TResult> selector,
            string operationName) where TResult : class
        {
            TResult data = null;

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var deserialized = JsonConvert.DeserializeObject<TResponse>(json);
                data = deserialized is not null ? selector(deserialized) : null;
            }
            else
            {
                _logger.LogWarning("{Operation} failed. Status: {StatusCode}", operationName, response.StatusCode);
            }

            return response.ToHttpDataResult(data);
        }

        public CookieCollection GetCookies()
        {
            return _cookieContainer.GetAllCookies();
        }

        public void SetCookies(CookieCollection cookieCollection)
        {
            foreach (Cookie cookie in cookieCollection)
            {
                _cookieContainer.Add(cookie);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _httpClientHandler?.Dispose();
                _disposed = true;
            }
        }
    }
}
