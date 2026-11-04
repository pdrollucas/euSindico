#!/bin/bash
# Roda NA INSTÂNCIA, como root: dump do MySQL compactado para s3://<bucket>/backups/ (os dumps expiram em
# 30 dias, regra do bucket). Chamado por deploy/scripts/parar.sh antes de parar a instância, ou à mão.
set -euo pipefail

cd /opt/eusindico

BUCKET=$(grep '^S3_BUCKET_NAME=' .env | cut -d= -f2)
DESTINO="s3://$BUCKET/backups/eusindico-$(date -u +%Y%m%dT%H%M%SZ).sql.gz"

# A senha de root já está no ambiente do container do MySQL; não passa pela linha de comando do host.
docker compose exec -T mysql sh -c \
  'MYSQL_PWD="$MYSQL_ROOT_PASSWORD" mysqldump -uroot --single-transaction --routines --triggers --databases eusindico' \
  | gzip | aws s3 cp - "$DESTINO" --region us-east-2 --only-show-errors

echo "Backup salvo em $DESTINO"
