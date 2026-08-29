#!/usr/bin/env bash
# Restores the VetPlatform database in the docker-compose stack from a
# backup made by scripts/backup-db.sh. DESTRUCTIVE: replaces the current
# database entirely. Run from the repo root:
#   bash scripts/restore-db.sh vetplatform-20260829-120000.bak
set -euo pipefail

if [ $# -ne 1 ]; then
  echo "Usage: $0 <backup-filename>" >&2
  echo "Available backups:" >&2
  ls -1 backups/*.bak 2>/dev/null || echo "  (none found in ./backups)" >&2
  exit 1
fi

BACKUP_FILE="$1"

if [ ! -f "backups/${BACKUP_FILE}" ]; then
  echo "backups/${BACKUP_FILE} not found." >&2
  exit 1
fi

echo "WARNING: this REPLACES the current VetPlatform database with the contents of ${BACKUP_FILE}."
echo "Everything written since that backup was taken will be lost."
read -r -p "Type 'yes' to continue: " CONFIRM
if [ "${CONFIRM}" != "yes" ]; then
  echo "Aborted."
  exit 1
fi

echo "Restoring..."

docker compose exec -T sqlserver bash -c "
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -C -Q \"
    ALTER DATABASE [VetPlatform] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    RESTORE DATABASE [VetPlatform] FROM DISK = N'/var/opt/mssql/backup/${BACKUP_FILE}' WITH REPLACE;
    ALTER DATABASE [VetPlatform] SET MULTI_USER;
  \"
"

echo "Restore complete. Restart the api container so it reconnects cleanly:"
echo "  docker compose restart api"
