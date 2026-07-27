import { z } from 'zod'
import { senhaEhForte } from '@/utils/senhaValidator'
import { nomeEhValido } from '@/utils/nomeValidator'

// Um schema por DTO trocado com /auth/* (AuthController do backend) — usados tanto para validar
// a resposta da API quanto os formulários (VeeValidate), ver frontend/documentation/ARCHITECTURE.md.
//
// `required_error` é obrigatório em todo campo aqui: sem ele, um campo nunca preenchido chega
// como `undefined` (não `''`) ao schema, e o Zod dispara sua mensagem genérica padrão ("Required")
// *antes* de qualquer `.min()`/`.length()` rodar — a mensagem customizada nunca seria usada.

const emailSchema = z
  .string({ required_error: 'E-mail obrigatório' })
  .email('E-mail inválido')

const senhaForteSchema = z
  .string({ required_error: 'Senha obrigatória' })
  .min(8, 'A senha deve ter no mínimo 8 caracteres')
  .refine(senhaEhForte, 'A senha deve conter maiúscula, minúscula, número e caractere especial')

const nomeSchema = z
  .string({ required_error: 'Nome obrigatório' })
  .min(1, 'Nome obrigatório')
  .refine(nomeEhValido, 'Nome deve conter apenas letras, espaços, hífen e apóstrofo')

export const loginRequestSchema = z.object({
  email: emailSchema,
  senha: z.string({ required_error: 'Senha obrigatória' }).min(1, 'Senha obrigatória'),
})

export const accessTokenResponseSchema = z.object({
  accessToken: z.string(),
})

export const registrarRequestSchema = z.object({
  nome: nomeSchema,
  email: emailSchema,
  senha: senhaForteSchema,
})

export const esqueciSenhaRequestSchema = z.object({
  email: emailSchema,
})

const codigoSchema = z
  .string({ required_error: 'Código obrigatório' })
  .length(6, 'O código tem 6 caracteres')

// Campos de nova senha + confirmação, compartilhados entre o schema de formulário
// (`redefinirSenhaFormSchema`, só os dois campos) e o de requisição (`redefinirSenhaRequestSchema`,
// que ainda leva email + codigo vindos do fluxo). A regra de "coincidem" é a mesma nos dois.
const novaSenhaFields = {
  novaSenha: senhaForteSchema,
  confirmarSenha: z
    .string({ required_error: 'Confirmação obrigatória' })
    .min(1, 'Confirmação obrigatória'),
}
const senhasCoincidem = (dto: { novaSenha: string; confirmarSenha: string }) =>
  dto.novaSenha === dto.confirmarSenha
const erroSenhasNaoCoincidem = {
  message: 'As senhas não coincidem',
  path: ['confirmarSenha'] as ['confirmarSenha'],
}

export const verificarCodigoRequestSchema = z.object({
  email: emailSchema,
  codigo: codigoSchema,
})

// Schema do formulário de "inserir código" — só o campo que o usuário digita; o e-mail vem
// da store do fluxo de recuperação (ver stores/recuperacaoSenhaStore.ts).
export const verificarCodigoFormSchema = z.object({
  codigo: codigoSchema,
})

export const redefinirSenhaRequestSchema = z
  .object({
    email: emailSchema,
    codigo: codigoSchema,
    ...novaSenhaFields,
  })
  .refine(senhasCoincidem, erroSenhasNaoCoincidem)

// Schema do formulário de "atualizar senha" — só os dois campos de senha; email + codigo vêm
// da store do fluxo (adicionados na chamada ao service).
export const redefinirSenhaFormSchema = z
  .object(novaSenhaFields)
  .refine(senhasCoincidem, erroSenhasNaoCoincidem)
