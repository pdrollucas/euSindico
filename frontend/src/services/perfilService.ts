import api from '@/plugins/axios'
import { usuarioSchema } from '@/schemas/usuario.schema'
import type { Usuario } from '@/types/usuario.types'

// Uma função por endpoint de /perfil (PerfilController do backend) — ver
// frontend/documentation/ARCHITECTURE.md, seção "services/". Valida a resposta com o schema Zod.

// GET /perfil — dados do usuário autenticado (RF04). O Authorization: Bearer é anexado pelo
// interceptor do Axios a partir do authStore (ver AUTHENTICATION.md).
async function obterPerfil(): Promise<Usuario> {
  const { data } = await api.get('/perfil')
  return usuarioSchema.parse(data)
}

export const perfilService = {
  obterPerfil,
}
