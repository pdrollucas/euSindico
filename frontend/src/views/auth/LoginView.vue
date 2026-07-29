<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
import LoginForm from '@/components/auth/LoginForm.vue'
import { useAuthStore } from '@/stores/authStore'
import type { LoginRequest } from '@/types/auth.types'

const router = useRouter()
const authStore = useAuthStore()

const erroLogin = ref<string | null>(null)
const carregando = ref(false)

async function onSubmit(payload: LoginRequest) {
  erroLogin.value = null
  carregando.value = true
  try {
    await authStore.login(payload.email, payload.senha)
    router.push('/home')
  } catch (error) {
    // Nunca decide a mensagem com base no `title` do backend — só no status HTTP
    // (ver frontend/documentation/ARCHITECTURE.md, seção 6).
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      erroLogin.value = 'E-mail ou senha inválidos.'
    } else {
      erroLogin.value = 'Não foi possível entrar. Tente novamente.'
    }
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <v-card :loading="carregando" rounded="lg" elevation="2" class="pa-2">
    <v-card-item>
      <v-card-title class="text-h5 font-weight-bold">Entrar</v-card-title>
      <v-card-subtitle style="white-space: normal">
        Acesse sua conta para gerenciar seus condomínios.
      </v-card-subtitle>
    </v-card-item>

    <v-card-text>
      <v-alert
        v-if="erroLogin"
        type="error"
        variant="tonal"
        density="compact"
        class="mb-4"
        data-cy="erro-login"
      >
        {{ erroLogin }}
      </v-alert>

      <LoginForm @submit="onSubmit" />

      <div class="text-center mt-4 text-body-2">
        <router-link to="/esqueci-senha" class="text-primary" data-cy="link-esqueci-senha">
          Esqueci minha senha
        </router-link>
      </div>

      <div class="text-center mt-6 text-body-2">
        <span class="text-medium-emphasis">Não tem uma conta?</span>
        <router-link
          to="/registrar"
          class="ml-1 font-weight-medium text-primary"
          data-cy="link-registrar"
        >
          Criar conta
        </router-link>
      </div>
    </v-card-text>
  </v-card>
</template>
