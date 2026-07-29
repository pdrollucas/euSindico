<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { usePerfilStore } from '@/stores/perfilStore'
import logoIconUrl from '@/assets/logo-eusindico-icon.svg'

const router = useRouter()
const authStore = useAuthStore()
const perfilStore = usePerfilStore()

// Hub central da área logada (ver ARCHITECTURE.md, seção 5): cards para os módulos.
const menu = [
  { titulo: 'Compromissos', icone: 'mdi-calendar-check-outline', destino: '/compromissos', dataCy: 'card-compromissos' },
  { titulo: 'Prédios', icone: 'mdi-office-building-outline', destino: '/predios', dataCy: 'card-predios' },
  { titulo: 'Configurações', icone: 'mdi-cog-outline', destino: '/configuracoes', dataCy: 'card-configuracoes' },
]

// Nome do usuário para a saudação — uma falha no carregamento não quebra a Home (só não mostra o
// nome); um 401 é tratado pelo interceptor do Axios (renova a sessão ou manda para o login).
onMounted(async () => {
  try {
    await perfilStore.carregar()
  } catch {
    // Silencioso de propósito: a Home é utilizável sem o nome.
  }
})

async function sair() {
  await authStore.logout()
  perfilStore.limpar()
  router.push('/login')
}
</script>

<template>
  <v-container class="home d-flex flex-column fill-height">
    <!-- Cabeçalho: marca à esquerda, nome do usuário à direita -->
    <header class="d-flex align-center justify-space-between py-2">
      <img :src="logoIconUrl" alt="euSíndico" class="home-logo" />
      <span v-if="perfilStore.primeiroNome" class="text-subtitle-1" data-cy="home-usuario">
        {{ perfilStore.primeiroNome }}
      </span>
    </header>

    <!-- Hub de navegação -->
    <nav class="home-menu d-flex flex-column align-center ga-4 my-auto">
      <v-card
        v-for="item in menu"
        :key="item.destino"
        :to="item.destino"
        :data-cy="item.dataCy"
        class="home-card"
        variant="flat"
        rounded="lg"
      >
        <div class="home-card-conteudo d-flex flex-column align-center justify-center text-center pa-4">
          <v-icon :icon="item.icone" size="40" color="primary" class="mb-2" />
          <span class="text-subtitle-1 font-weight-medium home-card-titulo">{{ item.titulo }}</span>
        </div>
      </v-card>
    </nav>

    <!-- Logout como FAB (padrão de ação flutuante do projeto — ver ARCHITECTURE.md, seção 5) -->
    <v-btn
      icon="mdi-logout"
      color="error"
      size="large"
      class="home-fab"
      aria-label="Sair"
      data-cy="btn-logout"
      @click="sair"
    />
  </v-container>
</template>

<style scoped>
.home {
  max-width: 480px;
  margin-inline: auto;
}

.home-logo {
  height: 30px;
  width: auto;
  display: block;
}

.home-menu {
  width: 100%;
}

/* Cards quadrados e centralizados, com sombra leve azulada (sem contorno).
   O seletor scoped tem prioridade sobre o `box-shadow: none` do variant="flat" do Vuetify. */
.home-card {
  width: 160px;
  aspect-ratio: 1 / 1;
  box-shadow: 0 2px 12px rgba(30, 79, 168, 0.1);
  transition: box-shadow 0.15s ease;
}

.home-card-conteudo {
  height: 100%;
}

.home-card-titulo {
  color: rgb(var(--v-theme-primary));
}

/* FAB fixo no canto inferior direito, sobre o conteúdo. */
.home-fab {
  position: fixed;
  right: 24px;
  bottom: 24px;
}
</style>
