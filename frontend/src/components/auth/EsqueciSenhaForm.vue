<script setup lang="ts">
import { computed } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { esqueciSenhaRequestSchema } from '@/schemas/auth.schema'
import type { EsqueciSenhaRequest } from '@/types/auth.types'
import { formatarContagem } from '@/utils/tempo'

// Componente "burro": valida o e-mail e emite. O cooldown (quantos segundos faltam) e o estado
// de carregando são decididos pela view (a partir da store do fluxo) e chegam como props — o
// form só reflete: quando em cooldown, o botão fica desabilitado com a contagem regressiva.
const props = withDefaults(
  defineProps<{ carregando?: boolean; cooldownSegundos?: number; emailInicial?: string }>(),
  { carregando: false, cooldownSegundos: 0, emailInicial: '' },
)
const emit = defineEmits<{ submit: [payload: EsqueciSenhaRequest] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(esqueciSenhaRequestSchema),
  initialValues: { email: props.emailInicial },
})
const [email, emailAttrs] = defineField('email')

const emCooldown = computed(() => props.cooldownSegundos > 0)
const rotuloBotao = computed(() =>
  emCooldown.value ? `Aguarde ${formatarContagem(props.cooldownSegundos)}` : 'Enviar código',
)

const onSubmit = handleSubmit((values) => emit('submit', values))
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
    <v-btn
      type="submit"
      color="primary"
      size="large"
      block
      class="mt-4"
      :loading="carregando"
      :disabled="emCooldown"
      data-cy="btn-enviar-codigo"
    >
      {{ rotuloBotao }}
    </v-btn>
  </v-form>
</template>
