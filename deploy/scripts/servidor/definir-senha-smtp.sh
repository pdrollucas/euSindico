#!/bin/bash
# Roda NA INSTÂNCIA, de forma interativa: grava a senha de app do Gmail no .env e reinicia a API.
# Abra um terminal pelo console da AWS (EC2 -> instância eusindico -> Connect -> Session Manager) e rode:
#   sudo bash /opt/eusindico/definir-senha-smtp.sh
# A senha não aparece ao digitar e não fica no histórico do shell.
set -euo pipefail

cd /opt/eusindico

read -r -s -p "Senha de app do Gmail (eusindico.contato@gmail.com): " SENHA
echo

# O Google exibe a senha de app em grupos de 4 separados por espaço.
SENHA="${SENHA// /}"
if [ "${#SENHA}" -ne 16 ]; then
  echo "A senha de app do Google tem 16 caracteres (sem os espaços). Nada foi alterado." >&2
  exit 1
fi

sed -i "s|^SMTP_SENHA=.*|SMTP_SENHA=$SENHA|" .env
docker compose up -d api
echo "Senha gravada e API reiniciada."
