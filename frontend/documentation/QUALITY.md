# Qualidade de código — Frontend euSíndico

Este documento descreve as medidas adotadas para manter a qualidade do código do frontend, equivalente à seção "Qualidade de código e observabilidade" do [GETTING_STARTED.md do backend](../../backend/documentation/GETTING_STARTED.md).

## Sumário

1. [Ferramentas](#1-ferramentas)
2. [Scripts npm padrão](#2-scripts-npm-padrão)
3. [SonarCloud](#3-sonarcloud)
4. [CI (frontend-ci.yml)](#4-ci-frontend-ciyml)
5. [Convenções de código](#5-convenções-de-código)
6. [Revisão de código](#6-revisão-de-código)
7. [Acessibilidade](#7-acessibilidade)
8. [Pendências conhecidas](#8-pendências-conhecidas)

## 1. Ferramentas

| Ferramenta | Papel |
|---|---|
| **oxlint** | Lint rápido (Rust) rodado antes do ESLint (`lint:oxlint`, com `--fix`) — pega uma primeira camada de problemas quase instantaneamente; o scaffold oficial do Vue 3 (`create-vue`) já inclui os dois lado a lado. |
| **ESLint** (`typescript-eslint` + `eslint-plugin-vue` + `eslint-plugin-cypress`) | Lint estático — erros de lógica, regras de estilo, regras de segurança (ex: `vue/no-v-html`, ver [SECURITY.md](SECURITY.md)) |
| **Prettier** | Formatação automática, sem debate de estilo em code review |
| **vue-tsc** | Checagem de tipos TypeScript em arquivos `.vue` (o `tsc` puro não entende SFC). Rodado em modo `--build` (`vue-tsc --build`), já que `tsconfig.json` usa *project references* (`tsconfig.app.json`/`tsconfig.node.json`/`tsconfig.vitest.json`) — `--noEmit` isolado não é suficiente nesse layout. |
| **Zod** | Validação de schema em runtime — pega divergências entre o DTO real da API e o que o frontend espera, algo que o TypeScript sozinho não detecta (ver [ARCHITECTURE.md](ARCHITECTURE.md)) |
| **Vitest** + **Cypress** | Testes — detalhado em [TEST.md](TEST.md) |
| **SonarCloud** | Análise estática contínua (duplicação, complexidade, vulnerabilidades, cobertura) |

## 2. Scripts npm padrão

```json
{
  "scripts": {
    "dev": "vite",
    "dev:cypress": "cross-env CYPRESS=true vite",
    "build": "run-p type-check \"build-only {@}\" --",
    "type-check": "vue-tsc --build",
    "lint": "run-s \"lint:*\"",
    "lint:oxlint": "oxlint . --fix",
    "lint:eslint": "eslint . --fix --cache --max-warnings=0",
    "format": "prettier --write --experimental-cli src/",
    "test:unit": "vitest run",
    "test:unit:watch": "vitest",
    "cypress:open": "cypress open",
    "cypress:run:component": "cypress run --component",
    "cypress:run:e2e": "start-server-and-test dev:cypress http://localhost:5173 'cypress run --e2e'"
  }
}
```

`--max-warnings=0` no ESLint garante que nenhum warning passe despercebido em CI — ou o código está de acordo com as regras, ou o PR não passa. `build` roda a checagem de tipos em paralelo com o build via Vite (`run-p`, do `npm-run-all2`) — se `type-check` falhar, o build falha junto. `cypress:run:e2e` sobe o dev server do Vite (porta padrão `5173`) antes de rodar as specs (`start-server-and-test`), já que testes E2E precisam de um servidor real no ar, diferente dos testes de componente — usar a porta padrão do dev server (em vez do preview de build em `4173`) mantém consistência com a origem já liberada no CORS do backend (`http://localhost:5173`, ver [SECURITY.md](SECURITY.md), seção 7) e com o restante da documentação (ver [GETTING_STARTED.md](GETTING_STARTED.md)).

**Por que `dev:cypress` em vez de `dev` puro:** o painel do Vue DevTools (`vite-plugin-vue-devtools`, ativo em `npm run dev`) se reposiciona na tela e pode cobrir um elemento no meio de uma ação do Cypress, causando falha intermitente do tipo "element is being covered by another element" em `cy.type()`/`cy.click()` — não é um problema do teste, é o painel sobrepondo o DOM em timings imprevisíveis. `CYPRESS=true` desliga o plugin só nesse script (ver [vite.config.ts](../vite.config.ts)); `cross-env` existe só pra essa variável funcionar igual no Windows (`cmd.exe` não entende `VAR=valor comando`) e no CI (Linux).

## 3. SonarCloud

Mesma plataforma já usada pelo backend ([backend-ci.yml](../../.github/workflows/backend-ci.yml)), com um projeto próprio para o frontend (chave `pdrollucas_euSindico-frontend`, organização `pdrollucas`). Configuração em `sonar-project.properties` na raiz de `frontend/`:

```properties
sonar.projectKey=pdrollucas_euSindico-frontend
sonar.organization=pdrollucas
sonar.sources=src
sonar.tests=src,cypress
sonar.test.inclusions=**/*.spec.ts,cypress/**
sonar.javascript.lcov.reportPaths=coverage/lcov.info,cypress-coverage/lcov.info
sonar.typescript.tsconfigPath=tsconfig.json
```

A análise cobre: duplicação de código, complexidade ciclomática, *code smells*, vulnerabilidades conhecidas (ex: uso de `v-html` sem sanitização, detectado tanto pelo ESLint quanto pelas regras de segurança do próprio Sonar) e cobertura de testes (alimentada pelos relatórios `lcov` do Vitest e do Cypress, ver [TEST.md](TEST.md), seção 7).

### Quality Gate

O projeto usa o **Sonar Way** (padrão do plano gratuito do SonarCloud — não há Quality Gate customizado configurado). As duas condições relevantes:

- **Coverage on New Code ≥ 80%** — cobertura combinada (Vitest + Cypress) das linhas/branches alteradas desde a análise de referência, não da base de código inteira.
- **Duplicated Lines on New Code ≤ 3%**.

A **New Code Definition** do projeto está como **"Number of Days: 30"** (`Project Settings → New Code`, na UI do SonarCloud — não é algo configurável por arquivo no repositório). Isso significa que qualquer código alterado/adicionado nos últimos 30 dias entra na conta dos 80% — na prática, durante a fase ativa de desenvolvimento, isso cobre praticamente tudo que for tocado.

Ponto de atenção prático: na primeira análise de um projeto (sem uma análise anterior pra comparar), o Sonar usa a análise mais antiga disponível como referência — ou seja, rodar o scanner duas vezes seguidas sem alterar nada entre elas dá "0 lines to cover" em New Code, e o gate passa trivialmente (não há como uma métrica sobre um conjunto vazio falhar). Isso já foi observado na prática ao configurar o projeto — não é bug, é o comportamento esperado.

## 4. CI (frontend-ci.yml)

Espelha a estrutura do [backend-ci.yml](../../.github/workflows/backend-ci.yml) existente, adaptado para Node.js:

```yaml
name: Frontend CI

on:
  push:
    branches: [main, develop]
    paths:
      - "frontend/**"
      - ".github/workflows/frontend-ci.yml"
  pull_request:
    branches: [main, develop]
    paths:
      - "frontend/**"
      - ".github/workflows/frontend-ci.yml"

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - uses: actions/setup-node@v4
        with:
          node-version: "22.x"
          cache: "npm"
          cache-dependency-path: frontend/package-lock.json

      - run: npm ci
        working-directory: frontend

      - run: npm run lint
        working-directory: frontend

      - run: npm run type-check
        working-directory: frontend

      - run: npm run build
        working-directory: frontend

      - run: npm run test:unit -- --coverage
        working-directory: frontend

      - run: npm run cypress:run:component
        working-directory: frontend

      - run: npm run cypress:run:e2e
        working-directory: frontend

      - uses: actions/setup-java@v4
        with:
          java-version: "17"
          distribution: "zulu"

      - name: SonarCloud Scan
        uses: SonarSource/sonarcloud-github-action@master
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN_FRONTEND }}
        with:
          projectBaseDir: frontend

      - name: Publicar vídeos/screenshots do Cypress (em falha)
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: cypress-results
          path: |
            frontend/cypress/videos
            frontend/cypress/screenshots
```

O projeto do frontend no SonarCloud foi criado manualmente (o fluxo de "importar repositório" só permite vincular um repositório do GitHub a um projeto por vez, e o repositório já estava vinculado ao projeto do backend — ver seção sobre monorepo). Por isso o token gerado é próprio desse projeto (escopo restrito), diferente do `SONAR_TOKEN` que o backend usa — guardado no secret `SONAR_TOKEN_FRONTEND` do GitHub. `projectKey`/`organization` ficam em [sonar-project.properties](../sonar-project.properties).

## 5. Convenções de código

- Componentes: `PascalCase.vue`, sempre `<script setup lang="ts">`.
- Composables: prefixo `use` (ex: `useCompromissoFiltros.ts`).
- Stores Pinia: prefixo `use` + sufixo `Store` (ex: `useAuthStore`).
- Um componente por arquivo; sem lógica de negócio em `components/` (ver [ARCHITECTURE.md](ARCHITECTURE.md), seção 2).
- Commits seguindo o padrão já usado no histórico do repositório (`tipo(escopo): descrição`, ex: `feat(frontend-auth): implementa tela de login`), mesma convenção observada nos commits do backend.

## 6. Revisão de código

- **Fluxo atual (desenvolvimento solo):** commits vão direto para `develop` enquanto houver um único desenvolvedor ativo no projeto — abrir e "auto-revisar" um PR para cada commit não agrega revisão nenhuma e só gera ruído no histórico. Um Pull Request é aberto especificamente para promover `develop` → `main` quando um fluxo completo (Autenticação, Prédios, Compromissos etc.) estiver pronto e validado — funciona como o ponto de corte de release, não como gate de revisão por commit.
- Assim que o projeto passar a ter outro(s) colaborador(es), a prática deve migrar para PR obrigatório também em `develop` (com revisão humana de verdade) — fica registrado aqui como o gatilho da mudança, não como algo a implementar desde já.
- CI (lint + type-check + build + testes + Sonar) precisa estar verde antes de qualquer merge para `main`, independente de quantos desenvolvedores existam.
- Sonar Quality Gate como critério objetivo adicional à revisão humana quando ela existir (mesmo padrão já adotado no backend).

## 7. Acessibilidade

O Vuetify já implementa boa parte das práticas básicas de acessibilidade (contraste, foco visível, `aria-*` em componentes interativos) — usar os componentes prontos da biblioteca em vez de reimplementar elementos de UI do zero (botões, inputs, diálogos) é, também, uma decisão de acessibilidade, não só de produtividade (RFC 5.4). Cuidados adicionais ficam a cargo de quem implementa cada tela: `alt` em imagens, `label` associado a todo input, e navegação por teclado testável nos fluxos principais.

## 8. Pendências conhecidas

- **Observabilidade do frontend** (erros em produção, ex: Sentry) ainda não definida — o backend já tem OpenTelemetry + Grafana Cloud ([GETTING_STARTED.md do backend](../../backend/documentation/GETTING_STARTED.md)); o equivalente do lado do frontend fica registrado como item futuro, não bloqueante.
- **Hooks de pre-commit** (`husky` + `lint-staged`, rodando lint/format antes de cada commit local) não configurados ainda — hoje a garantia de qualidade depende só do CI. Pode ser adicionado depois sem impacto em nada já implementado.
