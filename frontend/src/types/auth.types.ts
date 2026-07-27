import { z } from 'zod'
import type {
  loginRequestSchema,
  accessTokenResponseSchema,
  registrarRequestSchema,
  esqueciSenhaRequestSchema,
  verificarCodigoRequestSchema,
  verificarCodigoFormSchema,
  redefinirSenhaRequestSchema,
  redefinirSenhaFormSchema,
} from '@/schemas/auth.schema'

// Tipos inferidos dos schemas Zod — nunca escritos à mão em paralelo (ver
// frontend/documentation/ARCHITECTURE.md, seção "types/").
export type LoginRequest = z.infer<typeof loginRequestSchema>
export type AccessTokenResponse = z.infer<typeof accessTokenResponseSchema>
export type RegistrarRequest = z.infer<typeof registrarRequestSchema>
export type EsqueciSenhaRequest = z.infer<typeof esqueciSenhaRequestSchema>
export type VerificarCodigoRequest = z.infer<typeof verificarCodigoRequestSchema>
export type VerificarCodigoFormValues = z.infer<typeof verificarCodigoFormSchema>
export type RedefinirSenhaRequest = z.infer<typeof redefinirSenhaRequestSchema>
export type RedefinirSenhaFormValues = z.infer<typeof redefinirSenhaFormSchema>
