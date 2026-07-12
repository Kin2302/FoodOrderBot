# FoodOrderBot.Infrastructure

## Vai Trò
Layer implement các interfaces từ Domain và Application. **Biết về tất cả external dependencies.**

Đây là nơi duy nhất được phép dùng: EF Core, Npgsql, Groq API, Semantic Kernel, Facebook HTTP Client.

---

## Quy Tắc Bắt Buộc
- ✅ Được `using` Domain + Application
- ❌ KHÔNG được `using` API project
- Tất cả external services đều phải đăng ký qua DI trong `API/Program.cs`

---

## Cấu Trúc

### `AI/` — AI Engine (Sprint 6)

#### `AiKernelFactory.cs`
Singleton — tạo và **cache** `Kernel` per model (không tạo mới mỗi request).

```csharp
// Cách dùng:
var kernel = kernelFactory.GetKernel("OrderParser");  // → Kernel cho 70b model
var kernel = kernelFactory.GetKernel("IntentClassifier"); // → Kernel cho 8b model
```

Model IDs lấy từ `appsettings.json`:
```json
"Groq:Models:IntentClassifier" = "llama-3.1-8b-instant"
"Groq:Models:OrderParser"      = "llama-3.3-70b-versatile"
"Groq:Models:ChatBot"          = "llama-3.1-8b-instant"
"Groq:Models:Upsell"           = "llama-3.1-8b-instant"
"Groq:Models:Sentiment"        = "llama-3.1-8b-instant"
```

#### `AiOrchestrator.cs`
Implement `IAiOrchestrator`. Luồng xử lý:
1. Load 5 tin gần nhất (`ConversationRepository`)
2. Classify intent (`IntentClassifierPlugin`)
3. Route theo intent → gọi plugin phù hợp
4. Save conversation (user + assistant) vào DB
5. Return `AiResponse`

> ⚠️ **Complaint** xử lý song song: `SentimentPlugin` + `ChatBotPlugin` chạy cùng lúc (`Task.WhenAll`)

#### `Plugins/`

| Plugin | Model | Input | Output |
|---|---|---|---|
| `IntentClassifierPlugin` | 8b | tin nhắn + history | `AiIntent` enum |
| `OrderParserPlugin` | 70b | tin nhắn + menu + history | `ParseResultDto` |
| `ChatBotPlugin` | 8b | intent + tin nhắn + menu | reply text |
| `UpsellPlugin` | 8b | parsed order + menu | `List<UpsellSuggestion>` |
| `SentimentPlugin` | 8b | tin nhắn | `SentimentResult` |

> ⚠️ `UpsellPlugin` và `SentimentPlugin` **không throw exception** — lỗi được swallow và trả về giá trị mặc định để không làm hỏng flow chính.

#### `Prompts/`
Prompt templates — load từ file `.txt` lúc runtime (copy to output directory).

| File | Placeholder chính |
|---|---|
| `intent_classifier.txt` | `{MESSAGE}`, `{CONVERSATION_HISTORY}` |
| `order_parser.txt` | `{MESSAGE}`, `{MENU_JSON}`, `{CONVERSATION_HISTORY}` |
| `chatbot_reply.txt` | `{INTENT}`, `{MESSAGE}`, `{SHOP_INFO}`, `{TRACKING_INFO}` |
| `upsell_suggest.txt` | `{ORDER_ITEMS}`, `{MENU_JSON}` |
| `sentiment_analyzer.txt` | `{MESSAGE}` |

---

### `Persistence/`

#### `AppDbContext.cs`
7 DbSets: Shops, Customers, MenuItems, RawMessages, Orders, OrderItems, **ConversationMessages**.

Dùng `ApplyConfigurationsFromAssembly` — tự động pick up tất cả `IEntityTypeConfiguration<T>` trong assembly.

#### `Configurations/`
Fluent API config cho từng entity:

| File | Entity | Điểm đáng chú ý |
|---|---|---|
| `OrderConfiguration.cs` | Order | TrackingToken UNIQUE, Status là `text` |
| `RawMessageConfiguration.cs` | RawMessage | FbMessageId UNIQUE, ParsedResult là `jsonb` |
| `OtherConfigurations.cs` | MenuItem, Customer, Shop, OrderItem | FbSenderId UNIQUE (Customer) |
| `ConversationMessageConfiguration.cs` | ConversationMessage | Index trên (FbSenderId, CreatedAt) |

#### `DbInitializer.cs`
Chạy khi startup: `InitialiseAsync()` (migrate) + `SeedAsync()` (seed Shop + 10 món nếu chưa có).

#### `Repositories/`
Implement các interface từ Domain. Dùng **primary constructor** cho DI.

```csharp
// Pattern chuẩn:
public class OrderRepository(AppDbContext db) : IOrderRepository { ... }
```

**✅ Sprint 7 — `OrderRepository`:**
- `GetByShopIdAsync`: thêm `.Include(o => o.RawMessage)` để `ParseConfidence` + `ParsedResult` có thể được map trong `OrderService`

---

### `SemanticKernel/`

#### `MessageParserService.cs`
**Thin wrapper** — delegate sang `OrderParserPlugin` (không có conversation context).
Giữ backward compatibility với `IMessageParser`.

```csharp
// Nếu cần parse không qua Orchestrator (vẫn hoạt động):
await parser.ParseAsync(text, shopId, ct);
// = OrderParserPlugin.ParseAsync(text, shopId, history: [], ct)
```

---

### `Facebook/`

#### `MessengerClient.cs`
Implement `IMessengerReply`. Gọi Facebook Send API v21.0.

```csharp
SendTextAsync(fbSenderId, message, pageAccessToken, ct)
SendTrackingLinkAsync(fbSenderId, orderId, trackingToken, pageAccessToken, ct)
```

> Tracking link format: `{FrontendBaseUrl}/track/{trackingToken}`

---

## EF Migrations

```powershell
# Tạo migration (PowerShell — chạy từ thư mục gốc)
dotnet ef migrations add <TenMigration> `
  --project src/FoodOrderBot.Infrastructure `
  --startup-project src/FoodOrderBot.API

# Apply lên database
dotnet ef database update `
  --project src/FoodOrderBot.Infrastructure `
  --startup-project src/FoodOrderBot.API

# Package Manager Console (Visual Studio)
# Default project phải set = FoodOrderBot.Infrastructure
Add-Migration <TenMigration> -StartupProject FoodOrderBot.API
Update-Database -StartupProject FoodOrderBot.API
```

Migration history:
- `InitialCreate` — 6 tables gốc (Shop, Customer, MenuItem, Order, OrderItem, RawMessage)
- `AddConversationMessage` — bảng ConversationMessages + 2 indexes
- `AddOrderNote` — ✅ Sprint 7: `ALTER TABLE "Orders" ADD "Note" text`
