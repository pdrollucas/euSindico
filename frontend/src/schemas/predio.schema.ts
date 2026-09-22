import { z } from 'zod'
import { predioNomeEhValido, enderecoEhValido } from '@/utils/predioValidator'

const nomeSchema = z
  .string({ required_error: 'Nome obrigatório' })
  .min(1, 'Nome obrigatório')
  .max(150, 'O nome deve ter no máximo 150 caracteres')
  .refine(predioNomeEhValido, 'O nome contém caracteres não permitidos')

const enderecoSchema = z
  .string({ required_error: 'Endereço obrigatório' })
  .min(1, 'Endereço obrigatório')
  .max(255, 'O endereço deve ter no máximo 255 caracteres')
  .refine(enderecoEhValido, 'O endereço contém caracteres não permitidos')

// Espelha PredioFormDto — mesmo DTO em POST /predios e PUT /predios/{id}.
export const predioFormSchema = z.object({
  nome: nomeSchema,
  endereco: enderecoSchema,
})

// PapelPredio serializa como número (sem JsonStringEnumConverter no backend): 1 Síndico,
// 2 Gestor, 3 Colaborador.
export const papelPredioSchema = z.union([z.literal(1), z.literal(2), z.literal(3)])

// Espelha PredioDto do backend.
export const predioSchema = z.object({
  id: z.number(),
  nome: z.string(),
  endereco: z.string(),
  criadoEm: z.string(),
  papel: papelPredioSchema,
})

// Espelha PagedResultDto<PredioDto> (RNF10).
export const prediosPaginadosSchema = z.object({
  items: z.array(predioSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})
