import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '../store/authStore';
import { useOrdersStore } from '../store/ordersStore';
import type { Order, OrderStatus } from '../types';

const HUB_URL = import.meta.env.VITE_HUB_URL;

export function useSignalR() {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const token = useAuthStore((s) => s.token);
  const { addOrder, updateOrderStatus } = useOrdersStore();

  useEffect(() => {
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        // JWT qua query string vì WebSocket không cho phép set header
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Nhận Draft Order mới từ AI parse
    connection.on('NewDraftOrder', (order: Order) => {
      addOrder(order);
      // Hiển thị toast notification
      showToast(`🆕 Đơn mới từ ${order.customerName || 'Khách'}!`);
    });

    // Nhận cập nhật trạng thái đơn
    connection.on('OrderStatusUpdated', (id: string, status: OrderStatus) => {
      updateOrderStatus(id, status);
    });

    connection
      .start()
      .then(() => console.log('[SignalR] Connected'))
      .catch((err) => console.error('[SignalR] Connection failed:', err));

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, [token, addOrder, updateOrderStatus]);

  return connectionRef.current;
}

// Simple toast utility (không dùng thư viện ngoài)
function showToast(message: string) {
  const toast = document.createElement('div');
  toast.className = 'app-toast';
  toast.textContent = message;
  document.body.appendChild(toast);
  setTimeout(() => toast.classList.add('show'), 10);
  setTimeout(() => {
    toast.classList.remove('show');
    setTimeout(() => toast.remove(), 300);
  }, 3500);
}
