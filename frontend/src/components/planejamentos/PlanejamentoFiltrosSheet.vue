<script setup lang="ts">
import type { PlanejamentoFiltros } from '@/types/planejamento.types'
import { useFaixaOrcamento } from '@/composables/useFaixaOrcamento'

// Folha de filtros do planejamento (celular): faixa de orçamento (a busca por título fica no campo
// de busca da tela). No desktop os mesmos campos ficam na barra (PlanejamentoFiltrosBarra).
const props = defineProps<{
  modelValue: boolean
  filtros: PlanejamentoFiltros
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  alterar: [parcial: Partial<PlanejamentoFiltros>]
  limpar: []
}>()

const { minimo, maximo, erroMinimo, minimoCampo, maximoCampo, digitado, aplicar } =
  useFaixaOrcamento(
    () => props.filtros,
    (parcial) => emit('alterar', parcial),
  )

function fechar() {
  if (aplicar()) {
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
        <v-btn variant="text" data-cy="filtros-limpar" @click="emit('limpar')"
          >Limpar filtros</v-btn
        >
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
