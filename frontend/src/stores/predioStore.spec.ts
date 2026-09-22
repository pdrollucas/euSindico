import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { usePredioStore } from './predioStore'
import { predioService } from '@/services/predioService'

vi.mock('@/services/predioService', () => ({
  predioService: {
    listar: vi.fn<typeof predioService.listar>(),
    obter: vi.fn<typeof predioService.obter>(),
    criar: vi.fn<typeof predioService.criar>(),
    atualizar: vi.fn<typeof predioService.atualizar>(),
    remover: vi.fn<typeof predioService.remover>(),
  },
}))

const predioSolar = {
  id: 1,
  nome: 'Edifício Solar',
  endereco: 'Rua da Silva, 192, Floresta',
  criadoEm: '2026-01-01T12:00:00Z',
  papel: 1 as const,
}

describe('usePredioStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('começa vazio, sem prédio em foco', () => {
    const store = usePredioStore()
    expect(store.predios).toEqual([])
    expect(store.predioAtual).toBeNull()
  })

  it('listarPrimeiraPagina popula a lista e a paginação', async () => {
    vi.mocked(predioService.listar).mockResolvedValue({
      items: [predioSolar],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    })
    const store = usePredioStore()

    await store.listarPrimeiraPagina()

    expect(store.predios).toEqual([predioSolar])
    expect(store.temMaisPaginas).toBe(false)
  })

  it('carregarProximaPagina concatena os itens da página seguinte', async () => {
    vi.mocked(predioService.listar).mockResolvedValueOnce({
      items: [predioSolar],
      page: 1,
      pageSize: 1,
      totalCount: 2,
      totalPages: 2,
    })
    const outroPredio = { ...predioSolar, id: 2, nome: 'Edifício Nuvem' }
    vi.mocked(predioService.listar).mockResolvedValueOnce({
      items: [outroPredio],
      page: 2,
      pageSize: 1,
      totalCount: 2,
      totalPages: 2,
    })
    const store = usePredioStore()
    await store.listarPrimeiraPagina()
    expect(store.temMaisPaginas).toBe(true)

    await store.carregarProximaPagina()

    expect(store.predios).toEqual([predioSolar, outroPredio])
    expect(store.temMaisPaginas).toBe(false)
  })

  it('obter guarda o prédio como predioAtual', async () => {
    vi.mocked(predioService.obter).mockResolvedValue(predioSolar)
    const store = usePredioStore()

    const resultado = await store.obter(1)

    expect(resultado).toEqual(predioSolar)
    expect(store.predioAtual).toEqual(predioSolar)
  })

  it('criar adiciona o prédio criado à lista', async () => {
    vi.mocked(predioService.criar).mockResolvedValue(predioSolar)
    const store = usePredioStore()

    await store.criar({ nome: 'Edifício Solar', endereco: 'Rua da Silva, 192, Floresta' })

    expect(store.predios).toEqual([predioSolar])
  })

  it('atualizar substitui o prédio na lista e no predioAtual', async () => {
    vi.mocked(predioService.listar).mockResolvedValue({
      items: [predioSolar],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    })
    const predioAtualizado = { ...predioSolar, nome: 'Edifício Solar Renovado' }
    vi.mocked(predioService.atualizar).mockResolvedValue(predioAtualizado)
    const store = usePredioStore()
    await store.listarPrimeiraPagina()
    await store.obter(1)

    await store.atualizar(1, { nome: 'Edifício Solar Renovado', endereco: predioSolar.endereco })

    expect(store.predios).toEqual([predioAtualizado])
    expect(store.predioAtual).toEqual(predioAtualizado)
  })

  it('remover tira o prédio da lista e limpa o predioAtual quando é o mesmo', async () => {
    vi.mocked(predioService.listar).mockResolvedValue({
      items: [predioSolar],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    })
    vi.mocked(predioService.remover).mockResolvedValue(undefined)
    const store = usePredioStore()
    await store.listarPrimeiraPagina()
    await store.obter(1)

    await store.remover(1)

    expect(store.predios).toEqual([])
    expect(store.predioAtual).toBeNull()
  })
})
