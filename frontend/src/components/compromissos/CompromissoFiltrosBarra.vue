<script setup lang="ts">
import { computed } from 'vue'
import type { Predio } from '@/types/predio.types'
import { SITUACOES_COMPROMISSO, type CompromissoFiltros } from '@/types/compromisso.types'
import { periodoObrigatorio } from '@/utils/periodo'
import {
  ITENS_ANO_COMPROMISSO,
  ITENS_MES,
  parcialAoEscolherAno,
  parcialAoEscolherMes,
} from '@/utils/filtrosCompromisso'

// Desktop: busca (slot) e filtros à vista. Mesmos filtros da folha do celular
// (CompromissoFiltrosSheet); quem aplica é a view.
const props = defineProps<{
  filtros: CompromissoFiltros
  predios: Predio[]
  // RF34/RN21: Colaborador só vê os próprios.
  mostrarEscopo: boolean
  filtrosAtivos: boolean
}>()

const emit = defineEmits<{
  alterar: [parcial: Partial<CompromissoFiltros>]
  limpar: []
}>()

const itensSituacao = SITUACOES_COMPROMISSO.map((s) => ({ title: s.rotulo, value: s.valor }))
const itensPredio = computed(() => props.predios.map((p) => ({ title: p.nome, value: p.id })))
const obrigatorio = computed(() => periodoObrigatorio(props.filtros.situacao))
const semPeriodo = computed(() => props.filtros.situacao === 'hoje')
</script>

<template>
  <!-- Cada peça tem lugar fixo na grade: o que não se aplica fica desabilitado, não some, para a
       barra não mudar de forma. -->
  <div class="filtros-barra" data-cy="filtros-barra">
    <div class="area-busca">
      <slot name="busca" />
    </div>

    <v-select
      :model-value="filtros.situacao"
      :items="itensSituacao"
      label="Situação"
      density="compact"
      hide-details
      class="area-situacao"
      data-cy="filtro-situacao"
      @update:model-value="emit('alterar', { situacao: $event })"
    />

    <v-select
      :model-value="filtros.predioId"
      :items="itensPredio"
      label="Prédio"
      placeholder="Todos"
      persistent-placeholder
      clearable
      density="compact"
      hide-details
      class="area-predio"
      data-cy="filtro-predio"
      @update:model-value="emit('alterar', { predioId: $event ?? null })"
    />

    <v-select
      :model-value="filtros.mes"
      :items="ITENS_MES"
      label="Mês"
      :placeholder="semPeriodo ? '—' : 'Qualquer'"
      persistent-placeholder
      :clearable="!obrigatorio"
      :disabled="semPeriodo"
      density="compact"
      hide-details
      class="area-mes"
      data-cy="filtro-mes"
      @update:model-value="emit('alterar', parcialAoEscolherMes($event, filtros))"
    />

    <v-select
      :model-value="filtros.ano"
      :items="ITENS_ANO_COMPROMISSO"
      label="Ano"
      :placeholder="semPeriodo ? '—' : 'Qualquer'"
      persistent-placeholder
      :clearable="!obrigatorio"
      :disabled="semPeriodo"
      density="compact"
      hide-details
      class="area-ano"
      data-cy="filtro-ano"
      @update:model-value="emit('alterar', parcialAoEscolherAno($event, filtros))"
    />

    <div class="area-acoes d-flex align-center justify-end ga-2">
      <v-btn-toggle
        v-if="mostrarEscopo"
        :model-value="filtros.escopo"
        mandatory
        divided
        variant="outlined"
        color="primary"
        density="compact"
        data-cy="filtro-escopo"
        @update:model-value="emit('alterar', { escopo: $event })"
      >
        <v-btn value="meus" data-cy="filtro-escopo-meus">Meus</v-btn>
        <v-btn value="todos" data-cy="filtro-escopo-todos">Todos</v-btn>
      </v-btn-toggle>

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
  </div>
</template>

<style scoped>
/* Até 1279px, duas linhas em 4 colunas iguais; a partir de 1280px, uma linha só. */
.filtros-barra {
  display: grid;
  gap: 8px 12px;
  align-items: center;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  grid-template-areas:
    'busca busca acoes acoes'
    'situacao predio mes ano';
}

@media (min-width: 1280px) {
  .filtros-barra {
    grid-template-columns: minmax(200px, 1.4fr) repeat(4, minmax(0, 1fr)) auto;
    grid-template-areas: 'busca situacao predio mes ano acoes';
  }
}

.area-busca {
  grid-area: busca;
}

.area-situacao {
  grid-area: situacao;
}

.area-predio {
  grid-area: predio;
}

.area-mes {
  grid-area: mes;
}

.area-ano {
  grid-area: ano;
}

.area-acoes {
  grid-area: acoes;
}
</style>
