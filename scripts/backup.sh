#!/bin/bash

# ============================================
# Descansario - Script de Backup Automático
# ============================================
#
# Realiza backup de la base de datos SQLite y logs
# Retiene backups de los últimos 30 días
#
# Uso:
#   ./scripts/backup.sh
#
# Cron (backup diario a las 2 AM):
#   0 2 * * * /ruta/descansario/scripts/backup.sh >> /var/log/descansario/backup.log 2>&1
#
# ============================================

set -e  # Exit on error

# Configuración
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
DATE=$(date +%Y-%m-%d)
BACKUP_DIR="${BACKUP_DIR:-/backups/descansario}"
CONTAINER_NAME="${CONTAINER_NAME:-descansario-api-prod}"
DB_PATH="${DB_PATH:-/data/descansario.db}"
RETENTION_DAYS="${RETENTION_DAYS:-30}"
LOG_FILE="${LOG_FILE:-/var/log/descansario/backup.log}"

# Colores para output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Función de logging
log() {
    echo "[$(date +'%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}✓${NC} $1" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}⚠${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}✗${NC} $1" | tee -a "$LOG_FILE"
}

# Crear directorio de backups si no existe
mkdir -p "$BACKUP_DIR"

log "===== Iniciando backup de Descansario ====="

# 1. Verificar que el contenedor está corriendo
if ! docker ps --format '{{.Names}}' | grep -q "$CONTAINER_NAME"; then
    log_error "El contenedor $CONTAINER_NAME no está corriendo"
    exit 1
fi

log_success "Contenedor $CONTAINER_NAME está corriendo"

# 2. Crear directorio para el backup de hoy
DAILY_BACKUP_DIR="$BACKUP_DIR/$DATE"
mkdir -p "$DAILY_BACKUP_DIR"

# 3. Backup de SQLite usando el comando .backup
log "Creando backup de base de datos..."

# Backup dentro del contenedor primero
docker exec "$CONTAINER_NAME" sqlite3 "$DB_PATH" ".backup /tmp/descansario_backup_$TIMESTAMP.db"

if [ $? -ne 0 ]; then
    log_error "Error al crear backup dentro del contenedor"
    exit 1
fi

# Copiar backup desde el contenedor
docker cp "$CONTAINER_NAME:/tmp/descansario_backup_$TIMESTAMP.db" "$DAILY_BACKUP_DIR/descansario_$TIMESTAMP.db"

if [ $? -ne 0 ]; then
    log_error "Error al copiar backup desde el contenedor"
    exit 1
fi

# Limpiar backup temporal del contenedor
docker exec "$CONTAINER_NAME" rm -f "/tmp/descansario_backup_$TIMESTAMP.db"

# Comprimir backup
log "Comprimiendo backup..."
gzip "$DAILY_BACKUP_DIR/descansario_$TIMESTAMP.db"

if [ ! -f "$DAILY_BACKUP_DIR/descansario_$TIMESTAMP.db.gz" ]; then
    log_error "Error al comprimir backup"
    exit 1
fi

# Verificar integridad del backup
log "Verificando integridad del backup..."
gunzip -c "$DAILY_BACKUP_DIR/descansario_$TIMESTAMP.db.gz" > /tmp/test_backup.db
INTEGRITY=$(sqlite3 /tmp/test_backup.db "PRAGMA integrity_check;" 2>/dev/null)
rm -f /tmp/test_backup.db

if [ "$INTEGRITY" != "ok" ]; then
    log_error "Backup corrupto! Integrity check falló: $INTEGRITY"
    exit 1
fi

# Calcular tamaño del backup
BACKUP_SIZE=$(du -h "$DAILY_BACKUP_DIR/descansario_$TIMESTAMP.db.gz" | cut -f1)
log_success "Backup creado: descansario_$TIMESTAMP.db.gz ($BACKUP_SIZE)"

# 4. Backup de logs (opcional)
if docker exec "$CONTAINER_NAME" test -d /var/log/descansario; then
    log "Creando backup de logs..."
    docker exec "$CONTAINER_NAME" tar -czf "/tmp/logs_$TIMESTAMP.tar.gz" -C /var/log/descansario . 2>/dev/null || true

    if docker exec "$CONTAINER_NAME" test -f "/tmp/logs_$TIMESTAMP.tar.gz"; then
        docker cp "$CONTAINER_NAME:/tmp/logs_$TIMESTAMP.tar.gz" "$DAILY_BACKUP_DIR/logs_$TIMESTAMP.tar.gz"
        docker exec "$CONTAINER_NAME" rm -f "/tmp/logs_$TIMESTAMP.tar.gz"

        LOG_SIZE=$(du -h "$DAILY_BACKUP_DIR/logs_$TIMESTAMP.tar.gz" | cut -f1)
        log_success "Backup de logs creado: logs_$TIMESTAMP.tar.gz ($LOG_SIZE)"
    else
        log_warning "No se encontraron logs para respaldar"
    fi
fi

# 5. Limpiar backups antiguos (retener solo RETENTION_DAYS días)
log "Limpiando backups antiguos (retención: $RETENTION_DAYS días)..."

DELETED_COUNT=0
while IFS= read -r -d '' old_dir; do
    rm -rf "$old_dir"
    DELETED_COUNT=$((DELETED_COUNT + 1))
done < <(find "$BACKUP_DIR" -maxdepth 1 -type d -name "20*" -mtime +$RETENTION_DAYS -print0)

if [ $DELETED_COUNT -gt 0 ]; then
    log_success "Eliminados $DELETED_COUNT backups antiguos"
else
    log "No hay backups antiguos para eliminar"
fi

# 6. Resumen
TOTAL_SIZE=$(du -sh "$BACKUP_DIR" | cut -f1)
BACKUP_COUNT=$(find "$BACKUP_DIR" -name "*.db.gz" | wc -l)

log "===== Backup completado ====="
log_success "Total de backups: $BACKUP_COUNT"
log_success "Espacio total usado: $TOTAL_SIZE"
log_success "Último backup: $DAILY_BACKUP_DIR/descansario_$TIMESTAMP.db.gz"

# 7. Opcional: Enviar backup a ubicación remota (descomentar y configurar)
# log "Sincronizando con ubicación remota..."
# rsync -avz "$DAILY_BACKUP_DIR/" usuario@servidor-remoto:/backups/descansario/
# aws s3 sync "$BACKUP_DIR" s3://mi-bucket/descansario-backups/

exit 0
