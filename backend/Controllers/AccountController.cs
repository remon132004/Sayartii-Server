using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        //Create Account new User "Registration" "Post"
        [HttpPost("register")]//api/account/register
        public async Task<IActionResult> Registration([FromBody] RegisterUserDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Query directly via EF (no UserManager) to avoid Identity's internal multi-query pattern
                var existingUser = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == userDto.Email.ToUpper());

                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "the Email is already taken");
                    return BadRequest(ModelState);
                }

                ApplicationUser user = new ApplicationUser
                {
                    Name = userDto.Name,
                    CarName = userDto.CarName,
                    UserName = userDto.Email,
                    NormalizedUserName = userDto.Email.ToUpper(),
                    Email = userDto.Email,
                    NormalizedEmail = userDto.Email.ToUpper(),
                    RegisterDate = DateTime.UtcNow,
                    EmailConfirmed = false,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    LockoutEnabled = true,
                    AccessFailedCount = 0,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                };

                // Hash password manually then save directly - bypasses all Identity multi-queries
                var passwordHasher = new PasswordHasher<ApplicationUser>();
                user.PasswordHash = passwordHasher.HashPassword(user, userDto.Password);

                db.Users.Add(user);
                await db.SaveChangesAsync();

                return Ok("Account Add Success");
            }
            catch (DbUpdateException dbEx)
                when (dbEx.InnerException?.Message.Contains("23505") == true ||
                      dbEx.InnerException?.Message.Contains("duplicate key") == true ||
                      dbEx.InnerException?.Message.Contains("unique") == true)
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
            if (!ModelState.IsValid)
                return Unauthorized();

            try
            {
                // Direct EF query instead of UserManager.FindByNameAsync (avoids multi-query chain)
                var user = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == userDto.Email.ToUpper());

                if (user == null)
                    return Unauthorized();

                // Verify password directly
                var passwordHasher = new PasswordHasher<ApplicationUser>();
                var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, userDto.Password);

                if (result == PasswordVerificationResult.Failed)
                    return Unauthorized();

                DateTime expiresornot = userDto.RememberMe ? DateTime.UtcNow.AddMonths(10) : DateTime.UtcNow.AddMonths(1);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

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
                    expiration = mytoken.ValidTo,
                    name = user.Name,
                    carName = user.CarName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
