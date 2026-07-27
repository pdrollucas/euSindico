<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
import EsqueciSenhaForm from '@/components/auth/EsqueciSenhaForm.vue'
import { useRecuperacaoSenhaStore } from '@/stores/recuperacaoSenhaStore'
import { useContagemRegressiva } from '@/composables/useContagemRegressiva'
import type { EsqueciSenhaRequest } from '@/types/auth.types'

const router = useRouter()
const store = useRecuperacaoSenhaStore()

const erro = ref<string | null>(null)
const carregando = ref(false)

// Cooldown compartilhado com a tela de "inserir código": se o usuário voltar para cá durante os
// 5 minutos, o botão continua desabilitado com a contagem (ver recuperacaoSenhaStore).
const cooldownExpiraEm = computed(() => store.cooldownExpiraEm)
const { segundos: cooldownSegundos } = useContagemRegressiva(cooldownExpiraEm)

async function onSubmit(payload: EsqueciSenhaRequest) {
  erro.value = null
  carregando.value = true
  try {
    await store.solicitarCodigo(payload.email)
    // Anti-enumeração: a API responde igual exista ou não o e-mail; seguimos sempre para a
    // tela de código (ver backend AUTHENTICATION.md, Fluxo 8).
    router.push('/verificar-codigo')
  } catch (error) {
    // Só o status HTTP decide a mensagem (ver ARCHITECTURE.md, seção 6).
    if (axios.isAxiosError(error) && error.response?.status === 429) {
      erro.value = 'Muitas tentativas. Aguarde um momento e tente novamente.'
    } else {
      erro.value = 'Não foi possível enviar o código. Tente novamente.'
    }
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <v-card :loading="carregando" rounded="lg" elevation="2" class="pa-2">
    <v-card-item>
      <v-card-title class="text-h5 font-weight-bold">Esqueci minha senha</v-card-title>
      <v-card-subtitle style="white-space: normal">
        Informe seu e-mail. Se houver uma conta associada a ele, enviaremos um código para redefinir
        a senha.
      </v-card-subtitle>
    </v-card-item>

    <v-card-text>
      <v-alert
        v-if="erro"
        type="error"
        variant="tonal"
        density="compact"
        class="mb-4"
        data-cy="erro-esqueci-senha"
      >
        {{ erro }}
      </v-alert>

      <EsqueciSenhaForm
        :carregando="carregando"
        :cooldown-segundos="cooldownSegundos"
        :email-inicial="store.email ?? ''"
        @submit="onSubmit"
      />

      <div class="text-center mt-6 text-body-2">
        <span class="text-medium-emphasis">Lembrou a senha?</span>
        <router-link to="/login" class="ml-1 font-weight-medium text-primary" data-cy="link-login">
          Entrar
        </router-link>
      </div>
    </v-card-text>
  </v-card>
</template>
