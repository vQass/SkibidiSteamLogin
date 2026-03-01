using Microsoft.AspNetCore.Mvc;
using SkibidiSteamLogin.Core.Enums;
using SkibidiSteamLogin.Core.Interfaces;
using SkibidiSteamLogin.Core.Models.Externals;

namespace SkibidiSteamLogin.WebApi.Controllers
{
    public record LoginRequest(string Username, string Password);

    public record SteamGuardCodeRequest(
        string ClientId, string SteamId, string RequestId,
        string AuthCode, AuthGuardType GuardType);

    [Route("api/[controller]")]
    [ApiController]
    public class LoginHandlerController : ControllerBase
    {
        private readonly ILoginHandler _loginHandler;
        private readonly ICookiePersistenceService _cookiePersistenceService;

        public LoginHandlerController(ILoginHandler loginHandler, ICookiePersistenceService cookiePersistenceService)
        {
            _loginHandler = loginHandler;
            _cookiePersistenceService = cookiePersistenceService;
        }

        [HttpPost("Login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult<LoginResult>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required.");

            var result = await _loginHandler.LoginAsync(request.Username, request.Password);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("EnterSteamGuardCode")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OperationResult<LoginResult>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EnterSteamGuardCode([FromBody] SteamGuardCodeRequest request)
        {
            var loginSession = new LoginResult
            {
                ClientId = request.ClientId,
                SteamId = request.SteamId,
                RequestId = request.RequestId
            };

            var result = await _loginHandler.EnterSteamGuardCodeAsync(loginSession, request.AuthCode, request.GuardType);

            if (!result.IsSuccess)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("SaveCookies")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SaveCookies()
        {
            var cookies = _loginHandler.GetCookies();
            await _cookiePersistenceService.SaveCookiesAsync(cookies);
            return NoContent();
        }

        [HttpPost("LoadCookies")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> LoadCookies()
        {
            var cookies = await _cookiePersistenceService.LoadCookiesAsync();
            _loginHandler.SetCookies(cookies);
            return NoContent();
        }
    }
}
