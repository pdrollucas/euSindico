import { reactive } from 'vue'
import { defineStore } from 'pinia'
import { documentoService } from '@/services/documentoService'
import type { Documento, TipoDocumento } from '@/types/documento.types'

// Teto de pageSize do backend (RNF10).
const TAMANHO_PAGINA = 20

interface ListaDocumentos {
  documentos: Documento[]
  pagina: number
  totalPaginas: number
  // Busca da última primeira página carregada: "Carregar mais" continua nela.
  nome: string
}

function listaVazia(): ListaDocumentos {
  return { documentos: [], pagina: 1, totalPaginas: 1, nome: '' }
}

// Atas e normas são telas separadas, mas têm o mesmo comportamento: uma lista por tipo, para a
// lista de um não aparecer (nem ser sobrescrita) enquanto se mexe no outro.
export const useDocumentoStore = defineStore('documento', () => {
  const listas = reactive<Record<TipoDocumento, ListaDocumentos>>({
    atas: listaVazia(),
    normas: listaVazia(),
  })

  // A busca dispara uma requisição por digitação (com debounce, mas ainda assim): uma resposta
  // antiga que chegue depois da nova não pode sobrescrever a lista.
  const ultimaRequisicao: Record<TipoDocumento, number> = { atas: 0, normas: 0 }

  function temMaisPaginas(tipo: TipoDocumento): boolean {
    return listas[tipo].pagina < listas[tipo].totalPaginas
  }

  async function listarPrimeiraPagina(tipo: TipoDocumento, predioId: number, nome = '') {
    const requisicao = ++ultimaRequisicao[tipo]
    const resultado = await documentoService.listar(tipo, predioId, nome, 1, TAMANHO_PAGINA)
    if (requisicao !== ultimaRequisicao[tipo]) {
      return
    }
    listas[tipo] = {
      documentos: resultado.items,
      pagina: resultado.page,
      totalPaginas: resultado.totalPages,
      nome,
    }
  }

  async function carregarProximaPagina(tipo: TipoDocumento, predioId: number) {
    if (!temMaisPaginas(tipo)) {
      return
    }
    const requisicao = ultimaRequisicao[tipo]
    const { nome, pagina } = listas[tipo]
    const resultado = await documentoService.listar(
      tipo,
      predioId,
      nome,
      pagina + 1,
      TAMANHO_PAGINA,
    )
    if (requisicao !== ultimaRequisicao[tipo]) {
      return
    }
    listas[tipo] = {
      documentos: [...listas[tipo].documentos, ...resultado.items],
      pagina: resultado.page,
      totalPaginas: resultado.totalPages,
      nome,
    }
  }

  // Depois de enviar, recarrega a primeira página (sem busca) em vez de empilhar o item no topo:
  // assim a ordem e a paginação continuam as do backend (mais recente primeiro).
  async function enviar(tipo: TipoDocumento, predioId: number, arquivo: File): Promise<Documento> {
    const documento = await documentoService.enviar(tipo, predioId, arquivo)
    await listarPrimeiraPagina(tipo, predioId)
    return documento
  }

  async function baixar(
    tipo: TipoDocumento,
    predioId: number,
    id: number,
    nomeArquivo: string,
  ): Promise<Blob> {
    return documentoService.baixar(tipo, predioId, id, nomeArquivo)
  }

  async function remover(tipo: TipoDocumento, predioId: number, id: number): Promise<void> {
    await documentoService.remover(tipo, predioId, id)
    listas[tipo].documentos = listas[tipo].documentos.filter((item) => item.id !== id)
  }

  return {
    listas,
    temMaisPaginas,
    listarPrimeiraPagina,
    carregarProximaPagina,
    enviar,
    baixar,
    remover,
  }
})
