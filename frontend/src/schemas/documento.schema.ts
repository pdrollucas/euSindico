import { z } from 'zod'
import { validarArquivoDocumento } from '@/utils/arquivoDocumento'

// Formulário de envio: um arquivo, validado pelas mesmas regras do backend (RN12/RN38) para dar
// feedback sem ida e volta. O backend é a fonte da verdade — confere também o conteúdo.
export const enviarDocumentoFormSchema = z.object({
  arquivo: z.instanceof(File, { message: 'Selecione um arquivo' }).superRefine((arquivo, ctx) => {
    const erro = validarArquivoDocumento(arquivo)
    if (erro) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, message: erro })
    }
  }),
})

// Espelha DocumentoDto. `criadoEm` chega como ISO do servidor (UTC, às vezes sem o "Z"). A chave do
// objeto no S3 não é exposta pela API.
export const documentoSchema = z.object({
  id: z.number(),
  nomeArquivo: z.string(),
  predioId: z.number(),
  predioNome: z.string(),
  criadoEm: z.string(),
})

// Espelha PagedResultDto<DocumentoDto> (RNF10).
export const documentosPaginadosSchema = z.object({
  items: z.array(documentoSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})
