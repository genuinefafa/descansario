#!/bin/bash

# ============================================
# Descansario - Script de Restauración
# ============================================
#
# Restaura la base de datos desde un backup
#
# Uso:
#   ./scripts/restore.sh /backups/descansario/2025-11-18/descansario_20251118_020000.db.gz
#
# ⚠️  ADVERTENCIA: Este script SOBRESCRIBIRÁ la base de datos actual
#
# ============================================

set -e  # Exit on error

# Colores
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Configuración
CONTAINER_NAME="${CONTAINER_NAME:-descansario-api-prod}"
DB_PATH="${DB_PATH:-/data/descansario.db}"

# Verificar que se pasó un archivo de backup
if [ $# -ne 1 ]; then
    echo -e "${RED}Error:${NC} Debe especificar el archivo de backup"
    echo "Uso: $0 <archivo-backup.db.gz>"
    echo ""
    echo "Ejemplo:"
    echo "  $0 /backups/descansario/2025-11-18/descansario_20251118_020000.db.gz"
    exit 1
fi

BACKUP_FILE="$1"

# Verificar que el archivo existe
if [ ! -f "$BACKUP_FILE" ]; then
    echo -e "${RED}Error:${NC} El archivo $BACKUP_FILE no existe"
    exit 1
fi

# Verificar que es un archivo .gz
if [[ ! "$BACKUP_FILE" =~ \.db\.gz$ ]]; then
    echo -e "${RED}Error:${NC} El archivo debe ser un backup .db.gz"
    exit 1
fi

echo -e "${YELLOW}⚠️  ADVERTENCIA:${NC} Este script SOBRESCRIBIRÁ la base de datos actual"
echo "Archivo de backup: $BACKUP_FILE"
echo "Contenedor: $CONTAINER_NAME"
echo "Base de datos: $DB_PATH"
echo ""
read -p "¿Está seguro de continuar? (escriba 'SI' para confirmar): " -r
echo

if [ "$REPLY" != "SI" ]; then
    echo -e "${YELLOW}Cancelado${NC}"
    exit 0
fi

# Verificar que el contenedor está corriendo
if ! docker ps --format '{{.Names}}' | grep -q "$CONTAINER_NAME"; then
    echo -e "${RED}Error:${NC} El contenedor $CONTAINER_NAME no está corriendo"
    exit 1
fi

echo -e "${GREEN}✓${NC} Contenedor $CONTAINER_NAME está corriendo"

# 1. Crear backup de la base de datos actual (por seguridad)
echo "Creando backup de seguridad de la DB actual..."
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
docker exec "$CONTAINER_NAME" sqlite3 "$DB_PATH" ".backup /tmp/pre_restore_backup_$TIMESTAMP.db"
docker cp "$CONTAINER_NAME:/tmp/pre_restore_backup_$TIMESTAMP.db" "/tmp/pre_restore_backup_$TIMESTAMP.db"
docker exec "$CONTAINER_NAME" rm -f "/tmp/pre_restore_backup_$TIMESTAMP.db"

echo -e "${GREEN}✓${NC} Backup de seguridad creado: /tmp/pre_restore_backup_$TIMESTAMP.db"

# 2. Detener la aplicación (para evitar escrituras durante restore)
echo "Deteniendo contenedor..."
docker-compose -f docker-compose.prod.yml stop api

if [ $? -ne 0 ]; then
    echo -e "${RED}Error:${NC} No se pudo detener el contenedor"
    exit 1
fi

echo -e "${GREEN}✓${NC} Contenedor detenido"

# 3. Descomprimir backup
echo "Descomprimiendo backup..."
TEMP_DB="/tmp/restore_temp_$TIMESTAMP.db"
gunzip -c "$BACKUP_FILE" > "$TEMP_DB"

if [ ! -f "$TEMP_DB" ]; then
    echo -e "${RED}Error:${NC} No se pudo descomprimir el backup"
    docker-compose -f docker-compose.prod.yml start api
    exit 1
fi

echo -e "${GREEN}✓${NC} Backup descomprimido"

# 4. Verificar integridad del backup
echo "Verificando integridad del backup..."
INTEGRITY=$(sqlite3 "$TEMP_DB" "PRAGMA integrity_check;" 2>/dev/null)

if [ "$INTEGRITY" != "ok" ]; then
    echo -e "${RED}Error:${NC} Backup corrupto! Integrity check falló: $INTEGRITY"
    rm -f "$TEMP_DB"
    docker-compose -f docker-compose.prod.yml start api
    exit 1
fi

echo -e "${GREEN}✓${NC} Integridad verificada"

# 5. Obtener info del backup
TABLES=$(sqlite3 "$TEMP_DB" "SELECT name FROM sqlite_master WHERE type='table';" | wc -l)
PERSONS=$(sqlite3 "$TEMP_DB" "SELECT COUNT(*) FROM Persons;" 2>/dev/null || echo "0")
VACATIONS=$(sqlite3 "$TEMP_DB" "SELECT COUNT(*) FROM Vacations;" 2>/dev/null || echo "0")
HOLIDAYS=$(sqlite3 "$TEMP_DB" "SELECT COUNT(*) FROM Holidays;" 2>/dev/null || echo "0")

echo ""
echo "Contenido del backup:"
echo "  - Tablas: $TABLES"
echo "  - Personas: $PERSONS"
echo "  - Vacaciones: $VACATIONS"
echo "  - Feriados: $HOLIDAYS"
echo ""

read -p "¿Confirma la restauración? (escriba 'SI' para confirmar): " -r
echo

if [ "$REPLY" != "SI" ]; then
    echo -e "${YELLOW}Cancelado${NC}"
    rm -f "$TEMP_DB"
    docker-compose -f docker-compose.prod.yml start api
    exit 0
fi

# 6. Reemplazar base de datos
echo "Restaurando base de datos..."

# Copiar backup al contenedor
docker cp "$TEMP_DB" "$CONTAINER_NAME:$DB_PATH"

if [ $? -ne 0 ]; then
    echo -e "${RED}Error:${NC} No se pudo copiar el backup al contenedor"
    rm -f "$TEMP_DB"
    docker-compose -f docker-compose.prod.yml start api
    exit 1
fi

# Limpiar archivo temporal
rm -f "$TEMP_DB"

echo -e "${GREEN}✓${NC} Base de datos restaurada"

# 7. Reiniciar contenedor
echo "Reiniciando contenedor..."
docker-compose -f docker-compose.prod.yml start api

if [ $? -ne 0 ]; then
    echo -e "${RED}Error:${NC} No se pudo reiniciar el contenedor"
    exit 1
fi

# Esperar a que el contenedor esté listo
echo "Esperando a que la API esté lista..."
sleep 5

# Verificar healthcheck
for i in {1..10}; do
    if curl -f http://localhost:5000/health >/dev/null 2>&1; then
        echo -e "${GREEN}✓${NC} API está funcionando"
        break
    fi

    if [ $i -eq 10 ]; then
        echo -e "${RED}Error:${NC} La API no respondió después de la restauración"
        echo "Revisa los logs: docker-compose -f docker-compose.prod.yml logs -f api"
        exit 1
    fi

    sleep 2
done

# 8. Resumen
echo ""
echo -e "${GREEN}===== Restauración completada =====${NC}"
echo "Base de datos restaurada desde: $BACKUP_FILE"
echo ""
echo "Backup de seguridad guardado en: /tmp/pre_restore_backup_$TIMESTAMP.db"
echo "(Puede eliminarlo manualmente cuando esté seguro de que la restauración fue exitosa)"
echo ""
echo "Para verificar la restauración:"
echo "  docker-compose -f docker-compose.prod.yml logs -f api"
echo "  curl http://localhost:5000/api/persons"

exit 0
