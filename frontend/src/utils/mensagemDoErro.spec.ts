import { describe, it, expect } from 'vitest'
import { AxiosError, type AxiosResponse } from 'axios'
import { mensagemDoErro } from './mensagemDoErro'

function erroApi(status: number, data: unknown): AxiosError {
  const erro = new AxiosError('falhou')
  erro.response = { status, data } as AxiosResponse
  return erro
}

describe('mensagemDoErro', () => {
  it('usa o title do backend quando o status está na lista', () => {
    const erro = erroApi(400, { title: 'A senha atual informada está incorreta.', status: 400 })

    expect(mensagemDoErro(erro, 'padrão', [400])).toBe('A senha atual informada está incorreta.')
  })

  it('usa a mensagem padrão quando o status não está na lista', () => {
    const erro = erroApi(500, { title: 'Ocorreu um erro inesperado.', status: 500 })

    expect(mensagemDoErro(erro, 'padrão', [400, 409])).toBe('padrão')
  })

  it('ignora o title genérico de um 400 de validação (ValidationProblemDetails)', () => {
    const erro = erroApi(400, {
      title: 'One or more validation errors occurred.',
      errors: { Senha: ['A senha é obrigatória.'] },
    })

    expect(mensagemDoErro(erro, 'padrão', [400])).toBe('padrão')
  })

  it('usa a mensagem padrão quando a resposta não traz title', () => {
    expect(mensagemDoErro(erroApi(409, {}), 'padrão', [409])).toBe('padrão')
    expect(mensagemDoErro(erroApi(409, { title: '' }), 'padrão', [409])).toBe('padrão')
  })

  it('usa a mensagem padrão quando não houve resposta (rede fora)', () => {
    expect(mensagemDoErro(new AxiosError('Network Error'), 'padrão', [400])).toBe('padrão')
  })

  it('usa a mensagem padrão para erros que não vêm do Axios', () => {
    expect(mensagemDoErro(new Error('qualquer'), 'padrão', [400])).toBe('padrão')
  })
})
