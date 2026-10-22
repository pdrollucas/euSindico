import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import MockAdapter from 'axios-mock-adapter'
import api from '@/plugins/axios'
import { relatorioService } from './relatorioService'

const dto = {
  id: 1,
  mesReferencia: 9,
  anoReferencia: 2026,
  nomeArquivo: 'Edificio-Solar_Relatorio_2026-09.pdf',
  predioId: 4,
  predioNome: 'Edifício Solar',
  geradoEm: '2026-10-20T15:30:12Z',
}

const vazio = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }

describe('relatorioService', () => {
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

  it('listar usa a rota aninhada no prédio e envia só a paginação', async () => {
    mock.onGet('/predios/4/relatorios').reply(200, vazio)

    await relatorioService.listar(4, 2, 20)

    expect(mock.history.get[0]?.params).toEqual({ page: 2, pageSize: 20 })
  })

  it('listar valida o envelope da resposta', async () => {
    mock.onGet('/predios/4/relatorios').reply(200, { items: [{ id: 1 }] })

    await expect(relatorioService.listar(4, 1, 20)).rejects.toThrow('Required')
  })

  it('gerar usa POST em /predios/{id}/relatorios com mês e ano', async () => {
    mock.onPost('/predios/4/relatorios').reply(201, dto)

    const resultado = await relatorioService.gerar(4, { mes: 9, ano: 2026 })

    expect(JSON.parse(mock.history.post[0]?.data as string)).toEqual({ mes: 9, ano: 2026 })
    expect(resultado).toEqual(dto)
  })

  it('gerar não envia nada se o período for inválido (mês futuro)', async () => {
    await expect(relatorioService.gerar(4, { mes: 11, ano: 2026 })).rejects.toThrow('mês futuro')

    expect(mock.history.post).toHaveLength(0)
  })

  it('baixar pede o PDF como blob e o devolve como application/pdf', async () => {
    mock.onGet('/predios/4/relatorios/1/download').reply(200, new Blob(['%PDF-1.7']))

    const arquivo = await relatorioService.baixar(4, 1)

    expect(mock.history.get[0]?.responseType).toBe('blob')
    expect(arquivo).toBeInstanceOf(Blob)
    expect(arquivo.type).toBe('application/pdf')
  })

  it('baixar propaga o erro do backend (ex: 404)', async () => {
    mock.onGet('/predios/4/relatorios/1/download').reply(404)

    await expect(relatorioService.baixar(4, 1)).rejects.toMatchObject({
      response: { status: 404 },
    })
  })

  it('remover usa DELETE em /predios/{id}/relatorios/{id}', async () => {
    mock.onDelete('/predios/4/relatorios/7').reply(204)

    await relatorioService.remover(4, 7)

    expect(mock.history.delete[0]?.url).toBe('/predios/4/relatorios/7')
  })
})
