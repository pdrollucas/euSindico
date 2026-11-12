<script setup lang="ts">
import { watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDisplay } from 'vuetify'
import AppNavRail from '@/components/common/AppNavRail.vue'

const { mdAndUp } = useDisplay()
const route = useRoute()
const router = useRouter()

// A guarda do router tira a Home do desktop ao navegar; isto cobre a janela que vira desktop com a
// Home aberta (redimensionada, tablet girado).
watch([mdAndUp, () => route.name], ([desktop, nome]) => {
  if (desktop && nome === 'home') {
    void router.replace('/compromissos')
  }
})
</script>

<template>
  <v-app>
    <AppNavRail v-if="mdAndUp" />
    <v-main>
      <router-view />
    </v-main>
  </v-app>
</template>
