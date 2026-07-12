# 🍜 FoodOrderBot

> Trợ lý nhận đơn món ăn qua Facebook Messenger, kết hợp AI để hiểu tin nhắn tự nhiên và giúp chủ quán quản lý đơn hàng theo thời gian thực.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)

## ✨ Tính năng

- 💬 Nhận và phản hồi tin nhắn Facebook Messenger thông qua webhook.
- 🤖 Phân loại ý định, trích xuất món ăn và tạo phản hồi bằng AI.
- 🧾 Tạo, xác nhận, cập nhật và theo dõi đơn hàng bằng liên kết công khai.
- 📡 Cập nhật đơn hàng real-time cho dashboard qua SignalR.
- 🍱 Quản lý menu và trạng thái hiển thị món ăn.
- 🔐 Đăng nhập dashboard bằng JWT.

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
| [`FoodOrderBot.API`](src/FoodOrderBot.API/README.md) | HTTP API, webhook, xác thực JWT, SignalR, background worker |
| [`FoodOrderBot.Application`](src/FoodOrderBot.Application/README.md) | Use case, DTO, interface/service contract và nghiệp vụ đơn hàng |
| [`FoodOrderBot.Domain`](src/FoodOrderBot.Domain/README.md) | Entity, enum và repository interface thuần nghiệp vụ |
| [`FoodOrderBot.Infrastructure`](src/FoodOrderBot.Infrastructure/README.md) | EF Core/PostgreSQL, AI orchestration, Facebook Messenger client |
| [`frontend`](frontend/README.md) | Dashboard React + Vite cho chủ quán |

## 🧰 Công nghệ

- **Backend:** ASP.NET Core, Entity Framework Core, PostgreSQL, JWT, SignalR
- **AI:** Semantic Kernel và Groq API (tùy chọn)
- **Frontend:** React, TypeScript, Vite, TanStack Query, Zustand
- **Giao diện:** Vanilla CSS, drag & drop với dnd-kit

## 🚀 Chạy local

### Yêu cầu

- .NET SDK 10
- Node.js 20+
- PostgreSQL 16+ (hoặc một PostgreSQL tương thích)

### 1. Cấu hình secret

Không commit mật khẩu database, JWT key hoặc Facebook/Groq key. Dùng User Secrets cho máy local:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=foodorderbot;Username=YOUR_USER;Password=YOUR_PASSWORD" --project src/FoodOrderBot.API
dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_KEY_AT_LEAST_32_CHARACTERS" --project src/FoodOrderBot.API
dotnet user-secrets set "Admin:Email" "admin@example.com" --project src/FoodOrderBot.API
dotnet user-secrets set "Admin:Password" "YOUR_STRONG_PASSWORD" --project src/FoodOrderBot.API
```

Để bật AI/Facebook, thêm các giá trị cần thiết vào User Secrets hoặc biến môi trường (ví dụ `Groq__ApiKey`, `Facebook__AppSecret`, `Facebook__VerifyToken`, `Facebook__PageAccessToken`).

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
├── src/
│   ├── FoodOrderBot.API/              # API, webhook, SignalR
│   ├── FoodOrderBot.Application/      # Nghiệp vụ ứng dụng
│   ├── FoodOrderBot.Domain/           # Core domain
│   └── FoodOrderBot.Infrastructure/   # Database, AI, Facebook
└── FoodOrderBot.slnx
```

## 🔒 Lưu ý bảo mật

- Không đưa `.env`, User Secrets, access token hay file publish lên Git.
- Trước khi deploy, thay JWT key và toàn bộ mật khẩu/credential mặc định.
- Dùng biến môi trường của nền tảng triển khai để cấu hình production.
