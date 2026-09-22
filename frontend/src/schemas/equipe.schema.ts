import { z } from 'zod'
import { senhaEhForte } from '@/utils/senhaValidator'
import { nomeEhValido } from '@/utils/nomeValidator'
import { papelPredioSchema } from '@/schemas/predio.schema'

// Convite nunca cria outro Síndico — ConvidarFuncionarioDtoValidator rejeita PapelPredio.Sindico.
export const papelConvidavelSchema = z.union([z.literal(2), z.literal(3)])

export const convidarFuncionarioRequestSchema = z.object({
  email: z.string({ required_error: 'E-mail obrigatório' }).email('E-mail inválido'),
  papel: papelConvidavelSchema,
})

// Espelha EquipeMembroDto.
export const equipeMembroSchema = z.object({
  usuarioId: z.number(),
  nome: z.string(),
  email: z.string(),
  papel: papelPredioSchema,
  criadoEm: z.string(),
})

// Espelha ConviteDetalheDto.
export const conviteDetalheSchema = z.object({
  predioNome: z.string(),
  papel: papelPredioSchema,
  contaJaExiste: z.boolean(),
  expiraEm: z.string(),
})

// AceitarConviteDto só exige Nome/Senha quando o e-mail do convite ainda não tem conta
// (contaJaExiste = false) — mesmas regras de RegistrarView (RNF04), ver schemas/auth.schema.ts.
export const aceitarConviteNovaContaFormSchema = z.object({
  nome: z
    .string({ required_error: 'Nome obrigatório' })
    .min(1, 'Nome obrigatório')
    .refine(nomeEhValido, 'Nome deve conter apenas letras, espaços, hífen e apóstrofo'),
  senha: z
    .string({ required_error: 'Senha obrigatória' })
    .min(8, 'A senha deve ter no mínimo 8 caracteres')
    .refine(senhaEhForte, 'A senha deve conter maiúscula, minúscula, número e caractere especial'),
})
