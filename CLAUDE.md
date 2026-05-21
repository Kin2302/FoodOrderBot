# FoodOrderBot – Project Context

## Tổng Quan

Hệ thống tự động hóa tiếp nhận & xử lý đơn hàng đồ ăn từ **Facebook Messenger + Comment** bằng AI.
Mục tiêu: đồ án tốt nghiệp / CV portfolio — demo full flow, clean code, deployed thực tế.

**Luồng chính:**
```
Khách chat/comment → Facebook Webhook → .NET API (200 OK ngay) → Channel<T> Queue
→ BackgroundWorker → [Dedup → Save RawMessage → AI Parse → Draft Order → SignalR Push]
→ Chủ quán duyệt trên Kanban → Confirm → Gửi Messenger link tracking → Khách tracking realtime
```

---

## Tech Stack

### Backend
| | |
|---|---|
| Framework | ASP.NET Core 10 (Web API) |
| ORM | EF Core **9.0.4** + Npgsql **9.0.4** (phải cùng major version) |
| Database | PostgreSQL 16+ |
| AI | Microsoft.SemanticKernel + Groq API (`llama-3.3-70b-versatile`) |
| Realtime | ASP.NET Core SignalR (built-in, KHÔNG dùng package riêng) |
| Auth | JWT Bearer 9.0.4 (hardcode credentials trong appsettings cho MVP) |
| Queue | `System.Threading.Channels.Channel<T>` (in-memory, no Redis) |
| API Docs | Scalar.AspNetCore |
| Background | `IHostedService` / `BackgroundService` |

### Frontend (chưa làm)
| | |
|---|---|
| Framework | React 18 + Vite + TypeScript |
| State | Zustand |
| Realtime | @microsoft/signalr |
| HTTP | Axios + React Query |
| Drag & Drop | @dnd-kit/core + @dnd-kit/sortable |
| Routing | React Router v6 |

### Deploy
| | |
|---|---|
| Backend | Railway.app (Docker-based) |
| Frontend | Vercel |
| Dev webhook | ngrok |

---

## Kiến Trúc – Clean Architecture (4 layers)

```
API → Application → Domain
 ↓
Infrastructure → Application + Domain
```

### Dependency Rules (KHÔNG ĐƯỢC VI PHẠM)
- **Domain**: chỉ chứa Entities, Enums, Repository Interfaces — không biết gì về Application/Infrastructure
- **Application**: chứa Service Interfaces (Contracts/), DTOs, business logic — không biết về Infrastructure
- **Infrastructure**: implement các interfaces từ Domain và Application — biết về EF Core, Groq, Facebook API
- **API**: composition root — inject tất cả, chứa Controllers, Hubs, Middleware, BackgroundServices

---

## Cấu Trúc Thư Mục

```
D:\DotNet\FoodOrderBot\
├── CLAUDE.md                          ← file này
├── FoodOrderBot.slnx
└── src\
    ├── FoodOrderBot.Domain\
    │   ├── Entities\                  Shop, Customer, MenuItem, RawMessage, Order, OrderItem
    │   ├── Enums\                     OrderStatus, MessageSource, PaymentStatus
    │   └── Interfaces\                IOrderRepository, IRawMessageRepository
    │
    ├── FoodOrderBot.Application\
    │   ├── Contracts\                 IOrderService, IMessageParser, IMessengerReply
    │   ├── Orders\
    │   │   ├── Dtos\                  OrderDto, CreateOrderRequest, UpdateOrderRequest
    │   │   ├── OrderService.cs        (chưa làm)
    │   │   └── OrderStateMachine.cs   ✅ done — Draft→Confirmed→Preparing→Completed/Cancelled
    │   ├── Parsing\
    │   │   ├── ParseResultDto.cs      ✅ done
    │   │   └── MessageParserService.cs (chưa làm)
    │   ├── Messaging\
    │   │   └── MessengerReplyService.cs (chưa làm)
    │   └── Auth\
    │       └── AuthDtos.cs            ✅ done — LoginRequest, AuthResult
    │
    ├── FoodOrderBot.Infrastructure\
    │   ├── Persistence\
    │   │   ├── AppDbContext.cs         ✅ done
    │   │   ├── Configurations\        ✅ done — Fluent API cho tất cả entities
    │   │   └── Repositories\          ⬜ chưa làm
    │   ├── SemanticKernel\            ⬜ chưa làm
    │   └── Facebook\                  ⬜ chưa làm
    │
    └── FoodOrderBot.API\
        ├── Controllers\
        │   ├── AuthController.cs      ✅ done — POST /api/auth/login → JWT
        │   ├── WebhookController.cs   ✅ done — GET verify + POST receive
        │   ├── OrderController.cs     ⬜ chưa làm
        │   └── ShopController.cs      ⬜ chưa làm
        ├── Hubs\
        │   └── OrderHub.cs            ✅ done — [Authorize], /hubs/orders
        ├── BackgroundServices\
        │   ├── WebhookTaskQueue.cs    ✅ done — Channel<WebhookTask>
        │   └── WebhookProcessingWorker.cs ✅ skeleton done, logic chưa điền
        ├── Middleware\
        │   └── ExceptionHandlingMiddleware.cs ✅ done
        ├── Program.cs                 ✅ done
        └── appsettings.json           ✅ done (chứa placeholder secrets)
```

---

## Database Schema (PostgreSQL)

6 bảng, PK dạng UUID, quan hệ:

```
Shop (1:N) → MenuItem
Shop (1:N) → Order
Shop (1:N) → RawMessage
Customer (1:N) → Order
Order (1:N) → OrderItem
OrderItem (N:1) → MenuItem
RawMessage (1:1) → Order
```

### Các điểm quan trọng
- `RawMessage.FbMessageId` — **UNIQUE index** — dùng để dedup webhook events
- `Order.TrackingToken` — **UNIQUE index** — chuỗi CSPRNG 32-byte, không expose OrderId ra ngoài
- `RawMessage.ParsedResult` — kiểu `jsonb` trong PostgreSQL
- `Order.Status` và `Order.PaymentStatus` — lưu dạng **string** (không phải int) để dễ đọc trong DB
- `OrderItem.ItemName` + `OrderItem.UnitPrice` — **snapshot** tại thời điểm đặt, không thay đổi theo menu

---

## Business Rules Quan Trọng

### OrderStateMachine
Chỉ cho phép chuyển trạng thái theo đúng luồng:
```
Draft → Confirmed | Cancelled
Confirmed → Preparing | Cancelled
Preparing → Completed | Cancelled
Completed → (terminal, không chuyển)
Cancelled → (terminal, không chuyển)
```
Dùng `OrderStateMachine.ThrowIfInvalidTransition()` trước mọi thay đổi trạng thái.

### Webhook Processing
1. `WebhookController` chỉ được làm 2 việc: validate chữ ký + enqueue. Trả 200 OK trong < 500ms.
2. Mọi xử lý nặng (AI, DB, SignalR) thuộc về `WebhookProcessingWorker`.
3. Dedup bắt buộc: check `FbMessageId` tồn tại trước khi xử lý.

### AI Parser
- Confidence < 0.8 → tự động gửi Messenger hỏi lại khách, KHÔNG tạo Draft Order
- Confidence >= 0.8 → tạo Draft Order, push SignalR lên Dashboard
- Luôn lưu `ParsedResult` (JSONB) vào `RawMessage` kể cả khi parse thất bại (để fine-tune sau)

### SignalR & JWT
- Hub `/hubs/orders` yêu cầu `[Authorize]`
- JWT từ query string `?access_token=...` được chấp nhận (vì WebSocket không set header)

---

## Config Keys (appsettings.json)

```json
{
  "ConnectionStrings": { "DefaultConnection": "..." },
  "Jwt": { "Key": "...", "Issuer": "FoodOrderBot", "Audience": "FoodOrderBot", "ExpiryMinutes": 1440 },
  "Facebook": { "AppSecret": "...", "VerifyToken": "...", "FrontendBaseUrl": "..." },
  "Groq": { "ApiKey": "...", "ModelId": "llama-3.3-70b-versatile" },
  "Admin": { "Email": "admin@foodorderbot.com", "Password": "Admin@123!" },
  "Shop": { "DefaultShopId": "00000000-0000-0000-0000-000000000001" },
  "AllowedOrigins": ["http://localhost:5173"]
}
```

> KHÔNG commit secrets thật. Dùng `appsettings.Development.json` hoặc Railway Environment Variables.

---

## Trạng Thái Hiện Tại

**Sprint 1 – Foundation**: ✅ HOÀN THÀNH (`dotnet build` → 0 errors)

**Đang ở Sprint 2** — Cần làm:
1. `OrderRepository` + `RawMessageRepository`
2. EF Core migration (cần PostgreSQL đang chạy)
3. Semantic Kernel + Groq integration
4. `MessageParserService` với prompt tiếng Việt
5. Điền logic vào `WebhookProcessingWorker`

---

## Coding Conventions

- Dùng **primary constructor** cho dependency injection khi có thể: `class Foo(IBar bar) {}`
- Dùng **collection expression** `[]` thay `new List<T>()`: `public List<X> Items { get; set; } = [];`
- Async methods kèm `CancellationToken ct = default` parameter
- Repository pattern: Domain/Interfaces định nghĩa, Infrastructure/Repositories implement
- Service interfaces trong `Application/Contracts/`, không phải `Domain/Interfaces/`
- Tất cả error handling tập trung tại `ExceptionHandlingMiddleware`
- Comment bằng tiếng Việt nếu giải thích business logic, tiếng Anh cho technical detail

---

## Lệnh Hữu Ích

```powershell
# Build
dotnet build

# Chạy dev
dotnet run --project src/FoodOrderBot.API

# EF Migration (cần PostgreSQL)
dotnet ef migrations add <TenMigration> --project src/FoodOrderBot.Infrastructure --startup-project src/FoodOrderBot.API
dotnet ef database update --project src/FoodOrderBot.Infrastructure --startup-project src/FoodOrderBot.API

# Test ngrok
ngrok http 5000
```
