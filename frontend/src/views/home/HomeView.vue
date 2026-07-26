<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

const router = useRouter()
const authStore = useAuthStore()

async function sair() {
  await authStore.logout()
  router.push('/login')
}
</script>

<template>
  <v-container class="d-flex flex-column fill-height">
    <h1 class="text-h5" data-cy="home-titulo">Bem-vindo ao euSíndico</h1>

    <v-btn
      color="error"
      variant="text"
      block
      class="mt-auto"
      data-cy="btn-logout"
      @click="sair"
    >
      Logout
    </v-btn>
  </v-container>
</template>
