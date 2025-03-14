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
            try
            {
                Console.WriteLine($"Login attempt for email: {model.Email} from IP: {HttpContext.Connection.RemoteIpAddress}");

                // Find user by email directly
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    Console.WriteLine("User not found");
                    return Unauthorized(new { Message = "Invalid email or password" });
                }

                Console.WriteLine("User found, checking password");
                // Verify password directly (more efficient for APIs)
                var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                if (!isPasswordValid)
                {
                    Console.WriteLine("Password invalid");
                    return Unauthorized(new { Message = "Invalid email or password" });
                }

                Console.WriteLine("Password valid, generating token");
                // Update last login time
                user.LastLoginTime = DateTime.Now;
                await _userManager.UpdateAsync(user);

                // Generate token
                var token = await GenerateJwtToken(user);
                Console.WriteLine("Login successful");
                return Ok(token);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Login error: {ex}");
                return StatusCode(500, new { Message = "An error occurred during login. Please try again." });
            }
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
            try
            {
                // Get user roles (potential database operation)
                var userRoles = await _userManager.GetRolesAsync(user);

                // Create minimal claims
                var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

                // Add roles as a single claim to reduce token size
                if (userRoles.Any())
                {
                    claims.Add(new Claim("roles", string.Join(",", userRoles)));
                }

                // Use cached key if possible
                var keyBytes = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ??
                    throw new InvalidOperationException("JWT Key is missing in configuration"));
                var key = new SymmetricSecurityKey(keyBytes);
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.UtcNow.AddDays(7); // Consider longer expiration

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );

                var tokenHandler = new JwtSecurityTokenHandler();
                return new AuthResponseDto
                {
                    Token = tokenHandler.WriteToken(token),
                    RefreshToken = Guid.NewGuid().ToString(),
                    ExpiresAt = expires,
                    User = new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName ?? user.Email ?? "User",
                        Email = user.Email
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token generation error: {ex}");
                throw;
            }
        }
    }
}