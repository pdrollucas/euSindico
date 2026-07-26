import { describe, it, expect } from 'vitest'
import { z } from 'zod'
import {
  loginRequestSchema,
  registrarRequestSchema,
  redefinirSenhaRequestSchema,
  verificarCodigoRequestSchema,
} from './auth.schema'

describe('loginRequestSchema', () => {
  it('aceita e-mail e senha preenchidos', () => {
    expect(() =>
      loginRequestSchema.parse({ email: 'sindico@exemplo.com', senha: 'qualquer' }),
    ).not.toThrow()
  })

  it('rejeita e-mail inválido', () => {
    expect(() =>
      loginRequestSchema.parse({ email: 'nao-e-um-email', senha: 'qualquer' }),
    ).toThrow(z.ZodError)
  })

  it('usa a mensagem customizada (não o "Required" padrão do Zod) quando a senha não é preenchida', () => {
    const resultado = loginRequestSchema.safeParse({ email: 'sindico@exemplo.com', senha: undefined })
    expect(resultado.success).toBe(false)
    expect(resultado.error?.issues[0]?.message).toBe('Senha obrigatória')
  })
})

describe('registrarRequestSchema', () => {
  it('aceita payload que atende RNF04 e à regra de nome', () => {
    expect(() =>
      registrarRequestSchema.parse({
        nome: 'João da Silva',
        email: 'joao@exemplo.com',
        senha: 'SenhaForte1!',
      }),
    ).not.toThrow()
  })

  it('rejeita senha fraca (RNF04)', () => {
    expect(() =>
      registrarRequestSchema.parse({
        nome: 'João da Silva',
        email: 'joao@exemplo.com',
        senha: 'fraca',
      }),
    ).toThrow(z.ZodError)
  })

  it('rejeita nome com caracteres não permitidos', () => {
    expect(() =>
      registrarRequestSchema.parse({
        nome: 'João123',
        email: 'joao@exemplo.com',
        senha: 'SenhaForte1!',
      }),
    ).toThrow(z.ZodError)
  })
})

describe('verificarCodigoRequestSchema', () => {
  it('rejeita código com tamanho diferente de 6', () => {
    expect(() =>
      verificarCodigoRequestSchema.parse({ email: 'joao@exemplo.com', codigo: '123' }),
    ).toThrow(z.ZodError)
  })
})

describe('redefinirSenhaRequestSchema', () => {
  it('rejeita quando novaSenha e confirmarSenha divergem', () => {
    expect(() =>
      redefinirSenhaRequestSchema.parse({
        email: 'joao@exemplo.com',
        codigo: 'AB12CD',
        novaSenha: 'SenhaForte1!',
        confirmarSenha: 'Outra1!',
      }),
    ).toThrow(z.ZodError)
  })
})
