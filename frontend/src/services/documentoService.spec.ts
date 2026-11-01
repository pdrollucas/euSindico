import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import MockAdapter from 'axios-mock-adapter'
import api from '@/plugins/axios'
import { documentoService } from './documentoService'

const dto = {
  id: 1,
  nomeArquivo: 'Ata 03.pdf',
  predioId: 4,
  predioNome: 'Edifício Solar',
  criadoEm: '2026-10-01T12:00:00',
}

const vazio = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }

describe('documentoService', () => {
  let mock: MockAdapter

  beforeEach(() => {
    // O interceptor de request do Axios lê o authStore.
    setActivePinia(createPinia())
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('listar usa a rota do tipo, aninhada no prédio, e só envia a paginação sem busca', async () => {
    mock.onGet('/predios/4/atas').reply(200, vazio)
    mock.onGet('/predios/4/normas').reply(200, vazio)

    await documentoService.listar('atas', 4, '  ', 2, 20)
    await documentoService.listar('normas', 4, '', 1, 20)

    expect(mock.history.get[0]?.url).toBe('/predios/4/atas')
    expect(mock.history.get[0]?.params).toEqual({ page: 2, pageSize: 20 })
    expect(mock.history.get[1]?.url).toBe('/predios/4/normas')
  })

  it('listar inclui o nome (sem espaços nas pontas) quando informado', async () => {
    mock.onGet('/predios/4/atas').reply(200, vazio)

    await documentoService.listar('atas', 4, ' assembleia ', 1, 20)

    expect(mock.history.get[0]?.params).toEqual({ page: 1, pageSize: 20, nome: 'assembleia' })
  })

  it('listar valida a resposta com o schema', async () => {
    mock.onGet('/predios/4/atas').reply(200, { items: [{ id: 1 }] })

    await expect(documentoService.listar('atas', 4, '', 1, 20)).rejects.toThrow(/nomeArquivo/)
  })

  it('enviar manda multipart com o campo "arquivo" na rota do tipo', async () => {
    mock.onPost('/predios/4/normas').reply(201, dto)
    const arquivo = new File(['%PDF-1.4'], 'regimento.pdf', { type: 'application/pdf' })

    const resultado = await documentoService.enviar('normas', 4, arquivo)

    const corpo = mock.history.post[0]?.data as FormData
    expect(corpo).toBeInstanceOf(FormData)
    expect((corpo.get('arquivo') as File).name).toBe('regimento.pdf')
    expect(resultado).toEqual(dto)
  })

  it('enviar não faz requisição se o arquivo for inválido', async () => {
    const arquivo = new File(['x'], 'planilha.xlsx')

    await expect(documentoService.enviar('atas', 4, arquivo)).rejects.toThrow(
      /Formato de arquivo não aceito/,
    )

    expect(mock.history.post).toHaveLength(0)
  })

  it('baixar pede o arquivo como blob e define o tipo pela extensão do nome, não pela resposta', async () => {
    mock.onGet('/predios/4/atas/1/download').reply(200, new Blob(['%PDF'], { type: 'text/html' }))

    const arquivo = await documentoService.baixar('atas', 4, 1, 'Ata 03.pdf')

    expect(mock.history.get[0]?.responseType).toBe('blob')
    expect(arquivo.type).toBe('application/pdf')
  })

  it('remover usa DELETE em /predios/{id}/{tipo}/{id}', async () => {
    mock.onDelete('/predios/4/normas/7').reply(204)

    await documentoService.remover('normas', 4, 7)

    expect(mock.history.delete[0]?.url).toBe('/predios/4/normas/7')
  })
})
