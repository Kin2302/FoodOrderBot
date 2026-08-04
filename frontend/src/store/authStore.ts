import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthState {
  token: string | null;
  shopId: string | null;
  email: string | null;
  isAuthenticated: boolean;
  login: (token: string, shopId: string, email?: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      email: null,
      isAuthenticated: false,
      shopId: null,

      login: (token, shopId, email = '') =>
        set({ token, shopId, email, isAuthenticated: true }),

      logout: () =>
        set({ token: null, shopId: null, email: null, isAuthenticated: false }),
    }),
    {
      name: 'food-order-auth', // localStorage key
    }
  )
);
