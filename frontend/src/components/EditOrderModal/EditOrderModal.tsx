import { useState, useEffect } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { updateOrder } from '../../api/endpoints';
import { useOrdersStore } from '../../store/ordersStore';
import type { Order, UpdateOrderItemRequest } from '../../types';
import './EditOrderModal.css';

interface Props {
  order: Order;
  onClose: () => void;
}

export default function EditOrderModal({ order, onClose }: Props) {
  const queryClient = useQueryClient();
  const { updateOrder: updateLocal } = useOrdersStore();

  const [receiverName, setReceiverName]       = useState(order.receiverName);
  const [receiverPhone, setReceiverPhone]     = useState(order.receiverPhone);
  const [deliveryAddress, setDeliveryAddress] = useState(order.deliveryAddress);
  const [paymentMethod, setPaymentMethod]     = useState(order.paymentMethod || 'COD');
  const [note, setNote]                       = useState(order.note ?? '');
  const [items, setItems] = useState<UpdateOrderItemRequest[]>(
    order.items.map((i) => ({
      menuItemId: i.menuItemId,
      itemName:   i.itemName,
      unitPrice:  i.unitPrice,
      quantity:   i.quantity,
      note:       i.note ?? '',
    }))
  );

  // Đóng khi nhấn Escape
  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if (e.key === 'Escape') onClose(); };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [onClose]);

  const mutation = useMutation({
    mutationFn: () =>
      updateOrder(order.id, {
        receiverName,
        receiverPhone,
        deliveryAddress,
        paymentMethod,
        note: note || undefined,
        items,
      }),
    onSuccess: (updated) => {
      updateLocal(order.id, updated);
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      onClose();
    },
  });

  const totalAmount = items.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0);

  const updateItemQty = (idx: number, qty: number) =>
    setItems((prev) => prev.map((item, i) => i === idx ? { ...item, quantity: Math.max(1, qty) } : item));

  const removeItem = (idx: number) =>
    setItems((prev) => prev.filter((_, i) => i !== idx));

  return (
    <div className="edit-modal-overlay" onClick={onClose}>
      <div className="edit-modal" onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className="edit-modal__header">
          <div>
            <h2 className="edit-modal__title">✏️ Sửa đơn hàng</h2>
            <p className="edit-modal__subtitle">
              Khách: <strong>{order.customerName || 'Ẩn danh'}</strong>
            </p>
          </div>
          <button className="edit-modal__close" onClick={onClose} aria-label="Đóng">✕</button>
        </div>

        <div className="edit-modal__body">
          {/* Thông tin giao hàng */}
          <section className="edit-section">
            <h3 className="edit-section__title">📍 Thông tin giao hàng</h3>
            <div className="edit-grid">
              <label className="edit-label">
                Tên người nhận
                <input
                  id="edit-receiver-name"
                  className="edit-input"
                  value={receiverName}
                  onChange={(e) => setReceiverName(e.target.value)}
                  placeholder="Nguyễn Văn A"
                />
              </label>
              <label className="edit-label">
                Số điện thoại
                <input
                  id="edit-receiver-phone"
                  className="edit-input"
                  value={receiverPhone}
                  onChange={(e) => setReceiverPhone(e.target.value)}
                  placeholder="0901234567"
                />
              </label>
              <label className="edit-label edit-label--full">
                Địa chỉ giao hàng
                <input
                  id="edit-delivery-address"
                  className="edit-input"
                  value={deliveryAddress}
                  onChange={(e) => setDeliveryAddress(e.target.value)}
                  placeholder="123 Nguyễn Huệ, Q.1, TP.HCM"
                />
              </label>
              <label className="edit-label">
                Thanh toán
                <select
                  id="edit-payment-method"
                  className="edit-input edit-select"
                  value={paymentMethod}
                  onChange={(e) => setPaymentMethod(e.target.value)}
                >
                  <option value="COD">💵 Tiền mặt (COD)</option>
                  <option value="Transfer">🏦 Chuyển khoản</option>
                  <option value="MoMo">💜 MoMo</option>
                </select>
              </label>
            </div>
          </section>

          {/* Danh sách món */}
          <section className="edit-section">
            <h3 className="edit-section__title">🍜 Danh sách món</h3>
            <div className="edit-items">
              {items.map((item, idx) => (
                <div key={idx} className="edit-item">
                  <span className="edit-item__name">{item.itemName}</span>
                  <span className="edit-item__price">
                    {item.unitPrice.toLocaleString('vi-VN')}₫
                  </span>
                  <div className="edit-item__qty-control">
                    <button
                      className="qty-btn"
                      onClick={() => updateItemQty(idx, item.quantity - 1)}
                      disabled={item.quantity <= 1}
                    >−</button>
                    <span className="qty-value">{item.quantity}</span>
                    <button
                      className="qty-btn"
                      onClick={() => updateItemQty(idx, item.quantity + 1)}
                    >+</button>
                  </div>
                  <span className="edit-item__subtotal">
                    {(item.unitPrice * item.quantity).toLocaleString('vi-VN')}₫
                  </span>
                  <button
                    className="edit-item__remove"
                    onClick={() => removeItem(idx)}
                    title="Xóa món"
                  >✕</button>
                </div>
              ))}
            </div>

            <div className="edit-total">
              <span>Tổng cộng</span>
              <span className="edit-total__amount">
                {totalAmount.toLocaleString('vi-VN')}₫
              </span>
            </div>
          </section>

          {/* Ghi chú */}
          <section className="edit-section">
            <h3 className="edit-section__title">💬 Ghi chú</h3>
            <textarea
              id="edit-note"
              className="edit-input edit-textarea"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="Không cần muối, thêm ớt..."
              rows={2}
            />
          </section>
        </div>

        {/* Footer actions */}
        <div className="edit-modal__footer">
          <button className="btn-secondary" onClick={onClose}>
            Hủy
          </button>
          <button
            id="save-order-btn"
            className="btn-primary"
            onClick={() => mutation.mutate()}
            disabled={mutation.isPending}
          >
            {mutation.isPending ? '⏳ Đang lưu...' : '💾 Lưu thay đổi'}
          </button>
        </div>
      </div>
    </div>
  );
}
