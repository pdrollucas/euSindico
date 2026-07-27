<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
import RedefinirSenhaForm from '@/components/auth/RedefinirSenhaForm.vue'
import { useRecuperacaoSenhaStore } from '@/stores/recuperacaoSenhaStore'
import type { RedefinirSenhaFormValues } from '@/types/auth.types'

const router = useRouter()
const store = useRecuperacaoSenhaStore()

const erro = ref<string | null>(null)
const carregando = ref(false)

// O código não sobrevive a um F5 (não é persistido — é segredo). Sem e-mail, volta ao início;
// com e-mail mas sem código verificado (ex: reload nesta tela), volta para a tela de código.
onMounted(() => {
  if (!store.email) router.replace('/esqueci-senha')
  else if (!store.codigo) router.replace('/verificar-codigo')
})

async function onSubmit(payload: RedefinirSenhaFormValues) {
  erro.value = null
  carregando.value = true
  try {
    await store.redefinir(payload.novaSenha, payload.confirmarSenha)
    // Redefinição derruba todas as sessões (backend) — o usuário entra de novo com a senha nova.
    router.push({ path: '/login', query: { senhaRedefinida: '1' } })
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 400) {
      erro.value =
        'Não foi possível redefinir a senha. O código pode ter expirado — solicite um novo.'
    } else {
      erro.value = 'Não foi possível redefinir a senha. Tente novamente.'
    }
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <v-card :loading="carregando" rounded="lg" elevation="2" class="pa-2">
    <v-card-item>
      <v-card-title class="text-h5 font-weight-bold">Atualizar senha</v-card-title>
      <v-card-subtitle style="white-space: normal">
        Defina uma nova senha para a sua conta.
      </v-card-subtitle>
    </v-card-item>

    <v-card-text>
      <v-alert
        v-if="erro"
        type="error"
        variant="tonal"
        density="compact"
        class="mb-4"
        data-cy="erro-redefinir-senha"
      >
        {{ erro }}
      </v-alert>

      <RedefinirSenhaForm :carregando="carregando" @submit="onSubmit" />
    </v-card-text>
  </v-card>
</template>
