import { describe, it, expect } from 'vitest'
import { z } from 'zod'
import {
  alterarSenhaFormSchema,
  alterarSenhaRequestSchema,
  atualizarPerfilRequestSchema,
  confirmarTrocaEmailRequestSchema,
  enviarSuporteRequestSchema,
  excluirContaRequestSchema,
  solicitarTrocaEmailRequestSchema,
} from './perfil.schema'

describe('atualizarPerfilRequestSchema', () => {
  it('aceita um nome válido', () => {
    expect(() => atualizarPerfilRequestSchema.parse({ nome: "Maria D'Ávila-Souza" })).not.toThrow()
  })

  it('rejeita nome vazio, ausente ou com caracteres inválidos', () => {
    expect(() => atualizarPerfilRequestSchema.parse({ nome: '' })).toThrow(z.ZodError)
    expect(() => atualizarPerfilRequestSchema.parse({})).toThrow('Nome obrigatório')
    expect(() => atualizarPerfilRequestSchema.parse({ nome: '<script>' })).toThrow(z.ZodError)
  })
})

describe('solicitarTrocaEmailRequestSchema', () => {
  it('aceita novo e-mail válido com a senha atual', () => {
    expect(() =>
      solicitarTrocaEmailRequestSchema.parse({ novoEmail: 'novo@exemplo.com', senhaAtual: 'qualquer' }),
    ).not.toThrow()
  })

  it('rejeita e-mail inválido', () => {
    expect(() =>
      solicitarTrocaEmailRequestSchema.parse({ novoEmail: 'invalido', senhaAtual: 'qualquer' }),
    ).toThrow('E-mail inválido')
  })

  it('exige a senha atual', () => {
    expect(() =>
      solicitarTrocaEmailRequestSchema.parse({ novoEmail: 'novo@exemplo.com', senhaAtual: '' }),
    ).toThrow('Senha obrigatória')
  })
})

describe('confirmarTrocaEmailRequestSchema', () => {
  it('aceita um código de 6 caracteres', () => {
    expect(() => confirmarTrocaEmailRequestSchema.parse({ codigo: 'ABC234' })).not.toThrow()
  })

  it('rejeita código de outro tamanho', () => {
    expect(() => confirmarTrocaEmailRequestSchema.parse({ codigo: 'ABC' })).toThrow(
      'O código tem 6 caracteres',
    )
  })
})

describe('alterarSenhaRequestSchema', () => {
  it('aceita senha atual + nova senha forte', () => {
    expect(() =>
      alterarSenhaRequestSchema.parse({ senhaAtual: 'Antiga@123', novaSenha: 'Nova@12345' }),
    ).not.toThrow()
  })

  it('rejeita nova senha fraca', () => {
    expect(() =>
      alterarSenhaRequestSchema.parse({ senhaAtual: 'Antiga@123', novaSenha: 'fraca' }),
    ).toThrow(z.ZodError)
  })

  it('exige a senha atual', () => {
    expect(() =>
      alterarSenhaRequestSchema.parse({ senhaAtual: '', novaSenha: 'Nova@12345' }),
    ).toThrow('Senha obrigatória')
  })
})

describe('alterarSenhaFormSchema', () => {
  const valido = { senhaAtual: 'Antiga@123', novaSenha: 'Nova@12345', confirmarSenha: 'Nova@12345' }

  it('aceita quando a confirmação coincide', () => {
    expect(() => alterarSenhaFormSchema.parse(valido)).not.toThrow()
  })

  it('rejeita quando a confirmação não coincide, apontando o campo da confirmação', () => {
    const resultado = alterarSenhaFormSchema.safeParse({ ...valido, confirmarSenha: 'Outra@12345' })

    expect(resultado.success).toBe(false)
    expect(resultado.error?.issues[0]).toMatchObject({
      message: 'As senhas não coincidem',
      path: ['confirmarSenha'],
    })
  })

  it('exige a confirmação', () => {
    expect(() => alterarSenhaFormSchema.parse({ ...valido, confirmarSenha: '' })).toThrow(
      'Confirmação obrigatória',
    )
  })
})

describe('excluirContaRequestSchema', () => {
  it('aceita senha preenchida', () => {
    expect(() => excluirContaRequestSchema.parse({ senha: 'Senha@123' })).not.toThrow()
  })

  it('exige a senha', () => {
    expect(() => excluirContaRequestSchema.parse({ senha: '' })).toThrow('Senha obrigatória')
    expect(() => excluirContaRequestSchema.parse({})).toThrow('Senha obrigatória')
  })
})

describe('enviarSuporteRequestSchema', () => {
  const valido = { assunto: 'Erro no relatório', mensagem: 'Não consigo gerar o relatório de março.' }

  it('aceita assunto e mensagem válidos', () => {
    expect(() => enviarSuporteRequestSchema.parse(valido)).not.toThrow()
  })

  it('rejeita assunto curto e acima de 100 caracteres', () => {
    expect(() => enviarSuporteRequestSchema.parse({ ...valido, assunto: 'ab' })).toThrow(
      'no mínimo 3',
    )
    expect(() =>
      enviarSuporteRequestSchema.parse({ ...valido, assunto: 'a'.repeat(101) }),
    ).toThrow('no máximo 100')
  })

  it('rejeita mensagem curta e acima de 2000 caracteres', () => {
    expect(() => enviarSuporteRequestSchema.parse({ ...valido, mensagem: 'curta' })).toThrow(
      'no mínimo 10',
    )
    expect(() =>
      enviarSuporteRequestSchema.parse({ ...valido, mensagem: 'a'.repeat(2001) }),
    ).toThrow('no máximo 2000')
  })

  it('conta o tamanho sem os espaços das pontas (mesma regra de quem só digitou espaços)', () => {
    expect(() =>
      enviarSuporteRequestSchema.parse({ ...valido, mensagem: '          x          ' }),
    ).toThrow('no mínimo 10')
  })

  it('exige os dois campos', () => {
    expect(() => enviarSuporteRequestSchema.parse({})).toThrow(z.ZodError)
  })
})
