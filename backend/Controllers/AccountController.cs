using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Sayartii.Api.Data;
using Sayartii.Api.Models;

namespace Sayartii.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> usermanger;
        private readonly IConfiguration config;

        public AccountController(ApplicationDbContext _db, UserManager<ApplicationUser> _usermanger, IConfiguration _config)
        {
            db = _db;
            usermanger = _usermanger;
            config = _config;
        }

        // Helper: retry a DB operation up to maxAttempts times on transient errors
        private static async Task<T> RetryAsync<T>(Func<Task<T>> action, int maxAttempts = 4)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    attempt++;
                    return await action();
                }
                catch (Exception ex) when (attempt < maxAttempts && IsTransient(ex))
                {
                    await Task.Delay(500 * attempt); // exponential backoff: 500ms, 1s, 1.5s
                }
            }
        }

        private static async Task RetryAsync(Func<Task> action, int maxAttempts = 4)
        {
            await RetryAsync<bool>(async () => { await action(); return true; }, maxAttempts);
        }

        private static bool IsTransient(Exception ex)
        {
            var msg = ex.Message + (ex.InnerException?.Message ?? "");
            return msg.Contains("reading from stream")
                || msg.Contains("transient failure")
                || msg.Contains("timeout")
                || msg.Contains("connection")
                || msg.Contains("broken pipe")
                || msg.Contains("EOF");
        }

        //Create Account new User "Registration" "Post"
        [HttpPost("register")]//api/account/register
        public async Task<IActionResult> Registration([FromBody] RegisterUserDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Retry transient DB errors
                var existingUser = await RetryAsync(() => usermanger.FindByEmailAsync(userDto.Email));
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "the Email is already taken");
                    return BadRequest(ModelState);
                }

                ApplicationUser user = new ApplicationUser();
                user.Name = userDto.Name;
                user.UserName = userDto.Email;
                user.Email = userDto.Email;
                user.RegisterDate = DateTime.UtcNow;

                IdentityResult result = await RetryAsync(() => usermanger.CreateAsync(user, userDto.Password));
                if (result.Succeeded)
                    return Ok("Account Add Success");

                // Check if failure is due to duplicate key (race condition / retry)
                bool isDuplicate = result.Errors.Any(e =>
                    e.Code == "DuplicateUserName" || e.Code == "DuplicateEmail");
                if (isDuplicate)
                {
                    ModelState.AddModelError("Email", "the Email is already taken");
                    return BadRequest(ModelState);
                }

                return BadRequest(result.Errors.FirstOrDefault());
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                when (dbEx.InnerException?.Message.Contains("23505") == true ||
                      dbEx.InnerException?.Message.Contains("duplicate key") == true)
            {
                ModelState.AddModelError("Email", "the Email is already taken");
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto userDto)
        {
            if (ModelState.IsValid == true)
            {
                try
                {
                    ApplicationUser user = await RetryAsync(() => usermanger.FindByNameAsync(userDto.Email));
                    if (user != null)
                    {
                        bool found = await RetryAsync(() => usermanger.CheckPasswordAsync(user, userDto.Password));
                        if (found)
                        {
                            DateTime expiresornot = userDto.RememberMe ? DateTime.UtcNow.AddMonths(10) : DateTime.UtcNow.AddMonths(1);

                            var claims = new List<Claim>();
                            claims.Add(new Claim(ClaimTypes.Name, user.Name));
                            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
                            claims.Add(new Claim(ClaimTypes.Email, user.Email!));
                            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

                            var roles = await RetryAsync(() => usermanger.GetRolesAsync(user));
                            foreach (var itemRole in roles)
                                claims.Add(new Claim(ClaimTypes.Role, itemRole));

                            string secretStr = config["JWT:Secret"] ?? "SuperSecretKeyForSayartiiAppWhichIsVeryLongAndSecureHere123!!";
                            SecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretStr));
                            SigningCredentials signincred = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                            JwtSecurityToken mytoken = new JwtSecurityToken(
                                issuer: config["JWT:ValidIssuer"],
                                audience: config["JWT:ValidAudience"],
                                claims: claims,
                                expires: expiresornot,
                                signingCredentials: signincred
                            );

                            return Ok(new
                            {
                                token = new JwtSecurityTokenHandler().WriteToken(mytoken),
                                expiration = mytoken.ValidTo
                            });
                        }
                    }
                    return Unauthorized();
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    return StatusCode(503, new { message = "Database temporarily unavailable, please retry." });
                }
            }
            return Unauthorized();
        }
    }
}
