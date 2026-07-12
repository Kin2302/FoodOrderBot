import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getMenuItems, createMenuItem, updateMenuItem, deleteMenuItem } from '../api/endpoints';
import Sidebar from '../components/Sidebar/Sidebar';
import type { MenuItem, CreateMenuItemRequest } from '../types';
import './MenuPage.css';

export default function MenuPage() {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editItem, setEditItem] = useState<MenuItem | null>(null);
  const [form, setForm] = useState<CreateMenuItemRequest>({
    name: '',
    price: 0,
    description: '',
    imageUrl: '',
  });

  const { data: items = [], isLoading } = useQuery({
    queryKey: ['menu'],
    queryFn: getMenuItems,
  });

  const createMutation = useMutation({
    mutationFn: createMenuItem,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['menu'] });
      resetForm();
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateMenuItemRequest }) =>
      updateMenuItem(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['menu'] });
      resetForm();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteMenuItem,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['menu'] }),
  });

  const toggleAvailability = useMutation({
    mutationFn: ({ id, isAvailable }: { id: string; isAvailable: boolean }) =>
      updateMenuItem(id, { isAvailable }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['menu'] }),
  });

  const resetForm = () => {
    setForm({ name: '', price: 0, description: '', imageUrl: '' });
    setShowForm(false);
    setEditItem(null);
  };

  const handleEdit = (item: MenuItem) => {
    setEditItem(item);
    setForm({
      name: item.name,
      price: item.price,
      description: item.description ?? '',
      imageUrl: item.imageUrl ?? '',
    });
    setShowForm(true);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (editItem) {
      updateMutation.mutate({ id: editItem.id, data: form });
    } else {
      createMutation.mutate(form);
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="app-layout">
      <Sidebar />

      <main className="menu-page">
        <header className="menu-page__header">
          <div>
            <h1 className="menu-page__title">Thực đơn</h1>
            <p className="menu-page__subtitle">
              {items.length} món · Quản lý các món ăn của quán
            </p>
          </div>
          <button
            id="add-menu-item-btn"
            className="btn-primary"
            onClick={() => { resetForm(); setShowForm(true); }}
          >
            + Thêm món
          </button>
        </header>

        {/* Form Modal */}
        {showForm && (
          <div className="modal-overlay" onClick={resetForm}>
            <div className="modal" onClick={(e) => e.stopPropagation()}>
              <h2 className="modal__title">
                {editItem ? '✏️ Sửa món' : '➕ Thêm món mới'}
              </h2>
              <form className="menu-form" onSubmit={handleSubmit}>
                <label className="menu-form__label">
                  Tên món *
                  <input
                    id="menu-name"
                    className="menu-form__input"
                    value={form.name}
                    onChange={(e) => setForm({ ...form, name: e.target.value })}
                    required
                    placeholder="VD: Phở bò tái"
                  />
                </label>
                <label className="menu-form__label">
                  Giá (₫) *
                  <input
                    id="menu-price"
                    type="number"
                    className="menu-form__input"
                    value={form.price}
                    onChange={(e) => setForm({ ...form, price: Number(e.target.value) })}
                    required
                    min={0}
                    placeholder="65000"
                  />
                </label>
                <label className="menu-form__label">
                  Mô tả
                  <textarea
                    id="menu-desc"
                    className="menu-form__input menu-form__textarea"
                    value={form.description}
                    onChange={(e) => setForm({ ...form, description: e.target.value })}
                    placeholder="Mô tả ngắn về món ăn..."
                    rows={3}
                  />
                </label>
                <label className="menu-form__label">
                  URL ảnh
                  <input
                    id="menu-image"
                    className="menu-form__input"
                    value={form.imageUrl}
                    onChange={(e) => setForm({ ...form, imageUrl: e.target.value })}
                    placeholder="https://..."
                  />
                </label>
                <div className="menu-form__actions">
                  <button type="button" className="btn-secondary" onClick={resetForm}>
                    Hủy
                  </button>
                  <button id="save-menu-btn" type="submit" className="btn-primary" disabled={isPending}>
                    {isPending ? 'Đang lưu...' : editItem ? 'Cập nhật' : 'Thêm món'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}

        {/* Menu Grid */}
        {isLoading ? (
          <div className="menu-page__loading">
            <div className="spinner" />
          </div>
        ) : (
          <div className="menu-grid">
            {items.map((item) => (
              <div key={item.id} className={`menu-card ${!item.isAvailable ? 'menu-card--unavailable' : ''}`}>
                {item.imageUrl ? (
                  <img src={item.imageUrl} alt={item.name} className="menu-card__img" />
                ) : (
                  <div className="menu-card__img-placeholder">🍜</div>
                )}
                <div className="menu-card__body">
                  <h3 className="menu-card__name">{item.name}</h3>
                  {item.description && (
                    <p className="menu-card__desc">{item.description}</p>
                  )}
                  <div className="menu-card__footer">
                    <span className="menu-card__price">
                      {item.price.toLocaleString('vi-VN')}₫
                    </span>
                    <div className="menu-card__actions">
                      <button
                        className={`toggle-btn ${item.isAvailable ? 'toggle-btn--on' : 'toggle-btn--off'}`}
                        onClick={() =>
                          toggleAvailability.mutate({
                            id: item.id,
                            isAvailable: !item.isAvailable,
                          })
                        }
                        title={item.isAvailable ? 'Đang bán' : 'Hết hàng'}
                      >
                        {item.isAvailable ? '✅' : '🔴'}
                      </button>
                      <button
                        className="icon-btn"
                        onClick={() => handleEdit(item)}
                        title="Sửa"
                      >
                        ✏️
                      </button>
                      <button
                        className="icon-btn icon-btn--danger"
                        onClick={() => {
                          if (confirm(`Xóa "${item.name}"?`)) {
                            deleteMutation.mutate(item.id);
                          }
                        }}
                        title="Xóa"
                      >
                        🗑️
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
