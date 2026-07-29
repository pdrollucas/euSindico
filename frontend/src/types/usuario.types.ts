import { z } from 'zod'
import type { usuarioSchema } from '@/schemas/usuario.schema'

export type Usuario = z.infer<typeof usuarioSchema>
