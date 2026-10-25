import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import MockAdapter from 'axios-mock-adapter'
import api from '@/plugins/axios'
import { compromissoService } from './compromissoService'

const dto = {
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

describe('compromissoService', () => {
  let mock: MockAdapter

  beforeEach(() => {
    // O interceptor de request do Axios lê o authStore.
    setActivePinia(createPinia())
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('listar envia situação/escopo e só inclui titulo e predioId quando preenchidos', async () => {
    mock
      .onGet('/compromissos')
      .reply(200, { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })

    await compromissoService.listar(
      { titulo: '  ', situacao: 'hoje', escopo: 'todos', predioId: null, ano: null, mes: null },
      2,
      20,
    )

    expect(mock.history.get[0]?.params).toEqual({
      page: 2,
      pageSize: 20,
      situacao: 'hoje',
      escopo: 'todos',
    })
  })

  it('listar inclui titulo (sem espaços nas pontas) e predioId quando informados', async () => {
    mock
      .onGet('/compromissos')
      .reply(200, { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })

    await compromissoService.listar(
      {
        titulo: ' portão ',
        situacao: 'proximos',
        escopo: 'meus',
        predioId: 4,
        ano: null,
        mes: null,
      },
      1,
      20,
    )

    expect(mock.history.get[0]?.params).toMatchObject({ titulo: 'portão', predioId: 4 })
  })

  it('listar envia ano e mês juntos, e nenhum dos dois quando falta um', async () => {
    mock
      .onGet('/compromissos')
      .reply(200, { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 })
    const base = { titulo: '', situacao: 'proximos', escopo: 'meus', predioId: null } as const

    await compromissoService.listar({ ...base, ano: 2026, mes: 11 }, 1, 20)
    await compromissoService.listar({ ...base, ano: 2026, mes: null }, 1, 20)
    await compromissoService.listar({ ...base, ano: null, mes: 11 }, 1, 20)

    expect(mock.history.get[0]?.params).toMatchObject({ ano: 2026, mes: 11 })
    for (const indice of [1, 2]) {
      expect(mock.history.get[indice]?.params).not.toHaveProperty('ano')
      expect(mock.history.get[indice]?.params).not.toHaveProperty('mes')
    }
  })

  it('criar completa o horário com segundos e troca opcionais vazios por null', async () => {
    mock.onPost('/compromissos').reply(201, dto)

    await compromissoService.criar({
      predioId: 1,
      titulo: 'Reunião',
      dataCompromisso: '2026-10-20',
      horarioCompromisso: '18:30',
      nomePrestadorServico: '',
      detalhes: '  ',
    })

    expect(JSON.parse(mock.history.post[0]?.data as string)).toEqual({
      predioId: 1,
      titulo: 'Reunião',
      dataCompromisso: '2026-10-20',
      horarioCompromisso: '18:30:00',
      nomePrestadorServico: null,
      detalhes: null,
      orcamento: null,
      responsavelUsuarioId: null,
    })
  })

  it('criar converte o orçamento mascarado em número', async () => {
    mock.onPost('/compromissos').reply(201, dto)

    await compromissoService.criar({
      predioId: 1,
      titulo: 'Reunião',
      dataCompromisso: '2026-10-20',
      horarioCompromisso: '18:30',
      orcamento: '1.500,50',
    })

    expect(JSON.parse(mock.history.post[0]?.data as string)).toMatchObject({ orcamento: 1500.5 })
  })

  it('atualizar envia orçamento null quando o campo está vazio e 0 quando é zero', async () => {
    mock.onPut('/compromissos/5').reply(200, dto)
    const base = {
      predioId: 1,
      titulo: 'Reunião',
      dataCompromisso: '2026-10-20',
      horarioCompromisso: '18:30',
    }

    await compromissoService.atualizar(5, { ...base, orcamento: '' })
    await compromissoService.atualizar(5, { ...base, orcamento: '0,00' })

    expect(JSON.parse(mock.history.put[0]?.data as string).orcamento).toBeNull()
    expect(JSON.parse(mock.history.put[1]?.data as string).orcamento).toBe(0)
  })

  it('criar não envia nada se o formulário for inválido', async () => {
    await expect(
      compromissoService.criar({
        predioId: 1,
        titulo: '',
        dataCompromisso: '2026-10-20',
        horarioCompromisso: '18:30',
      }),
    ).rejects.toThrow('Título obrigatório')

    expect(mock.history.post).toHaveLength(0)
  })

  it('atualizar usa PUT em /compromissos/{id} e mantém o responsável informado', async () => {
    mock.onPut('/compromissos/7').reply(200, dto)

    await compromissoService.atualizar(7, {
      predioId: 1,
      titulo: 'Reunião',
      dataCompromisso: '2026-10-20',
      horarioCompromisso: '18:30:00',
      responsavelUsuarioId: 3,
    })

    expect(JSON.parse(mock.history.put[0]?.data as string)).toMatchObject({
      horarioCompromisso: '18:30:00',
      responsavelUsuarioId: 3,
    })
  })

  it('alterarConclusao usa PATCH com { concluido }', async () => {
    mock.onPatch('/compromissos/7').reply(200, { ...dto, concluido: true })

    const resultado = await compromissoService.alterarConclusao(7, true)

    expect(JSON.parse(mock.history.patch[0]?.data as string)).toEqual({ concluido: true })
    expect(resultado.concluido).toBe(true)
  })

  it('remover usa DELETE em /compromissos/{id}', async () => {
    mock.onDelete('/compromissos/7').reply(204)

    await compromissoService.remover(7)

    expect(mock.history.delete[0]?.url).toBe('/compromissos/7')
  })
})
