import { z } from 'zod'
import api from '@/plugins/axios'
import {
  convidarFuncionarioRequestSchema,
  equipeMembroSchema,
  conviteDetalheSchema,
} from '@/schemas/equipe.schema'
import type {
  ConvidarFuncionarioRequest,
  EquipeMembro,
  ConviteDetalhe,
  AceitarConviteRequest,
} from '@/types/equipe.types'

async function convidar(predioId: number, payload: ConvidarFuncionarioRequest): Promise<void> {
  const body = convidarFuncionarioRequestSchema.parse(payload)
  await api.post(`/predios/${predioId}/convites`, body)
}

async function listar(predioId: number): Promise<EquipeMembro[]> {
  const { data } = await api.get(`/predios/${predioId}/equipe`)
  return z.array(equipeMembroSchema).parse(data)
}

async function remover(predioId: number, usuarioId: number): Promise<void> {
  await api.delete(`/predios/${predioId}/equipe/${usuarioId}`)
}

// Público — sem sessão, sem Authorization.
async function obterConvite(token: string): Promise<ConviteDetalhe> {
  const { data } = await api.get(`/convites/${token}`)
  return conviteDetalheSchema.parse(data)
}

async function aceitarConvite(token: string, payload: AceitarConviteRequest): Promise<void> {
  await api.post(`/convites/${token}/aceitar`, payload)
}

export const equipeService = {
  convidar,
  listar,
  remover,
  obterConvite,
  aceitarConvite,
}
