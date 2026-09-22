<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

// Cabeçalho compartilhado das telas filhas da área logada — ver ARCHITECTURE.md, seção 5.
const props = defineProps<{ titulo?: string }>()

const route = useRoute()
const router = useRouter()

const titulo = computed(() => props.titulo ?? route.meta.titulo ?? '')

function voltar() {
  const destino = route.meta.voltarPara?.(route) ?? '/home'
  router.push(destino)
}
</script>

<template>
  <header class="app-header d-flex align-center justify-space-between mx-n4 mt-n4 px-2 py-1 mb-2 bg-surface">
    <v-btn
      icon="mdi-arrow-left"
      variant="text"
      aria-label="Voltar"
      data-cy="btn-voltar"
      @click="voltar"
    />
    <span class="text-subtitle-1 font-weight-medium text-truncate app-header-titulo">
      {{ titulo }}
    </span>
    <v-btn
      icon="mdi-home-outline"
      variant="text"
      color="primary"
      to="/home"
      aria-label="Ir para a Home"
      data-cy="btn-home"
    />
  </header>
</template>

<style scoped>
/* mx-n4/mt-n4 sangram até a borda do v-container. Fixo no topo ao rolar (mesma box que sangra e
   gruda — não separar isso em pai/filho, ver PrediosListView.vue). min-height casa com
   --app-header-height (src/assets/main.css). */
.app-header {
  position: sticky;
  top: 0;
  z-index: 2;
  min-height: var(--app-header-height);
  border-bottom: 1px solid rgba(27, 37, 54, 0.08);
}

.app-header-titulo {
  flex: 1;
  text-align: center;
  padding-inline: 8px;
}
</style>
