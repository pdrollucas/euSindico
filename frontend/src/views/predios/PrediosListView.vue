<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
import AppHeader from '@/components/common/AppHeader.vue'
import PredioCard from '@/components/predios/PredioCard.vue'
import PredioForm from '@/components/predios/PredioForm.vue'
import { usePredioStore } from '@/stores/predioStore'
import type { PredioFormValues } from '@/types/predio.types'
import type { ErroApiDto } from '@/types/erroApi.types'

const router = useRouter()
const predioStore = usePredioStore()

const busca = ref('')
const carregando = ref(false)
const erro = ref<string | null>(null)

const mostrarFormCriar = ref(false)
const criando = ref(false)
const erroCriar = ref<string | null>(null)

// Filtro client-side — o backend não expõe busca por nome.
const prediosFiltrados = computed(() => {
  const termo = busca.value.trim().toLowerCase()
  if (!termo) {
    return predioStore.predios
  }
  return predioStore.predios.filter((predio) => predio.nome.toLowerCase().includes(termo))
})

onMounted(carregar)

async function carregar() {
  erro.value = null
  carregando.value = true
  try {
    await predioStore.listarPrimeiraPagina()
  } catch {
    erro.value = 'Não foi possível carregar os prédios.'
  } finally {
    carregando.value = false
  }
}

async function carregarMais() {
  try {
    await predioStore.carregarProximaPagina()
  } catch {
    erro.value = 'Não foi possível carregar mais prédios.'
  }
}

async function onCriar(payload: PredioFormValues) {
  erroCriar.value = null
  criando.value = true
  try {
    const predio = await predioStore.criar(payload)
    mostrarFormCriar.value = false
    router.push(`/predios/${predio.id}`)
  } catch (error) {
    // Um 409 cobre duas exceções diferentes (duplicidade vs. limite de 20 prédios, PREDIOS.md)
    // — mostra o title do backend em vez de reinventar a mensagem.
    if (axios.isAxiosError<ErroApiDto>(error) && error.response?.status === 409) {
      erroCriar.value = error.response.data?.title ?? 'Não foi possível criar o prédio. Tente novamente.'
    } else {
      erroCriar.value = 'Não foi possível criar o prédio. Tente novamente.'
    }
  } finally {
    criando.value = false
  }
}
</script>

<template>
  <v-container class="predios-list d-flex flex-column fill-height">
    <AppHeader />

    <v-text-field
      v-model="busca"
      data-cy="busca-predio"
      placeholder="Buscar prédio..."
      prepend-inner-icon="mdi-magnify"
      density="compact"
      hide-details
      single-line
      class="predios-busca"
    />

    <v-alert v-if="erro" type="error" variant="tonal" density="compact" class="mb-4" data-cy="erro-predios">
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
        v-if="prediosFiltrados.length === 0"
        class="text-center text-medium-emphasis mt-8"
        data-cy="predios-vazio"
      >
        {{ busca ? 'Nenhum prédio encontrado.' : 'Você ainda não tem nenhum prédio. Crie o primeiro!' }}
      </p>

      <div v-else class="flex-grow-1">
        <PredioCard v-for="predio in prediosFiltrados" :key="predio.id" :predio="predio" />

        <v-btn
          v-if="!busca && predioStore.temMaisPaginas"
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
      icon="mdi-plus"
      color="primary"
      size="large"
      class="predios-fab"
      aria-label="Criar prédio"
      data-cy="btn-criar-predio"
      @click="mostrarFormCriar = true"
    />

    <v-dialog v-model="mostrarFormCriar" max-width="400">
      <v-card rounded="lg">
        <v-card-item>
          <v-card-title class="text-h6 font-weight-bold">Novo prédio</v-card-title>
          <v-card-subtitle style="white-space: normal">
            Informe os dados do prédio que você administra.
          </v-card-subtitle>
        </v-card-item>
        <v-card-text>
          <v-alert
            v-if="erroCriar"
            type="error"
            variant="tonal"
            density="compact"
            class="mb-4"
            data-cy="erro-predio-form"
          >
            {{ erroCriar }}
          </v-alert>
          <PredioForm :salvando="criando" @submit="onCriar" />
        </v-card-text>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<style scoped>
.predios-list {
  max-width: 480px;
  margin-inline: auto;
}

/* Gruda logo abaixo do AppHeader (`top` = mesma var --app-header-height). `margin-top: -8px`
   cancela o mb-2 do AppHeader pra descanso e posição "grudada" coincidirem (sem salto ao
   rolar); o respiro de 8px volta como padding-top, dentro da área coberta pelo background.
   `flex: none` porque VInput vem com `flex: 1 1 auto` por padrão no Vuetify, que dentro de um
   container flex-column faz o campo crescer e brigar com a lista abaixo. */
.predios-busca {
  position: sticky;
  top: var(--app-header-height);
  z-index: 1;
  flex: none;
  margin-top: -8px;
  background: rgb(var(--v-theme-background));
  padding-top: 8px;
  padding-bottom: 16px;
}

/* FAB fixo no canto inferior direito — mesmo padrão da Home (ver DESIGN.md, "FAB"). */
.predios-fab {
  position: fixed;
  right: 24px;
  bottom: 24px;
}
</style>
