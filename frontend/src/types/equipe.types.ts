import { z } from 'zod'
import type {
  convidarFuncionarioRequestSchema,
  equipeMembroSchema,
  conviteDetalheSchema,
  aceitarConviteNovaContaFormSchema,
  papelConvidavelSchema,
} from '@/schemas/equipe.schema'

export type PapelConvidavel = z.infer<typeof papelConvidavelSchema>
export type ConvidarFuncionarioRequest = z.infer<typeof convidarFuncionarioRequestSchema>
export type EquipeMembro = z.infer<typeof equipeMembroSchema>
export type ConviteDetalhe = z.infer<typeof conviteDetalheSchema>
export type AceitarConviteNovaContaForm = z.infer<typeof aceitarConviteNovaContaFormSchema>

// AceitarConviteDto (backend): Nome/Senha só existem quando a conta ainda não existe.
export interface AceitarConviteRequest {
  nome?: string
  senha?: string
}
