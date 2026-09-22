<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'
import AppHeader from '@/components/common/AppHeader.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import ConvidarFuncionarioForm from '@/components/equipe/ConvidarFuncionarioForm.vue'
import { useEquipeStore } from '@/stores/equipeStore'
import { usePerfilStore } from '@/stores/perfilStore'
import { PAPEL_PREDIO } from '@/types/predio.types'
import type { ConvidarFuncionarioRequest, EquipeMembro } from '@/types/equipe.types'

const route = useRoute()
const equipeStore = useEquipeStore()
const perfilStore = usePerfilStore()

const predioId = computed(() => Number(route.params.id))

const carregando = ref(true)
const erro = ref<string | null>(null)
const mostrarFormConvite = ref(false)
const convidando = ref(false)
const erroConvite = ref<string | null>(null)
const membroParaRemover = ref<EquipeMembro | null>(null)
const removendo = ref(false)

function nomePapel(papel: number) {
  if (papel === PAPEL_PREDIO.SINDICO) {
    return 'Síndico'
  }
  if (papel === PAPEL_PREDIO.GESTOR) {
    return 'Gestor'
  }
  return 'Colaborador'
}

onMounted(async () => {
  erro.value = null
  carregando.value = true
  try {
    // perfilStore precisa carregar pra esconder o botão de remover na própria linha.
    await Promise.all([equipeStore.listar(predioId.value), perfilStore.carregar()])
  } catch {
    erro.value = 'Não foi possível carregar a equipe do prédio.'
  } finally {
    carregando.value = false
  }
})

async function onConvidar(payload: ConvidarFuncionarioRequest) {
  erroConvite.value = null
  convidando.value = true
  try {
    await equipeStore.convidar(predioId.value, payload)
    mostrarFormConvite.value = false
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 409) {
      erroConvite.value = 'Esse e-mail já faz parte da equipe ou já tem um convite pendente.'
    } else {
      erroConvite.value = 'Não foi possível enviar o convite. Tente novamente.'
    }
  } finally {
    convidando.value = false
  }
}

async function confirmarRemocao() {
  if (!membroParaRemover.value) {
    return
  }
  removendo.value = true
  try {
    await equipeStore.remover(predioId.value, membroParaRemover.value.usuarioId)
    membroParaRemover.value = null
  } catch {
    erro.value = 'Não foi possível remover este membro. Tente novamente.'
  } finally {
    removendo.value = false
  }
}
</script>

<template>
  <v-container class="equipe d-flex flex-column">
    <AppHeader titulo="Equipe" />

    <v-alert
      v-if="erro"
      type="error"
      variant="tonal"
      density="compact"
      class="mb-4"
      data-cy="erro-equipe"
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
        v-if="equipeStore.membros.length === 0"
        class="text-center text-medium-emphasis mt-8"
        data-cy="equipe-vazia"
      >
        Só você faz parte deste prédio por enquanto.
      </p>

      <v-card
        v-for="membro in equipeStore.membros"
        :key="membro.usuarioId"
        variant="flat"
        border
        rounded="lg"
        class="mb-3"
        :data-cy="`membro-${membro.usuarioId}`"
      >
        <v-card-text class="d-flex align-center justify-space-between">
          <div class="d-flex flex-column">
            <span class="text-subtitle-2 font-weight-medium">{{ membro.nome }}</span>
            <span class="text-caption text-medium-emphasis">{{ membro.email }}</span>
            <span class="text-caption text-primary">{{ nomePapel(membro.papel) }}</span>
          </div>
          <v-btn
            v-if="membro.usuarioId !== perfilStore.perfil?.id"
            icon="mdi-close"
            variant="text"
            color="error"
            size="small"
            aria-label="Remover membro"
            :data-cy="`btn-remover-membro-${membro.usuarioId}`"
            @click="membroParaRemover = membro"
          />
        </v-card-text>
      </v-card>
    </template>

    <v-btn
      icon="mdi-account-plus-outline"
      color="primary"
      size="large"
      class="equipe-fab"
      aria-label="Convidar funcionário"
      data-cy="btn-convidar"
      @click="mostrarFormConvite = true"
    />

    <v-dialog v-model="mostrarFormConvite" max-width="400">
      <v-card rounded="lg">
        <v-card-item>
          <v-card-title class="text-h6 font-weight-bold">Convidar funcionário</v-card-title>
        </v-card-item>
        <v-card-text>
          <v-alert
            v-if="erroConvite"
            type="error"
            variant="tonal"
            density="compact"
            class="mb-4"
            data-cy="erro-convite"
          >
            {{ erroConvite }}
          </v-alert>
          <ConvidarFuncionarioForm :enviando="convidando" @submit="onConvidar" />
        </v-card-text>
      </v-card>
    </v-dialog>

    <ConfirmDialog
      :model-value="membroParaRemover !== null"
      titulo="Remover membro"
      :mensagem="`Tem certeza que deseja remover ${membroParaRemover?.nome ?? ''} da equipe deste prédio?`"
      texto-confirmar="Remover"
      cor-confirmar="error"
      :carregando="removendo"
      @update:model-value="membroParaRemover = null"
      @confirmar="confirmarRemocao"
    />
  </v-container>
</template>

<style scoped>
.equipe {
  max-width: 480px;
  margin-inline: auto;
}

.equipe-fab {
  position: fixed;
  right: 24px;
  bottom: 24px;
}
</style>
