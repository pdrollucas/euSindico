<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'
import AppHeader from '@/components/common/AppHeader.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import CompromissoForm from '@/components/compromissos/CompromissoForm.vue'
import { useCompromissoStore } from '@/stores/compromissoStore'
import { useEquipeStore } from '@/stores/equipeStore'
import { usePerfilStore } from '@/stores/perfilStore'
import { usePredioStore } from '@/stores/predioStore'
import { PAPEL_PREDIO } from '@/types/predio.types'
import type { CompromissoFormValues } from '@/types/compromisso.types'
import { formatarHorario } from '@/utils/dataHora'
import { lerPredioIdDaQuery, rotaListaCompromissos } from '@/utils/rotasCompromisso'

// Uma tela só para criar (/compromissos/novo) e para ver/editar (/compromissos/:id) — RFC 4.2,
// "Detalhes do compromisso".
const route = useRoute()
const router = useRouter()
const compromissoStore = useCompromissoStore()
const equipeStore = useEquipeStore()
const perfilStore = usePerfilStore()
const predioStore = usePredioStore()

const modoNovo = computed(() => route.name === 'compromissos-novo')
const compromissoId = computed(() => Number(route.params.id))
const destinoLista = computed(() => rotaListaCompromissos(route.query.predioId))

const carregando = ref(true)
const erro = ref<string | null>(null)
const erroForm = ref<string | null>(null)
const salvando = ref(false)
const alterandoConclusao = ref(false)
const confirmandoRemocao = ref(false)
const removendo = ref(false)

const compromisso = computed(() => compromissoStore.compromissoAtual)

// Foto tirada uma única vez, depois da carga: se fosse derivada do `compromissoAtual`, marcar o
// switch "Concluído" (que atualiza o compromisso na store) reiniciaria o formulário e apagaria o
// que o usuário já digitou e ainda não salvou.
const valoresIniciais = ref<Partial<CompromissoFormValues>>()
const responsavelInicial = ref<{ usuarioId: number; nome: string }>()

function tirarFotoInicial() {
  if (modoNovo.value) {
    // Vindo da tela de um prédio (?predioId=), o prédio já vem preenchido.
    const predioId = lerPredioIdDaQuery(route.query.predioId)
    valoresIniciais.value = predioId ? { predioId } : undefined
    return
  }
  const atual = compromisso.value
  if (!atual) {
    return
  }
  valoresIniciais.value = {
    predioId: atual.predioId,
    titulo: atual.titulo,
    dataCompromisso: atual.dataCompromisso,
    horarioCompromisso: formatarHorario(atual.horarioCompromisso),
    nomePrestadorServico: atual.nomePrestadorServico ?? '',
    detalhes: atual.detalhes ?? '',
    responsavelUsuarioId: atual.responsavelUsuarioId,
  }
  responsavelInicial.value = { usuarioId: atual.responsavelUsuarioId, nome: atual.responsavelNome }
}

onMounted(async () => {
  try {
    await Promise.all([
      predioStore.carregarTodas(),
      perfilStore.perfil ? Promise.resolve() : perfilStore.carregar(),
      modoNovo.value ? Promise.resolve() : compromissoStore.obter(compromissoId.value),
    ])
    tirarFotoInicial()
  } catch (error) {
    erro.value = mensagemCarga(error)
  } finally {
    carregando.value = false
  }
})

onBeforeUnmount(() => {
  compromissoStore.limparAtual()
  equipeStore.limpar()
})

function mensagemCarga(error: unknown): string {
  const status = axios.isAxiosError(error) ? error.response?.status : undefined
  if (status === 404) {
    return 'Compromisso não encontrado.'
  }
  if (status === 403) {
    return 'Você não tem acesso a este compromisso.'
  }
  return 'Não foi possível carregar os dados do compromisso.'
}

function mensagemSalvar(error: unknown): string {
  const status = axios.isAxiosError(error) ? error.response?.status : undefined
  if (status === 400) {
    return 'Verifique os dados informados — o responsável precisa fazer parte da equipe do prédio.'
  }
  if (status === 403) {
    return 'Você não tem permissão para fazer esta alteração.'
  }
  if (status === 404) {
    return 'Compromisso ou prédio não encontrado.'
  }
  return 'Não foi possível salvar o compromisso. Tente novamente.'
}

// A equipe alimenta o seletor de responsável (RN20) — só Síndico/Gestor do prédio escolhido a
// enxergam (o Colaborador nem tem o seletor, e o backend responderia 403).
let ultimoPredioSolicitado: number | null = null
async function carregarEquipe(predioId: number) {
  ultimoPredioSolicitado = predioId
  equipeStore.limpar()
  const papel = predioStore.predios.find((predio) => predio.id === predioId)?.papel
  if (papel === undefined || papel === PAPEL_PREDIO.COLABORADOR) {
    return
  }
  try {
    await equipeStore.listar(predioId)
    if (ultimoPredioSolicitado !== predioId) {
      // Chegou depois de o usuário já ter trocado de prédio — descarta.
      equipeStore.limpar()
    }
  } catch {
    erroForm.value = 'Não foi possível carregar a equipe do prédio.'
  }
}

async function onSubmit(payload: CompromissoFormValues) {
  erroForm.value = null
  salvando.value = true
  try {
    if (modoNovo.value) {
      await compromissoStore.criar(payload)
    } else {
      await compromissoStore.atualizar(compromissoId.value, payload)
    }
    router.push(destinoLista.value)
  } catch (error) {
    erroForm.value = mensagemSalvar(error)
  } finally {
    salvando.value = false
  }
}

async function alternarConclusao(concluido: boolean | null) {
  alterandoConclusao.value = true
  erroForm.value = null
  try {
    await compromissoStore.alterarConclusao(compromissoId.value, concluido === true)
  } catch {
    erroForm.value = 'Não foi possível atualizar a conclusão. Tente novamente.'
  } finally {
    alterandoConclusao.value = false
  }
}

async function confirmarRemocao() {
  removendo.value = true
  try {
    await compromissoStore.remover(compromissoId.value)
    router.push(destinoLista.value)
  } catch {
    confirmandoRemocao.value = false
    erroForm.value = 'Não foi possível remover o compromisso. Tente novamente.'
  } finally {
    removendo.value = false
  }
}
</script>

<template>
  <v-container class="compromisso-detalhe d-flex flex-column">
    <AppHeader :titulo="modoNovo ? 'Novo compromisso' : 'Compromisso'" />

    <v-alert
      v-if="erro"
      type="error"
      variant="tonal"
      density="compact"
      class="mb-4"
      data-cy="erro-compromisso-detalhe"
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
        data-cy="erro-compromisso-form"
      >
        {{ erroForm }}
      </v-alert>

      <v-switch
        v-if="!modoNovo && compromisso"
        :model-value="compromisso.concluido"
        label="Concluído"
        color="primary"
        density="comfortable"
        hide-details
        class="mb-2"
        :loading="alterandoConclusao"
        :disabled="alterandoConclusao"
        data-cy="switch-concluido"
        @update:model-value="alternarConclusao"
      />

      <CompromissoForm
        :valores-iniciais="valoresIniciais"
        :predios="predioStore.predios"
        :membros="equipeStore.membros"
        :perfil-id="perfilStore.perfil?.id ?? null"
        :responsavel-inicial="responsavelInicial"
        :salvando="salvando"
        :modo-edicao="!modoNovo"
        @predio-alterado="carregarEquipe"
        @submit="onSubmit"
      />

      <v-btn
        v-if="!modoNovo"
        variant="text"
        color="error"
        prepend-icon="mdi-trash-can-outline"
        class="align-self-center mb-4"
        data-cy="btn-remover-compromisso"
        @click="confirmandoRemocao = true"
      >
        Remover compromisso
      </v-btn>
    </template>

    <ConfirmDialog
      v-model="confirmandoRemocao"
      titulo="Remover compromisso"
      :mensagem="`Tem certeza que deseja remover ${compromisso?.titulo ?? 'este compromisso'}? Essa ação não pode ser desfeita.`"
      texto-confirmar="Remover"
      cor-confirmar="error"
      :carregando="removendo"
      @confirmar="confirmarRemocao"
    />
  </v-container>
</template>

<style scoped>
.compromisso-detalhe {
  max-width: 480px;
  margin-inline: auto;
}
</style>
