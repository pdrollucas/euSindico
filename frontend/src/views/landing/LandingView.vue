<script setup lang="ts"></script>

<template>
  <v-container class="fill-height" fluid>
    <v-row justify="center" align="center" class="fill-height text-center">
      <v-col cols="12" sm="8" md="6">
        <h1 class="text-h4 font-weight-bold mb-4">euSíndico</h1>
        <p class="text-body-1 mb-8">
          Organize compromissos, prédios e documentos do seu dia a dia como síndico, tudo em um só
          lugar.
        </p>
        <div class="d-flex flex-column ga-3">
          <v-btn color="primary" size="large" to="/registrar" data-cy="btn-registrar">
            Criar conta
          </v-btn>
          <v-btn variant="outlined" size="large" to="/login" data-cy="btn-login">
            Já tenho conta
          </v-btn>
        </div>
      </v-col>
    </v-row>
  </v-container>
</template>
