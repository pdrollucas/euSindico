// Todas as navegações abaixo acontecem clicando pela UI (nunca cy.visit() no meio do teste): o
// accessToken vive só em memória (ver AUTHENTICATION.md) e cy.visit() recarrega a página de
// verdade, o que derrubaria a sessão autenticada pelo login() abaixo.
function login() {
  cy.intercept('POST', '**/auth/login', { statusCode: 200, fixture: 'usuario.json' }).as('login')
  cy.intercept('GET', '**/perfil', { statusCode: 200, fixture: 'perfil.json' }).as('perfil')
  cy.visit('/login')
  cy.get('[data-cy=email]').type('sindico@exemplo.com')
  cy.get('[data-cy=senha]').type('SenhaForte1!')
  cy.get('[data-cy=btn-entrar]').click()
  cy.wait('@login')
  cy.url().should('include', '/home')
}

function abrirListaDePredios() {
  cy.intercept('GET', '**/predios?*', { statusCode: 200, fixture: 'predios.json' }).as('listar')
  cy.get('[data-cy=card-predios]').click()
  cy.wait('@listar')
}

describe('Prédios (RF08-RF11)', () => {
  beforeEach(login)

  it('lista os prédios do usuário e navega para o detalhe ao clicar num card', () => {
    cy.intercept('GET', '**/predios/1', { statusCode: 200, fixture: 'predio.json' }).as('detalhe')
    abrirListaDePredios()
    cy.url().should('include', '/predios')
    cy.get('[data-cy=predio-card-1]').should('contain', 'Edifício Solar')
    cy.get('[data-cy=predio-card-2]').should('contain', 'Edifício Nuvem')

    cy.get('[data-cy=predio-card-1]').click()
    cy.wait('@detalhe')
    cy.url().should('include', '/predios/1')
    cy.get('[data-cy=predio-endereco]').should('contain', 'Rua da Silva, 192, Floresta')
  })

  it('exibe o estado vazio quando o usuário não tem nenhum prédio', () => {
    cy.intercept('GET', '**/predios?*', {
      statusCode: 200,
      body: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
    }).as('listarVazio')
    cy.get('[data-cy=card-predios]').click()
    cy.wait('@listarVazio')
    cy.get('[data-cy=predios-vazio]').should('contain', 'Você ainda não tem nenhum prédio')
  })

  it('filtra a lista por nome (busca client-side)', () => {
    abrirListaDePredios()
    cy.get('[data-cy=busca-predio] input').type('nuvem')
    cy.get('[data-cy=predio-card-2]').should('be.visible')
    cy.get('[data-cy=predio-card-1]').should('not.exist')
  })

  it('cria um prédio por um modal aberto pelo FAB "+" e navega para o detalhe criado (RF08)', () => {
    cy.intercept('POST', '**/predios', { statusCode: 201, fixture: 'predio.json' }).as('criar')
    cy.intercept('GET', '**/predios/1', { statusCode: 200, fixture: 'predio.json' }).as('detalhe')
    abrirListaDePredios()

    cy.get('[data-cy=btn-criar-predio]').click()
    cy.contains('Novo prédio').should('be.visible')

    cy.get('[data-cy=nome]').type('Edifício Solar')
    cy.get('[data-cy=endereco]').type('Rua da Silva, 192, Floresta')
    cy.get('[data-cy=btn-salvar-predio]').click()

    cy.wait('@criar')
    cy.wait('@detalhe')
    cy.url().should('include', '/predios/1')
  })

  it('exibe mensagem de conflito ao criar prédio duplicado ou acima do limite (409)', () => {
    cy.intercept('POST', '**/predios', {
      statusCode: 409,
      body: { title: 'Já existe um prédio ativo com esse nome e endereço.', status: 409 },
    }).as('criarConflito')
    abrirListaDePredios()
    cy.get('[data-cy=btn-criar-predio]').click()

    cy.get('[data-cy=nome]').type('Edifício Solar')
    cy.get('[data-cy=endereco]').type('Rua da Silva, 192, Floresta')
    cy.get('[data-cy=btn-salvar-predio]').click()

    cy.wait('@criarConflito')
    // Mostra o title exato do backend, não um "X ou Y" genérico.
    cy.get('[data-cy=erro-predio-form]').should(
      'contain',
      'Já existe um prédio ativo com esse nome e endereço.',
    )
    // O modal continua aberto pra correção — não navega pra lugar nenhum num erro.
    cy.get('[data-cy=nome]').should('be.visible')
  })

  it('edita um prédio existente pelo modal do menu de ações (RF10) — só pro papel Síndico', () => {
    cy.intercept('GET', '**/predios/1', { statusCode: 200, fixture: 'predio.json' }).as('detalhe')
    abrirListaDePredios()
    cy.get('[data-cy=predio-card-1]').click()
    cy.wait('@detalhe')

    cy.get('[data-cy=btn-mais-acoes]').should('be.visible').click()
    cy.get('[data-cy=btn-editar-predio]').click()
    cy.get('[data-cy=nome] input').should('have.value', 'Edifício Solar')

    const predioAtualizado = {
      id: 1,
      nome: 'Edifício Solar Renovado',
      endereco: 'Rua da Silva, 192, Floresta',
      criadoEm: '2026-01-01T12:00:00Z',
      papel: 1,
    }
    cy.intercept('PUT', '**/predios/1', { statusCode: 200, body: predioAtualizado }).as('atualizar')

    cy.get('[data-cy=nome]').clear()
    cy.get('[data-cy=nome]').type('Edifício Solar Renovado')
    cy.get('[data-cy=btn-salvar-predio]').click()

    cy.wait('@atualizar')
    // O modal fecha e o próprio detalhe reflete o nome novo, sem navegar pra outra rota.
    cy.url().should('match', /\/predios\/1$/)
    cy.contains('Edifício Solar Renovado').should('be.visible')
  })

  it('remove um prédio após confirmação no diálogo, a partir do menu de ações (RN07, RF11)', () => {
    cy.intercept('GET', '**/predios/1', { statusCode: 200, fixture: 'predio.json' }).as('detalhe')
    cy.intercept('DELETE', '**/predios/1', { statusCode: 204 }).as('remover')
    abrirListaDePredios()
    cy.get('[data-cy=predio-card-1]').click()
    cy.wait('@detalhe')

    cy.get('[data-cy=btn-mais-acoes]').click()
    cy.get('[data-cy=btn-remover-predio]').click()
    // Confirmação é obrigatória antes da exclusão (RN07) — o diálogo aparece antes de qualquer
    // chamada ao backend.
    cy.contains('Remover prédio').should('be.visible')
    cy.get('[data-cy=dialog-confirmar]').click()

    cy.wait('@remover')
    cy.url().should('match', /\/predios$/)
  })

  it('esconde o menu de editar/remover para quem não é Síndico do prédio (RF33)', () => {
    cy.intercept('GET', '**/predios/3', { statusCode: 200, fixture: 'predio-colaborador.json' }).as(
      'detalheColaborador',
    )
    abrirListaDePredios()
    cy.get('[data-cy=predio-card-3]').click()
    cy.wait('@detalheColaborador')

    cy.get('[data-cy=btn-mais-acoes]').should('not.exist')
  })
})
