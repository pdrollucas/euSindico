import { defineStore } from 'pinia'
import { suporteService } from '@/services/suporteService'
import type { EnviarSuporteRequest } from '@/types/perfil.types'

// Contato com o suporte (RF35). Não guarda estado — a mensagem vira um e-mail e não volta para a
// tela —, mas passa pelo store para manter o fluxo view -> store -> service (mesmo caso de
// authStore.registrar; ver frontend/documentation/ARCHITECTURE.md, seção 3).
export const useSuporteStore = defineStore('suporte', () => {
  async function enviar(payload: EnviarSuporteRequest) {
    await suporteService.enviar(payload)
  }

  return { enviar }
})
