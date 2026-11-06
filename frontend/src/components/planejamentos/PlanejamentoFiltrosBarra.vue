<script setup lang="ts">
import type { PlanejamentoFiltros } from '@/types/planejamento.types'
import { useFaixaOrcamento } from '@/composables/useFaixaOrcamento'

// Desktop: busca (slot) e faixa de orçamento à vista. Mesmos campos da folha do celular
// (PlanejamentoFiltrosSheet); quem aplica é a view.
const props = defineProps<{
  filtros: PlanejamentoFiltros
  filtrosAtivos: boolean
}>()

const emit = defineEmits<{
  alterar: [parcial: Partial<PlanejamentoFiltros>]
  limpar: []
}>()

const { minimo, maximo, erroMinimo, minimoCampo, maximoCampo, digitado, aplicar } =
  useFaixaOrcamento(
    () => props.filtros,
    (parcial) => emit('alterar', parcial),
  )
</script>

<template>
  <div class="filtros-barra" data-cy="filtros-barra">
    <slot name="busca" />

    <v-text-field
      ref="minimoCampo"
      :model-value="minimo"
      data-cy="filtro-orcamento-minimo"
      inputmode="numeric"
      maxlength="14"
      label="Orçamento mínimo"
      placeholder="0,00"
      persistent-placeholder
      autocomplete="off"
      prefix="R$"
      density="compact"
      hide-details="auto"
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
      label="Orçamento máximo"
      placeholder="0,00"
      persistent-placeholder
      autocomplete="off"
      prefix="R$"
      density="compact"
      hide-details
      @update:model-value="digitado($event, 'maximo')"
      @blur="aplicar"
      @keydown.enter="aplicar"
    />

    <v-btn
      variant="text"
      color="primary"
      :disabled="!filtrosAtivos"
      data-cy="filtros-limpar"
      @click="emit('limpar')"
    >
      Limpar filtros
    </v-btn>
  </div>
</template>

<style scoped>
.filtros-barra {
  display: grid;
  grid-template-columns: minmax(0, 2fr) minmax(0, 1fr) minmax(0, 1fr) auto;
  gap: 12px;
  align-items: start;
}
</style>
