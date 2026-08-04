using FoodOrderBot.Application.Auth;
using FoodOrderBot.Application.Contracts;
using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FoodOrderBot.API.Controllers;

/// <summary>
/// Xác thực chủ quán qua Facebook OAuth — trả JWT có shopId claim dùng cho Dashboard và SignalR Hub.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IConfiguration config,
                                ILogger<AuthController> logger,
                                IFacebookAuthService facebookAuthService,
                                IShopRepository shopRepository) : ControllerBase
{

    private AuthResult GenerateJwtToken(Guid shopId, string fbPageId)
    {
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key missing");
        var issuer = config["Jwt:Issuer"] ?? "FoodOrderBot";
        var audience = config["Jwt:Audience"] ?? "FoodOrderBot";
        var expiryMinutes = config.GetValue<int>("Jwt:ExpiryMinutes", 1440);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "ShopOwner"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("shopId", shopId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, fbPageId),
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

    [HttpPost("facebook")]
    public async Task<IActionResult> FacebookLogin([FromBody] FacebookAuthRequest request, CancellationToken ct)
    {
        var longLived = await facebookAuthService.ExchangeForLongLivedTokenAsync(request.UserAccessToken, ct);
        var pages = await facebookAuthService.GetManagedPagesAsync(longLived, ct);
        // Map DTO (có token) → Response (không token) — đúng quyết định (a) của bạn
        var result = pages.Select(p => new FacebookPageResponse
        {
            PageId = p.PageId,
            PageName = p.PageName,
            PictureUrl = p.PictureUrl
        }).ToList();

        logger.LogInformation("User fetched {Count} pages from Facebook", result.Count);
        return Ok(result);
    }

    [HttpPost("facebook/select")]
    public async Task<IActionResult> SelectPage([FromBody] SelectPageRequest request, CancellationToken ct)
    {
        // 1. Tìm shop theo page — đây chính là lúc câu hỏi tracking entity lúc nãy được áp dụng
        var shop = await shopRepository.GetByFbPageIdAsync(request.PageId, ct);

        // 2. Upsert:
        if (shop is null)
        {
            // Page chưa từng đăng ký → tạo Shop MỚI
            shop = new Shop
            {
                Id = Guid.NewGuid(),
                Name = request.PageName,
                FbPageId = request.PageId,
                FbAccessToken = request.PageAccessToken,
                // FbOwnerUserId chưa có ở bước này MVP: chưa phân biệt chủ sở hữu
            };
            await shopRepository.AddAsync(shop, ct);
        }
        else
        {
            shop.FbAccessToken = request.PageAccessToken;
            await shopRepository.UpdateAsync(shop, ct);
        }

        await shopRepository.SaveChangesAsync(ct);

        logger.LogInformation("Shop {ShopId} selected via page {PageId}", shop.Id, request.PageId);

        return Ok(GenerateJwtToken(shop.Id, request.PageId));
    }
}
