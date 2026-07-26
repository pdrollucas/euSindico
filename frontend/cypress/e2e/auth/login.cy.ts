describe('Login', () => {
  // /login é rota pública — o bootstrap de sessão (POST /auth/refresh) só dispara para rotas
  // protegidas (ver AUTHENTICATION.md, seção 4), então nenhum intercept dele é necessário aqui.

  it('redireciona para /home após login válido', () => {
    cy.intercept('POST', '**/auth/login', { statusCode: 200, fixture: 'usuario.json' }).as('login')
    cy.visit('/login')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-entrar]').click()
    cy.wait('@login')
    cy.url().should('include', '/home')
  })

  it('exibe mensagem genérica em credenciais inválidas (RFC 3.2)', () => {
    cy.intercept('POST', '**/auth/login', {
      statusCode: 401,
      body: { title: 'Credenciais inválidas', status: 401 },
    }).as('loginFalho')
    cy.visit('/login')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaErrada1!')
    cy.get('[data-cy=btn-entrar]').click()
    cy.wait('@loginFalho')
    cy.get('[data-cy=erro-login]').should('be.visible')
    cy.url().should('include', '/login')
  })

  it('exibe mensagem genérica em erro inesperado (não-401, ex: 500)', () => {
    cy.intercept('POST', '**/auth/login', {
      statusCode: 500,
      body: { title: 'Erro interno', status: 500 },
    }).as('loginErro')
    cy.visit('/login')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-entrar]').click()
    cy.wait('@loginErro')
    cy.get('[data-cy=erro-login]').should('be.visible').and('contain', 'Não foi possível entrar')
    cy.url().should('include', '/login')
  })
})
