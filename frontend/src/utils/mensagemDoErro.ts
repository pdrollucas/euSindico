import axios from 'axios'
import type { ErroApiDto } from '@/types/erroApi.types'

// Alguns erros do backend já trazem no `title` uma mensagem pronta para o usuário (ex: "A senha
// atual informada está incorreta.", "Aguarde alguns minutos antes de solicitar um novo código.").
// Para os status informados em `statusComTitulo` o frontend a mostra em vez de reescrevê-la; nos
// demais casos usa a mensagem `padrao`. Um 400 de validação (ValidationProblemDetails, com `errors`)
// nunca usa o `title`, que é um texto genérico em inglês — o formulário já valida as mesmas regras.
export function mensagemDoErro(
  error: unknown,
  padrao: string,
  statusComTitulo: readonly number[],
): string {
  if (!axios.isAxiosError<ErroApiDto & { errors?: unknown }>(error)) return padrao

  const resposta = error.response
  if (!resposta || !statusComTitulo.includes(resposta.status)) return padrao
  if (resposta.data?.errors) return padrao

  return resposta.data?.title || padrao
}
