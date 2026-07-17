# Getting Started — Backend euSíndico

Guia para configurar o ambiente e rodar a API localmente. Se você acabou de entrar no projeto, comece por aqui.

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [MySQL Server](https://dev.mysql.com/downloads/mysql/) 8+ (local ou em um container Docker)
- Uma IDE: Visual Studio 2026 ou VS Code com a extensão C# Dev Kit

Para gerar/aplicar migrations, instale também a ferramenta de linha de comando do EF Core (uma única vez por máquina):

```bash
dotnet tool install --global dotnet-ef
```

## Clonando e restaurando

```bash
git clone <url-do-repositorio>
cd euSindico/backend
dotnet restore
dotnet build
```

## Configurando a conexão com o banco (User Secrets)

O projeto **não** guarda credenciais de banco de dados em `appsettings.json` — nada disso vai para o Git. Em desenvolvimento local, usamos o [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) do .NET, que guarda os valores fora do repositório (na sua pasta de perfil do usuário).

Cada dev precisa configurar o próprio segredo depois de clonar o projeto:

```bash
cd euSindico.Api

# só é necessário na primeira vez (o csproj já tem o UserSecretsId configurado)
dotnet user-secrets init

# defina a sua connection string local
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=eusindico;User=root;Password=SUA_SENHA;"
```

Comandos úteis do dia a dia:

```bash
dotnet user-secrets list      # ver os segredos configurados
dotnet user-secrets remove "ConnectionStrings:DefaultConnection"
dotnet user-secrets clear     # apaga todos os segredos do projeto
```

> Por que não usar `.env`? O ASP.NET Core não lê arquivos `.env` nativamente (isso é uma convenção de Node/Vite). O equivalente idiomático para segredos locais é o User Secrets.

## Rodando a API

```bash
cd euSindico.Api
dotnet run
```

O `launchSettings.json` já configura o ambiente como `Development` e abre o navegador automaticamente na documentação dos contratos (Scalar), em:

```
https://localhost:7091/scalar/v1
```

Se preferir, também dá pra rodar direto pela IDE (F5 no Visual Studio, selecionando o perfil `https`).

## Migrations do banco de dados

Sempre que uma entidade do `euSindico.Domain` ou o `AppDbContext` (em `euSindico.Infrastructure`) mudar, é preciso gerar uma nova migration:

```bash
# a partir da pasta backend/
dotnet ef migrations add NomeDaMigration --project euSindico.Infrastructure --startup-project euSindico.Api

# aplica as migrations pendentes no banco configurado no User Secrets
dotnet ef database update --project euSindico.Infrastructure --startup-project euSindico.Api
```

## Rodando os testes

O projeto usa **xUnit**, com um projeto de teste por camada em `backend/tests/`:

- `euSindico.Domain.Tests`
- `euSindico.Application.Tests`
- `euSindico.Infrastructure.Tests`
- `euSindico.Api.Tests` — testes de integração via `WebApplicationFactory`, subindo a API inteira em memória.

```bash
# a partir da pasta backend/
dotnet test
```

Toda nova funcionalidade (Service, Repository, Controller etc.) deve vir acompanhada dos testes correspondentes no projeto de teste da mesma camada.

Um workflow de CI (`.github/workflows/backend-ci.yml`) roda `dotnet build` + `dotnet test` automaticamente a cada push/PR que toque na pasta `backend/`.

## Diferença entre ambientes (Development vs. Production)

A configuração segue a ordem de precedência padrão do ASP.NET Core (a última que aparece na lista abaixo sobrescreve as anteriores):

1. `appsettings.json` — configuração base, comum a todos os ambientes, sem segredos.
2. `appsettings.{Environment}.json` — overrides específicos do ambiente (ex: `appsettings.Development.json`), definido pela variável `ASPNETCORE_ENVIRONMENT`.
3. **User Secrets** — só carregado quando `ASPNETCORE_ENVIRONMENT=Development`. Nunca existe em produção.
4. **Variáveis de ambiente** — é assim que os segredos chegam em produção (ex: `ConnectionStrings__DefaultConnection`, usando `__` no lugar de `:`). Em produção, isso deve vir de variáveis de ambiente do host ou de um secret manager (ex: AWS Secrets Manager/Parameter Store, já que o projeto usa AWS S3).

Localmente, o `ASPNETCORE_ENVIRONMENT` já vem definido como `Development` pelos profiles do `launchSettings.json` — você não precisa configurar nada manualmente para rodar.

## Qualidade de código e observabilidade

- **Testes:** obrigatórios para toda funcionalidade nova (ver seção acima).
- **Análise estática/segurança:** **SonarCloud** integrado ao `.github/workflows/backend-ci.yml` via SonarScanner for .NET.
- **Observabilidade/monitoramento:** **OpenTelemetry + Grafana Cloud**.

### Configurando o OpenTelemetry (opcional em desenvolvimento)

A `euSindico.Api` já vem instrumentada com OpenTelemetry (tracing de HTTP, EF Core e chamadas HTTP de saída, mais métricas básicas). A exportação para um backend **só é ativada se `Observability:OtlpEndpoint` estiver configurado** — sem isso, a aplicação roda normalmente e a telemetria simplesmente não é coletada. Não é necessário configurar nada para desenvolver localmente.

Quando você tiver uma conta no [Grafana Cloud](https://grafana.com/products/cloud/) (tier gratuito, com retenção de 14 dias), configure via User Secrets:

```bash
cd euSindico.Api
dotnet user-secrets set "Observability:OtlpEndpoint" "https://otlp-gateway-<sua-regiao>.grafana.net/otlp"
dotnet user-secrets set "Observability:OtlpHeaders" "Authorization=Basic <token-base64-da-instancia>"
```

Em produção, os mesmos valores devem vir de variáveis de ambiente (`Observability__OtlpEndpoint`, `Observability__OtlpHeaders`), nunca de arquivo versionado.

> Nota: o pacote `OpenTelemetry.Instrumentation.EntityFrameworkCore` ainda está em versão *beta* — é amplamente usado em produção por outros times, mas vale acompanhar releases estáveis futuras.

## Documentação dos contratos (Scalar)

A API expõe o documento OpenAPI nativo em `/openapi/v1.json` e uma interface interativa (Scalar) em `/scalar/v1`, disponível apenas em ambiente `Development`.

## Ver também

- [ARCHITECTURE.md](ARCHITECTURE.md) — responsabilidades de cada camada (Api, Application, Domain, Infrastructure) e o fluxo de uma requisição.
- [RFC](../../documentation/RFC/RFC.md) — visão de produto, requisitos e modelo de dados completo.
