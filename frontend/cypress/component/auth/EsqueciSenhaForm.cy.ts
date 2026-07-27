import EsqueciSenhaForm from '../../../src/components/auth/EsqueciSenhaForm.vue'

describe('EsqueciSenhaForm', () => {
  it('emite "submit" com o e-mail preenchido', () => {
    cy.mount(EsqueciSenhaForm, { props: { onSubmit: cy.spy().as('onSubmit') } })
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=btn-enviar-codigo]').click()
    cy.get('@onSubmit').should('have.been.calledWith', { email: 'sindico@exemplo.com' })
  })

  it('exibe erro de validação com e-mail inválido', () => {
    cy.mount(EsqueciSenhaForm)
    cy.get('[data-cy=email]').type('nao-e-um-email')
    cy.get('[data-cy=btn-enviar-codigo]').click()
    cy.contains('E-mail inválido').should('be.visible')
  })

  it('desabilita o botão e mostra a contagem regressiva durante o cooldown', () => {
    cy.mount(EsqueciSenhaForm, { props: { cooldownSegundos: 125 } })
    cy.get('[data-cy=btn-enviar-codigo]').should('be.disabled').and('contain', '2:05')
  })

  it('pré-preenche o e-mail a partir da prop emailInicial', () => {
    cy.mount(EsqueciSenhaForm, { props: { emailInicial: 'joao@exemplo.com' } })
    // O data-cy fica no wrapper do v-text-field; o valor mora no <input> interno.
    cy.get('[data-cy=email] input').should('have.value', 'joao@exemplo.com')
  })
})
