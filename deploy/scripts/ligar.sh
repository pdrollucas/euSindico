#!/bin/bash
# Roda na SUA MÁQUINA (Git Bash): liga a instância e espera o site voltar.
# Uso: bash deploy/scripts/ligar.sh        (perfil da AWS CLI: AWS_PROFILE, padrão "eusindico")
#
# O IP é fixo (Elastic IP), então o DNS continua valendo. Os containers sobem sozinhos
# (restart: unless-stopped) com os dados e os certificados que já estavam no disco.
set -euo pipefail
export MSYS_NO_PATHCONV=1

AWS_ARGS=(--profile "${AWS_PROFILE:-eusindico}" --region us-east-2)
DOMINIO=eusindicogestao.com.br
INSTANCIA=$(aws cloudformation describe-stacks "${AWS_ARGS[@]}" --stack-name eusindico \
  --query 'Stacks[0].Outputs[?OutputKey==`InstanceId`].OutputValue' --output text)

echo "Ligando a instância $INSTANCIA..."
aws ec2 start-instances "${AWS_ARGS[@]}" --instance-ids "$INSTANCIA" --output text > /dev/null
aws ec2 wait instance-running "${AWS_ARGS[@]}" --instance-ids "$INSTANCIA"

echo "Instância ligada. Aguardando a API responder em https://api.$DOMINIO/health ..."
for _ in $(seq 1 40); do
  if [ "$(curl -fsS --max-time 5 "https://api.$DOMINIO/health" 2>/dev/null)" = "Healthy" ]; then
    echo "Site no ar: https://$DOMINIO"
    exit 0
  fi
  sleep 6
done

echo "A API não respondeu em 4 minutos. Veja os logs pelo Session Manager: cd /opt/eusindico && sudo docker compose logs --tail 50" >&2
exit 1
