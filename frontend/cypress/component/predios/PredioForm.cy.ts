import PredioForm from '../../../src/components/predios/PredioForm.vue'

describe('PredioForm', () => {
  it('emite "submit" com nome e endereço preenchidos', () => {
    cy.mount(PredioForm, { props: { onSubmit: cy.spy().as('onSubmit') } })
    cy.get('[data-cy=nome]').type('Edifício Solar')
    cy.get('[data-cy=endereco]').type('Rua da Silva, 192, Floresta')
    cy.get('[data-cy=btn-salvar-predio]').click()
    cy.get('@onSubmit').should('have.been.calledWith', {
      nome: 'Edifício Solar',
      endereco: 'Rua da Silva, 192, Floresta',
    })
  })

  it('exibe erro de validação com nome vazio', () => {
    cy.mount(PredioForm)
    cy.get('[data-cy=endereco]').type('Rua da Silva, 192')
    cy.get('[data-cy=btn-salvar-predio]').click()
    cy.contains('Nome obrigatório').should('be.visible')
  })

  it('exibe erro de validação com endereço contendo caracteres não permitidos', () => {
    cy.mount(PredioForm)
    cy.get('[data-cy=nome]').type('Edifício Solar')
    cy.get('[data-cy=endereco]').type('<script>alert(1)</script>')
    cy.get('[data-cy=btn-salvar-predio]').click()
    cy.contains('O endereço contém caracteres não permitidos').should('be.visible')
  })

  it('pré-preenche os campos e troca o rótulo do botão em modo edição', () => {
    cy.mount(PredioForm, {
      props: {
        modoEdicao: true,
        valoresIniciais: { nome: 'Edifício Solar', endereco: 'Rua da Silva, 192' },
      },
    })
    cy.get('[data-cy=nome] input').should('have.value', 'Edifício Solar')
    cy.get('[data-cy=endereco] textarea').should('have.value', 'Rua da Silva, 192')
    cy.get('[data-cy=btn-salvar-predio]').should('contain', 'Salvar alterações')
  })
})
