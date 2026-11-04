#!/bin/bash
# Roda NA INSTÂNCIA, como root, chamado pelo workflow de deploy (.github/workflows/deploy.yml) via Session
# Manager, a partir de /opt/eusindico — onde o workflow já baixou docker-compose.yml, Caddyfile e estes scripts.
#
# Uso: deploy.sh <API_IMAGE> <WEB_IMAGE> <S3_BUCKET_NAME> <SITE_DOMAIN>
#
# No primeiro deploy cria o .env com senhas novas (MySQL e JWT), geradas aqui e que nunca saem da instância.
# A senha de app do Gmail não vem por aqui: é digitada depois com definir-senha-smtp.sh (ver deploy/README.md).
set -euo pipefail

API_IMAGE="$1"
WEB_IMAGE="$2"
S3_BUCKET_NAME="$3"
SITE_DOMAIN="$4"
REGIAO=us-east-2

cd /opt/eusindico
umask 077

if [ ! -f .env ]; then
  cat > .env <<EOF
SITE_DOMAIN=$SITE_DOMAIN
ACME_EMAIL=eusindico.contato@gmail.com
API_IMAGE=$API_IMAGE
WEB_IMAGE=$WEB_IMAGE
MYSQL_PASSWORD=$(openssl rand -hex 24)
MYSQL_ROOT_PASSWORD=$(openssl rand -hex 24)
JWT_SECRET_KEY=$(openssl rand -base64 48 | tr -d '\n')
S3_BUCKET_NAME=$S3_BUCKET_NAME
SMTP_USUARIO=eusindico.contato@gmail.com
SMTP_SENHA=
OTLP_ENDPOINT=
OTLP_HEADERS=
EOF
  echo ".env criado com senhas novas (MySQL e JWT). Falta a senha de app do Gmail: definir-senha-smtp.sh"
fi

# Só as imagens mudam a cada deploy; os segredos ficam como estão.
sed -i "s|^API_IMAGE=.*|API_IMAGE=$API_IMAGE|; s|^WEB_IMAGE=.*|WEB_IMAGE=$WEB_IMAGE|" .env

# Login no ECR com a role da instância (o token vale 12 h).
aws ecr get-login-password --region "$REGIAO" | docker login --username AWS --password-stdin "${API_IMAGE%%/*}"

docker compose pull --quiet
# Recria só o que mudou. O "migrate" roda de novo e a API espera ele terminar com sucesso.
docker compose up -d --remove-orphans

echo "Aguardando a API ficar saudável..."
for _ in $(seq 1 36); do
  estado=$(docker inspect -f '{{.State.Health.Status}}' eusindico-api-1 2>/dev/null || echo "ausente")
  if [ "$estado" = "healthy" ]; then
    echo "API saudável."
    docker compose ps --format '{{.Service}}: {{.Status}}'
    # Imagens antigas ocupam o disco de 20 GB; as em uso (inclusive o MySQL) são mantidas.
    docker image prune -af > /dev/null
    exit 0
  fi
  sleep 5
done

echo "A API não ficou saudável em 3 minutos. Últimos logs:" >&2
docker compose logs --tail 40 migrate api >&2
exit 1
