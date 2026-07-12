import { useState } from 'react';
import type { Order } from '../../types';
import './OrderCard.css';

interface Props {
  order: Order;
  onConfirm?: (id: string) => void;
  onCancel?:  (id: string) => void;
  onComplete?: (id: string) => void;
  onEdit?: (order: Order) => void;
  isDragging?: boolean;
}

// ─── Confidence badge helpers ─────────────────────────────────────────────────

function getConfidenceLevel(confidence: number | null) {
  if (confidence === null || confidence === undefined) return null;
  if (confidence >= 0.8) return { label: `${Math.round(confidence * 100)}%`, cls: 'confidence--high',   icon: '✅' };
  if (confidence >= 0.5) return { label: `${Math.round(confidence * 100)}%`, cls: 'confidence--medium', icon: '⚠️' };
  return                        { label: `${Math.round(confidence * 100)}%`, cls: 'confidence--low',    icon: '❌' };
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
              {confidence.icon} {confidence.label}
            </span>
          )}
          <span className="order-card__time">{formattedTime}</span>
        </div>
      </div>

      <div className="order-card__date">{formattedDate}</div>

      {/* UnclearParts warning */}
      {hasUnclear && (
        <div className="order-card__unclear">
          <span className="unclear-icon">⚠️</span>
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
        <p className="order-card__note">💬 {order.note}</p>
      )}

      {/* AI Panel toggle (only for Draft orders with AI data) */}
      {order.status === 'Draft' && hasAiData && (
        <button
          className="ai-panel-toggle"
          onClick={(e) => { e.stopPropagation(); setShowAiPanel((v) => !v); }}
          aria-expanded={showAiPanel}
        >
          <span>🤖 AI hiểu gì</span>
          <span className={`toggle-chevron ${showAiPanel ? 'open' : ''}`}>›</span>
        </button>
      )}

      {/* Expandable AI interpretation panel */}
      {showAiPanel && (
        <div className="ai-panel">
          {order.rawMessageContent && (
            <div className="ai-panel__raw">
              <span className="ai-panel__label">📩 Tin gốc</span>
              <p className="ai-panel__value ai-panel__value--italic">
                "{order.rawMessageContent.slice(0, 120)}{order.rawMessageContent.length > 120 ? '…' : ''}"
              </p>
            </div>
          )}
          <div className="ai-panel__grid">
            {order.receiverName && (
              <div className="ai-panel__row">
                <span className="ai-panel__label">👤 Tên</span>
                <span className="ai-panel__value">{order.receiverName}</span>
              </div>
            )}
            {order.receiverPhone && (
              <div className="ai-panel__row">
                <span className="ai-panel__label">📞 SĐT</span>
                <span className="ai-panel__value">{order.receiverPhone}</span>
              </div>
            )}
            {order.deliveryAddress && (
              <div className="ai-panel__row ai-panel__row--full">
                <span className="ai-panel__label">📍 Địa chỉ</span>
                <span className="ai-panel__value">{order.deliveryAddress}</span>
              </div>
            )}
            {order.paymentMethod && (
              <div className="ai-panel__row">
                <span className="ai-panel__label">💳 Thanh toán</span>
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
              ✏️
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
