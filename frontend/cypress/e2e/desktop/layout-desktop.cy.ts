// Desktop (a partir de 840px): rail, sem Home, e QR code no login e no cadastro.
const listaVazia = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }

function entrar() {
  cy.intercept('POST', '**/auth/login', { statusCode: 200, fixture: 'usuario.json' }).as('login')
  cy.intercept('GET', '**/perfil', { statusCode: 200, fixture: 'perfil.json' }).as('perfil')
  // Tela inicial do desktop: a lista de compromissos (com o filtro de prédios).
  cy.intercept('GET', '**/predios?*', { statusCode: 200, body: listaVazia }).as('listarPredios')
  cy.intercept('GET', '**/compromissos?*', { statusCode: 200, body: listaVazia }).as(
    'listarCompromissos',
  )

  cy.visit('/login')
  cy.get('[data-cy=email]').type('sindico@exemplo.com')
  cy.get('[data-cy=senha]').type('SenhaForte1!')
  cy.get('[data-cy=btn-entrar]').click()
  cy.wait('@login')
}

describe('Layout desktop', () => {
  beforeEach(() => {
    cy.viewport(1280, 800)
  })

  it('mostra o QR code no login e no cadastro', () => {
    cy.visit('/login')
    cy.get('[data-cy=qrcode-celular]').should('be.visible')

    cy.visit('/registrar')
    cy.get('[data-cy=qrcode-celular]').should('be.visible')
  })

  describe('área logada', () => {
    beforeEach(() => {
      entrar()
    })

    it('abre em Compromissos, sem Home, com o rail no lugar do hub', () => {
      cy.url().should('match', /\/compromissos$/)
      cy.wait('@listarCompromissos')

      cy.get('[data-cy=app-rail]').should('be.visible')
      cy.get('[data-cy=rail-marca]').should('contain', 'euSíndico')
      cy.get('[data-cy=rail-home]').should('not.exist')
      cy.get('[data-cy=rail-compromissos]').should('have.attr', 'aria-current', 'page')

      // Tela de topo: o "voltar" iria para a Home, que não existe no desktop.
      cy.get('[data-cy=btn-voltar]').should('not.exist')
      cy.get('[data-cy=btn-home]').should('not.exist')
    })

    it('redireciona /home para Compromissos (deep link)', () => {
      // cy.visit recarrega a página: a sessão volta pelo bootstrap (POST /auth/refresh).
      cy.intercept('POST', '**/auth/refresh', { statusCode: 200, fixture: 'usuario.json' }).as(
        'refresh',
      )
      cy.visit('/home')
      cy.wait('@refresh')
      cy.url().should('match', /\/compromissos$/)
    })

    it('navega pelo rail e marca o módulo pai numa subtela', () => {
      cy.get('[data-cy=rail-predios]').click()
      cy.url().should('match', /\/predios$/)
      cy.get('[data-cy=rail-predios]').should('have.attr', 'aria-current', 'page')

      cy.get('[data-cy=rail-configuracoes]').click()
      cy.get('[data-cy=card-config-perfil]').click()
      cy.url().should('include', '/configuracoes/perfil')
      cy.get('[data-cy=rail-configuracoes]').should('have.attr', 'aria-current', 'page')

      // Subtela: o "voltar" continua, porque leva a outra tela (Configurações), não à Home.
      cy.get('[data-cy=btn-voltar]').click()
      cy.url().should('match', /\/configuracoes$/)
    })

    it('desloga pelo rodapé do rail', () => {
      cy.intercept('POST', '**/auth/logout', { statusCode: 204 }).as('logout')

      cy.get('[data-cy=rail-sair]').click()
      cy.wait('@logout')
      cy.url().should('include', '/login')
    })
  })

  it('sai da Home quando a tela aberta passa a ser desktop (janela redimensionada)', () => {
    cy.viewport(390, 844)
    entrar()
    cy.url().should('include', '/home')

    cy.viewport(1280, 800)
    cy.url().should('match', /\/compromissos$/)
  })

  // Rail (useDisplay do Vuetify), redirecionamento da Home (matchMedia) e CSS precisam virar juntos
  // no mesmo limite (840px, breakpoint md do Vuetify 4) — senão a faixa entre eles fica misturada.
  it('celular até 839px, desktop a partir de 840px', () => {
    cy.viewport(830, 800)
    entrar()
    cy.url().should('include', '/home')
    cy.get('[data-cy=app-rail]').should('not.exist')
    cy.get('[data-cy=btn-logout]').should('be.visible')

    cy.viewport(850, 800)
    cy.url().should('match', /\/compromissos$/)
    cy.get('[data-cy=app-rail]').should('be.visible')
    cy.get('[data-cy=btn-home]').should('not.exist')
  })
})
