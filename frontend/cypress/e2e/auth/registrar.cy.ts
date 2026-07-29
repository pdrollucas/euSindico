describe('Registrar', () => {
  // /registrar é rota pública — ver mesmo raciocínio em cypress/e2e/auth/login.cy.ts.

  it('redireciona para /login após cadastro válido (RFC 4.1: cadastro OK -> login)', () => {
    cy.intercept('POST', '**/auth/registrar', {
      statusCode: 201,
      fixture: 'usuario-registrado.json',
    }).as('registrar')
    cy.visit('/registrar')
    cy.get('[data-cy=nome]').type('João da Silva')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-registrar]').click()
    cy.wait('@registrar')
    cy.url().should('include', '/login')
  })

  it('exibe mensagem de e-mail já cadastrado em conflito (409)', () => {
    cy.intercept('POST', '**/auth/registrar', {
      statusCode: 409,
      body: { title: 'E-mail já cadastrado', status: 409 },
    }).as('registrarConflito')
    cy.visit('/registrar')
    cy.get('[data-cy=nome]').type('João da Silva')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-registrar]').click()
    cy.wait('@registrarConflito')
    cy.get('[data-cy=erro-registrar]').should('be.visible').and('contain', 'já está cadastrado')
    cy.url().should('include', '/registrar')
  })

  it('exibe mensagem genérica em erro inesperado (não-409, ex: 500)', () => {
    cy.intercept('POST', '**/auth/registrar', {
      statusCode: 500,
      body: { title: 'Erro interno', status: 500 },
    }).as('registrarErro')
    cy.visit('/registrar')
    cy.get('[data-cy=nome]').type('João da Silva')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-registrar]').click()
    cy.wait('@registrarErro')
    cy.get('[data-cy=erro-registrar]')
      .should('be.visible')
      .and('contain', 'Não foi possível criar a conta')
    cy.url().should('include', '/registrar')
  })
})
