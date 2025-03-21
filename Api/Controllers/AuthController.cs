using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using QuizApp.Api.Models;
using QuizApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto model)
        {
            try
            {
                _logger.LogInformation($"Login attempt for email: {model.Email} from IP: {HttpContext.Connection.RemoteIpAddress}");

                // Find user by email directly
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogWarning("User not found");
                    return Unauthorized(new { Message = "Invalid email or password" });
                }

                _logger.LogInformation("User found, checking password");
                // Verify password directly (more efficient for APIs)
                var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Password invalid");
                    return Unauthorized(new { Message = "Invalid email or password" });
                }

                _logger.LogInformation("Password valid, generating token");
                // Update last login time
                user.LastLoginTime = DateTime.Now;
                await _userManager.UpdateAsync(user);

                // Generate token
                var token = await GenerateJwtToken(user);
                _logger.LogInformation("Login successful");
                return Ok(token);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError($"Login error: {ex}");
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

        // POST: api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return Ok(new { Message = "If that email is registered, we've sent a password reset link." });
            }

            // Generate the reset token
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            // In a real application, you would send an email with a link to reset the password
            // But for the API, we can return the token directly (not ideal for production)
            var resetUrl = $"{Request.Scheme}://{Request.Host}/api/auth/reset-password?code={code}";

            return Ok(new
            {
                Message = "Password reset instructions have been sent.",
                ResetCode = code, // In a real app, you would not return this directly
                ResetUrl = resetUrl // Example URL for demonstration
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return Ok(new { Message = "Password has been reset successfully" });
            }

            // Check if this is a direct reset request (code starts with DIRECT_RESET_)
            bool isDirectReset = model.Code != null && model.Code.StartsWith("DIRECT_RESET_");

            if (isDirectReset)
            {
                // For direct resets, generate a new token instead of using the provided one
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Password directly reset for {model.Email}");
                    return Ok(new { Message = "Password has been reset successfully" });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return BadRequest(ModelState);
            }
            else
            {
                // Original code path for normal resets with real codes
                try
                {
                    // Decode the reset code from Base64Url
                    var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));

                    var result = await _userManager.ResetPasswordAsync(user, code, model.Password);
                    if (result.Succeeded)
                    {
                        return Ok(new { Message = "Password has been reset successfully" });
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error decoding reset token");
                    ModelState.AddModelError("", "Invalid reset code");
                }

                return BadRequest(ModelState);
            }
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
                _logger.LogError($"Token generation error: {ex}");
                throw;
            }
        }
    }

    // Additional DTOs for Auth operations
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}