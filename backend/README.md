# Backend — euSíndico

API REST desenvolvida em **ASP.NET Core (.NET 10)**, com **Entity Framework Core** + **MySQL** e arquitetura em camadas, conforme definido no [RFC](../documentation/RFC/RFC.md).

## Documentação

- **[Getting Started](documentation/GETTING_STARTED.md)** — pré-requisitos, como rodar o projeto localmente, configuração de User Secrets, migrations e diferenças entre ambientes.
- **[Architecture](documentation/ARCHITECTURE.md)** — responsabilidades de cada camada (Api, Application, Domain, Infrastructure), suas dependências, o fluxo de uma requisição e a hospedagem planejada.
- **[Authentication](documentation/AUTHENTICATION.md)** — fluxo completo de autenticação (cadastro, login, logout, perfil, exclusão de conta, recuperação de senha), estrutura do JWT e onde cada peça mora na arquitetura.
- **[Security](documentation/SECURITY.md)** — medidas de segurança implementadas: modelo de tokens, proteção contra XSS, tratamento de erros, autorização, rate limiting, recuperação de senha e mapeamento com a seção 6.1 do RFC.

## Resumo rápido

```bash
cd euSindico.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=eusindico;User=root;Password=SUA_SENHA;"
dotnet run
```

Detalhes completos em [Getting Started](documentation/GETTING_STARTED.md).
