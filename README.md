# 🍜 FoodOrderBot

> AI-powered food ordering assistant for Facebook Messenger — understands natural Vietnamese messages, creates orders in real time, and helps shop owners manage everything from a live dashboard.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Recharts](https://img.shields.io/badge/Recharts-2.x-22b5bf)](https://recharts.org/)

## ✨ Features

- 💬 Receives and replies to Facebook Messenger messages via webhook.
- 🤖 Understands natural-language orders using AI (Groq + Semantic Kernel) — no rigid menu-number bots.
- 🔐 **Facebook Login (OAuth)** — shop owners log in with their Facebook account, pick which Page to manage, and get a JWT scoped to their shop.
- 🧾 Creates, confirms, updates, and tracks orders via a public tracking link.
- 📡 Real-time dashboard updates via SignalR — new orders appear instantly, no refresh needed.
- 👤 Human-in-the-loop: AI drafts the order, the owner reviews/edits before confirming.
- 🍱 Menu management with availability toggles.
- 📊 Analytics — daily revenue, top-selling items, peak hours, AI performance stats.
- ⚠️ Complaint management — sentiment detection with an attention flag and per-customer conversation history.

## 🏗️ Architecture

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

Clean Architecture with strict dependency flow — the domain stays completely isolated from infrastructure concerns:

| Module | Responsibility |
| --- | --- |
| [`FoodOrderBot.API`](src/FoodOrderBot.API) | HTTP API, webhook, JWT auth, SignalR, background worker |
| [`FoodOrderBot.Application`](src/FoodOrderBot.Application) | Use cases, DTOs, service contracts, order/analytics business logic |
| [`FoodOrderBot.Domain`](src/FoodOrderBot.Domain) | Entities, enums, repository interfaces — pure business rules |
| [`FoodOrderBot.Infrastructure`](src/FoodOrderBot.Infrastructure) | EF Core/PostgreSQL, AI orchestration, Facebook Messenger client |
| [`frontend`](frontend) | React dashboard for shop owners |

## 🧰 Tech Stack

- **Backend:** ASP.NET Core 10, Entity Framework Core, PostgreSQL 16, JWT, SignalR
- **AI:** Semantic Kernel + Groq API (llama-3.1-8b / llama-3.3-70b) — 5 plugins: intent classifier, order parser, chatbot, upsell, sentiment
- **Frontend:** React 19, TypeScript, Vite, TanStack Query, Zustand, Recharts, dnd-kit
- **Styling:** Vanilla CSS dark mode with design tokens (no UI framework)

## 🗺️ Sprint Roadmap

| Sprint | Content | Status |
|--------|----------|-----------|
| Sprint 1 | Foundation — Clean Architecture scaffold | ✅ Done |
| Sprint 2 | Database + AI parser (EF Core, Semantic Kernel) | ✅ Done |
| Sprint 3 | Business Logic & REST API (Orders, Menu) | ✅ Done |
| Sprint 4 | Webhook Pipeline + Facebook Messenger Reply | ✅ Done |
| Sprint 5 | Frontend Dashboard (React, Kanban, SignalR) | ✅ Done |
| Sprint 6 | AI Agent Layer — 5 plugins, multi-model | ✅ Done |
| Sprint 7 | Human-In-The-Loop + Dashboard Polish | ✅ Done |
| Sprint 8 | Analytics Dashboard + Complaint Management | ✅ Done |
| Sprint 9 | **Facebook Login (OAuth) + shop-scoped JWT** | ✅ Done |
| Sprint 10 | Menu Categories + AI Fuzzy Match | 📋 Planned |
| Sprint 11 | Deploy + Demo + Portfolio | 📋 Planned |

## 🚀 Getting Started

### Prerequisites

- .NET SDK 10
- Node.js 20+
- PostgreSQL 16+ (or Docker)
- A Facebook App on [developers.facebook.com](https://developers.facebook.com) (for Messenger + Login)

### 1. Configure secrets

Never commit database passwords, JWT keys, or Facebook/Groq keys. Use User Secrets locally:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=foodorderbot;Username=YOUR_USER;Password=YOUR_PASSWORD" --project src/FoodOrderBot.API
dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_KEY_AT_LEAST_32_CHARACTERS" --project src/FoodOrderBot.API
dotnet user-secrets set "Facebook:AppId" "YOUR_FB_APP_ID" --project src/FoodOrderBot.API
dotnet user-secrets set "Facebook:AppSecret" "YOUR_FB_APP_SECRET" --project src/FoodOrderBot.API
```

### 2. Set up the database and run the backend

```powershell
dotnet ef database update --project src/FoodOrderBot.Infrastructure --startup-project src/FoodOrderBot.API
dotnet run --project src/FoodOrderBot.API
```

The API runs at `http://localhost:5209`. Interactive API docs are available at `/scalar/v1` while the app is running.

### 3. Run the frontend

```powershell
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. Vite proxies `/api` and `/hubs` to the local backend.

## 🔐 Facebook App Configuration

To enable Messenger + Facebook Login for new shop owners:

1. Create an app on [developers.facebook.com](https://developers.facebook.com) and add the **Facebook Login** and **Messenger** products.
2. Add the required permissions (App Review → Permissions): `pages_show_list`, `pages_read_engagement`, `pages_messaging`, `pages_manage_metadata`.
3. In the app dashboard, set your webhook callback URL to `https://YOUR_HOST/api/webhook` with the verify token from `Facebook:VerifyToken`.
4. In `frontend/index.html`, replace `YOUR_FB_APP_ID` with your real App ID (needed by the Facebook JS SDK).
5. Development mode: only app admins/testers can log in. Switch to **Live mode** + App Review for other users.

The login flow: Facebook JS SDK → short-lived user token → backend exchanges it for a long-lived token → lists the user's Pages → owner selects a Page → upsert Shop → JWT with `shopId` claim.

## 📁 Project Structure

```text
FoodOrderBot/
├── frontend/                         # React dashboard
│   └── src/
│       ├── api/                      # Axios instance + endpoints
│       ├── components/               # Sidebar, OrderCard, KanbanBoard, ...
│       ├── pages/                    # Login, Dashboard, Menu, AiTest, Analytics, Complaints, Track
│       ├── store/                    # Zustand (authStore, ordersStore)
│       └── types/                    # TypeScript interfaces
├── src/
│   ├── FoodOrderBot.API/             # API, webhook, SignalR, controllers
│   ├── FoodOrderBot.Application/     # Business logic, analytics service
│   ├── FoodOrderBot.Domain/          # Core domain — entities, interfaces
│   └── FoodOrderBot.Infrastructure/  # Database, AI plugins, Facebook client
└── FoodOrderBot.slnx
```

## 📸 Screenshots

_Coming soon — dashboard Kanban, analytics charts, and the 3-step Facebook login._

## 🔒 Security Notes

- Secrets are stored via User Secrets / environment variables — never committed.
- Change the JWT signing key and all default credentials before deploying to production.
- Public order tracking uses an unguessable 32-byte token instead of the order ID.
- JWT `shopId` claim scopes every API call to the owner's shop.

## 📄 License

[MIT](LICENSE)
