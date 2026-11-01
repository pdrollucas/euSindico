import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import MockAdapter from 'axios-mock-adapter'
import { ZodError } from 'zod'
import api from '@/plugins/axios'
import { suporteService } from './suporteService'

describe('suporteService', () => {
  let mock: MockAdapter

  beforeEach(() => {
    // O interceptor de request do Axios lê o authStore.
    setActivePinia(createPinia())
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('enviar usa POST /suporte com assunto e mensagem', async () => {
    mock.onPost('/suporte').reply(204)

    await suporteService.enviar({
      assunto: 'Erro no relatório',
      mensagem: 'Não consigo gerar o relatório de março.',
    })

    expect(JSON.parse(mock.history.post[0]?.data as string)).toEqual({
      assunto: 'Erro no relatório',
      mensagem: 'Não consigo gerar o relatório de março.',
    })
  })

  it('enviar não chama a API com mensagem curta demais', async () => {
    await expect(
      suporteService.enviar({ assunto: 'Assunto válido', mensagem: 'curta' }),
    ).rejects.toBeInstanceOf(ZodError)

    expect(mock.history.post).toHaveLength(0)
  })

  it('enviar propaga o erro da API (ex: 429 do limite por hora)', async () => {
    mock.onPost('/suporte').reply(429, { title: 'Muitas tentativas. Tente novamente em instantes.' })

    await expect(
      suporteService.enviar({ assunto: 'Assunto válido', mensagem: 'Mensagem com tamanho suficiente.' }),
    ).rejects.toMatchObject({ response: { status: 429 } })
  })
})
