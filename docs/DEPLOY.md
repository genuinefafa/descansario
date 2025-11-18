# 🚀 Guía de Deploy - Descansario

**Última actualización:** 2025-11-18
**Target:** VM Debian con Docker + Caddy (HTTPS automático)

---

## 📋 Pre-requisitos

### En tu Máquina Local
- [ ] Git instalado
- [ ] Acceso SSH al servidor
- [ ] Dominio configurado apuntando a IP del servidor (DNS propagado)

### En el Servidor (VM Debian)
- [ ] Debian 11+ o Ubuntu 20.04+
- [ ] Mínimo 2GB RAM
- [ ] 10GB espacio en disco libre
- [ ] Acceso root o sudo
- [ ] Puertos 80/443 accesibles desde internet

---

## 🔧 Parte 1: Preparación del Servidor

### 1.1 Conectar al Servidor

```bash
# Desde tu máquina local
ssh usuario@tu-servidor-ip

# Si usas key SSH
ssh -i ~/.ssh/tu-key.pem usuario@tu-servidor-ip
```

### 1.2 Actualizar Sistema

```bash
# Actualizar paquetes
sudo apt update && sudo apt upgrade -y

# Instalar utilidades básicas
sudo apt install -y curl git ufw sqlite3
```

### 1.3 Instalar Docker

```bash
# Descargar script de instalación
curl -fsSL https://get.docker.com -o get-docker.sh

# Revisar script (opcional pero recomendado)
cat get-docker.sh

# Ejecutar instalación
sudo sh get-docker.sh

# Verificar instalación
docker --version

# Instalar Docker Compose plugin
sudo apt install -y docker-compose-plugin

# Verificar Docker Compose
docker compose version
```

### 1.4 Configurar Firewall (UFW)

```bash
# Configurar reglas default
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Permitir SSH (IMPORTANTE: hacer antes de enable)
sudo ufw allow 22/tcp

# Permitir HTTP/HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Activar firewall
sudo ufw enable

# Verificar status
sudo ufw status verbose
```

**⚠️ IMPORTANTE:** Asegurate de permitir SSH (puerto 22) ANTES de activar el firewall, o te quedarás bloqueado.

### 1.5 Crear Usuario para la Aplicación

```bash
# Crear usuario descansario
sudo useradd -m -s /bin/bash descansario

# Agregar a grupo docker (para ejecutar sin sudo)
sudo usermod -aG docker descansario

# Crear directorios necesarios
sudo mkdir -p /opt/descansario
sudo mkdir -p /var/log/descansario
sudo mkdir -p /backups/descansario

# Cambiar ownership
sudo chown -R descansario:descansario /opt/descansario
sudo chown -R descansario:descansario /var/log/descansario
sudo chown -R descansario:descansario /backups/descansario
```

### 1.6 Configurar Fail2ban (Opcional pero Recomendado)

```bash
# Instalar
sudo apt install -y fail2ban

# Copiar configuración default
sudo cp /etc/fail2ban/jail.conf /etc/fail2ban/jail.local

# Editar configuración
sudo nano /etc/fail2ban/jail.local

# Buscar y modificar:
# [DEFAULT]
# bantime = 1h
# findtime = 10m
# maxretry = 5

# Iniciar servicio
sudo systemctl enable fail2ban
sudo systemctl start fail2ban

# Verificar status
sudo fail2ban-client status
```

---

## 📦 Parte 2: Deploy de la Aplicación

### 2.1 Clonar Repositorio

```bash
# Cambiar a usuario descansario
sudo su - descansario

# Clonar repo
cd /opt/descansario
git clone https://github.com/tu-usuario/descansario.git .

# Verificar archivos
ls -la
```

### 2.2 Configurar Variables de Entorno

```bash
# Copiar template
cp .env.example .env

# Editar con valores reales
nano .env
```

**Completar los siguientes valores:**

```bash
# 1. Generar JWT secret
openssl rand -base64 32

# 2. Editar .env con el secret generado
JWT_SECRET=<pegar-secret-generado>
JWT_ISSUER=descansario-api
JWT_AUDIENCE=descansario-web
JWT_EXPIRATION_HOURS=168

# 3. Configurar dominio
DOMAIN=tu-dominio.com
CADDY_EMAIL=tu-email@example.com

# 4. Configurar CORS
CORS_ORIGINS=https://tu-dominio.com

# 5. Verificar paths
DATABASE_PATH=/data/descansario.db
BACKUP_PATH=/backups
LOG_PATH=/var/log/descansario
```

**Guardar y salir:** `Ctrl+X`, luego `Y`, luego `Enter`

### 2.3 Crear docker-compose.prod.yml

**NOTA:** Este archivo será creado durante la implementación de auth. Por ahora, usar docker-compose.yml existente para testing.

```bash
# Verificar que docker-compose.yml existe
cat docker-compose.yml
```

### 2.4 Crear Caddyfile

```bash
# Crear directorio para configuración
mkdir -p docker/caddy

# Crear Caddyfile
nano docker/caddy/Caddyfile
```

**Contenido del Caddyfile:**

```caddyfile
{
  email {$CADDY_EMAIL}

  # Descomentar para testing (usa staging de Let's Encrypt)
  # acme_ca https://acme-staging-v02.api.letsencrypt.org/directory
}

# Redirect www → apex
www.{$DOMAIN} {
  redir https://{$DOMAIN}{uri} permanent
}

{$DOMAIN} {
  # Frontend (SPA)
  reverse_proxy /api/* api:5000
  reverse_proxy web:80

  # Security headers
  header {
    Strict-Transport-Security "max-age=31536000; includeSubDomains"
    X-Content-Type-Options "nosniff"
    X-Frame-Options "DENY"
    X-XSS-Protection "1; mode=block"
    Referrer-Policy "strict-origin-when-cross-origin"
    -Server
  }

  # Logging
  log {
    output file /var/log/caddy/access.log {
      roll_size 100mb
      roll_keep 5
    }
  }
}
```

### 2.5 Build y Levantar Servicios

```bash
# Build de imágenes
docker compose build

# Levantar en modo detached
docker compose up -d

# Ver logs
docker compose logs -f

# Verificar que todos los servicios están up
docker compose ps
```

**Servicios esperados:**
- `descansario-api` - Estado: `Up`
- `descansario-web` - Estado: `Up`
- `descansario-caddy` (si ya configuraste producción) - Estado: `Up`

### 2.6 Verificar Deploy

```bash
# Verificar healthcheck de API
curl http://localhost:5000/health

# Verificar frontend
curl http://localhost:3000

# Verificar logs de API
docker compose logs api | tail -n 50

# Verificar logs de web
docker compose logs web | tail -n 50
```

### 2.7 Crear Usuario Admin Inicial

**IMPORTANTE:** Esto se hará después de implementar auth. Por ahora, documentar credenciales que usarás:

```bash
# Documentar en lugar seguro (1Password, Bitwarden)
Admin Email: admin@tu-dominio.com
Admin Password: <generar password seguro>
```

---

## 🔐 Parte 3: HTTPS con Caddy (Post-Auth)

**NOTA:** Completar después de implementar autenticación JWT.

### 3.1 Verificar DNS

```bash
# Verificar que tu dominio apunta al servidor
nslookup tu-dominio.com

# Debe devolver la IP de tu servidor
```

### 3.2 Testing HTTPS Local (Staging)

```bash
# Descomentar línea acme_ca en Caddyfile (usa staging)
nano docker/caddy/Caddyfile

# Reiniciar Caddy
docker compose restart caddy

# Ver logs de certificados
docker compose logs caddy | grep -i certificate
```

### 3.3 HTTPS Producción

```bash
# Comentar línea acme_ca en Caddyfile
nano docker/caddy/Caddyfile

# Reiniciar Caddy
docker compose restart caddy

# Verificar certificado
curl -I https://tu-dominio.com

# Verificar en navegador
# https://tu-dominio.com
```

### 3.4 Verificar Seguridad

```bash
# Headers de seguridad
curl -I https://tu-dominio.com | grep -i "strict-transport\|x-frame\|x-content"

# Testing SSL (opcional)
# Visitar: https://www.ssllabs.com/ssltest/
# Ingresar: tu-dominio.com
```

---

## 💾 Parte 4: Backups

### 4.1 Crear Script de Backup

```bash
# Volver a usuario descansario
sudo su - descansario

# Crear script
nano /opt/descansario/scripts/backup.sh
```

**Contenido del script:**

```bash
#!/bin/bash

# Configuración
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR=/backups/descansario
DB_CONTAINER=descansario-api
DB_PATH=/data/descansario.db
LOG_FILE=/var/log/descansario/backup.log

# Crear directorio de backups
mkdir -p $BACKUP_DIR

# Logging
echo "[$(date)] Iniciando backup..." >> $LOG_FILE

# Backup de SQLite desde contenedor
docker exec $DB_CONTAINER sqlite3 $DB_PATH ".backup /tmp/backup.db"
docker cp $DB_CONTAINER:/tmp/backup.db $BACKUP_DIR/descansario_$TIMESTAMP.db

# Comprimir
gzip $BACKUP_DIR/descansario_$TIMESTAMP.db

# Verificar que se creó
if [ -f "$BACKUP_DIR/descansario_$TIMESTAMP.db.gz" ]; then
  SIZE=$(du -h "$BACKUP_DIR/descansario_$TIMESTAMP.db.gz" | cut -f1)
  echo "[$(date)] Backup completado: descansario_$TIMESTAMP.db.gz ($SIZE)" >> $LOG_FILE
else
  echo "[$(date)] ERROR: Backup falló" >> $LOG_FILE
  exit 1
fi

# Limpiar backups antiguos (retener 30 días)
find $BACKUP_DIR -name "*.db.gz" -mtime +30 -delete
echo "[$(date)] Backups antiguos eliminados (>30 días)" >> $LOG_FILE

# Opcional: Backup a remoto (descomentar y configurar)
# rsync -avz $BACKUP_DIR/ usuario@servidor-remoto:/backups/descansario/

echo "[$(date)] Proceso de backup finalizado" >> $LOG_FILE
```

**Guardar y dar permisos:**

```bash
chmod +x /opt/descansario/scripts/backup.sh

# Testing manual
/opt/descansario/scripts/backup.sh

# Verificar backup creado
ls -lh /backups/descansario/
```

### 4.2 Configurar Cron para Backups Automáticos

```bash
# Editar crontab del usuario descansario
crontab -e

# Agregar línea (backup diario a las 2 AM)
0 2 * * * /opt/descansario/scripts/backup.sh

# Verificar crontab
crontab -l
```

### 4.3 Restaurar desde Backup (Procedimiento)

**Solo ejecutar en caso de emergencia:**

```bash
# 1. Detener servicios
docker compose down

# 2. Listar backups disponibles
ls -lh /backups/descansario/

# 3. Descomprimir backup deseado
gunzip -c /backups/descansario/descansario_20251118_020000.db.gz > /tmp/restore.db

# 4. Copiar a volumen de datos (ajustar path según tu docker-compose)
docker volume ls | grep descansario
docker run --rm -v descansario_db-data:/data -v /tmp:/backup alpine cp /backup/restore.db /data/descansario.db

# 5. Verificar permisos
docker run --rm -v descansario_db-data:/data alpine ls -la /data/

# 6. Reiniciar servicios
docker compose up -d

# 7. Verificar logs
docker compose logs -f api

# 8. Verificar app funciona
curl http://localhost:5000/health
```

---

## 📊 Parte 5: Monitoreo

### 5.1 Configurar Uptime Monitoring (Opcional)

**Opción A: UptimeRobot (Gratis)**

1. Crear cuenta en https://uptimerobot.com
2. Agregar nuevo monitor:
   - Type: HTTP(s)
   - URL: `https://tu-dominio.com/health`
   - Monitoring Interval: 5 minutes
3. Configurar alertas:
   - Email: tu-email@example.com
   - Alert When: Down

**Opción B: Healthchecks.io (Gratis)**

1. Crear cuenta en https://healthchecks.io
2. Crear nuevo check
3. Copiar URL de ping
4. Agregar a crontab (ping cada hora):

```bash
crontab -e

# Agregar línea
0 * * * * curl -fsS --retry 3 https://hc-ping.com/tu-uuid > /dev/null
```

### 5.2 Logs

```bash
# Ver logs en tiempo real
docker compose logs -f

# Logs solo de API
docker compose logs -f api

# Logs con timestamp de últimos 100 líneas
docker compose logs --tail=100 --timestamps api

# Logs de Caddy (access log)
docker exec descansario-caddy cat /var/log/caddy/access.log | tail -n 50
```

### 5.3 Espacio en Disco

```bash
# Verificar espacio
df -h

# Espacio usado por Docker
docker system df

# Limpiar imágenes antiguas (cuidado en producción)
docker system prune -a --volumes
```

---

## 🔧 Troubleshooting

### Problema: Servicios no levantan

```bash
# Ver logs detallados
docker compose logs

# Ver status de servicios
docker compose ps

# Verificar que no haya otro proceso usando puertos
sudo netstat -tulpn | grep -E ':(80|443|5000|3000)'

# Reiniciar servicios
docker compose restart
```

### Problema: HTTPS no funciona

```bash
# Verificar DNS apunta correctamente
nslookup tu-dominio.com

# Verificar firewall permite 80/443
sudo ufw status

# Ver logs de Caddy
docker compose logs caddy | grep -i certificate

# Verificar rate limits de Let's Encrypt
# https://letsencrypt.org/docs/rate-limits/

# Testing con staging (no consume rate limits)
# Descomentar acme_ca en Caddyfile
```

### Problema: Base de datos corrupta

```bash
# Verificar integridad
docker exec descansario-api sqlite3 /data/descansario.db "PRAGMA integrity_check;"

# Debería devolver "ok"

# Si está corrupta, restaurar desde backup
# Ver "4.3 Restaurar desde Backup"
```

### Problema: Sin espacio en disco

```bash
# Verificar espacio
df -h

# Limpiar logs viejos
sudo journalctl --vacuum-time=7d

# Limpiar Docker
docker system prune -a

# Limpiar backups muy antiguos (>90 días)
find /backups/descansario -name "*.db.gz" -mtime +90 -delete
```

---

## 📋 Checklist Post-Deploy

### Funcionalidad
- [ ] App accesible en https://tu-dominio.com
- [ ] Healthcheck responde: `/health`
- [ ] Login funciona (después de implementar auth)
- [ ] CRUD de personas funciona
- [ ] CRUD de vacaciones funciona
- [ ] Calendario renderiza correctamente
- [ ] Cálculo de días hábiles correcto

### Seguridad
- [ ] HTTPS activo y certificado válido
- [ ] HTTP redirect a HTTPS
- [ ] Headers de seguridad presentes
- [ ] Autenticación funciona (post-auth)
- [ ] Rate limiting activo (post-auth)
- [ ] Firewall configurado (solo 22, 80, 443)
- [ ] Fail2ban activo

### Operaciones
- [ ] Backups automáticos configurados
- [ ] Backup manual probado y funciona
- [ ] Logs accesibles y rotados
- [ ] Monitoreo uptime configurado
- [ ] Alertas configuradas
- [ ] Docker healthcheck funcional

### Documentación
- [ ] Credenciales admin guardadas (seguro)
- [ ] Variables de entorno documentadas
- [ ] Procedimiento de backup documentado
- [ ] Procedimiento de restauración documentado
- [ ] Contactos de emergencia documentados

---

## 🔄 Actualización de la Aplicación

```bash
# 1. Conectar al servidor
ssh usuario@servidor

# 2. Cambiar a usuario descansario
sudo su - descansario

# 3. Ir al directorio
cd /opt/descansario

# 4. Hacer backup manual (por las dudas)
/opt/descansario/scripts/backup.sh

# 5. Pull de cambios
git pull origin main

# 6. Rebuild de imágenes
docker compose build

# 7. Reiniciar servicios (downtime mínimo)
docker compose up -d

# 8. Verificar logs
docker compose logs -f

# 9. Verificar healthcheck
curl https://tu-dominio.com/health
```

---

## 📞 Contactos de Emergencia

**Documentar:**
- Proveedor de hosting: [nombre, teléfono, URL]
- Proveedor de dominio: [nombre, URL]
- Contacto técnico: [email, teléfono]

**Recursos:**
- Logs: `/var/log/descansario/`
- Backups: `/backups/descansario/`
- Aplicación: `/opt/descansario/`

---

**Última actualización:** 2025-11-18
**Próxima revisión:** Post-deploy (documentar issues encontrados)
