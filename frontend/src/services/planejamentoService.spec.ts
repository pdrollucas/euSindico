import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import MockAdapter from 'axios-mock-adapter'
import api from '@/plugins/axios'
import { planejamentoService } from './planejamentoService'

const dto = {
  id: 1,
  titulo: 'Pintura',
  dataPlanejamento: '2026-12-01',
  orcamentoPrevisto: 1500.5,
  nomePrestadorServico: null,
  detalhes: null,
  predioId: 4,
  predioNome: 'Edifício Solar',
  criadoEm: '2026-10-01T12:00:00Z',
}

const vazio = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }
const semFiltros = { titulo: '', orcamentoMinimo: null, orcamentoMaximo: null }

describe('planejamentoService', () => {
  let mock: MockAdapter

  beforeEach(() => {
    // O interceptor de request do Axios lê o authStore.
    setActivePinia(createPinia())
    mock = new MockAdapter(api)
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(2026, 9, 20, 12, 0, 0))
  })

  afterEach(() => {
    mock.restore()
    vi.useRealTimers()
  })

  it('listar usa a rota aninhada no prédio e só envia a paginação sem filtros', async () => {
    mock.onGet('/predios/4/planejamentos').reply(200, vazio)

    await planejamentoService.listar(4, { ...semFiltros, titulo: '  ' }, 2, 20)

    expect(mock.history.get[0]?.params).toEqual({ page: 2, pageSize: 20 })
  })

  it('listar inclui titulo (sem espaços nas pontas) e a faixa de orçamento quando informados', async () => {
    mock.onGet('/predios/4/planejamentos').reply(200, vazio)

    await planejamentoService.listar(
      4,
      { titulo: ' fachada ', orcamentoMinimo: 100, orcamentoMaximo: 500 },
      1,
      20,
    )

    expect(mock.history.get[0]?.params).toEqual({
      page: 1,
      pageSize: 20,
      titulo: 'fachada',
      orcamentoMinimo: 100,
      orcamentoMaximo: 500,
    })
  })

  it('listar envia orçamento zero (zero é um limite válido, não "sem limite")', async () => {
    mock.onGet('/predios/4/planejamentos').reply(200, vazio)

    await planejamentoService.listar(4, { ...semFiltros, orcamentoMinimo: 0 }, 1, 20)

    expect(mock.history.get[0]?.params).toMatchObject({ orcamentoMinimo: 0 })
    expect(mock.history.get[0]?.params).not.toHaveProperty('orcamentoMaximo')
  })

  it('obter lê o planejamento dentro do prédio', async () => {
    mock.onGet('/predios/4/planejamentos/1').reply(200, dto)

    const resultado = await planejamentoService.obter(4, 1)

    expect(resultado).toEqual(dto)
  })

  it('criar troca opcionais vazios por null e converte o orçamento em número', async () => {
    mock.onPost('/predios/4/planejamentos').reply(201, dto)

    await planejamentoService.criar(4, {
      titulo: 'Pintura',
      dataPlanejamento: '',
      orcamentoPrevisto: '1.500,50',
      nomePrestadorServico: '',
      detalhes: '  ',
    })

    expect(JSON.parse(mock.history.post[0]?.data as string)).toEqual({
      titulo: 'Pintura',
      dataPlanejamento: null,
      orcamentoPrevisto: 1500.5,
      nomePrestadorServico: null,
      detalhes: null,
    })
  })

  it('criar mantém a data informada e envia o orçamento zero como número, não null', async () => {
    mock.onPost('/predios/4/planejamentos').reply(201, dto)

    await planejamentoService.criar(4, {
      titulo: 'Pintura',
      dataPlanejamento: '2026-12-01',
      orcamentoPrevisto: '0,00',
    })

    expect(JSON.parse(mock.history.post[0]?.data as string)).toMatchObject({
      dataPlanejamento: '2026-12-01',
      orcamentoPrevisto: 0,
    })
  })

  it('criar não envia nada se o formulário for inválido', async () => {
    await expect(planejamentoService.criar(4, { titulo: '' })).rejects.toThrow('Título obrigatório')

    expect(mock.history.post).toHaveLength(0)
  })

  it('atualizar usa PUT em /predios/{id}/planejamentos/{id}', async () => {
    mock.onPut('/predios/4/planejamentos/7').reply(200, dto)

    await planejamentoService.atualizar(4, 7, { titulo: 'Reforma', orcamentoPrevisto: '10,00' })

    expect(JSON.parse(mock.history.put[0]?.data as string)).toMatchObject({
      titulo: 'Reforma',
      orcamentoPrevisto: 10,
    })
  })

  it('remover usa DELETE em /predios/{id}/planejamentos/{id}', async () => {
    mock.onDelete('/predios/4/planejamentos/7').reply(204)

    await planejamentoService.remover(4, 7)

    expect(mock.history.delete[0]?.url).toBe('/predios/4/planejamentos/7')
  })
})
