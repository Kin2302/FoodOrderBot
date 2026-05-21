using FoodOrderBot.Application.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FoodOrderBot.API.Controllers;

/// <summary>
/// Xác thực chủ quán — trả JWT token dùng để truy cập Dashboard và SignalR Hub.
/// MVP: hardcode credentials trong appsettings (không cần Identity DB).
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IConfiguration config, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // MVP: đọc credentials từ appsettings — không cần DB
        var adminEmail = config["Admin:Email"] ?? "admin@foodorderbot.com";
        var adminPassword = config["Admin:Password"] ?? "Admin@123";

        if (request.Email != adminEmail || request.Password != adminPassword)
        {
            logger.LogWarning("Login failed for email: {Email}", request.Email);
            return Unauthorized(AuthResult.Fail("Email hoặc mật khẩu không đúng."));
        }

        var token = GenerateJwtToken(request.Email);
        logger.LogInformation("Login successful for: {Email}", request.Email);
        return Ok(token);
    }

    private AuthResult GenerateJwtToken(string email)
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key missing");
        var issuer = config["Jwt:Issuer"] ?? "FoodOrderBot";
        var audience = config["Jwt:Audience"] ?? "FoodOrderBot";
        var expiryMinutes = config.GetValue<int>("Jwt:ExpiryMinutes", 1440);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "ShopOwner"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return AuthResult.Ok(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
