using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SkibidiSteamLogin.Core.Enums;
using SkibidiSteamLogin.Core.Interfaces;
using SkibidiSteamLogin.Core.Models.Configurations;
using SkibidiSteamLogin.Core.Models.Externals;
using SkibidiSteamLogin.Core.Models.Internals;
using SkibidiSteamLogin.Core.Models.SteamResponses;
using SkibidiSteamLogin.Core.Services;

namespace SkibidiSteamLogin.Core.Tests.Services
{
    public class LoginHandlerTests
    {
        private readonly Mock<IHttpClientWrapper> _httpClientWrapperMock;
        private readonly Mock<ILogger<LoginHandler>> _loggerMock;
        private readonly IOptions<SkibidiLoginConfiguration> _options;
        private readonly LoginHandler _sut;

        public LoginHandlerTests()
        {
            _httpClientWrapperMock = new Mock<IHttpClientWrapper>();
            _loggerMock = new Mock<ILogger<LoginHandler>>();
            _options = Options.Create(new SkibidiLoginConfiguration
            {
                SetTokenDomains = ["steamcommunity.com"]
            });
            _sut = new LoginHandler(_httpClientWrapperMock.Object, _options, _loggerMock.Object);
        }

        private static RsaData CreateTestRsaData()
        {
            using var rsa = RSA.Create(2048);
            var parameters = rsa.ExportParameters(false);
            return new RsaData
            {
                Modulus = BitConverter.ToString(parameters.Modulus).Replace("-", ""),
                Exponent = BitConverter.ToString(parameters.Exponent).Replace("-", ""),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        [Fact]
        public async Task LoginAsync_WhenSessionFails_ReturnsFailure()
        {
            _httpClientWrapperMock
                .Setup(x => x.StartSessionAsync())
                .ReturnsAsync(new HttpResult { IsSuccess = false, StatusCode = HttpStatusCode.InternalServerError });

            var result = await _sut.LoginAsync("user", "pass");

            Assert.False(result.IsSuccess);
            Assert.Contains("session", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoginAsync_WhenRsaFails_ReturnsFailure()
        {
            _httpClientWrapperMock
                .Setup(x => x.StartSessionAsync())
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.GetRsaDataAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<RsaData> { IsSuccess = false, StatusCode = HttpStatusCode.BadRequest });

            var result = await _sut.LoginAsync("user", "pass");

            Assert.False(result.IsSuccess);
            Assert.Contains("RSA", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoginAsync_WhenLoginRequestFails_ReturnsFailure()
        {
            var rsaData = CreateTestRsaData();

            _httpClientWrapperMock
                .Setup(x => x.StartSessionAsync())
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.GetRsaDataAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<RsaData>
                {
                    IsSuccess = true,
                    Data = rsaData
                });

            _httpClientWrapperMock
                .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                .ReturnsAsync(new HttpDataResult<SteamLoginResponse> { IsSuccess = false, StatusCode = HttpStatusCode.Unauthorized });

            var result = await _sut.LoginAsync("user", "pass");

            Assert.False(result.IsSuccess);
            Assert.Contains("failed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LoginAsync_WhenSuccess_ReturnsLoginResult()
        {
            var rsaData = CreateTestRsaData();

            _httpClientWrapperMock
                .Setup(x => x.StartSessionAsync())
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.GetRsaDataAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<RsaData>
                {
                    IsSuccess = true,
                    Data = rsaData
                });

            _httpClientWrapperMock
                .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                .ReturnsAsync(new HttpDataResult<SteamLoginResponse>
                {
                    IsSuccess = true,
                    Data = new SteamLoginResponse
                    {
                        ClientId = "123",
                        SteamId = "456",
                        RequestId = "789",
                        AllowedConfirmations = new List<AllowedConfirmation>()
                    }
                });

            var result = await _sut.LoginAsync("user", "pass");

            Assert.True(result.IsSuccess);
            Assert.Equal("123", result.Data.ClientId);
            Assert.Equal("456", result.Data.SteamId);
            Assert.Equal("789", result.Data.RequestId);
        }

        [Fact]
        public async Task EnterSteamGuardCodeAsync_WhenGuardFails_ReturnsFailure()
        {
            var session = new LoginResult { ClientId = "1", SteamId = "2", RequestId = "3" };

            _httpClientWrapperMock
                .Setup(x => x.EnterSteamGuardCodeAsync(It.IsAny<SteamGuardRequest>()))
                .ReturnsAsync(new HttpResult { IsSuccess = false, StatusCode = HttpStatusCode.BadRequest });

            var result = await _sut.EnterSteamGuardCodeAsync(session, "code", AuthGuardType.EmailCode);

            Assert.False(result.IsSuccess);
            Assert.Contains("steam guard", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EnterSteamGuardCodeAsync_WhenPollFails_ReturnsFailure()
        {
            var session = new LoginResult { ClientId = "1", SteamId = "2", RequestId = "3" };

            _httpClientWrapperMock
                .Setup(x => x.EnterSteamGuardCodeAsync(It.IsAny<SteamGuardRequest>()))
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.PollAuthSessionStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<string> { IsSuccess = false, StatusCode = HttpStatusCode.BadRequest });

            var result = await _sut.EnterSteamGuardCodeAsync(session, "code", AuthGuardType.EmailCode);

            Assert.False(result.IsSuccess);
            Assert.Contains("Polling", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EnterSteamGuardCodeAsync_WhenFinalizeFails_ReturnsFailure()
        {
            var session = new LoginResult { ClientId = "1", SteamId = "2", RequestId = "3" };

            _httpClientWrapperMock
                .Setup(x => x.EnterSteamGuardCodeAsync(It.IsAny<SteamGuardRequest>()))
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.PollAuthSessionStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<string> { IsSuccess = true, Data = "refresh_token_value" });

            _httpClientWrapperMock
                .Setup(x => x.FinalizeLoginAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<FinalizeLoginResult> { IsSuccess = false, StatusCode = HttpStatusCode.InternalServerError });

            var result = await _sut.EnterSteamGuardCodeAsync(session, "code", AuthGuardType.EmailCode);

            Assert.False(result.IsSuccess);
            Assert.Contains("finalization", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EnterSteamGuardCodeAsync_WhenSuccess_SetsTokensAndReturnsSuccess()
        {
            var session = new LoginResult { ClientId = "1", SteamId = "2", RequestId = "3" };

            _httpClientWrapperMock
                .Setup(x => x.EnterSteamGuardCodeAsync(It.IsAny<SteamGuardRequest>()))
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.PollAuthSessionStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<string> { IsSuccess = true, Data = "refresh_token_value" });

            _httpClientWrapperMock
                .Setup(x => x.FinalizeLoginAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<FinalizeLoginResult>
                {
                    IsSuccess = true,
                    Data = new FinalizeLoginResult
                    {
                        SteamID = "2",
                        TransferInfo = new List<TransferInfo>
                        {
                            new TransferInfo
                            {
                                Url = "https://steamcommunity.com/settoken",
                                Params = new Params { Auth = "auth_value", Nonce = "nonce_value" }
                            },
                            new TransferInfo
                            {
                                Url = "https://other.domain.com/settoken",
                                Params = new Params { Auth = "auth2", Nonce = "nonce2" }
                            }
                        }
                    }
                });

            _httpClientWrapperMock
                .Setup(x => x.SetTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            var result = await _sut.EnterSteamGuardCodeAsync(session, "code", AuthGuardType.EmailCode);

            Assert.True(result.IsSuccess);
            Assert.Equal(session, result.Data);

            // Only steamcommunity.com token should be set (matching configured domain)
            _httpClientWrapperMock.Verify(
                x => x.SetTokenAsync("2", "auth_value", "nonce_value", "https://steamcommunity.com/settoken"),
                Times.Once);

            // other.domain.com should NOT be called
            _httpClientWrapperMock.Verify(
                x => x.SetTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "https://other.domain.com/settoken"),
                Times.Never);
        }

        [Fact]
        public void GetCookies_DelegatesToWrapper()
        {
            var expectedCookies = new CookieCollection { new Cookie("test", "value", "/", "example.com") };

            _httpClientWrapperMock
                .Setup(x => x.GetCookies())
                .Returns(expectedCookies);

            var result = _sut.GetCookies();

            Assert.Equal(expectedCookies, result);
        }

        [Fact]
        public void SetCookies_DelegatesToWrapper()
        {
            var cookies = new CookieCollection { new Cookie("test", "value", "/", "example.com") };

            _sut.SetCookies(cookies);

            _httpClientWrapperMock.Verify(x => x.SetCookies(cookies), Times.Once);
        }

        #region LoginAsync Edge Cases

        [Fact]
        public async Task LoginAsync_WhenPasswordIsNull_ThrowsArgumentNullException()
        {
            var rsaData = CreateTestRsaData();

            _httpClientWrapperMock
                .Setup(x => x.StartSessionAsync())
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.GetRsaDataAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<RsaData> { IsSuccess = true, Data = rsaData });

            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.LoginAsync("user", null));
        }

        [Fact]
        public async Task LoginAsync_WhenPasswordIsEmpty_ThrowsArgumentNullException()
        {
            var rsaData = CreateTestRsaData();

            _httpClientWrapperMock
                .Setup(x => x.StartSessionAsync())
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.GetRsaDataAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<RsaData> { IsSuccess = true, Data = rsaData });

            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.LoginAsync("user", ""));
        }

        [Fact]
        public async Task LoginAsync_WhenPasswordIsWhitespace_ThrowsArgumentNullException()
        {
            var rsaData = CreateTestRsaData();

            _httpClientWrapperMock
                .Setup(x => x.StartSessionAsync())
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.GetRsaDataAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<RsaData> { IsSuccess = true, Data = rsaData });

            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.LoginAsync("user", "   "));
        }

        [Fact]
        public async Task LoginAsync_WhenRsaDataHasNullModulus_ThrowsArgumentNullException()
        {
            _httpClientWrapperMock
                .Setup(x => x.StartSessionAsync())
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.GetRsaDataAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<RsaData>
                {
                    IsSuccess = true,
                    Data = new RsaData { Modulus = null, Exponent = "010001", Timestamp = 123 }
                });

            await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.LoginAsync("user", "pass"));
        }

        [Fact]
        public async Task LoginAsync_WhenStartSessionThrowsHttpRequestException_PropagatesException()
        {
            _httpClientWrapperMock
                .Setup(x => x.StartSessionAsync())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            await Assert.ThrowsAsync<HttpRequestException>(() => _sut.LoginAsync("user", "pass"));
        }

        [Fact]
        public async Task LoginAsync_WhenGetRsaDataThrowsHttpRequestException_PropagatesException()
        {
            _httpClientWrapperMock
                .Setup(x => x.StartSessionAsync())
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.GetRsaDataAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Timeout"));

            await Assert.ThrowsAsync<HttpRequestException>(() => _sut.LoginAsync("user", "pass"));
        }

        [Fact]
        public async Task LoginAsync_WhenLoginResponseHasNullFields_ReturnsSuccessWithNullFields()
        {
            var rsaData = CreateTestRsaData();

            _httpClientWrapperMock
                .Setup(x => x.StartSessionAsync())
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.GetRsaDataAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<RsaData> { IsSuccess = true, Data = rsaData });

            _httpClientWrapperMock
                .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                .ReturnsAsync(new HttpDataResult<SteamLoginResponse>
                {
                    IsSuccess = true,
                    Data = new SteamLoginResponse
                    {
                        ClientId = null,
                        SteamId = null,
                        RequestId = null,
                        AllowedConfirmations = null
                    }
                });

            var result = await _sut.LoginAsync("user", "pass");

            Assert.True(result.IsSuccess);
            Assert.Null(result.Data.ClientId);
            Assert.Null(result.Data.SteamId);
            Assert.Null(result.Data.RequestId);
        }

        #endregion

        #region EnterSteamGuardCodeAsync Edge Cases

        [Fact]
        public async Task EnterSteamGuardCodeAsync_WhenSessionIsNull_ThrowsNullReferenceException()
        {
            await Assert.ThrowsAsync<NullReferenceException>(
                () => _sut.EnterSteamGuardCodeAsync(null, "code", AuthGuardType.EmailCode));
        }

        [Fact]
        public async Task EnterSteamGuardCodeAsync_WhenTransferInfoIsEmpty_DoesNotCallSetToken()
        {
            var session = new LoginResult { ClientId = "1", SteamId = "2", RequestId = "3" };

            _httpClientWrapperMock
                .Setup(x => x.EnterSteamGuardCodeAsync(It.IsAny<SteamGuardRequest>()))
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.PollAuthSessionStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<string> { IsSuccess = true, Data = "token" });

            _httpClientWrapperMock
                .Setup(x => x.FinalizeLoginAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<FinalizeLoginResult>
                {
                    IsSuccess = true,
                    Data = new FinalizeLoginResult
                    {
                        SteamID = "2",
                        TransferInfo = new List<TransferInfo>()
                    }
                });

            var result = await _sut.EnterSteamGuardCodeAsync(session, "code", AuthGuardType.EmailCode);

            Assert.True(result.IsSuccess);
            _httpClientWrapperMock.Verify(
                x => x.SetTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task EnterSteamGuardCodeAsync_WhenTransferInfoIsNull_ThrowsArgumentNullException()
        {
            var session = new LoginResult { ClientId = "1", SteamId = "2", RequestId = "3" };

            _httpClientWrapperMock
                .Setup(x => x.EnterSteamGuardCodeAsync(It.IsAny<SteamGuardRequest>()))
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.PollAuthSessionStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<string> { IsSuccess = true, Data = "token" });

            _httpClientWrapperMock
                .Setup(x => x.FinalizeLoginAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<FinalizeLoginResult>
                {
                    IsSuccess = true,
                    Data = new FinalizeLoginResult
                    {
                        SteamID = "2",
                        TransferInfo = null
                    }
                });

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _sut.EnterSteamGuardCodeAsync(session, "code", AuthGuardType.EmailCode));
        }

        [Fact]
        public async Task EnterSteamGuardCodeAsync_WhenSetTokenFails_ContinuesAndReturnsSuccess()
        {
            var session = new LoginResult { ClientId = "1", SteamId = "2", RequestId = "3" };

            _httpClientWrapperMock
                .Setup(x => x.EnterSteamGuardCodeAsync(It.IsAny<SteamGuardRequest>()))
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.PollAuthSessionStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<string> { IsSuccess = true, Data = "token" });

            _httpClientWrapperMock
                .Setup(x => x.FinalizeLoginAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<FinalizeLoginResult>
                {
                    IsSuccess = true,
                    Data = new FinalizeLoginResult
                    {
                        SteamID = "2",
                        TransferInfo = new List<TransferInfo>
                        {
                            new TransferInfo
                            {
                                Url = "https://steamcommunity.com/settoken",
                                Params = new Params { Auth = "auth", Nonce = "nonce" }
                            }
                        }
                    }
                });

            _httpClientWrapperMock
                .Setup(x => x.SetTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpResult { IsSuccess = false, StatusCode = HttpStatusCode.InternalServerError });

            var result = await _sut.EnterSteamGuardCodeAsync(session, "code", AuthGuardType.EmailCode);

            Assert.True(result.IsSuccess);
            _httpClientWrapperMock.Verify(
                x => x.SetTokenAsync("2", "auth", "nonce", "https://steamcommunity.com/settoken"),
                Times.Once);
        }

        [Fact]
        public async Task EnterSteamGuardCodeAsync_WithUnknownGuardType_SubmitsCodeSuccessfully()
        {
            var session = new LoginResult { ClientId = "1", SteamId = "2", RequestId = "3" };

            _httpClientWrapperMock
                .Setup(x => x.EnterSteamGuardCodeAsync(It.Is<SteamGuardRequest>(r => r.CodeType == AuthGuardType.Unknown)))
                .ReturnsAsync(new HttpResult { IsSuccess = true });

            _httpClientWrapperMock
                .Setup(x => x.PollAuthSessionStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<string> { IsSuccess = true, Data = "token" });

            _httpClientWrapperMock
                .Setup(x => x.FinalizeLoginAsync(It.IsAny<string>()))
                .ReturnsAsync(new HttpDataResult<FinalizeLoginResult>
                {
                    IsSuccess = true,
                    Data = new FinalizeLoginResult
                    {
                        SteamID = "2",
                        TransferInfo = new List<TransferInfo>()
                    }
                });

            var result = await _sut.EnterSteamGuardCodeAsync(session, "code", AuthGuardType.Unknown);

            Assert.True(result.IsSuccess);
            _httpClientWrapperMock.Verify(
                x => x.EnterSteamGuardCodeAsync(It.Is<SteamGuardRequest>(r => r.CodeType == AuthGuardType.Unknown)),
                Times.Once);
        }

        #endregion
    }
}
