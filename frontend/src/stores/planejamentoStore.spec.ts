import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { usePlanejamentoStore } from './planejamentoStore'
import { planejamentoService } from '@/services/planejamentoService'
import type { Planejamento } from '@/types/planejamento.types'

vi.mock('@/services/planejamentoService', () => ({
  planejamentoService: {
    listar: vi.fn<typeof planejamentoService.listar>(),
    obter: vi.fn<typeof planejamentoService.obter>(),
    criar: vi.fn<typeof planejamentoService.criar>(),
    atualizar: vi.fn<typeof planejamentoService.atualizar>(),
    remover: vi.fn<typeof planejamentoService.remover>(),
  },
}))

const pintura: Planejamento = {
  id: 1,
  titulo: 'Pintura',
  dataPlanejamento: '2026-12-01',
  orcamentoPrevisto: 1500,
  nomePrestadorServico: null,
  detalhes: null,
  predioId: 4,
  predioNome: 'Edifício Solar',
  criadoEm: '2026-10-01T12:00:00Z',
}
const portao = { ...pintura, id: 2, titulo: 'Trocar o portão', dataPlanejamento: null }

function pagina(items: Planejamento[], page = 1, totalPages = 1) {
  return { items, page, pageSize: 20, totalCount: items.length, totalPages }
}

describe('usePlanejamentoStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('começa vazio e sem filtros', () => {
    const store = usePlanejamentoStore()
    expect(store.planejamentos).toEqual([])
    expect(store.planejamentoAtual).toBeNull()
    expect(store.filtros).toEqual({ titulo: '', orcamentoMinimo: null, orcamentoMaximo: null })
  })

  it('listarPrimeiraPagina envia o prédio e os filtros atuais e popula a lista', async () => {
    vi.mocked(planejamentoService.listar).mockResolvedValue(pagina([pintura]))
    const store = usePlanejamentoStore()
    store.filtros.orcamentoMinimo = 100

    await store.listarPrimeiraPagina(4)

    expect(planejamentoService.listar).toHaveBeenCalledWith(4, store.filtros, 1, 20)
    expect(store.planejamentos).toEqual([pintura])
    expect(store.temMaisPaginas).toBe(false)
  })

  it('carregarProximaPagina concatena os itens da página seguinte', async () => {
    vi.mocked(planejamentoService.listar).mockResolvedValueOnce(pagina([pintura], 1, 2))
    vi.mocked(planejamentoService.listar).mockResolvedValueOnce(pagina([portao], 2, 2))
    const store = usePlanejamentoStore()
    await store.listarPrimeiraPagina(4)
    expect(store.temMaisPaginas).toBe(true)

    await store.carregarProximaPagina(4)

    expect(planejamentoService.listar).toHaveBeenLastCalledWith(4, store.filtros, 2, 20)
    expect(store.planejamentos).toEqual([pintura, portao])
    expect(store.temMaisPaginas).toBe(false)
  })

  it('carregarProximaPagina não faz nada sem mais páginas', async () => {
    vi.mocked(planejamentoService.listar).mockResolvedValue(pagina([pintura]))
    const store = usePlanejamentoStore()
    await store.listarPrimeiraPagina(4)

    await store.carregarProximaPagina(4)

    expect(planejamentoService.listar).toHaveBeenCalledTimes(1)
  })

  it('descarta a resposta antiga quando uma busca mais nova termina antes', async () => {
    let resolverAntiga!: (valor: ReturnType<typeof pagina>) => void
    vi.mocked(planejamentoService.listar).mockImplementationOnce(
      () => new Promise((resolve) => (resolverAntiga = resolve)),
    )
    vi.mocked(planejamentoService.listar).mockResolvedValueOnce(pagina([portao]))
    const store = usePlanejamentoStore()

    const antiga = store.listarPrimeiraPagina(4)
    await store.listarPrimeiraPagina(4)
    resolverAntiga(pagina([pintura]))
    await antiga

    expect(store.planejamentos).toEqual([portao])
  })

  it('obter guarda o planejamento como planejamentoAtual', async () => {
    vi.mocked(planejamentoService.obter).mockResolvedValue(pintura)
    const store = usePlanejamentoStore()

    const resultado = await store.obter(4, 1)

    expect(planejamentoService.obter).toHaveBeenCalledWith(4, 1)
    expect(resultado).toEqual(pintura)
    expect(store.planejamentoAtual).toEqual(pintura)
  })

  it('criar delega ao service sem mexer na lista (a view volta e recarrega)', async () => {
    vi.mocked(planejamentoService.criar).mockResolvedValue(pintura)
    const store = usePlanejamentoStore()

    await store.criar(4, { titulo: 'Pintura' })

    expect(planejamentoService.criar).toHaveBeenCalledWith(4, { titulo: 'Pintura' })
    expect(store.planejamentos).toEqual([])
  })

  it('atualizar substitui o item na lista e no planejamentoAtual', async () => {
    vi.mocked(planejamentoService.listar).mockResolvedValue(pagina([pintura, portao]))
    const atualizado = { ...pintura, titulo: 'Pintura geral' }
    vi.mocked(planejamentoService.atualizar).mockResolvedValue(atualizado)
    vi.mocked(planejamentoService.obter).mockResolvedValue(pintura)
    const store = usePlanejamentoStore()
    await store.listarPrimeiraPagina(4)
    await store.obter(4, 1)

    await store.atualizar(4, 1, { titulo: 'Pintura geral' })

    expect(store.planejamentos).toEqual([atualizado, portao])
    expect(store.planejamentoAtual).toEqual(atualizado)
  })

  it('remover apaga no backend, tira da lista e limpa o planejamentoAtual quando é o mesmo', async () => {
    vi.mocked(planejamentoService.listar).mockResolvedValue(pagina([pintura, portao]))
    vi.mocked(planejamentoService.obter).mockResolvedValue(pintura)
    vi.mocked(planejamentoService.remover).mockResolvedValue(undefined)
    const store = usePlanejamentoStore()
    await store.listarPrimeiraPagina(4)
    await store.obter(4, 1)

    await store.remover(4, 1)

    expect(planejamentoService.remover).toHaveBeenCalledWith(4, 1)
    expect(store.planejamentos).toEqual([portao])
    expect(store.planejamentoAtual).toBeNull()
  })

  it('remover mantém o planejamentoAtual quando o removido é outro', async () => {
    vi.mocked(planejamentoService.obter).mockResolvedValue(pintura)
    vi.mocked(planejamentoService.remover).mockResolvedValue(undefined)
    const store = usePlanejamentoStore()
    await store.obter(4, 1)

    await store.remover(4, 2)

    expect(store.planejamentoAtual).toEqual(pintura)
  })

  it('resetarFiltros volta ao padrão', () => {
    const store = usePlanejamentoStore()
    store.filtros.titulo = 'portão'
    store.filtros.orcamentoMinimo = 10
    store.filtros.orcamentoMaximo = 20

    store.resetarFiltros()

    expect(store.filtros).toEqual({ titulo: '', orcamentoMinimo: null, orcamentoMaximo: null })
  })

  it('limparAtual zera o planejamentoAtual', async () => {
    vi.mocked(planejamentoService.obter).mockResolvedValue(pintura)
    const store = usePlanejamentoStore()
    await store.obter(4, 1)

    store.limparAtual()

    expect(store.planejamentoAtual).toBeNull()
  })
})
