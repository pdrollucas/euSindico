<script setup lang="ts">
import { nextTick, ref, watch } from 'vue'
import type { PlanejamentoFiltros } from '@/types/planejamento.types'
import {
  mascararMoedaDigitada,
  moedaParaNumero,
  numeroParaMoedaMascarada,
} from '@/utils/moeda'

// Folha de filtros do planejamento: faixa de orçamento (a busca por título fica no campo de busca
// da tela). Componente "burro": recebe os filtros atuais e emite o que mudou — quem aplica
// (store) é a view. Diferente de compromissos, os campos são digitados: aplicam ao sair do campo,
// no Enter ou em "Ver resultados", não a cada tecla.
const props = defineProps<{
  modelValue: boolean
  filtros: PlanejamentoFiltros
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  alterar: [parcial: Partial<PlanejamentoFiltros>]
  limpar: []
}>()

const paraCampo = (valor: number | null) =>
  valor === null ? '' : numeroParaMoedaMascarada(valor)

const minimo = ref(paraCampo(props.filtros.orcamentoMinimo))
const maximo = ref(paraCampo(props.filtros.orcamentoMaximo))
const erroMinimo = ref<string | null>(null)
const erroMaximo = ref<string | null>(null)

// "Limpar filtros" e os chips da tela mudam os filtros por fora — os campos acompanham.
watch(
  () => props.filtros,
  (filtros) => {
    minimo.value = paraCampo(filtros.orcamentoMinimo)
    maximo.value = paraCampo(filtros.orcamentoMaximo)
    erroMinimo.value = null
    erroMaximo.value = null
  },
  { deep: true },
)

// Mesma máscara de moeda do formulário (ver PlanejamentoForm.vue): o valor mascarado volta ao
// próprio <input>, senão uma letra digitada ficaria visível quando o texto mascarado não muda.
const minimoCampo = ref<{ $el: HTMLElement } | null>(null)
const maximoCampo = ref<{ $el: HTMLElement } | null>(null)

function digitado(valor: string | null, limite: 'minimo' | 'maximo') {
  const alvo = limite === 'minimo' ? minimo : maximo
  const campo = limite === 'minimo' ? minimoCampo : maximoCampo
  const mascarado = mascararMoedaDigitada(valor, alvo.value)
  alvo.value = mascarado
  nextTick(() => {
    const input = campo.value?.$el.querySelector('input')
    if (input && input.value !== mascarado) {
      input.value = mascarado
    }
  })
}

function aplicar() {
  const novoMinimo = moedaParaNumero(minimo.value)
  const novoMaximo = moedaParaNumero(maximo.value)

  erroMinimo.value = null
  erroMaximo.value = null
  if (novoMinimo !== null && novoMaximo !== null && novoMinimo > novoMaximo) {
    erroMinimo.value = 'O mínimo não pode ser maior que o máximo'
    return
  }

  const parcial: Partial<PlanejamentoFiltros> = {}
  if (novoMinimo !== props.filtros.orcamentoMinimo) {
    parcial.orcamentoMinimo = novoMinimo
  }
  if (novoMaximo !== props.filtros.orcamentoMaximo) {
    parcial.orcamentoMaximo = novoMaximo
  }
  if (Object.keys(parcial).length > 0) {
    emit('alterar', parcial)
  }
}

function fechar() {
  aplicar()
  if (!erroMinimo.value && !erroMaximo.value) {
    emit('update:modelValue', false)
  }
}
</script>

<template>
  <v-bottom-sheet :model-value="modelValue" @update:model-value="emit('update:modelValue', $event)">
    <v-card rounded="t-lg" class="planejamento-filtros mx-auto" data-cy="filtros-sheet">
      <v-card-item>
        <v-card-title class="text-h6 font-weight-bold">Filtros</v-card-title>
      </v-card-item>

      <v-card-text>
        <p class="text-subtitle-2 mb-2">Orçamento previsto</p>
        <div class="d-flex ga-3">
          <v-text-field
            ref="minimoCampo"
            :model-value="minimo"
            data-cy="filtro-orcamento-minimo"
            inputmode="numeric"
            maxlength="14"
            label="Mínimo"
            placeholder="0,00"
            persistent-placeholder
            autocomplete="off"
            prefix="R$"
            :error-messages="erroMinimo ?? undefined"
            @update:model-value="digitado($event, 'minimo')"
            @blur="aplicar"
            @keydown.enter="aplicar"
          />
          <v-text-field
            ref="maximoCampo"
            :model-value="maximo"
            data-cy="filtro-orcamento-maximo"
            inputmode="numeric"
            maxlength="14"
            label="Máximo"
            placeholder="0,00"
            persistent-placeholder
            autocomplete="off"
            prefix="R$"
            :error-messages="erroMaximo ?? undefined"
            @update:model-value="digitado($event, 'maximo')"
            @blur="aplicar"
            @keydown.enter="aplicar"
          />
        </div>
        <p class="text-caption text-medium-emphasis">
          Planejamentos sem orçamento não aparecem quando um limite é informado.
        </p>
      </v-card-text>

      <v-card-actions class="pa-4 pt-0">
        <v-btn variant="text" data-cy="filtros-limpar" @click="emit('limpar')">Limpar filtros</v-btn>
        <v-spacer />
        <v-btn color="primary" variant="flat" data-cy="filtros-fechar" @click="fechar">
          Ver resultados
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-bottom-sheet>
</template>

<style scoped>
.planejamento-filtros {
  width: 100%;
  max-width: 480px;
}
</style>
