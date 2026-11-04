# Imagem de produção da euSindico.Api (ver deploy/README.md). Contexto de build: a pasta backend/.
# A mesma imagem roda a API e, com o argumento --migrate, só as migrations (serviço "migrate" do compose).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore em camada própria: só refaz quando algum .csproj muda.
COPY euSindico.Domain/euSindico.Domain.csproj euSindico.Domain/
COPY euSindico.Application/euSindico.Application.csproj euSindico.Application/
COPY euSindico.Infrastructure/euSindico.Infrastructure.csproj euSindico.Infrastructure/
COPY euSindico.Api/euSindico.Api.csproj euSindico.Api/
RUN dotnet restore euSindico.Api/euSindico.Api.csproj

COPY euSindico.Domain/ euSindico.Domain/
COPY euSindico.Application/ euSindico.Application/
COPY euSindico.Infrastructure/ euSindico.Infrastructure/
COPY euSindico.Api/ euSindico.Api/
RUN dotnet publish euSindico.Api/euSindico.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# tzdata: o fuso America/Sao_Paulo (TimeZoneId) depende da base de fusos do sistema — ver GETTING_STARTED.md.
# curl: usado só pelo healthcheck do container (GET /health).
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Usuário sem privilégios que já vem na imagem oficial do .NET.
USER $APP_UID

ENTRYPOINT ["dotnet", "euSindico.Api.dll"]
