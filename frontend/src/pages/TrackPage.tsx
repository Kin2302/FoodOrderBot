import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { trackOrder } from '../api/endpoints';
import type { OrderStatus } from '../types';
import './TrackPage.css';

const STEPS: { status: OrderStatus; label: string; emoji: string }[] = [
  { status: 'Draft',     label: 'Đã nhận đơn',    emoji: '📋' },
  { status: 'Confirmed', label: 'Đã xác nhận',     emoji: '✅' },
  { status: 'Preparing', label: 'Đang chuẩn bị',   emoji: '🍳' },
  { status: 'Completed', label: 'Hoàn thành',       emoji: '🎉' },
];

const STATUS_ORDER: OrderStatus[] = ['Draft', 'Confirmed', 'Preparing', 'Completed'];

export default function TrackPage() {
  const { token } = useParams<{ token: string }>();

  const { data: order, isLoading, isError } = useQuery({
    queryKey: ['track', token],
    queryFn: () => trackOrder(token!),
    enabled: !!token,
    refetchInterval: 10_000, // poll mỗi 10s
  });

  if (isLoading) {
    return (
      <div className="track-page track-page--loading">
        <div className="track-spinner" />
        <p>Đang tải thông tin đơn hàng...</p>
      </div>
    );
  }

  if (isError || !order) {
    return (
      <div className="track-page track-page--error">
        <span className="track-error-icon">😕</span>
        <h2>Không tìm thấy đơn hàng</h2>
        <p>Token không hợp lệ hoặc đơn hàng không tồn tại.</p>
      </div>
    );
  }

  const isCancelled = order.status === 'Cancelled';
  const currentIdx = STATUS_ORDER.indexOf(order.status);

  return (
    <div className="track-page">
      <div className="track-bg">
        <div className="track-blob track-blob--1" />
        <div className="track-blob track-blob--2" />
      </div>

      <div className="track-card">
        <div className="track-header">
          <span className="track-logo">🤖</span>
          <h1 className="track-title">Theo dõi đơn hàng</h1>
          <p className="track-token">#{token?.slice(0, 8).toUpperCase()}</p>
        </div>

        {isCancelled ? (
          <div className="track-cancelled">
            <span>❌</span>
            <p>Đơn hàng đã bị hủy</p>
          </div>
        ) : (
          <div className="track-timeline">
            {STEPS.map((step, idx) => {
              const isDone = idx <= currentIdx;
              const isActive = idx === currentIdx;
              return (
                <div
                  key={step.status}
                  className={`track-step ${isDone ? 'track-step--done' : ''} ${isActive ? 'track-step--active' : ''}`}
                >
                  <div className="track-step__indicator">
                    <span className="track-step__emoji">
                      {isDone ? step.emoji : '○'}
                    </span>
                    {idx < STEPS.length - 1 && (
                      <div className={`track-step__line ${isDone && idx < currentIdx ? 'track-step__line--done' : ''}`} />
                    )}
                  </div>
                  <div className="track-step__content">
                    <span className="track-step__label">{step.label}</span>
                    {isActive && (
                      <span className="track-step__badge">Hiện tại</span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}

        {/* Order details */}
        <div className="track-items">
          <h3 className="track-items__title">Chi tiết đơn</h3>
          <ul className="track-items__list">
            {order.items.map((item) => (
              <li key={item.id} className="track-items__item">
                <span>{item.itemName}</span>
                <span className="track-items__qty">×{item.quantity}</span>
                <span className="track-items__price">
                  {item.subtotal.toLocaleString('vi-VN')}₫
                </span>
              </li>
            ))}
          </ul>
          {order.note && (
            <p className="track-note">💬 Ghi chú: {order.note}</p>
          )}
          <div className="track-total">
            <span>Tổng cộng</span>
            <span className="track-total__amount">
              {order.totalAmount.toLocaleString('vi-VN')}₫
            </span>
          </div>
        </div>

        <p className="track-footer">
          Cập nhật mỗi 10 giây · FoodOrderBot 🤖
        </p>
      </div>
    </div>
  );
}
