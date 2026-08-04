import { useState } from 'react';
import type { Order } from '../../types';
import './OrderCard.css';

// ─── SVG Icons ────────────────────────────────────────────────────────────────
function IconEdit() {
  return (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7" />
      <path d="M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5z" />
    </svg>
  );
}

function IconChevron() {
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="9 18 15 12 9 6" />
    </svg>
  );
}

function IconRobot() {
  return (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round">
      <rect x="2" y="7" width="20" height="13" rx="2.5" />
      <path d="M8 21v-2" />
      <path d="M16 21v-2" />
      <path d="M12 7V4" />
      <circle cx="12" cy="3" r="1" />
      <circle cx="9" cy="13" r="1.2" fill="currentColor" stroke="none" />
      <circle cx="15" cy="13" r="1.2" fill="currentColor" stroke="none" />
      <path d="M9 17s1 1.2 3 1.2 3-1.2 3-1.2" />
    </svg>
  );
}

function IconMessage() {
  return (
    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z" />
    </svg>
  );
}

function IconUser() {
  return (
    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2" />
      <circle cx="12" cy="7" r="4" />
    </svg>
  );
}

function IconPhone() {
  return (
    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M22 16.92v3a2 2 0 01-2.18 2 19.79 19.79 0 01-8.63-3.07A19.5 19.5 0 013.07 9.81 19.79 19.79 0 01.12 1.18 2 2 0 012.1 0h3a2 2 0 012 1.72c.127.96.361 1.903.7 2.81a2 2 0 01-.45 2.11L6.91 7.09a16 16 0 006 6l.46-.46a2 2 0 012.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0122 16.92z" />
    </svg>
  );
}

function IconPin() {
  return (
    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0118 0z" />
      <circle cx="12" cy="10" r="3" />
    </svg>
  );
}

function IconCard() {
  return (
    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <rect x="1" y="4" width="22" height="16" rx="2" ry="2" />
      <line x1="1" y1="10" x2="23" y2="10" />
    </svg>
  );
}

function IconWarning() {
  return (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
      <line x1="12" y1="9" x2="12" y2="13" />
      <line x1="12" y1="17" x2="12.01" y2="17" />
    </svg>
  );
}

function IconNote() {
  return (
    <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" />
      <polyline points="14 2 14 8 20 8" />
      <line x1="16" y1="13" x2="8" y2="13" />
      <line x1="16" y1="17" x2="8" y2="17" />
    </svg>
  );
}

// ─── Confidence badge helpers ─────────────────────────────────────────────────

function getConfidenceLevel(confidence: number | null) {
  if (confidence === null || confidence === undefined) return null;
  if (confidence >= 0.8) return { label: `${Math.round(confidence * 100)}%`, cls: 'confidence--high'   };
  if (confidence >= 0.5) return { label: `${Math.round(confidence * 100)}%`, cls: 'confidence--medium' };
  return                        { label: `${Math.round(confidence * 100)}%`, cls: 'confidence--low'    };
}

// ─── Component ────────────────────────────────────────────────────────────────
interface Props {
  order: Order;
  onConfirm?:  (id: string) => void;
  onCancel?:   (id: string) => void;
  onComplete?: (id: string) => void;
  onEdit?:     (order: Order) => void;
  isDragging?: boolean;
}

export default function OrderCard({
  order,
  onConfirm,
  onCancel,
  onComplete,
  onEdit,
  isDragging,
}: Props) {
  const [showAiPanel, setShowAiPanel] = useState(false);

  const formattedTime = new Date(order.createdAt).toLocaleTimeString('vi-VN', {
    hour: '2-digit', minute: '2-digit',
  });
  const formattedDate = new Date(order.createdAt).toLocaleDateString('vi-VN');

  const confidence  = getConfidenceLevel(order.parseConfidence);
  const hasAiData   = !!(order.receiverName || order.receiverPhone || order.deliveryAddress || order.rawMessageContent);
  const hasUnclear  = order.unclearParts?.length > 0;

  return (
    <div className={`order-card ${isDragging ? 'dragging' : ''} ${hasUnclear ? 'order-card--unclear' : ''}`}>
      {/* Top accent bar */}
      <div className="order-card__top-bar" />

      {/* Header: customer + time + confidence badge */}
      <div className="order-card__header">
        <span className="order-card__customer">
          {order.customerName || 'Khách ẩn danh'}
        </span>
        <div className="order-card__meta">
          {confidence && (
            <span className={`confidence-badge ${confidence.cls}`} title="Độ tin cậy AI">
              <span className="confidence-dot" />
              {confidence.label}
            </span>
          )}
          <span className="order-card__time">{formattedTime}</span>
        </div>
      </div>

      <div className="order-card__date">{formattedDate}</div>

      {/* UnclearParts warning */}
      {hasUnclear && (
        <div className="order-card__unclear">
          <span className="unclear-icon">
            <IconWarning />
          </span>
          <span className="unclear-text">
            AI chưa chắc: {order.unclearParts.join(' · ')}
          </span>
        </div>
      )}

      {/* Items list */}
      <ul className="order-card__items">
        {order.items.map((item) => (
          <li key={item.id} className="order-card__item">
            <span className="order-card__item-name">{item.itemName}</span>
            <span className="order-card__item-qty">×{item.quantity}</span>
            <span className="order-card__item-price">
              {(item.unitPrice * item.quantity).toLocaleString('vi-VN')}₫
            </span>
          </li>
        ))}
      </ul>

      {/* Note */}
      {order.note && (
        <p className="order-card__note">
          <span className="order-card__note-icon"><IconNote /></span>
          {order.note}
        </p>
      )}

      {/* AI Panel toggle */}
      {order.status === 'Draft' && hasAiData && (
        <button
          className="ai-panel-toggle"
          onClick={(e) => { e.stopPropagation(); setShowAiPanel((v) => !v); }}
          aria-expanded={showAiPanel}
        >
          <span className="ai-panel-toggle__left">
            <span className="ai-panel-toggle__icon"><IconRobot /></span>
            AI hiểu gì
          </span>
          <span className={`toggle-chevron ${showAiPanel ? 'open' : ''}`}>
            <IconChevron />
          </span>
        </button>
      )}

      {/* Expandable AI interpretation panel */}
      {showAiPanel && (
        <div className="ai-panel">
          {order.rawMessageContent && (
            <div className="ai-panel__raw">
              <span className="ai-panel__label">
                <IconMessage />
                Tin gốc
              </span>
              <p className="ai-panel__value ai-panel__value--italic">
                "{order.rawMessageContent.slice(0, 120)}{order.rawMessageContent.length > 120 ? '…' : ''}"
              </p>
            </div>
          )}
          <div className="ai-panel__grid">
            {order.receiverName && (
              <div className="ai-panel__row">
                <span className="ai-panel__label"><IconUser />Tên</span>
                <span className="ai-panel__value">{order.receiverName}</span>
              </div>
            )}
            {order.receiverPhone && (
              <div className="ai-panel__row">
                <span className="ai-panel__label"><IconPhone />SĐT</span>
                <span className="ai-panel__value">{order.receiverPhone}</span>
              </div>
            )}
            {order.deliveryAddress && (
              <div className="ai-panel__row ai-panel__row--full">
                <span className="ai-panel__label"><IconPin />Địa chỉ</span>
                <span className="ai-panel__value">{order.deliveryAddress}</span>
              </div>
            )}
            {order.paymentMethod && (
              <div className="ai-panel__row">
                <span className="ai-panel__label"><IconCard />Thanh toán</span>
                <span className="ai-panel__value">{order.paymentMethod}</span>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Footer: total + action buttons */}
      <div className="order-card__footer">
        <span className="order-card__total">
          {order.totalAmount.toLocaleString('vi-VN')}₫
        </span>

        <div className="order-card__actions">
          {/* Edit button — only on Draft */}
          {order.status === 'Draft' && onEdit && (
            <button
              id={`edit-${order.id}`}
              className="btn btn--edit"
              onClick={(e) => { e.stopPropagation(); onEdit(order); }}
              title="Sửa đơn"
            >
              <IconEdit />
            </button>
          )}

          {/* Confirm button — only on Draft */}
          {order.status === 'Draft' && onConfirm && (
            <button
              id={`confirm-${order.id}`}
              className="btn btn--confirm"
              onClick={(e) => { e.stopPropagation(); onConfirm(order.id); }}
            >
              Xác nhận
            </button>
          )}

          {/* Complete button — only on Preparing */}
          {order.status === 'Preparing' && onComplete && (
            <button
              id={`complete-${order.id}`}
              className="btn btn--complete"
              onClick={(e) => { e.stopPropagation(); onComplete(order.id); }}
            >
              Hoàn thành
            </button>
          )}

          {/* Cancel button — Draft or Confirmed */}
          {(order.status === 'Draft' || order.status === 'Confirmed') && onCancel && (
            <button
              id={`cancel-${order.id}`}
              className="btn btn--cancel"
              onClick={(e) => { e.stopPropagation(); onCancel(order.id); }}
            >
              Hủy
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
