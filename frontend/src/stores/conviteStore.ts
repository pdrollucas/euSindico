import { ref } from 'vue'
import { defineStore } from 'pinia'
import { equipeService } from '@/services/equipeService'
import type { ConviteDetalhe, AceitarConviteRequest } from '@/types/equipe.types'

// Separado do equipeStore: domínio público (sem sessão), não o do Síndico autenticado.
export const useConviteStore = defineStore('convite', () => {
  const convite = ref<ConviteDetalhe | null>(null)

  async function obter(token: string): Promise<ConviteDetalhe> {
    convite.value = await equipeService.obterConvite(token)
    return convite.value
  }

  async function aceitar(token: string, payload: AceitarConviteRequest): Promise<void> {
    await equipeService.aceitarConvite(token, payload)
  }

  return { convite, obter, aceitar }
})
