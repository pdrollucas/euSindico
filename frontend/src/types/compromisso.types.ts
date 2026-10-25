import { z } from 'zod'
import type {
  compromissoFormSchema,
  compromissoSchema,
  compromissosPaginadosSchema,
  escopoCompromissosSchema,
  situacaoCompromissosSchema,
} from '@/schemas/compromisso.schema'

export type CompromissoFormValues = z.infer<typeof compromissoFormSchema>
export type Compromisso = z.infer<typeof compromissoSchema>
export type CompromissosPaginados = z.infer<typeof compromissosPaginadosSchema>
export type EscopoCompromissos = z.infer<typeof escopoCompromissosSchema>
export type SituacaoCompromissos = z.infer<typeof situacaoCompromissosSchema>

export interface CompromissoFiltros {
  titulo: string
  situacao: SituacaoCompromissos
  escopo: EscopoCompromissos
  predioId: number | null
  // Período pela data do compromisso. `mes` só vale com `ano` (sem mês = o ano todo).
  ano: number | null
  mes: number | null
}

export const SITUACOES_COMPROMISSO: { valor: SituacaoCompromissos; rotulo: string }[] = [
  { valor: 'proximos', rotulo: 'Próximos' },
  { valor: 'hoje', rotulo: 'Hoje' },
  { valor: 'atrasados', rotulo: 'Atrasados' },
  { valor: 'concluidos', rotulo: 'Concluídos' },
  { valor: 'todos', rotulo: 'Todos' },
]
