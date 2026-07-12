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

## 📚 Tài Liệu Chi Tiết Per-Layer

> **Đọc file tương ứng khi cần thông tin chi tiết về 1 layer cụ thể.**

| Layer | File | Nội dung chính |
|---|---|---|
| Domain | `src/FoodOrderBot.Domain/README.md` | Entities, Enums, Repository interfaces, quy tắc không phụ thuộc |
| Application | `src/FoodOrderBot.Application/README.md` | Contracts (IAiOrchestrator, IOrderService...), AI types (AiIntent, AiDtos), OrderService, State machine |
| Infrastructure | `src/FoodOrderBot.Infrastructure/README.md` | AiKernelFactory, 5 AI plugins, prompt templates, EF config, Repositories, MessengerClient |
| API | `src/FoodOrderBot.API/README.md` | Controllers + endpoints, SignalR events, WebhookProcessingWorker 5-bước, DI registration order, config keys |
| Frontend | `frontend/README.md` | Routes, Zustand stores, SignalR events, AiTestPage, design system CSS variables |

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
    │   │                              ConversationMessage ✅ (Sprint 6)
    │   ├── Enums\                     OrderStatus, MessageSource, PaymentStatus
    │   └── Interfaces\                IOrderRepository, IRawMessageRepository, IMenuItemRepository
    │                                  ICustomerRepository, IConversationRepository ✅ (Sprint 6)
    │
    ├── FoodOrderBot.Application\
    │   ├── Contracts\                 IOrderService, IMessageParser, IMessengerReply
    │   │                              IAiOrchestrator ✅ (Sprint 6)
    │   ├── AI\                        AiIntent enum, AiDtos (AiRequest/Response/UpsellSuggestion/SentimentResult)
    │   ├── Orders\
    │   │   ├── Dtos\                  OrderDto, CreateOrderRequest, UpdateOrderRequest
    │   │   ├── OrderService.cs        ✅ done
    │   │   └── OrderStateMachine.cs   ✅ done — Draft→Confirmed→Preparing→Completed/Cancelled
    │   ├── Parsing\
    │   │   └── ParseResultDto.cs      ✅ done
    │   ├── Messaging\
    │   │   └── (IMessengerReply interface)
    │   └── Auth\
    │       └── AuthDtos.cs            ✅ done — LoginRequest, AuthResult
    │
    ├── FoodOrderBot.Infrastructure\
    │   ├── AI\                        ← MỚI (Sprint 6)
    │   │   ├── AiKernelFactory.cs     ✅ Singleton, cache Kernel per model
    │   │   ├── AiOrchestrator.cs      ✅ Implement IAiOrchestrator — route theo intent
    │   │   ├── Plugins\
    │   │   │   ├── IntentClassifierPlugin.cs  ✅ (model: 8b)
    │   │   │   ├── OrderParserPlugin.cs       ✅ (model: 70b) — refactor + context
    │   │   │   ├── ChatBotPlugin.cs           ✅ (model: 8b)
    │   │   │   ├── UpsellPlugin.cs            ✅ (model: 8b)
    │   │   │   └── SentimentPlugin.cs         ✅ (model: 8b)
    │   │   └── Prompts\
    │   │       ├── intent_classifier.txt
    │   │       ├── order_parser.txt
    │   │       ├── chatbot_reply.txt
    │   │       ├── upsell_suggest.txt
    │   │       └── sentiment_analyzer.txt
    │   ├── Persistence\
    │   │   ├── AppDbContext.cs         ✅ done (+ ConversationMessages DbSet)
    │   │   ├── Configurations\        ✅ done — + ConversationMessageConfiguration
    │   │   ├── DbInitializer.cs       ✅ done — migrate + seed 1 Shop + 10 món
    │   │   └── Repositories\
    │   │       ├── OrderRepository.cs        ✅ done
    │   │       ├── RawMessageRepository.cs   ✅ done
    │   │       ├── MenuItemRepository.cs     ✅ done
    │   │       ├── CustomerRepository.cs     ✅ done
    │   │       └── ConversationRepository.cs ✅ done (Sprint 6)
    │   ├── SemanticKernel\
    │   │   └── MessageParserService.cs ✅ thin wrapper → OrderParserPlugin
    │   └── Facebook\
    │       └── MessengerClient.cs      ✅ done — SendText + SendTrackingLink
    │
    └── FoodOrderBot.API\
        ├── Controllers\
        │   ├── AuthController.cs      ✅ done — POST /api/auth/login → JWT
        │   ├── WebhookController.cs   ✅ done — GET verify + POST receive
        │   ├── OrderController.cs     ✅ done — 5× [Authorize] + 1× [AllowAnonymous] /track
        │   ├── ShopController.cs      ✅ done — GET/POST/PUT/DELETE /api/shop/menu
        │   └── AiController.cs        ✅ done (Sprint 6) — POST /api/ai/test
        ├── Hubs\
        │   └── OrderHub.cs            ✅ done — [Authorize], /hubs/orders
        ├── BackgroundServices\
        │   ├── WebhookTaskQueue.cs    ✅ done — Channel<WebhookTask>
        │   └── WebhookProcessingWorker.cs ✅ refactored (Sprint 6) — dùng IAiOrchestrator
        ├── Middleware\
        │   └── ExceptionHandlingMiddleware.cs ✅ done
        ├── Program.cs                 ✅ done — DI đầy đủ bao gồm AI services
        └── appsettings.json           ✅ done — multi-model config (Groq:Models:*)
```

---

## Database Schema (PostgreSQL)

6 bảng, PK dạng UUID, quan hệ:

```
Shop (1:N) → MenuItem
Shop (1:N) → Order
Shop (1:N) → RawMessage
Shop (1:N) → ConversationMessage      ← MỚI (Sprint 6)
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

**Sprint 1 – Foundation**: ✅ HOÀN THÀNH
**Sprint 2 – Database + AI**: ✅ HOÀN THÀNH
- OrderRepository, RawMessageRepository ✅
- DbInitializer (migrate + seed 1 Shop + 10 món) ✅
- MessageParserService (Groq + SK + prompt tiếng Việt) ✅

**Sprint 3 – Business Logic & REST API**: ✅ HOÀN THÀNH (2026-05-26, build 0 errors)
- `OrderService.cs` — CreateDraft (TrackingToken URL-safe), Confirm, UpdateStatus (StateMachine), UpdateOrder, GetAll, GetByToken ✅
- `IMenuItemRepository` + `MenuItemRepository` ✅
- `IRawMessageRepository.UpdateAsync` + `RawMessageRepository.UpdateAsync` ✅
- `OrderController.cs` — 6 endpoints (5× Authorize + 1× AllowAnonymous `/track/{token}`) ✅
- `ShopController.cs` — 4 endpoints CRUD menu (soft delete) ✅
- DI Registration: IOrderService, IMenuItemRepository đã đăng ký ✅

**Sprint 4 – Webhook Pipeline + Messenger Reply**: ✅ HOÀN THÀNH (2026-06-04, build 0 errors)
- `ICustomerRepository` + `CustomerRepository` — upsert customer theo FbSenderId ✅
- `MessengerClient.cs` — implement IMessengerReply (Facebook Send API v21.0) ✅
- `WebhookProcessingWorker.cs` — logic 5 bước hoàn chỉnh (Dedup → Customer → RawMsg → AI Parse → Draft/Reply) ✅
- DI: `AddHttpClient<IMessengerReply, MessengerClient>()` + `ICustomerRepository` ✅
- `appsettings.json` — thêm `PageAccessToken` fallback ✅

**Sprint 5 – Frontend Dashboard (React)**: ✅ HOÀN THÀNH (2026-06-24, build 0 errors)
- `frontend/` — scaffold React 18 + Vite + TypeScript ✅
- `src/types/index.ts` — TypeScript interfaces (Order, MenuItem, OrderStatus, ...) ✅
- `src/api/axios.ts` — Axios instance + JWT interceptor + auto-redirect 401 ✅
- `src/api/endpoints.ts` — Auth, Orders, Menu API calls ✅
- `src/store/authStore.ts` — Zustand + persist middleware (JWT → localStorage) ✅
- `src/store/ordersStore.ts` — Zustand orders store (realtime updates) ✅
- `src/hooks/useSignalR.ts` — SignalR hook (NewDraftOrder + OrderStatusUpdated events) ✅
- `src/components/ProtectedRoute.tsx` — redirect /login nếu chưa auth ✅
- `src/components/OrderCard/` — card hiển thị đơn hàng + hover/drag animations ✅
- `src/components/KanbanBoard/` — 5 cột DnD-Kit, Droppable zones, DragOverlay ✅
- `src/components/Sidebar/` — navigation, user info, logout ✅
- `src/pages/LoginPage.tsx` — glassmorphism card, animated blobs, React Query mutation ✅
- `src/pages/DashboardPage.tsx` — Kanban + SignalR + stats header + DnD Context ✅
- `src/pages/MenuPage.tsx` — CRUD menu items, modal form, grid layout, toggle availability ✅
- `src/pages/TrackPage.tsx` — public tracking, timeline animation, auto-refresh 10s ✅
- `src/index.css` — design system (dark mode tokens, Inter font, toast notification) ✅
- `vite.config.ts` — proxy /api & /hubs/orders → port 5209 (WebSocket support) ✅
- Build: 158 modules, 446KB JS / 27KB CSS — **0 TypeScript errors** ✅

**Sprint 6 – AI Agent Layer (Multi-Model)**: ✅ HOÀN THÀNH (2026-06-25, build 0 errors)
- `ConversationMessage` entity + `IConversationRepository` — lịch sử hội thoại AI ✅
- `AiIntent` enum (7 intents) + `AiDtos` (AiRequest/Response/UpsellSuggestion/SentimentResult) ✅
- `IAiOrchestrator` interface — entry point thay thế IMessageParser trực tiếp ✅
- `AiKernelFactory` — Singleton, cache Kernel per model (không tạo mới mỗi request) ✅
- 5 prompt template files (tách khỏi code hardcode) ✅
- `IntentClassifierPlugin` — phân loại intent (model: llama-3.1-8b-instant) ✅
- `OrderParserPlugin` — parse đơn hàng + conversation context (model: llama-3.3-70b-versatile) ✅
- `ChatBotPlugin` — reply Greeting/AskMenu/Compliment/Other (model: 8b) ✅
- `UpsellPlugin` — gợi ý bán thêm sau khi đặt đơn (model: 8b) ✅
- `SentimentPlugin` — phân tích cảm xúc + flag NeedsAttention (model: 8b) ✅
- `AiOrchestrator` — routing + parallel complaint handling + save conversation ✅
- `ConversationRepository` — query 5 tin gần nhất theo sender ✅
- `MessageParserService` refactored → thin wrapper (backward compatible) ✅
- `WebhookProcessingWorker` refactored — reply mọi intent (không bỏ qua greeting/complaint) ✅
- `AiController` — POST /api/ai/test cho live demo ✅
- `AiTestPage` (`/ai-test`) — chat UI + intent stats + expandable AI result cards ✅
- `appsettings.json` — multi-model config `Groq:Models:*` ✅
- EF Migration `AddConversationMessage` — table + 2 indexes ✅
- Build: **0 errors** backend + **0 TypeScript errors** frontend ✅

---

## Kế Hoạch Các Sprint Tiếp Theo

### Sprint 7 – Human-In-The-Loop + Dashboard Polish ✅ HOÀN THÀNH

**Backend:**
- `OrderRepository.GetByShopIdAsync`: Include RawMessage để map ParseConfidence ✅
- `OrderService.MapToDto`: Deserialize ParsedResult JSON → ParseConfidence + UnclearParts ✅
- `OrderController`: thêm endpoint `POST /api/orders/{id}/confirm` ✅
- `UpdateOrderRequest`: thêm field `Note` ✅

**Frontend:**
- `types/index.ts`: thêm AI fields (parseConfidence, unclearParts, rawMessageContent, receiverName, receiverPhone, deliveryAddress, paymentMethod) vào Order interface ✅
- `api/endpoints.ts`: fix confirmOrder, thêm updateOrder + completeOrder ✅
- `ordersStore.ts`: thêm updateOrder action ✅
- `OrderCard`: AI Confidence badge (màu theo mức), Expandable AI panel "AI hiểu gì", UnclearParts warnings, nút Complete (Preparing), nút Edit ✅
- `EditOrderModal`: form sửa thông tin giao hàng + items (modal glassmorphism) ✅
- `DashboardPage`: filter bar (status tabs + search input), handlers handleComplete + handleEdit ✅
- `KanbanBoard`: thêm onComplete + onEdit props ✅

---

### Sprint 8 – Complaint Dashboard + Analytics 📋 KẾ HOẠCH

**Backend:**
- `GET /api/analytics/summary` — doanh thu, số đơn, món bán chạy, giờ cao điểm
- `GET /api/analytics/ai-stats` — tỷ lệ parse success, tỷ lệ hỏi lại
- `GET /api/complaints` — query complaints với NeedsAttention flag
- `GET /api/conversations/{senderId}` — lịch sử chat theo khách

**Frontend:**
- `/analytics` page — KPI cards, charts doanh thu theo ngày/tuần/tháng
- Tab "Cần xử lý" trên Dashboard — complaints với severity badge
- Lịch sử hội thoại per-khách
- Sidebar: thêm nav item Analytics + Complaints

---

### Sprint 9 – Menu Categories + AI Fuzzy Match + Auth Refactor 📋 KẾ HOẠCH

**Backend:**
- `Category` entity + API CRUD
- `MenuItem`: thêm field `Category`, `ImageUrl` upload lên cloud
- `OrderParserPlugin`: fuzzy matching tên món (Levenshtein) → gợi ý "Bạn có muốn đặt X thay vì Y?"
- `ShopId` lấy từ JWT claims thay vì hard-code `DefaultShopId`

**Frontend:**
- MenuPage: filter/group theo danh mục, drag-to-reorder (DisplayOrder)
- Upload ảnh món ăn
- TrackPage: SignalR realtime thay vì poll 10s
- ETA "dự kiến 20-30 phút" trên TrackPage + OrderCard

---

### Sprint 10 – Deploy + Demo + Portfolio 📋 KẾ HOẠCH

- `Dockerfile` cho API project
- Deploy Railway (backend) + Vercel (frontend)
- Seed data đẹp — nhiều orders, nhiều trạng thái
- Demo script: khách nhắn Messenger giả lập → AI tạo đơn → dashboard realtime → chủ quán xác nhận → khách tracking → analytics cập nhật
- Screenshots + Video demo 1-2 phút
- `README.md` viết theo kiểu sản phẩm thật (features, tech stack, demo link, screenshots)

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

# EF Migration (cần PostgreSQL đang chạy qua Docker)
dotnet ef migrations add <TenMigration> --project src/FoodOrderBot.Infrastructure --startup-project src/FoodOrderBot.API
dotnet ef database update --project src/FoodOrderBot.Infrastructure --startup-project src/FoodOrderBot.API

# Test ngrok (khi cần test Facebook Webhook)
ngrok http 5000
```

## Docker PostgreSQL (đang dùng)

```yaml
# User: admin | Password: mysecretpassword123 | DB: foodorderbot | Port: 5432
# pgAdmin: localhost:8080 | Email: admin@admin.com | Password: adminpassword123
```

Connection string hiện tại:
```
Host=localhost;Port=5432;Database=foodorderbot;Username=admin;Password=mysecretpassword123
```
