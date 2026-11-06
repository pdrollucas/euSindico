# euSíndico

Aplicação web **mobile-first** para síndicos profissionais organizarem o dia a dia dos condomínios que administram: compromissos, prédios, documentos e relatórios em um só lugar.

Projeto de TCC do curso de Engenharia de Software da Católica SC. Problema, requisitos, regras de negócio e arquitetura estão no **[RFC](documentation/RFC/RFC.md)**.

## Foco em celular

O euSíndico foi pensado para ser usado **no celular**, durante as visitas e a rotina do síndico (RNF08 do RFC). Todas as telas são desenhadas primeiro para a coluna estreita do celular, e esse é o fluxo de referência do projeto: navegação pela Home, ações em botões flutuantes, filtros em folhas que sobem da parte de baixo da tela.

Em telas a partir de 840px (tablet deitado e desktop), a mesma aplicação se adapta: menu lateral fixo, listas em grade e formulários centralizados. No login e no cadastro, um QR code leva a pessoa para o celular. A adaptação ao desktop nunca altera o fluxo mobile. Detalhes em [frontend/documentation/DESIGN.md](frontend/documentation/DESIGN.md), seção "Layout responsivo".

## O que a aplicação faz

- **Prédios:** cadastro dos condomínios administrados.
- **Compromissos:** agenda de reuniões, visitas técnicas e inspeções, com responsável, conclusão e filtros.
- **Planejamentos futuros:** obras e melhorias planejadas para cada prédio, com orçamento.
- **Relatórios mensais:** geração em PDF das atividades de um prédio no mês.
- **Documentos:** atas de reunião e normas internas de cada prédio.
- **Equipe:** convite de funcionários, com os perfis Síndico, Gestor e Colaborador.
- **Conta:** perfil, troca de e-mail e senha, recuperação de senha, suporte e exclusão de conta.

Fora do escopo (RFC, seção 2.6): gestão financeira do condomínio, comunicação com moradores, controle de portaria e aplicativo nativo.

## Stacks e ferramentas

### Frontend

| Ferramenta | Uso |
|---|---|
| Vue.js 3 + TypeScript | SPA |
| Vite | Build e servidor de desenvolvimento |
| Vuetify 4 + Material Design Icons | Componentes, tema e layout responsivo |
| Pinia | Estado (stores) |
| Vue Router | Rotas e guarda de autenticação |
| Axios | Chamadas à API, com renovação automática do token |
| VeeValidate + Zod | Validação de formulários |

### Backend

| Ferramenta | Uso |
|---|---|
| ASP.NET Core (.NET 10) | API REST em camadas (Api, Application, Domain, Infrastructure) |
| Entity Framework Core + Pomelo | ORM e migrations para MySQL |
| MySQL 8 | Banco de dados |
| JWT + BCrypt | Autenticação (access token + refresh token em cookie HttpOnly) e hash de senha |
| FluentValidation | Validação das requisições |
| QuestPDF | Geração dos relatórios em PDF |
| MailKit | Envio de e-mails (SMTP) |
| AWS SDK (S3) | Armazenamento de documentos e relatórios |
| OpenAPI + Scalar | Documentação interativa da API |
| OpenTelemetry | Traces, métricas e logs, com exportação OTLP preparada para o Grafana Cloud |

### Testes e qualidade

| Ferramenta | Uso |
|---|---|
| xUnit + Moq | Testes do backend, por camada |
| Vitest + Vue Test Utils | Testes unitários do frontend |
| Cypress | Testes de componente e E2E do frontend |
| ESLint, Oxlint, Prettier, vue-tsc | Lint, formatação e checagem de tipos do frontend |
| SonarCloud | Análise estática e cobertura |
| GitHub Actions | CI do backend e do frontend, e deploy manual |

### Infraestrutura

| Ferramenta | Uso |
|---|---|
| Docker + Docker Compose | Imagens da API e do frontend, e orquestração em produção |
| Caddy | HTTPS automático, arquivos do frontend e proxy da API |
| AWS (EC2, ECR, S3, CloudFormation, Systems Manager) | Hospedagem, registro das imagens, arquivos, infraestrutura como código e deploy |

## Estrutura do repositório

```
euSindico/
├── backend/         # API em .NET (código, testes e documentação)
├── frontend/        # SPA em Vue (código, testes e documentação)
├── deploy/          # Docker Compose, Caddyfile, CloudFormation e scripts de produção
├── documentation/   # RFC e planos do projeto
└── .github/         # Workflows de CI e deploy
```

## Como rodar

Cada parte tem seu passo a passo completo, com pré-requisitos, configuração e diferenças entre ambientes:

- **Backend:** [backend/documentation/GETTING_STARTED.md](backend/documentation/GETTING_STARTED.md). Requer o .NET 10 SDK e o MySQL 8+.
- **Frontend:** [frontend/documentation/GETTING_STARTED.md](frontend/documentation/GETTING_STARTED.md). Requer o Node.js 22.18+ ou 24.12+.

Em resumo, com o banco e os segredos já configurados (ver os guias acima):

```bash
# API
cd backend/euSindico.Api
dotnet run

# Frontend, em outro terminal
cd frontend
npm install
npm run dev
```

## Documentação

| Onde | O que tem |
|---|---|
| [RFC](documentation/RFC/RFC.md) | Visão do produto, requisitos, regras de negócio, fluxos e arquitetura |
| [backend/README.md](backend/README.md) | Índice da documentação do backend: arquitetura, autenticação, segurança e um documento por módulo |
| [frontend/README.md](frontend/README.md) | Índice da documentação do frontend: arquitetura, design, autenticação, segurança, testes e qualidade |
| [deploy/README.md](deploy/README.md) | Hospedagem, serviços de produção e operação |

## Licença

[MIT](LICENSE)
