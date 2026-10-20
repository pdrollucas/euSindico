<script setup lang="ts">
import { computed } from 'vue'
import type { Predio } from '@/types/predio.types'
import {
  SITUACOES_COMPROMISSO,
  type CompromissoFiltros,
  type EscopoCompromissos,
  type SituacaoCompromissos,
} from '@/types/compromisso.types'

// Folha de filtros (RFC 3.1: prédio, atrasados, hoje, concluídos, meus/todos). Componente "burro":
// recebe os filtros atuais e emite o que mudou — quem aplica (store/rota) é a view.
const props = defineProps<{
  modelValue: boolean
  filtros: CompromissoFiltros
  predios: Predio[]
  // RF34/RN21: Colaborador só vê os próprios, então o seletor nem aparece para ele.
  mostrarEscopo: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  alterar: [parcial: Partial<CompromissoFiltros>]
  limpar: []
}>()

const itensPredio = computed(() => props.predios.map((p) => ({ title: p.nome, value: p.id })))

function onSituacao(valor: SituacaoCompromissos | undefined) {
  // v-chip-group sem `mandatory` devolve undefined ao desmarcar — volta ao padrão.
  emit('alterar', { situacao: valor ?? 'proximos' })
}

function onEscopo(valor: EscopoCompromissos | undefined) {
  if (valor) {
    emit('alterar', { escopo: valor })
  }
}
</script>

<template>
  <v-bottom-sheet :model-value="modelValue" @update:model-value="emit('update:modelValue', $event)">
    <v-card rounded="t-lg" class="compromisso-filtros mx-auto" data-cy="filtros-sheet">
      <v-card-item>
        <v-card-title class="text-h6 font-weight-bold">Filtros</v-card-title>
      </v-card-item>

      <v-card-text>
        <p class="text-subtitle-2 mb-2">Situação</p>
        <v-chip-group
          :model-value="filtros.situacao"
          selected-class="text-primary"
          class="mb-4"
          data-cy="filtro-situacao"
          @update:model-value="onSituacao"
        >
          <v-chip
            v-for="situacao in SITUACOES_COMPROMISSO"
            :key="situacao.valor"
            :value="situacao.valor"
            filter
            variant="outlined"
            :data-cy="`filtro-situacao-${situacao.valor}`"
          >
            {{ situacao.rotulo }}
          </v-chip>
        </v-chip-group>

        <v-select
          :model-value="filtros.predioId"
          :items="itensPredio"
          label="Prédio"
          placeholder="Todos os prédios"
          clearable
          prepend-inner-icon="mdi-office-building-outline"
          class="mb-2"
          data-cy="filtro-predio"
          @update:model-value="emit('alterar', { predioId: $event ?? null })"
        />

        <template v-if="mostrarEscopo">
          <p class="text-subtitle-2 mb-2">Mostrar</p>
          <v-btn-toggle
            :model-value="filtros.escopo"
            mandatory
            divided
            variant="outlined"
            color="primary"
            density="comfortable"
            class="w-100"
            data-cy="filtro-escopo"
            @update:model-value="onEscopo"
          >
            <v-btn value="meus" class="flex-grow-1" data-cy="filtro-escopo-meus">Meus</v-btn>
            <v-btn value="todos" class="flex-grow-1" data-cy="filtro-escopo-todos">Todos</v-btn>
          </v-btn-toggle>
        </template>
      </v-card-text>

      <v-card-actions class="pa-4 pt-0">
        <v-btn variant="text" data-cy="filtros-limpar" @click="emit('limpar')">Limpar filtros</v-btn>
        <v-spacer />
        <v-btn
          color="primary"
          variant="flat"
          data-cy="filtros-fechar"
          @click="emit('update:modelValue', false)"
        >
          Ver resultados
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-bottom-sheet>
</template>

<style scoped>
.compromisso-filtros {
  width: 100%;
  max-width: 480px;
}
</style>
