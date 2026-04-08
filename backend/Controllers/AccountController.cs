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

        //Create Account new User "Registration" "Post"
        [HttpPost("register")]//api/account/register
        public async Task<IActionResult> Registration([FromBody] RegisterUserDto userDto)
        {
            var Users = db.Users.FirstOrDefault(a => a.Email == userDto.Email);

            if (Users == null)
            {
                if (ModelState.IsValid)
                {
                    //save
                    ApplicationUser user = new ApplicationUser();
                    user.Name = userDto.Name;
                    user.UserName = userDto.Email;
                    user.Email = userDto.Email;
                    
                    IdentityResult result = await usermanger.CreateAsync(user, userDto.Password);
                    if (result.Succeeded)
                    {
                        return Ok("Account Add Success");
                    }
                    return BadRequest(result.Errors.FirstOrDefault());
                }
                return BadRequest(ModelState);
            }
            else
            {
                ModelState.AddModelError("Email", "the Email is already taken");
                return BadRequest(ModelState);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto userDto)
        {
            if (ModelState.IsValid == true)
            {
                //check - create token
                ApplicationUser user = await usermanger.FindByNameAsync(userDto.Email);
                if (user != null)
                {
                    bool found = await usermanger.CheckPasswordAsync(user, userDto.Password);
                    if (found)
                    {
                        DateTime expiresornot = userDto.RememberMe ? DateTime.UtcNow.AddMonths(10) : DateTime.UtcNow.AddMonths(1);

                        //Claims Token
                        var claims = new List<Claim>();
                        claims.Add(new Claim(ClaimTypes.Name, user.Name));
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
                        claims.Add(new Claim(ClaimTypes.Email, user.Email));

                        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

                        //get role
                        var roles = await usermanger.GetRolesAsync(user);
                        foreach (var itemRole in roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, itemRole));
                        }
                        
                        // Fallback to avoid crash if Secret is too short or missing in appsettings.
                        string secretStr = config["JWT:Secret"] ?? "SuperSecretKeyForSayartiiAppWhichIsVeryLongAndSecureHere123!!";
                        SecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretStr));

                        SigningCredentials signincred = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                        //Create token
                        JwtSecurityToken mytoken = new JwtSecurityToken(
                            issuer: config["JWT:ValidIssuer"],
                            audience: config["JWT:ValidAudience"],//url consumer angular
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
            return Unauthorized();
        }
    }
}
