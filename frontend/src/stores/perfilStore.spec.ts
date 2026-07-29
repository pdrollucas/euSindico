import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { usePerfilStore } from './perfilStore'
import { perfilService } from '@/services/perfilService'

vi.mock('@/services/perfilService', () => ({
  perfilService: {
    obterPerfil: vi.fn<typeof perfilService.obterPerfil>(),
  },
}))

const perfilFake = {
  id: 1,
  nome: 'Luciano Souza',
  email: 'luciano@exemplo.com',
  criadoEm: '2026-01-01T12:00:00Z',
}

describe('usePerfilStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('começa sem perfil e sem primeiro nome', () => {
    const store = usePerfilStore()
    expect(store.perfil).toBeNull()
    expect(store.primeiroNome).toBeNull()
  })

  it('carregar busca o perfil e expõe o primeiro nome', async () => {
    vi.mocked(perfilService.obterPerfil).mockResolvedValue(perfilFake)
    const store = usePerfilStore()

    await store.carregar()

    expect(store.perfil?.nome).toBe('Luciano Souza')
    expect(store.primeiroNome).toBe('Luciano')
  })

  it('limpar remove o perfil (usado no logout)', async () => {
    vi.mocked(perfilService.obterPerfil).mockResolvedValue(perfilFake)
    const store = usePerfilStore()
    await store.carregar()

    store.limpar()

    expect(store.perfil).toBeNull()
    expect(store.primeiroNome).toBeNull()
  })
})
