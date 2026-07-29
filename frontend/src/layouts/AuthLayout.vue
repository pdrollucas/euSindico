<script setup lang="ts">
import logoIconUrl from '@/assets/logo-eusindico-icon.svg'
</script>

<template>
  <v-app>
    <v-main>
      <v-container class="fill-height" fluid>
        <v-row justify="center" align="center" class="fill-height">
          <v-col cols="12" sm="8" md="5" lg="4">
            <!-- Identidade da marca compartilhada por todas as telas de auth (login, cadastro,
                 recuperação de senha) — clicável, volta para a landing pública. -->
            <router-link
              to="/"
              class="auth-brand d-flex align-center justify-center ga-2 mb-6"
              data-cy="link-landing"
            >
              <img :src="logoIconUrl" alt="euSíndico" class="auth-logo" />
              <span class="text-h6 font-weight-bold">euSíndico</span>
            </router-link>

            <router-view />
          </v-col>
        </v-row>
      </v-container>
    </v-main>
  </v-app>
</template>

<style scoped>
.auth-brand {
  text-decoration: none;
  color: rgb(var(--v-theme-primary));
}

.auth-logo {
  height: 26px;
  width: auto;
  display: block;
}
</style>
