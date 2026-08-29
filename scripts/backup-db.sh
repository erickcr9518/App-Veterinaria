#!/usr/bin/env bash
# Backs up the VetPlatform database running in the docker-compose stack.
# Run from the repo root: bash scripts/backup-db.sh
set -euo pipefail

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_FILE="vetplatform-${TIMESTAMP}.bak"

mkdir -p backups

echo "Backing up VetPlatform database to backups/${BACKUP_FILE} ..."

docker compose exec -T sqlserver bash -c "
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -C -Q \"BACKUP DATABASE [VetPlatform] TO DISK = N'/var/opt/mssql/backup/${BACKUP_FILE}' WITH FORMAT, INIT, COMPRESSION;\"
"

echo "Done."
ls -lh "backups/${BACKUP_FILE}"
