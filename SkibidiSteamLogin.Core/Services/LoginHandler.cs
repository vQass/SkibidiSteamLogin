using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkibidiSteamLogin.Core.Enums;
using SkibidiSteamLogin.Core.Helpers;
using SkibidiSteamLogin.Core.Interfaces;
using SkibidiSteamLogin.Core.Mapping;
using SkibidiSteamLogin.Core.Models.Configurations;
using SkibidiSteamLogin.Core.Models.Externals;
using SkibidiSteamLogin.Core.Models.SteamResponses;

namespace SkibidiSteamLogin.Core.Services
{
    internal class LoginHandler : ILoginHandler
    {
        /// <summary>
        /// Steam API requires a short delay before polling for session status after submitting a guard code.
        /// </summary>
        private const int SteamGuardPollDelayMs = 500;

        private readonly IHttpClientWrapper _httpClientWrapper;
        private readonly SkibidiLoginConfiguration _options;
        private readonly ILogger<LoginHandler> _logger;

        public LoginHandler(
            IHttpClientWrapper httpClientWrapper,
            IOptions<SkibidiLoginConfiguration> options,
            ILogger<LoginHandler> logger)
        {
            _httpClientWrapper = httpClientWrapper;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<OperationResult<LoginResult>> LoginAsync(string username, string password)
        {
            _logger.LogInformation("Starting login for user {Username}.", username);

            var sessionResult = await _httpClientWrapper.StartSessionAsync();
            if (!sessionResult.IsSuccess)
            {
                _logger.LogWarning("Failed to start Steam session. Status: {StatusCode}", sessionResult.StatusCode);
                return OperationResult<LoginResult>.Failure("Failed to start Steam session.");
            }

            var rsaResult = await _httpClientWrapper.GetRsaDataAsync(username);
            if (!rsaResult.IsSuccess || rsaResult.Data is null)
            {
                _logger.LogWarning("Failed to fetch RSA data. Status: {StatusCode}", rsaResult.StatusCode);
                return OperationResult<LoginResult>.Failure("Failed to fetch RSA data.");
            }

            var encryptedPassword = EncryptionHelper.EncryptPassword(rsaResult.Data, password);

            var loginResult = await _httpClientWrapper.LoginAsync(username, encryptedPassword, rsaResult.Data.Timestamp);
            if (!loginResult.IsSuccess || loginResult.Data is null)
            {
                _logger.LogWarning("Login request failed. Status: {StatusCode}", loginResult.StatusCode);
                return OperationResult<LoginResult>.Failure("Login request failed.");
            }

            var result = loginResult.Data.ToLoginResult();
            _logger.LogInformation("Login successful for user {Username}.", username);
            return OperationResult<LoginResult>.Success(result);
        }

        public async Task<OperationResult<LoginResult>> EnterSteamGuardCodeAsync(
            LoginResult loginSession, string authCode, AuthGuardType guardType)
        {
            _logger.LogInformation("Submitting Steam Guard code for SteamId {SteamId}.", loginSession.SteamId);

            var submitResult = await SubmitGuardCodeAsync(loginSession, authCode, guardType);
            if (!submitResult.IsSuccess)
                return submitResult;

            var pollResult = await PollForRefreshTokenAsync(loginSession);
            if (!pollResult.IsSuccess)
                return OperationResult<LoginResult>.Failure(pollResult.ErrorMessage);

            var finalizeResult = await FinalizeAndApplyTokensAsync(loginSession, pollResult.Data);
            if (!finalizeResult.IsSuccess)
                return finalizeResult;

            _logger.LogInformation("Steam Guard flow completed successfully.");
            return OperationResult<LoginResult>.Success(loginSession);
        }

        private async Task<OperationResult<LoginResult>> SubmitGuardCodeAsync(
            LoginResult session, string authCode, AuthGuardType guardType)
        {
            var request = new SteamGuardRequest
            {
                ClientId = session.ClientId,
                SteamId = session.SteamId,
                Code = authCode,
                CodeType = guardType
            };

            if (guardTypeEnum != AuthGuardTypeEnum.None)
            {
                var result = await _httpClientWrapper.EnterSteamGuardCodeAsync(steamGuardRequest);
            }

            await Task.Delay(500);

            return OperationResult<LoginResult>.Success(session);
        }

        private async Task<OperationResult<string>> PollForRefreshTokenAsync(LoginResult session)
        {
            await Task.Delay(SteamGuardPollDelayMs);

            var pollResult = await _httpClientWrapper.PollAuthSessionStatusAsync(
                session.ClientId, session.RequestId);

            if (!pollResult.IsSuccess || pollResult.Data is null)
            {
                _logger.LogWarning("Polling auth session status failed.");
                return OperationResult<string>.Failure("Polling auth session status failed.");
            }

            return OperationResult<string>.Success(pollResult.Data);
        }

        private async Task<OperationResult<LoginResult>> FinalizeAndApplyTokensAsync(
            LoginResult session, string refreshToken)
        {
            var finalizeResult = await _httpClientWrapper.FinalizeLoginAsync(refreshToken);
            if (!finalizeResult.IsSuccess || finalizeResult.Data is null)
            {
                _logger.LogWarning("Login finalization failed.");
                return OperationResult<LoginResult>.Failure("Login finalization failed.");
            }

            await ApplyRelevantTokensAsync(session.SteamId, finalizeResult.Data);
            return OperationResult<LoginResult>.Success(session);
        }

        private async Task ApplyRelevantTokensAsync(string steamId, FinalizeLoginResult finalizeData)
        {
            var relevantTokens = finalizeData.TransferInfo
                .Where(t => _options.SetTokenDomains.Any(domain => t.Url.Contains(domain)));

            foreach (var token in relevantTokens)
            {
                var result = await _httpClientWrapper.SetTokenAsync(
                    steamId, token.Params.Auth, token.Params.Nonce, token.Url);

                if (!result.IsSuccess)
                    _logger.LogWarning("Failed to set token for {Url}.", token.Url);
            }
        }

        public CookieCollection GetCookies()
        {
            return _httpClientWrapper.GetCookies();
        }

        public void SetCookies(CookieCollection cookies)
        {
            _httpClientWrapper.SetCookies(cookies);
        }
    }
}
