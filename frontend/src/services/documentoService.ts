import api from '@/plugins/axios'
import {
  documentoSchema,
  documentosPaginadosSchema,
  enviarDocumentoFormSchema,
} from '@/schemas/documento.schema'
import type { Documento, DocumentosPaginados, TipoDocumento } from '@/types/documento.types'
import { contentTypeDe } from '@/utils/arquivoDocumento'

// As rotas são aninhadas no prédio e separadas por tipo (DOCUMENTOS.md) — não há listagem global
// nem rota única com `?tipo=`.
const base = (tipo: TipoDocumento, predioId: number) => `/predios/${predioId}/${tipo}`

async function listar(
  tipo: TipoDocumento,
  predioId: number,
  nome: string,
  page: number,
  pageSize: number,
): Promise<DocumentosPaginados> {
  const params: Record<string, string | number> = { page, pageSize }
  const busca = nome.trim()
  if (busca) {
    params.nome = busca
  }
  const { data } = await api.get(base(tipo, predioId), { params })
  return documentosPaginadosSchema.parse(data)
}

// multipart/form-data com o campo `arquivo` (nome do parâmetro `IFormFile` no backend). Sem definir
// `Content-Type`: o navegador monta o cabeçalho com o boundary.
async function enviar(tipo: TipoDocumento, predioId: number, arquivo: File): Promise<Documento> {
  const { arquivo: valido } = enviarDocumentoFormSchema.parse({ arquivo })
  const formulario = new FormData()
  formulario.append('arquivo', valido)
  const { data } = await api.post(base(tipo, predioId), formulario)
  return documentoSchema.parse(data)
}

// O arquivo só sai com o token (o bucket é privado): chega como Blob e a view decide se baixa ou
// abre. O tipo vem da extensão do nome, não da resposta — é o que decide como o navegador o trata.
async function baixar(
  tipo: TipoDocumento,
  predioId: number,
  id: number,
  nomeArquivo: string,
): Promise<Blob> {
  const { data } = await api.get<Blob>(`${base(tipo, predioId)}/${id}/download`, {
    responseType: 'blob',
  })
  return new Blob([data], { type: contentTypeDe(nomeArquivo) })
}

async function remover(tipo: TipoDocumento, predioId: number, id: number): Promise<void> {
  await api.delete(`${base(tipo, predioId)}/${id}`)
}

export const documentoService = {
  listar,
  enviar,
  baixar,
  remover,
}
