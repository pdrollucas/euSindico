import api from '@/plugins/axios'
import { predioFormSchema, predioSchema, prediosPaginadosSchema } from '@/schemas/predio.schema'
import type { Predio, PredioFormValues, PrediosPaginados } from '@/types/predio.types'

async function listar(page: number, pageSize: number): Promise<PrediosPaginados> {
  const { data } = await api.get('/predios', { params: { page, pageSize } })
  return prediosPaginadosSchema.parse(data)
}

async function obter(id: number): Promise<Predio> {
  const { data } = await api.get(`/predios/${id}`)
  return predioSchema.parse(data)
}

async function criar(payload: PredioFormValues): Promise<Predio> {
  const body = predioFormSchema.parse(payload)
  const { data } = await api.post('/predios', body)
  return predioSchema.parse(data)
}

async function atualizar(id: number, payload: PredioFormValues): Promise<Predio> {
  const body = predioFormSchema.parse(payload)
  const { data } = await api.put(`/predios/${id}`, body)
  return predioSchema.parse(data)
}

async function remover(id: number): Promise<void> {
  await api.delete(`/predios/${id}`)
}

export const predioService = {
  listar,
  obter,
  criar,
  atualizar,
  remover,
}
