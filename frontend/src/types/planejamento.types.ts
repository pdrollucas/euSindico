import { z } from 'zod'
import type {
  planejamentoFormSchema,
  planejamentoSchema,
  planejamentosPaginadosSchema,
} from '@/schemas/planejamento.schema'

export type PlanejamentoFormValues = z.infer<typeof planejamentoFormSchema>
export type Planejamento = z.infer<typeof planejamentoSchema>
export type PlanejamentosPaginados = z.infer<typeof planejamentosPaginadosSchema>

// Faixa de orçamento inclusiva; `null` = sem limite naquela ponta.
export interface PlanejamentoFiltros {
  titulo: string
  orcamentoMinimo: number | null
  orcamentoMaximo: number | null
}
