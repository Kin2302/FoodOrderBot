# 🍜 FoodOrderBot

> Trợ lý nhận đơn món ăn qua Facebook Messenger, kết hợp AI để hiểu tin nhắn tự nhiên và giúp chủ quán quản lý đơn hàng theo thời gian thực.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Recharts](https://img.shields.io/badge/Recharts-2.x-22b5bf)](https://recharts.org/)

## ✨ Tính năng

- 💬 Nhận và phản hồi tin nhắn Facebook Messenger thông qua webhook.
- 🤖 Phân loại ý định, trích xuất món ăn và tạo phản hồi bằng AI (Groq + Semantic Kernel).
- 🧾 Tạo, xác nhận, cập nhật và theo dõi đơn hàng bằng liên kết công khai.
- 📡 Cập nhật đơn hàng real-time cho dashboard qua SignalR.
- 🍱 Quản lý menu và trạng thái hiển thị món ăn.
- 🔐 Đăng nhập dashboard bằng JWT.
- 📊 **Thống kê Analytics** — doanh thu theo ngày, top món bán chạy, phân bố giờ cao điểm, AI performance.
- ⚠️ **Quản lý Khiếu Nại** — danh sách complaints với sentiment badge, lịch sử hội thoại per-khách.

## 🏗️ Kiến trúc

```text
Facebook Messenger ── Webhook ──► API (.NET)
                                      │
                     ┌────────────────┼────────────────┐
                     ▼                ▼                ▼
               Application      Infrastructure      SignalR
                 use cases      AI · EF Core · FB      Hub
                     │                │                │
                     ▼                ▼                ▼
                  Domain         PostgreSQL       Dashboard (React)
```

Dự án theo hướng Clean Architecture, tách biệt domain và nghiệp vụ khỏi hạ tầng kỹ thuật:

| Module | Trách nhiệm |
| --- | --- |
| [`FoodOrderBot.API`](src/FoodOrderBot.API) | HTTP API, webhook, xác thực JWT, SignalR, background worker |
| [`FoodOrderBot.Application`](src/FoodOrderBot.Application) | Use case, DTO, service contract, nghiệp vụ đơn hàng, analytics |
| [`FoodOrderBot.Domain`](src/FoodOrderBot.Domain) | Entity, enum và repository interface thuần nghiệp vụ |
| [`FoodOrderBot.Infrastructure`](src/FoodOrderBot.Infrastructure) | EF Core/PostgreSQL, AI orchestration, Facebook Messenger client |
| [`frontend`](frontend) | Dashboard React + Vite cho chủ quán |

## 🧰 Công nghệ

- **Backend:** ASP.NET Core 10, Entity Framework Core 9, PostgreSQL 16, JWT, SignalR
- **AI:** Semantic Kernel + Groq API (llama-3.1-8b / llama-3.3-70b)
- **Frontend:** React 19, TypeScript, Vite 8, TanStack Query, Zustand, Recharts
- **Giao diện:** Vanilla CSS dark mode, drag & drop với dnd-kit

## 🗺️ Sprint Roadmap

| Sprint | Nội dung | Trạng thái |
|--------|----------|-----------|
| Sprint 1 | Foundation — Clean Architecture scaffold | ✅ Hoàn thành |
| Sprint 2 | Database + AI parser (EF Core, Semantic Kernel) | ✅ Hoàn thành |
| Sprint 3 | Business Logic & REST API (Orders, Menu) | ✅ Hoàn thành |
| Sprint 4 | Webhook Pipeline + Facebook Messenger Reply | ✅ Hoàn thành |
| Sprint 5 | Frontend Dashboard (React, Kanban, SignalR) | ✅ Hoàn thành |
| Sprint 6 | AI Agent Layer — 5 plugins, multi-model | ✅ Hoàn thành |
| Sprint 7 | Human-In-The-Loop + Dashboard Polish | ✅ Hoàn thành |
| Sprint 8 | Analytics Dashboard + Complaint Management | ✅ Hoàn thành |
| Sprint 9 | Menu Categories + AI Fuzzy Match | 📋 Kế hoạch |
| Sprint 10 | Deploy + Demo + Portfolio | 📋 Kế hoạch |

## 🚀 Chạy local

### Yêu cầu

- .NET SDK 10
- Node.js 20+
- PostgreSQL 16+ (hoặc Docker)

### 1. Cấu hình secret

Không commit mật khẩu database, JWT key hoặc Facebook/Groq key. Dùng User Secrets cho máy local:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=foodorderbot;Username=YOUR_USER;Password=YOUR_PASSWORD" --project src/FoodOrderBot.API
dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_KEY_AT_LEAST_32_CHARACTERS" --project src/FoodOrderBot.API
dotnet user-secrets set "Admin:Email" "admin@example.com" --project src/FoodOrderBot.API
dotnet user-secrets set "Admin:Password" "YOUR_STRONG_PASSWORD" --project src/FoodOrderBot.API
```

Để bật AI/Facebook, thêm các giá trị cần thiết (`Groq__ApiKey`, `Facebook__AppSecret`, `Facebook__VerifyToken`, `Facebook__PageAccessToken`).

### 2. Khởi tạo database và chạy backend

```powershell
dotnet ef database update --project src/FoodOrderBot.Infrastructure --startup-project src/FoodOrderBot.API
dotnet run --project src/FoodOrderBot.API
```

API mặc định chạy tại `http://localhost:5209`. Tài liệu API có tại `/scalar/v1` khi ứng dụng đang chạy.

### 3. Chạy frontend

```powershell
cd frontend
npm install
npm run dev
```

Mở `http://localhost:5173`. Vite đã cấu hình proxy `/api` và `/hubs` đến backend local.

## 📁 Cấu trúc thư mục

```text
FoodOrderBot/
├── frontend/                         # React dashboard
│   └── src/
│       ├── api/                      # Axios + endpoints (auth, orders, menu, analytics, complaints)
│       ├── components/               # Sidebar, OrderCard, KanbanBoard, ...
│       ├── pages/                    # Login, Dashboard, Menu, AiTest, Analytics, Complaints
│       ├── store/                    # Zustand (authStore, ordersStore)
│       └── types/                    # TypeScript interfaces
├── src/
│   ├── FoodOrderBot.API/             # API, webhook, SignalR, controllers
│   ├── FoodOrderBot.Application/     # Nghiệp vụ ứng dụng, analytics service
│   ├── FoodOrderBot.Domain/          # Core domain — entities, interfaces
│   └── FoodOrderBot.Infrastructure/  # Database, AI plugins, Facebook client
└── FoodOrderBot.slnx
```

## 🔒 Lưu ý bảo mật

- Không đưa `.env`, User Secrets, access token hay file publish lên Git.
- Trước khi deploy, thay JWT key và toàn bộ mật khẩu/credential mặc định.
- Dùng biến môi trường của nền tảng triển khai để cấu hình production.
