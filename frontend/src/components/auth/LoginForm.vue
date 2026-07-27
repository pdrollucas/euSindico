<script setup lang="ts">
import { ref } from 'vue'
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

// Estado puramente de UI (mostrar/ocultar senha) — vive no componente, não vaza para o schema.
const mostrarSenha = ref(false)

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
      autocomplete="email"
      prepend-inner-icon="mdi-email-outline"
      :error-messages="errors.email"
    />
    <v-text-field
      v-model="senha"
      v-bind="senhaAttrs"
      data-cy="senha"
      label="Senha"
      :type="mostrarSenha ? 'text' : 'password'"
      autocomplete="current-password"
      prepend-inner-icon="mdi-lock-outline"
      :append-inner-icon="mostrarSenha ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
      :error-messages="errors.senha"
      @click:append-inner="mostrarSenha = !mostrarSenha"
    />
    <v-btn type="submit" color="primary" size="large" block class="mt-6" data-cy="btn-entrar">
      Entrar
    </v-btn>
  </v-form>
</template>
