import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useCompromissoStore } from './compromissoStore'
import { compromissoService } from '@/services/compromissoService'

vi.mock('@/services/compromissoService', () => ({
  compromissoService: {
    listar: vi.fn<typeof compromissoService.listar>(),
    obter: vi.fn<typeof compromissoService.obter>(),
    criar: vi.fn<typeof compromissoService.criar>(),
    atualizar: vi.fn<typeof compromissoService.atualizar>(),
    alterarConclusao: vi.fn<typeof compromissoService.alterarConclusao>(),
    remover: vi.fn<typeof compromissoService.remover>(),
  },
}))

const reuniao = {
  id: 1,
  titulo: 'Reunião',
  dataCompromisso: '2026-10-20',
  horarioCompromisso: '18:30:00',
  nomePrestadorServico: null,
  detalhes: null,
  orcamento: null,
  concluido: false,
  predioId: 1,
  predioNome: 'Edifício Solar',
  responsavelUsuarioId: 2,
  responsavelNome: 'Luciano Souza',
  criadoEm: '2026-10-01T12:00:00Z',
}
const portao = { ...reuniao, id: 2, titulo: 'Conserto de portão', horarioCompromisso: '18:00:00' }

function pagina(items: (typeof reuniao)[], page = 1, totalPages = 1) {
  return { items, page, pageSize: 20, totalCount: items.length, totalPages }
}

describe('useCompromissoStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('começa vazio, com os filtros padrão do backend (próximos, meus)', () => {
    const store = useCompromissoStore()
    expect(store.compromissos).toEqual([])
    expect(store.compromissoAtual).toBeNull()
    expect(store.filtros).toEqual({ titulo: '', situacao: 'proximos', escopo: 'meus', predioId: null })
  })

  it('listarPrimeiraPagina envia os filtros atuais e popula a lista', async () => {
    vi.mocked(compromissoService.listar).mockResolvedValue(pagina([reuniao]))
    const store = useCompromissoStore()
    store.filtros.situacao = 'hoje'
    store.filtros.predioId = 1

    await store.listarPrimeiraPagina()

    expect(compromissoService.listar).toHaveBeenCalledWith(store.filtros, 1, 20)
    expect(store.compromissos).toEqual([reuniao])
    expect(store.temMaisPaginas).toBe(false)
  })

  it('carregarProximaPagina concatena os itens da página seguinte', async () => {
    vi.mocked(compromissoService.listar).mockResolvedValueOnce(pagina([reuniao], 1, 2))
    vi.mocked(compromissoService.listar).mockResolvedValueOnce(pagina([portao], 2, 2))
    const store = useCompromissoStore()
    await store.listarPrimeiraPagina()
    expect(store.temMaisPaginas).toBe(true)

    await store.carregarProximaPagina()

    expect(store.compromissos).toEqual([reuniao, portao])
    expect(store.temMaisPaginas).toBe(false)
  })

  it('carregarProximaPagina não faz nada sem mais páginas', async () => {
    vi.mocked(compromissoService.listar).mockResolvedValue(pagina([reuniao]))
    const store = useCompromissoStore()
    await store.listarPrimeiraPagina()

    await store.carregarProximaPagina()

    expect(compromissoService.listar).toHaveBeenCalledTimes(1)
  })

  it('descarta a resposta antiga quando uma busca mais nova termina antes', async () => {
    let resolverAntiga!: (valor: ReturnType<typeof pagina>) => void
    vi.mocked(compromissoService.listar).mockImplementationOnce(
      () => new Promise((resolve) => (resolverAntiga = resolve)),
    )
    vi.mocked(compromissoService.listar).mockResolvedValueOnce(pagina([portao]))
    const store = useCompromissoStore()

    const antiga = store.listarPrimeiraPagina()
    await store.listarPrimeiraPagina()
    resolverAntiga(pagina([reuniao]))
    await antiga

    expect(store.compromissos).toEqual([portao])
  })

  it('obter guarda o compromisso como compromissoAtual', async () => {
    vi.mocked(compromissoService.obter).mockResolvedValue(reuniao)
    const store = useCompromissoStore()

    const resultado = await store.obter(1)

    expect(resultado).toEqual(reuniao)
    expect(store.compromissoAtual).toEqual(reuniao)
  })

  it('criar delega ao service sem mexer na lista (a view volta e recarrega)', async () => {
    vi.mocked(compromissoService.criar).mockResolvedValue(reuniao)
    const store = useCompromissoStore()

    const payload = {
      predioId: 1,
      titulo: 'Reunião',
      dataCompromisso: '2026-10-20',
      horarioCompromisso: '18:30',
    }
    await store.criar(payload)

    expect(compromissoService.criar).toHaveBeenCalledWith(payload)
    expect(store.compromissos).toEqual([])
  })

  it('atualizar substitui o item na lista e no compromissoAtual', async () => {
    vi.mocked(compromissoService.listar).mockResolvedValue(pagina([reuniao, portao]))
    const atualizado = { ...reuniao, titulo: 'Reunião extraordinária' }
    vi.mocked(compromissoService.atualizar).mockResolvedValue(atualizado)
    vi.mocked(compromissoService.obter).mockResolvedValue(reuniao)
    const store = useCompromissoStore()
    await store.listarPrimeiraPagina()
    await store.obter(1)

    await store.atualizar(1, {
      predioId: 1,
      titulo: 'Reunião extraordinária',
      dataCompromisso: '2026-10-20',
      horarioCompromisso: '18:30',
    })

    expect(store.compromissos).toEqual([atualizado, portao])
    expect(store.compromissoAtual).toEqual(atualizado)
  })

  it('alterarConclusao atualiza o item na lista com a resposta do PATCH', async () => {
    vi.mocked(compromissoService.listar).mockResolvedValue(pagina([reuniao]))
    const concluido = { ...reuniao, concluido: true }
    vi.mocked(compromissoService.alterarConclusao).mockResolvedValue(concluido)
    const store = useCompromissoStore()
    await store.listarPrimeiraPagina()

    await store.alterarConclusao(1, true)

    expect(compromissoService.alterarConclusao).toHaveBeenCalledWith(1, true)
    expect(store.compromissos).toEqual([concluido])
  })

  it('removerDaLista tira só da memória, sem chamar o backend', async () => {
    vi.mocked(compromissoService.listar).mockResolvedValue(pagina([reuniao, portao]))
    const store = useCompromissoStore()
    await store.listarPrimeiraPagina()

    store.removerDaLista(1)

    expect(store.compromissos).toEqual([portao])
    expect(compromissoService.remover).not.toHaveBeenCalled()
  })

  it('remover apaga no backend, tira da lista e limpa o compromissoAtual quando é o mesmo', async () => {
    vi.mocked(compromissoService.listar).mockResolvedValue(pagina([reuniao, portao]))
    vi.mocked(compromissoService.obter).mockResolvedValue(reuniao)
    vi.mocked(compromissoService.remover).mockResolvedValue(undefined)
    const store = useCompromissoStore()
    await store.listarPrimeiraPagina()
    await store.obter(1)

    await store.remover(1)

    expect(compromissoService.remover).toHaveBeenCalledWith(1)
    expect(store.compromissos).toEqual([portao])
    expect(store.compromissoAtual).toBeNull()
  })

  it('resetarFiltros volta ao padrão', () => {
    const store = useCompromissoStore()
    store.filtros.situacao = 'concluidos'
    store.filtros.escopo = 'todos'
    store.filtros.predioId = 3
    store.filtros.titulo = 'portão'

    store.resetarFiltros()

    expect(store.filtros).toEqual({ titulo: '', situacao: 'proximos', escopo: 'meus', predioId: null })
  })
})
