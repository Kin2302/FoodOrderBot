using FoodOrderBot.Application.Contracts;
using FoodOrderBot.Application.Orders.Dtos;
using FoodOrderBot.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodOrderBot.API.Controllers;

/// <summary>
/// Quản lý đơn hàng — CRUD + tracking công khai.
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController(
    IOrderService orderService,
    IConfiguration config,
    ILogger<OrderController> logger) : ControllerBase
{
    private Guid DefaultShopId =>
        Guid.Parse(config["Shop:DefaultShopId"]
            ?? throw new InvalidOperationException("Shop:DefaultShopId chưa được cấu hình"));

    // ─── GET /api/orders?shopId={shopId} ─────────────────────────────────────

    /// <summary>Lấy danh sách đơn hàng của shop (mặc định dùng DefaultShopId).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? shopId,
        CancellationToken ct)
    {
        var id = shopId ?? DefaultShopId;
        var orders = await orderService.GetAllByShopAsync(id, ct);
        return Ok(orders);
    }

    // ─── GET /api/orders/{id} ─────────────────────────────────────────────────

    /// <summary>Lấy chi tiết một đơn hàng theo Id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            // Tái dùng GetAllByShop rồi filter — tránh expose method GetById riêng
            // Hoặc gọi trực tiếp qua tracking token trick, nhưng ở đây ta cần GetById
            // Thêm GetByIdAsync vào IOrderService nếu cần chi tiết hơn
            var orders = await orderService.GetAllByShopAsync(DefaultShopId, ct);
            var order = orders.FirstOrDefault(o => o.Id == id);
            if (order is null) return NotFound(new { message = $"Không tìm thấy đơn hàng {id}" });
            return Ok(order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetById order {OrderId} failed", id);
            return StatusCode(500, new { message = "Lỗi server" });
        }
    }

    // ─── POST /api/orders/{id}/confirm ──────────────────────────────────────

    /// <summary>Xác nhận đơn hàng (Draft → Confirmed).</summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        try
        {
            var order = await orderService.UpdateStatusAsync(id, OrderStatus.Confirmed, ct);
            return Ok(order);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Đơn hàng {id} không tồn tại" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─── PATCH /api/orders/{id}/status ───────────────────────────────────────

    /// <summary>Cập nhật trạng thái đơn hàng (Confirmed / Preparing / Completed / Cancelled).</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var newStatus))
            return BadRequest(new { message = $"Trạng thái không hợp lệ: {request.Status}" });

        try
        {
            var order = await orderService.UpdateStatusAsync(id, newStatus, ct);
            return Ok(order);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Không tìm thấy đơn hàng {id}" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─── PATCH /api/orders/{id} ───────────────────────────────────────────────

    /// <summary>Cập nhật thông tin giao hàng và items (chỉ ở trạng thái Draft).</summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateOrder(
        Guid id,
        [FromBody] UpdateOrderRequest request,
        CancellationToken ct)
    {
        try
        {
            var order = await orderService.UpdateOrderAsync(id, request, ct);
            return Ok(order);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Không tìm thấy đơn hàng {id}" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─── DELETE /api/orders/{id} ──────────────────────────────────────────────

    /// <summary>Huỷ đơn hàng.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        try
        {
            var order = await orderService.UpdateStatusAsync(id, OrderStatus.Cancelled, ct);
            return Ok(order);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Không tìm thấy đơn hàng {id}" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─── GET /api/orders/track/{token} ───────────────────────────────────────

    /// <summary>Tracking công khai — không cần JWT, khách dùng link gửi qua Messenger.</summary>
    [HttpGet("track/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> Track(string token, CancellationToken ct)
    {
        var order = await orderService.GetByTrackingTokenAsync(token, ct);
        if (order is null)
            return NotFound(new { message = "Link tracking không hợp lệ hoặc đã hết hạn." });

        // Chỉ trả về fields cần thiết cho trang tracking (ẩn thông tin nhạy cảm)
        return Ok(new
        {
            order.Id,
            order.Status,
            order.PaymentStatus,
            order.TotalAmount,
            order.Note,
            order.ReceiverName,
            order.CreatedAt,
            order.UpdatedAt,
            Items = order.Items.Select(i => new
            {
                i.Id,
                i.MenuItemId,
                i.ItemName,
                i.UnitPrice,
                i.Quantity,
                i.Note,
                Subtotal = i.UnitPrice * i.Quantity
            })
        });
    }
}

// ─── Request DTOs ─────────────────────────────────────────────────────────────

public record UpdateStatusRequest(string Status);

