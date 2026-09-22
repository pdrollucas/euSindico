<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'
import AppHeader from '@/components/common/AppHeader.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import PredioForm from '@/components/predios/PredioForm.vue'
import { usePredioStore } from '@/stores/predioStore'
import { PAPEL_PREDIO } from '@/types/predio.types'
import type { PredioFormValues } from '@/types/predio.types'
import type { ErroApiDto } from '@/types/erroApi.types'

const route = useRoute()
const router = useRouter()
const predioStore = usePredioStore()

const predioId = computed(() => Number(route.params.id))
const predio = computed(() => predioStore.predioAtual)
// PUT/DELETE são exclusivos do Síndico (RF33) — a UI só esconde o que o backend já rejeitaria.
const podeGerenciar = computed(() => predio.value?.papel === PAPEL_PREDIO.SINDICO)

const carregando = ref(true)
const erro = ref<string | null>(null)
const removendo = ref(false)
const confirmandoRemocao = ref(false)

const mostrarFormEditar = ref(false)
const salvandoEdicao = ref(false)
const erroEdicao = ref<string | null>(null)
const valoresIniciaisEdicao = computed<PredioFormValues | undefined>(() =>
  predio.value ? { nome: predio.value.nome, endereco: predio.value.endereco } : undefined,
)

// Módulos ainda não implementados no backend — mostrados como roadmap, sem navegação.
const modulosFuturos = [
  { titulo: 'Compromissos', icone: 'mdi-calendar-check-outline' },
  { titulo: 'Relatórios de atividade', icone: 'mdi-file-chart-outline' },
  { titulo: 'Atas', icone: 'mdi-file-document-outline' },
  { titulo: 'Normas', icone: 'mdi-file-document-multiple-outline' },
  { titulo: 'Planejamentos futuros', icone: 'mdi-calendar-clock-outline' },
]

onMounted(carregar)

async function carregar() {
  erro.value = null
  carregando.value = true
  try {
    await predioStore.obter(predioId.value)
  } catch {
    erro.value = 'Não foi possível carregar os dados do prédio.'
  } finally {
    carregando.value = false
  }
}

async function confirmarRemocao() {
  removendo.value = true
  try {
    await predioStore.remover(predioId.value)
    router.push('/predios')
  } catch {
    erro.value = 'Não foi possível remover o prédio. Tente novamente.'
    confirmandoRemocao.value = false
  } finally {
    removendo.value = false
  }
}

async function onEditar(payload: PredioFormValues) {
  erroEdicao.value = null
  salvandoEdicao.value = true
  try {
    await predioStore.atualizar(predioId.value, payload)
    mostrarFormEditar.value = false
  } catch (error) {
    // Um 409 cobre duas exceções diferentes (duplicidade vs. limite de 20 prédios, PREDIOS.md)
    // — mostra o title do backend em vez de reinventar a mensagem.
    if (axios.isAxiosError<ErroApiDto>(error) && error.response?.status === 409) {
      erroEdicao.value = error.response.data?.title ?? 'Não foi possível salvar as alterações. Tente novamente.'
    } else {
      erroEdicao.value = 'Não foi possível salvar as alterações. Tente novamente.'
    }
  } finally {
    salvandoEdicao.value = false
  }
}
</script>

<template>
  <v-container class="predio-detalhe d-flex flex-column">
    <AppHeader :titulo="predio?.nome ?? 'Prédio'" />

    <v-alert
      v-if="erro"
      type="error"
      variant="tonal"
      density="compact"
      class="mb-4"
      data-cy="erro-predio-detalhe"
    >
      {{ erro }}
    </v-alert>

    <v-progress-circular
      v-if="carregando"
      indeterminate
      color="primary"
      class="align-self-center mt-8"
    />

    <template v-else-if="predio">
      <p class="text-body-2 text-medium-emphasis mb-4" data-cy="predio-endereco">
        {{ predio.endereco }}
      </p>

      <v-card
        v-if="podeGerenciar"
        :to="`/predios/${predioId}/equipe`"
        data-cy="card-equipe"
        variant="flat"
        border
        rounded="lg"
        class="mb-3"
      >
        <v-card-text class="d-flex align-center ga-3">
          <v-icon icon="mdi-account-group-outline" color="primary" />
          <span class="text-subtitle-2 font-weight-medium">Equipe</span>
        </v-card-text>
      </v-card>

      <v-card
        v-for="modulo in modulosFuturos"
        :key="modulo.titulo"
        variant="flat"
        border
        rounded="lg"
        class="mb-3 predio-modulo-futuro"
      >
        <v-card-text class="d-flex align-center ga-3">
          <v-icon :icon="modulo.icone" color="primary" />
          <div class="d-flex flex-column">
            <span class="text-subtitle-2 font-weight-medium">{{ modulo.titulo }}</span>
            <span class="text-caption text-medium-emphasis">Em breve</span>
          </div>
        </v-card-text>
      </v-card>
    </template>

    <v-menu v-if="podeGerenciar">
      <template #activator="{ props: menuProps }">
        <v-btn
          icon="mdi-dots-vertical"
          color="primary"
          size="large"
          class="predio-detalhe-fab"
          aria-label="Mais ações"
          data-cy="btn-mais-acoes"
          v-bind="menuProps"
        />
      </template>
      <v-list density="compact">
        <v-list-item
          prepend-icon="mdi-pencil-outline"
          title="Editar prédio"
          data-cy="btn-editar-predio"
          @click="mostrarFormEditar = true"
        />
        <v-list-item
          prepend-icon="mdi-trash-can-outline"
          title="Remover prédio"
          base-color="error"
          data-cy="btn-remover-predio"
          @click="confirmandoRemocao = true"
        />
      </v-list>
    </v-menu>

    <v-dialog v-model="mostrarFormEditar" max-width="400">
      <v-card rounded="lg">
        <v-card-item>
          <v-card-title class="text-h6 font-weight-bold">Editar prédio</v-card-title>
        </v-card-item>
        <v-card-text>
          <v-alert
            v-if="erroEdicao"
            type="error"
            variant="tonal"
            density="compact"
            class="mb-4"
            data-cy="erro-predio-form"
          >
            {{ erroEdicao }}
          </v-alert>
          <PredioForm
            :valores-iniciais="valoresIniciaisEdicao"
            :salvando="salvandoEdicao"
            modo-edicao
            @submit="onEditar"
          />
        </v-card-text>
      </v-card>
    </v-dialog>

    <ConfirmDialog
      v-model="confirmandoRemocao"
      titulo="Remover prédio"
      :mensagem="`Tem certeza que deseja remover ${predio?.nome ?? 'este prédio'}? Essa ação não pode ser desfeita.`"
      texto-confirmar="Remover"
      cor-confirmar="error"
      :carregando="removendo"
      @confirmar="confirmarRemocao"
    />
  </v-container>
</template>

<style scoped>
.predio-detalhe {
  max-width: 480px;
  margin-inline: auto;
}

.predio-modulo-futuro {
  opacity: 0.6;
}

.predio-detalhe-fab {
  position: fixed;
  right: 24px;
  bottom: 24px;
}
</style>
