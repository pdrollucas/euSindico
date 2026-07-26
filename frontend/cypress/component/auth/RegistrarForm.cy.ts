import RegistrarForm from '../../../src/components/auth/RegistrarForm.vue'

describe('RegistrarForm', () => {
  it('emite "submit" com nome, email e senha preenchidos', () => {
    cy.mount(RegistrarForm, { props: { onSubmit: cy.spy().as('onSubmit') } })
    cy.get('[data-cy=nome]').type('João da Silva')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-registrar]').click()
    cy.get('@onSubmit').should('have.been.calledWith', {
      nome: 'João da Silva',
      email: 'sindico@exemplo.com',
      senha: 'SenhaForte1!',
    })
  })

  it('exibe erro de validação com nome contendo caracteres não permitidos', () => {
    cy.mount(RegistrarForm)
    cy.get('[data-cy=nome]').type('João123')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-registrar]').click()
    cy.contains('Nome deve conter apenas letras, espaços, hífen e apóstrofo').should('be.visible')
  })

  it('exibe erro de validação com senha fraca (RNF04)', () => {
    cy.mount(RegistrarForm)
    cy.get('[data-cy=nome]').type('João da Silva')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('fraca')
    cy.get('[data-cy=btn-registrar]').click()
    cy.contains('A senha deve ter no mínimo 8 caracteres').should('be.visible')
  })
})
