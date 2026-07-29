describe('Recuperação de senha (RF06-A)', () => {
  // Fluxo público (esqueci-senha -> verificar-codigo -> redefinir-senha): rotas sem requiresAuth,
  // nenhum bootstrap de sessão dispara aqui (ver AUTHENTICATION.md, seção 4).

  it('percorre o fluxo completo: e-mail -> código -> nova senha -> login', () => {
    cy.intercept('POST', '**/auth/esqueci-senha', { statusCode: 204 }).as('esqueci')
    cy.intercept('POST', '**/auth/verificar-codigo', { statusCode: 204 }).as('verificar')
    cy.intercept('POST', '**/auth/redefinir-senha', { statusCode: 204 }).as('redefinir')

    cy.visit('/esqueci-senha')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=btn-enviar-codigo]').click()
    cy.wait('@esqueci')
    cy.url().should('include', '/verificar-codigo')

    // O e-mail informado aparece na tela do código; o reenvio começa desabilitado (cooldown ativo).
    cy.contains('sindico@exemplo.com').should('be.visible')
    cy.get('[data-cy=btn-reenviar]').should('be.disabled')

    cy.get('[data-cy=codigo]').type('AB12CD')
    cy.get('[data-cy=btn-verificar]').click()
    cy.wait('@verificar')
    cy.url().should('include', '/redefinir-senha')

    cy.get('[data-cy=nova-senha]').type('NovaSenha1!')
    cy.get('[data-cy=confirmar-senha]').type('NovaSenha1!')
    cy.get('[data-cy=btn-atualizar-senha]').click()
    cy.wait('@redefinir')
    cy.url().should('include', '/login')
  })

  it('exibe erro e permanece na tela quando o código é inválido (400)', () => {
    cy.intercept('POST', '**/auth/esqueci-senha', { statusCode: 204 }).as('esqueci')
    cy.intercept('POST', '**/auth/verificar-codigo', {
      statusCode: 400,
      body: { title: 'Código inválido', status: 400 },
    }).as('verificarFalho')

    cy.visit('/esqueci-senha')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=btn-enviar-codigo]').click()
    cy.wait('@esqueci')

    cy.get('[data-cy=codigo]').type('WRONG1')
    cy.get('[data-cy=btn-verificar]').click()
    cy.wait('@verificarFalho')
    cy.get('[data-cy=erro-verificar-codigo]').should('be.visible')
    cy.url().should('include', '/verificar-codigo')
  })

  it('redireciona para /esqueci-senha ao acessar /verificar-codigo direto (sem fluxo)', () => {
    cy.visit('/verificar-codigo')
    cy.url().should('include', '/esqueci-senha')
  })

  it('redireciona para /esqueci-senha ao acessar /redefinir-senha direto (sem fluxo)', () => {
    cy.visit('/redefinir-senha')
    cy.url().should('include', '/esqueci-senha')
  })

  it('mantém o cooldown e o fluxo após recarregar a página (F5)', () => {
    cy.intercept('POST', '**/auth/esqueci-senha', { statusCode: 204 }).as('esqueci')

    cy.visit('/esqueci-senha')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=btn-enviar-codigo]').click()
    cy.wait('@esqueci')
    cy.url().should('include', '/verificar-codigo')
    cy.get('[data-cy=btn-reenviar]').should('be.disabled')

    cy.reload()

    // Após o F5: continua na tela de código, com e-mail e cooldown preservados (sessionStorage) —
    // sem reset do timer nem chance de reenvio duplicado.
    cy.url().should('include', '/verificar-codigo')
    cy.contains('sindico@exemplo.com').should('be.visible')
    cy.get('[data-cy=btn-reenviar]').should('be.disabled')
  })

  it('reabilita o botão de reenviar quando o cooldown de 2 minutos zera', () => {
    cy.intercept('POST', '**/auth/esqueci-senha', { statusCode: 204 }).as('esqueci')
    // Relógio controlado só para o que a contagem usa (Date + setInterval) — deixar setTimeout e
    // requestAnimationFrame reais, senão o scheduler do Vue/Vuetify congela e o clique nunca
    // dispara a requisição ("no request ever occurred").
    cy.clock(Date.now(), ['Date', 'setInterval', 'clearInterval'])

    cy.visit('/esqueci-senha')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=btn-enviar-codigo]').click()
    cy.wait('@esqueci')
    cy.url().should('include', '/verificar-codigo')

    cy.get('[data-cy=btn-reenviar]').should('be.disabled')
    cy.tick(2 * 60 * 1000 + 1000)
    cy.get('[data-cy=btn-reenviar]').should('not.be.disabled')
  })
})
