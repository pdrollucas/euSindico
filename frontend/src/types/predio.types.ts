import { z } from 'zod'
import type {
  predioFormSchema,
  predioSchema,
  prediosPaginadosSchema,
  papelPredioSchema,
} from '@/schemas/predio.schema'

export type PredioFormValues = z.infer<typeof predioFormSchema>
export type PapelPredio = z.infer<typeof papelPredioSchema>
export type Predio = z.infer<typeof predioSchema>
export type PrediosPaginados = z.infer<typeof prediosPaginadosSchema>

// Espelha euSindico.Domain.Enums.PapelPredio — valores fixos, nunca renumerar.
export const PAPEL_PREDIO = {
  SINDICO: 1,
  GESTOR: 2,
  COLABORADOR: 3,
} as const satisfies Record<string, PapelPredio>
