import { z } from 'zod'
import type {
  atualizarPerfilRequestSchema,
  solicitarTrocaEmailRequestSchema,
  confirmarTrocaEmailRequestSchema,
  alterarSenhaRequestSchema,
  alterarSenhaFormSchema,
  excluirContaRequestSchema,
  enviarSuporteRequestSchema,
} from '@/schemas/perfil.schema'

// Tipos inferidos dos schemas Zod — nunca escritos à mão em paralelo (ver
// frontend/documentation/ARCHITECTURE.md, seção "types/").
export type AtualizarPerfilRequest = z.infer<typeof atualizarPerfilRequestSchema>
export type SolicitarTrocaEmailRequest = z.infer<typeof solicitarTrocaEmailRequestSchema>
export type ConfirmarTrocaEmailRequest = z.infer<typeof confirmarTrocaEmailRequestSchema>
export type AlterarSenhaRequest = z.infer<typeof alterarSenhaRequestSchema>
export type AlterarSenhaFormValues = z.infer<typeof alterarSenhaFormSchema>
export type ExcluirContaRequest = z.infer<typeof excluirContaRequestSchema>
export type EnviarSuporteRequest = z.infer<typeof enviarSuporteRequestSchema>
