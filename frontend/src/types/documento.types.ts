import { z } from 'zod'
import type {
  documentoSchema,
  documentosPaginadosSchema,
  enviarDocumentoFormSchema,
} from '@/schemas/documento.schema'

// Segmento da rota do backend: `/predios/{id}/atas` e `/predios/{id}/normas` (DOCUMENTOS.md). É
// também o identificador do tipo em todo o frontend (rotas, store, service).
export type TipoDocumento = 'atas' | 'normas'

export type EnviarDocumentoFormValues = z.infer<typeof enviarDocumentoFormSchema>
export type Documento = z.infer<typeof documentoSchema>
export type DocumentosPaginados = z.infer<typeof documentosPaginadosSchema>
