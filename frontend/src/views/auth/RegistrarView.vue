<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
import RegistrarForm from '@/components/auth/RegistrarForm.vue'
import { useAuthStore } from '@/stores/authStore'
import type { RegistrarRequest } from '@/types/auth.types'

const router = useRouter()
const authStore = useAuthStore()

const erroRegistrar = ref<string | null>(null)
const carregando = ref(false)

async function onSubmit(payload: RegistrarRequest) {
  erroRegistrar.value = null
  carregando.value = true
  try {
    await authStore.registrar(payload)
    // Cadastro não autentica automaticamente — o fluxo do RFC (seção 4.1) segue para o login.
    router.push({ path: '/login', query: { registrado: '1' } })
  } catch (error) {
    // Nunca decide a mensagem com base no `title` do backend — só no status HTTP
    // (ver frontend/documentation/ARCHITECTURE.md, seção 6).
    if (axios.isAxiosError(error) && error.response?.status === 409) {
      erroRegistrar.value = 'Este e-mail já está cadastrado.'
    } else {
      erroRegistrar.value = 'Não foi possível criar a conta. Tente novamente.'
    }
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <v-card :loading="carregando">
    <v-card-title>Criar conta</v-card-title>
    <v-card-text>
      <v-alert
        v-if="erroRegistrar"
        type="error"
        density="compact"
        class="mb-4"
        data-cy="erro-registrar"
      >
        {{ erroRegistrar }}
      </v-alert>

      <RegistrarForm @submit="onSubmit" />

      <div class="d-flex justify-center mt-4">
        <router-link to="/login" data-cy="link-login">Já tenho conta</router-link>
      </div>
    </v-card-text>
  </v-card>
</template>
