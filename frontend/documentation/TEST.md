# Testes — Frontend euSíndico

Este documento descreve como os testes do frontend são organizados. Segue o mesmo princípio já registrado no backend ([GETTING_STARTED.md do backend](../../backend/documentation/GETTING_STARTED.md), seção "Rodando os testes"): **toda funcionalidade nova (view, componente, store, composable, service ou schema) deve vir acompanhada dos testes correspondentes** antes de um PR ser aberto.

Duas ferramentas, cada uma no que faz melhor — mesmo combo sugerido pelo scaffold oficial do Vue 3 (`create-vue`):

- **Vitest** para testes unitários: funções puras, composables, stores e schemas Zod. Roda em Node/jsdom, sem abrir um browser real — feedback quase instantâneo, com `vi.mock`/fake timers nativos.
- **Cypress** para testes de componente (montagem de um `.vue` real, num browser real) e testes end-to-end (fluxo completo de tela a tela).

## Sumário

1. [Camadas de teste](#1-camadas-de-teste)
2. [Organização das specs](#2-organização-das-specs)
3. [Testes unitários (Vitest)](#3-testes-unitários-vitest)
4. [Testes de componente (Cypress)](#4-testes-de-componente-cypress)
5. [Testes end-to-end (Cypress)](#5-testes-end-to-end-cypress)
6. [Mock de API com cy.intercept](#6-mock-de-api-com-cyintercept)
7. [Cobertura de código](#7-cobertura-de-código)
8. [Execução em CI](#8-execução-em-ci)
9. [O que ainda não está coberto](#9-o-que-ainda-não-está-coberto)

## 1. Camadas de teste

| Camada | Ferramenta | O que testa | Precisa de browser/backend? | Localização |
|---|---|---|---|---|
| Unitário | **Vitest** | Funções puras (`utils/`), composables, lógica de `stores/` (Pinia), schemas (`schemas/`) | Não (Node + jsdom) | Colocado junto do arquivo testado (`*.spec.ts`) |
| Componente | **Cypress** (Component Testing, `cy.mount`) | Componentes Vue isolados: props, eventos, slots, estados visuais | Browser real, sem backend (stores/services mockados) | `cypress/component/**` |
| E2E mockado | **Cypress** E2E + `cy.intercept` | Fluxos completos de tela a tela, com respostas de API stubadas | Browser real, sem backend | `cypress/e2e/**` |
| E2E completo | **Cypress** E2E, sem intercept | Integração real: frontend + backend + banco de teste | Browser real + backend + banco (ver seção 9) | Execução manual/pipeline separado |

## 2. Organização das specs

```
src/
├── utils/
│   ├── senhaValidator.ts
│   └── senhaValidator.spec.ts        # Vitest, colocado junto do arquivo testado
├── composables/
│   ├── useCompromissoFiltros.ts
│   └── useCompromissoFiltros.spec.ts
├── stores/
│   ├── authStore.ts
│   └── authStore.spec.ts
└── schemas/
    ├── usuario.schema.ts
    └── usuario.schema.spec.ts

cypress/
├── component/
│   ├── auth/LoginForm.cy.ts
│   ├── predios/PredioCard.cy.ts
│   └── compromissos/CompromissoListItem.cy.ts
├── e2e/
│   ├── landing/navegacao.cy.ts
│   ├── auth/login.cy.ts
│   ├── auth/registrar.cy.ts
│   ├── auth/esqueci-senha.cy.ts
│   ├── predios/gerenciar-predios.cy.ts
│   ├── compromissos/gerenciar-compromissos.cy.ts
│   └── relatorios/gerar-relatorio.cy.ts
├── fixtures/
│   ├── usuario.json
│   ├── predios.json
│   └── compromissos.json
└── support/
    ├── commands.ts        # comandos customizados (ex: cy.login())
    └── component.ts / e2e.ts
```

Testes Vitest ficam **colocados** junto do código-fonte (convenção `*.spec.ts` ao lado do arquivo) — mais fácil de achar o teste de algo e de notar quando algo mudou sem o teste acompanhar. Testes Cypress ficam centralizados em `cypress/`, já que não pertencem a um único arquivo de código, e sim a um fluxo de telas.

## 3. Testes unitários (Vitest)

Cobrem tudo que é lógica pura, sem necessidade de renderizar uma árvore de componentes:

```ts
// src/utils/senhaValidator.spec.ts
import { describe, it, expect } from 'vitest'
import { senhaEhForte } from './senhaValidator'

describe('senhaEhForte', () => {
  it('rejeita senha sem caractere especial', () => {
    expect(senhaEhForte('Abc12345')).toBe(false)
  })

  it('aceita senha que atende RNF04', () => {
    expect(senhaEhForte('Abc12345!')).toBe(true)
  })
})
```

Essas regras devem ficar sincronizadas com as equivalentes no backend (`SenhaForteValidator`, `EmailValidator`, `NomeValidator` — ver [SECURITY.md do backend](../../backend/documentation/SECURITY.md), seção 3) — um teste que falhe aqui é sinal de que as duas pontas divergiram.

**Schemas Zod (`schemas/`) também são testados aqui** — o próprio propósito do schema é rejeitar um formato inesperado, então o teste cobre exatamente isso:

```ts
// src/schemas/usuario.schema.spec.ts
import { describe, it, expect } from 'vitest'
import { usuarioSchema } from './usuario.schema'

describe('usuarioSchema', () => {
  it('aceita um payload válido', () => {
    expect(() => usuarioSchema.parse({ id: 1, nome: 'João', email: 'joao@ex.com' })).not.toThrow()
  })

  it('rejeita payload com campo faltando (API mudou/quebrou o contrato)', () => {
    expect(() => usuarioSchema.parse({ id: 1, nome: 'João' })).toThrow()
  })
})
```

Stores Pinia (lógica de `actions`/`getters`, sem depender de um componente montado) e composables seguem o mesmo padrão, instanciando a store/composable diretamente no teste.

**Interceptors do Axios (`plugins/axios.ts`)** são um caso à parte: mockar `axios` inteiro (`vi.mock('axios')`) não testa os interceptors de verdade, já que eles são registrados na instância real. Em vez disso, usamos `axios-mock-adapter`, que substitui só a camada de transporte HTTP — os interceptors continuam rodando normalmente:

```ts
// src/plugins/axios.spec.ts
import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import MockAdapter from 'axios-mock-adapter'
import api from './axios'
import { useAuthStore } from '@/stores/authStore'

describe('plugins/axios', () => {
  let mock: MockAdapter

  beforeEach(() => {
    setActivePinia(createPinia())
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('em 401 numa rota protegida, renova o token e repete a requisição original', async () => {
    const authStore = useAuthStore()
    authStore.setAccessToken('token-expirado')

    mock.onGet('/predios').replyOnce(401).onGet('/predios').replyOnce(200, [])
    mock.onPost('/auth/refresh').replyOnce(200, { accessToken: 'token-novo' })

    await api.get('/predios')

    expect(authStore.accessToken).toBe('token-novo')
  })
})
```

## 4. Testes de componente (Cypress)

Cada componente reutilizável (`src/components/`) e cada componente de tela mais complexo tem um spec que o monta isoladamente (`cy.mount`), com stores e services mockados via `createTestingPinia` (ou stub manual), verificando renderização condicional, eventos emitidos e interações do usuário:

```ts
// cypress/component/auth/LoginForm.cy.ts
import LoginForm from '../../../src/components/auth/LoginForm.vue'

describe('LoginForm', () => {
  it('emite "submit" com email e senha preenchidos', () => {
    // O spy precisa ser passado como prop no mount — emits em Vue 3 viram props `onNomeDoEvento`.
    // Sem isso, o alias @onSubmit não existe e o teste falha com "cy.wait() timed out".
    cy.mount(LoginForm, { props: { onSubmit: cy.spy().as('onSubmit') } })
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-entrar]').click()
    cy.get('@onSubmit').should('have.been.calledWith', {
      email: 'sindico@exemplo.com',
      senha: 'SenhaForte1!',
    })
  })

  it('exibe erro de validação com email inválido', () => {
    cy.mount(LoginForm)
    cy.get('[data-cy=email]').type('nao-e-um-email')
    cy.get('[data-cy=btn-entrar]').click()
    cy.contains('E-mail inválido').should('be.visible')
  })
})
```

Elementos-alvo de teste usam sempre atributos `data-cy` dedicados (nunca classes CSS ou texto visível) — evita que uma mudança de estilo ou copy quebre um teste que não tem nada a ver com o que está sendo validado.

## 5. Testes end-to-end (Cypress)

Cobrem os fluxos descritos no [RFC](../../documentation/RFC/RFC.md), seções 2.2 (casos de uso) e 3.1 (fluxo principal do usuário), e os fluxos de autenticação detalhados no [AUTHENTICATION.md do backend](../../backend/documentation/AUTHENTICATION.md). Cada spec percorre telas de verdade (roteamento real do Vue Router), mas com as respostas de API stubadas via `cy.intercept` (seção 6) — determinístico e rápido, sem depender de um backend no ar.

Exemplo, cobrindo o fluxo alternativo "Falha no login" do RFC (seção 3.2):

```ts
// cypress/e2e/auth/login.cy.ts
describe('Login', () => {
  it('redireciona para /home após login válido', () => {
    cy.intercept('POST', '**/auth/login', { statusCode: 200, fixture: 'usuario.json' }).as('login')
    cy.visit('/login')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaForte1!')
    cy.get('[data-cy=btn-entrar]').click()
    cy.wait('@login')
    cy.url().should('include', '/home')
  })

  it('exibe mensagem genérica em credenciais inválidas (RFC 3.2)', () => {
    cy.intercept('POST', '**/auth/login', { statusCode: 401, body: { title: 'Credenciais inválidas', status: 401 } }).as('loginFalho')
    cy.visit('/login')
    cy.get('[data-cy=email]').type('sindico@exemplo.com')
    cy.get('[data-cy=senha]').type('SenhaErrada1!')
    cy.get('[data-cy=btn-entrar]').click()
    cy.wait('@loginFalho')
    cy.get('[data-cy=erro-login]').should('be.visible')
    cy.url().should('include', '/login')
  })
})
```

## 6. Mock de API com cy.intercept

Fixtures em `cypress/fixtures/*.json` espelham os DTOs de resposta do backend, validados contra os mesmos `schemas/` Zod usados em produção (ver [ARCHITECTURE.md](ARCHITECTURE.md#2-estrutura-de-pastas)). Sempre que um DTO do backend mudar, o schema e a fixture correspondentes precisam ser atualizados — o mesmo cuidado de sincronização já mencionado na seção 3.

## 7. Cobertura de código

- **Vitest:** cobertura nativa via `@vitest/coverage-v8`, saída em `lcov`.
- **Cypress:** instrumentação via `vite-plugin-istanbul` + `@cypress/code-coverage`, cobrindo specs de componente e E2E, saída em `lcov` também.
- Os dois relatórios `lcov` (Vitest + Cypress) são apontados juntos para o SonarCloud (`sonar.javascript.lcov.reportPaths=coverage/lcov.info,cypress-coverage/lcov.info`) — detalhado em [QUALITY.md](QUALITY.md).

## 8. Execução em CI

O workflow `frontend-ci.yml` (ver [QUALITY.md](QUALITY.md)) roda, a cada push/PR que toque `frontend/**`:

```bash
npm run lint
npm run type-check
npm run build
npm run test:unit -- --coverage
npm run cypress:run:component
npm run cypress:run:e2e
```

(`cypress:run:e2e` já sobe o dev server do Vite (script `dev:cypress`, porta padrão `5173`) antes das specs via `start-server-and-test` — não precisa de um passo manual para servir a aplicação. A porta padrão do dev server é usada em vez do preview de build (`4173`) para ficar consistente com a origem já liberada no CORS do backend, ver [GETTING_STARTED.md](GETTING_STARTED.md). `dev:cypress` existe em vez do `dev` puro porque o painel do Vue DevTools cobre elementos da tela e causa falha intermitente do Cypress — detalhado em [QUALITY.md](QUALITY.md), seção 2.)

Em caso de falha, vídeos e screenshots de cada spec do Cypress (gerados automaticamente) são publicados como artifact do workflow — mesmo padrão já usado no `backend-ci.yml` para os resultados de teste (`.trx`).

## 9. O que ainda não está coberto

**E2E completo (integração real):** rodar o Cypress contra o frontend buildado + backend real + um banco de dados MySQL de teste (via `docker-compose`, por exemplo) ainda não está configurado. Isso exigiria orquestrar três serviços na pipeline de CI (banco, backend, frontend), o que é uma complexidade maior do que o E2E mockado da seção 5. Fica registrado como evolução futura, no mesmo espírito das pendências "podem esperar o gatilho correspondente" do backend — não bloqueia o início do desenvolvimento, mas deveria existir antes de marcos importantes de entrega (ex: antes do M8 do RFC, "implantação em ambiente de produção").
