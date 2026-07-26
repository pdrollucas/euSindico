import { describe, it, expect } from 'vitest'
import { z } from 'zod'
import { usuarioSchema } from './usuario.schema'

describe('usuarioSchema', () => {
  it('aceita um payload válido (UsuarioDto do backend)', () => {
    expect(() =>
      usuarioSchema.parse({
        id: 1,
        nome: 'João da Silva',
        email: 'joao@exemplo.com',
        criadoEm: '2026-01-01T12:00:00Z',
      }),
    ).not.toThrow()
  })

  it('rejeita payload com campo faltando (API mudou/quebrou o contrato)', () => {
    expect(() => usuarioSchema.parse({ id: 1, nome: 'João' })).toThrow(z.ZodError)
  })

  it('rejeita e-mail em formato inválido', () => {
    expect(() =>
      usuarioSchema.parse({
        id: 1,
        nome: 'João',
        email: 'nao-e-um-email',
        criadoEm: '2026-01-01T12:00:00Z',
      }),
    ).toThrow(z.ZodError)
  })
})
