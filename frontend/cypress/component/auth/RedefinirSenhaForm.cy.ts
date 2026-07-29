import RedefinirSenhaForm from '../../../src/components/auth/RedefinirSenhaForm.vue'

describe('RedefinirSenhaForm', () => {
  it('emite "submit" quando as senhas são fortes e coincidem', () => {
    cy.mount(RedefinirSenhaForm, { props: { onSubmit: cy.spy().as('onSubmit') } })
    cy.get('[data-cy=nova-senha]').type('NovaSenha1!')
    cy.get('[data-cy=confirmar-senha]').type('NovaSenha1!')
    cy.get('[data-cy=btn-atualizar-senha]').click()
    cy.get('@onSubmit').should('have.been.calledWith', {
      novaSenha: 'NovaSenha1!',
      confirmarSenha: 'NovaSenha1!',
    })
  })

  it('exibe erro quando as senhas não coincidem', () => {
    cy.mount(RedefinirSenhaForm)
    cy.get('[data-cy=nova-senha]').type('NovaSenha1!')
    cy.get('[data-cy=confirmar-senha]').type('Outra1!')
    cy.get('[data-cy=btn-atualizar-senha]').click()
    cy.contains('As senhas não coincidem').should('be.visible')
  })

  it('exibe erro de senha fraca (RNF04)', () => {
    cy.mount(RedefinirSenhaForm)
    cy.get('[data-cy=nova-senha]').type('fraca')
    cy.get('[data-cy=confirmar-senha]').type('fraca')
    cy.get('[data-cy=btn-atualizar-senha]').click()
    cy.contains('A senha deve ter no mínimo 8 caracteres').should('be.visible')
  })

  it('alterna a visibilidade das duas senhas ao clicar no ícone de olho', () => {
    cy.mount(RedefinirSenhaForm)
    cy.get('[data-cy=nova-senha] input').should('have.attr', 'type', 'password')
    cy.get('[data-cy=nova-senha] .v-field__append-inner').click()
    // O mesmo estado controla os dois campos.
    cy.get('[data-cy=nova-senha] input').should('have.attr', 'type', 'text')
    cy.get('[data-cy=confirmar-senha] input').should('have.attr', 'type', 'text')
  })
})
