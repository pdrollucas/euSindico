import {
  createRouter,
  createWebHistory,
  type RouteLocationNormalizedLoaded,
  type RouteLocationRaw,
} from 'vue-router'
import { useAuthStore } from '@/stores/authStore'

// `voltarPara`: destino do botão "voltar" do AppHeader — nunca o histórico do navegador (ver
// ARCHITECTURE.md, seção 5).
declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    titulo?: string
    voltarPara?: (route: RouteLocationNormalizedLoaded) => RouteLocationRaw
  }
}

// Mapa de rotas conforme frontend/documentation/ARCHITECTURE.md, seção 5.
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
        {
          path: 'convite/:token',
          name: 'aceitar-convite',
          component: () => import('@/views/equipe/AceitarConviteView.vue'),
        },
      ],
    },
    {
      path: '/',
      component: () => import('@/layouts/AppLayout.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: 'home', name: 'home', component: () => import('@/views/home/HomeView.vue') },
        // Placeholders "em construção" dos módulos ainda não implementados — a Home (hub) já
        // linka para cá; a tela real substitui o EmConstrucaoView no marco correspondente.
        {
          path: 'compromissos',
          name: 'compromissos',
          meta: { titulo: 'Compromissos' },
          component: () => import('@/views/EmConstrucaoView.vue'),
        },
        {
          path: 'predios',
          name: 'predios',
          meta: { titulo: 'Prédios', voltarPara: () => '/home' },
          component: () => import('@/views/predios/PrediosListView.vue'),
        },
        {
          path: 'predios/:id',
          name: 'predios-detalhe',
          meta: { titulo: 'Prédio', voltarPara: () => '/predios' },
          component: () => import('@/views/predios/PredioDetalheView.vue'),
        },
        {
          path: 'predios/:id/equipe',
          name: 'predios-equipe',
          meta: {
            titulo: 'Equipe',
            voltarPara: (route) => `/predios/${route.params.id}`,
          },
          component: () => import('@/views/equipe/EquipePredioView.vue'),
        },
        {
          path: 'configuracoes',
          name: 'configuracoes',
          meta: { titulo: 'Configurações' },
          component: () => import('@/views/EmConstrucaoView.vue'),
        },
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
