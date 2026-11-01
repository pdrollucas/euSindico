<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { useRoute } from 'vue-router'
import AppHeader from '@/components/common/AppHeader.vue'
import ConfirmDialog from '@/components/common/ConfirmDialog.vue'
import DocumentoListItem from '@/components/documentos/DocumentoListItem.vue'
import EnviarDocumentoForm from '@/components/documentos/EnviarDocumentoForm.vue'
import { useDocumentoStore } from '@/stores/documentoStore'
import { usePredioStore } from '@/stores/predioStore'
import { PAPEL_PREDIO } from '@/types/predio.types'
import type { Documento } from '@/types/documento.types'
import { abrirAbaEmBranco, baixarArquivo, mostrarArquivoNaAba } from '@/utils/arquivo'
import { FORMATOS_ACEITOS_TEXTO } from '@/utils/arquivoDocumento'
import { mensagemErroEnvio } from '@/utils/documentoErro'
import { TIPOS_DOCUMENTO } from '@/utils/documentoTipo'

// Uma view para as duas telas: a rota (`meta.tipoDocumento`) diz se é a de atas ou a de normas, e os
// textos vêm de TIPOS_DOCUMENTO. Cada tela tem a sua rota e o seu título (ARCHITECTURE.md, seção 4).
const route = useRoute()
const documentoStore = useDocumentoStore()
const predioStore = usePredioStore()

const tipo = computed(() => route.meta.tipoDocumento ?? 'atas')
const config = computed(() => TIPOS_DOCUMENTO[tipo.value])
const lista = computed(() => documentoStore.listas[tipo.value])
const predioId = computed(() => Number(route.params.id))

// Enviar é de Síndico/Gestor e excluir só do Síndico (RN37); o Colaborador lê, abre e baixa.
// Enquanto o prédio não carrega o papel é desconhecido, então nada de escrita aparece.
const papel = computed(() => predioStore.predioAtual?.papel)
const podeEnviar = computed(
  () => papel.value !== undefined && papel.value !== PAPEL_PREDIO.COLABORADOR,
)
const podeExcluir = computed(() => papel.value === PAPEL_PREDIO.SINDICO)

const busca = ref<string | null>('')
const carregando = ref(false)
const erro = ref<string | null>(null)

const mostrarFormEnviar = ref(false)
// Troca a cada abertura do diálogo para o formulário voltar vazio (sem o arquivo da vez anterior).
const chaveFormEnviar = ref(0)
const enviando = ref(false)
const erroEnviar = ref<string | null>(null)

const paraRemover = ref<Documento | null>(null)
const removendo = ref(false)
// Documento cujo arquivo está sendo buscado (abrir ou baixar): o item fica desabilitado até terminar.
const ocupadoId = ref<number | null>(null)

const snackbar = ref<{ aberto: boolean; mensagem: string; cor?: string }>({
  aberto: false,
  mensagem: '',
})

const mensagemVazio = computed(() => {
  if ((busca.value ?? '').trim()) {
    return 'Nenhum arquivo encontrado.'
  }
  return podeEnviar.value ? config.value.vazioPodeEnviar : config.value.vazio
})

onMounted(async () => {
  // O prédio dá o papel (e o título); a lista carrega em paralelo, e uma falha no prédio não a
  // impede de aparecer — sem papel, a tela fica só de leitura.
  predioStore.obter(predioId.value).catch(() => undefined)
  await carregar()
})

// Ir de atas para normas (ou de um prédio para outro) reaproveita esta mesma instância da view: a
// busca e a lista precisam recomeçar.
watch([tipo, predioId], () => {
  clearTimeout(timerBusca)
  busca.value = ''
  predioStore.obter(predioId.value).catch(() => undefined)
  carregar()
})

// Debounce da busca: a requisição só sai depois de o usuário parar de digitar. Limpar o campo (X) e
// Enter buscam na hora — são ações deliberadas, não digitação em andamento.
const ATRASO_BUSCA_MS = 500
let timerBusca: ReturnType<typeof setTimeout> | undefined

function aplicarBusca() {
  clearTimeout(timerBusca)
  // `clearable` do Vuetify põe `null` no v-model ao clicar no X.
  const texto = (busca.value ?? '').trim()
  if (texto !== lista.value.nome) {
    carregar()
  }
}

watch(busca, (valor) => {
  clearTimeout(timerBusca)
  if (!(valor ?? '').trim()) {
    aplicarBusca()
    return
  }
  timerBusca = setTimeout(aplicarBusca, ATRASO_BUSCA_MS)
})
onBeforeUnmount(() => clearTimeout(timerBusca))

async function carregar() {
  erro.value = null
  carregando.value = true
  try {
    await documentoStore.listarPrimeiraPagina(
      tipo.value,
      predioId.value,
      (busca.value ?? '').trim(),
    )
  } catch {
    erro.value = `Não foi possível carregar as ${config.value.titulo.toLowerCase()}.`
  } finally {
    carregando.value = false
  }
}

async function carregarMais() {
  try {
    await documentoStore.carregarProximaPagina(tipo.value, predioId.value)
  } catch {
    erro.value = `Não foi possível carregar mais ${config.value.titulo.toLowerCase()}.`
  }
}

function avisar(mensagem: string, cor?: string) {
  snackbar.value = { aberto: true, mensagem, cor }
}

function abrirFormEnviar() {
  erroEnviar.value = null
  chaveFormEnviar.value++
  mostrarFormEnviar.value = true
}

async function onEnviar(arquivo: File) {
  erroEnviar.value = null
  enviando.value = true
  try {
    await documentoStore.enviar(tipo.value, predioId.value, arquivo)
    // Enviar recarrega a primeira página sem busca: o campo acompanha.
    busca.value = ''
    mostrarFormEnviar.value = false
    avisar(config.value.enviada)
  } catch (error) {
    // O diálogo continua aberto para o usuário escolher outro arquivo.
    erroEnviar.value = mensagemErroEnvio(error)
  } finally {
    enviando.value = false
  }
}

// A aba precisa nascer dentro do clique: o arquivo só chega depois da requisição, e abrir a aba
// depois dessa espera é bloqueado pelos navegadores (Safari, celulares). Ela abre em branco e é
// apontada para o arquivo quando ele chega; se a busca falhar, é fechada. Só PDF e imagem chegam
// aqui (o item baixa DOCX direto) — o tipo do Blob vem da extensão, nunca da resposta.
async function abrir(documento: Documento) {
  const aba = abrirAbaEmBranco()
  if (!aba) {
    avisar('O navegador bloqueou a nova aba. Permita pop-ups ou use o ícone de baixar.', 'warning')
    return
  }
  ocupadoId.value = documento.id
  try {
    const arquivo = await documentoStore.baixar(
      tipo.value,
      predioId.value,
      documento.id,
      documento.nomeArquivo,
    )
    mostrarArquivoNaAba(aba, arquivo)
  } catch {
    aba.close()
    avisar('Não foi possível abrir o arquivo. Tente novamente.', 'error')
  } finally {
    ocupadoId.value = null
  }
}

async function baixar(documento: Documento) {
  ocupadoId.value = documento.id
  try {
    const arquivo = await documentoStore.baixar(
      tipo.value,
      predioId.value,
      documento.id,
      documento.nomeArquivo,
    )
    baixarArquivo(arquivo, documento.nomeArquivo)
  } catch {
    avisar('Não foi possível baixar o arquivo. Tente novamente.', 'error')
  } finally {
    ocupadoId.value = null
  }
}

async function confirmarRemocao() {
  if (!paraRemover.value) {
    return
  }
  removendo.value = true
  try {
    await documentoStore.remover(tipo.value, predioId.value, paraRemover.value.id)
    paraRemover.value = null
    avisar(config.value.removida)
  } catch {
    paraRemover.value = null
    avisar(`Não foi possível remover a ${config.value.singular}. Tente novamente.`, 'error')
  } finally {
    removendo.value = false
  }
}
</script>

<template>
  <v-container class="documentos-list d-flex flex-column fill-height">
    <AppHeader />

    <div class="documentos-busca">
      <v-text-field
        v-model="busca"
        data-cy="busca-documento"
        placeholder="Buscar pelo nome do arquivo..."
        prepend-inner-icon="mdi-magnify"
        density="compact"
        hide-details
        single-line
        clearable
        @keydown.enter="aplicarBusca"
      />
    </div>

    <v-alert
      v-if="erro"
      type="error"
      variant="tonal"
      density="compact"
      class="mb-4"
      data-cy="erro-documentos"
    >
      {{ erro }}
    </v-alert>

    <v-progress-circular
      v-if="carregando"
      indeterminate
      color="primary"
      class="align-self-center mt-8"
    />

    <template v-else>
      <p
        v-if="lista.documentos.length === 0"
        class="text-center text-medium-emphasis mt-8"
        data-cy="documentos-vazio"
      >
        {{ mensagemVazio }}
      </p>

      <div v-else class="flex-grow-1">
        <DocumentoListItem
          v-for="documento in lista.documentos"
          :key="documento.id"
          :documento="documento"
          :pode-remover="podeExcluir"
          :ocupado="ocupadoId === documento.id"
          class="mb-3"
          @abrir="abrir(documento)"
          @baixar="baixar(documento)"
          @remover="paraRemover = documento"
        />

        <v-btn
          v-if="documentoStore.temMaisPaginas(tipo)"
          variant="text"
          color="primary"
          block
          data-cy="btn-carregar-mais"
          @click="carregarMais"
        >
          Carregar mais
        </v-btn>
      </div>
    </template>

    <v-btn
      v-if="podeEnviar"
      icon="mdi-plus"
      color="primary"
      size="large"
      class="documentos-fab"
      :aria-label="`Enviar ${config.singular}`"
      data-cy="btn-enviar-documento-fab"
      @click="abrirFormEnviar"
    />

    <v-dialog v-model="mostrarFormEnviar" max-width="400">
      <v-card rounded="lg">
        <v-card-item>
          <v-card-title class="text-h6 font-weight-bold">Enviar {{ config.singular }}</v-card-title>
          <v-card-subtitle class="text-wrap">
            Formatos aceitos: {{ FORMATOS_ACEITOS_TEXTO }}, até 20 MB. {{ config.limite }}
          </v-card-subtitle>
        </v-card-item>
        <v-card-text>
          <v-alert
            v-if="erroEnviar"
            type="error"
            variant="tonal"
            density="compact"
            class="mb-4"
            data-cy="erro-enviar-documento"
          >
            {{ erroEnviar }}
          </v-alert>
          <EnviarDocumentoForm
            :key="chaveFormEnviar"
            :enviando="enviando"
            :rotulo-botao="`Enviar ${config.singular}`"
            @submit="onEnviar"
          />
        </v-card-text>
      </v-card>
    </v-dialog>

    <ConfirmDialog
      :model-value="paraRemover !== null"
      :titulo="`Remover ${config.singular}`"
      :mensagem="`Tem certeza que deseja remover ${paraRemover?.nomeArquivo ?? `esta ${config.singular}`}? Essa ação não pode ser desfeita.`"
      texto-confirmar="Remover"
      cor-confirmar="error"
      :carregando="removendo"
      @update:model-value="paraRemover = null"
      @confirmar="confirmarRemocao"
    />

    <v-snackbar v-model="snackbar.aberto" :color="snackbar.cor" :timeout="5000" data-cy="snackbar">
      {{ snackbar.mensagem }}
    </v-snackbar>
  </v-container>
</template>

<style scoped>
.documentos-list {
  max-width: 480px;
  margin-inline: auto;
}

/* Gruda logo abaixo do AppHeader — mesmo truque de PlanejamentosListView.vue (margin-top negativo
   cancela o mb-2 do AppHeader, o respiro volta como padding-top). */
.documentos-busca {
  position: sticky;
  top: var(--app-header-height);
  z-index: 1;
  flex: none;
  margin-top: -8px;
  background: rgb(var(--v-theme-background));
  padding-top: 8px;
  padding-bottom: 16px;
}

.documentos-fab {
  position: fixed;
  right: 24px;
  bottom: 24px;
}
</style>
