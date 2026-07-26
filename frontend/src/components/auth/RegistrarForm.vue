<script setup lang="ts">
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { registrarRequestSchema } from '@/schemas/auth.schema'
import type { RegistrarRequest } from '@/types/auth.types'

// Componente "burro": só valida e emite — sem conhecer store/axios (ver
// frontend/documentation/ARCHITECTURE.md, seção 2).
const emit = defineEmits<{ submit: [payload: RegistrarRequest] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(registrarRequestSchema),
})

const [nome, nomeAttrs] = defineField('nome')
const [email, emailAttrs] = defineField('email')
const [senha, senhaAttrs] = defineField('senha')

const onSubmit = handleSubmit((values) => {
  emit('submit', values)
})
</script>

<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="nome"
      v-bind="nomeAttrs"
      data-cy="nome"
      label="Nome"
      :error-messages="errors.nome"
    />
    <v-text-field
      v-model="email"
      v-bind="emailAttrs"
      data-cy="email"
      label="E-mail"
      type="email"
      :error-messages="errors.email"
    />
    <v-text-field
      v-model="senha"
      v-bind="senhaAttrs"
      data-cy="senha"
      label="Senha"
      type="password"
      :error-messages="errors.senha"
    />
    <v-btn type="submit" color="primary" block data-cy="btn-registrar">Criar conta</v-btn>
  </v-form>
</template>
