import { z } from 'zod'
import { tituloEhValido, prestadorEhValido, detalhesEhValido } from '@/utils/compromissoValidator'
import { moedaParaNumero } from '@/utils/moeda'

// Mesmo teto do backend (decimal(10,2)). Compartilhado com o orçamento previsto do planejamento.
export const ORCAMENTO_MAXIMO = 99_999_999.99

// Valor mascarado do campo ("1.500,50"), ou vazio. A máscara (`mascararMoeda`) já garante o formato
// e as 2 casas; o schema confere o formato por defesa e o teto do banco. Zero é válido.
export const orcamentoSchema = z
  .string()
  .trim()
  .refine(
    (valor) => valor === '' || /^\d{1,3}(\.\d{3})*,\d{2}$/.test(valor),
    'Informe um valor válido, como 1.500,00',
  )
  .refine(
    (valor) => valor === '' || (moedaParaNumero(valor) ?? 0) <= ORCAMENTO_MAXIMO,
    'O valor excede o máximo permitido',
  )
  .nullish()

export const tituloSchema = z
  .string({ required_error: 'Título obrigatório' })
  .trim()
  .min(1, 'Título obrigatório')
  .max(150, 'O título deve ter no máximo 150 caracteres')
  .refine(tituloEhValido, 'O título contém caracteres não permitidos')

// Opcionais: o campo vazio é válido, a regra só vale quando preenchido (igual ao backend).
export const prestadorSchema = z
  .string()
  .trim()
  .max(150, 'O nome do prestador deve ter no máximo 150 caracteres')
  .refine((valor) => valor === '' || prestadorEhValido(valor), 'O nome do prestador contém caracteres não permitidos')

export const detalhesSchema = z
  .string()
  .max(2000, 'Os detalhes devem ter no máximo 2000 caracteres')
  .refine(detalhesEhValido, 'Os detalhes contêm caracteres não permitidos')

// Valor cru do <input type="date"> ("2026-10-20").
const dataSchema = z
  .string({ required_error: 'Selecione a data', invalid_type_error: 'Selecione a data' })
  .regex(/^\d{4}-\d{2}-\d{2}$/, 'Selecione a data')

// Horário digitado ("18:30", com máscara) — hora 00–23 e minuto 00–59; segundos opcionais porque o
// backend devolve "18:30:00".
const horarioSchema = z
  .string({ required_error: 'Informe o horário', invalid_type_error: 'Informe o horário' })
  .min(1, 'Informe o horário')
  .regex(/^([01]\d|2[0-3]):[0-5]\d(:[0-5]\d)?$/, 'Horário inválido. Use o formato HH:mm')

// Espelha CompromissoFormDto — mesmo DTO em POST /compromissos e PUT /compromissos/{id}.
export const compromissoFormSchema = z.object({
  predioId: z
    .number({ required_error: 'Selecione o prédio', invalid_type_error: 'Selecione o prédio' })
    .int()
    .positive('Selecione o prédio'),
  titulo: tituloSchema,
  dataCompromisso: dataSchema,
  horarioCompromisso: horarioSchema,
  nomePrestadorServico: prestadorSchema.optional(),
  // Opcional. Fica como texto mascarado no formulário e vira número em `compromissoService`.
  orcamento: orcamentoSchema,
  detalhes: detalhesSchema.optional(),
  // Omitido = o backend usa o próprio autor (criar) ou mantém o atual (editar), RN20.
  responsavelUsuarioId: z.number().int().positive().nullish(),
})

// Espelha CompromissoDto. DateOnly/TimeOnly chegam como "2026-10-20" / "14:30:00".
export const compromissoSchema = z.object({
  id: z.number(),
  titulo: z.string(),
  dataCompromisso: z.string(),
  horarioCompromisso: z.string(),
  nomePrestadorServico: z.string().nullable(),
  detalhes: z.string().nullable(),
  orcamento: z.number().nullable(),
  concluido: z.boolean(),
  predioId: z.number(),
  predioNome: z.string(),
  responsavelUsuarioId: z.number(),
  responsavelNome: z.string(),
  criadoEm: z.string(),
})

// Espelha PagedResultDto<CompromissoDto> (RNF10).
export const compromissosPaginadosSchema = z.object({
  items: z.array(compromissoSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

// Filtros da listagem — valores aceitos por `escopo`/`situacao` em GET /compromissos.
export const escopoCompromissosSchema = z.enum(['meus', 'todos'])
export const situacaoCompromissosSchema = z.enum(['proximos', 'hoje', 'atrasados', 'concluidos', 'todos'])
