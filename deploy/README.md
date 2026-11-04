# Deploy — euSíndico

Arquivos da produção do TCC: uma instância EC2 (`t4g.small`, ARM, `us-east-2`) rodando tudo em Docker Compose. Visão geral da hospedagem em [backend/documentation/ARCHITECTURE.md](../backend/documentation/ARCHITECTURE.md), seção "Hospedagem".

```
Internet ──► Caddy (80/443, HTTPS automático) ──┬─► /srv (frontend: build do Vite)    eusindicogestao.com.br
                                                ├─► redirect para o domínio sem www   www.eusindicogestao.com.br
                                                └─► api:8080 (euSindico.Api)          api.eusindicogestao.com.br
                                                        │
                                                        └─► mysql:3306 (rede interna, sem porta exposta)
```

## Arquivos

| Arquivo | O que é |
|---|---|
| [`docker-compose.yml`](docker-compose.yml) | Produção: MySQL 8.4, `migrate`, API e Caddy, com limites de memória e rotação de logs |
| [`docker-compose.local.yml`](docker-compose.local.yml) | Override para validar o compose de produção localmente, construindo as imagens a partir do código |
| [`Caddyfile`](Caddyfile) | HTTPS (Let's Encrypt), SPA, redirect do `www`, proxy da API e security headers (CSP etc.) |
| [`.env.example`](.env.example) | Modelo do `deploy/.env`, que guarda os segredos e **nunca** vai para o Git |
| [`../backend/Dockerfile`](../backend/Dockerfile) | Imagem da API (também roda as migrations com `--migrate`) |
| [`../frontend/Dockerfile`](../frontend/Dockerfile) | Imagem do frontend: build do Vite dentro da imagem oficial do Caddy |
| [`cloudformation.yaml`](cloudformation.yaml) | Infraestrutura (stack `eusindico`): EC2, Elastic IP, bucket, ECR, snapshots, role de deploy (OIDC) e teto de custo |
| [`../.github/workflows/deploy.yml`](../.github/workflows/deploy.yml) | Deploy manual: testes, build arm64, push no ECR e deploy na instância pelo Session Manager |
| [`scripts/servidor/`](scripts/servidor/) | Rodam **na instância**: `deploy.sh`, `backup.sh` e `definir-senha-smtp.sh` |
| [`scripts/parar.sh`](scripts/parar.sh), [`scripts/ligar.sh`](scripts/ligar.sh) | Rodam **na sua máquina**: backup + parada, e ligar + conferir o site |

## Como os serviços sobem

1. **`mysql`** sobe e só é considerado pronto quando responde ao `mysqladmin ping`.
2. **`migrate`** (mesma imagem da API, com `--migrate`) aplica as migrations pendentes e encerra. Se falhar, a API não sobe.
3. **`api`** sobe só depois do `migrate` terminar com sucesso. Healthcheck em `GET /health`.
4. **`caddy`** serve o frontend e faz o proxy da API, emitindo e renovando os certificados sozinho.

Redes:
- **`borda`** (`172.30.0.0/24`): Caddy e API. A API só confia em `X-Forwarded-For` vindo dela (`ReverseProxy__KnownNetworks__0`, ver [SECURITY.md do backend](../backend/documentation/SECURITY.md), seção 9).
- **`dados`** (interna): API, `migrate` e MySQL. Sem saída para a internet e sem porta publicada.

## Validação local

Pré-requisito: Docker Desktop rodando. Na validação local as imagens são construídas para a arquitetura da sua máquina; em produção, o GitHub Actions as constrói para arm64.

1. Crie um `.env` local **fora do repositório** a partir do [`.env.example`](.env.example), com `SITE_DOMAIN=localhost`, `API_IMAGE=eusindico-api:local`, `WEB_IMAGE=eusindico-web:local` e senhas geradas na hora.
2. Suba tudo:

   ```bash
   cd deploy
   docker compose -f docker-compose.yml -f docker-compose.local.yml --env-file <caminho do .env local> up --build -d
   ```

3. Confira:

   ```bash
   curl -k https://api.localhost/health      # Healthy
   curl -kI https://localhost                # 200, com os security headers
   curl -kI https://www.localhost            # 301 para https://localhost
   ```

4. Derrube e apague os dados locais: `docker compose -f docker-compose.yml -f docker-compose.local.yml --env-file <...> down -v`.

Localmente, S3 e SMTP ficam vazios: gerar ou baixar relatórios e enviar e-mails respondem erro, o que é esperado.

## Operação em produção

Os comandos `aws` abaixo usam o perfil `eusindico` (faça `aws login --profile eusindico` se a sessão expirar) e rodam no Git Bash.

### Configuração única no GitHub

Em **Settings → Secrets and variables → Actions → Variables**, crie a variável `AWS_DEPLOY_ROLE_ARN` com a saída `GitHubDeployRoleArn` da stack:

```bash
aws cloudformation describe-stacks --stack-name eusindico --profile eusindico --region us-east-2 \
  --query "Stacks[0].Outputs[?OutputKey=='GitHubDeployRoleArn'].OutputValue" --output text
```

Instância, bucket e repositórios ECR são lidos das saídas da stack pelo próprio workflow.

### DNS (Registro.br)

Três registros **A** apontando para a saída `PublicIp` da stack: o próprio domínio (`@`/nome vazio), `api` e `www`. O Caddy só consegue emitir os certificados depois que o DNS aponta para a instância — faça o primeiro deploy depois disso.

### Deploy

**Actions → Deploy → Run workflow**, sempre na branch `main` (a role da AWS só aceita a `main`). O workflow roda os testes, constrói as imagens arm64, publica no ECR e manda a instância baixar os arquivos de `deploy/` daquele commit e rodar `deploy.sh`. A instância precisa estar ligada.

No **primeiro deploy**, `deploy.sh` cria `/opt/eusindico/.env` com senhas novas do MySQL e do JWT, geradas na instância (nunca passam pelo Git nem pelo GitHub). Esse arquivo é a única cópia: se a instância for perdida, as senhas vão junto (o dump do banco no S3 continua restaurável, mas com um `.env` novo os usuários precisam logar de novo).

### Senha de app do Gmail

Uma vez, depois do primeiro deploy: no console da AWS, **EC2 → Instances → `eusindico` → Connect → Session Manager → Connect**, e no terminal do navegador:

```bash
sudo bash /opt/eusindico/definir-senha-smtp.sh
```

A senha não aparece ao digitar. Sem ela, só os e-mails (recuperação de senha, convites, suporte) falham.

### Parar e ligar

```bash
bash deploy/scripts/parar.sh   # backup do banco no S3 e depois para a instância (não para se o backup falhar)
bash deploy/scripts/ligar.sh   # liga e espera https://api.eusindicogestao.com.br/health responder
```

Parada, a instância custa ~US$ 5,70/mês (disco + IP fixo). O IP não muda, então o DNS continua valendo.

### Backup e restauração

- **Snapshot diário** do disco (03:00 em Brasília, os 3 últimos), automático.
- **Dump do MySQL** em `s3://<bucket>/backups/` a cada `parar.sh` (ou `sudo bash /opt/eusindico/backup.sh` na instância). Expiram em 30 dias.

Restaurar um dump (na instância, como root, a partir de `/opt/eusindico`):

```bash
aws s3 ls s3://<bucket>/backups/ --region us-east-2
aws s3 cp s3://<bucket>/backups/<arquivo>.sql.gz - --region us-east-2 | gunzip \
  | docker compose exec -T mysql sh -c 'MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mysql -uroot'
```

### Teto de custo

Budget mensal `eusindico-mensal-20usd` com alertas por e-mail em 50%, 70%, 90% e 125%, e uma ação que **para a instância** ao passar de 125% (US$ 25) em gasto real. Os dados de custo têm atraso de horas.

## Variáveis do `deploy/.env`

Ver comentários no [`.env.example`](.env.example). Resumo:

| Variável | Uso |
|---|---|
| `SITE_DOMAIN` | Domínio do frontend; a API fica em `api.${SITE_DOMAIN}`. Também define CORS e o link de convite |
| `ACME_EMAIL` | E-mail de avisos do Let's Encrypt |
| `API_IMAGE`, `WEB_IMAGE` | Imagens no ECR, com a tag do deploy |
| `MYSQL_PASSWORD`, `MYSQL_ROOT_PASSWORD` | Senhas do MySQL (só letras e números) |
| `JWT_SECRET_KEY` | Chave do JWT (32+ caracteres) |
| `S3_BUCKET_NAME` | Bucket privado dos arquivos |
| `SMTP_USUARIO`, `SMTP_SENHA` | Conta do Gmail e a senha de app dela |
| `OTLP_ENDPOINT`, `OTLP_HEADERS` | Grafana Cloud (opcional) |
