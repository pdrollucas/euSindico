describe('Landing e guarda de rotas', () => {
  it('exibe a landing page e navega para o login', () => {
    // "/" é rota pública — não dispara bootstrap de sessão, então nenhum intercept de
    // /auth/refresh é necessário aqui (ver AUTHENTICATION.md, seção 4).
    cy.visit('/')
    cy.get('[data-cy=btn-login]').click()
    cy.url().should('include', '/login')
  })

  it('navega para o cadastro pelo botão "Criar conta"', () => {
    // O CTA de cadastro é a ação principal da landing — uma regressão no roteamento dele
    // quebraria a conversão silenciosamente. "/" é pública, sem bootstrap de sessão.
    cy.visit('/')
    cy.get('[data-cy=btn-registrar]').click()
    cy.url().should('include', '/registrar')
  })

  it('redireciona para /login ao tentar acessar uma rota protegida sem sessão (RFC 3.2)', () => {
    // /home é protegida: a guarda de rota dispara o bootstrap na primeira visita — intercepta
    // para o teste não depender de um backend real.
    cy.intercept('POST', '**/auth/refresh', { statusCode: 401 })
    cy.visit('/home')
    cy.url().should('include', '/login')
  })
})
