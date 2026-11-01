<script setup lang="ts">
import { ref } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { excluirContaRequestSchema } from '@/schemas/perfil.schema'
import type { ExcluirContaRequest } from '@/types/perfil.types'

// Componente "burro": pede a senha como confirmação de uma ação irreversível e emite.
withDefaults(defineProps<{ carregando?: boolean }>(), { carregando: false })
const emit = defineEmits<{ submit: [payload: ExcluirContaRequest] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(excluirContaRequestSchema),
})
const [senha, senhaAttrs] = defineField('senha')

const mostrarSenha = ref(false)

const onSubmit = handleSubmit((values) => emit('submit', values))
</script>

<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="senha"
      v-bind="senhaAttrs"
      data-cy="senha-excluir-conta"
      label="Senha"
      :type="mostrarSenha ? 'text' : 'password'"
      autocomplete="current-password"
      prepend-inner-icon="mdi-lock-outline"
      :append-inner-icon="mostrarSenha ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
      :error-messages="errors.senha"
      @click:append-inner="mostrarSenha = !mostrarSenha"
    />
    <v-btn
      type="submit"
      color="error"
      size="large"
      block
      class="mt-4"
      :loading="carregando"
      data-cy="btn-confirmar-excluir-conta"
    >
      Excluir definitivamente
    </v-btn>
  </v-form>
</template>
