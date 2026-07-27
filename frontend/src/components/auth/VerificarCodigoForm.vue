<script setup lang="ts">
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { verificarCodigoFormSchema } from '@/schemas/auth.schema'
import type { VerificarCodigoFormValues } from '@/types/auth.types'

// Componente "burro": valida só o código (6 caracteres) e emite. O e-mail do fluxo vive na store
// e é adicionado pela view na chamada ao service.
withDefaults(defineProps<{ carregando?: boolean }>(), { carregando: false })
const emit = defineEmits<{ submit: [payload: VerificarCodigoFormValues] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(verificarCodigoFormSchema),
})
const [codigo, codigoAttrs] = defineField('codigo')

const onSubmit = handleSubmit((values) => emit('submit', values))
</script>

<template>
  <v-form @submit.prevent="onSubmit">
    <v-text-field
      v-model="codigo"
      v-bind="codigoAttrs"
      data-cy="codigo"
      label="Código"
      autocomplete="one-time-code"
      prepend-inner-icon="mdi-shield-key-outline"
      maxlength="6"
      class="codigo-field"
      :error-messages="errors.codigo"
    />
    <v-btn
      type="submit"
      color="primary"
      size="large"
      block
      class="mt-4"
      :loading="carregando"
      data-cy="btn-verificar"
    >
      Prosseguir
    </v-btn>
  </v-form>
</template>

<style scoped>
/* Só apresentação: o código é curto e alfanumérico — caixa alta, espaçado e centralizado ajuda
   a leitura e a digitação. O backend normaliza caixa/espacos, então o valor real não depende disto. */
.codigo-field :deep(input) {
  text-transform: uppercase;
  letter-spacing: 0.3em;
  text-align: center;
  font-weight: 600;
}
</style>
