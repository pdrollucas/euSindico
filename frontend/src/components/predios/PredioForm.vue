<script setup lang="ts">
import { watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { predioFormSchema } from '@/schemas/predio.schema'
import type { PredioFormValues } from '@/types/predio.types'

// Reaproveitado na criação e na edição — mesmo formato (PredioFormDto no backend).
const props = defineProps<{
  valoresIniciais?: PredioFormValues
  salvando?: boolean
  modoEdicao?: boolean
}>()
const emit = defineEmits<{ submit: [payload: PredioFormValues] }>()

const { defineField, handleSubmit, errors, setValues } = useForm({
  validationSchema: toTypedSchema(predioFormSchema),
  initialValues: props.valoresIniciais,
})

const [nome, nomeAttrs] = defineField('nome')
const [endereco, enderecoAttrs] = defineField('endereco')

// Os valores iniciais só chegam depois de um GET assíncrono (modo edição) — o form já existe
// antes disso, então precisa reagir quando eles carregarem.
watch(
  () => props.valoresIniciais,
  (valores) => {
    if (valores) {
      setValues(valores)
    }
  },
)

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
      label="Nome do prédio"
      placeholder="Ex: Edifício Solar"
      autocomplete="off"
      prepend-inner-icon="mdi-office-building-outline"
      :error-messages="errors.nome"
    />
    <v-textarea
      v-model="endereco"
      v-bind="enderecoAttrs"
      data-cy="endereco"
      label="Endereço"
      placeholder="Rua, número, bairro"
      autocomplete="off"
      rows="1"
      auto-grow
      prepend-inner-icon="mdi-map-marker-outline"
      :error-messages="errors.endereco"
    />
    <v-btn
      type="submit"
      color="primary"
      size="large"
      block
      class="mt-4"
      :loading="salvando"
      data-cy="btn-salvar-predio"
    >
      {{ modoEdicao ? 'Salvar alterações' : 'Criar prédio' }}
    </v-btn>
  </v-form>
</template>
