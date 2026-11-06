<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'

// Cabeçalho compartilhado das telas filhas da área logada — ver ARCHITECTURE.md, seção 5.
// Slot `acoes`: ação principal da tela, só no desktop (no celular ela é o FAB da view).
const props = defineProps<{ titulo?: string }>()

const route = useRoute()
const router = useRouter()
const { mdAndUp } = useDisplay()

const titulo = computed(() => props.titulo ?? route.meta.titulo ?? '')
const destinoVoltar = computed(() => route.meta.voltarPara?.(route) ?? '/home')

// No desktop não há Home: nas telas de topo o "voltar" não levaria a lugar nenhum.
const mostrarVoltar = computed(() => !(mdAndUp.value && destinoVoltar.value === '/home'))

function voltar() {
  router.push(destinoVoltar.value)
}
</script>

<template>
  <header class="app-header d-flex align-center justify-space-between mx-n4 mt-n4 px-2 py-1 mb-2 bg-surface">
    <template v-if="mdAndUp">
      <v-btn
        v-if="mostrarVoltar"
        icon="mdi-arrow-left"
        variant="text"
        aria-label="Voltar"
        data-cy="btn-voltar"
        @click="voltar"
      />
      <h1
        class="app-header-titulo-desktop text-title-large font-weight-bold text-truncate"
        :class="mostrarVoltar ? 'ml-1' : 'ml-2'"
      >
        {{ titulo }}
      </h1>
      <div class="d-flex align-center ga-2 ml-auto mr-2">
        <slot name="acoes" />
      </div>
    </template>

    <template v-else>
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
    </template>
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

/* Desktop: fundo e linha inferior de borda a borda, com o conteúdo alinhado à coluna. Sombras com
   spread grande + clip-path em vez de width: 100dvw, que somaria rail e barra de rolagem e criaria
   rolagem horizontal. */
@media (min-width: 840px) {
  .app-header {
    border-bottom: none;
    box-shadow:
      0 0 0 100vmax rgb(var(--v-theme-surface)),
      0 1px 0 100vmax rgba(27, 37, 54, 0.08);
    clip-path: inset(0 -100vmax -1px);
  }
}

.app-header-titulo-desktop {
  min-width: 0;
}

.app-header-titulo {
  flex: 1;
  text-align: center;
  padding-inline: 8px;
}
</style>
