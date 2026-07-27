import VerificarCodigoForm from '../../../src/components/auth/VerificarCodigoForm.vue'

describe('VerificarCodigoForm', () => {
  it('emite "submit" com o código preenchido', () => {
    cy.mount(VerificarCodigoForm, { props: { onSubmit: cy.spy().as('onSubmit') } })
    cy.get('[data-cy=codigo]').type('AB12CD')
    cy.get('[data-cy=btn-verificar]').click()
    cy.get('@onSubmit').should('have.been.calledWith', { codigo: 'AB12CD' })
  })

  it('exibe erro de validação com código de tamanho diferente de 6', () => {
    cy.mount(VerificarCodigoForm)
    cy.get('[data-cy=codigo]').type('123')
    cy.get('[data-cy=btn-verificar]').click()
    cy.contains('O código tem 6 caracteres').should('be.visible')
  })
})
