using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QuizApp.Api.Models;
using QuizApp.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace QuizApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto model)
        {
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

            if (!result.Succeeded)
            {
                return Unauthorized(new { Message = "Invalid login attempt" });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid login attempt" });
            }

            // Update last login time
            user.LastLoginTime = DateTime.Now;
            await _userManager.UpdateAsync(user);

            var token = await GenerateJwtToken(user);
            return Ok(token);
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true, // Auto-confirm for the API
                LastLoginTime = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            // Add user to the "User" role
            await _userManager.AddToRoleAsync(user, "User");

            // Generate the JWT token
            var token = await GenerateJwtToken(user);
            return Ok(token);
        }

        // POST: api/auth/refresh-token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(string refreshToken)
        {
            // In a real application, you would validate the refresh token
            // For now, this is just a placeholder
            return BadRequest(new { Message = "Refresh token functionality not implemented" });
        }

        private async Task<AuthResponseDto> GenerateJwtToken(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(1);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            // In a real application, you would generate a proper refresh token and store it
            var refreshToken = Guid.NewGuid().ToString();

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                ExpiresAt = expires,
                User = new UserDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email
                }
            };
        }
    }
}