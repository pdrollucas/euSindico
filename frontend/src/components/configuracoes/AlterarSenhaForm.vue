<script setup lang="ts">
import { ref } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { alterarSenhaFormSchema } from '@/schemas/perfil.schema'
import type { AlterarSenhaFormValues } from '@/types/perfil.types'

// Componente "burro": valida senha atual + nova senha (RNF04) + confirmação e emite.
withDefaults(defineProps<{ carregando?: boolean }>(), { carregando: false })
const emit = defineEmits<{ submit: [payload: AlterarSenhaFormValues] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(alterarSenhaFormSchema),
})
const [senhaAtual, senhaAtualAttrs] = defineField('senhaAtual')
const [novaSenha, novaSenhaAttrs] = defineField('novaSenha')
const [confirmarSenha, confirmarSenhaAttrs] = defineField('confirmarSenha')

// Estado puramente de UI (mostrar/ocultar senha) — vale para os três campos, não vaza pro schema.
const mostrarSenha = ref(false)

const onSubmit = handleSubmit((values) => emit('submit', values))
</script>

<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="senhaAtual"
      v-bind="senhaAtualAttrs"
      data-cy="senha-atual"
      label="Senha atual"
      :type="mostrarSenha ? 'text' : 'password'"
      autocomplete="current-password"
      prepend-inner-icon="mdi-lock-outline"
      :append-inner-icon="mostrarSenha ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"
      :error-messages="errors.senhaAtual"
      @click:append-inner="mostrarSenha = !mostrarSenha"
    />
    <v-text-field
      v-model="novaSenha"
      v-bind="novaSenhaAttrs"
      data-cy="nova-senha"
      label="Nova senha"
      :type="mostrarSenha ? 'text' : 'password'"
      autocomplete="new-password"
      prepend-inner-icon="mdi-lock-plus-outline"
      hint="Mínimo 8 caracteres, com maiúscula, minúscula, número e símbolo."
      persistent-hint
      class="mt-3"
      :error-messages="errors.novaSenha"
    />
    <v-text-field
      v-model="confirmarSenha"
      v-bind="confirmarSenhaAttrs"
      data-cy="confirmar-senha"
      label="Confirmar nova senha"
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
      data-cy="btn-alterar-senha"
    >
      Alterar senha
    </v-btn>
  </v-form>
</template>
