<script setup lang="ts">
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import {
  ASSUNTO_SUPORTE_MAX,
  MENSAGEM_SUPORTE_MAX,
  enviarSuporteRequestSchema,
} from '@/schemas/perfil.schema'
import type { EnviarSuporteRequest } from '@/types/perfil.types'

// Componente "burro": valida assunto e mensagem e emite — quem envia é a view. Depois de um envio
// com sucesso a view troca `:key` para remontar o formulário vazio.
withDefaults(defineProps<{ carregando?: boolean }>(), { carregando: false })
const emit = defineEmits<{ submit: [payload: EnviarSuporteRequest] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(enviarSuporteRequestSchema),
})
const [assunto, assuntoAttrs] = defineField('assunto')
const [mensagem, mensagemAttrs] = defineField('mensagem')

const onSubmit = handleSubmit((values) => emit('submit', values))
</script>

<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="assunto"
      v-bind="assuntoAttrs"
      data-cy="suporte-assunto"
      label="Assunto"
      prepend-inner-icon="mdi-format-title"
      :maxlength="ASSUNTO_SUPORTE_MAX"
      :error-messages="errors.assunto"
    />
    <v-textarea
      v-model="mensagem"
      v-bind="mensagemAttrs"
      data-cy="suporte-mensagem"
      label="Mensagem"
      rows="6"
      auto-grow
      counter
      class="mt-3"
      :maxlength="MENSAGEM_SUPORTE_MAX"
      :error-messages="errors.mensagem"
    />
    <v-btn
      type="submit"
      color="primary"
      size="large"
      block
      class="mt-4"
      :loading="carregando"
      data-cy="btn-enviar-suporte"
    >
      Enviar mensagem
    </v-btn>
  </v-form>
</template>
