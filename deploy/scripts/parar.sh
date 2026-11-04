#!/bin/bash
# Roda na SUA MÁQUINA (Git Bash): faz o backup do banco no S3 e só então para a instância.
# Uso: bash deploy/scripts/parar.sh        (perfil da AWS CLI: AWS_PROFILE, padrão "eusindico")
#
# Parada, a instância deixa de cobrar a hora da EC2; disco e IP fixo continuam (~US$ 5,70/mês).
# Se o backup falhar, a instância NÃO é parada.
set -euo pipefail
export MSYS_NO_PATHCONV=1 # o Git Bash não deve converter caminhos como /opt/... em caminhos do Windows

AWS_ARGS=(--profile "${AWS_PROFILE:-eusindico}" --region us-east-2)
INSTANCIA=$(aws cloudformation describe-stacks "${AWS_ARGS[@]}" --stack-name eusindico \
  --query 'Stacks[0].Outputs[?OutputKey==`InstanceId`].OutputValue' --output text)

echo "Backup do banco na instância $INSTANCIA..."
COMANDO=$(aws ssm send-command "${AWS_ARGS[@]}" --instance-ids "$INSTANCIA" \
  --document-name AWS-RunShellScript --comment "backup antes de parar" \
  --parameters 'commands=["bash /opt/eusindico/backup.sh"]' \
  --query Command.CommandId --output text)

aws ssm wait command-executed "${AWS_ARGS[@]}" --command-id "$COMANDO" --instance-id "$INSTANCIA" || true
STATUS=$(aws ssm get-command-invocation "${AWS_ARGS[@]}" --command-id "$COMANDO" --instance-id "$INSTANCIA" \
  --query '[Status,StandardOutputContent,StandardErrorContent]' --output text)
echo "$STATUS"
if [[ "$STATUS" != Success* ]]; then
  echo "Backup falhou: a instância continua ligada." >&2
  exit 1
fi

echo "Parando a instância..."
aws ec2 stop-instances "${AWS_ARGS[@]}" --instance-ids "$INSTANCIA" --output text > /dev/null
aws ec2 wait instance-stopped "${AWS_ARGS[@]}" --instance-ids "$INSTANCIA"
echo "Instância parada."
