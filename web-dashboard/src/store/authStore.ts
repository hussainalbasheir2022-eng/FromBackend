import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { authApi } from '../api/client'

interface AuthState {
  accessToken: string | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      isAuthenticated: false,

      login: async (email, password) => {
        const { data } = await authApi.login(email, password)
        localStorage.setItem('accessToken', data.accessToken)
        set({ accessToken: data.accessToken, isAuthenticated: true })
      },

      logout: () => {
        localStorage.removeItem('accessToken')
        set({ accessToken: null, isAuthenticated: false })
      },
    }),
    { name: 'auth-store' }
  )
)
