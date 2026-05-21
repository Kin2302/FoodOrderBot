using FoodOrderBot.Domain.Enums;

namespace FoodOrderBot.Application.Orders;

/// <summary>
/// Kiểm soát vòng đời đơn hàng — chỉ cho phép chuyển theo đúng luồng hợp lệ.
/// Draft → Confirmed → Preparing → Completed / Cancelled
/// </summary>
public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> _validTransitions = new()
    {
        [OrderStatus.Draft]     = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Preparing, OrderStatus.Cancelled],
        [OrderStatus.Preparing] = [OrderStatus.Completed, OrderStatus.Cancelled],
        [OrderStatus.Completed] = [],  // Terminal state
        [OrderStatus.Cancelled] = [],  // Terminal state
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to)
        => _validTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static void ThrowIfInvalidTransition(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException(
                $"Không thể chuyển đơn hàng từ '{from}' sang '{to}'. " +
                $"Các trạng thái hợp lệ: [{string.Join(", ", _validTransitions[from])}]");
    }
}
