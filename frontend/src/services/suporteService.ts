import api from '@/plugins/axios'
import { enviarSuporteRequestSchema } from '@/schemas/perfil.schema'
import type { EnviarSuporteRequest } from '@/types/perfil.types'

// POST /suporte (SuporteController do backend) — a mensagem vira um e-mail para a equipe, com o
// Reply-To do usuário; o backend anexa id, nome e e-mail a partir do token (204).
async function enviar(payload: EnviarSuporteRequest): Promise<void> {
  const body = enviarSuporteRequestSchema.parse(payload)
  await api.post('/suporte', body)
}

export const suporteService = {
  enviar,
}
