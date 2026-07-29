<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

// Placeholder das telas ainda não implementadas (Compromissos, Prédios, Configurações). O título
// vem de route.meta.titulo (definido no router). Substituído pela tela real no marco correspondente.
const route = useRoute()
const titulo = computed(() => route.meta.titulo ?? 'Em construção')
</script>

<template>
  <v-container class="d-flex flex-column fill-height text-center">
    <div class="my-auto">
      <v-icon icon="mdi-progress-wrench" size="64" color="primary" class="mb-4" />
      <h1 class="text-h5 font-weight-bold mb-2" data-cy="em-construcao-titulo">{{ titulo }}</h1>
      <p class="text-body-2 text-medium-emphasis mb-6">Esta tela está em construção.</p>
      <v-btn variant="text" color="primary" to="/home" data-cy="link-home">Voltar para a Home</v-btn>
    </div>
  </v-container>
</template>
