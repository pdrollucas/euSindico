<script setup lang="ts">
import { computed } from 'vue'
import type { Documento } from '@/types/documento.types'
import { abreNoNavegador, categoriaDe } from '@/utils/arquivoDocumento'
import { formatarDataHoraUtc } from '@/utils/dataHora'

// Clicar no card abre PDF/imagem numa nova aba (DOCX não abre no navegador, então o clique baixa); o
// ícone sempre baixa. A lixeira só aparece para quem pode excluir (Síndico). A lista é de um prédio
// só, então o item não repete o nome do prédio.
const props = defineProps<{
  documento: Documento
  podeRemover?: boolean
  // Enquanto o arquivo está sendo buscado (abrir ou baixar), o item não aceita um segundo clique.
  ocupado?: boolean
}>()
const emit = defineEmits<{ abrir: []; baixar: []; remover: [] }>()

const ICONES = {
  pdf: 'mdi-file-pdf-box',
  imagem: 'mdi-file-image-outline',
  docx: 'mdi-file-word-outline',
  outro: 'mdi-file-outline',
} as const

const icone = computed(() => ICONES[categoriaDe(props.documento.nomeArquivo)])
const enviadoEm = computed(() => formatarDataHoraUtc(props.documento.criadoEm))
const abreEmNovaAba = computed(() => abreNoNavegador(props.documento.nomeArquivo))
const acaoDoClique = computed(() => (abreEmNovaAba.value ? 'Abrir' : 'Baixar'))

function aoClicar() {
  if (abreEmNovaAba.value) {
    emit('abrir')
  } else {
    emit('baixar')
  }
}
</script>

<template>
  <v-card
    variant="flat"
    border
    rounded="lg"
    class="documento-item"
    :data-cy="`documento-item-${documento.id}`"
  >
    <div class="d-flex align-center pa-3 ga-1">
      <v-icon :icon="icone" color="primary" class="mr-2" />

      <button
        type="button"
        class="documento-item__conteudo d-flex flex-column"
        :disabled="ocupado"
        :aria-label="`${acaoDoClique} ${documento.nomeArquivo}`"
        :data-cy="`documento-abrir-${documento.id}`"
        @click="aoClicar"
      >
        <span
          class="text-subtitle-1 font-weight-medium text-truncate"
          :data-cy="`documento-nome-${documento.id}`"
        >
          {{ documento.nomeArquivo }}
        </span>
        <span
          class="text-caption text-medium-emphasis"
          :data-cy="`documento-enviado-em-${documento.id}`"
        >
          Enviado em {{ enviadoEm }}
        </span>
      </button>

      <v-btn
        icon="mdi-download-outline"
        variant="text"
        color="primary"
        density="comfortable"
        :loading="ocupado"
        :disabled="ocupado"
        aria-label="Baixar arquivo"
        :data-cy="`documento-baixar-${documento.id}`"
        @click="emit('baixar')"
      />

      <v-btn
        v-if="podeRemover"
        icon="mdi-trash-can-outline"
        variant="text"
        color="error"
        density="comfortable"
        :disabled="ocupado"
        aria-label="Remover arquivo"
        :data-cy="`documento-remover-${documento.id}`"
        @click="emit('remover')"
      />
    </div>
  </v-card>
</template>

<style scoped>
.documento-item {
  width: 100%;
}

/* Botão nativo (acessível por teclado) com cara de texto: ocupa o espaço do card e herda a cor. */
.documento-item__conteudo {
  flex: 1;
  min-width: 0;
  padding: 0;
  border: 0;
  background: none;
  color: inherit;
  font: inherit;
  text-align: left;
  cursor: pointer;
}

.documento-item__conteudo:disabled {
  cursor: default;
  opacity: 0.6;
}
</style>
