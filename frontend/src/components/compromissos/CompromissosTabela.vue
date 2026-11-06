<script setup lang="ts">
import { useRouter } from 'vue-router'
import type { Compromisso } from '@/types/compromisso.types'
import type { EstadoItem } from '@/components/compromissos/CompromissoListItem.vue'
import { formatarData, formatarHorario } from '@/utils/dataHora'
import { rotaDetalheCompromisso } from '@/utils/rotasCompromisso'

// Desktop: a lista de compromissos em tabela. Mesmos eventos e `data-cy` do card do celular
// (CompromissoListItem).
const props = defineProps<{
  compromissos: Compromisso[]
  estados: Record<number, EstadoItem>
  mostrarResponsavel?: boolean
  predioIdFiltro?: number | null
}>()
const emit = defineEmits<{
  alternar: [compromisso: Compromisso, concluido: boolean]
  remover: [compromisso: Compromisso]
}>()

const router = useRouter()

function destino(compromisso: Compromisso) {
  return rotaDetalheCompromisso(compromisso.id, props.predioIdFiltro)
}

function estado(compromisso: Compromisso): EstadoItem {
  return props.estados[compromisso.id] ?? 'normal'
}

function marcado(compromisso: Compromisso) {
  const atual = estado(compromisso)
  if (atual === 'concluindo') return true
  if (atual === 'reabrindo') return false
  return compromisso.concluido
}

// Evita reentrada enquanto a linha está saindo.
function ocupado(compromisso: Compromisso) {
  return estado(compromisso) !== 'normal'
}

function onAlternar(compromisso: Compromisso) {
  if (!ocupado(compromisso)) {
    emit('alternar', compromisso, !marcado(compromisso))
  }
}
</script>

<template>
  <v-card variant="flat" border rounded="lg" data-cy="compromissos-tabela">
    <v-table hover density="comfortable">
      <thead>
        <tr>
          <th class="coluna-check"><span class="d-sr-only">Concluído</span></th>
          <th>Título</th>
          <th>Prédio</th>
          <th class="coluna-data">Data</th>
          <th v-if="mostrarResponsavel">Responsável</th>
          <th class="coluna-acoes"><span class="d-sr-only">Ações</span></th>
        </tr>
      </thead>
      <tbody>
        <!-- A linha abre o detalhe no clique; o título é o link real, para o teclado. -->
        <tr
          v-for="compromisso in compromissos"
          :key="compromisso.id"
          class="linha"
          :class="{
            'linha--concluindo': estado(compromisso) === 'concluindo',
            'linha--reabrindo': estado(compromisso) === 'reabrindo',
          }"
          :data-cy="`compromisso-item-${compromisso.id}`"
          @click="router.push(destino(compromisso))"
        >
          <td class="coluna-check" @click.stop>
            <v-checkbox-btn
              :model-value="marcado(compromisso)"
              color="primary"
              density="comfortable"
              :aria-label="marcado(compromisso) ? 'Reabrir compromisso' : 'Marcar como concluído'"
              :data-cy="`compromisso-check-${compromisso.id}`"
              @update:model-value="onAlternar(compromisso)"
            />
          </td>
          <td>
            <router-link
              :to="destino(compromisso)"
              class="linha-titulo font-weight-medium"
              :data-cy="`compromisso-link-${compromisso.id}`"
              @click.stop
            >
              {{ compromisso.titulo }}
            </router-link>
          </td>
          <td class="text-medium-emphasis" :data-cy="`compromisso-predio-${compromisso.id}`">
            {{ compromisso.predioNome }}
          </td>
          <td
            class="coluna-data text-medium-emphasis"
            :data-cy="`compromisso-data-${compromisso.id}`"
          >
            {{ formatarData(compromisso.dataCompromisso) }} ·
            {{ formatarHorario(compromisso.horarioCompromisso) }}
          </td>
          <td
            v-if="mostrarResponsavel"
            class="text-medium-emphasis"
            :data-cy="`compromisso-responsavel-${compromisso.id}`"
          >
            {{ compromisso.responsavelNome }}
          </td>
          <td class="coluna-acoes" @click.stop>
            <v-btn
              icon="mdi-trash-can-outline"
              variant="text"
              color="error"
              density="comfortable"
              aria-label="Remover compromisso"
              :disabled="ocupado(compromisso)"
              :data-cy="`compromisso-remover-${compromisso.id}`"
              @click="emit('remover', compromisso)"
            />
          </td>
        </tr>
      </tbody>
    </v-table>
  </v-card>
</template>

<style scoped>
.linha {
  cursor: pointer;
}

.coluna-check,
.coluna-acoes {
  width: 56px;
}

.coluna-acoes {
  text-align: right;
}

.coluna-data {
  white-space: nowrap;
}

.linha-titulo {
  color: inherit;
  text-decoration: none;
}

.linha-titulo:hover,
.linha-titulo:focus-visible {
  text-decoration: underline;
}

/* Saída ao concluir/reabrir: 450ms + 300ms = SAIDA_CONCLUIR_MS (utils/animacao.ts). */
.linha--concluindo .linha-titulo {
  text-decoration: line-through;
}

.linha--concluindo {
  animation: linha-sumir 300ms ease-in 450ms forwards;
}

.linha--reabrindo {
  animation: linha-sumir 300ms ease-in forwards;
}

@keyframes linha-sumir {
  to {
    opacity: 0;
  }
}

@media (prefers-reduced-motion: reduce) {
  .linha--concluindo,
  .linha--reabrindo {
    animation: none;
  }
}
</style>
