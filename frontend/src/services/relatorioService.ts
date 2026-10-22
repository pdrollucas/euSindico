import api from '@/plugins/axios'
import {
  gerarRelatorioFormSchema,
  relatorioSchema,
  relatoriosPaginadosSchema,
} from '@/schemas/relatorio.schema'
import type {
  GerarRelatorioFormValues,
  Relatorio,
  RelatoriosPaginados,
} from '@/types/relatorio.types'

// As rotas são aninhadas no prédio (RELATORIOS.md) — não há listagem global.
const base = (predioId: number) => `/predios/${predioId}/relatorios`

async function listar(
  predioId: number,
  page: number,
  pageSize: number,
): Promise<RelatoriosPaginados> {
  const { data } = await api.get(base(predioId), { params: { page, pageSize } })
  return relatoriosPaginadosSchema.parse(data)
}

async function gerar(predioId: number, payload: GerarRelatorioFormValues): Promise<Relatorio> {
  const { mes, ano } = gerarRelatorioFormSchema.parse(payload)
  const { data } = await api.post(base(predioId), { mes, ano })
  return relatorioSchema.parse(data)
}

// O PDF só sai com o token (o bucket é privado): chega como Blob e a view decide se baixa ou abre.
// O tipo é forçado para `application/pdf` — é o que faz o navegador abrir o visualizador.
async function baixar(predioId: number, id: number): Promise<Blob> {
  const { data } = await api.get<Blob>(`${base(predioId)}/${id}/download`, { responseType: 'blob' })
  return new Blob([data], { type: 'application/pdf' })
}

async function remover(predioId: number, id: number): Promise<void> {
  await api.delete(`${base(predioId)}/${id}`)
}

export const relatorioService = {
  listar,
  gerar,
  baixar,
  remover,
}
