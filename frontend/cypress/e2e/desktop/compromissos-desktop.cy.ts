// Compromissos no desktop: ação no cabeçalho, filtros na barra e tabela.
function entrar() {
  cy.intercept('POST', '**/auth/login', { statusCode: 200, fixture: 'usuario.json' }).as('login')
  cy.intercept('GET', '**/perfil', { statusCode: 200, fixture: 'perfil.json' }).as('perfil')
  cy.intercept('GET', '**/predios?*', { statusCode: 200, fixture: 'predios.json' }).as('predios')
  cy.intercept('GET', '**/compromissos?*', { statusCode: 200, fixture: 'compromissos.json' }).as(
    'listar',
  )

  cy.visit('/login')
  cy.get('[data-cy=email]').type('sindico@exemplo.com')
  cy.get('[data-cy=senha]').type('SenhaForte1!')
  cy.get('[data-cy=btn-entrar]').click()
  cy.wait('@login')
  cy.url().should('match', /\/compromissos$/)
  cy.wait('@listar')
}

describe('Compromissos no desktop', () => {
  beforeEach(() => {
    cy.viewport(1280, 800)
    entrar()
  })

  it('mostra a tabela, os filtros na barra e a ação no cabeçalho, sem FAB nem folha', () => {
    cy.get('[data-cy=compromissos-tabela]').should('be.visible')
    cy.get('[data-cy=compromisso-item-11]')
      .should('contain', 'Reunião')
      .and('contain', 'Edifício Solar')
    cy.get('[data-cy=filtros-barra]').should('be.visible')
    cy.get('[data-cy=btn-filtros]').should('not.exist')
    cy.get('.app-header [data-cy=btn-criar-compromisso]').should('contain', 'Novo compromisso')
    cy.get('.compromissos-fab').should('not.exist')
  })

  it('clicar na linha abre o detalhe', () => {
    cy.intercept('GET', '**/compromissos/12', { statusCode: 200, fixture: 'compromisso.json' }).as(
      'detalhe',
    )
    cy.intercept('GET', '**/predios/2/equipe', { statusCode: 200, fixture: 'equipe.json' })

    cy.get('[data-cy=compromisso-predio-12]').click()
    cy.wait('@detalhe')
    cy.url().should('include', '/compromissos/12')
    cy.get('[data-cy=titulo] input').should('have.value', 'Conserto de portão')
  })

  it('filtra pela barra: situação e escopo mudam a consulta, e "Limpar filtros" volta ao padrão', () => {
    cy.get('[data-cy=filtros-limpar]').should('be.disabled')

    cy.get('[data-cy=filtro-situacao]').click()
    cy.get('.v-overlay--active .v-list-item').contains('Concluídos').click()
    cy.wait('@listar').its('request.url').should('include', 'situacao=concluidos')

    cy.get('[data-cy=filtro-escopo-todos]').click()
    cy.wait('@listar').its('request.url').should('include', 'escopo=todos')
    // Com "todos", a tabela ganha a coluna de responsável.
    cy.get('[data-cy=compromisso-responsavel-11]').should('be.visible')

    cy.get('[data-cy=filtros-limpar]').click()
    cy.wait('@listar')
      .its('request.url')
      .should('include', 'situacao=proximos')
      .and('include', 'escopo=meus')
  })

  it('"Novo compromisso" do cabeçalho abre o formulário', () => {
    cy.get('[data-cy=btn-criar-compromisso]').click()
    cy.url().should('include', '/compromissos/novo')
  })
})
