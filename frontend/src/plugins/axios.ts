import axios, { type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from '@/stores/authStore'

interface RetriableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean
}

// Endpoints públicos de /auth/* (sem [Authorize] no AuthController do backend) — um 401 aqui
// significa "credenciais/código inválidos", não "sessão expirada". Nunca deve disparar uma
// tentativa de refresh: não há sessão para renovar, e reenviar a MESMA requisição original com
// um novo access token não muda o motivo do 401 (ex: senha errada continua errada). Só
// /auth/logout fica de fora desta lista — é o único endpoint de /auth/* protegido por
// [Authorize] no backend, então um 401 ali pode legitimamente significar token expirado.
const PUBLIC_AUTH_PATHS = [
  '/auth/login',
  '/auth/refresh',
  '/auth/registrar',
  '/auth/esqueci-senha',
  '/auth/verificar-codigo',
  '/auth/redefinir-senha',
]

// Instância única do Axios — ver frontend/documentation/ARCHITECTURE.md, seção 6, e
// frontend/documentation/AUTHENTICATION.md, seção 5.
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  // Necessário para o cookie HttpOnly do refresh token trafegar (ver AUTHENTICATION.md, seção 1).
  withCredentials: true,
})

api.interceptors.request.use((config) => {
  const authStore = useAuthStore()
  if (authStore.accessToken) {
    config.headers.Authorization = `Bearer ${authStore.accessToken}`
  }
  return config
})

// Deduplica renovações concorrentes: se várias requisições caírem em 401 ao mesmo tempo,
// só a primeira chama /auth/refresh — as demais aguardam a mesma promise.
let refreshPromise: Promise<string | null> | null = null

async function renovarAccessToken(): Promise<string | null> {
  const authStore = useAuthStore()
  try {
    const { data } = await api.post<{ accessToken: string }>('/auth/refresh')
    authStore.setAccessToken(data.accessToken)
    return data.accessToken
  } catch {
    authStore.clearSession()
    return null
  }
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as RetriableRequestConfig | undefined
    const status = error.response?.status
    const isPublicAuthCall = PUBLIC_AUTH_PATHS.some((path) => originalRequest?.url?.includes(path))

    if (status !== 401 || !originalRequest || isPublicAuthCall || originalRequest._retry) {
      return Promise.reject(error)
    }

    originalRequest._retry = true
    refreshPromise ??= renovarAccessToken().finally(() => {
      refreshPromise = null
    })

    const newAccessToken = await refreshPromise
    if (!newAccessToken) {
      return Promise.reject(error)
    }

    originalRequest.headers.Authorization = `Bearer ${newAccessToken}`
    return api(originalRequest)
  },
)

export default api
