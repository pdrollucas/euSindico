import { z } from 'zod'
import { codigoSchema, emailSchema, nomeSchema, senhaForteSchema } from '@/schemas/auth.schema'

// Um schema por DTO trocado com /perfil/* e /suporte (PerfilController e SuporteController do
// backend) — usados nos formulários de Configurações e na validação antes da chamada ao service.
// `required_error` em todo campo pelo mesmo motivo de auth.schema.ts.

const senhaAtualSchema = z
  .string({ required_error: 'Senha obrigatória' })
  .min(1, 'Senha obrigatória')

export const atualizarPerfilRequestSchema = z.object({
  nome: nomeSchema,
})

// Passo 1 da troca de e-mail: o backend exige a senha atual e envia o código ao NOVO e-mail.
export const solicitarTrocaEmailRequestSchema = z.object({
  novoEmail: emailSchema,
  senhaAtual: senhaAtualSchema,
})

// Passo 2: só o código digitado (o mesmo formato de 6 caracteres da recuperação de senha).
export const confirmarTrocaEmailRequestSchema = z.object({
  codigo: codigoSchema,
})

const novaSenhaFields = {
  senhaAtual: senhaAtualSchema,
  novaSenha: senhaForteSchema,
}

export const alterarSenhaRequestSchema = z.object(novaSenhaFields)

// A confirmação existe só no formulário; o backend recebe apenas senha atual + nova senha.
export const alterarSenhaFormSchema = z
  .object({
    ...novaSenhaFields,
    confirmarSenha: z
      .string({ required_error: 'Confirmação obrigatória' })
      .min(1, 'Confirmação obrigatória'),
  })
  .refine((dto) => dto.novaSenha === dto.confirmarSenha, {
    message: 'As senhas não coincidem',
    path: ['confirmarSenha'],
  })

export const excluirContaRequestSchema = z.object({
  senha: senhaAtualSchema,
})

// Limites espelhando o EnviarSuporteDtoValidator do backend (assunto 3–100, mensagem 10–2000).
export const ASSUNTO_SUPORTE_MAX = 100
export const MENSAGEM_SUPORTE_MAX = 2000

export const enviarSuporteRequestSchema = z.object({
  assunto: z
    .string({ required_error: 'Assunto obrigatório' })
    .trim()
    .min(3, 'O assunto deve ter no mínimo 3 caracteres')
    .max(ASSUNTO_SUPORTE_MAX, `O assunto deve ter no máximo ${ASSUNTO_SUPORTE_MAX} caracteres`),
  mensagem: z
    .string({ required_error: 'Mensagem obrigatória' })
    .trim()
    .min(10, 'A mensagem deve ter no mínimo 10 caracteres')
    .max(MENSAGEM_SUPORTE_MAX, `A mensagem deve ter no máximo ${MENSAGEM_SUPORTE_MAX} caracteres`),
})
