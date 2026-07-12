import { useDroppable } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import type { Order, OrderStatus } from '../../types';
import OrderCard from '../OrderCard/OrderCard';
import './KanbanBoard.css';

// ─── Sortable Item wrapper ─────────────────────────────────
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

// ─── Kanban Column ────────────────────────────────────────
interface ColumnConfig {
  id: OrderStatus;
  label: string;
  emoji: string;
  color: string;
}

export const COLUMNS: ColumnConfig[] = [
  { id: 'Draft',     label: 'Chờ duyệt',    emoji: '📋', color: '#6366f1' },
  { id: 'Confirmed', label: 'Đã xác nhận',  emoji: '✅', color: '#10b981' },
  { id: 'Preparing', label: 'Đang làm',     emoji: '🍳', color: '#f59e0b' },
  { id: 'Completed', label: 'Hoàn thành',   emoji: '🎉', color: '#22d3ee' },
  { id: 'Cancelled', label: 'Đã hủy',       emoji: '❌', color: '#ef4444' },
];

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
        <span className="kanban-column__emoji">{column.emoji}</span>
        <span className="kanban-column__title">{column.label}</span>
        <span className="kanban-column__count">{orders.length}</span>
      </div>

      <div ref={setNodeRef} className="kanban-column__body">
        <SortableContext
          items={orders.map((o) => o.id)}
          strategy={verticalListSortingStrategy}
        >
          {orders.length === 0 ? (
            <div className="kanban-column__empty">Không có đơn</div>
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

// ─── KanbanBoard (exported) ───────────────────────────────
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
