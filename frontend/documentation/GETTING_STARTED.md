# Getting Started — Frontend euSíndico

Guia para configurar o ambiente e rodar o frontend localmente. Se você acabou de entrar no projeto, comece por aqui — e, antes, dê uma olhada no [GETTING_STARTED.md do backend](../../backend/documentation/GETTING_STARTED.md), já que o frontend depende da API rodando localmente para qualquer funcionalidade além da landing page.

## Pré-requisitos

- [Node.js](https://nodejs.org/) 22.x (LTS) ou superior
- npm (instalado junto com o Node.js)
- Uma IDE: VS Code, com a extensão [Vue - Official (Volar)](https://marketplace.visualstudio.com/items?itemName=Vue.volar) — **desative a extensão Vetur antiga**, se instalada, ela conflita com o Volar
- Backend rodando localmente em `https://localhost:7091` (ver [GETTING_STARTED.md do backend](../../backend/documentation/GETTING_STARTED.md))

## Clonando e instalando

```bash
git clone <url-do-repositorio>
cd euSindico/frontend
npm install
```

## Configurando as variáveis de ambiente

O projeto usa variáveis de ambiente do Vite (prefixo `VITE_`, embutidas no build — ver [SECURITY.md](SECURITY.md), seção "Variáveis de ambiente"). Copie o modelo e ajuste para o seu ambiente local:

```bash
cp .env.example .env.local
```

```bash
# .env.local
VITE_API_BASE_URL=https://localhost:7091
```

`.env.local` nunca é versionado (já vem no `.gitignore`) — cada dev aponta para a própria instância local do backend.

> **Sobre CORS:** o backend já libera a origem `http://localhost:5173` por padrão em desenvolvimento (`Cors:AllowedOrigins` em `appsettings.Development.json`, ver [SECURITY.md](SECURITY.md), seção 7) — chamadas diretas do dev server à API funcionam sem proxy. Se você rodar o frontend numa porta diferente da padrão do Vite, adicione a nova origem em `Cors:AllowedOrigins` no `appsettings.Development.json` do backend (ou via User Secrets) antes de testar.

## Rodando o dev server

```bash
npm run dev
```

Abre em `http://localhost:5173`. Hot Module Replacement (HMR) já vem configurado pelo Vite — qualquer alteração em `.vue`/`.ts` reflete na tela sem recarregar a página.

## Lint, checagem de tipos e build

```bash
npm run lint          # ESLint
npm run type-check    # vue-tsc --build
npm run build         # gera frontend/dist, pronto para deploy (Amplify)
```

Todos os três rodam também no CI (`frontend-ci.yml`, ver [QUALITY.md](QUALITY.md)) — rodá-los localmente antes de abrir um PR evita surpresa.

## Rodando os testes (Vitest + Cypress)

```bash
npm run test:unit:watch           # Vitest — testes unitários (utils, composables, stores, schemas), modo watch
npm run test:unit                 # Vitest — mesma coisa, execução única (usado no CI)
npm run cypress:open              # modo interativo — escolhe entre Component/E2E na UI
npm run cypress:run:component     # testes de componente, headless
npm run cypress:run:e2e           # testes end-to-end, headless
```

Detalhes de como os testes são organizados e o que cada camada cobre estão em [TEST.md](TEST.md). Toda nova view, componente, store, composable ou service deve vir acompanhado do teste correspondente antes do PR ser aberto — mesma regra já aplicada no backend.

## Diferença entre ambientes (Development vs. Production)

| | Desenvolvimento | Produção |
|---|---|---|
| Onde roda | `npm run dev` (Vite dev server), local | AWS Amplify Hosting, build estático |
| `VITE_API_BASE_URL` | `.env.local` (não versionado), apontando para `https://localhost:7091` | Variável de ambiente configurada no Amplify Console, apontando para a URL do AWS App Runner |
| HTTPS | Não (dev server em HTTP; ver [SECURITY.md](SECURITY.md), seção 6) | Sim, forçado automaticamente pelo Amplify |
| CORS no backend | `http://localhost:5173` já liberado (`appsettings.Development.json`) | Falta liberar o domínio do Amplify (pendência, ver [SECURITY.md](SECURITY.md)) |

## Ver também

- [ARCHITECTURE.md](ARCHITECTURE.md) — stack, estrutura de pastas, módulos e roteamento.
- [AUTHENTICATION.md](AUTHENTICATION.md) — onde o token vive, renovação automática e logout.
- [SECURITY.md](SECURITY.md) — XSS, CSRF, CORS, upload de arquivos, variáveis de ambiente.
- [TEST.md](TEST.md) — como os testes são organizados e executados.
- [QUALITY.md](QUALITY.md) — lint, SonarCloud, CI e convenções de código.
- [Documentação do backend](../../backend/documentation/) — arquitetura, segurança, autenticação e getting started da API.
- [RFC](../../documentation/RFC/RFC.md) — visão de produto, requisitos e modelo de dados completo.
