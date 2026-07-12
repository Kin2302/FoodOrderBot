import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { login } from '../api/endpoints';
import { useAuthStore } from '../store/authStore';
import './LoginPage.css';

export default function LoginPage() {
  const navigate = useNavigate();
  const setAuth = useAuthStore((s) => s.login);
  const [email, setEmail] = useState('admin@foodorderbot.com');
  const [password, setPassword] = useState('');
  const [errorMsg, setErrorMsg] = useState('');

  const { mutate, isPending } = useMutation({
    mutationFn: login,
    onSuccess: (data) => {
      setAuth(data.token, data.email);
      navigate('/dashboard');
    },
    onError: () => {
      setErrorMsg('Email hoặc mật khẩu không đúng.');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg('');
    mutate({ email, password });
  };

  return (
    <div className="login-page">
      {/* Background decoration */}
      <div className="login-page__bg">
        <div className="login-page__blob login-page__blob--1" />
        <div className="login-page__blob login-page__blob--2" />
        <div className="login-page__blob login-page__blob--3" />
      </div>

      <div className="login-card">
        <div className="login-card__logo">
          <span className="login-card__logo-icon">🤖</span>
          <h1 className="login-card__logo-title">FoodOrderBot</h1>
          <p className="login-card__logo-sub">Hệ thống quản lý đơn hàng thông minh</p>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          <div className="login-form__group">
            <label htmlFor="email" className="login-form__label">Email</label>
            <input
              id="email"
              type="email"
              className="login-form__input"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
              placeholder="admin@foodorderbot.com"
            />
          </div>

          <div className="login-form__group">
            <label htmlFor="password" className="login-form__label">Mật khẩu</label>
            <input
              id="password"
              type="password"
              className="login-form__input"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="current-password"
              placeholder="••••••••"
            />
          </div>

          {errorMsg && (
            <div className="login-form__error" role="alert">
              ⚠️ {errorMsg}
            </div>
          )}

          <button
            id="login-btn"
            type="submit"
            className="login-form__submit"
            disabled={isPending}
          >
            {isPending ? (
              <span className="login-form__spinner" />
            ) : (
              'Đăng nhập'
            )}
          </button>
        </form>

        <p className="login-card__hint">
          Default: <code>Admin@123!</code>
        </p>
      </div>
    </div>
  );
}
