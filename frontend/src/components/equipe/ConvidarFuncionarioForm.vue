<script setup lang="ts">
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { convidarFuncionarioRequestSchema } from '@/schemas/equipe.schema'
import type { ConvidarFuncionarioRequest } from '@/types/equipe.types'
import { PAPEL_PREDIO } from '@/types/predio.types'

defineProps<{ enviando?: boolean }>()
const emit = defineEmits<{ submit: [payload: ConvidarFuncionarioRequest] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(convidarFuncionarioRequestSchema),
  initialValues: { papel: PAPEL_PREDIO.COLABORADOR },
})

const [email, emailAttrs] = defineField('email')
const [papel, papelAttrs] = defineField('papel')

const opcoesPapel = [
  { title: 'Gestor', value: PAPEL_PREDIO.GESTOR },
  { title: 'Colaborador', value: PAPEL_PREDIO.COLABORADOR },
]

const onSubmit = handleSubmit((values) => {
  emit('submit', values)
})
</script>

<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="email"
      v-bind="emailAttrs"
      data-cy="email-convite"
      label="E-mail"
      type="email"
      autocomplete="email"
      prepend-inner-icon="mdi-email-outline"
      :error-messages="errors.email"
    />
    <v-select
      v-model="papel"
      v-bind="papelAttrs"
      data-cy="papel-convite"
      label="Papel"
      :items="opcoesPapel"
      prepend-inner-icon="mdi-shield-account-outline"
      :error-messages="errors.papel"
    />
    <v-btn
      type="submit"
      color="primary"
      size="large"
      block
      class="mt-2"
      :loading="enviando"
      data-cy="btn-enviar-convite"
    >
      Enviar convite
    </v-btn>
  </v-form>
</template>
