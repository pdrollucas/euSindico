import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useRelatorioStore } from './relatorioStore'
import { relatorioService } from '@/services/relatorioService'
import type { Relatorio } from '@/types/relatorio.types'

vi.mock('@/services/relatorioService', () => ({
  relatorioService: {
    listar: vi.fn<typeof relatorioService.listar>(),
    gerar: vi.fn<typeof relatorioService.gerar>(),
    baixar: vi.fn<typeof relatorioService.baixar>(),
    remover: vi.fn<typeof relatorioService.remover>(),
  },
}))

const setembro: Relatorio = {
  id: 1,
  mesReferencia: 9,
  anoReferencia: 2026,
  nomeArquivo: 'Edificio-Solar_Relatorio_2026-09.pdf',
  predioId: 4,
  predioNome: 'Edifício Solar',
  geradoEm: '2026-10-01T12:00:00Z',
}
const agosto = { ...setembro, id: 2, mesReferencia: 8, nomeArquivo: 'Edificio-Solar_Relatorio_2026-08.pdf' }

function pagina(items: Relatorio[], page = 1, totalPages = 1) {
  return { items, page, pageSize: 20, totalCount: items.length, totalPages }
}

describe('useRelatorioStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('começa vazio', () => {
    const store = useRelatorioStore()

    expect(store.relatorios).toEqual([])
    expect(store.temMaisPaginas).toBe(false)
  })

  it('listarPrimeiraPagina envia o prédio e popula a lista', async () => {
    vi.mocked(relatorioService.listar).mockResolvedValue(pagina([setembro]))
    const store = useRelatorioStore()

    await store.listarPrimeiraPagina(4)

    expect(relatorioService.listar).toHaveBeenCalledWith(4, 1, 20)
    expect(store.relatorios).toEqual([setembro])
    expect(store.temMaisPaginas).toBe(false)
  })

  it('carregarProximaPagina concatena os itens da página seguinte', async () => {
    vi.mocked(relatorioService.listar).mockResolvedValueOnce(pagina([setembro], 1, 2))
    vi.mocked(relatorioService.listar).mockResolvedValueOnce(pagina([agosto], 2, 2))
    const store = useRelatorioStore()
    await store.listarPrimeiraPagina(4)
    expect(store.temMaisPaginas).toBe(true)

    await store.carregarProximaPagina(4)

    expect(relatorioService.listar).toHaveBeenLastCalledWith(4, 2, 20)
    expect(store.relatorios).toEqual([setembro, agosto])
    expect(store.temMaisPaginas).toBe(false)
  })

  it('carregarProximaPagina não faz nada na última página', async () => {
    vi.mocked(relatorioService.listar).mockResolvedValue(pagina([setembro]))
    const store = useRelatorioStore()
    await store.listarPrimeiraPagina(4)
    vi.mocked(relatorioService.listar).mockClear()

    await store.carregarProximaPagina(4)

    expect(relatorioService.listar).not.toHaveBeenCalled()
  })

  it('ignora a resposta antiga que chega depois de uma requisição mais nova', async () => {
    let resolverAntiga: (valor: ReturnType<typeof pagina>) => void = () => undefined
    vi.mocked(relatorioService.listar).mockReturnValueOnce(
      new Promise((resolve) => {
        resolverAntiga = resolve
      }),
    )
    vi.mocked(relatorioService.listar).mockResolvedValueOnce(pagina([setembro]))
    const store = useRelatorioStore()

    const antiga = store.listarPrimeiraPagina(4)
    await store.listarPrimeiraPagina(4)
    resolverAntiga(pagina([agosto]))
    await antiga

    expect(store.relatorios).toEqual([setembro])
  })

  it('gerar chama o service e recarrega a primeira página (a ordem continua a do backend)', async () => {
    vi.mocked(relatorioService.gerar).mockResolvedValue(setembro)
    vi.mocked(relatorioService.listar).mockResolvedValue(pagina([setembro, agosto]))
    const store = useRelatorioStore()

    const resultado = await store.gerar(4, { mes: 9, ano: 2026 })

    expect(relatorioService.gerar).toHaveBeenCalledWith(4, { mes: 9, ano: 2026 })
    expect(relatorioService.listar).toHaveBeenCalledWith(4, 1, 20)
    expect(resultado).toEqual(setembro)
    expect(store.relatorios).toEqual([setembro, agosto])
  })

  it('gerar com erro (ex: limite atingido) propaga e não recarrega a lista', async () => {
    vi.mocked(relatorioService.gerar).mockRejectedValue(new Error('409'))
    const store = useRelatorioStore()

    await expect(store.gerar(4, { mes: 9, ano: 2026 })).rejects.toThrow('409')

    expect(relatorioService.listar).not.toHaveBeenCalled()
  })

  it('baixar devolve o arquivo do service sem mexer na lista', async () => {
    const arquivo = new Blob(['%PDF'], { type: 'application/pdf' })
    vi.mocked(relatorioService.baixar).mockResolvedValue(arquivo)
    const store = useRelatorioStore()

    expect(await store.baixar(4, 1)).toBe(arquivo)
    expect(relatorioService.baixar).toHaveBeenCalledWith(4, 1)
  })

  it('remover tira o item da lista só depois de o backend confirmar', async () => {
    vi.mocked(relatorioService.listar).mockResolvedValue(pagina([setembro, agosto]))
    vi.mocked(relatorioService.remover).mockResolvedValue()
    const store = useRelatorioStore()
    await store.listarPrimeiraPagina(4)

    await store.remover(4, 1)

    expect(relatorioService.remover).toHaveBeenCalledWith(4, 1)
    expect(store.relatorios).toEqual([agosto])
  })

  it('remover com erro mantém o item na lista', async () => {
    vi.mocked(relatorioService.listar).mockResolvedValue(pagina([setembro]))
    vi.mocked(relatorioService.remover).mockRejectedValue(new Error('403'))
    const store = useRelatorioStore()
    await store.listarPrimeiraPagina(4)

    await expect(store.remover(4, 1)).rejects.toThrow('403')

    expect(store.relatorios).toEqual([setembro])
  })
})
