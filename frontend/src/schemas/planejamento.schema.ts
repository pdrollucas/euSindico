import { z } from 'zod'
import {
  detalhesSchema,
  ORCAMENTO_MAXIMO,
  orcamentoSchema,
  prestadorSchema,
  tituloSchema,
} from '@/schemas/compromisso.schema'
import { hojeIso } from '@/utils/dataHora'

export { ORCAMENTO_MAXIMO }

// Valor cru do <input type="date"> ("2026-10-20"), ou vazio: a data é opcional (RN11). Quando
// preenchida, hoje ou futura (RN30) — o backend repete a regra no fuso da aplicação e é a fonte
// da verdade; aqui evita só a ida e volta de um 400 previsível.
const dataSchema = z
  .string()
  .refine((valor) => valor === '' || /^\d{4}-\d{2}-\d{2}$/.test(valor), 'Data inválida')
  .refine((valor) => valor === '' || valor >= hojeIso(), 'A data não pode estar no passado')
  .nullish()

// Espelha PlanejamentoFormDto — mesmo DTO em POST e PUT. O prédio vai na rota, não no corpo. O
// orçamento fica como texto mascarado no formulário e vira número em `planejamentoService`.
export const planejamentoFormSchema = z.object({
  titulo: tituloSchema,
  dataPlanejamento: dataSchema,
  orcamentoPrevisto: orcamentoSchema,
  nomePrestadorServico: prestadorSchema.optional(),
  detalhes: detalhesSchema.optional(),
})

// Espelha PlanejamentoDto. DateOnly chega como "2026-10-20"; decimal como número.
export const planejamentoSchema = z.object({
  id: z.number(),
  titulo: z.string(),
  dataPlanejamento: z.string().nullable(),
  orcamentoPrevisto: z.number().nullable(),
  nomePrestadorServico: z.string().nullable(),
  detalhes: z.string().nullable(),
  predioId: z.number(),
  predioNome: z.string(),
  criadoEm: z.string(),
})

// Espelha PagedResultDto<PlanejamentoDto> (RNF10).
export const planejamentosPaginadosSchema = z.object({
  items: z.array(planejamentoSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})
