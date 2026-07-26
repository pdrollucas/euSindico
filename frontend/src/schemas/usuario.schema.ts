import { z } from 'zod'

// Espelha UsuarioDto do backend (id, nome, email, criadoEm) — ver
// frontend/documentation/ARCHITECTURE.md, seção "schemas/".
export const usuarioSchema = z.object({
  id: z.number(),
  nome: z.string(),
  email: z.string().email(),
  criadoEm: z.string(),
})
