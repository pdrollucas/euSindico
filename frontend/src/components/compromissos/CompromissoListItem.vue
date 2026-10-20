<script setup lang="ts">
import { computed } from 'vue'
import type { Compromisso } from '@/types/compromisso.types'
import { formatarData, formatarHorario } from '@/utils/dataHora'

// Estado visual transitório controlado pela view: o item está saindo da lista (concluído ->
// riscado e some; reaberto -> só some). "normal" mostra o checkbox conforme `compromisso.concluido`.
export type EstadoItem = 'normal' | 'concluindo' | 'reabrindo'

const props = defineProps<{
  compromisso: Compromisso
  estado?: EstadoItem
  mostrarResponsavel?: boolean
  // Repassado nos links para o "voltar" do detalhe saber que a lista veio filtrada por prédio.
  predioIdFiltro?: number | null
}>()
const emit = defineEmits<{ alternar: [concluido: boolean]; remover: [] }>()

const marcado = computed(() => {
  if (props.estado === 'concluindo') return true
  if (props.estado === 'reabrindo') return false
  return props.compromisso.concluido
})

const destino = computed(() => ({
  path: `/compromissos/${props.compromisso.id}`,
  query: props.predioIdFiltro ? { predioId: String(props.predioIdFiltro) } : undefined,
}))

const dataHorario = computed(
  () =>
    `${formatarData(props.compromisso.dataCompromisso)} · ${formatarHorario(props.compromisso.horarioCompromisso)}`,
)

// Evita reentrada enquanto a animação de saída roda.
const ocupado = computed(() => props.estado === 'concluindo' || props.estado === 'reabrindo')

function onAlternar() {
  if (!ocupado.value) {
    emit('alternar', !marcado.value)
  }
}
</script>

<template>
  <v-card
    variant="flat"
    border
    rounded="lg"
    class="compromisso-item"
    :class="{
      'compromisso-item--concluindo': estado === 'concluindo',
      'compromisso-item--reabrindo': estado === 'reabrindo',
    }"
    :data-cy="`compromisso-item-${compromisso.id}`"
  >
    <div class="d-flex align-center pa-3 ga-1">
      <router-link
        :to="destino"
        class="compromisso-item__conteudo d-flex flex-column"
        :data-cy="`compromisso-link-${compromisso.id}`"
      >
        <span class="compromisso-item__titulo text-subtitle-1 font-weight-medium">
          <span class="compromisso-item__titulo-texto">{{ compromisso.titulo }}</span>
        </span>
        <!-- Prédio e data/horário em linhas separadas: um nome de prédio longo é cortado sozinho e
             não leva consigo a data e o horário. -->
        <span
          class="text-caption text-medium-emphasis text-truncate"
          :data-cy="`compromisso-predio-${compromisso.id}`"
        >
          {{ compromisso.predioNome }}
        </span>
        <span
          class="text-caption text-medium-emphasis"
          :data-cy="`compromisso-data-${compromisso.id}`"
        >
          {{ dataHorario }}
        </span>
        <span
          v-if="mostrarResponsavel"
          class="text-caption text-medium-emphasis text-truncate"
          :data-cy="`compromisso-responsavel-${compromisso.id}`"
        >
          Responsável: {{ compromisso.responsavelNome }}
        </span>
      </router-link>

      <v-checkbox-btn
        :model-value="marcado"
        color="primary"
        density="comfortable"
        class="compromisso-item__check"
        :aria-label="marcado ? 'Reabrir compromisso' : 'Marcar como concluído'"
        :data-cy="`compromisso-check-${compromisso.id}`"
        @update:model-value="onAlternar"
      />
      <v-btn
        icon="mdi-trash-can-outline"
        variant="text"
        color="error"
        density="comfortable"
        aria-label="Remover compromisso"
        :disabled="ocupado"
        :data-cy="`compromisso-remover-${compromisso.id}`"
        @click="emit('remover')"
      />
    </div>
  </v-card>
</template>

<style scoped>
.compromisso-item {
  width: 100%;
}

.compromisso-item__conteudo {
  flex: 1;
  min-width: 0;
  color: inherit;
  text-decoration: none;
}

/* O v-checkbox-btn vem com `flex: 1 0` e dividiria a linha com o título, ficando no meio do card;
   `flex: none` encosta o checkbox na lixeira. */
.compromisso-item__check {
  flex: none;
}

.compromisso-item__titulo {
  align-self: flex-start;
  max-width: 100%;
}

/* O riscado é uma linha que cresce da esquerda para a direita (não `text-decoration`, que não
   anima) — acompanha só a largura do texto, por isso o span interno. */
.compromisso-item__titulo-texto {
  position: relative;
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.compromisso-item__titulo-texto::after {
  content: '';
  position: absolute;
  left: 0;
  top: 50%;
  width: 0;
  height: 2px;
  background: currentColor;
}

.compromisso-item--concluindo .compromisso-item__titulo-texto::after {
  animation: compromisso-riscar 350ms ease-out forwards;
}

.compromisso-item--concluindo {
  animation: compromisso-sumir 300ms ease-in 450ms forwards;
}

.compromisso-item--reabrindo {
  animation: compromisso-sumir 300ms ease-in forwards;
}

@keyframes compromisso-riscar {
  to {
    width: 100%;
  }
}

@keyframes compromisso-sumir {
  to {
    opacity: 0;
  }
}

@media (prefers-reduced-motion: reduce) {
  .compromisso-item--concluindo .compromisso-item__titulo-texto::after {
    animation: none;
    width: 100%;
  }

  .compromisso-item--concluindo,
  .compromisso-item--reabrindo {
    animation: none;
  }
}
</style>
