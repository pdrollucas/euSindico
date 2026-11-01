import axios from 'axios'
import { ZodError } from 'zod'
import type { ErroApiDto } from '@/types/erroApi.types'

const GENERICA = 'Não foi possível enviar o arquivo. Tente novamente.'

// Mensagem do erro de envio de uma ata/norma (DOCUMENTOS.md). 400 (arquivo recusado pelo backend,
// ex: conteúdo que não bate com a extensão) e 409 (limite de quantidade, RN40/RN41) trazem no
// `title` uma mensagem já pronta para o usuário — o frontend a mostra em vez de reescrevê-la. O
// comportamento nunca depende do texto, só do status (ARCHITECTURE.md, seção 6).
export function mensagemErroEnvio(error: unknown): string {
  // O próprio schema barrou o arquivo antes da requisição.
  if (error instanceof ZodError) {
    return error.issues[0]?.message ?? GENERICA
  }

  if (!axios.isAxiosError<ErroApiDto>(error)) {
    return GENERICA
  }

  switch (error.response?.status) {
    case 400:
    case 409:
      return error.response.data?.title ?? GENERICA
    case 403:
      return 'Você não tem permissão para enviar arquivos neste prédio.'
    case 413:
      return 'O arquivo é grande demais. O tamanho máximo é de 20 MB.'
    case 429:
      return 'Muitos envios em pouco tempo. Aguarde um minuto e tente novamente.'
    default:
      return GENERICA
  }
}
