import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import {
  gerarRelatorioFormSchema,
  relatorioSchema,
  relatoriosPaginadosSchema,
} from './relatorio.schema'

const relatorioDto = {
  id: 5,
  mesReferencia: 9,
  anoReferencia: 2026,
  nomeArquivo: 'Edificio-Aurora_Relatorio_2026-09.pdf',
  predioId: 1,
  predioNome: 'Edifício Aurora',
  geradoEm: '2026-10-20T15:30:12Z',
}

describe('gerarRelatorioFormSchema', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 9, 20, 12, 0, 0)) // 20/10/2026, horário local
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('aceita um mês passado', () => {
    expect(() => gerarRelatorioFormSchema.parse({ mes: 9, ano: 2026 })).not.toThrow()
  })

  it('aceita o mês corrente (relatório parcial, RN34)', () => {
    expect(() => gerarRelatorioFormSchema.parse({ mes: 10, ano: 2026 })).not.toThrow()
  })

  it('rejeita um mês futuro', () => {
    expect(() => gerarRelatorioFormSchema.parse({ mes: 11, ano: 2026 })).toThrow('mês futuro')
    expect(() => gerarRelatorioFormSchema.parse({ mes: 1, ano: 2027 })).toThrow('mês futuro')
  })

  it.each([0, 13, -1])('rejeita o mês %i', (mes) => {
    expect(() => gerarRelatorioFormSchema.parse({ mes, ano: 2026 })).toThrow('Selecione o mês')
  })

  it('rejeita ano anterior ao piso do backend', () => {
    expect(() => gerarRelatorioFormSchema.parse({ mes: 1, ano: 1999 })).toThrow('2000')
  })

  it('exige mês e ano', () => {
    expect(() => gerarRelatorioFormSchema.parse({ ano: 2026 })).toThrow('Selecione o mês')
    expect(() => gerarRelatorioFormSchema.parse({ mes: 9 })).toThrow('Selecione o ano')
  })

  it('rejeita valores que não são números inteiros', () => {
    expect(() => gerarRelatorioFormSchema.parse({ mes: '9', ano: 2026 })).toThrow('Selecione o mês')
    expect(() => gerarRelatorioFormSchema.parse({ mes: 9.5, ano: 2026 })).toThrow('integer')
  })
})

describe('relatorioSchema', () => {
  it('aceita um payload válido', () => {
    expect(relatorioSchema.parse(relatorioDto)).toEqual(relatorioDto)
  })

  it('rejeita payload com campo faltando (API mudou/quebrou o contrato)', () => {
    const { nomeArquivo: _omitido, ...semNome } = relatorioDto

    expect(() => relatorioSchema.parse(semNome)).toThrow('nomeArquivo')
  })
})

describe('relatoriosPaginadosSchema', () => {
  it('aceita o envelope paginado do backend', () => {
    const envelope = { items: [relatorioDto], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 }

    expect(relatoriosPaginadosSchema.parse(envelope)).toEqual(envelope)
  })

  it('rejeita um item fora do contrato', () => {
    expect(() =>
      relatoriosPaginadosSchema.parse({
        items: [{ id: 1 }],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
      }),
    ).toThrow('Required')
  })
})
