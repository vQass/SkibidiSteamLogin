using Microsoft.AspNetCore.Mvc;
using SkibidiSteamLogin.Core.Enums;
using SkibidiSteamLogin.Core.Interfaces;
using SkibidiSteamLogin.Core.Models.Externals;

namespace SkibidiSteamLogin.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginHandlerController : ControllerBase
    {
        private readonly ILoginHandler _loginHandler;

        public LoginHandlerController(ILoginHandler loginHandler)
        {
            _loginHandler = loginHandler;
        }

        [HttpGet("Login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResult))]
        public async Task<IActionResult> Login(string username, string password)
        {
            var result = await _loginHandler.LoginAsync(username, password);

            return Ok(result);
        }

        [HttpGet("EnterSteamGuardCode")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResult))]
        public async Task<IActionResult> EnterSteamGuardCode(string authcode, AuthGuardTypeEnum guardTypeEnum)
        {
            var result = await _loginHandler.EnterSteamGuardCodeAsync(authcode, guardTypeEnum);

            return Ok(result);
        }

        [HttpPost("SaveCookies")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResult))]
        public IActionResult SaveCookies(string authcode, AuthGuardTypeEnum guardTypeEnum)
        {
            _loginHandler.SaveCookies();

            return Ok();
        }

        [HttpPost("LoadCookies")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResult))]
        public IActionResult LoadCookies(string authcode, AuthGuardTypeEnum guardTypeEnum)
        {
           _loginHandler.LoadCookies();

            return Ok();
        }
    }
}
