<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
import VerificarCodigoForm from '@/components/auth/VerificarCodigoForm.vue'
import { useRecuperacaoSenhaStore } from '@/stores/recuperacaoSenhaStore'
import { useContagemRegressiva } from '@/composables/useContagemRegressiva'
import { formatarContagem } from '@/utils/tempo'
import type { VerificarCodigoFormValues } from '@/types/auth.types'

const router = useRouter()
const store = useRecuperacaoSenhaStore()

const erro = ref<string | null>(null)
const carregando = ref(false)
const reenviando = ref(false)

// Sem e-mail no fluxo (F5 ou deep link direto), não há o que verificar — volta ao início.
onMounted(() => {
  if (!store.email) router.replace('/esqueci-senha')
})

const cooldownExpiraEm = computed(() => store.cooldownExpiraEm)
const { segundos: cooldownSegundos } = useContagemRegressiva(cooldownExpiraEm)
const podeReenviar = computed(() => cooldownSegundos.value === 0 && !reenviando.value)

async function onSubmit(payload: VerificarCodigoFormValues) {
  erro.value = null
  carregando.value = true
  try {
    await store.verificar(payload.codigo)
    router.push('/redefinir-senha')
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 400) {
      erro.value = 'Código inválido ou expirado. Verifique e tente novamente.'
    } else {
      erro.value = 'Não foi possível verificar o código. Tente novamente.'
    }
  } finally {
    carregando.value = false
  }
}

async function reenviar() {
  if (!podeReenviar.value || !store.email) return
  erro.value = null
  reenviando.value = true
  try {
    await store.solicitarCodigo(store.email)
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 429) {
      erro.value = 'Muitas tentativas. Aguarde um momento e tente novamente.'
    } else {
      erro.value = 'Não foi possível reenviar o código. Tente novamente.'
    }
  } finally {
    reenviando.value = false
  }
}
</script>

<template>
  <v-card :loading="carregando" rounded="lg" elevation="2" class="pa-2">
    <v-card-item>
      <v-card-title class="text-h5 font-weight-bold">Inserir código</v-card-title>
      <v-card-subtitle style="white-space: normal">
        Caso exista uma conta vinculada ao e-mail <strong>{{ store.email }}</strong>, um código de 6 caracteres será enviado. Informe-o abaixo para continuar.
      </v-card-subtitle>
    </v-card-item>

    <v-card-text>
      <v-alert
        v-if="erro"
        type="error"
        variant="tonal"
        density="compact"
        class="mb-4"
        data-cy="erro-verificar-codigo"
      >
        {{ erro }}
      </v-alert>

      <VerificarCodigoForm :carregando="carregando" @submit="onSubmit" />

      <!-- Reenvio governado pelo mesmo cooldown de 5 min do envio inicial: desabilitado com a
           contagem enquanto não zera (ver recuperacaoSenhaStore / SECURITY.md). -->
      <div class="text-center mt-4">
        <v-btn
          variant="text"
          color="primary"
          size="small"
          :disabled="!podeReenviar"
          :loading="reenviando"
          data-cy="btn-reenviar"
          @click="reenviar"
        >
          <template v-if="podeReenviar">Reenviar e-mail</template>
          <template v-else>Reenviar e-mail em {{ formatarContagem(cooldownSegundos) }}</template>
        </v-btn>
      </div>

      <div class="text-center mt-4 text-body-2">
        <router-link to="/login" class="text-medium-emphasis" data-cy="link-login">
          Voltar para o login
        </router-link>
      </div>
    </v-card-text>
  </v-card>
</template>
