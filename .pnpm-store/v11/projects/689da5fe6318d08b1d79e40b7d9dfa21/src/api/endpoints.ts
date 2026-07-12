import api from './axios';
import type {
  AuthResult,
  LoginRequest,
  MenuItem,
  Order,
  CreateMenuItemRequest,
  UpdateMenuItemRequest,
  UpdateOrderRequest,
  OrderStatus,
  TrackOrderResult,
  AiResponse,
  AiTestRequest,
} from '../types';

// ─── Auth ────────────────────────────────────────────────
export const login = (data: LoginRequest) =>
  api.post<AuthResult>('/api/auth/login', data).then((r) => r.data);

// ─── Orders ──────────────────────────────────────────────
export const getOrders = () =>
  api.get<Order[]>('/api/orders').then((r) => r.data);

export const updateOrderStatus = (id: string, status: OrderStatus) =>
  api.patch<Order>(`/api/orders/${id}/status`, { status }).then((r) => r.data);

export const confirmOrder = (id: string) =>
  api.post<Order>(`/api/orders/${id}/confirm`).then((r) => r.data);

export const completeOrder = (id: string) =>
  api.patch<Order>(`/api/orders/${id}/status`, { status: 'Completed' }).then((r) => r.data);

export const cancelOrder = (id: string) =>
  api.patch<Order>(`/api/orders/${id}/status`, { status: 'Cancelled' }).then((r) => r.data);

export const updateOrder = (id: string, data: UpdateOrderRequest) =>
  api.patch<Order>(`/api/orders/${id}`, data).then((r) => r.data);

export const trackOrder = (token: string) =>
  api.get<TrackOrderResult>(`/api/orders/track/${token}`).then((r) => r.data);

// ─── Menu ─────────────────────────────────────────────────
export const getMenuItems = () =>
  api.get<MenuItem[]>('/api/shop/menu').then((r) => r.data);

export const createMenuItem = (data: CreateMenuItemRequest) =>
  api.post<MenuItem>('/api/shop/menu', data).then((r) => r.data);

export const updateMenuItem = (id: string, data: UpdateMenuItemRequest) =>
  api.put<MenuItem>(`/api/shop/menu/${id}`, data).then((r) => r.data);

export const deleteMenuItem = (id: string) =>
  api.delete(`/api/shop/menu/${id}`);

// ─── AI ───────────────────────────────────────────────────
export const testAi = (data: AiTestRequest) =>
  api.post<AiResponse>('/api/ai/test', data).then((r) => r.data);
