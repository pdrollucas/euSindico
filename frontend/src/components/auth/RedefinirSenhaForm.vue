<script setup lang="ts">
import { ref } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { redefinirSenhaFormSchema } from '@/schemas/auth.schema'
import type { RedefinirSenhaFormValues } from '@/types/auth.types'

// Componente "burro": valida nova senha (RNF04) + confirmação (coincidem) e emite. O e-mail e o
// código do fluxo vivem na store e são adicionados pela view na chamada ao service.
withDefaults(defineProps<{ carregando?: boolean }>(), { carregando: false })
const emit = defineEmits<{ submit: [payload: RedefinirSenhaFormValues] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(redefinirSenhaFormSchema),
})
const [novaSenha, novaSenhaAttrs] = defineField('novaSenha')
const [confirmarSenha, confirmarSenhaAttrs] = defineField('confirmarSenha')

// Estado puramente de UI (mostrar/ocultar senha) — vale para os dois campos, não vaza pro schema.
const mostrarSenha = ref(false)

const onSubmit = handleSubmit((values) => emit('submit', values))
</script>

<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="novaSenha"
      v-bind="novaSenhaAttrs"
      data-cy="nova-senha"
      label="Nova senha"
      :type="mostrarSenha ? 'text' : 'password'"
      autocomplete="new-password"
      prepend-inner-icon="mdi-lock-outline"
      :append-inner-icon="mostrarSenha ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
      hint="Mínimo 8 caracteres, com maiúscula, minúscula, número e símbolo."
      persistent-hint
      :error-messages="errors.novaSenha"
      @click:append-inner="mostrarSenha = !mostrarSenha"
    />
    <v-text-field
      v-model="confirmarSenha"
      v-bind="confirmarSenhaAttrs"
      data-cy="confirmar-senha"
      label="Confirmar senha"
      :type="mostrarSenha ? 'text' : 'password'"
      autocomplete="new-password"
      prepend-inner-icon="mdi-lock-check-outline"
      class="mt-3"
      :error-messages="errors.confirmarSenha"
    />
    <v-btn
      type="submit"
      color="primary"
      size="large"
      block
      class="mt-6"
      :loading="carregando"
      data-cy="btn-atualizar-senha"
    >
      Atualizar senha
    </v-btn>
  </v-form>
</template>
