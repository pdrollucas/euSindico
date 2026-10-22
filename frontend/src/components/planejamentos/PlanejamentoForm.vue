<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { planejamentoFormSchema } from '@/schemas/planejamento.schema'
import { hojeIso } from '@/utils/dataHora'
import { mascararMoedaDigitada } from '@/utils/moeda'
import type { PlanejamentoFormValues } from '@/types/planejamento.types'

// Reaproveitado na criação e na edição — mesmo formato (PlanejamentoFormDto no backend). Componente
// "burro": quem carrega os dados é a view. O Colaborador só lê (RN29), então `somenteLeitura` trava
// os campos e esconde o botão.
const props = defineProps<{
  valoresIniciais?: Partial<PlanejamentoFormValues>
  salvando?: boolean
  modoEdicao?: boolean
  somenteLeitura?: boolean
}>()
const emit = defineEmits<{ submit: [payload: PlanejamentoFormValues] }>()

const { defineField, handleSubmit, errors, setValues } = useForm({
  validationSchema: toTypedSchema(planejamentoFormSchema),
  initialValues: props.valoresIniciais,
})

const [titulo, tituloAttrs] = defineField('titulo')
const [dataPlanejamento, dataAttrs] = defineField('dataPlanejamento')
const [orcamentoPrevisto, orcamentoAttrs] = defineField('orcamentoPrevisto')
const [nomePrestadorServico, prestadorAttrs] = defineField('nomePrestadorServico')
const [detalhes, detalhesAttrs] = defineField('detalhes')

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

// Aplica a máscara e devolve o valor mascarado ao próprio <input>: quando o texto mascarado é igual
// ao anterior (ex: uma letra digitada), o modelo não muda, o Vue não re-renderiza e a letra
// ficaria visível no campo (mesmo cuidado da máscara de horário em CompromissoForm).
const orcamentoCampo = ref<{ $el: HTMLElement } | null>(null)
function onOrcamentoDigitado(valor: string | null) {
  const mascarado = mascararMoedaDigitada(valor, orcamentoPrevisto.value ?? '')
  orcamentoPrevisto.value = mascarado
  nextTick(() => {
    const input = orcamentoCampo.value?.$el.querySelector('input')
    if (input && input.value !== mascarado) {
      input.value = mascarado
    }
  })
}

// Mesmo recurso de CompromissoForm: o ícone nativo do <input type="date"> fica escondido, então o
// clique em qualquer parte do campo abre o seletor. `showPicker` pode lançar — o foco no input
// continua funcionando.
function abrirSeletor(evento: Event) {
  if (props.somenteLeitura) {
    return
  }
  const input = (evento.currentTarget as HTMLElement).querySelector('input')
  try {
    input?.showPicker?.()
  } catch {
    // Sem seletor nativo: o usuário digita.
  }
}

const onSubmit = handleSubmit((values) => emit('submit', values))
</script>

<template>
  <v-form class="planejamento-form" :readonly="somenteLeitura" @submit.prevent="onSubmit">
    <v-text-field
      v-model="titulo"
      v-bind="tituloAttrs"
      data-cy="titulo"
      label="Título"
      placeholder="Ex: Pintura da fachada"
      autocomplete="off"
      prepend-inner-icon="mdi-text-short"
      :error-messages="errors.titulo"
    />
    <v-text-field
      v-model="dataPlanejamento"
      v-bind="dataAttrs"
      data-cy="data"
      type="date"
      :min="hojeIso()"
      label="Data"
      hint="Opcional"
      persistent-placeholder
      prepend-inner-icon="mdi-calendar-outline"
      :error-messages="errors.dataPlanejamento"
      @click="abrirSeletor"
    />
    <!-- Máscara de moeda: `inputmode="numeric"` abre o teclado numérico (sem letras) no celular, e a
         vírgula e os pontos entram sozinhos ("150050" -> "1.500,50"). -->
    <v-text-field
      ref="orcamentoCampo"
      v-bind="orcamentoAttrs"
      :model-value="orcamentoPrevisto"
      data-cy="orcamento"
      inputmode="numeric"
      maxlength="14"
      label="Orçamento previsto"
      placeholder="0,00 (opcional)"
      persistent-placeholder
      autocomplete="off"
      prefix="R$"
      prepend-inner-icon="mdi-cash"
      :error-messages="errors.orcamentoPrevisto"
      @update:model-value="onOrcamentoDigitado"
    />
    <v-text-field
      v-model="nomePrestadorServico"
      v-bind="prestadorAttrs"
      data-cy="prestador"
      label="Prestador de serviços"
      placeholder="Digite o nome (opcional)"
      autocomplete="off"
      prepend-inner-icon="mdi-account-hard-hat-outline"
      :error-messages="errors.nomePrestadorServico"
    />
    <v-textarea
      v-model="detalhes"
      v-bind="detalhesAttrs"
      data-cy="detalhes"
      label="Detalhes"
      placeholder="Digite os detalhes (opcional)"
      autocomplete="off"
      rows="2"
      auto-grow
      prepend-inner-icon="mdi-text-box-outline"
      :error-messages="errors.detalhes"
    />

    <div v-if="!somenteLeitura" class="planejamento-form__acao">
      <v-btn
        type="submit"
        color="primary"
        size="large"
        block
        :loading="salvando"
        data-cy="btn-salvar-planejamento"
      >
        {{ modoEdicao ? 'Salvar alterações' : 'Criar planejamento' }}
      </v-btn>
    </div>
  </v-form>
</template>

<style scoped>
/* Esconde o indicador nativo de calendário (duplicava o ícone à esquerda). */
.planejamento-form :deep(input[type='date']::-webkit-calendar-picker-indicator) {
  display: none;
}

/* Botão fixo no rodapé enquanto o formulário rola — fundo sólido para não deixar os campos
   aparecerem por baixo (mesmo padrão de CompromissoForm). */
.planejamento-form__acao {
  position: sticky;
  bottom: 0;
  z-index: 1;
  margin-inline: -16px;
  padding: 12px 16px;
  background: rgb(var(--v-theme-background));
}
</style>
