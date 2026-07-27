import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

// Mapa de rotas conforme frontend/documentation/ARCHITECTURE.md, seção 5. Views de autenticação
// já implementadas: Landing, Login, Registrar, o fluxo de recuperação de senha (EsqueciSenha ->
// VerificarCodigo -> RedefinirSenha, RF06-A) e Home. Os demais módulos (Prédios, Compromissos,
// Planejamentos, Documentos, Relatórios, restante de Perfil) entram nos próximos marcos do RFC.
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: () => import('@/layouts/LandingLayout.vue'),
      children: [{ path: '', name: 'landing', component: () => import('@/views/landing/LandingView.vue') }],
    },
    {
      path: '/',
      component: () => import('@/layouts/AuthLayout.vue'),
      children: [
        { path: 'login', name: 'login', component: () => import('@/views/auth/LoginView.vue') },
        {
          path: 'registrar',
          name: 'registrar',
          component: () => import('@/views/auth/RegistrarView.vue'),
        },
        {
          path: 'esqueci-senha',
          name: 'esqueci-senha',
          component: () => import('@/views/auth/EsqueciSenhaView.vue'),
        },
        {
          path: 'verificar-codigo',
          name: 'verificar-codigo',
          component: () => import('@/views/auth/VerificarCodigoView.vue'),
        },
        {
          path: 'redefinir-senha',
          name: 'redefinir-senha',
          component: () => import('@/views/auth/RedefinirSenhaView.vue'),
        },
      ],
    },
    {
      path: '/',
      component: () => import('@/layouts/AppLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: 'home', name: 'home', component: () => import('@/views/home/HomeView.vue') },
      ],
    },
  ],
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()

  if (!to.meta.requiresAuth) {
    return
  }

  // Bootstrap de sessão (F5/deep link) só acontece aqui, na primeira vez que uma rota protegida
  // é visitada — nunca em rotas públicas (landing, login, registrar) — ver AUTHENTICATION.md,
  // seção 4.
  if (!authStore.isAuthenticated && !authStore.bootstrapped) {
    await authStore.bootstrap()
  }

  if (!authStore.isAuthenticated) {
    // Nunca redireciona para "/" — quem tenta acessar uma rota protegida já demonstrou intenção
    // de entrar no sistema, não de conhecer o produto (ver AUTHENTICATION.md/ARCHITECTURE.md, seção 5).
    return { name: 'login' }
  }
})

export default router
