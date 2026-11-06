<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import AppHeader from '@/components/common/AppHeader.vue'
import AlterarSenhaForm from '@/components/configuracoes/AlterarSenhaForm.vue'
import { useAuthStore } from '@/stores/authStore'
import { usePerfilStore } from '@/stores/perfilStore'
import { mensagemDoErro } from '@/utils/mensagemDoErro'
import type { AlterarSenhaFormValues } from '@/types/perfil.types'

const router = useRouter()
const authStore = useAuthStore()
const perfilStore = usePerfilStore()

const carregando = ref(false)
const erro = ref<string | null>(null)

async function onSubmit(payload: AlterarSenhaFormValues) {
  erro.value = null
  carregando.value = true
  try {
    await perfilStore.alterarSenha({ senhaAtual: payload.senhaAtual, novaSenha: payload.novaSenha })
    // A troca derruba todas as sessões no backend (inclusive esta): encerra a sessão local e leva
    // o usuário a entrar de novo com a senha nova, como na redefinição por e-mail.
    authStore.clearSession()
    perfilStore.limpar()
    router.push({ path: '/login', query: { senhaRedefinida: '1' } })
  } catch (error) {
    // 400 = senha atual incorreta; o title já é a mensagem para o usuário.
    erro.value = mensagemDoErro(error, 'Não foi possível alterar a senha. Tente novamente.', [400])
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <v-container class="config-seguranca d-flex flex-column fill-height">
    <AppHeader />
    <div class="app-bloco-form d-flex flex-column">
      <v-card rounded="lg" elevation="2">
        <v-card-item>
          <v-card-title class="text-h6 font-weight-bold">Alterar senha</v-card-title>
          <v-card-subtitle class="text-wrap">
            Ao alterar a senha, você sai de todos os dispositivos e entra de novo com a nova senha.
          </v-card-subtitle>
        </v-card-item>
        <v-card-text>
          <v-alert
            v-if="erro"
            type="error"
            variant="tonal"
            density="compact"
            class="mb-4"
            data-cy="erro-alterar-senha"
          >
            {{ erro }}
          </v-alert>
          <AlterarSenhaForm :carregando="carregando" @submit="onSubmit" />
        </v-card-text>
      </v-card>
    </div>
  </v-container>
</template>

<style scoped>
.config-seguranca {
  max-width: var(--app-largura-conteudo);
  margin-inline: auto;
}
</style>
