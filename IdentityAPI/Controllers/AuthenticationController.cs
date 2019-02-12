using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using IdentityAPI.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace IdentityAPI.Controllers
{
    // authentication
    [Route("Authenticate")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService AuthenticationService;
        private readonly IConfiguration Configuration;

        public AuthenticationController(IAuthenticationService AuthenticationService, IConfiguration Configuration)
        {
            this.AuthenticationService = AuthenticationService;
            this.Configuration = Configuration;
        }

        [Authorize]
        [HttpPost("CreateAccount")]
        public IActionResult CreateAccount([FromBody]CreateAccountMessage message)
        {
            Console.WriteLine("Generating Acccount...");
            if (message == null)
            {
                Console.WriteLine("MESSAGE IS EMPTY");
                return StatusCode(StatusCodes.Status204NoContent);
            }
            else
            {
                if (AuthenticationService.CreateNewAccount(message.Account, message.Otp))
                {
                    // TODO : Return Token
                    return Ok("success");
                }
                else
                {
                    return Unauthorized();
                }
            }
        }

        [HttpPost("Login")]
        public IActionResult VerifyOtp([FromBody]VerifyOtpMessage message)
        {
            Console.WriteLine("Verifying OTP...");
            var account = AuthenticationService.ConfirmUserOtp(message.Email, message.Otp);

            if (account != null)
            {
                // Generate Claims
                var claims = new[]
                {
                    new Claim(ClaimTypes.Email, message.Email),
                    new Claim(ClaimTypes.AuthenticationMethod, "TOTP")
                };

                // Generate Security Token
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["TokenKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: "roman015.com",
                    audience: "roman015.com",
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(30),
                    signingCredentials: creds);


                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token)
                });
            }
            else
            {
                return Unauthorized();
            }
        }

        [Authorize]
        [HttpGet("TestToken")]
        public IActionResult TestToken()
        {
            Console.WriteLine("Calling TestToken");
            return Ok(new
            {
                result = "token valid"
            });
        }
    }

    // Expected Format of incoming messages
    public class CreateAccountMessage
    {
        public Account Account { get; set; }
        public string Otp { get; set; }
    }

    public class VerifyOtpMessage
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }
}
