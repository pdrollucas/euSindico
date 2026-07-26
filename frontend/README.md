# Frontend — euSíndico

SPA desenvolvida em **Vue.js 3 + TypeScript**, com **Vuetify** e abordagem mobile-first, conforme definido no [RFC](../documentation/RFC/RFC.md).

## Documentação

- **[Getting Started](documentation/GETTING_STARTED.md)** — pré-requisitos, como rodar o projeto localmente, variáveis de ambiente e diferenças entre ambientes.
- **[Architecture](documentation/ARCHITECTURE.md)** — stack tecnológica, estrutura de pastas, módulos da aplicação, roteamento e o fluxo de uma requisição ponta a ponta.
- **[Authentication](documentation/AUTHENTICATION.md)** — onde e como o token vive no frontend: armazenamento, bootstrap de sessão, renovação automática e logout.
- **[Security](documentation/SECURITY.md)** — XSS, CSRF, CORS, upload de arquivos, variáveis de ambiente e o mapeamento com a seção 6.1 do RFC.
- **[Test](documentation/TEST.md)** — como os testes (Vitest + Cypress) são organizados e o que cada camada cobre.
- **[Quality](documentation/QUALITY.md)** — lint, SonarCloud, CI e convenções de código.

## Resumo rápido

```bash
cd frontend
npm install
cp .env.example .env.local
npm run dev
```

Detalhes completos em [Getting Started](documentation/GETTING_STARTED.md).
