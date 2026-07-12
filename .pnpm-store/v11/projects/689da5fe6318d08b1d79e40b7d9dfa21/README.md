# FoodOrderBot — Frontend

## Tech Stack
- **React 18** + **Vite** + **TypeScript**
- **Zustand** + persist middleware (state management)
- **@microsoft/signalr** (realtime)
- **Axios** + **@tanstack/react-query** (HTTP + caching)
- **@dnd-kit/core** + **@dnd-kit/sortable** (Kanban DnD)
- **React Router v6** (routing)
- **Vanilla CSS** (dark mode tokens, glassmorphism)

---

## Chạy Dev

```powershell
npm install
npm run dev      # http://localhost:5173
npm run build    # production bundle
```

> Vite proxy `/api` và `/hubs` → `http://localhost:5209` (xem `vite.config.ts`).
> Backend phải đang chạy ở port 5209.

---

## Cấu Trúc `src/`

```
src/
├── api/
│   ├── axios.ts        — Axios instance, JWT interceptor, auto-redirect 401
│   └── endpoints.ts    — Auth, Orders, Menu, AI API calls
├── components/
│   ├── KanbanBoard/    — 5 cột DnD, DroppableColumn, DragOverlay
│   ├── OrderCard/      — Card đơn hàng, hover/drag animations
│   ├── Sidebar/        — Navigation: Dashboard | Thực đơn | AI Console
│   └── ProtectedRoute.tsx — Redirect /login nếu chưa auth
├── hooks/
│   └── useSignalR.ts   — Kết nối SignalR hub, xử lý events
├── pages/
│   ├── LoginPage.tsx   — Glassmorphism login form
│   ├── DashboardPage.tsx — Kanban board chính
│   ├── MenuPage.tsx    — CRUD menu items
│   ├── TrackPage.tsx   — Public order tracking (không cần auth)
│   └── AiTestPage.tsx  — 🤖 AI Console (test AI pipeline trực tiếp)
├── store/
│   ├── authStore.ts    — JWT token, email, login/logout (persist localStorage)
│   └── ordersStore.ts  — Orders state, realtime updates
├── types/
│   └── index.ts        — Tất cả TypeScript interfaces
└── index.css           — Design system: CSS variables, fonts, toast, scrollbar
```

---

## Pages & Routes

| Route | Component | Auth | Mô tả |
|---|---|---|---|
| `/login` | `LoginPage` | ❌ | Login với JWT |
| `/dashboard` | `DashboardPage` | ✅ | Kanban + SignalR |
| `/menu` | `MenuPage` | ✅ | CRUD menu items |
| `/ai-test` | `AiTestPage` | ✅ | Test AI pipeline |
| `/track/:token` | `TrackPage` | ❌ | Khách tracking đơn |

---

## State Management (Zustand)

### `authStore`
```typescript
{ token, email, login(token, email), logout() }
// persist: localStorage key = "auth-store"
```

### `ordersStore`
```typescript
{ orders, setOrders, addOrder, updateOrderStatus }
// Được cập nhật bởi: initial fetch + SignalR events
```

---

## SignalR Events (`useSignalR.ts`)

| Event (server → client) | Handler |
|---|---|
| `NewOrderReceived` | Thêm đơn mới vào store + hiện toast notification |
| `OrderStatusUpdated` | Cập nhật status đơn trong store |

Connection URL: `/hubs/orders?access_token=<JWT>`

---

## AI Test Page (`/ai-test`)

Trang test AI pipeline trực tiếp cho chủ quán:
- **Chat UI** — giao diện chat, gửi tin → xem AI reply
- **Quick test buttons** — các tin nhắn mẫu cho từng intent
- **Session context** — tin nhắn cùng session có conversation memory
- **AI Result Card** — expandable, hiển thị:
  - Intent badge (màu theo loại)
  - Confidence bar
  - Sentiment indicator (🟢/🟡/🔴) + ⚠️ NeedsAttention
  - Parse result (items, địa chỉ, SĐT)
  - Upsell suggestions
- **Stats Panel** — đếm theo intent trong session

API: `POST /api/ai/test` (yêu cầu auth)

---

## Design System (`index.css`)

CSS Variables chính:
```css
--bg-primary    — nền tối chính
--bg-secondary  — nền card/sidebar
--surface       — surface elements
--border        — màu viền
--accent        — màu chủ đạo (tím)
--text-primary  — text chính
--text-muted    — text phụ
```

Font: **Inter** (Google Fonts)

---

## TypeScript Types (`types/index.ts`)

| Type/Interface | Mô tả |
|---|---|
| `Order` | Đơn hàng đầy đủ |
| `OrderItem` | Dòng đơn hàng |
| `MenuItem` | Món ăn |
| `OrderStatus` | `'Draft' \| 'Confirmed' \| 'Preparing' \| 'Completed' \| 'Cancelled'` |
| `AiIntent` | 7 intents (PlaceOrder, AskMenu, ...) |
| `AiResponse` | Kết quả từ `/api/ai/test` |
| `SentimentResult` | `{ label, score, needsAttention }` |
| `UpsellSuggestion` | `{ itemName, price, reason }` |
