# FoodOrderBot.Domain

## Vai Trò
Layer trong cùng của Clean Architecture. **Không phụ thuộc vào bất kỳ layer nào khác.**

Chứa toàn bộ **business concepts thuần túy** — entities, enums, và repository interfaces.

---

## Quy Tắc Bắt Buộc
- ❌ KHÔNG được `using` bất cứ namespace nào từ Application, Infrastructure, hoặc API
- ❌ KHÔNG được reference EF Core, Npgsql, hay bất kỳ thư viện infrastructure nào
- ✅ Chỉ dùng thư viện .NET BCL thuần túy

---

## Entities

| File | Mô tả |
|---|---|
| `Shop.cs` | Quán ăn — có menu, orders, FbPageId, FbAccessToken |
| `Customer.cs` | Khách hàng — liên kết qua FbSenderId (Facebook PSID) |
| `MenuItem.cs` | Món ăn — thuộc 1 Shop, có IsAvailable để bật/tắt |
| `Order.cs` | Đơn hàng — `TrackingToken` (CSPRNG, UNIQUE), `Status` dạng string, `Note` (Sprint 7) |
| `OrderItem.cs` | Dòng đơn hàng — **snapshot** tên + giá tại thời điểm đặt, `Note` per-item |
| `RawMessage.cs` | Tin nhắn thô từ Messenger/Comment — `ParsedResult` (JSONB), `ParseConfidence` (float) |
| `ConversationMessage.cs` | Lịch sử hội thoại AI — dùng để AI nhớ context qua nhiều tin |

### Lưu ý quan trọng
- `Order.TrackingToken` — UNIQUE, không expose `OrderId` ra ngoài
- `Order.Note` — ghi chú chung đơn hàng (chủ quán sửa trước khi confirm) ✅ Sprint 7
- `OrderItem.ItemName` + `OrderItem.UnitPrice` — snapshot, không thay đổi dù menu thay
- `RawMessage.FbMessageId` — UNIQUE, dùng để **dedup** webhook events
- `RawMessage.ParseConfidence` — float? lưu ngay khi AI parse để truy xuất sau
- `ConversationMessage.Role` — `"User"` | `"Assistant"` | `"System"`

---

## Enums

| Enum | Values |
|---|---|
| `OrderStatus` | `Draft → Confirmed → Preparing → Completed / Cancelled` |
| `MessageSource` | `Messenger`, `Comment` |
| `PaymentStatus` | `Unpaid`, `Paid`, `Refunded` |

> `OrderStatus` lưu dạng **string** trong DB (không phải int) để dễ đọc khi debug.

---

## Repository Interfaces

Đây là **contracts** — Domain định nghĩa, Infrastructure implement.

| Interface | Phương thức chính |
|---|---|
| `IOrderRepository` | `GetAllAsync`, `GetByIdAsync`, `GetByTrackingTokenAsync`, `AddAsync`, `UpdateAsync` |
| `IRawMessageRepository` | `ExistsByFbMessageIdAsync` (dedup), `AddAsync`, `UpdateAsync` |
| `IMenuItemRepository` | `GetByShopIdAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` |
| `ICustomerRepository` | `GetByFbSenderIdAsync`, `AddAsync` |
| `IConversationRepository` | `GetRecentBySenderAsync(limit=5)`, `AddAsync` |
