# Backend — euSíndico

API REST desenvolvida em **ASP.NET Core (.NET 10)**, com **Entity Framework Core** + **MySQL** e arquitetura em camadas, conforme definido no [RFC](../documentation/RFC/RFC.md).

## Documentação

- **[Getting Started](documentation/GETTING_STARTED.md)** — pré-requisitos, como rodar o projeto localmente, configuração de User Secrets, migrations e diferenças entre ambientes.
- **[Architecture](documentation/ARCHITECTURE.md)** — responsabilidades de cada camada (Api, Application, Domain, Infrastructure), suas dependências e o fluxo de uma requisição.

## Resumo rápido

```bash
cd euSindico.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=eusindico;User=root;Password=SUA_SENHA;"
dotnet run
```

Detalhes completos em [Getting Started](documentation/GETTING_STARTED.md).
