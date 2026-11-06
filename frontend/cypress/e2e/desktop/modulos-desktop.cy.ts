// Demais módulos no desktop: ação principal no cabeçalho (sem FAB) e lista em tabela.
const pagina = <T>(items: T[]) => ({
  items,
  page: 1,
  pageSize: 20,
  totalCount: items.length,
  totalPages: 1,
})

const planejamento = {
  id: 7,
  titulo: 'Pintura da fachada',
  dataPlanejamento: '2099-03-10',
  orcamentoPrevisto: 15000,
  nomePrestadorServico: null,
  detalhes: null,
  predioId: 1,
  predioNome: 'Edifício Solar',
  criadoEm: '2026-01-02T12:00:00Z',
}

const relatorio = {
  id: 3,
  mesReferencia: 10,
  anoReferencia: 2026,
  nomeArquivo: 'relatorio-2026-10.pdf',
  predioId: 1,
  predioNome: 'Edifício Solar',
  geradoEm: '2026-11-01T12:00:00Z',
}

const ata = {
  id: 5,
  nomeArquivo: 'ata-assembleia.pdf',
  predioId: 1,
  predioNome: 'Edifício Solar',
  criadoEm: '2026-10-20T12:00:00Z',
}

function entrarEAbrirPredio() {
  cy.intercept('POST', '**/auth/login', { statusCode: 200, fixture: 'usuario.json' }).as('login')
  cy.intercept('GET', '**/perfil', { statusCode: 200, fixture: 'perfil.json' })
  cy.intercept('GET', '**/predios?*', { statusCode: 200, fixture: 'predios.json' }).as('predios')
  cy.intercept('GET', '**/compromissos?*', { statusCode: 200, body: pagina([]) })
  cy.intercept('GET', '**/predios/1', { statusCode: 200, fixture: 'predio.json' }).as('predio')

  cy.visit('/login')
  cy.get('[data-cy=email]').type('sindico@exemplo.com')
  cy.get('[data-cy=senha]').type('SenhaForte1!')
  cy.get('[data-cy=btn-entrar]').click()
  cy.wait('@login')

  cy.get('[data-cy=rail-predios]').click()
  cy.wait('@predios')
  cy.get('[data-cy=predio-link-1]').click()
  cy.wait('@predio')
}

describe('Módulos no desktop', () => {
  beforeEach(() => {
    cy.viewport(1280, 800)
  })

  it('Prédios: tabela e "Novo prédio" no cabeçalho', () => {
    cy.intercept('POST', '**/auth/login', { statusCode: 200, fixture: 'usuario.json' })
    cy.intercept('GET', '**/perfil', { statusCode: 200, fixture: 'perfil.json' })
    cy.intercept('GET', '**/compromissos?*', { statusCode: 200, body: pagina([]) })
    cy.intercept('GET', '**/predios?*', { statusCode: 200, fixture: 'predios.json' }).as('predios')
    cy.visit('/login')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-entrar]').click()

    cy.get('[data-cy=rail-predios]').click()
    cy.wait('@predios')
    cy.get('[data-cy=predio-item-1]').should('contain', 'Edifício Solar')
    cy.get('.predios-fab').should('not.exist')
    cy.get('.app-header [data-cy=btn-criar-predio]').click()
    cy.contains('.v-card-title', 'Novo prédio').should('be.visible')
  })

  it('Detalhe do prédio: editar e remover no cabeçalho, sem o menu', () => {
    entrarEAbrirPredio()

    cy.get('[data-cy=btn-mais-acoes]').should('not.exist')
    cy.get('.app-header [data-cy=btn-remover-predio]').should('be.visible')
    cy.get('.app-header [data-cy=btn-editar-predio]').click()
    cy.contains('.v-card-title', 'Editar prédio').should('be.visible')
  })

  it('Equipe: tabela e as duas ações no cabeçalho', () => {
    cy.intercept('GET', '**/predios/1/equipe', { statusCode: 200, fixture: 'equipe.json' }).as(
      'equipe',
    )
    entrarEAbrirPredio()
    cy.get('[data-cy=card-equipe]').click()
    cy.wait('@equipe')

    cy.get('[data-cy=membro-2]').should('contain', 'Ana Souza').and('contain', 'ana@exemplo.com')
    cy.get('[data-cy=btn-remover-membro-2]').should('be.visible')
    cy.get('.equipe-fab').should('not.exist')
    cy.get('.app-header [data-cy=btn-ajuda-perfis]').should('be.visible')
    cy.get('.app-header [data-cy=btn-convidar]').click()
    cy.contains('.v-card-title', 'Convidar funcionário').should('be.visible')
  })

  it('Planejamentos: tabela, faixa de orçamento na barra e ação no cabeçalho', () => {
    cy.intercept('GET', '**/predios/1/planejamentos*', {
      statusCode: 200,
      body: pagina([planejamento]),
    }).as('planejamentos')
    entrarEAbrirPredio()
    cy.get('[data-cy=card-planejamentos-predio]').click()
    cy.wait('@planejamentos')

    cy.get('[data-cy=planejamento-item-7]').should('contain', 'Pintura da fachada')
    cy.get('[data-cy=planejamento-orcamento-7]').should('contain', '15.000,00')
    cy.get('[data-cy=btn-filtros]').should('not.exist')
    cy.get('.app-header [data-cy=btn-criar-planejamento]').should('be.visible')

    cy.get('[data-cy=filtro-orcamento-minimo] input').type('1000000')
    cy.get('[data-cy=filtro-orcamento-minimo] input').blur()
    cy.wait('@planejamentos').its('request.url').should('include', 'orcamentoMinimo=10000')
    cy.get('[data-cy=filtros-limpar]').should('not.be.disabled')
  })

  it('Relatórios: tabela e "Gerar relatório" no cabeçalho', () => {
    cy.intercept('GET', '**/predios/1/relatorios*', {
      statusCode: 200,
      body: pagina([relatorio]),
    }).as('relatorios')
    entrarEAbrirPredio()
    cy.get('[data-cy=card-relatorios-predio]').click()
    cy.wait('@relatorios')

    cy.get('[data-cy=relatorio-item-3]').should('contain', 'Outubro de 2026')
    cy.get('[data-cy=relatorio-baixar-3]').should('be.visible')
    cy.get('.relatorios-fab').should('not.exist')
    cy.get('.app-header [data-cy=btn-gerar-relatorio-cabecalho]').click()
    cy.contains('.v-card-title', 'Gerar relatório').should('be.visible')
  })

  it('Atas: tabela e "Enviar ata" no cabeçalho', () => {
    cy.intercept('GET', '**/predios/1/atas*', { statusCode: 200, body: pagina([ata]) }).as('atas')
    entrarEAbrirPredio()
    cy.get('[data-cy=card-atas-predio]').click()
    cy.wait('@atas')

    cy.get('[data-cy=documento-item-5]').should('contain', 'ata-assembleia.pdf')
    cy.get('.documentos-fab').should('not.exist')
    cy.get('.app-header [data-cy=btn-enviar-documento-cabecalho]')
      .should('contain', 'Enviar ata')
      .click()
    cy.contains('.v-card-title', 'Enviar ata').should('be.visible')
  })
})
