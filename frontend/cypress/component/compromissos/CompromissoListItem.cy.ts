import { createMemoryHistory, createRouter } from 'vue-router'
import CompromissoListItem from '../../../src/components/compromissos/CompromissoListItem.vue'

const compromisso = {
  id: 11,
  titulo: 'Reunião',
  dataCompromisso: '2099-02-18',
  horarioCompromisso: '18:30:00',
  nomePrestadorServico: null,
  detalhes: null,
  orcamento: null,
  concluido: false,
  predioId: 1,
  predioNome: 'Edifício Solar',
  responsavelUsuarioId: 1,
  responsavelNome: 'Luciano Souza',
  criadoEm: '2026-01-01T12:00:00Z',
}

// O título é um <router-link> — precisa de um router montado (ver PredioCard.cy.ts).
function montar(props: Record<string, unknown> = {}) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/compromissos', component: { template: '<div>lista</div>' } },
      { path: '/compromissos/:id', component: { template: '<div>detalhe</div>' } },
    ],
  })
  cy.mount(CompromissoListItem, {
    props: { compromisso, ...props },
    global: { plugins: [router] },
  })
  return cy.wrap(router)
}

describe('CompromissoListItem', () => {
  it('exibe título, prédio, data e horário', () => {
    montar()
    cy.get('[data-cy=compromisso-item-11]').should('contain', 'Reunião')
    cy.get('[data-cy=compromisso-predio-11]').should('have.text', 'Edifício Solar')
    cy.get('[data-cy=compromisso-data-11]').should('contain', '18/02/2099 · 18:30')
  })

  it('mantém data e horário visíveis mesmo com nome de prédio muito longo', () => {
    montar({
      compromisso: {
        ...compromisso,
        predioNome: 'Condomínio Residencial Parque das Palmeiras Imperiais '.repeat(3),
      },
    })
    cy.get('[data-cy=compromisso-data-11]').should('be.visible').and('contain', '18:30')
    cy.get('[data-cy=compromisso-data-11]').then(($data) => {
      cy.get('[data-cy=compromisso-item-11]').then(($card) => {
        cy.wrap($data[0]!.getBoundingClientRect().right).should(
          'be.lessThan',
          $card[0]!.getBoundingClientRect().right,
        )
      })
    })
  })

  it('mostra o responsável só quando pedido (escopo "todos")', () => {
    montar()
    cy.get('[data-cy=compromisso-responsavel-11]').should('not.exist')
    montar({ mostrarResponsavel: true })
    cy.get('[data-cy=compromisso-responsavel-11]').should('contain', 'Luciano Souza')
  })

  it('navega para o detalhe ao clicar no título', () => {
    const router = montar()
    cy.get('[data-cy=compromisso-link-11]').click()
    router.its('currentRoute.value.fullPath').should('eq', '/compromissos/11')
  })

  it('mantém o filtro de prédio na URL do detalhe', () => {
    const router = montar({ predioIdFiltro: 1 })
    cy.get('[data-cy=compromisso-link-11]').click()
    router.its('currentRoute.value.fullPath').should('eq', '/compromissos/11?predioId=1')
  })

  it('emite "alternar" com o estado desejado ao marcar o checkbox', () => {
    montar({ onAlternar: cy.spy().as('onAlternar') })
    cy.get('[data-cy=compromisso-check-11] input').click()
    cy.get('@onAlternar').should('have.been.calledWith', true)
  })

  it('emite "alternar" com false ao desmarcar um compromisso concluído', () => {
    montar({
      compromisso: { ...compromisso, concluido: true },
      onAlternar: cy.spy().as('onAlternar'),
    })
    cy.get('[data-cy=compromisso-check-11] input').should('be.checked').click()
    cy.get('@onAlternar').should('have.been.calledWith', false)
  })

  it('mantém o checkbox colado na lixeira, no canto direito do card', () => {
    montar()
    cy.get('[data-cy=compromisso-check-11]').then(($check) => {
      cy.get('[data-cy=compromisso-remover-11]').then(($lixeira) => {
        const folga =
          $lixeira[0]!.getBoundingClientRect().left - $check[0]!.getBoundingClientRect().right
        cy.wrap(folga).should('be.lessThan', 8)
      })
    })
  })

  it('emite "remover" ao clicar na lixeira', () => {
    montar({ onRemover: cy.spy().as('onRemover') })
    cy.get('[data-cy=compromisso-remover-11]').click()
    cy.get('@onRemover').should('have.been.called')
  })

  it('no estado "concluindo" mostra o checkbox marcado e bloqueia novas ações', () => {
    montar({ estado: 'concluindo' })
    cy.get('[data-cy=compromisso-check-11] input').should('be.checked')
    cy.get('[data-cy=compromisso-remover-11]').should('be.disabled')
  })

  it('no estado "reabrindo" mostra o checkbox desmarcado mesmo sendo um item concluído', () => {
    montar({ compromisso: { ...compromisso, concluido: true }, estado: 'reabrindo' })
    cy.get('[data-cy=compromisso-check-11] input').should('not.be.checked')
  })
})
