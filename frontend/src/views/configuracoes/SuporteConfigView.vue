<script setup lang="ts">
import { ref } from 'vue'
import AppHeader from '@/components/common/AppHeader.vue'
import EnviarSuporteForm from '@/components/configuracoes/EnviarSuporteForm.vue'
import { useSuporteStore } from '@/stores/suporteStore'
import { mensagemDoErro } from '@/utils/mensagemDoErro'
import type { EnviarSuporteRequest } from '@/types/perfil.types'

const suporteStore = useSuporteStore()

const carregando = ref(false)
const erro = ref<string | null>(null)
// Trocar a chave remonta o formulário vazio depois de um envio bem-sucedido.
const chaveFormulario = ref(0)

const snackbar = ref<{ aberto: boolean; mensagem: string }>({ aberto: false, mensagem: '' })

async function onSubmit(payload: EnviarSuporteRequest) {
  erro.value = null
  carregando.value = true
  try {
    await suporteStore.enviar(payload)
    chaveFormulario.value++
    snackbar.value = { aberto: true, mensagem: 'Mensagem enviada. Responderemos por e-mail.' }
  } catch (error) {
    // 429 = limite de mensagens por hora; o title do rate limiter já é a mensagem.
    erro.value = mensagemDoErro(error, 'Não foi possível enviar a mensagem. Tente novamente.', [429])
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <v-container class="config-suporte d-flex flex-column fill-height">
    <AppHeader />

    <v-card rounded="lg" elevation="2">
      <v-card-item>
        <v-card-title class="text-h6 font-weight-bold">Fale com o suporte</v-card-title>
        <v-card-subtitle class="text-wrap">
          Descreva sua dúvida ou problema. Responderemos no e-mail da sua conta.
        </v-card-subtitle>
      </v-card-item>
      <v-card-text>
        <v-alert
          v-if="erro"
          type="error"
          variant="tonal"
          density="compact"
          class="mb-4"
          data-cy="erro-suporte"
        >
          {{ erro }}
        </v-alert>
        <EnviarSuporteForm :key="chaveFormulario" :carregando="carregando" @submit="onSubmit" />
      </v-card-text>
    </v-card>

    <v-snackbar v-model="snackbar.aberto" :timeout="5000" data-cy="snackbar">
      {{ snackbar.mensagem }}
    </v-snackbar>
  </v-container>
</template>

<style scoped>
.config-suporte {
  max-width: 480px;
  margin-inline: auto;
}
</style>
