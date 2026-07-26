import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from './authStore'
import { authService } from '@/services/authService'

vi.mock('@/services/authService', () => ({
  authService: {
    login: vi.fn<typeof authService.login>(),
    registrar: vi.fn<typeof authService.registrar>(),
    refresh: vi.fn<typeof authService.refresh>(),
    logout: vi.fn<typeof authService.logout>(),
  },
}))

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('começa não autenticado', () => {
    const store = useAuthStore()
    expect(store.isAuthenticated).toBe(false)
  })

  it('fica autenticado após login', async () => {
    vi.mocked(authService.login).mockResolvedValue({ accessToken: 'token-123' })
    const store = useAuthStore()

    await store.login('sindico@exemplo.com', 'SenhaForte1!')

    expect(store.isAuthenticated).toBe(true)
    expect(store.accessToken).toBe('token-123')
  })

  it('registrar não altera a sessão (só cria a conta, não autentica)', async () => {
    const usuarioCriado = {
      id: 1,
      nome: 'João da Silva',
      email: 'joao@exemplo.com',
      criadoEm: '2026-01-01T12:00:00Z',
    }
    vi.mocked(authService.registrar).mockResolvedValue(usuarioCriado)
    const store = useAuthStore()

    const resultado = await store.registrar({
      nome: 'João da Silva',
      email: 'joao@exemplo.com',
      senha: 'SenhaForte1!',
    })

    expect(resultado).toEqual(usuarioCriado)
    expect(store.isAuthenticated).toBe(false)
  })

  it('limpa a sessão no logout mesmo se a chamada ao backend falhar', async () => {
    vi.mocked(authService.login).mockResolvedValue({ accessToken: 'token-123' })
    vi.mocked(authService.logout).mockRejectedValue(new Error('network error'))
    const store = useAuthStore()
    await store.login('sindico@exemplo.com', 'SenhaForte1!')

    await expect(store.logout()).resolves.toBeUndefined()

    expect(store.isAuthenticated).toBe(false)
    expect(store.accessToken).toBeNull()
  })

  it('bootstrap autentica quando existe cookie de refresh válido', async () => {
    vi.mocked(authService.refresh).mockResolvedValue({ accessToken: 'token-456' })
    const store = useAuthStore()

    await store.bootstrap()

    expect(store.isAuthenticated).toBe(true)
  })

  it('bootstrap mantém não-autenticado quando não há sessão (refresh falha)', async () => {
    vi.mocked(authService.refresh).mockRejectedValue(new Error('401'))
    const store = useAuthStore()

    await store.bootstrap()

    expect(store.isAuthenticated).toBe(false)
  })

  it('marca bootstrapped mesmo quando o refresh falha (evita repetir a chamada à toa)', async () => {
    vi.mocked(authService.refresh).mockRejectedValue(new Error('401'))
    const store = useAuthStore()

    expect(store.bootstrapped).toBe(false)
    await store.bootstrap()

    expect(store.bootstrapped).toBe(true)
  })
})
