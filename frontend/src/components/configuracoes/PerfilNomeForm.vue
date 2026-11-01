<script setup lang="ts">
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { atualizarPerfilRequestSchema } from '@/schemas/perfil.schema'
import type { AtualizarPerfilRequest } from '@/types/perfil.types'

// Componente "burro": valida o nome e emite — quem salva é a view (ARCHITECTURE.md, seção 3).
const props = withDefaults(defineProps<{ nomeInicial?: string; carregando?: boolean }>(), {
  nomeInicial: '',
  carregando: false,
})
const emit = defineEmits<{ submit: [payload: AtualizarPerfilRequest] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(atualizarPerfilRequestSchema),
  initialValues: { nome: props.nomeInicial },
})
const [nome, nomeAttrs] = defineField('nome')

const onSubmit = handleSubmit((values) => emit('submit', values))
</script>

<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="nome"
      v-bind="nomeAttrs"
      data-cy="perfil-nome"
      label="Nome"
      autocomplete="name"
      prepend-inner-icon="mdi-account-outline"
      :error-messages="errors.nome"
    />
    <v-btn
      type="submit"
      color="primary"
      size="large"
      block
      class="mt-2"
      :loading="carregando"
      data-cy="btn-salvar-nome"
    >
      Salvar nome
    </v-btn>
  </v-form>
</template>
