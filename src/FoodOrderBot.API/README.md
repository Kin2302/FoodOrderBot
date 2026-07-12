# FoodOrderBot.API

## Vai Trò
**Composition Root** — layer ngoài cùng. Biết về tất cả các layer khác.

Chứa: Controllers, SignalR Hub, Background Services, Middleware, và `Program.cs` (DI container).

---

## Quy Tắc Bắt Buộc
- ✅ Được `using` tất cả layers (Domain, Application, Infrastructure)
- Controllers chỉ được gọi Application services, KHÔNG gọi Infrastructure trực tiếp
- `WebhookController` chỉ được làm 2 việc: **validate chữ ký + enqueue**. Trả `200 OK` trong < 500ms
- Mọi xử lý nặng (AI, DB, SignalR) phải ở `WebhookProcessingWorker`

---

## Controllers

### `AuthController` — `POST /api/auth/login`
Nhận `LoginRequest` → kiểm tra credentials từ `Admin` config → trả JWT.

> Credentials hardcode trong `appsettings.json` cho MVP. Production: dùng env vars.

### `WebhookController` — `GET /api/webhook` + `POST /api/webhook`
- `GET` — Facebook verify token challenge
- `POST` — nhận event → validate HMAC-SHA256 signature → `WebhookTaskQueue.EnqueueAsync()`

### `OrderController` — `/api/orders/*`
| Method | Route | Auth | Mô tả |
|---|---|---|---|
| GET | `/api/orders` | ✅ | Lấy tất cả đơn |
| GET | `/api/orders/{id}` | ✅ | Chi tiết 1 đơn |
| PUT | `/api/orders/{id}` | ✅ | Sửa thông tin đơn |
| POST | `/api/orders/{id}/confirm` | ✅ | Draft → Confirmed |
| PUT | `/api/orders/{id}/status` | ✅ | Chuyển trạng thái |
| GET | `/api/orders/track/{token}` | ❌ public | Khách tracking đơn |

### `ShopController` — `/api/shop/menu`
CRUD menu items (soft delete — set `IsAvailable = false`).

### `AiController` — `POST /api/ai/test` (Sprint 6)
Test AI pipeline trực tiếp — hữu ích cho demo và debugging.

```json
// Request:
{ "text": "2 phở tái giao Q3", "shopId": "...", "fbSenderId": "test-user" }

// Response:
{
  "intent": "PlaceOrder",
  "parseResult": { "items": [...], "confidence": 0.85 },
  "replyText": "✅ Đơn hàng của bạn đã được ghi nhận!",
  "suggestions": [{ "itemName": "Trà đá", "price": 5000, "reason": "..." }],
  "sentiment": null,
  "confidence": 0.85
}
```

> `fbSenderId` tuỳ chọn — cùng sender = có conversation context. Dùng để test multi-turn.

---

## Hubs

### `OrderHub` — `/hubs/orders`
Yêu cầu `[Authorize]`. JWT qua query string `?access_token=...` được chấp nhận.

**Events do server push:**

| Event | Trigger | Payload |
|---|---|---|
| `NewOrderReceived` | Worker tạo Draft Order | `{ orderId, status, customerName, totalAmount, intent, upsellSuggestions }` |
| `OrderStatusUpdated` | `OrderController.UpdateStatus` | `{ orderId, newStatus }` |

---

## BackgroundServices

### `WebhookTaskQueue`
`Channel<WebhookTask>` in-memory. Capacity unbounded.

```csharp
public record WebhookTask(
    string FbMessageId, string FbSenderId, string Content,
    string Source, Guid ShopId, string? FbPostId, string? FbCommentId);
```

### `WebhookProcessingWorker`
Xử lý event theo 5 bước:
1. **Dedup** — check `FbMessageId` tồn tại → skip nếu đã có
2. **Upsert Customer** — tạo Customer nếu FbSenderId mới
3. **Save RawMessage** — lưu ngay trước khi gọi AI
4. **AI Orchestrator** — `IAiOrchestrator.ProcessMessageAsync()`
5. **Xử lý theo Intent:**
   - `PlaceOrder` + confidence ≥ 0.8 → Draft Order + SignalR push + Tracking link
   - `PlaceOrder` + confidence < 0.8 → Reply hỏi lại
   - Mọi intent khác → Reply text từ AI (greeting, menu, complaint, ...)

---

## Middleware

### `ExceptionHandlingMiddleware`
Bắt tất cả exception chưa được handle → trả `ProblemDetails` JSON.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred",
  "status": 500,
  "detail": "..."
}
```

---

## Program.cs — DI Registration Order

```csharp
// 1. Database (AppDbContext)
// 2. JWT Authentication
// 3. SignalR
// 4. Background Queue (WebhookTaskQueue + WebhookProcessingWorker)
// 5. CORS (AllowedOrigins từ config)
// 6. Controllers + OpenAPI (Scalar)
// 7. Repositories (IOrderRepository, IRawMessageRepository, ...)
// 8. Application Services (IOrderService, IMessageParser, IMessengerReply)
// 9. AI Services (AiKernelFactory singleton, plugins, IAiOrchestrator, IConversationRepository)
// 10. DbInitializer
```

---

## appsettings.json — Config Keys

```json
{
  "ConnectionStrings:DefaultConnection": "Host=...;Database=foodorderbot;...",
  "Jwt:Key": "...",
  "Jwt:Issuer": "FoodOrderBot",
  "Jwt:Audience": "FoodOrderBot",
  "Jwt:ExpiryMinutes": 1440,
  "Facebook:AppSecret": "...",
  "Facebook:VerifyToken": "...",
  "Facebook:PageAccessToken": "...",
  "Facebook:FrontendBaseUrl": "http://localhost:5173",
  "Groq:ApiKey": "gsk_...",
  "Groq:BaseUrl": "https://api.groq.com/openai/v1",
  "Groq:Models:IntentClassifier": "llama-3.1-8b-instant",
  "Groq:Models:OrderParser": "llama-3.3-70b-versatile",
  "Groq:Models:ChatBot": "llama-3.1-8b-instant",
  "Groq:Models:Upsell": "llama-3.1-8b-instant",
  "Groq:Models:Sentiment": "llama-3.1-8b-instant",
  "Admin:Email": "admin@foodorderbot.com",
  "Admin:Password": "Admin@123!",
  "Shop:DefaultShopId": "00000000-0000-0000-0000-000000000001",
  "AllowedOrigins": ["http://localhost:5173"]
}
```

> ⚠️ KHÔNG commit secrets thật. Dùng `appsettings.Development.json` hoặc Railway env vars.

---

## Chạy Development

```powershell
# Backend (port 5209)
dotnet run --project src/FoodOrderBot.API

# Frontend (port 5173, proxy → 5209)
cd frontend && npm run dev

# Test AI
curl -X POST http://localhost:5209/api/ai/test \
  -H "Authorization: Bearer <JWT>" \
  -H "Content-Type: application/json" \
  -d '{"text":"2 phở tái","shopId":"00000000-0000-0000-0000-000000000001"}'
```
