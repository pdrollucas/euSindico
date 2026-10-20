import CompromissoForm from '../../../src/components/compromissos/CompromissoForm.vue'

const predios = [
  {
    id: 1,
    nome: 'Edifício Solar',
    endereco: 'Rua A',
    criadoEm: '2026-01-01T12:00:00Z',
    papel: 1 as const,
  },
  {
    id: 2,
    nome: 'Edifício Nuvem',
    endereco: 'Rua B',
    criadoEm: '2026-01-01T12:00:00Z',
    papel: 2 as const,
  },
  {
    id: 3,
    nome: 'Edifício Praiol',
    endereco: 'Rua C',
    criadoEm: '2026-01-01T12:00:00Z',
    papel: 3 as const,
  },
]

const membros = [
  {
    usuarioId: 1,
    nome: 'Luciano Souza',
    email: 'l@x.com',
    papel: 1 as const,
    criadoEm: '2026-01-01T12:00:00Z',
  },
  {
    usuarioId: 2,
    nome: 'Ana Souza',
    email: 'a@x.com',
    papel: 2 as const,
    criadoEm: '2026-01-01T12:00:00Z',
  },
]

const props = { predios, membros, perfilId: 1 }

describe('CompromissoForm', () => {
  it('emite "submit" com os campos preenchidos e o próprio autor como responsável padrão', () => {
    cy.mount(CompromissoForm, {
      props: { ...props, valoresIniciais: { predioId: 1 }, onSubmit: cy.spy().as('onSubmit') },
    })
    cy.get('[data-cy=titulo]').type('Visita técnica')
    cy.get('[data-cy=data]').type('2099-02-18')
    cy.get('[data-cy=horario]').type('18:30')
    cy.get('[data-cy=btn-salvar-compromisso]').click()

    cy.get('@onSubmit').should('have.been.calledWithMatch', {
      predioId: 1,
      titulo: 'Visita técnica',
      dataCompromisso: '2099-02-18',
      horarioCompromisso: '18:30',
      responsavelUsuarioId: 1,
    })
  })

  it('mostra só o ícone da esquerda na data, sem o indicador nativo do navegador', () => {
    cy.mount(CompromissoForm, { props })
    // O indicador é shadow DOM: getComputedStyle com o pseudo-elemento devolve o padrão do
    // navegador, então confere-se a regra que o esconde nas folhas de estilo.
    cy.document().then((doc) => {
      const regras = [...doc.styleSheets]
        .flatMap((folha) => {
          try {
            return [...folha.cssRules]
          } catch {
            return []
          }
        })
        .filter((regra): regra is CSSStyleRule => regra instanceof CSSStyleRule)
        .filter(
          (regra) =>
            regra.selectorText.includes('calendar-picker-indicator') &&
            regra.style.display === 'none',
        )
        .map((regra) => regra.selectorText)
      cy.wrap(regras.some((s) => s.includes('type="date"') || s.includes("type='date'"))).should(
        'be.true',
      )
    })
  })

  it('abre o seletor nativo ao clicar em qualquer parte do campo de data', () => {
    cy.mount(CompromissoForm, { props })
    cy.get('[data-cy=data] input').then(($input) => {
      cy.stub($input[0]!, 'showPicker').as('showPicker')
    })
    cy.get('[data-cy=data]').click()
    cy.get('@showPicker').should('have.been.called')
  })

  it('horário é digitado: teclado numérico, máscara HH:mm e sem seletor de relógio', () => {
    cy.mount(CompromissoForm, { props })
    cy.get('[data-cy=horario] input')
      .should('have.attr', 'inputmode', 'numeric')
      .and('not.have.attr', 'type', 'time')
  })

  it('máscara do horário: "1830" vira "18:30" e letras são descartadas', () => {
    cy.mount(CompromissoForm, { props })
    cy.get('[data-cy=horario] input').type('1830')
    cy.get('[data-cy=horario] input').should('have.value', '18:30')
    cy.get('[data-cy=horario] input').clear()
    cy.get('[data-cy=horario] input').type('a1b8c3d0')
    cy.get('[data-cy=horario] input').should('have.value', '18:30')
  })

  it('máscara do horário: backspace passa pelo ":" sem travar e não aceita mais de 4 dígitos', () => {
    cy.mount(CompromissoForm, { props })
    cy.get('[data-cy=horario] input').type('18309')
    cy.get('[data-cy=horario] input').should('have.value', '18:30')
    // "18:30" -> "18:3" -> "18" (o ":" some junto, sem consumir um backspace) -> "1".
    cy.get('[data-cy=horario] input').type('{backspace}{backspace}')
    cy.get('[data-cy=horario] input').should('have.value', '18')
    cy.get('[data-cy=horario] input').type('{backspace}')
    cy.get('[data-cy=horario] input').should('have.value', '1')
  })

  it('rejeita horário inexistente como 25:61', () => {
    cy.mount(CompromissoForm, { props: { ...props, valoresIniciais: { predioId: 1 } } })
    cy.get('[data-cy=titulo]').type('Visita')
    cy.get('[data-cy=data]').type('2099-02-18')
    cy.get('[data-cy=horario]').type('2561')
    cy.get('[data-cy=btn-salvar-compromisso]').click()
    cy.contains('Horário inválido').should('be.visible')
  })

  it('exige título, prédio, data e horário (RN03, RN10)', () => {
    cy.mount(CompromissoForm, { props: { ...props, onSubmit: cy.spy().as('onSubmit') } })
    cy.get('[data-cy=btn-salvar-compromisso]').click()

    cy.contains('Título obrigatório').should('be.visible')
    cy.contains('Selecione o prédio').should('be.visible')
    cy.contains('Selecione a data').should('be.visible')
    cy.contains('Informe o horário').should('be.visible')
    cy.get('@onSubmit').should('not.have.been.called')
  })

  it('rejeita título com caracteres não permitidos', () => {
    cy.mount(CompromissoForm, { props: { ...props, valoresIniciais: { predioId: 1 } } })
    cy.get('[data-cy=titulo]').type('<script>alert(1)</script>')
    cy.get('[data-cy=btn-salvar-compromisso]').click()
    cy.contains('O título contém caracteres não permitidos').should('be.visible')
  })

  it('mostra o seletor de responsável (com "você") para Síndico/Gestor do prédio', () => {
    cy.mount(CompromissoForm, { props: { ...props, valoresIniciais: { predioId: 1 } } })
    cy.get('[data-cy=responsavel]').should('be.visible')
    cy.get('[data-cy=responsavel]').click()
    cy.get('.v-list-item').should('contain', 'Luciano Souza (você)').and('contain', 'Ana Souza')
  })

  it('esconde o responsável e não o envia quando o papel no prédio é Colaborador (RN20)', () => {
    cy.mount(CompromissoForm, {
      props: { ...props, valoresIniciais: { predioId: 3 }, onSubmit: cy.spy().as('onSubmit') },
    })
    cy.get('[data-cy=responsavel]').should('not.exist')
    cy.get('[data-cy=titulo]').type('Trocar lâmpada')
    cy.get('[data-cy=data]').type('2099-02-18')
    cy.get('[data-cy=horario]').type('09:00')
    cy.get('[data-cy=btn-salvar-compromisso]').click()

    cy.get('@onSubmit').should('have.been.called')
    cy.get('@onSubmit').its('lastCall.args.0.responsavelUsuarioId').should('be.undefined')
  })

  it('avisa o prédio escolhido para a view carregar a equipe', () => {
    cy.mount(CompromissoForm, {
      props: {
        ...props,
        valoresIniciais: { predioId: 1 },
        onPredioAlterado: cy.spy().as('onPredioAlterado'),
      },
    })
    cy.get('@onPredioAlterado').should('have.been.calledWith', 1)
  })

  it('em modo edição pré-preenche, troca o rótulo e só oferece prédios onde pode mover (RN24)', () => {
    cy.mount(CompromissoForm, {
      props: {
        ...props,
        modoEdicao: true,
        valoresIniciais: {
          predioId: 1,
          titulo: 'Reunião',
          dataCompromisso: '2099-02-18',
          horarioCompromisso: '18:30',
          nomePrestadorServico: '',
          detalhes: '',
          responsavelUsuarioId: 2,
        },
      },
    })
    cy.get('[data-cy=titulo] input').should('have.value', 'Reunião')
    cy.get('[data-cy=btn-salvar-compromisso]').should('contain', 'Salvar alterações')

    cy.get('[data-cy=predio]').click()
    cy.get('.v-list-item').should('contain', 'Edifício Nuvem').and('not.contain', 'Edifício Praiol')
  })

  it('trava o prédio na edição de um compromisso onde o papel é Colaborador (RN24)', () => {
    cy.mount(CompromissoForm, {
      props: {
        ...props,
        modoEdicao: true,
        valoresIniciais: {
          predioId: 3,
          titulo: 'Trocar lâmpada',
          dataCompromisso: '2099-02-18',
          horarioCompromisso: '09:00',
        },
      },
    })
    cy.get('[data-cy=predio] input').should('be.disabled')
  })

  it('mantém um responsável que já saiu da equipe, identificado, em vez de trocá-lo (RN22)', () => {
    cy.mount(CompromissoForm, {
      props: {
        ...props,
        modoEdicao: true,
        responsavelInicial: { usuarioId: 9, nome: 'Carlos Mendes' },
        valoresIniciais: {
          predioId: 1,
          titulo: 'Reunião',
          dataCompromisso: '2099-02-18',
          horarioCompromisso: '18:30',
          responsavelUsuarioId: 9,
        },
      },
    })
    cy.get('[data-cy=responsavel]').should('contain', 'Carlos Mendes (fora da equipe)')
  })
})
