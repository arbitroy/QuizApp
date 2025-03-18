using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using QuizApp.Data;
using System;
using System.Threading.Tasks;

namespace QuizApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly IDbConnectionHealthCheck _dbCheck;

        public HealthController(ILogger<HealthController> logger, IDbConnectionHealthCheck dbCheck)
        {
            _logger = logger;
            _dbCheck = dbCheck;
        }

        // GET: api/health
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { Status = "API is running", Timestamp = DateTime.UtcNow });
        }

        // GET: api/health/database
        [HttpGet("database")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Database()
        {
            try
            {
                var result = await _dbCheck.CheckHealthAsync();
                return Ok(new
                {
                    Status = result.Status.ToString(),
                    Message = "Database connection is healthy",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return StatusCode(500, new
                {
                    Status = "Unhealthy",
                    Message = "Database connection failed",
                    Error = ex.Message
                });
            }
        }
    }

    // Interface for database health check
    public interface IDbConnectionHealthCheck
    {
        Task<HealthCheckResult> CheckHealthAsync();
    }

    // Implementation of the database health check
    public class DbConnectionHealthCheck : IDbConnectionHealthCheck
    {
        private readonly ApplicationDbContext _context;

        public DbConnectionHealthCheck(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync()
        {
            try
            {
                // Simple query to test database connection
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    return HealthCheckResult.Healthy("Database connection is healthy");
                }
                else
                {
                    return HealthCheckResult.Unhealthy("Unable to connect to database");
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database connection failed", ex);
            }
        }
    }
}