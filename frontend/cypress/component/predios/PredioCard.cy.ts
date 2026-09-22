import { createMemoryHistory, createRouter } from 'vue-router'
import PredioCard from '../../../src/components/predios/PredioCard.vue'

const predio = {
  id: 1,
  nome: 'Edifício Solar',
  endereco: 'Rua da Silva, 192, Floresta',
  criadoEm: '2026-01-01T12:00:00Z',
  papel: 1 as const,
}

// `:to` do v-card precisa de um router de verdade montado (não vem por padrão em cy.mount, ver
// cypress/support/component.ts) — sem ele o clique não navega.
function montarComRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/predios', component: { template: '<div>lista</div>' } },
      { path: '/predios/:id', component: { template: '<div>detalhe</div>' } },
    ],
  })
  cy.mount(PredioCard, { props: { predio }, global: { plugins: [router] } })
  return cy.wrap(router)
}

describe('PredioCard', () => {
  it('exibe nome e endereço do prédio', () => {
    montarComRouter()
    cy.get('[data-cy=predio-card-1]').should('contain', 'Edifício Solar')
    cy.get('[data-cy=predio-card-1]').should('contain', 'Rua da Silva, 192, Floresta')
  })

  it('navega para /predios/:id ao clicar no card', () => {
    const router = montarComRouter()
    cy.get('[data-cy=predio-card-1]').click()
    router.its('currentRoute.value.fullPath').should('eq', '/predios/1')
  })
})
