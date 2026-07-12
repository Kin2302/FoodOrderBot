# FoodOrderBot.Application

## Vai Trò
Layer thứ hai trong Clean Architecture. **Phụ thuộc Domain, không biết Infrastructure.**

Chứa **business logic**, **service interfaces (contracts)**, **DTOs**, và **AI type definitions**.

---

## Quy Tắc Bắt Buộc
- ✅ Được `using` Domain
- ❌ KHÔNG được `using` Infrastructure, API
- ❌ KHÔNG được reference EF Core, Groq SDK, Facebook API trực tiếp
- Service interfaces nằm ở `Contracts/`, KHÔNG phải `Domain/Interfaces/`

---

## Cấu Trúc

### `Contracts/` — Service Interfaces

| Interface | Implement bởi | Mô tả |
|---|---|---|
| `IOrderService` | `OrderService` (Application) | CRUD đơn hàng + state machine |
| `IMessageParser` | `MessageParserService` (Infrastructure) | Parse tin nhắn → ParseResultDto (thin wrapper) |
| `IMessengerReply` | `MessengerClient` (Infrastructure) | Gửi tin Messenger |
| `IAiOrchestrator` | `AiOrchestrator` (Infrastructure) | **Entry point chính AI** — classify intent → route → trả AiResponse |

### `AI/` — AI Types (Sprint 6)

**`AiIntent.cs`** — Enum phân loại ý định:
```
PlaceOrder | AskMenu | AskOrderStatus | Greeting | Complaint | Compliment | Other
```

**`AiDtos.cs`** — Records:
```csharp
AiRequest(FbSenderId, ShopId, Content, Source)
AiResponse { Intent, ParseResult?, ReplyText, Suggestions, Sentiment?, Confidence }
UpsellSuggestion(ItemName, Price, Reason)
SentimentResult(Label, Score, NeedsAttention)
```

### `Orders/`

- `OrderService.cs` — Implement `IOrderService`:
  - `CreateDraftAsync` — tạo đơn Draft, generate `TrackingToken` URL-safe
  - `ConfirmAsync` — Draft → Confirmed (qua StateMachine)
  - `UpdateStatusAsync` — chuyển trạng thái (có validate)
  - `UpdateOrderAsync` — sửa thông tin đơn (bao gồm `Note` — Sprint 7)
  - `GetAllAsync` — lấy tất cả đơn của Shop
  - `GetByTrackingTokenAsync` — public endpoint không cần auth
  - `MapToDtoFromDb` — ✅ Sprint 7: đọc `ParseConfidence` + `UnclearParts` từ `RawMessage.ParsedResult` JSONB
  - `ExtractUnclearParts` — ✅ Sprint 7: deserialize JSON để lấy mảng UnclearParts
- `OrderStateMachine.cs` — Validate transition hợp lệ:
  ```
  Draft → Confirmed | Cancelled
  Confirmed → Preparing | Cancelled
  Preparing → Completed | Cancelled
  Completed → (terminal)
  Cancelled → (terminal)
  ```
- `Dtos/OrderDto.cs` — ✅ Sprint 7 bổ sung fields:
  ```csharp
  float? ParseConfidence     // độ tin cậy AI (0.0–1.0)
  List<string> UnclearParts  // phần AI chưa chắc
  string? Note               // ghi chú chung đơn
  string RawMessageContent   // tin gốc của khách
  string ReceiverName/Phone/DeliveryAddress  // thông tin giao hàng từ AI parse
  ```
- `Dtos/UpdateOrderRequest.cs` — ✅ Sprint 7: thêm field `Note`

### `Parsing/`

- `ParseResultDto.cs` — DTO ánh xạ JSON từ AI:
  ```csharp
  Items (List<ParsedOrderItem>), ReceiverName?, ReceiverPhone?,
  DeliveryAddress?, Confidence (0.0-1.0), UnclearParts
  ```

### `Auth/`

- `AuthDtos.cs` — `LoginRequest`, `AuthResult` (token, expiresAt, email)

---

## Flow Sử Dụng

```
Worker nhận tin nhắn
  → orchestrator.ProcessMessageAsync(AiRequest)  ← gọi IAiOrchestrator
      → classify intent
      → route đến AI plugin phù hợp
      → trả AiResponse { Intent, ParseResult?, ReplyText, ... }
  → Worker xử lý theo Intent:
      PlaceOrder + conf≥0.8 → orderService.CreateDraftAsync()
      PlaceOrder + conf<0.8 → reply hỏi lại
      Mọi intent khác      → reply text từ AI
```
