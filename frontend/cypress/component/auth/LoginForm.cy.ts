import LoginForm from '../../../src/components/auth/LoginForm.vue'

describe('LoginForm', () => {
  it('emite "submit" com email e senha preenchidos', () => {
    cy.mount(LoginForm, { props: { onSubmit: cy.spy().as('onSubmit') } })
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-entrar]').click()
    cy.get('@onSubmit').should('have.been.calledWith', {
      email: 'sindico@exemplo.com',
      senha: 'SenhaForte1!',
    })
  })

  it('exibe erro de validação com email inválido', () => {
    cy.mount(LoginForm)
    cy.get('[data-cy=email]').type('nao-e-um-email')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-entrar]').click()
    cy.contains('E-mail inválido').should('be.visible')
  })

  it('exibe erro de validação com senha vazia', () => {
    cy.mount(LoginForm)
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=btn-entrar]').click()
    cy.contains('Senha obrigatória').should('be.visible')
  })
})
