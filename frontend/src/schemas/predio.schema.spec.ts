import { describe, it, expect } from 'vitest'
import { z } from 'zod'
import { predioFormSchema, predioSchema, prediosPaginadosSchema } from './predio.schema'

describe('predioFormSchema', () => {
  it('aceita um payload válido (PredioFormDto do backend)', () => {
    expect(() =>
      predioFormSchema.parse({ nome: 'Edifício Solar', endereco: 'Rua da Silva, 192, Floresta' }),
    ).not.toThrow()
  })

  it('rejeita nome vazio', () => {
    expect(() => predioFormSchema.parse({ nome: '', endereco: 'Rua da Silva, 192' })).toThrow(
      z.ZodError,
    )
  })

  it('rejeita nome acima de 150 caracteres', () => {
    expect(() =>
      predioFormSchema.parse({ nome: 'A'.repeat(151), endereco: 'Rua da Silva, 192' }),
    ).toThrow(z.ZodError)
  })

  it('rejeita endereço com caracteres fora da allowlist', () => {
    expect(() =>
      predioFormSchema.parse({ nome: 'Edifício Solar', endereco: '<script>alert(1)</script>' }),
    ).toThrow(z.ZodError)
  })
})

describe('predioSchema', () => {
  it('aceita um payload válido (PredioDto do backend)', () => {
    expect(() =>
      predioSchema.parse({
        id: 1,
        nome: 'Edifício Solar',
        endereco: 'Rua da Silva, 192, Floresta',
        criadoEm: '2026-01-01T12:00:00Z',
        papel: 1,
      }),
    ).not.toThrow()
  })

  it('rejeita papel fora dos valores conhecidos de PapelPredio (1, 2 ou 3)', () => {
    expect(() =>
      predioSchema.parse({
        id: 1,
        nome: 'Edifício Solar',
        endereco: 'Rua da Silva, 192',
        criadoEm: '2026-01-01T12:00:00Z',
        papel: 9,
      }),
    ).toThrow(z.ZodError)
  })
})

describe('prediosPaginadosSchema', () => {
  it('aceita o envelope de PagedResultDto<PredioDto> (RNF10)', () => {
    expect(() =>
      prediosPaginadosSchema.parse({
        items: [
          {
            id: 1,
            nome: 'Edifício Solar',
            endereco: 'Rua da Silva, 192',
            criadoEm: '2026-01-01T12:00:00Z',
            papel: 1,
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
      }),
    ).not.toThrow()
  })
})
