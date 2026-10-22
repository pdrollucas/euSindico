<script setup lang="ts">
import { computed } from 'vue'
import type { Planejamento } from '@/types/planejamento.types'
import { formatarData } from '@/utils/dataHora'
import { formatarMoeda } from '@/utils/moeda'

// Sem checkbox: planejamento nunca é "concluído" (RN11). A lixeira só aparece para quem gerencia
// (Síndico/Gestor, RN29) — o Colaborador só lê. A lista é de um prédio só, então o item não repete
// o nome do prédio.
const props = defineProps<{
  planejamento: Planejamento
  podeRemover?: boolean
}>()
const emit = defineEmits<{ remover: [] }>()

const destino = computed(
  () => `/predios/${props.planejamento.predioId}/planejamentos/${props.planejamento.id}`,
)

const data = computed(() =>
  props.planejamento.dataPlanejamento
    ? formatarData(props.planejamento.dataPlanejamento)
    : 'Sem data',
)

// Zero é um orçamento válido ("R$ 0,00"); só `null` (não informado) omite a linha.
const orcamento = computed(() =>
  props.planejamento.orcamentoPrevisto === null
    ? null
    : formatarMoeda(props.planejamento.orcamentoPrevisto),
)
</script>

<template>
  <v-card
    variant="flat"
    border
    rounded="lg"
    class="planejamento-item"
    :data-cy="`planejamento-item-${planejamento.id}`"
  >
    <div class="d-flex align-center pa-3 ga-1">
      <router-link
        :to="destino"
        class="planejamento-item__conteudo d-flex flex-column"
        :data-cy="`planejamento-link-${planejamento.id}`"
      >
        <span class="text-subtitle-1 font-weight-medium text-truncate">
          {{ planejamento.titulo }}
        </span>
        <span
          class="text-caption text-medium-emphasis"
          :data-cy="`planejamento-data-${planejamento.id}`"
        >
          {{ data }}
        </span>
        <span
          v-if="orcamento"
          class="text-caption text-medium-emphasis"
          :data-cy="`planejamento-orcamento-${planejamento.id}`"
        >
          Orçamento: {{ orcamento }}
        </span>
      </router-link>

      <v-btn
        v-if="podeRemover"
        icon="mdi-trash-can-outline"
        variant="text"
        color="error"
        density="comfortable"
        aria-label="Remover planejamento"
        :data-cy="`planejamento-remover-${planejamento.id}`"
        @click="emit('remover')"
      />
    </div>
  </v-card>
</template>

<style scoped>
.planejamento-item {
  width: 100%;
}

.planejamento-item__conteudo {
  flex: 1;
  min-width: 0;
  color: inherit;
  text-decoration: none;
}
</style>
