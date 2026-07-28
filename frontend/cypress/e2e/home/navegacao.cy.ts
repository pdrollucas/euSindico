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

  it('navega para o placeholder "em construção" ao clicar num card e volta para a Home', () => {
    cy.get('[data-cy=card-predios]').click()
    cy.url().should('include', '/predios')
    cy.get('[data-cy=em-construcao-titulo]').should('contain', 'Prédios')

    cy.get('[data-cy=link-home]').click()
    cy.url().should('include', '/home')
  })
})
