using FoodOrderBot.Domain.Entities;
using FoodOrderBot.Domain.Interfaces;
using FoodOrderBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderBot.API.Controllers;

/// <summary>
/// Quản lý thực đơn (Menu) của shop.
/// ShopId lấy từ config Shop:DefaultShopId cho MVP.
/// </summary>
[ApiController]
[Route("api/shop")]
[Authorize]
public class ShopController(
    IMenuItemRepository menuRepo,
    IConfiguration config,
    ILogger<ShopController> logger) : ControllerBase
{
    private Guid DefaultShopId =>
        Guid.Parse(config["Shop:DefaultShopId"]
            ?? throw new InvalidOperationException("Shop:DefaultShopId chưa được cấu hình"));

    // ─── GET /api/shop/menu ───────────────────────────────────────────────────

    /// <summary>Lấy toàn bộ thực đơn của shop.</summary>
    [HttpGet("menu")]
    public async Task<IActionResult> GetMenu(CancellationToken ct)
    {
        var items = await menuRepo.GetByShopIdAsync(DefaultShopId, ct);
        return Ok(items.Select(i => new MenuItemDto(
            i.Id, i.Name, i.Price, i.Description, i.IsAvailable, i.DisplayOrder)));
    }

    // ─── POST /api/shop/menu ──────────────────────────────────────────────────

    /// <summary>Thêm món mới vào thực đơn.</summary>
    [HttpPost("menu")]
    public async Task<IActionResult> AddMenuItem([FromBody] UpsertMenuItemRequest request, CancellationToken ct)
    {
        var item = new MenuItem
        {
            Id           = Guid.NewGuid(),
            ShopId       = DefaultShopId,
            Name         = request.Name,
            Price        = request.Price,
            Description  = request.Description ?? string.Empty,
            IsAvailable  = request.IsAvailable ?? true,
            DisplayOrder = request.DisplayOrder ?? 0,
            CreatedAt    = DateTime.UtcNow
        };

        await menuRepo.AddAsync(item, ct);
        await menuRepo.SaveChangesAsync(ct);

        logger.LogInformation("MenuItem {Name} added to shop {ShopId}", item.Name, DefaultShopId);

        return CreatedAtAction(nameof(GetMenu), new MenuItemDto(
            item.Id, item.Name, item.Price, item.Description, item.IsAvailable, item.DisplayOrder));
    }

    // ─── PUT /api/shop/menu/{id} ──────────────────────────────────────────────

    /// <summary>Cập nhật thông tin món ăn.</summary>
    [HttpPut("menu/{id:guid}")]
    public async Task<IActionResult> UpdateMenuItem(Guid id, [FromBody] UpsertMenuItemRequest request, CancellationToken ct)
    {
        var item = await menuRepo.GetByIdAsync(id, ct);
        if (item is null) return NotFound(new { message = $"Không tìm thấy món ăn {id}" });

        item.Name         = request.Name;
        item.Price        = request.Price;
        item.Description  = request.Description ?? item.Description;
        item.IsAvailable  = request.IsAvailable ?? item.IsAvailable;
        item.DisplayOrder = request.DisplayOrder ?? item.DisplayOrder;

        await menuRepo.UpdateAsync(item, ct);
        await menuRepo.SaveChangesAsync(ct);

        logger.LogInformation("MenuItem {Id} updated", id);
        return Ok(new MenuItemDto(item.Id, item.Name, item.Price, item.Description, item.IsAvailable, item.DisplayOrder));
    }

    // ─── DELETE /api/shop/menu/{id} ───────────────────────────────────────────

    /// <summary>Ẩn món khỏi thực đơn (IsAvailable = false, không xóa vật lý).</summary>
    [HttpDelete("menu/{id:guid}")]
    public async Task<IActionResult> HideMenuItem(Guid id, CancellationToken ct)
    {
        var item = await menuRepo.GetByIdAsync(id, ct);
        if (item is null) return NotFound(new { message = $"Không tìm thấy món ăn {id}" });

        item.IsAvailable = false;

        await menuRepo.UpdateAsync(item, ct);
        await menuRepo.SaveChangesAsync(ct);

        logger.LogInformation("MenuItem {Id} hidden", id);
        return NoContent();
    }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public record MenuItemDto(
    Guid Id,
    string Name,
    decimal Price,
    string Description,
    bool IsAvailable,
    int DisplayOrder);

public record UpsertMenuItemRequest(
    string Name,
    decimal Price,
    string? Description,
    bool? IsAvailable,
    int? DisplayOrder);
