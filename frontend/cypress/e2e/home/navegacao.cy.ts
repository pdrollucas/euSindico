describe('Home (hub da área logada)', () => {
  beforeEach(() => {
    cy.intercept('POST', '**/auth/login', { statusCode: 200, fixture: 'usuario.json' }).as('login')
    cy.intercept('GET', '**/perfil', { statusCode: 200, fixture: 'perfil.json' }).as('perfil')

    cy.visit('/login')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-entrar]').click()
    cy.wait('@login')
    cy.url().should('include', '/home')
  })

  it('exibe o primeiro nome do usuário (GET /perfil) e os cards do hub', () => {
    cy.wait('@perfil')
    cy.get('[data-cy=home-usuario]').should('contain', 'Luciano')
    cy.get('[data-cy=card-compromissos]').should('be.visible')
    cy.get('[data-cy=card-predios]').should('be.visible')
    cy.get('[data-cy=card-configuracoes]').should('be.visible')
  })

  it('navega para o placeholder "em construção" ao clicar num card ainda não implementado e volta para a Home', () => {
    cy.get('[data-cy=card-compromissos]').click()
    cy.url().should('include', '/compromissos')
    cy.get('[data-cy=em-construcao-titulo]').should('contain', 'Compromissos')

    cy.get('[data-cy=link-home]').click()
    cy.url().should('include', '/home')
  })

  it('navega para a lista de prédios ao clicar no card (módulo Prédios, RF08-RF11)', () => {
    cy.intercept('GET', '**/predios?*', {
      statusCode: 200,
      body: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
    }).as('listarPredios')

    cy.get('[data-cy=card-predios]').click()
    cy.wait('@listarPredios')
    cy.url().should('include', '/predios')

    cy.get('[data-cy=btn-home]').click()
    cy.url().should('include', '/home')
  })
})
