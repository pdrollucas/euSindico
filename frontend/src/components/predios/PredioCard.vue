<script setup lang="ts">
import type { Predio } from '@/types/predio.types'

defineProps<{ predio: Predio }>()
</script>

<template>
  <v-card
    :to="`/predios/${predio.id}`"
    :data-cy="`predio-card-${predio.id}`"
    variant="flat"
    border
    rounded="lg"
    class="predio-card mb-3"
  >
    <v-card-text class="d-flex flex-column">
      <span class="text-subtitle-1 font-weight-medium">{{ predio.nome }}</span>
      <span class="text-body-2 text-medium-emphasis text-truncate">{{ predio.endereco }}</span>
    </v-card-text>
  </v-card>
</template>

<style scoped>
.predio-card {
  width: 100%;
}
</style>
