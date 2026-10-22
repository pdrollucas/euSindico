import api from '@/plugins/axios'
import {
  planejamentoFormSchema,
  planejamentoSchema,
  planejamentosPaginadosSchema,
} from '@/schemas/planejamento.schema'
import { moedaParaNumero } from '@/utils/moeda'
import type {
  Planejamento,
  PlanejamentoFiltros,
  PlanejamentoFormValues,
  PlanejamentosPaginados,
} from '@/types/planejamento.types'

// As rotas são aninhadas no prédio (PLANEJAMENTOS.md) — não há listagem global.
const base = (predioId: number) => `/predios/${predioId}/planejamentos`

// Opcionais vazios viram null (o backend também normaliza, mas assim o corpo já sai limpo); o
// orçamento mascarado ("1.500,50") vira número.
function montarCorpo(payload: PlanejamentoFormValues) {
  const valores = planejamentoFormSchema.parse(payload)
  return {
    titulo: valores.titulo,
    dataPlanejamento: valores.dataPlanejamento || null,
    orcamentoPrevisto: moedaParaNumero(valores.orcamentoPrevisto),
    nomePrestadorServico: valores.nomePrestadorServico?.trim() || null,
    detalhes: valores.detalhes?.trim() || null,
  }
}

async function listar(
  predioId: number,
  filtros: PlanejamentoFiltros,
  page: number,
  pageSize: number,
): Promise<PlanejamentosPaginados> {
  const titulo = filtros.titulo.trim()
  const { data } = await api.get(base(predioId), {
    params: {
      page,
      pageSize,
      ...(titulo ? { titulo } : {}),
      ...(filtros.orcamentoMinimo !== null ? { orcamentoMinimo: filtros.orcamentoMinimo } : {}),
      ...(filtros.orcamentoMaximo !== null ? { orcamentoMaximo: filtros.orcamentoMaximo } : {}),
    },
  })
  return planejamentosPaginadosSchema.parse(data)
}

async function obter(predioId: number, id: number): Promise<Planejamento> {
  const { data } = await api.get(`${base(predioId)}/${id}`)
  return planejamentoSchema.parse(data)
}

async function criar(predioId: number, payload: PlanejamentoFormValues): Promise<Planejamento> {
  const { data } = await api.post(base(predioId), montarCorpo(payload))
  return planejamentoSchema.parse(data)
}

async function atualizar(
  predioId: number,
  id: number,
  payload: PlanejamentoFormValues,
): Promise<Planejamento> {
  const { data } = await api.put(`${base(predioId)}/${id}`, montarCorpo(payload))
  return planejamentoSchema.parse(data)
}

async function remover(predioId: number, id: number): Promise<void> {
  await api.delete(`${base(predioId)}/${id}`)
}

export const planejamentoService = {
  listar,
  obter,
  criar,
  atualizar,
  remover,
}
