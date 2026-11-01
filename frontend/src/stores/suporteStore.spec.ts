import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useSuporteStore } from './suporteStore'
import { suporteService } from '@/services/suporteService'

vi.mock('@/services/suporteService', () => ({
  suporteService: {
    enviar: vi.fn<typeof suporteService.enviar>(),
  },
}))

const mensagem = { assunto: 'Erro no relatório', mensagem: 'Não consigo gerar o relatório de março.' }

describe('useSuporteStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('enviar repassa assunto e mensagem ao service', async () => {
    vi.mocked(suporteService.enviar).mockResolvedValue()
    const store = useSuporteStore()

    await store.enviar(mensagem)

    expect(suporteService.enviar).toHaveBeenCalledWith(mensagem)
  })

  it('enviar propaga o erro do service (ex: 429 do limite por hora)', async () => {
    vi.mocked(suporteService.enviar).mockRejectedValue(new Error('429'))
    const store = useSuporteStore()

    await expect(store.enviar(mensagem)).rejects.toThrow('429')
  })
})
