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
  <v-card :loading="carregando">
    <v-card-title>Entrar</v-card-title>
    <v-card-text>
      <v-alert
        v-if="erroLogin"
        type="error"
        density="compact"
        class="mb-4"
        data-cy="erro-login"
      >
        {{ erroLogin }}
      </v-alert>

      <LoginForm @submit="onSubmit" />

      <div class="d-flex justify-space-between mt-4">
        <router-link to="/esqueci-senha" data-cy="link-esqueci-senha">
          Esqueci minha senha
        </router-link>
        <router-link to="/registrar" data-cy="link-registrar">Criar conta</router-link>
      </div>
    </v-card-text>
  </v-card>
</template>
