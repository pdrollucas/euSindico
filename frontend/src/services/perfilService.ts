import api from '@/plugins/axios'
import {
  atualizarPerfilRequestSchema,
  solicitarTrocaEmailRequestSchema,
  confirmarTrocaEmailRequestSchema,
  alterarSenhaRequestSchema,
  excluirContaRequestSchema,
} from '@/schemas/perfil.schema'
import { usuarioSchema } from '@/schemas/usuario.schema'
import type {
  AtualizarPerfilRequest,
  SolicitarTrocaEmailRequest,
  ConfirmarTrocaEmailRequest,
  AlterarSenhaRequest,
  ExcluirContaRequest,
} from '@/types/perfil.types'
import type { Usuario } from '@/types/usuario.types'

// Uma função por endpoint de /perfil (PerfilController do backend) — ver
// frontend/documentation/ARCHITECTURE.md, seção "services/". Valida a resposta com o schema Zod.
// O Authorization: Bearer é anexado pelo interceptor do Axios a partir do authStore (ver
// AUTHENTICATION.md).

// GET /perfil — dados do usuário autenticado (RF04).
async function obterPerfil(): Promise<Usuario> {
  const { data } = await api.get('/perfil')
  return usuarioSchema.parse(data)
}

// PUT /perfil — só o nome; o e-mail muda apenas pelo fluxo com código abaixo (RF05).
async function atualizarPerfil(payload: AtualizarPerfilRequest): Promise<Usuario> {
  const body = atualizarPerfilRequestSchema.parse(payload)
  const { data } = await api.put('/perfil', body)
  return usuarioSchema.parse(data)
}

// POST /perfil/email/solicitar — confere a senha e envia um código ao NOVO e-mail (204).
async function solicitarTrocaEmail(payload: SolicitarTrocaEmailRequest): Promise<void> {
  const body = solicitarTrocaEmailRequestSchema.parse(payload)
  await api.post('/perfil/email/solicitar', body)
}

// POST /perfil/email/confirmar — valida o código e aplica a troca; devolve o perfil atualizado.
async function confirmarTrocaEmail(payload: ConfirmarTrocaEmailRequest): Promise<Usuario> {
  const body = confirmarTrocaEmailRequestSchema.parse(payload)
  const { data } = await api.post('/perfil/email/confirmar', body)
  return usuarioSchema.parse(data)
}

// PUT /perfil/senha — o backend encerra todas as sessões do usuário ao trocar a senha (204).
async function alterarSenha(payload: AlterarSenhaRequest): Promise<void> {
  const body = alterarSenhaRequestSchema.parse(payload)
  await api.put('/perfil/senha', body)
}

// DELETE /perfil — exclusão permanente da conta e dos dados; exige a senha no corpo (204).
async function excluirConta(payload: ExcluirContaRequest): Promise<void> {
  const body = excluirContaRequestSchema.parse(payload)
  await api.delete('/perfil', { data: body })
}

export const perfilService = {
  obterPerfil,
  atualizarPerfil,
  solicitarTrocaEmail,
  confirmarTrocaEmail,
  alterarSenha,
  excluirConta,
}
