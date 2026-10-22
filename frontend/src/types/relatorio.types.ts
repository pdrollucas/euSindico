import { z } from 'zod'
import type {
  gerarRelatorioFormSchema,
  relatorioSchema,
  relatoriosPaginadosSchema,
} from '@/schemas/relatorio.schema'

export type GerarRelatorioFormValues = z.infer<typeof gerarRelatorioFormSchema>
export type Relatorio = z.infer<typeof relatorioSchema>
export type RelatoriosPaginados = z.infer<typeof relatoriosPaginadosSchema>
