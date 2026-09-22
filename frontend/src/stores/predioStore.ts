import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { predioService } from '@/services/predioService'
import type { Predio, PredioFormValues } from '@/types/predio.types'

// Teto de pageSize do backend (RNF10).
const TAMANHO_PAGINA = 20

export const usePredioStore = defineStore('predio', () => {
  const predios = ref<Predio[]>([])
  const pagina = ref(1)
  const totalPaginas = ref(1)
  const predioAtual = ref<Predio | null>(null)

  const temMaisPaginas = computed(() => pagina.value < totalPaginas.value)

  async function listarPrimeiraPagina() {
    const resultado = await predioService.listar(1, TAMANHO_PAGINA)
    predios.value = resultado.items
    pagina.value = resultado.page
    totalPaginas.value = resultado.totalPages
  }

  async function carregarProximaPagina() {
    if (!temMaisPaginas.value) {
      return
    }
    const resultado = await predioService.listar(pagina.value + 1, TAMANHO_PAGINA)
    predios.value = [...predios.value, ...resultado.items]
    pagina.value = resultado.page
    totalPaginas.value = resultado.totalPages
  }

  async function obter(id: number): Promise<Predio> {
    const predio = await predioService.obter(id)
    predioAtual.value = predio
    return predio
  }

  async function criar(payload: PredioFormValues): Promise<Predio> {
    const predio = await predioService.criar(payload)
    predios.value = [...predios.value, predio]
    return predio
  }

  async function atualizar(id: number, payload: PredioFormValues): Promise<Predio> {
    const predio = await predioService.atualizar(id, payload)
    predios.value = predios.value.map((item) => (item.id === id ? predio : item))
    if (predioAtual.value?.id === id) {
      predioAtual.value = predio
    }
    return predio
  }

  async function remover(id: number): Promise<void> {
    await predioService.remover(id)
    predios.value = predios.value.filter((item) => item.id !== id)
    if (predioAtual.value?.id === id) {
      predioAtual.value = null
    }
  }

  function limparAtual() {
    predioAtual.value = null
  }

  return {
    predios,
    predioAtual,
    temMaisPaginas,
    listarPrimeiraPagina,
    carregarProximaPagina,
    obter,
    criar,
    atualizar,
    remover,
    limparAtual,
  }
})
