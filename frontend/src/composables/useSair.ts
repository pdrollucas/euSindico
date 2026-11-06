import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { usePerfilStore } from '@/stores/perfilStore'

// Logout compartilhado pelo FAB da Home (celular) e pelo rodapé do navigation rail (desktop).
export function useSair() {
  const router = useRouter()
  const authStore = useAuthStore()
  const perfilStore = usePerfilStore()

  async function sair() {
    await authStore.logout()
    perfilStore.limpar()
    router.push('/login')
  }

  return { sair }
}
