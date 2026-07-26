describe('Logout', () => {
  it('desloga e redireciona para /login', () => {
    cy.intercept('POST', '**/auth/login', { statusCode: 200, fixture: 'usuario.json' }).as('login')
    cy.intercept('POST', '**/auth/logout', { statusCode: 204 }).as('logout')

    cy.visit('/login')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-entrar]').click()
    cy.wait('@login')
    cy.url().should('include', '/home')

    cy.get('[data-cy=btn-logout]').click()
    cy.wait('@logout')
    cy.url().should('include', '/login')
  })

  it('desloga mesmo se a chamada ao backend falhar (AUTHENTICATION.md, seção 6)', () => {
    cy.intercept('POST', '**/auth/login', { statusCode: 200, fixture: 'usuario.json' }).as('login')
    cy.intercept('POST', '**/auth/logout', { forceNetworkError: true }).as('logoutFalho')

    cy.visit('/login')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-entrar]').click()
    cy.wait('@login')
    cy.url().should('include', '/home')

    cy.get('[data-cy=btn-logout]').click()
    cy.wait('@logoutFalho')
    cy.url().should('include', '/login')
  })
})
