import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import {
  ORCAMENTO_MAXIMO,
  planejamentoFormSchema,
  planejamentoSchema,
  planejamentosPaginadosSchema,
} from './planejamento.schema'

const planejamentoDto = {
  id: 5,
  titulo: 'Pintura da fachada',
  dataPlanejamento: '2026-12-01',
  orcamentoPrevisto: 15000.5,
  nomePrestadorServico: null,
  detalhes: null,
  predioId: 1,
  predioNome: 'Edifício Solar',
  criadoEm: '2026-10-01T12:00:00Z',
}

describe('planejamentoFormSchema', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 9, 20, 12, 0, 0)) // 20/10/2026, horário local
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('aceita só o título (RN11: data e orçamento são opcionais)', () => {
    expect(() => planejamentoFormSchema.parse({ titulo: 'Pintura' })).not.toThrow()
  })

  it('aceita os opcionais vazios (campos limpos no formulário)', () => {
    expect(() =>
      planejamentoFormSchema.parse({
        titulo: 'Pintura',
        dataPlanejamento: '',
        orcamentoPrevisto: '',
        nomePrestadorServico: '',
        detalhes: '',
      }),
    ).not.toThrow()
  })

  it('rejeita título vazio ou com caracteres não permitidos', () => {
    expect(() => planejamentoFormSchema.parse({ titulo: '' })).toThrow('Título obrigatório')
    expect(() => planejamentoFormSchema.parse({ titulo: '<script>' })).toThrow(
      'caracteres não permitidos',
    )
  })

  describe('data (RN30)', () => {
    it('aceita hoje e datas futuras', () => {
      expect(() =>
        planejamentoFormSchema.parse({ titulo: 'x', dataPlanejamento: '2026-10-20' }),
      ).not.toThrow()
      expect(() =>
        planejamentoFormSchema.parse({ titulo: 'x', dataPlanejamento: '2027-01-01' }),
      ).not.toThrow()
    })

    it('rejeita datas anteriores a hoje', () => {
      expect(() =>
        planejamentoFormSchema.parse({ titulo: 'x', dataPlanejamento: '2026-10-19' }),
      ).toThrow('A data não pode estar no passado')
    })

    it('rejeita formato inválido', () => {
      expect(() =>
        planejamentoFormSchema.parse({ titulo: 'x', dataPlanejamento: '12/12/2026' }),
      ).toThrow('Data inválida')
    })
  })

  describe('orçamento', () => {
    // O campo guarda o valor mascarado ("1.500,50"); ver `mascararMoeda`.
    it.each(['0,00', '0,01', '1,50', '1.500,50', '99.999.999,99'])('aceita %s', (valor) => {
      expect(() =>
        planejamentoFormSchema.parse({ titulo: 'x', orcamentoPrevisto: valor }),
      ).not.toThrow()
    })

    it('o teto do campo é o mesmo do backend', () => {
      expect(ORCAMENTO_MAXIMO).toBe(99_999_999.99)
    })

    it.each(['-1,00', '10,123', 'abc', '1500.50', '1.50,00', '1500'])('rejeita %s', (valor) => {
      expect(() => planejamentoFormSchema.parse({ titulo: 'x', orcamentoPrevisto: valor })).toThrow(
        'Informe um valor válido, como 1.500,00',
      )
    })

    it('rejeita valor acima do máximo do banco', () => {
      expect(() =>
        planejamentoFormSchema.parse({ titulo: 'x', orcamentoPrevisto: '100.000.000,00' }),
      ).toThrow('O valor excede o máximo permitido')
    })
  })

  it('rejeita prestador e detalhes inválidos', () => {
    expect(() => planejamentoFormSchema.parse({ titulo: 'x', nomePrestadorServico: '<b>' })).toThrow(
      'caracteres não permitidos',
    )
    expect(() => planejamentoFormSchema.parse({ titulo: 'x', detalhes: '<img>' })).toThrow(
      'caracteres não permitidos',
    )
  })
})

describe('planejamentoSchema', () => {
  it('aceita a resposta do backend', () => {
    expect(planejamentoSchema.parse(planejamentoDto)).toEqual(planejamentoDto)
  })

  it('aceita data e orçamento nulos', () => {
    expect(() =>
      planejamentoSchema.parse({
        ...planejamentoDto,
        dataPlanejamento: null,
        orcamentoPrevisto: null,
      }),
    ).not.toThrow()
  })

  it('rejeita resposta sem título', () => {
    expect(() => planejamentoSchema.parse({ ...planejamentoDto, titulo: undefined })).toThrow(
      'Required',
    )
  })
})

describe('planejamentosPaginadosSchema', () => {
  it('aceita o envelope paginado', () => {
    const pagina = {
      items: [planejamentoDto],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    }
    expect(planejamentosPaginadosSchema.parse(pagina)).toEqual(pagina)
  })
})
