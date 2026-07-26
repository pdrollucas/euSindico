import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { authService } from '@/services/authService'
import type { RegistrarRequest } from '@/types/auth.types'

// Sessão do usuário — onde e como o token vive está detalhado em
// frontend/documentation/AUTHENTICATION.md. O accessToken mora só em memória (nunca persistido);
// o refreshToken é um cookie HttpOnly, inteiramente invisível a este store.
export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null)
  const isAuthenticated = computed(() => accessToken.value !== null)
  // true assim que bootstrap() já foi tentado (com sucesso ou não) — evita chamar
  // POST /auth/refresh de novo a cada navegação para uma rota protegida na mesma sessão da SPA
  // (ver router/index.ts).
  const bootstrapped = ref(false)

  function setAccessToken(token: string) {
    accessToken.value = token
  }

  function clearSession() {
    accessToken.value = null
  }

  async function login(email: string, senha: string) {
    const { accessToken: token } = await authService.login({ email, senha })
    setAccessToken(token)
  }

  // Não altera a sessão (registrar não retorna tokens, só o UsuarioDto criado) — passa pelo
  // store mesmo assim para manter o fluxo de dependências view -> store -> service (ver
  // frontend/documentation/ARCHITECTURE.md, seção 3).
  async function registrar(payload: RegistrarRequest) {
    return authService.registrar(payload)
  }

  async function logout() {
    try {
      await authService.logout()
    } catch {
      // Independente do resultado da chamada — ver AUTHENTICATION.md, seção 6.
    } finally {
      clearSession()
    }
  }

  // Chamado pela guarda de rota (router/index.ts) na primeira navegação a uma rota protegida —
  // nunca em rotas públicas, para não gastar uma chamada a /auth/refresh à toa (ver
  // AUTHENTICATION.md, seção 4). `bootstrapped` marca que já foi tentado, com sucesso ou não,
  // pra não repetir a chamada a cada navegação subsequente na mesma sessão da SPA.
  async function bootstrap() {
    try {
      const { accessToken: token } = await authService.refresh()
      setAccessToken(token)
    } catch {
      clearSession()
    } finally {
      bootstrapped.value = true
    }
  }

  return {
    accessToken,
    isAuthenticated,
    bootstrapped,
    setAccessToken,
    clearSession,
    login,
    registrar,
    logout,
    bootstrap,
  }
})
