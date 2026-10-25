import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { compromissoService } from '@/services/compromissoService'
import { periodoAtual } from '@/utils/periodo'
import type {
  Compromisso,
  CompromissoFiltros,
  CompromissoFormValues,
} from '@/types/compromisso.types'

// Teto de pageSize do backend (RNF10).
const TAMANHO_PAGINA = 20

function filtrosPadrao(): CompromissoFiltros {
  // Próximos exige período; o padrão é o mês atual.
  const { ano, mes } = periodoAtual()
  return { titulo: '', situacao: 'proximos', escopo: 'meus', predioId: null, ano, mes }
}

export const useCompromissoStore = defineStore('compromisso', () => {
  const compromissos = ref<Compromisso[]>([])
  const pagina = ref(1)
  const totalPaginas = ref(1)
  const filtros = ref<CompromissoFiltros>(filtrosPadrao())
  const compromissoAtual = ref<Compromisso | null>(null)

  // A busca por título dispara uma requisição por digitação (com debounce, mas ainda assim): uma
  // resposta antiga que chegue depois da nova não pode sobrescrever a lista.
  let ultimaRequisicao = 0

  const temMaisPaginas = computed(() => pagina.value < totalPaginas.value)

  async function listarPrimeiraPagina() {
    const requisicao = ++ultimaRequisicao
    const resultado = await compromissoService.listar(filtros.value, 1, TAMANHO_PAGINA)
    if (requisicao !== ultimaRequisicao) {
      return
    }
    compromissos.value = resultado.items
    pagina.value = resultado.page
    totalPaginas.value = resultado.totalPages
  }

  async function carregarProximaPagina() {
    if (!temMaisPaginas.value) {
      return
    }
    const requisicao = ultimaRequisicao
    const resultado = await compromissoService.listar(
      filtros.value,
      pagina.value + 1,
      TAMANHO_PAGINA,
    )
    if (requisicao !== ultimaRequisicao) {
      return
    }
    compromissos.value = [...compromissos.value, ...resultado.items]
    pagina.value = resultado.page
    totalPaginas.value = resultado.totalPages
  }

  async function obter(id: number): Promise<Compromisso> {
    const compromisso = await compromissoService.obter(id)
    compromissoAtual.value = compromisso
    return compromisso
  }

  async function criar(payload: CompromissoFormValues): Promise<Compromisso> {
    return compromissoService.criar(payload)
  }

  async function atualizar(id: number, payload: CompromissoFormValues): Promise<Compromisso> {
    const compromisso = await compromissoService.atualizar(id, payload)
    compromissos.value = compromissos.value.map((item) => (item.id === id ? compromisso : item))
    compromissoAtual.value = compromisso
    return compromisso
  }

  async function alterarConclusao(id: number, concluido: boolean): Promise<Compromisso> {
    const compromisso = await compromissoService.alterarConclusao(id, concluido)
    compromissos.value = compromissos.value.map((item) => (item.id === id ? compromisso : item))
    if (compromissoAtual.value?.id === id) {
      compromissoAtual.value = compromisso
    }
    return compromisso
  }

  async function remover(id: number): Promise<void> {
    await compromissoService.remover(id)
    removerDaLista(id)
    if (compromissoAtual.value?.id === id) {
      compromissoAtual.value = null
    }
  }

  // Tira só da lista em memória — a view chama depois da animação de saída (concluir/reabrir faz
  // o item deixar de combinar com a situação filtrada).
  function removerDaLista(id: number) {
    compromissos.value = compromissos.value.filter((item) => item.id !== id)
  }

  function resetarFiltros() {
    filtros.value = filtrosPadrao()
  }

  function limparAtual() {
    compromissoAtual.value = null
  }

  return {
    compromissos,
    filtros,
    compromissoAtual,
    temMaisPaginas,
    listarPrimeiraPagina,
    carregarProximaPagina,
    obter,
    criar,
    atualizar,
    alterarConclusao,
    remover,
    removerDaLista,
    resetarFiltros,
    limparAtual,
  }
})
