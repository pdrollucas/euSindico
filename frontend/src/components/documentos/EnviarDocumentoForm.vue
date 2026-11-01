<script setup lang="ts">
import { computed } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { enviarDocumentoFormSchema } from '@/schemas/documento.schema'
import { ACCEPT_ARQUIVO } from '@/utils/arquivoDocumento'

// Componente "burro": só escolhe o arquivo, valida o formato/tamanho e emite — quem envia é a view
// (ARCHITECTURE.md, seção 3). A validação aqui é UX; o backend confere o conteúdo de verdade.
defineProps<{ enviando?: boolean; rotuloBotao?: string }>()
const emit = defineEmits<{ submit: [arquivo: File] }>()

const { defineField, handleSubmit, errors } = useForm({
  validationSchema: toTypedSchema(enviarDocumentoFormSchema),
})

const [arquivo, arquivoAttrs] = defineField('arquivo')
// O vee-validate tipa o campo como `DeepPartial<File>`; o v-file-input quer o `File` inteiro.
const arquivoSelecionado = computed(() => arquivo.value as File | undefined)

// O v-file-input entrega o `File` direto (ou uma lista, conforme a versão/`multiple`); o formulário
// guarda sempre um só.
function aoEscolher(valor: File | File[] | null | undefined) {
  arquivo.value = (Array.isArray(valor) ? valor[0] : valor) ?? undefined
}

const onSubmit = handleSubmit((values) => {
  emit('submit', values.arquivo)
})
</script>

<template>
  <v-form @submit.prevent="onSubmit">
    <v-file-input
      v-bind="arquivoAttrs"
      :model-value="arquivoSelecionado"
      data-cy="input-arquivo"
      label="Arquivo"
      :accept="ACCEPT_ARQUIVO"
      prepend-icon=""
      prepend-inner-icon="mdi-paperclip"
      show-size
      clearable
      :error-messages="errors.arquivo"
      @update:model-value="aoEscolher"
    />
    <v-btn
      type="submit"
      color="primary"
      size="large"
      block
      class="mt-2"
      :loading="enviando"
      data-cy="btn-enviar-documento"
    >
      {{ rotuloBotao ?? 'Enviar' }}
    </v-btn>
  </v-form>
</template>
