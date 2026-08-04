import { useState, useCallback, useMemo } from 'react';
import {
  DndContext,
  type DragEndEvent,
  type DragStartEvent,
  DragOverlay,
  MouseSensor,
  TouchSensor,
  useSensor,
  useSensors,
  closestCorners,
} from '@dnd-kit/core';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getOrders,
  updateOrderStatus,
  confirmOrder,
  completeOrder,
  cancelOrder,
} from '../api/endpoints';
import { useOrdersStore } from '../store/ordersStore';
import { useSignalR } from '../hooks/useSignalR';
import KanbanBoard, { COLUMNS } from '../components/KanbanBoard/KanbanBoard';
import OrderCard from '../components/OrderCard/OrderCard';
import Sidebar from '../components/Sidebar/Sidebar';
import EditOrderModal from '../components/EditOrderModal/EditOrderModal';
import type { Order, OrderStatus } from '../types';
import './DashboardPage.css';

// ─── SVG Icons ───────────────────────────────────────────────────────────────
function IconSearch() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="11" cy="11" r="8" />
      <line x1="21" y1="21" x2="16.65" y2="16.65" />
    </svg>
  );
}

function IconX() {
  return (
    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
      <line x1="18" y1="6" x2="6" y2="18" />
      <line x1="6" y1="6" x2="18" y2="18" />
    </svg>
  );
}

// ─── Filter Status Config ─────────────────────────────────────────────────────
const STATUS_FILTERS = [
  { value: 'All',       label: 'Tất cả' },
  { value: 'Draft',     label: 'Chờ duyệt' },
  { value: 'Confirmed', label: 'Xác nhận' },
  { value: 'Preparing', label: 'Đang làm' },
  { value: 'Completed', label: 'Hoàn thành' },
  { value: 'Cancelled', label: 'Đã hủy' },
] as const;

export default function DashboardPage() {
  const queryClient = useQueryClient();
  const { orders, setOrders, updateOrderStatus: updateLocal } = useOrdersStore();

  const [activeOrder,   setActiveOrder]   = useState<Order | null>(null);
  const [editingOrder,  setEditingOrder]  = useState<Order | null>(null);
  const [searchQuery,   setSearchQuery]   = useState('');
  const [statusFilter,  setStatusFilter]  = useState<OrderStatus | 'All'>('All');

  // Kết nối SignalR để nhận realtime updates
  useSignalR();

  // Load orders từ API
  useQuery({
    queryKey: ['orders'],
    queryFn: async () => {
      const data = await getOrders();
      setOrders(data);
      return data;
    },
  });

  // ─── Mutations ─────────────────────────────────────────────────────────────

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: OrderStatus }) =>
      updateOrderStatus(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['orders'] }),
  });

  const confirmMutation = useMutation({
    mutationFn: confirmOrder,
    onMutate: (id) => updateLocal(id, 'Confirmed'),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['orders'] }),
  });

  const completeMutation = useMutation({
    mutationFn: completeOrder,
    onMutate: (id) => updateLocal(id, 'Completed'),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['orders'] }),
  });

  const cancelMutation = useMutation({
    mutationFn: cancelOrder,
    onMutate: (id) => updateLocal(id, 'Cancelled'),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['orders'] }),
  });

  // ─── DnD ──────────────────────────────────────────────────────────────────

  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 8 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 200, tolerance: 5 } })
  );

  const handleDragStart = useCallback(
    ({ active }: DragStartEvent) => {
      const found = orders.find((o) => o.id === active.id);
      setActiveOrder(found ?? null);
    },
    [orders]
  );

  const handleDragEnd = useCallback(
    ({ active, over }: DragEndEvent) => {
      setActiveOrder(null);
      if (!over) return;

      const orderId   = active.id as string;
      const targetCol = over.id as OrderStatus;

      const isColumn = COLUMNS.some((c) => c.id === targetCol);
      if (!isColumn) return;

      const order = orders.find((o) => o.id === orderId);
      if (!order || order.status === targetCol) return;

      updateLocal(orderId, targetCol);
      statusMutation.mutate({ id: orderId, status: targetCol });
    },
    [orders, updateLocal, statusMutation]
  );

  // ─── Handlers ─────────────────────────────────────────────────────────────

  const handleConfirm  = useCallback((id: string)   => confirmMutation.mutate(id),  [confirmMutation]);
  const handleComplete = useCallback((id: string)   => completeMutation.mutate(id), [completeMutation]);
  const handleCancel   = useCallback((id: string)   => cancelMutation.mutate(id),   [cancelMutation]);
  const handleEdit     = useCallback((order: Order) => setEditingOrder(order),       []);

  // ─── Filter + Search ──────────────────────────────────────────────────────

  const filteredOrders = useMemo(() => {
    let result = orders;

    if (statusFilter !== 'All') {
      result = result.filter((o) => o.status === statusFilter);
    }

    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase().trim();
      result = result.filter(
        (o) =>
          o.customerName?.toLowerCase().includes(q) ||
          o.receiverName?.toLowerCase().includes(q) ||
          o.receiverPhone?.includes(q) ||
          o.deliveryAddress?.toLowerCase().includes(q)
      );
    }

    return result;
  }, [orders, statusFilter, searchQuery]);

  // ─── Stats ────────────────────────────────────────────────────────────────

  const totalOrders  = orders.length;
  const draftCount   = orders.filter((o) => o.status === 'Draft').length;
  const todayRevenue = orders
    .filter(
      (o) =>
        o.status === 'Completed' &&
        new Date(o.createdAt).toDateString() === new Date().toDateString()
    )
    .reduce((sum, o) => sum + o.totalAmount, 0);

  // ─── Render ───────────────────────────────────────────────────────────────

  return (
    <div className="app-layout">
      <Sidebar />

      <main className="dashboard">
        {/* Header */}
        <header className="dashboard__header">
          <div className="dashboard__header-left">
            <h1 className="dashboard__title">Dashboard</h1>
            <p className="dashboard__subtitle">Quản lý đơn hàng realtime</p>
          </div>
          <div className="dashboard__stats">
            <div className="stat-chip">
              <span className="stat-chip__value">{totalOrders}</span>
              <span className="stat-chip__label">Tổng đơn</span>
            </div>
            <div className="stat-chip stat-chip--warning">
              <span className="stat-chip__value">{draftCount}</span>
              <span className="stat-chip__label">Chờ duyệt</span>
            </div>
            <div className="stat-chip stat-chip--success">
              <span className="stat-chip__value">
                {todayRevenue.toLocaleString('vi-VN')}₫
              </span>
              <span className="stat-chip__label">Hôm nay</span>
            </div>
          </div>
        </header>

        {/* Filter & Search Bar */}
        <div className="dashboard__toolbar">
          <div className="filter-tabs">
            {STATUS_FILTERS.map(({ value, label }) => (
              <button
                key={value}
                id={`filter-${value.toLowerCase()}`}
                className={`filter-tab ${statusFilter === value ? 'filter-tab--active' : ''}`}
                onClick={() => setStatusFilter(value)}
              >
                {label}
                {value !== 'All' && (
                  <span className="filter-tab__count">
                    {orders.filter((o) => o.status === value).length}
                  </span>
                )}
              </button>
            ))}
          </div>

          <div className="search-box">
            <span className="search-box__icon">
              <IconSearch />
            </span>
            <input
              id="order-search"
              type="search"
              className="search-box__input"
              placeholder="Tìm tên, SĐT, địa chỉ..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            {searchQuery && (
              <button
                className="search-box__clear"
                onClick={() => setSearchQuery('')}
                aria-label="Xóa tìm kiếm"
              >
                <IconX />
              </button>
            )}
          </div>
        </div>

        {/* Kanban Board */}
        <div className="dashboard__board">
          <DndContext
            sensors={sensors}
            collisionDetection={closestCorners}
            onDragStart={handleDragStart}
            onDragEnd={handleDragEnd}
          >
            <KanbanBoard
              orders={filteredOrders}
              onConfirm={handleConfirm}
              onCancel={handleCancel}
              onComplete={handleComplete}
              onEdit={handleEdit}
            />

            {/* Drag overlay */}
            <DragOverlay>
              {activeOrder && (
                <div style={{ opacity: 0.9, transform: 'rotate(1.5deg) scale(1.02)' }}>
                  <OrderCard order={activeOrder} />
                </div>
              )}
            </DragOverlay>
          </DndContext>
        </div>
      </main>

      {/* Edit Order Modal */}
      {editingOrder && (
        <EditOrderModal
          order={editingOrder}
          onClose={() => setEditingOrder(null)}
        />
      )}
    </div>
  );
}
