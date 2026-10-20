// Navegações só pela UI (nunca cy.visit() no meio do teste) — o accessToken vive em memória, ver
// predios/gerenciar-predios.cy.ts.
function login() {
  cy.intercept('POST', '**/auth/login', { statusCode: 200, fixture: 'usuario.json' }).as('login')
  cy.intercept('GET', '**/perfil', { statusCode: 200, fixture: 'perfil.json' }).as('perfil')
  cy.intercept('GET', '**/predios?*', { statusCode: 200, fixture: 'predios.json' }).as('predios')
  cy.visit('/login')
  cy.get('[data-cy=email]').type('sindico@exemplo.com')
  cy.get('[data-cy=senha]').type('SenhaForte1!')
  cy.get('[data-cy=btn-entrar]').click()
  cy.wait('@login')
  cy.url().should('include', '/home')
}

function abrirListaDeCompromissos() {
  cy.intercept('GET', '**/compromissos?*', { statusCode: 200, fixture: 'compromissos.json' }).as('listar')
  cy.get('[data-cy=card-compromissos]').click()
  cy.wait('@listar')
}

describe('Compromissos (RF12-RF17, RF34)', () => {
  beforeEach(login)

  it('lista os compromissos próprios e futuros por padrão (RN27)', () => {
    cy.intercept('GET', '**/compromissos?*', { statusCode: 200, fixture: 'compromissos.json' }).as('listar')
    cy.get('[data-cy=card-compromissos]').click()
    cy.wait('@listar')
      .its('request.url')
      .should('include', 'situacao=proximos')
      .and('include', 'escopo=meus')
    cy.get('[data-cy=compromisso-item-11]').should('contain', 'Reunião').and('contain', 'Edifício Solar')
    cy.get('[data-cy=compromisso-item-12]').should('contain', 'Conserto de portão')
  })

  it('exibe o estado vazio', () => {
    cy.intercept('GET', '**/compromissos?*', {
      statusCode: 200,
      body: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
    }).as('listarVazio')
    cy.get('[data-cy=card-compromissos]').click()
    cy.wait('@listarVazio')
    cy.get('[data-cy=compromissos-vazio]').should('contain', 'Você não tem compromissos próximos')
  })

  it('busca por título com debounce: uma única requisição por digitação, não uma por tecla', () => {
    abrirListaDeCompromissos()
    cy.get('[data-cy=busca-compromisso] input').type('portão')
    cy.wait('@listar').its('request.url').should('include', 'titulo=port')
    // 1 da carga inicial + 1 da busca.
    cy.get('@listar.all').should('have.length', 2)
  })

  it('limpar a busca pelo X refaz a listagem na hora, sem o termo', () => {
    abrirListaDeCompromissos()
    cy.get('[data-cy=busca-compromisso] input').type('portão')
    cy.wait('@listar')

    cy.get('[data-cy=busca-compromisso] .v-field__clearable .v-icon').click()
    cy.wait('@listar').its('request.url').should('not.include', 'titulo=')
    cy.get('[data-cy=busca-compromisso] input').should('have.value', '')
  })

  it('Enter busca imediatamente, sem esperar o atraso', () => {
    abrirListaDeCompromissos()
    cy.get('[data-cy=busca-compromisso] input').type('portão{enter}')
    cy.wait('@listar', { timeout: 400 }).its('request.url').should('include', 'titulo=port')
  })

  it('filtra por situação pela folha de filtros', () => {
    abrirListaDeCompromissos()
    cy.get('[data-cy=btn-filtros]').click()
    cy.get('[data-cy=filtro-situacao-concluidos]').click()
    cy.wait('@listar').its('request.url').should('include', 'situacao=concluidos')
    cy.get('[data-cy=filtros-fechar]').click()

    // O filtro aparece como chip; fechar o chip volta ao padrão ("próximos").
    cy.get('[data-cy=chip-situacao]').should('contain', 'Concluídos')
    cy.get('[data-cy=chip-situacao] .v-chip__close').click()
    cy.wait('@listar').its('request.url').should('include', 'situacao=proximos')
    cy.get('[data-cy=chip-situacao]').should('not.exist')
  })

  it('alterna entre meus e todos e mostra o responsável (RF34)', () => {
    abrirListaDeCompromissos()
    cy.get('[data-cy=btn-filtros]').click()
    cy.get('[data-cy=filtro-escopo-todos]').click()
    cy.wait('@listar').its('request.url').should('include', 'escopo=todos')
    cy.get('[data-cy=filtros-fechar]').click()
    cy.get('[data-cy=compromisso-responsavel-11]').should('contain', 'Luciano Souza')

    cy.get('[data-cy=chip-escopo]').should('contain', 'Todos')
    cy.get('[data-cy=chip-escopo] .v-chip__close').click()
    cy.wait('@listar').its('request.url').should('include', 'escopo=meus')
    cy.get('[data-cy=chip-escopo]').should('not.exist')
  })

  it('abre a lista filtrada pelo prédio a partir da tela do prédio', () => {
    cy.intercept('GET', '**/predios/1', { statusCode: 200, fixture: 'predio.json' }).as('detalhePredio')
    cy.intercept('GET', '**/compromissos?*', { statusCode: 200, fixture: 'compromissos.json' }).as('listar')
    cy.get('[data-cy=card-predios]').click()
    cy.get('[data-cy=predio-card-1]').click()
    cy.wait('@detalhePredio')

    cy.get('[data-cy=card-compromissos-predio]').click()
    cy.wait('@listar').its('request.url').should('include', 'predioId=1')
    cy.get('[data-cy=chip-predio]').should('contain', 'Edifício Solar')

    // O "voltar" devolve ao prédio de onde veio.
    cy.get('[data-cy=btn-voltar]').click()
    cy.url().should('match', /\/predios\/1$/)
  })

  it('conclui um compromisso: anima, sai da lista e permite desfazer (RF17, RN23)', () => {
    cy.intercept('PATCH', '**/compromissos/11', (req) => {
      req.reply({
        statusCode: 200,
        body: { ...req.body, id: 11, titulo: 'Reunião', dataCompromisso: '2099-02-18', horarioCompromisso: '18:30:00',
          nomePrestadorServico: null, detalhes: null, predioId: 1, predioNome: 'Edifício Solar',
          responsavelUsuarioId: 1, responsavelNome: 'Luciano Souza', criadoEm: '2026-01-01T12:00:00Z' },
      })
    }).as('concluir')
    abrirListaDeCompromissos()

    cy.get('[data-cy=compromisso-check-11] input').click()
    cy.get('[data-cy=compromisso-item-11]').should('have.class', 'compromisso-item--concluindo')
    cy.wait('@concluir').its('request.body').should('deep.equal', { concluido: true })

    cy.get('[data-cy=compromisso-item-11]').should('not.exist')
    cy.get('[data-cy=compromisso-item-12]').should('be.visible')
    cy.get('[data-cy=snackbar]').should('contain', 'Compromisso concluído')

    cy.get('[data-cy=btn-desfazer]').click()
    cy.wait('@concluir').its('request.body').should('deep.equal', { concluido: false })
    cy.wait('@listar')
    cy.get('[data-cy=compromisso-item-11]').should('be.visible')
  })

  it('mantém o item na lista e avisa quando concluir falha', () => {
    cy.intercept('PATCH', '**/compromissos/11', { statusCode: 500, body: { title: 'Erro', status: 500 } }).as('concluir')
    abrirListaDeCompromissos()

    cy.get('[data-cy=compromisso-check-11] input').click()
    cy.wait('@concluir')

    cy.get('[data-cy=snackbar]').should('contain', 'Não foi possível atualizar o compromisso')
    cy.get('[data-cy=compromisso-item-11]').should('exist')
  })

  it('remove um compromisso após confirmação (RF16, RN25)', () => {
    cy.intercept('DELETE', '**/compromissos/12', { statusCode: 204 }).as('remover')
    abrirListaDeCompromissos()

    cy.get('[data-cy=compromisso-remover-12]').click()
    cy.contains('Remover compromisso').should('be.visible')
    cy.get('[data-cy=dialog-confirmar]').click()

    cy.wait('@remover')
    cy.get('[data-cy=compromisso-item-12]').should('not.exist')
    cy.get('[data-cy=snackbar]').should('contain', 'Compromisso removido')
  })

  it('cria um compromisso escolhendo prédio e responsável e volta para a lista (RF12, RF13, RN20)', () => {
    cy.intercept('GET', '**/predios/1/equipe', { statusCode: 200, fixture: 'equipe.json' }).as('equipe')
    cy.intercept('POST', '**/compromissos', { statusCode: 201, fixture: 'compromisso.json' }).as('criar')
    abrirListaDeCompromissos()

    cy.get('[data-cy=btn-criar-compromisso]').click()
    cy.url().should('include', '/compromissos/novo')

    cy.get('[data-cy=titulo]').type('Visita técnica')
    cy.get('[data-cy=predio]').click()
    cy.get('.v-list-item').contains('Edifício Solar').click()
    cy.wait('@equipe')
    cy.get('[data-cy=data]').type('2099-02-18')
    cy.get('[data-cy=horario]').type('18:30')
    cy.get('[data-cy=responsavel]').click()
    cy.get('.v-list-item').contains('Ana Souza').click()
    cy.get('[data-cy=btn-salvar-compromisso]').click()

    cy.wait('@criar').its('request.body').should('deep.include', {
      predioId: 1,
      titulo: 'Visita técnica',
      dataCompromisso: '2099-02-18',
      horarioCompromisso: '18:30:00',
      responsavelUsuarioId: 2,
    })
    cy.url().should('match', /\/compromissos$/)
  })

  it('não oferece responsável num prédio onde o usuário é Colaborador (RN20)', () => {
    abrirListaDeCompromissos()
    cy.get('[data-cy=btn-criar-compromisso]').click()

    cy.get('[data-cy=predio]').click()
    cy.get('.v-list-item').contains('Edifício Praiol').click()
    cy.get('[data-cy=responsavel]').should('not.exist')
  })

  it('edita um compromisso pelo detalhe e volta para a lista (RF15)', () => {
    cy.intercept('GET', '**/compromissos/12', { statusCode: 200, fixture: 'compromisso.json' }).as('detalhe')
    cy.intercept('GET', '**/predios/2/equipe', { statusCode: 200, fixture: 'equipe.json' }).as('equipe')
    cy.intercept('PUT', '**/compromissos/12', { statusCode: 200, fixture: 'compromisso.json' }).as('atualizar')
    abrirListaDeCompromissos()

    cy.get('[data-cy=compromisso-link-12]').click()
    cy.wait('@detalhe')
    cy.get('[data-cy=titulo] input').should('have.value', 'Conserto de portão')
    cy.get('[data-cy=horario] input').should('have.value', '18:00')
    cy.get('[data-cy=prestador] input').should('have.value', 'Portões Silva')

    cy.get('[data-cy=titulo] input').clear()
    cy.get('[data-cy=titulo] input').type('Conserto do portão da garagem')
    cy.get('[data-cy=btn-salvar-compromisso]').click()

    cy.wait('@atualizar').its('request.body').should('deep.include', {
      titulo: 'Conserto do portão da garagem',
      responsavelUsuarioId: 1,
    })
    cy.url().should('match', /\/compromissos$/)
  })

  it('mostra erro quando o compromisso não existe (404)', () => {
    cy.intercept('GET', '**/compromissos/12', { statusCode: 404, body: { title: 'Compromisso não encontrado', status: 404 } }).as('detalhe')
    abrirListaDeCompromissos()

    cy.get('[data-cy=compromisso-link-12]').click()
    cy.wait('@detalhe')
    cy.get('[data-cy=erro-compromisso-detalhe]').should('contain', 'Compromisso não encontrado')
  })
})
