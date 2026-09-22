<script setup lang="ts">
// Diálogo de confirmação genérico (ex: RN07 — confirmar antes de excluir um prédio, é
// responsabilidade do frontend). Componente "burro": não conhece store/service, só emite
// "confirmar" — quem chama decide o que fazer (ver ARCHITECTURE.md, seção 2).
defineProps<{
  modelValue: boolean
  titulo: string
  mensagem: string
  textoConfirmar?: string
  corConfirmar?: string
  carregando?: boolean
}>()

const emit = defineEmits<{ 'update:modelValue': [value: boolean]; confirmar: [] }>()
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="400"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <v-card rounded="lg">
      <v-card-item>
        <v-card-title class="text-h6 font-weight-bold">{{ titulo }}</v-card-title>
      </v-card-item>
      <v-card-text style="white-space: normal">{{ mensagem }}</v-card-text>
      <v-card-actions class="pa-4 pt-0">
        <v-spacer />
        <v-btn variant="outlined" data-cy="dialog-cancelar" @click="emit('update:modelValue', false)">
          Cancelar
        </v-btn>
        <v-btn
          :color="corConfirmar ?? 'primary'"
          variant="flat"
          :loading="carregando"
          data-cy="dialog-confirmar"
          @click="emit('confirmar')"
        >
          {{ textoConfirmar ?? 'Confirmar' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
