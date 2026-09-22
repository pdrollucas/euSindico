import { ref } from 'vue'
import { defineStore } from 'pinia'
import { equipeService } from '@/services/equipeService'
import type { EquipeMembro, ConvidarFuncionarioRequest } from '@/types/equipe.types'

export const useEquipeStore = defineStore('equipe', () => {
  const membros = ref<EquipeMembro[]>([])

  async function listar(predioId: number) {
    membros.value = await equipeService.listar(predioId)
  }

  async function convidar(predioId: number, payload: ConvidarFuncionarioRequest) {
    await equipeService.convidar(predioId, payload)
  }

  async function remover(predioId: number, usuarioId: number) {
    await equipeService.remover(predioId, usuarioId)
    membros.value = membros.value.filter((membro) => membro.usuarioId !== usuarioId)
  }

  function limpar() {
    membros.value = []
  }

  return { membros, listar, convidar, remover, limpar }
})
