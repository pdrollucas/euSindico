import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { usePerfilStore } from './perfilStore'
import { COOLDOWN_REENVIO_MS } from './recuperacaoSenhaStore'
import { perfilService } from '@/services/perfilService'

vi.mock('@/services/perfilService', () => ({
  perfilService: {
    obterPerfil: vi.fn<typeof perfilService.obterPerfil>(),
    atualizarPerfil: vi.fn<typeof perfilService.atualizarPerfil>(),
    solicitarTrocaEmail: vi.fn<typeof perfilService.solicitarTrocaEmail>(),
    confirmarTrocaEmail: vi.fn<typeof perfilService.confirmarTrocaEmail>(),
    alterarSenha: vi.fn<typeof perfilService.alterarSenha>(),
    excluirConta: vi.fn<typeof perfilService.excluirConta>(),
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

  afterEach(() => {
    vi.useRealTimers()
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

  it('atualizarNome envia o nome e guarda o perfil devolvido', async () => {
    vi.mocked(perfilService.atualizarPerfil).mockResolvedValue({ ...perfilFake, nome: 'Luciano Lima' })
    const store = usePerfilStore()

    await store.atualizarNome('Luciano Lima')

    expect(perfilService.atualizarPerfil).toHaveBeenCalledWith({ nome: 'Luciano Lima' })
    expect(store.perfil?.nome).toBe('Luciano Lima')
    expect(store.primeiroNome).toBe('Luciano')
  })

  describe('troca de e-mail', () => {
    it('solicitarTrocaEmail marca o pedido como pendente e inicia o cooldown', async () => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2026-11-01T12:00:00Z'))
      vi.mocked(perfilService.solicitarTrocaEmail).mockResolvedValue()
      const store = usePerfilStore()

      await store.solicitarTrocaEmail('novo@exemplo.com', 'Senha@123')

      expect(perfilService.solicitarTrocaEmail).toHaveBeenCalledWith({
        novoEmail: 'novo@exemplo.com',
        senhaAtual: 'Senha@123',
      })
      expect(store.novoEmailPendente).toBe('novo@exemplo.com')
      expect(store.cooldownExpiraEm).toBe(Date.now() + COOLDOWN_REENVIO_MS)
    })

    it('solicitarTrocaEmail com erro da API não avança nem inicia o cooldown', async () => {
      vi.mocked(perfilService.solicitarTrocaEmail).mockRejectedValue(new Error('400'))
      const store = usePerfilStore()

      await expect(store.solicitarTrocaEmail('novo@exemplo.com', 'errada')).rejects.toThrow('400')

      expect(store.novoEmailPendente).toBeNull()
      expect(store.cooldownExpiraEm).toBeNull()
    })

    it('confirmarTrocaEmail guarda o perfil novo e encerra o pedido pendente', async () => {
      vi.mocked(perfilService.solicitarTrocaEmail).mockResolvedValue()
      vi.mocked(perfilService.confirmarTrocaEmail).mockResolvedValue({
        ...perfilFake,
        email: 'novo@exemplo.com',
      })
      const store = usePerfilStore()
      await store.solicitarTrocaEmail('novo@exemplo.com', 'Senha@123')

      await store.confirmarTrocaEmail('ABC234')

      expect(perfilService.confirmarTrocaEmail).toHaveBeenCalledWith({ codigo: 'ABC234' })
      expect(store.perfil?.email).toBe('novo@exemplo.com')
      expect(store.novoEmailPendente).toBeNull()
      expect(store.cooldownExpiraEm).toBeNull()
    })

    it('confirmarTrocaEmail com código inválido mantém o pedido pendente', async () => {
      vi.mocked(perfilService.solicitarTrocaEmail).mockResolvedValue()
      vi.mocked(perfilService.confirmarTrocaEmail).mockRejectedValue(new Error('400'))
      const store = usePerfilStore()
      await store.solicitarTrocaEmail('novo@exemplo.com', 'Senha@123')

      await expect(store.confirmarTrocaEmail('ZZZZZZ')).rejects.toThrow('400')

      expect(store.novoEmailPendente).toBe('novo@exemplo.com')
    })

    it('cancelarTrocaEmail volta ao passo 1 sem mexer no cooldown', async () => {
      vi.mocked(perfilService.solicitarTrocaEmail).mockResolvedValue()
      const store = usePerfilStore()
      await store.solicitarTrocaEmail('novo@exemplo.com', 'Senha@123')
      const cooldown = store.cooldownExpiraEm

      store.cancelarTrocaEmail()

      expect(store.novoEmailPendente).toBeNull()
      expect(store.cooldownExpiraEm).toBe(cooldown)
    })

    it('limpar também descarta o pedido pendente e o cooldown', async () => {
      vi.mocked(perfilService.solicitarTrocaEmail).mockResolvedValue()
      const store = usePerfilStore()
      await store.solicitarTrocaEmail('novo@exemplo.com', 'Senha@123')

      store.limpar()

      expect(store.novoEmailPendente).toBeNull()
      expect(store.cooldownExpiraEm).toBeNull()
    })
  })

  it('alterarSenha repassa senha atual e nova ao service', async () => {
    vi.mocked(perfilService.alterarSenha).mockResolvedValue()
    const store = usePerfilStore()

    await store.alterarSenha({ senhaAtual: 'Antiga@123', novaSenha: 'Nova@12345' })

    expect(perfilService.alterarSenha).toHaveBeenCalledWith({
      senhaAtual: 'Antiga@123',
      novaSenha: 'Nova@12345',
    })
  })

  it('excluirConta repassa a senha ao service e propaga o erro', async () => {
    vi.mocked(perfilService.excluirConta).mockRejectedValue(new Error('400'))
    const store = usePerfilStore()

    await expect(store.excluirConta('errada')).rejects.toThrow('400')

    expect(perfilService.excluirConta).toHaveBeenCalledWith({ senha: 'errada' })
  })
})
