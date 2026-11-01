<script setup lang="ts">
import { ref } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { solicitarTrocaEmailRequestSchema } from '@/schemas/perfil.schema'
import type { SolicitarTrocaEmailRequest } from '@/types/perfil.types'

// Passo 1 da troca de e-mail: novo e-mail + senha atual. Componente "burro": valida e emite.
withDefaults(defineProps<{ carregando?: boolean }>(), { carregando: false })
const emit = defineEmits<{ submit: [payload: SolicitarTrocaEmailRequest] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(solicitarTrocaEmailRequestSchema),
})
const [novoEmail, novoEmailAttrs] = defineField('novoEmail')
const [senhaAtual, senhaAtualAttrs] = defineField('senhaAtual')

const mostrarSenha = ref(false)

const onSubmit = handleSubmit((values) => emit('submit', values))
</script>

<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="novoEmail"
      v-bind="novoEmailAttrs"
      data-cy="novo-email"
      label="Novo e-mail"
      type="email"
      autocomplete="email"
      prepend-inner-icon="mdi-email-outline"
      :error-messages="errors.novoEmail"
    />
    <v-text-field
      v-model="senhaAtual"
      v-bind="senhaAtualAttrs"
      data-cy="senha-troca-email"
      label="Senha atual"
      :type="mostrarSenha ? 'text' : 'password'"
      autocomplete="current-password"
      prepend-inner-icon="mdi-lock-outline"
      :append-inner-icon="mostrarSenha ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
      class="mt-3"
      :error-messages="errors.senhaAtual"
      @click:append-inner="mostrarSenha = !mostrarSenha"
    />
    <v-btn
      type="submit"
      color="primary"
      size="large"
      block
      class="mt-4"
      :loading="carregando"
      data-cy="btn-enviar-codigo-email"
    >
      Enviar código
    </v-btn>
  </v-form>
</template>
