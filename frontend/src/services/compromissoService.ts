import api from '@/plugins/axios'
import {
  compromissoFormSchema,
  compromissoSchema,
  compromissosPaginadosSchema,
} from '@/schemas/compromisso.schema'
import type {
  Compromisso,
  CompromissoFiltros,
  CompromissoFormValues,
  CompromissosPaginados,
} from '@/types/compromisso.types'

// <input type="time"> entrega "HH:mm"; o backend (TimeOnly) recebe "HH:mm:ss". Opcionais vazios
// viram null — o backend também normaliza, mas assim o corpo já sai limpo.
function montarCorpo(payload: CompromissoFormValues) {
  const valores = compromissoFormSchema.parse(payload)
  return {
    ...valores,
    horarioCompromisso:
      valores.horarioCompromisso.length === 5
        ? `${valores.horarioCompromisso}:00`
        : valores.horarioCompromisso,
    nomePrestadorServico: valores.nomePrestadorServico?.trim() || null,
    detalhes: valores.detalhes?.trim() || null,
    responsavelUsuarioId: valores.responsavelUsuarioId ?? null,
  }
}

async function listar(
  filtros: CompromissoFiltros,
  page: number,
  pageSize: number,
): Promise<CompromissosPaginados> {
  const titulo = filtros.titulo.trim()
  const { data } = await api.get('/compromissos', {
    params: {
      page,
      pageSize,
      situacao: filtros.situacao,
      escopo: filtros.escopo,
      ...(titulo ? { titulo } : {}),
      ...(filtros.predioId ? { predioId: filtros.predioId } : {}),
    },
  })
  return compromissosPaginadosSchema.parse(data)
}

async function obter(id: number): Promise<Compromisso> {
  const { data } = await api.get(`/compromissos/${id}`)
  return compromissoSchema.parse(data)
}

async function criar(payload: CompromissoFormValues): Promise<Compromisso> {
  const { data } = await api.post('/compromissos', montarCorpo(payload))
  return compromissoSchema.parse(data)
}

async function atualizar(id: number, payload: CompromissoFormValues): Promise<Compromisso> {
  const { data } = await api.put(`/compromissos/${id}`, montarCorpo(payload))
  return compromissoSchema.parse(data)
}

// PATCH parcial e idempotente (COMPROMISSOS.md) — o checkbox só informa o estado desejado.
async function alterarConclusao(id: number, concluido: boolean): Promise<Compromisso> {
  const { data } = await api.patch(`/compromissos/${id}`, { concluido })
  return compromissoSchema.parse(data)
}

async function remover(id: number): Promise<void> {
  await api.delete(`/compromissos/${id}`)
}

export const compromissoService = {
  listar,
  obter,
  criar,
  atualizar,
  alterarConclusao,
  remover,
}
