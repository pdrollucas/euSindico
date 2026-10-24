import { describe, it, expect } from 'vitest'
import { z } from 'zod'
import {
  compromissoFormSchema,
  compromissoSchema,
  compromissosPaginadosSchema,
} from './compromisso.schema'

const formValido = {
  predioId: 1,
  titulo: 'Visita técnica do elevador',
  dataCompromisso: '2026-10-20',
  horarioCompromisso: '14:30',
}

const compromissoDto = {
  id: 5,
  titulo: 'Reunião',
  dataCompromisso: '2026-10-20',
  horarioCompromisso: '14:30:00',
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

describe('compromissoFormSchema', () => {
  it('aceita só os campos obrigatórios (RN10: data e horário)', () => {
    expect(() => compromissoFormSchema.parse(formValido)).not.toThrow()
  })

  it('aceita os opcionais vazios (campo limpo no formulário)', () => {
    expect(() =>
      compromissoFormSchema.parse({ ...formValido, nomePrestadorServico: '', detalhes: '' }),
    ).not.toThrow()
  })

  it.each(['', '0,00', '1.500,50', '99.999.999,99'])('aceita o orçamento %s', (orcamento) => {
    expect(() => compromissoFormSchema.parse({ ...formValido, orcamento })).not.toThrow()
  })

  it.each(['1500', '1,5', '1.50,00', 'abc'])('rejeita o orçamento de formato inválido %s', (orcamento) => {
    expect(() => compromissoFormSchema.parse({ ...formValido, orcamento })).toThrow(
      'Informe um valor válido',
    )
  })

  it('rejeita o orçamento acima do máximo', () => {
    expect(() =>
      compromissoFormSchema.parse({ ...formValido, orcamento: '100.000.000,00' }),
    ).toThrow('O valor excede o máximo permitido')
  })

  it('aceita horário com segundos', () => {
    expect(() =>
      compromissoFormSchema.parse({ ...formValido, horarioCompromisso: '14:30:00' }),
    ).not.toThrow()
  })

  it.each(['24:00', '25:61', '18:60', '1830', '9:30', '18:3'])(
    'rejeita o horário inválido %s',
    (horario) => {
      expect(() =>
        compromissoFormSchema.parse({ ...formValido, horarioCompromisso: horario }),
      ).toThrow(z.ZodError)
    },
  )

  it.each(['00:00', '09:05', '23:59'])('aceita o horário válido %s', (horario) => {
    expect(() =>
      compromissoFormSchema.parse({ ...formValido, horarioCompromisso: horario }),
    ).not.toThrow()
  })

  it('rejeita prédio ausente', () => {
    const { predioId: _predioId, ...semPredio } = formValido
    expect(() => compromissoFormSchema.parse(semPredio)).toThrow(z.ZodError)
  })

  it('rejeita prédio nulo (select sem escolha)', () => {
    expect(() => compromissoFormSchema.parse({ ...formValido, predioId: null })).toThrow(z.ZodError)
  })

  it('rejeita título vazio ou só com espaços', () => {
    expect(() => compromissoFormSchema.parse({ ...formValido, titulo: '   ' })).toThrow(z.ZodError)
  })

  it('rejeita título acima de 150 caracteres', () => {
    expect(() => compromissoFormSchema.parse({ ...formValido, titulo: 'A'.repeat(151) })).toThrow(
      z.ZodError,
    )
  })

  it('rejeita título com caracteres fora da allowlist', () => {
    expect(() =>
      compromissoFormSchema.parse({ ...formValido, titulo: '<script>alert(1)</script>' }),
    ).toThrow(z.ZodError)
  })

  it('rejeita data e horário em branco', () => {
    expect(() => compromissoFormSchema.parse({ ...formValido, dataCompromisso: '' })).toThrow(
      z.ZodError,
    )
    expect(() => compromissoFormSchema.parse({ ...formValido, horarioCompromisso: '' })).toThrow(
      z.ZodError,
    )
  })

  it('rejeita prestador com caracteres fora da allowlist', () => {
    expect(() =>
      compromissoFormSchema.parse({ ...formValido, nomePrestadorServico: '<b>Atlas</b>' }),
    ).toThrow(z.ZodError)
  })

  it('aceita detalhes com texto livre, mas rejeita < e >', () => {
    expect(() =>
      compromissoFormSchema.parse({ ...formValido, detalhes: 'Orçamento: R$ 1.200,00 (50%)' }),
    ).not.toThrow()
    expect(() => compromissoFormSchema.parse({ ...formValido, detalhes: 'olá <b>' })).toThrow(
      z.ZodError,
    )
  })

  it('rejeita detalhes acima de 2000 caracteres', () => {
    expect(() =>
      compromissoFormSchema.parse({ ...formValido, detalhes: 'a'.repeat(2001) }),
    ).toThrow(z.ZodError)
  })

  it('aceita responsável nulo, omitido ou um id válido', () => {
    expect(() =>
      compromissoFormSchema.parse({ ...formValido, responsavelUsuarioId: null }),
    ).not.toThrow()
    expect(() =>
      compromissoFormSchema.parse({ ...formValido, responsavelUsuarioId: 3 }),
    ).not.toThrow()
  })
})

describe('compromissoSchema', () => {
  it('aceita um payload válido (CompromissoDto do backend)', () => {
    expect(() => compromissoSchema.parse(compromissoDto)).not.toThrow()
  })

  it('aceita prestador e detalhes preenchidos', () => {
    expect(() =>
      compromissoSchema.parse({ ...compromissoDto, nomePrestadorServico: 'Atlas', detalhes: 'x' }),
    ).not.toThrow()
  })

  it('rejeita payload sem o nome do prédio (API mudou/quebrou o contrato)', () => {
    const { predioNome: _predioNome, ...semNome } = compromissoDto
    expect(() => compromissoSchema.parse(semNome)).toThrow(z.ZodError)
  })
})

describe('compromissosPaginadosSchema', () => {
  it('aceita o envelope de PagedResultDto<CompromissoDto> (RNF10)', () => {
    expect(() =>
      compromissosPaginadosSchema.parse({
        items: [compromissoDto],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
      }),
    ).not.toThrow()
  })
})
