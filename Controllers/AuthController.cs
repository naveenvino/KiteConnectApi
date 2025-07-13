using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace KiteConnectApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            _logger.LogInformation("Login attempt for user: {Username}", model.Username);

            // For simplicity, hardcoding credentials. In a real app, validate against a database.
            if (model.Username == "admin" && model.Password == "password")
            {
                try
                {
                    var claims = new[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub, model.Username),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                    };

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:ExpireDays"] ?? "7"));

                    var token = new JwtSecurityToken(
                        issuer: _configuration["Jwt:Issuer"],
                        audience: _configuration["Jwt:Audience"],
                        claims: claims,
                        expires: expires,
                        signingCredentials: creds
                    );

                    _logger.LogInformation("Login successful for user: {Username}", model.Username);
                    return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating JWT token for user: {Username}", model.Username);
                    return StatusCode(500, "Internal server error");
                }
            }

            _logger.LogWarning("Login failed for user: {Username}. Invalid credentials.", model.Username);
            return Unauthorized();
        }
    }

    public class LoginModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}