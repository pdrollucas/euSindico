import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { nextTick } from 'vue'
import { setActivePinia, createPinia } from 'pinia'
import { useRecuperacaoSenhaStore, COOLDOWN_REENVIO_MS } from './recuperacaoSenhaStore'
import { authService } from '@/services/authService'

vi.mock('@/services/authService', () => ({
  authService: {
    esqueciSenha: vi.fn<typeof authService.esqueciSenha>(),
    verificarCodigo: vi.fn<typeof authService.verificarCodigo>(),
    redefinirSenha: vi.fn<typeof authService.redefinirSenha>(),
  },
}))

describe('useRecuperacaoSenhaStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    sessionStorage.clear()
    vi.clearAllMocks()
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('solicitarCodigo guarda o e-mail e inicia o cooldown de 2 minutos', async () => {
    vi.setSystemTime(new Date('2026-07-27T12:00:00Z'))
    vi.mocked(authService.esqueciSenha).mockResolvedValue(undefined)
    const store = useRecuperacaoSenhaStore()

    await store.solicitarCodigo('joao@exemplo.com')

    expect(authService.esqueciSenha).toHaveBeenCalledWith({ email: 'joao@exemplo.com' })
    expect(store.email).toBe('joao@exemplo.com')
    expect(store.cooldownExpiraEm).toBe(Date.now() + COOLDOWN_REENVIO_MS)
  })

  it('não inicia cooldown nem guarda e-mail se a solicitação falhar (ex: 429)', async () => {
    vi.mocked(authService.esqueciSenha).mockRejectedValue(new Error('429'))
    const store = useRecuperacaoSenhaStore()

    await expect(store.solicitarCodigo('joao@exemplo.com')).rejects.toThrow(Error)
    expect(store.email).toBeNull()
    expect(store.cooldownExpiraEm).toBeNull()
  })

  it('verificar guarda o código quando a API confirma', async () => {
    vi.mocked(authService.esqueciSenha).mockResolvedValue(undefined)
    vi.mocked(authService.verificarCodigo).mockResolvedValue(undefined)
    const store = useRecuperacaoSenhaStore()
    await store.solicitarCodigo('joao@exemplo.com')

    await store.verificar('AB12CD')

    expect(authService.verificarCodigo).toHaveBeenCalledWith({
      email: 'joao@exemplo.com',
      codigo: 'AB12CD',
    })
    expect(store.codigo).toBe('AB12CD')
  })

  it('verificar não guarda o código quando a API rejeita (400)', async () => {
    vi.mocked(authService.esqueciSenha).mockResolvedValue(undefined)
    vi.mocked(authService.verificarCodigo).mockRejectedValue(new Error('400'))
    const store = useRecuperacaoSenhaStore()
    await store.solicitarCodigo('joao@exemplo.com')

    await expect(store.verificar('WRONG1')).rejects.toThrow(Error)
    expect(store.codigo).toBeNull()
  })

  it('verificar sem e-mail no fluxo lança erro e não chama a API', async () => {
    const store = useRecuperacaoSenhaStore()

    await expect(store.verificar('AB12CD')).rejects.toThrow('Fluxo de recuperação sem e-mail definido')
    expect(authService.verificarCodigo).not.toHaveBeenCalled()
  })

  it('redefinir envia e-mail+código do fluxo e limpa a store no sucesso', async () => {
    vi.mocked(authService.esqueciSenha).mockResolvedValue(undefined)
    vi.mocked(authService.verificarCodigo).mockResolvedValue(undefined)
    vi.mocked(authService.redefinirSenha).mockResolvedValue(undefined)
    const store = useRecuperacaoSenhaStore()
    await store.solicitarCodigo('joao@exemplo.com')
    await store.verificar('AB12CD')

    await store.redefinir('NovaSenha1!', 'NovaSenha1!')

    expect(authService.redefinirSenha).toHaveBeenCalledWith({
      email: 'joao@exemplo.com',
      codigo: 'AB12CD',
      novaSenha: 'NovaSenha1!',
      confirmarSenha: 'NovaSenha1!',
    })
    expect(store.email).toBeNull()
    expect(store.codigo).toBeNull()
    expect(store.cooldownExpiraEm).toBeNull()
  })

  it('persiste e-mail e cooldown no sessionStorage, mas nunca o código (segredo)', async () => {
    vi.mocked(authService.esqueciSenha).mockResolvedValue(undefined)
    vi.mocked(authService.verificarCodigo).mockResolvedValue(undefined)
    const store = useRecuperacaoSenhaStore()

    await store.solicitarCodigo('joao@exemplo.com')
    await store.verificar('AB12CD')
    await nextTick()

    const bruto = sessionStorage.getItem('recuperacaoSenha')
    expect(bruto).not.toBeNull()
    const dados = JSON.parse(bruto as string)
    expect(dados.email).toBe('joao@exemplo.com')
    expect(typeof dados.cooldownExpiraEm).toBe('number')
    // O código verificado nunca vai para o storage.
    expect(dados).not.toHaveProperty('codigo')
    expect(bruto).not.toContain('AB12CD')
  })

  it('hidrata e-mail e cooldown de uma sessão anterior (sobrevive ao reload/F5)', async () => {
    vi.mocked(authService.esqueciSenha).mockResolvedValue(undefined)
    const store1 = useRecuperacaoSenhaStore()
    await store1.solicitarCodigo('joao@exemplo.com')
    await nextTick()

    // Simula o F5: novo Pinia + novo store, lendo o sessionStorage que sobreviveu.
    setActivePinia(createPinia())
    const store2 = useRecuperacaoSenhaStore()

    expect(store2.email).toBe('joao@exemplo.com')
    expect(store2.cooldownExpiraEm).toBe(store1.cooldownExpiraEm)
    expect(store2.codigo).toBeNull() // o código não é reidratado
  })

  it('limpar remove o estado persistido do sessionStorage', async () => {
    vi.mocked(authService.esqueciSenha).mockResolvedValue(undefined)
    const store = useRecuperacaoSenhaStore()
    await store.solicitarCodigo('joao@exemplo.com')
    await nextTick()

    store.limpar()
    await nextTick()

    expect(sessionStorage.getItem('recuperacaoSenha')).toBeNull()
  })
})
