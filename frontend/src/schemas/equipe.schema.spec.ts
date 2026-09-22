import { describe, it, expect } from 'vitest'
import { z } from 'zod'
import {
  convidarFuncionarioRequestSchema,
  equipeMembroSchema,
  conviteDetalheSchema,
  aceitarConviteNovaContaFormSchema,
} from './equipe.schema'

describe('convidarFuncionarioRequestSchema', () => {
  it('aceita um payload válido (ConvidarFuncionarioDto do backend)', () => {
    expect(() =>
      convidarFuncionarioRequestSchema.parse({ email: 'funcionario@exemplo.com', papel: 3 }),
    ).not.toThrow()
  })

  it('rejeita papel Síndico (1) — convite nunca cria outro dono (EQUIPE.md)', () => {
    expect(() =>
      convidarFuncionarioRequestSchema.parse({ email: 'funcionario@exemplo.com', papel: 1 }),
    ).toThrow(z.ZodError)
  })

  it('rejeita e-mail inválido', () => {
    expect(() =>
      convidarFuncionarioRequestSchema.parse({ email: 'nao-e-um-email', papel: 2 }),
    ).toThrow(z.ZodError)
  })
})

describe('equipeMembroSchema', () => {
  it('aceita um payload válido (EquipeMembroDto do backend)', () => {
    expect(() =>
      equipeMembroSchema.parse({
        usuarioId: 2,
        nome: 'Maria Souza',
        email: 'maria@exemplo.com',
        papel: 2,
        criadoEm: '2026-01-01T12:00:00Z',
      }),
    ).not.toThrow()
  })
})

describe('conviteDetalheSchema', () => {
  it('aceita um payload válido (ConviteDetalheDto do backend)', () => {
    expect(() =>
      conviteDetalheSchema.parse({
        predioNome: 'Edifício Solar',
        papel: 3,
        contaJaExiste: false,
        expiraEm: '2026-01-08T12:00:00Z',
      }),
    ).not.toThrow()
  })
})

describe('aceitarConviteNovaContaFormSchema', () => {
  it('aceita nome e senha válidos (RNF04)', () => {
    expect(() =>
      aceitarConviteNovaContaFormSchema.parse({ nome: 'João da Silva', senha: 'SenhaForte1!' }),
    ).not.toThrow()
  })

  it('rejeita senha fraca', () => {
    expect(() =>
      aceitarConviteNovaContaFormSchema.parse({ nome: 'João da Silva', senha: 'fraca' }),
    ).toThrow(z.ZodError)
  })
})
