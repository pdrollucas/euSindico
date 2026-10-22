import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { relatorioService } from '@/services/relatorioService'
import type { GerarRelatorioFormValues, Relatorio } from '@/types/relatorio.types'

// Teto de pageSize do backend (RNF10).
const TAMANHO_PAGINA = 20

export const useRelatorioStore = defineStore('relatorio', () => {
  const relatorios = ref<Relatorio[]>([])
  const pagina = ref(1)
  const totalPaginas = ref(1)

  // Uma resposta antiga que chegue depois da nova (ex: gerar logo após abrir a tela) não pode
  // sobrescrever a lista.
  let ultimaRequisicao = 0

  const temMaisPaginas = computed(() => pagina.value < totalPaginas.value)

  async function listarPrimeiraPagina(predioId: number) {
    const requisicao = ++ultimaRequisicao
    const resultado = await relatorioService.listar(predioId, 1, TAMANHO_PAGINA)
    if (requisicao !== ultimaRequisicao) {
      return
    }
    relatorios.value = resultado.items
    pagina.value = resultado.page
    totalPaginas.value = resultado.totalPages
  }

  async function carregarProximaPagina(predioId: number) {
    if (!temMaisPaginas.value) {
      return
    }
    const requisicao = ultimaRequisicao
    const resultado = await relatorioService.listar(predioId, pagina.value + 1, TAMANHO_PAGINA)
    if (requisicao !== ultimaRequisicao) {
      return
    }
    relatorios.value = [...relatorios.value, ...resultado.items]
    pagina.value = resultado.page
    totalPaginas.value = resultado.totalPages
  }

  // Depois de gerar, recarrega a primeira página em vez de empilhar o item no topo: assim a ordem e
  // a paginação continuam as do backend (mais recente primeiro).
  async function gerar(predioId: number, payload: GerarRelatorioFormValues): Promise<Relatorio> {
    const relatorio = await relatorioService.gerar(predioId, payload)
    await listarPrimeiraPagina(predioId)
    return relatorio
  }

  async function baixar(predioId: number, id: number): Promise<Blob> {
    return relatorioService.baixar(predioId, id)
  }

  async function remover(predioId: number, id: number): Promise<void> {
    await relatorioService.remover(predioId, id)
    relatorios.value = relatorios.value.filter((item) => item.id !== id)
  }

  return {
    relatorios,
    temMaisPaginas,
    listarPrimeiraPagina,
    carregarProximaPagina,
    gerar,
    baixar,
    remover,
  }
})
