import { useDroppable } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import type { JSX } from 'react';
import type { Order, OrderStatus } from '../../types';
import OrderCard from '../OrderCard/OrderCard';
import './KanbanBoard.css';

// ─── Column SVG Icons ─────────────────────────────────────────────────────────
function IconDraft() {
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" />
      <polyline points="14 2 14 8 20 8" />
      <line x1="16" y1="13" x2="8" y2="13" />
      <line x1="16" y1="17" x2="8" y2="17" />
      <polyline points="10 9 9 9 8 9" />
    </svg>
  );
}

function IconConfirmed() {
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="20 6 9 17 4 12" />
    </svg>
  );
}

function IconPreparing() {
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M12 2a10 10 0 110 20A10 10 0 0112 2z" />
      <polyline points="12 6 12 12 16 14" />
    </svg>
  );
}

function IconCompleted() {
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M22 11.08V12a10 10 0 11-5.93-9.14" />
      <polyline points="22 4 12 14.01 9 11.01" />
    </svg>
  );
}

function IconCancelled() {
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="10" />
      <line x1="15" y1="9" x2="9" y2="15" />
      <line x1="9" y1="9" x2="15" y2="15" />
    </svg>
  );
}

function IconEmptyBox() {
  return (
    <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.25" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="21 8 21 21 3 21 3 8" />
      <rect x="1" y="3" width="22" height="5" />
      <line x1="10" y1="12" x2="14" y2="12" />
    </svg>
  );
}

// ─── Column Config ────────────────────────────────────────────────────────────
interface ColumnConfig {
  id: OrderStatus;
  label: string;
  Icon: () => JSX.Element;
  color: string;
}

export const COLUMNS: ColumnConfig[] = [
  { id: 'Draft',     label: 'Chờ duyệt',   Icon: IconDraft,     color: '#6366f1' },
  { id: 'Confirmed', label: 'Xác nhận',    Icon: IconConfirmed, color: '#10b981' },
  { id: 'Preparing', label: 'Đang làm',    Icon: IconPreparing, color: '#f59e0b' },
  { id: 'Completed', label: 'Hoàn thành',  Icon: IconCompleted, color: '#22d3ee' },
  { id: 'Cancelled', label: 'Đã hủy',      Icon: IconCancelled, color: '#f43f5e' },
];

// ─── Sortable Item Wrapper ────────────────────────────────────────────────────
interface SortableOrderProps {
  order: Order;
  onConfirm?:  (id: string) => void;
  onCancel?:   (id: string) => void;
  onComplete?: (id: string) => void;
  onEdit?:     (order: Order) => void;
}

function SortableOrder({ order, onConfirm, onCancel, onComplete, onEdit }: SortableOrderProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } =
    useSortable({ id: order.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners}>
      <OrderCard
        order={order}
        isDragging={isDragging}
        onConfirm={onConfirm}
        onCancel={onCancel}
        onComplete={onComplete}
        onEdit={onEdit}
      />
    </div>
  );
}

// ─── Kanban Column ────────────────────────────────────────────────────────────
interface ColumnProps {
  column: ColumnConfig;
  orders: Order[];
  onConfirm?:  (id: string) => void;
  onCancel?:   (id: string) => void;
  onComplete?: (id: string) => void;
  onEdit?:     (order: Order) => void;
}

function KanbanColumn({ column, orders, onConfirm, onCancel, onComplete, onEdit }: ColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id: column.id });

  return (
    <div
      className={`kanban-column ${isOver ? 'kanban-column--over' : ''}`}
      style={{ '--col-color': column.color } as React.CSSProperties}
    >
      <div className="kanban-column__header">
        <span className="kanban-column__icon-wrap">
          <column.Icon />
        </span>
        <span className="kanban-column__title">{column.label}</span>
        <span className="kanban-column__count">{orders.length}</span>
      </div>

      <div ref={setNodeRef} className="kanban-column__body">
        <SortableContext
          items={orders.map((o) => o.id)}
          strategy={verticalListSortingStrategy}
        >
          {orders.length === 0 ? (
            <div className="kanban-column__empty">
              <span className="kanban-column__empty-icon">
                <IconEmptyBox />
              </span>
              <span className="kanban-column__empty-text">Không có đơn</span>
            </div>
          ) : (
            orders.map((order) => (
              <SortableOrder
                key={order.id}
                order={order}
                onConfirm={onConfirm}
                onCancel={onCancel}
                onComplete={onComplete}
                onEdit={onEdit}
              />
            ))
          )}
        </SortableContext>
      </div>
    </div>
  );
}

// ─── KanbanBoard (exported) ───────────────────────────────────────────────────
interface KanbanBoardProps {
  orders: Order[];
  onConfirm:  (id: string) => void;
  onCancel:   (id: string) => void;
  onComplete: (id: string) => void;
  onEdit:     (order: Order) => void;
}

export default function KanbanBoard({ orders, onConfirm, onCancel, onComplete, onEdit }: KanbanBoardProps) {
  return (
    <div className="kanban-board">
      {COLUMNS.map((col) => (
        <KanbanColumn
          key={col.id}
          column={col}
          orders={orders.filter((o) => o.status === col.id)}
          onConfirm={col.id === 'Draft' ? onConfirm : undefined}
          onCancel={
            col.id === 'Draft' || col.id === 'Confirmed' ? onCancel : undefined
          }
          onComplete={col.id === 'Preparing' ? onComplete : undefined}
          onEdit={col.id === 'Draft' ? onEdit : undefined}
        />
      ))}
    </div>
  );
}
