<script setup lang="ts">
import { useRoute } from 'vue-router'
import { useSair } from '@/composables/useSair'
import logoIconUrl from '@/assets/logo-eusindico-icon.svg'

// Navigation rail do desktop, no lugar do hub da Home (ver DESIGN.md, "Layout responsivo").
const route = useRoute()
const { sair } = useSair()

const itens = [
  { titulo: 'Compromissos', icone: 'mdi-calendar-check-outline', destino: '/compromissos' },
  { titulo: 'Prédios', icone: 'mdi-office-building-outline', destino: '/predios' },
  { titulo: 'Configurações', icone: 'mdi-cog-outline', destino: '/configuracoes' },
]

// Rotas planas: o ativo do router-link não pega subtelas como `/predios/5/atas`.
function ativo(destino: string) {
  return route.path === destino || route.path.startsWith(`${destino}/`)
}
</script>

<template>
  <v-navigation-drawer permanent width="88" data-cy="app-rail">
    <div class="app-rail py-4">
      <div class="app-rail-marca" data-cy="rail-marca">
        <img :src="logoIconUrl" alt="" class="app-rail-logo" />
        <span class="app-rail-marca-texto">euSíndico</span>
      </div>

      <nav class="app-rail-itens" aria-label="Navegação principal">
        <router-link
          v-for="item in itens"
          :key="item.destino"
          :to="item.destino"
          class="app-rail-item"
          :class="{ 'app-rail-item--ativo': ativo(item.destino) }"
          :aria-current="ativo(item.destino) ? 'page' : undefined"
          :data-cy="`rail-${item.destino.slice(1)}`"
        >
          <span class="app-rail-indicador">
            <v-icon :icon="item.icone" size="24" />
          </span>
          <span class="app-rail-rotulo">{{ item.titulo }}</span>
        </router-link>
      </nav>

      <button
        type="button"
        class="app-rail-item app-rail-item--sair"
        data-cy="rail-sair"
        @click="sair"
      >
        <span class="app-rail-indicador">
          <v-icon icon="mdi-logout" size="24" />
        </span>
        <span class="app-rail-rotulo">Sair</span>
      </button>
    </div>
  </v-navigation-drawer>
</template>

<style scoped>
/* Marca no topo, itens no meio exato da altura e Sair no rodapé. */
.app-rail {
  display: grid;
  grid-template-rows: 1fr auto 1fr;
  justify-items: center;
  height: 100%;
}

.app-rail-marca {
  align-self: start;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
}

.app-rail-logo {
  height: 26px;
  width: auto;
  display: block;
}

.app-rail-marca-texto {
  font-size: 0.8125rem;
  font-weight: 700;
  color: rgb(var(--v-theme-primary));
}

.app-rail-itens {
  display: flex;
  flex-direction: column;
  gap: 28px;
  width: 100%;
}

.app-rail-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  width: 100%;
  padding: 0;
  border: none;
  background: none;
  cursor: pointer;
  text-decoration: none;
  color: rgba(var(--v-theme-on-surface), var(--v-medium-emphasis-opacity));
}

.app-rail-indicador {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 56px;
  height: 32px;
  border-radius: 16px;
  transition: background-color 0.15s ease;
}

.app-rail-rotulo {
  font-size: 0.75rem;
  line-height: 1rem;
  font-weight: 500;
}

.app-rail-item:hover .app-rail-indicador {
  background: rgba(var(--v-theme-on-surface), 0.06);
}

/* Foco de teclado visível na pílula, não no item inteiro. */
.app-rail-item:focus-visible {
  outline: none;
}

.app-rail-item:focus-visible .app-rail-indicador {
  outline: 2px solid rgb(var(--v-theme-primary));
}

/* Ativo: pílula em secondary de baixa opacidade, o tom de marca do DESIGN.md (seção 2). */
.app-rail-item--ativo {
  color: rgb(var(--v-theme-primary));
}

.app-rail-item--ativo .app-rail-indicador,
.app-rail-item--ativo:hover .app-rail-indicador {
  background: rgba(var(--v-theme-secondary), 0.16);
}

.app-rail-item--sair {
  align-self: end;
  color: rgb(var(--v-theme-error));
}
</style>
