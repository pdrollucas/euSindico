<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'
import AppHeader from '@/components/common/AppHeader.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import RelatorioListItem from '@/components/relatorios/RelatorioListItem.vue'
import GerarRelatorioForm from '@/components/relatorios/GerarRelatorioForm.vue'
import { useRelatorioStore } from '@/stores/relatorioStore'
import { usePredioStore } from '@/stores/predioStore'
import { PAPEL_PREDIO } from '@/types/predio.types'
import type { ErroApiDto } from '@/types/erroApi.types'
import type { GerarRelatorioFormValues, Relatorio } from '@/types/relatorio.types'
import { abrirAbaEmBranco, baixarArquivo, mostrarArquivoNaAba } from '@/utils/arquivo'
import { formatarPeriodo } from '@/utils/periodo'

const route = useRoute()
const relatorioStore = useRelatorioStore()
const predioStore = usePredioStore()

const predioId = computed(() => Number(route.params.id))
// Gerar é de Síndico/Gestor e excluir só do Síndico (RN35, RN36); o Colaborador só lê, abre e baixa.
// Enquanto o prédio não carrega o papel é desconhecido, então nada de escrita aparece.
const papel = computed(() => predioStore.predioAtual?.papel)
const podeGerar = computed(() => papel.value !== undefined && papel.value !== PAPEL_PREDIO.COLABORADOR)
const podeExcluir = computed(() => papel.value === PAPEL_PREDIO.SINDICO)

const carregando = ref(false)
const erro = ref<string | null>(null)

const mostrarFormGerar = ref(false)
const gerando = ref(false)
const erroGerar = ref<string | null>(null)

const paraRemover = ref<Relatorio | null>(null)
const removendo = ref(false)
// Relatório cujo PDF está sendo buscado (abrir ou baixar): o item fica desabilitado até terminar.
const ocupadoId = ref<number | null>(null)

const snackbar = ref<{ aberto: boolean; mensagem: string; cor?: string }>({
  aberto: false,
  mensagem: '',
})

const mensagemVazio = computed(() =>
  podeGerar.value
    ? 'Este prédio ainda não tem relatórios. Gere o primeiro!'
    : 'Este prédio ainda não tem relatórios.',
)

onMounted(async () => {
  // O prédio dá o papel (e o título); a lista carrega em paralelo, e uma falha no prédio não a
  // impede de aparecer — sem papel, a tela fica só de leitura.
  predioStore.obter(predioId.value).catch(() => undefined)
  await carregar()
})

async function carregar() {
  erro.value = null
  carregando.value = true
  try {
    await relatorioStore.listarPrimeiraPagina(predioId.value)
  } catch {
    erro.value = 'Não foi possível carregar os relatórios.'
  } finally {
    carregando.value = false
  }
}

async function carregarMais() {
  try {
    await relatorioStore.carregarProximaPagina(predioId.value)
  } catch {
    erro.value = 'Não foi possível carregar mais relatórios.'
  }
}

function avisar(mensagem: string, cor?: string) {
  snackbar.value = { aberto: true, mensagem, cor }
}

function abrirFormGerar() {
  erroGerar.value = null
  mostrarFormGerar.value = true
}

async function onGerar(payload: GerarRelatorioFormValues) {
  erroGerar.value = null
  gerando.value = true
  try {
    await relatorioStore.gerar(predioId.value, payload)
    mostrarFormGerar.value = false
  } catch (error) {
    // 409 (limite mensal) e 422 (sem compromissos concluídos) trazem no title uma mensagem já
    // pronta para o usuário — o frontend a mostra em vez de reescrevê-la. O diálogo continua aberto
    // para ele escolher outro mês.
    if (
      axios.isAxiosError<ErroApiDto>(error) &&
      (error.response?.status === 409 || error.response?.status === 422)
    ) {
      erroGerar.value = error.response.data?.title ?? 'Não foi possível gerar o relatório.'
    } else {
      erroGerar.value = 'Não foi possível gerar o relatório. Tente novamente.'
    }
  } finally {
    gerando.value = false
  }
}

// A aba precisa nascer dentro do clique: o PDF só chega depois da requisição, e abrir a aba depois
// dessa espera é bloqueado pelos navegadores (Safari, celulares). Ela abre em branco e é apontada
// para o PDF quando ele chega; se a busca falhar, é fechada.
async function abrir(relatorio: Relatorio) {
  const aba = abrirAbaEmBranco()
  if (!aba) {
    avisar('O navegador bloqueou a nova aba. Permita pop-ups ou use o ícone de baixar.', 'warning')
    return
  }
  ocupadoId.value = relatorio.id
  try {
    const arquivo = await relatorioStore.baixar(predioId.value, relatorio.id)
    mostrarArquivoNaAba(aba, arquivo)
  } catch {
    aba.close()
    avisar('Não foi possível abrir o relatório. Tente novamente.', 'error')
  } finally {
    ocupadoId.value = null
  }
}

async function baixar(relatorio: Relatorio) {
  ocupadoId.value = relatorio.id
  try {
    const arquivo = await relatorioStore.baixar(predioId.value, relatorio.id)
    baixarArquivo(arquivo, relatorio.nomeArquivo)
  } catch {
    avisar('Não foi possível baixar o relatório. Tente novamente.', 'error')
  } finally {
    ocupadoId.value = null
  }
}

async function confirmarRemocao() {
  if (!paraRemover.value) {
    return
  }
  removendo.value = true
  try {
    await relatorioStore.remover(predioId.value, paraRemover.value.id)
    paraRemover.value = null
    avisar('Relatório removido')
  } catch {
    paraRemover.value = null
    avisar('Não foi possível remover o relatório. Tente novamente.', 'error')
  } finally {
    removendo.value = false
  }
}
</script>

<template>
  <v-container class="relatorios-list d-flex flex-column fill-height">
    <AppHeader />

    <v-alert
      v-if="erro"
      type="error"
      variant="tonal"
      density="compact"
      class="mb-4"
      data-cy="erro-relatorios"
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
        v-if="relatorioStore.relatorios.length === 0"
        class="text-center text-medium-emphasis mt-8"
        data-cy="relatorios-vazio"
      >
        {{ mensagemVazio }}
      </p>

      <div v-else class="flex-grow-1">
        <RelatorioListItem
          v-for="relatorio in relatorioStore.relatorios"
          :key="relatorio.id"
          :relatorio="relatorio"
          :pode-remover="podeExcluir"
          :ocupado="ocupadoId === relatorio.id"
          class="mb-3"
          @abrir="abrir(relatorio)"
          @baixar="baixar(relatorio)"
          @remover="paraRemover = relatorio"
        />

        <v-btn
          v-if="relatorioStore.temMaisPaginas"
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
      v-if="podeGerar"
      icon="mdi-plus"
      color="primary"
      size="large"
      class="relatorios-fab"
      aria-label="Gerar relatório"
      data-cy="btn-gerar-relatorio-fab"
      @click="abrirFormGerar"
    />

    <v-dialog v-model="mostrarFormGerar" max-width="400">
      <v-card rounded="lg">
        <v-card-item>
          <v-card-title class="text-h6 font-weight-bold">Gerar relatório</v-card-title>
          <v-card-subtitle class="text-wrap">
            Reúne os compromissos concluídos do mês escolhido. O limite é de 3 relatórios por mês.
          </v-card-subtitle>
        </v-card-item>
        <v-card-text>
          <v-alert
            v-if="erroGerar"
            type="error"
            variant="tonal"
            density="compact"
            class="mb-4"
            data-cy="erro-gerar-relatorio"
          >
            {{ erroGerar }}
          </v-alert>
          <GerarRelatorioForm :gerando="gerando" @submit="onGerar" />
        </v-card-text>
      </v-card>
    </v-dialog>

    <ConfirmDialog
      :model-value="paraRemover !== null"
      titulo="Remover relatório"
      :mensagem="`Tem certeza que deseja remover o relatório de ${paraRemover ? formatarPeriodo(paraRemover.mesReferencia, paraRemover.anoReferencia) : 'este período'}? Essa ação não pode ser desfeita.`"
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
.relatorios-list {
  max-width: 480px;
  margin-inline: auto;
}

.relatorios-fab {
  position: fixed;
  right: 24px;
  bottom: 24px;
}
</style>
