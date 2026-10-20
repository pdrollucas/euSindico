<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router'
import AppHeader from '@/components/common/AppHeader.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import CompromissoListItem from '@/components/compromissos/CompromissoListItem.vue'
import type { EstadoItem } from '@/components/compromissos/CompromissoListItem.vue'
import CompromissoFiltrosSheet from '@/components/compromissos/CompromissoFiltrosSheet.vue'
import { useCompromissoStore } from '@/stores/compromissoStore'
import { usePredioStore } from '@/stores/predioStore'
import { PAPEL_PREDIO } from '@/types/predio.types'
import { SITUACOES_COMPROMISSO } from '@/types/compromisso.types'
import type { Compromisso, CompromissoFiltros } from '@/types/compromisso.types'
import { SAIDA_CONCLUIR_MS, SAIDA_REABRIR_MS, reduzirMovimento } from '@/utils/animacao'
import { lerPredioIdDaQuery } from '@/utils/rotasCompromisso'

const route = useRoute()
const router = useRouter()
const compromissoStore = useCompromissoStore()
const predioStore = usePredioStore()

const busca = ref<string | null>(compromissoStore.filtros.titulo)
const carregando = ref(false)
const erro = ref<string | null>(null)
const mostrarFiltros = ref(false)

// Estado visual de cada item que está saindo da lista (animação de concluir/reabrir).
const estados = ref<Record<number, EstadoItem>>({})
const paraRemover = ref<Compromisso | null>(null)
const removendo = ref(false)

const snackbar = ref<{
  aberto: boolean
  mensagem: string
  cor?: string
  desfazer?: () => void
}>({ aberto: false, mensagem: '' })

// O prédio filtrado vem da URL (?predioId=), que é como a tela do prédio abre esta lista — a
// URL é a fonte única, a store só espelha para montar a consulta.
const predioIdRota = computed(() => lerPredioIdDaQuery(route.query.predioId))
const filtros = computed(() => compromissoStore.filtros)

const predioFiltrado = computed(() =>
  predioStore.predios.find((predio) => predio.id === filtros.value.predioId),
)

// RF34/RN21: Colaborador só vê os próprios, então o seletor "meus/todos" fica de fora quando o
// contexto (um prédio, ou todos) não tem nenhum prédio onde ele seja Síndico/Gestor.
const mostrarEscopo = computed(() => {
  if (predioFiltrado.value) {
    return predioFiltrado.value.papel !== PAPEL_PREDIO.COLABORADOR
  }
  return predioStore.predios.some((predio) => predio.papel !== PAPEL_PREDIO.COLABORADOR)
})

const rotuloSituacao = computed(
  () => SITUACOES_COMPROMISSO.find((s) => s.valor === filtros.value.situacao)?.rotulo ?? '',
)

const filtrosAtivos = computed(
  () =>
    Number(filtros.value.situacao !== 'proximos') +
    Number(filtros.value.predioId !== null) +
    Number(filtros.value.escopo !== 'meus'),
)

const mostrarResponsavel = computed(() => filtros.value.escopo === 'todos')

const mensagemVazio = computed(() => {
  if ((busca.value ?? '').trim() || filtrosAtivos.value > 0) {
    return 'Nenhum compromisso encontrado.'
  }
  return 'Você não tem compromissos próximos. Adicione o primeiro!'
})

// Só recarrega por mudança de URL depois da carga inicial (o watcher abaixo roda já no setup).
let montado = false

onMounted(async () => {
  // Prédios alimentam o filtro; uma falha aqui não impede a lista de aparecer.
  predioStore.carregarTodas().catch(() => undefined)
  await carregar()
  montado = true
})

// Mantém os filtros ao ir para novo/detalhe e voltar; qualquer outro destino (Home, prédio…) zera.
onBeforeRouteLeave((to) => {
  if (!to.path.startsWith('/compromissos')) {
    compromissoStore.resetarFiltros()
  }
})

watch(
  predioIdRota,
  (predioId) => {
    const mudou = compromissoStore.filtros.predioId !== predioId
    compromissoStore.filtros.predioId = predioId
    if (mudou && montado) {
      carregar()
    }
  },
  { immediate: true },
)

// Debounce da busca: a requisição só sai depois de o usuário parar de digitar, para não gerar uma
// chamada por tecla (a soma de muitos usuários digitando letra a letra pesa no backend). Limpar o
// campo (X) e Enter buscam na hora — são ações deliberadas, não digitação em andamento.
const ATRASO_BUSCA_MS = 500
let timerBusca: ReturnType<typeof setTimeout> | undefined

function aplicarBusca() {
  clearTimeout(timerBusca)
  // `clearable` do Vuetify põe `null` no v-model ao clicar no X.
  const texto = (busca.value ?? '').trim()
  if (texto !== compromissoStore.filtros.titulo) {
    compromissoStore.filtros.titulo = texto
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

// `silencioso` evita o spinner ao recarregar depois de um "Desfazer".
async function carregar(silencioso = false) {
  erro.value = null
  carregando.value = !silencioso
  try {
    await compromissoStore.listarPrimeiraPagina()
  } catch {
    erro.value = 'Não foi possível carregar os compromissos.'
  } finally {
    carregando.value = false
  }
}

async function carregarMais() {
  try {
    await compromissoStore.carregarProximaPagina()
  } catch {
    erro.value = 'Não foi possível carregar mais compromissos.'
  }
}

function aplicarFiltros(parcial: Partial<CompromissoFiltros>) {
  const { predioId, ...resto } = parcial
  Object.assign(compromissoStore.filtros, resto)
  if ('predioId' in parcial) {
    // A URL manda: o watcher de `predioIdRota` aplica na store e recarrega.
    router.replace({ query: predioId ? { predioId: String(predioId) } : {} })
  } else {
    carregar()
  }
}

function limparFiltros() {
  busca.value = ''
  compromissoStore.resetarFiltros()
  if (predioIdRota.value) {
    router.replace({ query: {} })
  } else {
    carregar()
  }
}

function avisar(mensagem: string, opcoes: { cor?: string; desfazer?: () => void } = {}) {
  snackbar.value = { aberto: true, mensagem, ...opcoes }
}

function esperar(ms: number) {
  return new Promise<void>((resolve) => setTimeout(resolve, ms))
}

// Concluir/reabrir: o item anima (riscado + fade) em paralelo ao PATCH; só sai da lista quando as
// duas coisas terminam. Concluído nunca combina com a lista padrão ("próximos"), e reaberto nunca
// combina com "concluídos" — por isso o item sempre sai.
async function alternarConclusao(compromisso: Compromisso, concluido: boolean) {
  estados.value = { ...estados.value, [compromisso.id]: concluido ? 'concluindo' : 'reabrindo' }
  const duracao = reduzirMovimento() ? 0 : concluido ? SAIDA_CONCLUIR_MS : SAIDA_REABRIR_MS
  try {
    await Promise.all([
      compromissoStore.alterarConclusao(compromisso.id, concluido),
      esperar(duracao),
    ])
    compromissoStore.removerDaLista(compromisso.id)
    avisar(concluido ? 'Compromisso concluído' : 'Compromisso reaberto', {
      desfazer: () => desfazerConclusao(compromisso.id, !concluido),
    })
  } catch {
    avisar('Não foi possível atualizar o compromisso. Tente novamente.', { cor: 'error' })
  } finally {
    const restantes = { ...estados.value }
    delete restantes[compromisso.id]
    estados.value = restantes
  }
}

async function desfazerConclusao(id: number, concluido: boolean) {
  snackbar.value.aberto = false
  try {
    await compromissoStore.alterarConclusao(id, concluido)
    await carregar(true)
  } catch {
    avisar('Não foi possível desfazer. Tente novamente.', { cor: 'error' })
  }
}

async function confirmarRemocao() {
  if (!paraRemover.value) {
    return
  }
  removendo.value = true
  try {
    await compromissoStore.remover(paraRemover.value.id)
    paraRemover.value = null
    avisar('Compromisso removido')
  } catch {
    paraRemover.value = null
    avisar('Não foi possível remover o compromisso. Tente novamente.', { cor: 'error' })
  } finally {
    removendo.value = false
  }
}

function criar() {
  router.push({
    path: '/compromissos/novo',
    query: predioIdRota.value ? { predioId: String(predioIdRota.value) } : {},
  })
}
</script>

<template>
  <v-container class="compromissos-list d-flex flex-column fill-height">
    <AppHeader />

    <div class="compromissos-busca d-flex align-center ga-2">
      <v-text-field
        v-model="busca"
        data-cy="busca-compromisso"
        @keydown.enter="aplicarBusca"
        placeholder="Buscar compromisso..."
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

    <!-- Um chip por filtro fora do padrão; fechar volta o filtro ao padrão (situação "próximos",
         escopo "meus", todos os prédios). -->
    <div
      v-if="predioFiltrado || filtros.situacao !== 'proximos' || filtros.escopo !== 'meus'"
      class="d-flex flex-wrap ga-2 mb-3"
    >
      <v-chip
        v-if="predioFiltrado"
        closable
        color="primary"
        variant="tonal"
        data-cy="chip-predio"
        @click:close="aplicarFiltros({ predioId: null })"
      >
        {{ predioFiltrado.nome }}
      </v-chip>
      <v-chip
        v-if="filtros.situacao !== 'proximos'"
        closable
        color="primary"
        variant="tonal"
        data-cy="chip-situacao"
        @click:close="aplicarFiltros({ situacao: 'proximos' })"
      >
        {{ rotuloSituacao }}
      </v-chip>
      <v-chip
        v-if="filtros.escopo !== 'meus'"
        closable
        color="primary"
        variant="tonal"
        data-cy="chip-escopo"
        @click:close="aplicarFiltros({ escopo: 'meus' })"
      >
        Todos
      </v-chip>
    </div>

    <v-alert
      v-if="erro"
      type="error"
      variant="tonal"
      density="compact"
      class="mb-4"
      data-cy="erro-compromissos"
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
        v-if="compromissoStore.compromissos.length === 0"
        class="text-center text-medium-emphasis mt-8"
        data-cy="compromissos-vazio"
      >
        {{ mensagemVazio }}
      </p>

      <div v-else class="flex-grow-1">
        <TransitionGroup name="lista" tag="div" class="compromissos-itens">
          <CompromissoListItem
            v-for="compromisso in compromissoStore.compromissos"
            :key="compromisso.id"
            :compromisso="compromisso"
            :estado="estados[compromisso.id] ?? 'normal'"
            :mostrar-responsavel="mostrarResponsavel"
            :predio-id-filtro="predioIdRota"
            class="mb-3"
            @alternar="(concluido) => alternarConclusao(compromisso, concluido)"
            @remover="paraRemover = compromisso"
          />
        </TransitionGroup>

        <v-btn
          v-if="compromissoStore.temMaisPaginas"
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
      icon="mdi-plus"
      color="primary"
      size="large"
      class="compromissos-fab"
      aria-label="Adicionar compromisso"
      data-cy="btn-criar-compromisso"
      @click="criar"
    />

    <CompromissoFiltrosSheet
      v-model="mostrarFiltros"
      :filtros="filtros"
      :predios="predioStore.predios"
      :mostrar-escopo="mostrarEscopo"
      @alterar="aplicarFiltros"
      @limpar="limparFiltros"
    />

    <ConfirmDialog
      :model-value="paraRemover !== null"
      titulo="Remover compromisso"
      :mensagem="`Tem certeza que deseja remover ${paraRemover?.titulo ?? 'este compromisso'}? Essa ação não pode ser desfeita.`"
      texto-confirmar="Remover"
      cor-confirmar="error"
      :carregando="removendo"
      @update:model-value="paraRemover = null"
      @confirmar="confirmarRemocao"
    />

    <v-snackbar v-model="snackbar.aberto" :color="snackbar.cor" :timeout="5000" data-cy="snackbar">
      {{ snackbar.mensagem }}
      <template v-if="snackbar.desfazer" #actions>
        <v-btn variant="text" data-cy="btn-desfazer" @click="snackbar.desfazer?.()">Desfazer</v-btn>
      </template>
    </v-snackbar>
  </v-container>
</template>

<style scoped>
.compromissos-list {
  max-width: 480px;
  margin-inline: auto;
}

/* Gruda logo abaixo do AppHeader — mesmo truque de PrediosListView.vue (margin-top negativo cancela
   o mb-2 do AppHeader, o respiro volta como padding-top). */
.compromissos-busca {
  position: sticky;
  top: var(--app-header-height);
  z-index: 1;
  flex: none;
  margin-top: -8px;
  background: rgb(var(--v-theme-background));
  padding-top: 8px;
  padding-bottom: 16px;
}

.compromissos-itens {
  position: relative;
}

/* Quando um item sai (já em opacity 0 pelo fade do próprio item), os de baixo sobem deslizando
   em vez de saltar. */
.lista-move {
  transition: transform 0.2s ease;
}

.lista-leave-active {
  position: absolute;
  width: 100%;
}

@media (prefers-reduced-motion: reduce) {
  .lista-move {
    transition: none;
  }
}

.compromissos-fab {
  position: fixed;
  right: 24px;
  bottom: 24px;
}
</style>
