import { describe, it, expect } from 'vitest'
import { AxiosError, type AxiosResponse } from 'axios'
import { ZodError, ZodIssueCode } from 'zod'
import { mensagemErroEnvio } from './documentoErro'

function erroHttp(status: number, title?: string) {
  const response = { status, data: title ? { title, status } : {} } as AxiosResponse
  return new AxiosError('falhou', 'ERR_BAD_REQUEST', undefined, undefined, response)
}

const GENERICA = 'Não foi possível enviar o arquivo. Tente novamente.'

describe('mensagemErroEnvio', () => {
  it('400 mostra o title do backend (arquivo recusado, com mensagem pronta)', () => {
    const title = 'O conteúdo do arquivo não corresponde ao formato informado.'

    expect(mensagemErroEnvio(erroHttp(400, title))).toBe(title)
  })

  it('409 mostra o title do backend (limite de normas ou de atas do mês)', () => {
    const title =
      'O limite de 10 normas foi atingido para este prédio. Exclua uma norma existente para enviar outra.'

    expect(mensagemErroEnvio(erroHttp(409, title))).toBe(title)
  })

  it('400 e 409 sem title caem na mensagem genérica', () => {
    expect(mensagemErroEnvio(erroHttp(400))).toBe(GENERICA)
    expect(mensagemErroEnvio(erroHttp(409))).toBe(GENERICA)
  })

  it('403, 413 e 429 têm mensagem própria, sem depender do title', () => {
    expect(mensagemErroEnvio(erroHttp(403, 'qualquer'))).toContain('permissão')
    expect(mensagemErroEnvio(erroHttp(413, 'qualquer'))).toContain('20 MB')
    expect(mensagemErroEnvio(erroHttp(429, 'qualquer'))).toContain('Aguarde')
  })

  it('outros status (500, rede) usam a mensagem genérica, sem expor detalhe', () => {
    expect(mensagemErroEnvio(erroHttp(500, 'stack trace'))).toBe(GENERICA)
    expect(mensagemErroEnvio(new Error('Network Error'))).toBe(GENERICA)
  })

  it('erro de validação do schema (arquivo barrado antes da requisição) mostra a regra violada', () => {
    const erro = new ZodError([
      { code: ZodIssueCode.custom, message: 'O arquivo está vazio.', path: ['arquivo'] },
    ])

    expect(mensagemErroEnvio(erro)).toBe('O arquivo está vazio.')
  })
})
