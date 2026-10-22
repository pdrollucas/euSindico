<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router'
import AppHeader from '@/components/common/AppHeader.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import PlanejamentoListItem from '@/components/planejamentos/PlanejamentoListItem.vue'
import PlanejamentoFiltrosSheet from '@/components/planejamentos/PlanejamentoFiltrosSheet.vue'
import { usePlanejamentoStore } from '@/stores/planejamentoStore'
import { usePredioStore } from '@/stores/predioStore'
import { PAPEL_PREDIO } from '@/types/predio.types'
import type { Planejamento, PlanejamentoFiltros } from '@/types/planejamento.types'
import { formatarMoeda } from '@/utils/moeda'

const route = useRoute()
const router = useRouter()
const planejamentoStore = usePlanejamentoStore()
const predioStore = usePredioStore()

const predioId = computed(() => Number(route.params.id))
const filtros = computed(() => planejamentoStore.filtros)
// Criar, editar e remover são de Síndico/Gestor (RN29); o Colaborador só lê. Enquanto o prédio não
// carrega o papel é desconhecido, então nada de escrita aparece.
const podeGerenciar = computed(() => {
  const papel = predioStore.predioAtual?.papel
  return papel !== undefined && papel !== PAPEL_PREDIO.COLABORADOR
})

const busca = ref<string | null>(planejamentoStore.filtros.titulo)
const carregando = ref(false)
const erro = ref<string | null>(null)
const mostrarFiltros = ref(false)
const paraRemover = ref<Planejamento | null>(null)
const removendo = ref(false)

const snackbar = ref<{ aberto: boolean; mensagem: string; cor?: string }>({
  aberto: false,
  mensagem: '',
})

const filtrosAtivos = computed(
  () => Number(filtros.value.orcamentoMinimo !== null) + Number(filtros.value.orcamentoMaximo !== null),
)

const rotuloOrcamento = computed(() => {
  const { orcamentoMinimo: minimo, orcamentoMaximo: maximo } = filtros.value
  if (minimo !== null && maximo !== null) {
    return `${formatarMoeda(minimo)} – ${formatarMoeda(maximo)}`
  }
  if (minimo !== null) {
    return `A partir de ${formatarMoeda(minimo)}`
  }
  return maximo !== null ? `Até ${formatarMoeda(maximo)}` : ''
})

const mensagemVazio = computed(() => {
  if ((busca.value ?? '').trim() || filtrosAtivos.value > 0) {
    return 'Nenhum planejamento encontrado.'
  }
  return podeGerenciar.value
    ? 'Este prédio ainda não tem planejamentos. Adicione o primeiro!'
    : 'Este prédio ainda não tem planejamentos.'
})

onMounted(async () => {
  // O prédio dá o papel (e o título); a lista carrega em paralelo, e uma falha no prédio não a
  // impede de aparecer — sem papel, a tela fica só de leitura.
  predioStore.obter(predioId.value).catch(() => undefined)
  await carregar()
})

// Mantém os filtros ao ir para novo/detalhe e voltar; qualquer outro destino (prédio, Home…) zera.
onBeforeRouteLeave((to) => {
  if (!to.path.startsWith(`/predios/${predioId.value}/planejamentos`)) {
    planejamentoStore.resetarFiltros()
  }
})

// Debounce da busca: a requisição só sai depois de o usuário parar de digitar. Limpar o campo (X) e
// Enter buscam na hora — são ações deliberadas, não digitação em andamento.
const ATRASO_BUSCA_MS = 500
let timerBusca: ReturnType<typeof setTimeout> | undefined

function aplicarBusca() {
  clearTimeout(timerBusca)
  // `clearable` do Vuetify põe `null` no v-model ao clicar no X.
  const texto = (busca.value ?? '').trim()
  if (texto !== planejamentoStore.filtros.titulo) {
    planejamentoStore.filtros.titulo = texto
    carregar()
  }
}

watch(busca, (valor) => {
  clearTimeout(timerBusca)
  if (!(valor ?? '').trim()) {
    aplicarBusca()
    return
  }
  timerBusca = setTimeout(aplicarBusca, ATRASO_BUSCA_MS)
})
onBeforeUnmount(() => clearTimeout(timerBusca))

async function carregar() {
  erro.value = null
  carregando.value = true
  try {
    await planejamentoStore.listarPrimeiraPagina(predioId.value)
  } catch {
    erro.value = 'Não foi possível carregar os planejamentos.'
  } finally {
    carregando.value = false
  }
}

async function carregarMais() {
  try {
    await planejamentoStore.carregarProximaPagina(predioId.value)
  } catch {
    erro.value = 'Não foi possível carregar mais planejamentos.'
  }
}

function aplicarFiltros(parcial: Partial<PlanejamentoFiltros>) {
  Object.assign(planejamentoStore.filtros, parcial)
  carregar()
}

function limparFiltros() {
  busca.value = ''
  planejamentoStore.resetarFiltros()
  carregar()
}

function avisar(mensagem: string, cor?: string) {
  snackbar.value = { aberto: true, mensagem, cor }
}

async function confirmarRemocao() {
  if (!paraRemover.value) {
    return
  }
  removendo.value = true
  try {
    await planejamentoStore.remover(predioId.value, paraRemover.value.id)
    paraRemover.value = null
    avisar('Planejamento removido')
  } catch {
    paraRemover.value = null
    avisar('Não foi possível remover o planejamento. Tente novamente.', 'error')
  } finally {
    removendo.value = false
  }
}

function criar() {
  router.push(`/predios/${predioId.value}/planejamentos/novo`)
}
</script>

<template>
  <v-container class="planejamentos-list d-flex flex-column fill-height">
    <AppHeader />

    <div class="planejamentos-busca d-flex align-center ga-2">
      <v-text-field
        v-model="busca"
        data-cy="busca-planejamento"
        @keydown.enter="aplicarBusca"
        placeholder="Buscar planejamento..."
        prepend-inner-icon="mdi-magnify"
        density="compact"
        hide-details
        single-line
        clearable
      />
      <v-badge :model-value="filtrosAtivos > 0" dot color="primary">
        <v-btn
          icon="mdi-filter-variant"
          variant="text"
          aria-label="Filtros"
          data-cy="btn-filtros"
          @click="mostrarFiltros = true"
        />
      </v-badge>
    </div>

    <!-- Um chip para a faixa de orçamento quando ativa; fechar remove os dois limites. -->
    <div v-if="filtrosAtivos > 0" class="d-flex flex-wrap ga-2 mb-3">
      <v-chip
        closable
        color="primary"
        variant="tonal"
        data-cy="chip-orcamento"
        @click:close="aplicarFiltros({ orcamentoMinimo: null, orcamentoMaximo: null })"
      >
        {{ rotuloOrcamento }}
      </v-chip>
    </div>

    <v-alert
      v-if="erro"
      type="error"
      variant="tonal"
      density="compact"
      class="mb-4"
      data-cy="erro-planejamentos"
    >
      {{ erro }}
    </v-alert>

    <v-progress-circular
      v-if="carregando"
      indeterminate
      color="primary"
      class="align-self-center mt-8"
    />

    <template v-else>
      <p
        v-if="planejamentoStore.planejamentos.length === 0"
        class="text-center text-medium-emphasis mt-8"
        data-cy="planejamentos-vazio"
      >
        {{ mensagemVazio }}
      </p>

      <div v-else class="flex-grow-1">
        <PlanejamentoListItem
          v-for="planejamento in planejamentoStore.planejamentos"
          :key="planejamento.id"
          :planejamento="planejamento"
          :pode-remover="podeGerenciar"
          class="mb-3"
          @remover="paraRemover = planejamento"
        />

        <v-btn
          v-if="planejamentoStore.temMaisPaginas"
          variant="text"
          color="primary"
          block
          data-cy="btn-carregar-mais"
          @click="carregarMais"
        >
          Carregar mais
        </v-btn>
      </div>
    </template>

    <v-btn
      v-if="podeGerenciar"
      icon="mdi-plus"
      color="primary"
      size="large"
      class="planejamentos-fab"
      aria-label="Adicionar planejamento"
      data-cy="btn-criar-planejamento"
      @click="criar"
    />

    <PlanejamentoFiltrosSheet
      v-model="mostrarFiltros"
      :filtros="filtros"
      @alterar="aplicarFiltros"
      @limpar="limparFiltros"
    />

    <ConfirmDialog
      :model-value="paraRemover !== null"
      titulo="Remover planejamento"
      :mensagem="`Tem certeza que deseja remover ${paraRemover?.titulo ?? 'este planejamento'}? Essa ação não pode ser desfeita.`"
      texto-confirmar="Remover"
      cor-confirmar="error"
      :carregando="removendo"
      @update:model-value="paraRemover = null"
      @confirmar="confirmarRemocao"
    />

    <v-snackbar v-model="snackbar.aberto" :color="snackbar.cor" :timeout="5000" data-cy="snackbar">
      {{ snackbar.mensagem }}
    </v-snackbar>
  </v-container>
</template>

<style scoped>
.planejamentos-list {
  max-width: 480px;
  margin-inline: auto;
}

/* Gruda logo abaixo do AppHeader — mesmo truque de CompromissosListView.vue (margin-top negativo
   cancela o mb-2 do AppHeader, o respiro volta como padding-top). */
.planejamentos-busca {
  position: sticky;
  top: var(--app-header-height);
  z-index: 1;
  flex: none;
  margin-top: -8px;
  background: rgb(var(--v-theme-background));
  padding-top: 8px;
  padding-bottom: 16px;
}

.planejamentos-fab {
  position: fixed;
  right: 24px;
  bottom: 24px;
}
</style>
