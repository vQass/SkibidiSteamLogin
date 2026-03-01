using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SkibidiSteamLogin.Core.Enums;
using SkibidiSteamLogin.Core.Interfaces;
using SkibidiSteamLogin.Core.Models.Externals;
using SkibidiSteamLogin.WebApi.Controllers;

namespace SkibidiSteamLogin.WebApi.Tests.Controllers
{
    public class LoginHandlerControllerTests
    {
        private readonly Mock<ILoginHandler> _loginHandlerMock;
        private readonly Mock<ICookiePersistenceService> _cookiePersistenceServiceMock;
        private readonly LoginHandlerController _sut;

        public LoginHandlerControllerTests()
        {
            _loginHandlerMock = new Mock<ILoginHandler>();
            _cookiePersistenceServiceMock = new Mock<ICookiePersistenceService>();
            _sut = new LoginHandlerController(_loginHandlerMock.Object, _cookiePersistenceServiceMock.Object);
        }

        #region Login

        [Fact]
        public async Task Login_NullUsername_ReturnsBadRequest()
        {
            var request = new LoginRequest(null, "password");

            var result = await _sut.Login(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_EmptyUsername_ReturnsBadRequest()
        {
            var request = new LoginRequest("", "password");

            var result = await _sut.Login(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_WhitespaceUsername_ReturnsBadRequest()
        {
            var request = new LoginRequest("   ", "password");

            var result = await _sut.Login(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_NullPassword_ReturnsBadRequest()
        {
            var request = new LoginRequest("user", null);

            var result = await _sut.Login(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_EmptyPassword_ReturnsBadRequest()
        {
            var request = new LoginRequest("user", "");

            var result = await _sut.Login(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_WhitespacePassword_ReturnsBadRequest()
        {
            var request = new LoginRequest("user", "   ");

            var result = await _sut.Login(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ValidCredentials_Success_ReturnsOkWithData()
        {
            var request = new LoginRequest("user", "pass");
            var loginResult = new LoginResult { ClientId = "1", SteamId = "2", RequestId = "3" };

            _loginHandlerMock
                .Setup(x => x.LoginAsync("user", "pass"))
                .ReturnsAsync(OperationResult<LoginResult>.Success(loginResult));

            var result = await _sut.Login(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var data = Assert.IsType<OperationResult<LoginResult>>(okResult.Value);
            Assert.True(data.IsSuccess);
            Assert.Equal("1", data.Data.ClientId);
        }

        [Fact]
        public async Task Login_ValidCredentials_Failure_ReturnsBadRequest()
        {
            var request = new LoginRequest("user", "pass");

            _loginHandlerMock
                .Setup(x => x.LoginAsync("user", "pass"))
                .ReturnsAsync(OperationResult<LoginResult>.Failure("Login failed"));

            var result = await _sut.Login(request);

            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            var data = Assert.IsType<OperationResult<LoginResult>>(badResult.Value);
            Assert.False(data.IsSuccess);
        }

        [Fact]
        public async Task Login_WhenHandlerThrowsException_ExceptionPropagates()
        {
            var request = new LoginRequest("user", "pass");

            _loginHandlerMock
                .Setup(x => x.LoginAsync("user", "pass"))
                .ThrowsAsync(new HttpRequestException("Connection failed"));

            await Assert.ThrowsAsync<HttpRequestException>(() => _sut.Login(request));
        }

        #endregion

        #region EnterSteamGuardCode

        [Fact]
        public async Task EnterSteamGuardCode_ValidRequest_Success_ReturnsOk()
        {
            var request = new SteamGuardCodeRequest("1", "2", "3", "code", AuthGuardType.EmailCode);

            _loginHandlerMock
                .Setup(x => x.EnterSteamGuardCodeAsync(
                    It.Is<LoginResult>(r => r.ClientId == "1" && r.SteamId == "2" && r.RequestId == "3"),
                    "code",
                    AuthGuardType.EmailCode))
                .ReturnsAsync(OperationResult<LoginResult>.Success(new LoginResult { ClientId = "1", SteamId = "2", RequestId = "3" }));

            var result = await _sut.EnterSteamGuardCode(request);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task EnterSteamGuardCode_Failure_ReturnsBadRequest()
        {
            var request = new SteamGuardCodeRequest("1", "2", "3", "code", AuthGuardType.EmailCode);

            _loginHandlerMock
                .Setup(x => x.EnterSteamGuardCodeAsync(It.IsAny<LoginResult>(), It.IsAny<string>(), It.IsAny<AuthGuardType>()))
                .ReturnsAsync(OperationResult<LoginResult>.Failure("Guard code failed"));

            var result = await _sut.EnterSteamGuardCode(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task EnterSteamGuardCode_NullClientIdAndSteamId_DelegatesNullsToHandler()
        {
            var request = new SteamGuardCodeRequest(null, null, null, "code", AuthGuardType.EmailCode);

            _loginHandlerMock
                .Setup(x => x.EnterSteamGuardCodeAsync(
                    It.Is<LoginResult>(r => r.ClientId == null && r.SteamId == null && r.RequestId == null),
                    "code",
                    AuthGuardType.EmailCode))
                .ReturnsAsync(OperationResult<LoginResult>.Success(new LoginResult()));

            var result = await _sut.EnterSteamGuardCode(request);

            Assert.IsType<OkObjectResult>(result);
            _loginHandlerMock.Verify(x => x.EnterSteamGuardCodeAsync(
                It.Is<LoginResult>(r => r.ClientId == null && r.SteamId == null),
                "code", AuthGuardType.EmailCode), Times.Once);
        }

        [Fact]
        public async Task EnterSteamGuardCode_WhenHandlerThrows_ExceptionPropagates()
        {
            var request = new SteamGuardCodeRequest("1", "2", "3", "code", AuthGuardType.EmailCode);

            _loginHandlerMock
                .Setup(x => x.EnterSteamGuardCodeAsync(It.IsAny<LoginResult>(), It.IsAny<string>(), It.IsAny<AuthGuardType>()))
                .ThrowsAsync(new NullReferenceException());

            await Assert.ThrowsAsync<NullReferenceException>(() => _sut.EnterSteamGuardCode(request));
        }

        #endregion

        #region SaveCookies

        [Fact]
        public async Task SaveCookies_DelegatesToBothServices_ReturnsNoContent()
        {
            var cookies = new CookieCollection();
            _loginHandlerMock.Setup(x => x.GetCookies()).Returns(cookies);
            _cookiePersistenceServiceMock.Setup(x => x.SaveCookiesAsync(cookies)).Returns(Task.CompletedTask);

            var result = await _sut.SaveCookies();

            Assert.IsType<NoContentResult>(result);
            _loginHandlerMock.Verify(x => x.GetCookies(), Times.Once);
            _cookiePersistenceServiceMock.Verify(x => x.SaveCookiesAsync(cookies), Times.Once);
        }

        [Fact]
        public async Task SaveCookies_WhenGetCookiesReturnsEmpty_SavesEmptyCollection()
        {
            var emptyCookies = new CookieCollection();
            _loginHandlerMock.Setup(x => x.GetCookies()).Returns(emptyCookies);
            _cookiePersistenceServiceMock.Setup(x => x.SaveCookiesAsync(emptyCookies)).Returns(Task.CompletedTask);

            var result = await _sut.SaveCookies();

            Assert.IsType<NoContentResult>(result);
            _cookiePersistenceServiceMock.Verify(x => x.SaveCookiesAsync(It.Is<CookieCollection>(c => c.Count == 0)), Times.Once);
        }

        #endregion

        #region LoadCookies

        [Fact]
        public async Task LoadCookies_DelegatesToBothServices_ReturnsNoContent()
        {
            var cookies = new CookieCollection();
            _cookiePersistenceServiceMock.Setup(x => x.LoadCookiesAsync()).ReturnsAsync(cookies);

            var result = await _sut.LoadCookies();

            Assert.IsType<NoContentResult>(result);
            _cookiePersistenceServiceMock.Verify(x => x.LoadCookiesAsync(), Times.Once);
            _loginHandlerMock.Verify(x => x.SetCookies(cookies), Times.Once);
        }

        [Fact]
        public async Task LoadCookies_WhenPersistenceServiceThrows_ExceptionPropagates()
        {
            _cookiePersistenceServiceMock
                .Setup(x => x.LoadCookiesAsync())
                .ThrowsAsync(new CryptographicException("Corrupted file"));

            await Assert.ThrowsAsync<CryptographicException>(() => _sut.LoadCookies());
        }

        #endregion
    }
}
