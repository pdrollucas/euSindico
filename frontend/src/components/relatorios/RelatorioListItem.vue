<script setup lang="ts">
import { computed } from 'vue'
import type { Relatorio } from '@/types/relatorio.types'
import { formatarDataHoraUtc } from '@/utils/dataHora'
import { formatarPeriodo } from '@/utils/periodo'

// Clicar no card abre o PDF numa nova aba; o ícone baixa. A lixeira só aparece para quem pode
// excluir (Síndico, RN36). A lista é de um prédio só, então o item não repete o nome do prédio.
const props = defineProps<{
  relatorio: Relatorio
  podeRemover?: boolean
  // Enquanto o PDF está sendo buscado (abrir ou baixar), o item não aceita um segundo clique.
  ocupado?: boolean
}>()
const emit = defineEmits<{ abrir: []; baixar: []; remover: [] }>()

const periodo = computed(() =>
  formatarPeriodo(props.relatorio.mesReferencia, props.relatorio.anoReferencia),
)
const geradoEm = computed(() => formatarDataHoraUtc(props.relatorio.geradoEm))
</script>

<template>
  <v-card
    variant="flat"
    border
    rounded="lg"
    class="relatorio-item"
    :data-cy="`relatorio-item-${relatorio.id}`"
  >
    <div class="d-flex align-center pa-3 ga-1">
      <button
        type="button"
        class="relatorio-item__conteudo d-flex flex-column"
        :disabled="ocupado"
        :aria-label="`Abrir relatório de ${periodo} em uma nova aba`"
        :data-cy="`relatorio-abrir-${relatorio.id}`"
        @click="emit('abrir')"
      >
        <span
          class="text-subtitle-1 font-weight-medium text-truncate"
          :data-cy="`relatorio-periodo-${relatorio.id}`"
        >
          {{ periodo }}
        </span>
        <span
          class="text-caption text-medium-emphasis"
          :data-cy="`relatorio-gerado-em-${relatorio.id}`"
        >
          Gerado em {{ geradoEm }}
        </span>
      </button>

      <v-btn
        icon="mdi-download-outline"
        variant="text"
        color="primary"
        density="comfortable"
        :loading="ocupado"
        :disabled="ocupado"
        aria-label="Baixar relatório"
        :data-cy="`relatorio-baixar-${relatorio.id}`"
        @click="emit('baixar')"
      />

      <v-btn
        v-if="podeRemover"
        icon="mdi-trash-can-outline"
        variant="text"
        color="error"
        density="comfortable"
        :disabled="ocupado"
        aria-label="Remover relatório"
        :data-cy="`relatorio-remover-${relatorio.id}`"
        @click="emit('remover')"
      />
    </div>
  </v-card>
</template>

<style scoped>
.relatorio-item {
  width: 100%;
}

/* Botão nativo (acessível por teclado) com cara de texto: ocupa o espaço do card e herda a cor. */
.relatorio-item__conteudo {
  flex: 1;
  min-width: 0;
  padding: 0;
  border: 0;
  background: none;
  color: inherit;
  font: inherit;
  text-align: left;
  cursor: pointer;
}

.relatorio-item__conteudo:disabled {
  cursor: default;
  opacity: 0.6;
}
</style>
