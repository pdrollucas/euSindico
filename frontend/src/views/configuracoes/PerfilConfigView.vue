<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import AppHeader from '@/components/common/AppHeader.vue'
import PerfilNomeForm from '@/components/configuracoes/PerfilNomeForm.vue'
import TrocarEmailForm from '@/components/configuracoes/TrocarEmailForm.vue'
import VerificarCodigoForm from '@/components/auth/VerificarCodigoForm.vue'
import { usePerfilStore } from '@/stores/perfilStore'
import { useContagemRegressiva } from '@/composables/useContagemRegressiva'
import { mensagemDoErro } from '@/utils/mensagemDoErro'
import { formatarContagem } from '@/utils/tempo'
import type { AtualizarPerfilRequest, SolicitarTrocaEmailRequest } from '@/types/perfil.types'
import type { VerificarCodigoFormValues } from '@/types/auth.types'

const perfilStore = usePerfilStore()

const carregando = ref(false)
const erroCarregar = ref<string | null>(null)

const salvandoNome = ref(false)
const erroNome = ref<string | null>(null)

const enviandoCodigo = ref(false)
const confirmando = ref(false)
const erroEmail = ref<string | null>(null)

const snackbar = ref<{ aberto: boolean; mensagem: string }>({ aberto: false, mensagem: '' })

// Passo 2 da troca de e-mail: já existe um pedido aguardando o código.
const aguardandoCodigo = computed(() => perfilStore.novoEmailPendente !== null)

// O backend só aceita um novo pedido depois de 2 min; o botão de voltar ao passo 1 espelha isso.
const cooldownExpiraEm = computed(() => perfilStore.cooldownExpiraEm)
const { segundos: cooldownSegundos } = useContagemRegressiva(cooldownExpiraEm)
const podePedirNovoCodigo = computed(() => cooldownSegundos.value === 0)

onMounted(async () => {
  // O perfil pode já estar na store (a Home o carrega para a saudação); recarregar garante o dado atual.
  carregando.value = perfilStore.perfil === null
  try {
    await perfilStore.carregar()
  } catch {
    if (perfilStore.perfil === null) erroCarregar.value = 'Não foi possível carregar o seu perfil.'
  } finally {
    carregando.value = false
  }
})

function avisar(mensagem: string) {
  snackbar.value = { aberto: true, mensagem }
}

async function onSalvarNome(payload: AtualizarPerfilRequest) {
  erroNome.value = null
  salvandoNome.value = true
  try {
    await perfilStore.atualizarNome(payload.nome)
    avisar('Nome atualizado.')
  } catch (error) {
    erroNome.value = mensagemDoErro(error, 'Não foi possível salvar o nome. Tente novamente.', [])
  } finally {
    salvandoNome.value = false
  }
}

async function onSolicitarCodigo(payload: SolicitarTrocaEmailRequest) {
  erroEmail.value = null
  enviandoCodigo.value = true
  try {
    await perfilStore.solicitarTrocaEmail(payload.novoEmail, payload.senhaAtual)
  } catch (error) {
    // 400 = senha incorreta, 409 = e-mail já cadastrado, 429 = cooldown/limite: o title já é a mensagem.
    erroEmail.value = mensagemDoErro(
      error,
      'Não foi possível enviar o código. Tente novamente.',
      [400, 409, 429],
    )
  } finally {
    enviandoCodigo.value = false
  }
}

async function onConfirmarCodigo(payload: VerificarCodigoFormValues) {
  erroEmail.value = null
  confirmando.value = true
  try {
    await perfilStore.confirmarTrocaEmail(payload.codigo)
    avisar('E-mail atualizado.')
  } catch (error) {
    erroEmail.value = mensagemDoErro(
      error,
      'Não foi possível confirmar o e-mail. Tente novamente.',
      [400, 409, 429],
    )
  } finally {
    confirmando.value = false
  }
}

function pedirNovoCodigo() {
  erroEmail.value = null
  perfilStore.cancelarTrocaEmail()
}
</script>

<template>
  <v-container class="config-perfil d-flex flex-column fill-height">
    <AppHeader />

    <v-progress-circular
      v-if="carregando"
      indeterminate
      color="primary"
      class="align-self-center mt-8"
    />

    <v-alert
      v-else-if="erroCarregar"
      type="error"
      variant="tonal"
      density="compact"
      data-cy="erro-carregar-perfil"
    >
      {{ erroCarregar }}
    </v-alert>

    <template v-else-if="perfilStore.perfil">
      <v-card rounded="lg" elevation="2" class="mb-4">
        <v-card-item>
          <v-card-title class="text-h6 font-weight-bold">Nome</v-card-title>
        </v-card-item>
        <v-card-text>
          <v-alert
            v-if="erroNome"
            type="error"
            variant="tonal"
            density="compact"
            class="mb-4"
            data-cy="erro-nome"
          >
            {{ erroNome }}
          </v-alert>
          <PerfilNomeForm
            :nome-inicial="perfilStore.perfil.nome"
            :carregando="salvandoNome"
            @submit="onSalvarNome"
          />
        </v-card-text>
      </v-card>

      <v-card rounded="lg" elevation="2">
        <v-card-item>
          <v-card-title class="text-h6 font-weight-bold">E-mail</v-card-title>
          <v-card-subtitle class="text-wrap" data-cy="email-atual">
            {{ perfilStore.perfil.email }}
          </v-card-subtitle>
        </v-card-item>
        <v-card-text>
          <v-alert
            v-if="erroEmail"
            type="error"
            variant="tonal"
            density="compact"
            class="mb-4"
            data-cy="erro-email"
          >
            {{ erroEmail }}
          </v-alert>

          <template v-if="aguardandoCodigo">
            <p class="text-body-2 mb-4" data-cy="email-aguardando-codigo">
              Enviamos um código de 6 caracteres para
              <strong>{{ perfilStore.novoEmailPendente }}</strong
              >. Informe-o abaixo para confirmar a troca.
            </p>
            <VerificarCodigoForm
              texto-botao="Confirmar e-mail"
              :carregando="confirmando"
              @submit="onConfirmarCodigo"
            />
            <div class="text-center mt-4">
              <v-btn
                variant="text"
                color="primary"
                size="small"
                :disabled="!podePedirNovoCodigo"
                data-cy="btn-novo-codigo-email"
                @click="pedirNovoCodigo"
              >
                <template v-if="podePedirNovoCodigo">Corrigir e-mail ou pedir novo código</template>
                <template v-else>
                  Pedir novo código em {{ formatarContagem(cooldownSegundos) }}
                </template>
              </v-btn>
            </div>
          </template>

          <template v-else>
            <p class="text-body-2 text-medium-emphasis mb-4">
              Para trocar, informe o novo e-mail e a sua senha. Enviaremos um código para o novo
              endereço.
            </p>
            <TrocarEmailForm :carregando="enviandoCodigo" @submit="onSolicitarCodigo" />
          </template>
        </v-card-text>
      </v-card>
    </template>

    <v-snackbar v-model="snackbar.aberto" :timeout="4000" data-cy="snackbar">
      {{ snackbar.mensagem }}
    </v-snackbar>
  </v-container>
</template>

<style scoped>
.config-perfil {
  max-width: 480px;
  margin-inline: auto;
}
</style>
