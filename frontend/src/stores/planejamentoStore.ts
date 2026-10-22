import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { planejamentoService } from '@/services/planejamentoService'
import type {
  Planejamento,
  PlanejamentoFiltros,
  PlanejamentoFormValues,
} from '@/types/planejamento.types'

// Teto de pageSize do backend (RNF10).
const TAMANHO_PAGINA = 20

function filtrosPadrao(): PlanejamentoFiltros {
  return { titulo: '', orcamentoMinimo: null, orcamentoMaximo: null }
}

export const usePlanejamentoStore = defineStore('planejamento', () => {
  const planejamentos = ref<Planejamento[]>([])
  const pagina = ref(1)
  const totalPaginas = ref(1)
  const filtros = ref<PlanejamentoFiltros>(filtrosPadrao())
  const planejamentoAtual = ref<Planejamento | null>(null)

  // A busca por título dispara uma requisição por digitação (com debounce, mas ainda assim): uma
  // resposta antiga que chegue depois da nova não pode sobrescrever a lista.
  let ultimaRequisicao = 0

  const temMaisPaginas = computed(() => pagina.value < totalPaginas.value)

  async function listarPrimeiraPagina(predioId: number) {
    const requisicao = ++ultimaRequisicao
    const resultado = await planejamentoService.listar(predioId, filtros.value, 1, TAMANHO_PAGINA)
    if (requisicao !== ultimaRequisicao) {
      return
    }
    planejamentos.value = resultado.items
    pagina.value = resultado.page
    totalPaginas.value = resultado.totalPages
  }

  async function carregarProximaPagina(predioId: number) {
    if (!temMaisPaginas.value) {
      return
    }
    const requisicao = ultimaRequisicao
    const resultado = await planejamentoService.listar(
      predioId,
      filtros.value,
      pagina.value + 1,
      TAMANHO_PAGINA,
    )
    if (requisicao !== ultimaRequisicao) {
      return
    }
    planejamentos.value = [...planejamentos.value, ...resultado.items]
    pagina.value = resultado.page
    totalPaginas.value = resultado.totalPages
  }

  async function obter(predioId: number, id: number): Promise<Planejamento> {
    const planejamento = await planejamentoService.obter(predioId, id)
    planejamentoAtual.value = planejamento
    return planejamento
  }

  async function criar(predioId: number, payload: PlanejamentoFormValues): Promise<Planejamento> {
    return planejamentoService.criar(predioId, payload)
  }

  async function atualizar(
    predioId: number,
    id: number,
    payload: PlanejamentoFormValues,
  ): Promise<Planejamento> {
    const planejamento = await planejamentoService.atualizar(predioId, id, payload)
    planejamentos.value = planejamentos.value.map((item) => (item.id === id ? planejamento : item))
    planejamentoAtual.value = planejamento
    return planejamento
  }

  async function remover(predioId: number, id: number): Promise<void> {
    await planejamentoService.remover(predioId, id)
    planejamentos.value = planejamentos.value.filter((item) => item.id !== id)
    if (planejamentoAtual.value?.id === id) {
      planejamentoAtual.value = null
    }
  }

  function resetarFiltros() {
    filtros.value = filtrosPadrao()
  }

  function limparAtual() {
    planejamentoAtual.value = null
  }

  return {
    planejamentos,
    filtros,
    planejamentoAtual,
    temMaisPaginas,
    listarPrimeiraPagina,
    carregarProximaPagina,
    obter,
    criar,
    atualizar,
    remover,
    resetarFiltros,
    limparAtual,
  }
})
