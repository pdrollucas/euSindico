<script setup lang="ts">
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { loginRequestSchema } from '@/schemas/auth.schema'
import type { LoginRequest } from '@/types/auth.types'

// Componente "burro": só valida e emite — sem conhecer store/axios (ver
// frontend/documentation/ARCHITECTURE.md, seção 2).
const emit = defineEmits<{ submit: [payload: LoginRequest] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(loginRequestSchema),
})

const [email, emailAttrs] = defineField('email')
const [senha, senhaAttrs] = defineField('senha')

const onSubmit = handleSubmit((values) => {
  emit('submit', values)
})
</script>

<template>
  <v-form @submit.prevent="onSubmit">
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
    <v-btn type="submit" color="primary" block data-cy="btn-entrar">Entrar</v-btn>
  </v-form>
</template>
