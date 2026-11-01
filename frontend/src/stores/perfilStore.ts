import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { perfilService } from '@/services/perfilService'
import { COOLDOWN_REENVIO_MS } from '@/stores/recuperacaoSenhaStore'
import type { AlterarSenhaRequest } from '@/types/perfil.types'
import type { Usuario } from '@/types/usuario.types'

// Perfil do usuário autenticado (RF04/RF05). Carregado sob demanda (ex: na Home, para a saudação)
// e limpo no logout. Não é persistido — some num F5 e é recarregado quando necessário.
export const usePerfilStore = defineStore('perfil', () => {
  const perfil = ref<Usuario | null>(null)

  // Troca de e-mail em andamento (passo 2: aguardando o código). Só em memória: o código é um
  // segredo e a senha nunca é guardada, então um F5 volta ao passo 1.
  const novoEmailPendente = ref<string | null>(null)
  // Espelha o cooldown de 2 minutos do backend entre pedidos de código (só governa a UX do botão).
  const cooldownExpiraEm = ref<number | null>(null)

  // Primeiro nome, para a saudação no cabeçalho da Home ("Olá, Luciano").
  const primeiroNome = computed(() => perfil.value?.nome.split(' ')[0] ?? null)

  async function carregar() {
    perfil.value = await perfilService.obterPerfil()
  }

  async function atualizarNome(nome: string) {
    perfil.value = await perfilService.atualizarPerfil({ nome })
  }

  // Só marca o pedido como pendente se a API aceitar (204); um 400/409/429 propaga sem avançar.
  async function solicitarTrocaEmail(novoEmail: string, senhaAtual: string) {
    await perfilService.solicitarTrocaEmail({ novoEmail, senhaAtual })
    novoEmailPendente.value = novoEmail
    cooldownExpiraEm.value = Date.now() + COOLDOWN_REENVIO_MS
  }

  async function confirmarTrocaEmail(codigo: string) {
    perfil.value = await perfilService.confirmarTrocaEmail({ codigo })
    novoEmailPendente.value = null
    cooldownExpiraEm.value = null
  }

  // Volta ao passo 1 (ex: e-mail digitado errado). O cooldown continua valendo no backend.
  function cancelarTrocaEmail() {
    novoEmailPendente.value = null
  }

  // O backend revoga todas as sessões; quem chama encerra a sessão local em seguida.
  async function alterarSenha(payload: AlterarSenhaRequest) {
    await perfilService.alterarSenha(payload)
  }

  async function excluirConta(senha: string) {
    await perfilService.excluirConta({ senha })
  }

  function limpar() {
    perfil.value = null
    novoEmailPendente.value = null
    cooldownExpiraEm.value = null
  }

  return {
    perfil,
    novoEmailPendente,
    cooldownExpiraEm,
    primeiroNome,
    carregar,
    atualizarNome,
    solicitarTrocaEmail,
    confirmarTrocaEmail,
    cancelarTrocaEmail,
    alterarSenha,
    excluirConta,
    limpar,
  }
})
