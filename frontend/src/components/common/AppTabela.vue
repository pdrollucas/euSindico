<script setup lang="ts" generic="T extends Record<string, unknown>">
// Desktop: estrutura comum das listas em tabela (ver DESIGN.md, "Layout responsivo"). A tela
// preenche as células pelo slot `linha`; a linha emite `abrir`, e ações dentro dela usam @click.stop.
const props = withDefaults(
  defineProps<{
    itens: T[]
    // `oculto`: título só para leitor de tela (coluna de ações).
    colunas: { titulo: string; oculto?: boolean }[]
    // Prefixo do `data-cy` da linha — o mesmo do card do celular (ex: "planejamento-item").
    dataCy: string
    chave?: keyof T
    clicavel?: boolean
  }>(),
  { chave: 'id', clicavel: true },
)
const emit = defineEmits<{ abrir: [item: T] }>()

function idDe(item: T) {
  return String(item[props.chave])
}
</script>

<template>
  <v-card variant="flat" border rounded="lg">
    <v-table hover density="comfortable">
      <thead>
        <tr>
          <th v-for="coluna in colunas" :key="coluna.titulo" scope="col">
            <span :class="{ 'd-sr-only': coluna.oculto }">{{ coluna.titulo }}</span>
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="item in itens"
          :key="idDe(item)"
          :class="{ 'app-tabela-linha': clicavel }"
          :data-cy="`${dataCy}-${idDe(item)}`"
          @click="clicavel && emit('abrir', item)"
        >
          <slot name="linha" :item="item" />
        </tr>
      </tbody>
    </v-table>
  </v-card>
</template>

<style scoped>
.app-tabela-linha {
  cursor: pointer;
}

/* Classes para as células do slot. */
:slotted(.app-tabela-acoes) {
  width: 1%;
  white-space: nowrap;
  text-align: right;
}

:slotted(.app-tabela-titulo) {
  color: inherit;
  text-decoration: none;
  font-weight: 500;
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  text-align: left;
}

:slotted(.app-tabela-titulo:hover),
:slotted(.app-tabela-titulo:focus-visible) {
  text-decoration: underline;
}
</style>
