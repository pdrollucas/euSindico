<script setup lang="ts">
import { computed, watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { gerarRelatorioFormSchema } from '@/schemas/relatorio.schema'
import type { GerarRelatorioFormValues } from '@/types/relatorio.types'
import { anosDisponiveis, mesesDoAno, periodoAtual, periodoEhFuturo } from '@/utils/periodo'

// Componente "burro": só escolhe o período e emite — quem gera é a view (ARCHITECTURE.md, seção 3).
defineProps<{ gerando?: boolean }>()
const emit = defineEmits<{ submit: [payload: GerarRelatorioFormValues] }>()

const { defineField, handleSubmit, errors, setFieldValue } = useForm({
  validationSchema: toTypedSchema(gerarRelatorioFormSchema),
  initialValues: periodoAtual(),
})

const [mes, mesAttrs] = defineField('mes')
const [ano, anoAttrs] = defineField('ano')

const anos = anosDisponiveis()
// Os meses futuros do ano escolhido aparecem desabilitados (RN34), evitando um 400 previsível.
const meses = computed(() => mesesDoAno(ano.value ?? periodoAtual().ano))

// Trocar para o ano corrente com um mês que ainda não começou (ex: dezembro, em outubro) puxa o
// mês para o último permitido em vez de deixar uma seleção inválida.
watch(ano, (novoAno) => {
  if (novoAno !== undefined && mes.value !== undefined && periodoEhFuturo(mes.value, novoAno)) {
    setFieldValue('mes', periodoAtual().mes)
  }
})

const onSubmit = handleSubmit((values) => {
  emit('submit', values)
})
</script>

<template>
  <v-form @submit.prevent="onSubmit">
    <v-select
      v-model="mes"
      v-bind="mesAttrs"
      data-cy="select-mes"
      label="Mês"
      :items="meses"
      prepend-inner-icon="mdi-calendar-month-outline"
      :error-messages="errors.mes"
    />
    <v-select
      v-model="ano"
      v-bind="anoAttrs"
      data-cy="select-ano"
      label="Ano"
      :items="anos"
      prepend-inner-icon="mdi-calendar-outline"
      :error-messages="errors.ano"
    />
    <v-btn
      type="submit"
      color="primary"
      size="large"
      block
      class="mt-2"
      :loading="gerando"
      data-cy="btn-gerar-relatorio"
    >
      Gerar relatório
    </v-btn>
  </v-form>
</template>
