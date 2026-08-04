import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { facebookAuth, selectPage } from '../api/endpoints';
import { useAuthStore } from '../store/authStore';
import type { FacebookPage } from '../types';
import './LoginPage.css';

// ─── SVG Icons ───────────────────────────────────────────────────────────────
function IconBrandMark() {
  return (
    <svg width="48" height="48" viewBox="0 0 52 52" fill="none">
      <rect width="52" height="52" rx="14" fill="#10b981" />
      <path d="M13 18h26M13 26h18M13 34h12" stroke="#fff" strokeWidth="3" strokeLinecap="round" />
      <circle cx="40" cy="34" r="7" fill="#fff" fillOpacity="0.95" />
      <path d="M37.5 34l2 2 4.5-4.5" stroke="#10b981" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function IconAlert() {
  return (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="12" cy="12" r="10" />
      <line x1="12" y1="8" x2="12" y2="12" />
      <line x1="12" y1="16" x2="12.01" y2="16" />
    </svg>
  );
}

// ─── Decorative features for left panel ──────────────────────────────────────
const FEATURES = [
  { label: 'Quản lý đơn hàng realtime qua Kanban' },
  { label: 'Thống kê doanh thu & khiếu nại' },
  { label: 'Theo dõi trạng thái đơn cho khách hàng' },
];

// ─── JWT helper ───────────────────────────────────────────────────────────────
const decodeJwt = (token: string) => {
  const payload = token.split('.')[1];
  const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
  return JSON.parse(atob(base64));
};

function CheckIcon() {
  return (
    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="20 6 9 17 4 12" />
    </svg>
  );
}

// ─── Component ───────────────────────────────────────────────────────────────
export default function LoginPage() {

  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [pages, setPages] = useState<FacebookPage[]>([]);
  const [errorMsg, setErrorMsg] = useState('');

  const navigate = useNavigate();
  const setAuth = useAuthStore((s) => s.login);


  const { mutate: fetchPagesMutate, isPending: isFetchingPages } = useMutation({
        mutationFn: facebookAuth,
        onSuccess: (data) => {
          setPages(data);
          setStep(2);
        },
        onError: () => {
          setErrorMsg('Không lấy được danh sách page từ Facebook.');
        },
      });


  const { mutate: selectPageMutate, isPending: isSelectingPage } = useMutation({
    mutationFn: selectPage,
    onSuccess: (data, variables) => {
      const decoded = decodeJwt(data.token);
      setAuth(data.token, decoded.shopId, variables.pageName);
      navigate('/dashboard');
    },
    onError: () => {
      setErrorMsg('Không chọn được page.');
    },
  });

  const handleFacebookLogin = () => {
  setErrorMsg('');
  const FB = (window as any).FB;
  FB.login(
    (response: any) => {
      if (response.status === 'connected') {
        fetchPagesMutate(response.authResponse.accessToken);
      } else {
        setErrorMsg('Bạn chưa đăng nhập Facebook hoặc chưa đồng ý cho ứng dụng.');
      }
    },
    { scope: 'pages_show_list' }
  );
  };

  const handleSelectPage = (page: FacebookPage) => {
      setErrorMsg('');
      const FB = (window as any).FB;
      FB.api(
        '/me/accounts',
        { fields: 'id,name,picture,access_token' },
        (res: any) => {
          const matched = res.data.find((p: any) => p.id === page.pageId);
          if (matched?.access_token) {
            selectPageMutate({
              pageId: page.pageId,
              pageAccessToken: matched.access_token,
              pageName: page.pageName,
            });
          } else {
            setErrorMsg('Không lấy được token của page này.');
          }
        }
      );
    };



  return (
    <div className="login-page">
      {/* ── Left Panel — Brand ─────────────────────────────────────────────── */}
      <div className="login-left">
        {/* Mesh gradient background — fixed, pointer-events-none */}
        <div className="login-left__mesh" aria-hidden="true">
          <div className="mesh-blob mesh-blob--1" />
          <div className="mesh-blob mesh-blob--2" />
          <div className="mesh-blob mesh-blob--3" />
        </div>

        <div className="login-left__content">
          <div className="login-left__brand">
            <IconBrandMark />
            <span className="login-left__brand-name">FoodOrderBot</span>
          </div>

          <div className="login-left__headline">
            <h1 className="login-left__title">Quản lý<br />đơn hàng<br />thông minh.</h1>
            <p className="login-left__subtitle">
              Hệ thống tích hợp AI giúp xử lý đơn hàng Zalo nhanh hơn, chính xác hơn.
            </p>
          </div>

          <ul className="login-left__features">
            {FEATURES.map((f, i) => (
              <li key={i} className="login-left__feature" style={{ animationDelay: `${i * 80}ms` }}>
                <span className="login-left__feature-check">
                  <CheckIcon />
                </span>
                {f.label}
              </li>
            ))}
          </ul>
        </div>
      </div>

      {/* ── Right Panel — 3-step ──────────────────────────────────────────── */}
      <div className="login-right">
        <div className="login-form-container">
          {step === 1 && (
            <>
              <div className="login-form-header">
                <h2 className="login-form-title">Đăng nhập</h2>
                <p className="login-form-subtitle">Kết nối Facebook Page của quán để bắt đầu.</p>
              </div>

              {errorMsg && (
                <div className="login-form__error" role="alert">
                  <IconAlert />
                  {errorMsg}
                </div>
              )}

              <button
                type="button"
                className="login-form__submit"
                onClick={handleFacebookLogin}
                disabled={isFetchingPages}
              >
                {isFetchingPages ? (
                  <span className="login-form__spinner" />
                ) : (
                  'Đăng nhập bằng Facebook'
                )}
              </button>

              <p className="login-form__hint">
                Dùng tài khoản Facebook của chủ quán để quản lý đơn hàng.
              </p>
            </>
          )}

          {step === 2 && (
            <>
              <div className="login-form-header">
                <h2 className="login-form-title">Chọn Page quản lý</h2>
                <p className="login-form-subtitle">Chọn Facebook Page bạn muốn dùng làm shop.</p>
              </div>

              {errorMsg && (
                <div className="login-form__error" role="alert">
                  <IconAlert />
                  {errorMsg}
                </div>
              )}

              <div className="page-grid">
                {pages.map((page) => (
                  <button
                    key={page.pageId}
                    type="button"
                    className="page-card"
                    onClick={() => handleSelectPage(page)}
                    disabled={isSelectingPage}
                  >
                    {page.pictureUrl && (
                      <img src={page.pictureUrl} alt={page.pageName} className="page-card__avatar" />
                    )}
                    <span className="page-card__name">{page.pageName}</span>
                  </button>
                ))}
              </div>
            </>
          )}

          {step === 3 && (
            <div className="login-form-header">
              <h2 className="login-form-title">Đang vào dashboard...</h2>
              <div className="login-form__spinner" style={{ margin: '24px auto' }} />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
