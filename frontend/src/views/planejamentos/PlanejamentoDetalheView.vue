<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'
import AppHeader from '@/components/common/AppHeader.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import PlanejamentoForm from '@/components/planejamentos/PlanejamentoForm.vue'
import { usePlanejamentoStore } from '@/stores/planejamentoStore'
import { usePredioStore } from '@/stores/predioStore'
import { PAPEL_PREDIO } from '@/types/predio.types'
import type { PlanejamentoFormValues } from '@/types/planejamento.types'
import { numeroParaMoedaMascarada } from '@/utils/moeda'

// Uma tela só para criar (/predios/:id/planejamentos/novo) e para ver/editar
// (/predios/:id/planejamentos/:planejamentoId). O Colaborador a vê somente leitura (RN29).
const route = useRoute()
const router = useRouter()
const planejamentoStore = usePlanejamentoStore()
const predioStore = usePredioStore()

const modoNovo = computed(() => route.name === 'planejamentos-novo')
const predioId = computed(() => Number(route.params.id))
const planejamentoId = computed(() => Number(route.params.planejamentoId))
const destinoLista = computed(() => `/predios/${predioId.value}/planejamentos`)

const carregando = ref(true)
const erro = ref<string | null>(null)
const erroForm = ref<string | null>(null)
const salvando = ref(false)
const confirmandoRemocao = ref(false)
const removendo = ref(false)

const planejamento = computed(() => planejamentoStore.planejamentoAtual)
const podeGerenciar = computed(() => {
  const papel = predioStore.predioAtual?.papel
  return papel !== undefined && papel !== PAPEL_PREDIO.COLABORADOR
})

// Foto tirada uma única vez, depois da carga (mesmo motivo de CompromissoDetalheView).
const valoresIniciais = ref<Partial<PlanejamentoFormValues>>()

function tirarFotoInicial() {
  const atual = planejamento.value
  if (modoNovo.value || !atual) {
    return
  }
  valoresIniciais.value = {
    titulo: atual.titulo,
    dataPlanejamento: atual.dataPlanejamento ?? '',
    orcamentoPrevisto:
      atual.orcamentoPrevisto !== null ? numeroParaMoedaMascarada(atual.orcamentoPrevisto) : '',
    nomePrestadorServico: atual.nomePrestadorServico ?? '',
    detalhes: atual.detalhes ?? '',
  }
}

onMounted(async () => {
  try {
    await Promise.all([
      predioStore.obter(predioId.value),
      modoNovo.value
        ? Promise.resolve()
        : planejamentoStore.obter(predioId.value, planejamentoId.value),
    ])
    if (modoNovo.value && !podeGerenciar.value) {
      erro.value = 'Você não tem permissão para criar planejamentos neste prédio.'
      return
    }
    tirarFotoInicial()
  } catch (error) {
    erro.value = mensagemCarga(error)
  } finally {
    carregando.value = false
  }
})

onBeforeUnmount(() => planejamentoStore.limparAtual())

function mensagemCarga(error: unknown): string {
  const status = axios.isAxiosError(error) ? error.response?.status : undefined
  if (status === 404) {
    return 'Planejamento não encontrado.'
  }
  if (status === 403) {
    return 'Você não tem acesso a este planejamento.'
  }
  return 'Não foi possível carregar os dados do planejamento.'
}

// Um 400 pode vir da regra de data (title da exceção) ou do validator do backend (ValidationProblem,
// com o texto em `errors`) — mostra o que o backend disse em vez de presumir a causa.
interface Erro400 {
  title?: string
  errors?: Record<string, string[]>
}

function mensagemSalvar(error: unknown): string {
  const status = axios.isAxiosError<Erro400>(error) ? error.response?.status : undefined
  if (status === 400 && axios.isAxiosError<Erro400>(error)) {
    const dados = error.response?.data
    const primeiroErro = dados?.errors ? Object.values(dados.errors).flat()[0] : undefined
    return primeiroErro ?? dados?.title ?? 'Verifique os dados informados.'
  }
  if (status === 403) {
    return 'Você não tem permissão para fazer esta alteração.'
  }
  if (status === 404) {
    return 'Planejamento ou prédio não encontrado.'
  }
  return 'Não foi possível salvar o planejamento. Tente novamente.'
}

async function onSubmit(payload: PlanejamentoFormValues) {
  erroForm.value = null
  salvando.value = true
  try {
    if (modoNovo.value) {
      await planejamentoStore.criar(predioId.value, payload)
    } else {
      await planejamentoStore.atualizar(predioId.value, planejamentoId.value, payload)
    }
    router.push(destinoLista.value)
  } catch (error) {
    erroForm.value = mensagemSalvar(error)
  } finally {
    salvando.value = false
  }
}

async function confirmarRemocao() {
  removendo.value = true
  try {
    await planejamentoStore.remover(predioId.value, planejamentoId.value)
    router.push(destinoLista.value)
  } catch {
    confirmandoRemocao.value = false
    erroForm.value = 'Não foi possível remover o planejamento. Tente novamente.'
  } finally {
    removendo.value = false
  }
}
</script>

<template>
  <v-container class="planejamento-detalhe d-flex flex-column">
    <AppHeader :titulo="modoNovo ? 'Novo planejamento' : 'Planejamento'" />

    <v-alert
      v-if="erro"
      type="error"
      variant="tonal"
      density="compact"
      class="mb-4"
      data-cy="erro-planejamento-detalhe"
    >
      {{ erro }}
    </v-alert>

    <v-progress-circular
      v-if="carregando"
      indeterminate
      color="primary"
      class="align-self-center mt-8"
    />

    <template v-else-if="!erro">
      <v-alert
        v-if="erroForm"
        type="error"
        variant="tonal"
        density="compact"
        class="mb-4"
        data-cy="erro-planejamento-form"
      >
        {{ erroForm }}
      </v-alert>

      <PlanejamentoForm
        :valores-iniciais="valoresIniciais"
        :salvando="salvando"
        :modo-edicao="!modoNovo"
        :somente-leitura="!podeGerenciar"
        @submit="onSubmit"
      />

      <v-btn
        v-if="!modoNovo && podeGerenciar"
        variant="text"
        color="error"
        prepend-icon="mdi-trash-can-outline"
        class="align-self-center mb-4"
        data-cy="btn-remover-planejamento"
        @click="confirmandoRemocao = true"
      >
        Remover planejamento
      </v-btn>
    </template>

    <ConfirmDialog
      v-model="confirmandoRemocao"
      titulo="Remover planejamento"
      :mensagem="`Tem certeza que deseja remover ${planejamento?.titulo ?? 'este planejamento'}? Essa ação não pode ser desfeita.`"
      texto-confirmar="Remover"
      cor-confirmar="error"
      :carregando="removendo"
      @confirmar="confirmarRemocao"
    />
  </v-container>
</template>

<style scoped>
.planejamento-detalhe {
  max-width: 480px;
  margin-inline: auto;
}
</style>
