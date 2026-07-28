import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { perfilService } from '@/services/perfilService'
import type { Usuario } from '@/types/usuario.types'

// Perfil do usuário autenticado (RF04). Carregado sob demanda (ex: na Home, para a saudação) e
// limpo no logout. Não é persistido — some num F5 e é recarregado quando necessário.
export const usePerfilStore = defineStore('perfil', () => {
  const perfil = ref<Usuario | null>(null)

  // Primeiro nome, para a saudação no cabeçalho da Home ("Olá, Luciano").
  const primeiroNome = computed(() => perfil.value?.nome.split(' ')[0] ?? null)

  async function carregar() {
    perfil.value = await perfilService.obterPerfil()
  }

  function limpar() {
    perfil.value = null
  }

  return { perfil, primeiroNome, carregar, limpar }
})
