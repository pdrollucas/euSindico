<script setup lang="ts">
// Explica o que cada perfil pode fazer no prédio. Reflete a matriz de permissões do backend
// (AutorizacaoPredioService, RF33) — se ela mudar, atualize aqui.
defineProps<{ modelValue: boolean }>()
defineEmits<{ 'update:modelValue': [value: boolean] }>()

const perfis = [
  {
    chave: 'sindico',
    nome: 'Síndico',
    icone: 'mdi-shield-crown-outline',
    resumo: 'Dono do prédio. Controle total.',
    permissoes: [
      'Convidar e remover funcionários',
      'Editar os dados do prédio',
      'Criar compromissos e escolher o responsável',
      'Ver todos os compromissos do prédio',
      'Criar, editar e remover planejamentos',
      'Gerar relatórios e enviar atas e normas',
      'Excluir relatórios, atas e normas',
    ],
  },
  {
    chave: 'gestor',
    nome: 'Gestor',
    icone: 'mdi-account-tie-outline',
    resumo: 'Braço direito do síndico no dia a dia.',
    permissoes: [
      'Ver a equipe (sem convidar nem remover)',
      'Criar compromissos e escolher o responsável',
      'Ver todos os compromissos do prédio',
      'Criar, editar e remover planejamentos',
      'Gerar relatórios e enviar atas e normas',
      'Consultar e baixar relatórios, atas e normas',
    ],
    limites: ['Não excluir relatórios, atas e normas', 'Não editar o prédio'],
  },
  {
    chave: 'colaborador',
    nome: 'Colaborador',
    icone: 'mdi-account-outline',
    resumo: 'Executa tarefas e consulta o que precisa.',
    permissoes: [
      'Criar compromissos (ele sempre é o responsável)',
      'Ver, editar e remover só os próprios compromissos',
      'Consultar planejamentos',
      'Consultar e baixar relatórios, atas e normas',
    ],
    limites: [
      'Não ver a equipe',
      'Não alterar planejamentos',
      'Não gerar relatórios nem enviar atas e normas',
    ],
  },
]
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    max-width="440"
    scrollable
    @update:model-value="$emit('update:modelValue', $event)"
  >
    <v-card rounded="lg" data-cy="dialog-perfis">
      <v-card-item>
        <v-card-title class="text-h6 font-weight-bold">O que cada perfil pode fazer</v-card-title>
        <v-card-subtitle class="text-wrap">
          O perfil vale só para este prédio. Escolha o que combina com a função do funcionário.
        </v-card-subtitle>
      </v-card-item>

      <v-card-text>
        <v-card
          v-for="perfil in perfis"
          :key="perfil.chave"
          variant="flat"
          border
          rounded="lg"
          class="mb-3"
          :data-cy="`perfil-${perfil.chave}`"
        >
          <v-card-text>
            <div class="d-flex align-center mb-2">
              <v-icon :icon="perfil.icone" color="primary" class="mr-2" />
              <div class="d-flex flex-column">
                <span class="text-subtitle-1 font-weight-bold">{{ perfil.nome }}</span>
                <span class="text-caption text-medium-emphasis">{{ perfil.resumo }}</span>
              </div>
            </div>

            <ul class="perfil-lista text-body-2">
              <li v-for="item in perfil.permissoes" :key="item" class="d-flex">
                <v-icon icon="mdi-check" size="small" color="primary" class="mr-2 mt-1" />
                <span>{{ item }}</span>
              </li>
              <li
                v-for="item in perfil.limites"
                :key="item"
                class="d-flex text-medium-emphasis"
              >
                <v-icon icon="mdi-minus" size="small" class="mr-2 mt-1" />
                <span>{{ item }}</span>
              </li>
            </ul>
          </v-card-text>
        </v-card>
      </v-card-text>

      <v-card-actions>
        <v-btn
          color="primary"
          variant="outlined"
          block
          data-cy="btn-fechar-perfis"
          @click="$emit('update:modelValue', false)"
        >
          Entendi
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.perfil-lista {
  list-style: none;
  padding: 0;
  margin: 0;
}

.perfil-lista li + li {
  margin-top: 4px;
}
</style>
