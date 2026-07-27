import { ref, watch } from 'vue'
import { defineStore } from 'pinia'
import { authService } from '@/services/authService'

// Duração do cooldown de reenvio do código, espelhando o cooldown de 2 minutos por conta que o
// backend aplica em POST /auth/esqueci-senha (RN15 / backend SECURITY.md, seção 10). O backend é
// a barreira real (vale mesmo que o front seja contornado); este valor só governa a UX de
// desabilitar o botão localmente — ver frontend/documentation/SECURITY.md.
export const COOLDOWN_REENVIO_MS = 2 * 60 * 1000

// Persistência do fluxo de recuperação (RF06-A) em sessionStorage, para o timer de cooldown
// sobreviver a um F5 — sem isso, recarregar zeraria a contagem e permitiria pedir um novo envio
// que o backend, por causa do cooldown, aceitaria (204) mas não enviaria: UX enganosa.
//
// Só o e-mail e o instante de expiração do cooldown são persistidos. O CÓDIGO NUNCA é persistido:
// é um segredo de redefinição de senha, e guardá-lo em storage acessível a JavaScript teria o
// mesmo risco que levou o refresh token para cookie HttpOnly (ver SECURITY.md). sessionStorage
// (e não localStorage): sobrevive ao reload, mas some ao fechar a aba e é isolado por aba.
const STORAGE_KEY = 'recuperacaoSenha'

interface EstadoPersistido {
  email: string | null
  cooldownExpiraEm: number | null
}

function carregar(): EstadoPersistido {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (raw === null) return { email: null, cooldownExpiraEm: null }
    const dados = JSON.parse(raw) as Partial<EstadoPersistido>
    return {
      email: dados.email ?? null,
      cooldownExpiraEm: typeof dados.cooldownExpiraEm === 'number' ? dados.cooldownExpiraEm : null,
    }
  } catch {
    // JSON corrompido ou sessionStorage indisponível — degrada para só-memória.
    return { email: null, cooldownExpiraEm: null }
  }
}

function persistir(estado: EstadoPersistido) {
  try {
    if (estado.email === null && estado.cooldownExpiraEm === null) {
      sessionStorage.removeItem(STORAGE_KEY)
    } else {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(estado))
    }
  } catch {
    // sessionStorage indisponível (bloqueado/cheio) — degrada para só-memória, sem quebrar o fluxo.
  }
}

// Estado do fluxo de recuperação de senha esquecida (RF06-A), compartilhado entre as três telas:
// EsqueciSenha -> VerificarCodigo -> RedefinirSenha. E-mail e cooldown sobrevivem a um F5
// (sessionStorage); o código, não — reabrir /redefinir-senha após um reload volta para a tela de
// código, e reabrir /verificar-codigo sem e-mail volta para o início.
export const useRecuperacaoSenhaStore = defineStore('recuperacaoSenha', () => {
  const inicial = carregar()
  const email = ref<string | null>(inicial.email)
  const codigo = ref<string | null>(null) // nunca persistido (segredo)
  const cooldownExpiraEm = ref<number | null>(inicial.cooldownExpiraEm)

  // Espelha email + cooldown para o sessionStorage a cada mudança (o código fica de fora).
  watch([email, cooldownExpiraEm], ([e, c]) => persistir({ email: e, cooldownExpiraEm: c }))

  // Reinicia os 2 minutos — chamado a cada envio/reenvio de código bem-sucedido.
  function iniciarCooldown() {
    cooldownExpiraEm.value = Date.now() + COOLDOWN_REENVIO_MS
  }

  function limpar() {
    email.value = null
    codigo.value = null
    cooldownExpiraEm.value = null
  }

  // Solicita (ou reenvia) o código. Só marca o e-mail e inicia o cooldown se a chamada tiver
  // sucesso — um 429 (rate limit) propaga o erro sem iniciar cooldown nem avançar de tela.
  async function solicitarCodigo(emailInformado: string) {
    await authService.esqueciSenha({ email: emailInformado })
    email.value = emailInformado
    iniciarCooldown()
  }

  // Verifica o código (UX — a validação real é refeita no redefinir). Só guarda o código se a
  // API confirmar (204); um 400 propaga o erro.
  async function verificar(codigoInformado: string) {
    if (email.value === null) throw new Error('Fluxo de recuperação sem e-mail definido')
    await authService.verificarCodigo({ email: email.value, codigo: codigoInformado })
    codigo.value = codigoInformado
  }

  async function redefinir(novaSenha: string, confirmarSenha: string) {
    if (email.value === null || codigo.value === null) {
      throw new Error('Fluxo de recuperação sem e-mail ou código definido')
    }
    await authService.redefinirSenha({
      email: email.value,
      codigo: codigo.value,
      novaSenha,
      confirmarSenha,
    })
    limpar()
  }

  return {
    email,
    codigo,
    cooldownExpiraEm,
    iniciarCooldown,
    limpar,
    solicitarCodigo,
    verificar,
    redefinir,
  }
})
