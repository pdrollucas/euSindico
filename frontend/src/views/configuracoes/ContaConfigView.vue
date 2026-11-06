<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import AppHeader from '@/components/common/AppHeader.vue'
import ExcluirContaForm from '@/components/configuracoes/ExcluirContaForm.vue'
import { useAuthStore } from '@/stores/authStore'
import { usePerfilStore } from '@/stores/perfilStore'
import { mensagemDoErro } from '@/utils/mensagemDoErro'
import type { ExcluirContaRequest } from '@/types/perfil.types'

const router = useRouter()
const authStore = useAuthStore()
const perfilStore = usePerfilStore()

const mostrarDialogo = ref(false)
const excluindo = ref(false)
const erro = ref<string | null>(null)

function abrirDialogo() {
  erro.value = null
  mostrarDialogo.value = true
}

async function onExcluir(payload: ExcluirContaRequest) {
  erro.value = null
  excluindo.value = true
  try {
    await perfilStore.excluirConta(payload.senha)
    // A conta e os tokens já não existem no backend: só limpa a sessão local e volta à landing.
    authStore.clearSession()
    perfilStore.limpar()
    router.push('/')
  } catch (error) {
    // 400 = senha incorreta; o title já é a mensagem para o usuário. O diálogo continua aberto.
    erro.value = mensagemDoErro(error, 'Não foi possível excluir a conta. Tente novamente.', [400])
  } finally {
    excluindo.value = false
  }
}
</script>

<template>
  <v-container class="config-conta d-flex flex-column fill-height">
    <AppHeader />
    <div class="app-bloco-form d-flex flex-column">
      <v-card rounded="lg" elevation="2">
        <v-card-item>
          <v-card-title class="text-h6 font-weight-bold">Excluir conta</v-card-title>
        </v-card-item>
        <v-card-text>
          <p class="text-body-2 mb-3">
            A exclusão é <strong>permanente</strong> e não pode ser desfeita. Ela apaga a sua conta e
            tudo o que está ligado a ela: seus prédios, compromissos, planejamentos, atas, normas e
            relatórios.
          </p>
          <p class="text-body-2 text-medium-emphasis mb-4">
            Se você faz parte da equipe de prédios de outro síndico, apenas o seu acesso a eles é
            removido.
          </p>
          <v-btn
            color="error"
            variant="outlined"
            size="large"
            block
            data-cy="btn-excluir-conta"
            @click="abrirDialogo"
          >
            Excluir minha conta
          </v-btn>
        </v-card-text>
      </v-card>
    </div>

    <v-dialog v-model="mostrarDialogo" max-width="400">
      <v-card rounded="lg">
        <v-card-item>
          <v-card-title class="text-h6 font-weight-bold">Excluir conta</v-card-title>
          <v-card-subtitle class="text-wrap">
            Para confirmar, informe a sua senha. Todos os seus dados serão apagados.
          </v-card-subtitle>
        </v-card-item>
        <v-card-text>
          <v-alert
            v-if="erro"
            type="error"
            variant="tonal"
            density="compact"
            class="mb-4"
            data-cy="erro-excluir-conta"
          >
            {{ erro }}
          </v-alert>
          <ExcluirContaForm :carregando="excluindo" @submit="onExcluir" />
          <v-btn
            variant="text"
            block
            class="mt-2"
            :disabled="excluindo"
            data-cy="btn-cancelar-excluir-conta"
            @click="mostrarDialogo = false"
          >
            Cancelar
          </v-btn>
        </v-card-text>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
.config-conta {
  max-width: var(--app-largura-conteudo);
  margin-inline: auto;
}
</style>
