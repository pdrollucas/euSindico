<script setup lang="ts">
import AppHeader from '@/components/common/AppHeader.vue'

// Hub de Configurações: cada seção é uma rota própria (ver router/index.ts), todas com o botão
// "voltar" do AppHeader apontando para cá. Mesmo card plano das listas (DESIGN.md, seção 4).
const secoes = [
  {
    titulo: 'Perfil',
    descricao: 'Altere seu nome e seu e-mail.',
    icone: 'mdi-account-outline',
    destino: '/configuracoes/perfil',
    dataCy: 'card-config-perfil',
  },
  {
    titulo: 'Segurança',
    descricao: 'Altere sua senha.',
    icone: 'mdi-lock-outline',
    destino: '/configuracoes/seguranca',
    dataCy: 'card-config-seguranca',
  },
  {
    titulo: 'Suporte',
    descricao: 'Fale com a nossa equipe.',
    icone: 'mdi-lifebuoy',
    destino: '/configuracoes/suporte',
    dataCy: 'card-config-suporte',
  },
  {
    titulo: 'Conta',
    descricao: 'Exclua sua conta e seus dados.',
    icone: 'mdi-account-cog-outline',
    destino: '/configuracoes/conta',
    dataCy: 'card-config-conta',
  },
]
</script>

<template>
  <v-container class="configuracoes d-flex flex-column fill-height">
    <AppHeader />

    <nav class="d-flex flex-column ga-3 mt-2">
      <v-card
        v-for="secao in secoes"
        :key="secao.destino"
        :to="secao.destino"
        :data-cy="secao.dataCy"
        variant="flat"
        border
        rounded="lg"
      >
        <div class="d-flex align-center pa-4 ga-4">
          <v-icon :icon="secao.icone" size="28" color="primary" />
          <div class="flex-grow-1">
            <div class="text-subtitle-1 font-weight-bold">{{ secao.titulo }}</div>
            <div class="text-body-2 text-medium-emphasis">{{ secao.descricao }}</div>
          </div>
          <v-icon icon="mdi-chevron-right" color="medium-emphasis" />
        </div>
      </v-card>
    </nav>
  </v-container>
</template>

<style scoped>
.configuracoes {
  max-width: 480px;
  margin-inline: auto;
}
</style>
