<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { useConviteStore } from '@/stores/conviteStore'
import { aceitarConviteNovaContaFormSchema } from '@/schemas/equipe.schema'
import { PAPEL_PREDIO } from '@/types/predio.types'
import type { AceitarConviteNovaContaForm } from '@/types/equipe.types'

// Rota pública (/convite/:token) — sem auto-login, o login continua um passo separado.
const route = useRoute()
const router = useRouter()
const conviteStore = useConviteStore()

const token = computed(() => String(route.params.token))
const convite = computed(() => conviteStore.convite)

const carregando = ref(true)
const conviteInvalido = ref(false)
const aceitando = ref(false)
const erroAceitar = ref<string | null>(null)
const aceito = ref(false)
const mostrarSenha = ref(false)

function nomePapel(papel: number) {
  return papel === PAPEL_PREDIO.GESTOR ? 'Gestor' : 'Colaborador'
}

onMounted(async () => {
  carregando.value = true
  try {
    await conviteStore.obter(token.value)
  } catch {
    conviteInvalido.value = true
  } finally {
    carregando.value = false
  }
})

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(aceitarConviteNovaContaFormSchema),
})
const [nome, nomeAttrs] = defineField('nome')
const [senha, senhaAttrs] = defineField('senha')

const onSubmitNovaConta = handleSubmit(async (values: AceitarConviteNovaContaForm) => {
  await aceitar(values)
})

async function vincularContaExistente() {
  await aceitar({})
}

async function aceitar(payload: { nome?: string; senha?: string }) {
  erroAceitar.value = null
  aceitando.value = true
  try {
    await conviteStore.aceitar(token.value, payload)
    aceito.value = true
  } catch {
    erroAceitar.value =
      'Não foi possível aceitar o convite. Ele pode ter expirado ou já ter sido usado.'
  } finally {
    aceitando.value = false
  }
}

function irParaLogin() {
  router.push('/login')
}
</script>

<template>
  <v-card rounded="lg" elevation="2" class="pa-2">
    <template v-if="carregando">
      <v-card-text class="d-flex justify-center py-8">
        <v-progress-circular indeterminate color="primary" />
      </v-card-text>
    </template>

    <template v-else-if="conviteInvalido">
      <v-card-item>
        <v-card-title class="text-h5 font-weight-bold">Convite inválido</v-card-title>
      </v-card-item>
      <v-card-text>
        <v-alert type="error" variant="tonal" density="compact" data-cy="erro-convite-invalido">
          Este convite não existe, expirou ou já foi usado.
        </v-alert>
        <v-btn variant="text" color="primary" class="mt-4" to="/login" data-cy="link-login">
          Ir para o login
        </v-btn>
      </v-card-text>
    </template>

    <template v-else-if="aceito">
      <v-card-item>
        <v-card-title class="text-h5 font-weight-bold">Convite aceito!</v-card-title>
      </v-card-item>
      <v-card-text>
        <p class="text-body-2 text-medium-emphasis mb-4">
          Agora é só entrar com sua conta para acessar o prédio.
        </p>
        <v-btn color="primary" size="large" block data-cy="btn-ir-login" @click="irParaLogin">
          Ir para o login
        </v-btn>
      </v-card-text>
    </template>

    <template v-else-if="convite">
      <v-card-item>
        <v-card-title class="text-h5 font-weight-bold">Convite para {{ convite.predioNome }}</v-card-title>
        <v-card-subtitle style="white-space: normal">
          Você foi convidado como {{ nomePapel(convite.papel) }}.
        </v-card-subtitle>
      </v-card-item>

      <v-card-text>
        <v-alert
          v-if="erroAceitar"
          type="error"
          variant="tonal"
          density="compact"
          class="mb-4"
          data-cy="erro-aceitar-convite"
        >
          {{ erroAceitar }}
        </v-alert>

        <template v-if="convite.contaJaExiste">
          <p class="text-body-2 text-medium-emphasis mb-4">
            Você já tem uma conta com este e-mail — vincule o acesso a este prédio a ela.
          </p>
          <v-btn
            color="primary"
            size="large"
            block
            :loading="aceitando"
            data-cy="btn-vincular-conta"
            @click="vincularContaExistente"
          >
            Vincular à minha conta
          </v-btn>
        </template>

        <v-form v-else @submit.prevent="onSubmitNovaConta">
          <v-text-field
            v-model="nome"
            v-bind="nomeAttrs"
            data-cy="nome"
            label="Nome"
            autocomplete="name"
            prepend-inner-icon="mdi-account-outline"
            :error-messages="errors.nome"
          />
          <v-text-field
            v-model="senha"
            v-bind="senhaAttrs"
            data-cy="senha"
            label="Senha"
            :type="mostrarSenha ? 'text' : 'password'"
            autocomplete="new-password"
            prepend-inner-icon="mdi-lock-outline"
            :append-inner-icon="mostrarSenha ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
            hint="Mínimo 8 caracteres, com maiúscula, minúscula, número e símbolo."
            persistent-hint
            :error-messages="errors.senha"
            @click:append-inner="mostrarSenha = !mostrarSenha"
          />
          <v-btn
            type="submit"
            color="primary"
            size="large"
            block
            class="mt-6"
            :loading="aceitando"
            data-cy="btn-criar-conta-convite"
          >
            Criar conta e aceitar convite
          </v-btn>
        </v-form>
      </v-card-text>
    </template>
  </v-card>
</template>
