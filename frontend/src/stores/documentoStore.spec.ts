import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useDocumentoStore } from './documentoStore'
import { documentoService } from '@/services/documentoService'
import type { Documento } from '@/types/documento.types'

vi.mock('@/services/documentoService', () => ({
  documentoService: {
    listar: vi.fn<typeof documentoService.listar>(),
    enviar: vi.fn<typeof documentoService.enviar>(),
    baixar: vi.fn<typeof documentoService.baixar>(),
    remover: vi.fn<typeof documentoService.remover>(),
  },
}))

const ata: Documento = {
  id: 1,
  nomeArquivo: 'Ata 03.pdf',
  predioId: 4,
  predioNome: 'Edifício Solar',
  criadoEm: '2026-10-01T12:00:00',
}
const outraAta = { ...ata, id: 2, nomeArquivo: 'Ata 02.pdf' }
const norma = { ...ata, id: 3, nomeArquivo: 'Regimento.pdf' }

function pagina(items: Documento[], page = 1, totalPages = 1) {
  return { items, page, pageSize: 20, totalCount: items.length, totalPages }
}

describe('useDocumentoStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('começa com as duas listas vazias', () => {
    const store = useDocumentoStore()

    expect(store.listas.atas.documentos).toEqual([])
    expect(store.listas.normas.documentos).toEqual([])
    expect(store.temMaisPaginas('atas')).toBe(false)
  })

  it('listarPrimeiraPagina envia tipo, prédio e busca e popula só a lista daquele tipo', async () => {
    vi.mocked(documentoService.listar).mockResolvedValue(pagina([ata]))
    const store = useDocumentoStore()

    await store.listarPrimeiraPagina('atas', 4, 'assembleia')

    expect(documentoService.listar).toHaveBeenCalledWith('atas', 4, 'assembleia', 1, 20)
    expect(store.listas.atas.documentos).toEqual([ata])
    expect(store.listas.normas.documentos).toEqual([])
  })

  it('as listas de atas e normas não se misturam', async () => {
    vi.mocked(documentoService.listar).mockResolvedValueOnce(pagina([ata]))
    vi.mocked(documentoService.listar).mockResolvedValueOnce(pagina([norma]))
    const store = useDocumentoStore()

    await store.listarPrimeiraPagina('atas', 4)
    await store.listarPrimeiraPagina('normas', 4)

    expect(store.listas.atas.documentos).toEqual([ata])
    expect(store.listas.normas.documentos).toEqual([norma])
  })

  it('carregarProximaPagina concatena e continua na mesma busca', async () => {
    vi.mocked(documentoService.listar).mockResolvedValueOnce(pagina([ata], 1, 2))
    vi.mocked(documentoService.listar).mockResolvedValueOnce(pagina([outraAta], 2, 2))
    const store = useDocumentoStore()
    await store.listarPrimeiraPagina('atas', 4, 'ata')
    expect(store.temMaisPaginas('atas')).toBe(true)

    await store.carregarProximaPagina('atas', 4)

    expect(documentoService.listar).toHaveBeenLastCalledWith('atas', 4, 'ata', 2, 20)
    expect(store.listas.atas.documentos).toEqual([ata, outraAta])
    expect(store.temMaisPaginas('atas')).toBe(false)
  })

  it('carregarProximaPagina não faz nada na última página', async () => {
    vi.mocked(documentoService.listar).mockResolvedValue(pagina([ata]))
    const store = useDocumentoStore()
    await store.listarPrimeiraPagina('atas', 4)
    vi.mocked(documentoService.listar).mockClear()

    await store.carregarProximaPagina('atas', 4)

    expect(documentoService.listar).not.toHaveBeenCalled()
  })

  it('ignora a resposta antiga que chega depois de uma requisição mais nova (busca digitada)', async () => {
    let resolverAntiga: (valor: ReturnType<typeof pagina>) => void = () => undefined
    vi.mocked(documentoService.listar).mockReturnValueOnce(
      new Promise((resolve) => {
        resolverAntiga = resolve
      }),
    )
    vi.mocked(documentoService.listar).mockResolvedValueOnce(pagina([ata]))
    const store = useDocumentoStore()

    const antiga = store.listarPrimeiraPagina('atas', 4, 'a')
    await store.listarPrimeiraPagina('atas', 4, 'ata')
    resolverAntiga(pagina([outraAta]))
    await antiga

    expect(store.listas.atas.documentos).toEqual([ata])
    expect(store.listas.atas.nome).toBe('ata')
  })

  it('uma resposta pendente de normas não é descartada por uma requisição de atas', async () => {
    let resolverNormas: (valor: ReturnType<typeof pagina>) => void = () => undefined
    vi.mocked(documentoService.listar).mockReturnValueOnce(
      new Promise((resolve) => {
        resolverNormas = resolve
      }),
    )
    vi.mocked(documentoService.listar).mockResolvedValueOnce(pagina([ata]))
    const store = useDocumentoStore()

    const normas = store.listarPrimeiraPagina('normas', 4)
    await store.listarPrimeiraPagina('atas', 4)
    resolverNormas(pagina([norma]))
    await normas

    expect(store.listas.normas.documentos).toEqual([norma])
  })

  it('enviar chama o service e recarrega a primeira página sem busca', async () => {
    const arquivo = new File(['%PDF-1.4'], 'ata.pdf')
    vi.mocked(documentoService.enviar).mockResolvedValue(ata)
    vi.mocked(documentoService.listar).mockResolvedValue(pagina([ata, outraAta]))
    const store = useDocumentoStore()

    const resultado = await store.enviar('atas', 4, arquivo)

    expect(documentoService.enviar).toHaveBeenCalledWith('atas', 4, arquivo)
    expect(documentoService.listar).toHaveBeenCalledWith('atas', 4, '', 1, 20)
    expect(resultado).toEqual(ata)
    expect(store.listas.atas.documentos).toEqual([ata, outraAta])
  })

  it('enviar com erro (ex: limite atingido) propaga e não recarrega a lista', async () => {
    vi.mocked(documentoService.enviar).mockRejectedValue(new Error('409'))
    const store = useDocumentoStore()

    await expect(store.enviar('normas', 4, new File(['x'], 'n.pdf'))).rejects.toThrow('409')

    expect(documentoService.listar).not.toHaveBeenCalled()
  })

  it('baixar devolve o arquivo do service sem mexer na lista', async () => {
    const arquivo = new Blob(['%PDF'], { type: 'application/pdf' })
    vi.mocked(documentoService.baixar).mockResolvedValue(arquivo)
    const store = useDocumentoStore()

    expect(await store.baixar('atas', 4, 1, 'Ata 03.pdf')).toBe(arquivo)
    expect(documentoService.baixar).toHaveBeenCalledWith('atas', 4, 1, 'Ata 03.pdf')
  })

  it('remover tira o item só da lista do tipo e só depois de o backend confirmar', async () => {
    vi.mocked(documentoService.listar).mockResolvedValueOnce(pagina([ata, outraAta]))
    vi.mocked(documentoService.listar).mockResolvedValueOnce(pagina([norma]))
    vi.mocked(documentoService.remover).mockResolvedValue()
    const store = useDocumentoStore()
    await store.listarPrimeiraPagina('atas', 4)
    await store.listarPrimeiraPagina('normas', 4)

    await store.remover('atas', 4, 1)

    expect(documentoService.remover).toHaveBeenCalledWith('atas', 4, 1)
    expect(store.listas.atas.documentos).toEqual([outraAta])
    expect(store.listas.normas.documentos).toEqual([norma])
  })

  it('remover com erro mantém o item na lista', async () => {
    vi.mocked(documentoService.listar).mockResolvedValue(pagina([ata]))
    vi.mocked(documentoService.remover).mockRejectedValue(new Error('403'))
    const store = useDocumentoStore()
    await store.listarPrimeiraPagina('atas', 4)

    await expect(store.remover('atas', 4, 1)).rejects.toThrow('403')

    expect(store.listas.atas.documentos).toEqual([ata])
  })
})
