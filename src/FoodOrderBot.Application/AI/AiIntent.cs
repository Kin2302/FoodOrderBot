namespace FoodOrderBot.Application.AI;

/// <summary>
/// Phân loại ý định của khách hàng trong tin nhắn.
/// </summary>
public enum AiIntent
{
    /// <summary>Đặt hàng: "cho tui 2 phở tái", "order 1 bún bò"</summary>
    PlaceOrder,

    /// <summary>Hỏi menu: "có món gì ngon?", "rẻ nhất bao nhiêu?"</summary>
    AskMenu,

    /// <summary>Hỏi trạng thái đơn: "đơn tui tới đâu rồi?", "bao giờ giao?"</summary>
    AskOrderStatus,

    /// <summary>Chào hỏi: "hello", "chào shop", "shop ơi"</summary>
    Greeting,

    /// <summary>Khiếu nại: "sao giao chậm vậy", "đồ ăn nguội rồi"</summary>
    Complaint,

    /// <summary>Khen ngợi: "ngon lắm", "cảm ơn shop", "tuyệt vời"</summary>
    Compliment,

    /// <summary>Không rõ ý định hoặc không liên quan</summary>
    Other
}
