# 🗺️ Roadmap - Descansario

**Última actualización:** 2025-11-18
**Estado actual:** Fase 4 completa (~95%), preparando deploy público

---

## 📍 Estado Actual del Proyecto

### ✅ Completado (Fases 1-4)

**Backend:**
- ✅ API REST completa (.NET 8 + Minimal APIs)
- ✅ 27 endpoints implementados
- ✅ Entity Framework Core + SQLite
- ✅ Cálculo de días hábiles (WorkingDaysCalculator)
- ✅ Sistema de feriados con sync desde API externa
- ✅ Validaciones de negocio
- ✅ Swagger/OpenAPI documentation

**Frontend:**
- ✅ SvelteKit 5 + TypeScript + TailwindCSS
- ✅ CRUD completo: Personas, Vacaciones, Feriados
- ✅ Calendario continuo con scroll infinito
- ✅ Slots conectados (vacaciones consecutivas)
- ✅ Cálculo automático de días hábiles
- ✅ Interfaz por tabs (Personas, Vacaciones, Feriados, Calendario)

**DevOps:**
- ✅ Docker + Docker Compose para desarrollo
- ✅ Auto-migración de base de datos
- ✅ CORS configurado para desarrollo

### ❌ Pendiente para Deploy Público

**Seguridad (CRÍTICO):**
- ❌ Autenticación JWT
- ❌ HTTPS con certificados SSL
- ❌ Rate limiting
- ❌ Secrets en variables de entorno
- ❌ CORS restrictivo para producción

**Operaciones:**
- ❌ Docker Compose para producción
- ❌ Backup automático de base de datos
- ❌ Logs estructurados
- ❌ Healthchecks y monitoreo
- ❌ Testing de seguridad básico

---

## 🎯 Objetivo Inmediato: Deploy Público

**Contexto:** Deploy en VM Debian con acceso "casi público" (URL privada pero accesible desde internet)

**Riesgos sin auth:**
- Bots van a encontrar la URL (crawlers)
- Cualquiera con la URL puede modificar datos
- Sin logs de auditoría
- Sin rate limiting = vulnerable a abuso

---

## 🚀 Sprint 1: Seguridad Básica (5-7 días)

### Día 1-2: Autenticación JWT

**Backend:**
```csharp
// Agregar a Descansario.Api
- Modelos: User, RefreshToken
- Endpoints: /api/auth/login, /api/auth/refresh, /api/auth/register
- Middleware: JWT validation en todos los endpoints protegidos
- Service: AuthService para hashing passwords (BCrypt)
```

**Frontend:**
```typescript
// Agregar a frontend/src/lib
- Página: routes/login/+page.svelte
- Service: services/auth.ts (login, logout, token storage)
- Store: stores/auth.ts (user state)
- Layout: Proteger rutas (+layout.server.ts)
```

**Checklist:**
- [ ] Instalar paquetes: `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next`
- [ ] Crear modelo User (email, passwordHash, role)
- [ ] Crear migración: `dotnet ef migrations add AddAuthentication`
- [ ] Implementar AuthService (login, register, generateToken)
- [ ] Agregar middleware JWT en Program.cs
- [ ] Proteger endpoints existentes con `[Authorize]`
- [ ] Crear seed user inicial (admin)
- [ ] Implementar login UI en frontend
- [ ] Guardar token en localStorage
- [ ] Interceptor HTTP para agregar Authorization header
- [ ] Redirect a /login si 401

**Configuración:**
```bash
# .env (NO commitear)
JWT_SECRET=<generar con: openssl rand -base64 32>
JWT_ISSUER=descansario-api
JWT_AUDIENCE=descansario-web
JWT_EXPIRATION_HOURS=168  # 7 días
```

---

### Día 3: Rate Limiting y Validación

**Rate Limiting:**
```csharp
// Agregar middleware AspNetCoreRateLimit
- 100 requests/minute por IP
- 10 requests/minute en /api/auth/login (prevenir brute force)
```

**Validación robusta:**
- [ ] Review de todos los endpoints POST/PUT
- [ ] Validar email format (regex)
- [ ] Sanitizar inputs (prevenir XSS)
- [ ] Validar rangos de fechas
- [ ] Error handling consistente (no exponer stack traces)

**Checklist:**
- [ ] Instalar `AspNetCoreRateLimit`
- [ ] Configurar rate limiting en appsettings.Production.json
- [ ] Agregar validación de email con DataAnnotations
- [ ] Revisar todos los DTOs
- [ ] Testing manual de inyección SQL (ya mitigado por EF Core)
- [ ] Testing de XSS en campo `notes` (validar sanitización)

---

### Día 4-5: HTTPS y Docker Compose Producción

**Caddy (reverse proxy + HTTPS automático):**
```Caddyfile
# docker/Caddyfile
{
  email tu-email@ejemplo.com
}

tu-dominio.com {
  reverse_proxy web:80
  reverse_proxy /api/* api:5000

  # Security headers
  header {
    Strict-Transport-Security "max-age=31536000;"
    X-Content-Type-Options "nosniff"
    X-Frame-Options "DENY"
    Referrer-Policy "strict-origin-when-cross-origin"
  }

  # Rate limiting
  rate_limit {
    zone static 10r/s
  }
}
```

**docker-compose.prod.yml:**
```yaml
services:
  caddy:
    image: caddy:2-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./docker/Caddyfile:/etc/caddy/Caddyfile
      - caddy-data:/data
      - caddy-config:/config
    restart: unless-stopped
    depends_on:
      - api
      - web

  api:
    build: ./backend
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JWT_SECRET=${JWT_SECRET}
      - ConnectionStrings__DefaultConnection=/data/descansario.db
    volumes:
      - db-data:/data
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  web:
    build: ./frontend
    restart: unless-stopped

volumes:
  db-data:
  caddy-data:
  caddy-config:
```

**Checklist:**
- [ ] Crear docker-compose.prod.yml
- [ ] Crear Caddyfile con tu dominio
- [ ] Configurar secrets en .env
- [ ] Agregar .env a .gitignore
- [ ] Crear appsettings.Production.json
- [ ] Testing local con `docker-compose -f docker-compose.prod.yml up`
- [ ] Verificar HTTPS funciona
- [ ] Verificar healthcheck funciona

---

### Día 6-7: Backups y Logging

**Backup automático:**
```bash
# scripts/backup.sh
#!/bin/bash
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR=/backups
DB_PATH=/data/descansario.db

# Backup SQLite
sqlite3 $DB_PATH ".backup $BACKUP_DIR/descansario_$TIMESTAMP.db"

# Comprimir
gzip $BACKUP_DIR/descansario_$TIMESTAMP.db

# Retener solo últimos 30 días
find $BACKUP_DIR -name "*.db.gz" -mtime +30 -delete

echo "Backup completado: descansario_$TIMESTAMP.db.gz"
```

**Cron job:**
```bash
# Agregar a crontab del servidor
0 2 * * * /ruta/scripts/backup.sh >> /var/log/descansario-backup.log 2>&1
```

**Logging estructurado (Serilog):**
```csharp
// Program.cs
builder.Host.UseSerilog((context, config) =>
{
    config
        .WriteTo.Console()
        .WriteTo.File("logs/descansario-.log", rollingInterval: RollingInterval.Day)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName();
});
```

**Checklist:**
- [ ] Crear script backup.sh
- [ ] Dar permisos ejecución: `chmod +x backup.sh`
- [ ] Testing manual del script
- [ ] Agregar volumen para backups en docker-compose
- [ ] Configurar cron en servidor
- [ ] Instalar Serilog packages
- [ ] Configurar Serilog en Program.cs
- [ ] Agregar logging en endpoints críticos (login, create/delete)
- [ ] Verificar logs se persisten correctamente

---

## 🚀 Sprint 2: Deploy y Hardening (3-5 días)

### Deploy en VM Debian

**Preparación servidor:**
```bash
# 1. Actualizar sistema
sudo apt update && sudo apt upgrade -y

# 2. Instalar Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# 3. Instalar Docker Compose
sudo apt install docker-compose-plugin -y

# 4. Configurar firewall
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw allow 22/tcp
sudo ufw enable

# 5. Crear usuario para la app
sudo useradd -m -s /bin/bash descansario
sudo usermod -aG docker descansario

# 6. Clonar repo
su - descansario
git clone <tu-repo> descansario
cd descansario

# 7. Configurar variables de entorno
cp .env.example .env
nano .env  # Editar con valores reales

# 8. Levantar servicios
docker-compose -f docker-compose.prod.yml up -d

# 9. Verificar logs
docker-compose -f docker-compose.prod.yml logs -f
```

**Checklist:**
- [ ] VM Debian instalada y accesible por SSH
- [ ] Docker y Docker Compose instalados
- [ ] Firewall configurado (UFW)
- [ ] Usuario no-root creado
- [ ] Repositorio clonado
- [ ] Variables de entorno configuradas
- [ ] Servicios levantados correctamente
- [ ] HTTPS funcionando (verificar con navegador)
- [ ] Login funcionando
- [ ] Crear usuario admin inicial

---

### Hardening y Monitoreo

**Fail2ban (bloquear IPs abusivas):**
```bash
sudo apt install fail2ban -y
sudo systemctl enable fail2ban
```

**Monitoreo uptime (opcional pero recomendado):**
- Crear cuenta en [UptimeRobot](https://uptimerobot.com) (gratis)
- Agregar monitor HTTP para `https://tu-dominio.com/health`
- Configurar alertas por email

**Testing de seguridad:**
```bash
# Verificar headers de seguridad
curl -I https://tu-dominio.com

# Verificar rate limiting
for i in {1..20}; do curl https://tu-dominio.com/api/persons; done

# Verificar HTTPS redirect
curl -I http://tu-dominio.com
```

**Checklist:**
- [ ] Fail2ban instalado y activo
- [ ] Monitoreo uptime configurado
- [ ] Testing de rate limiting
- [ ] Testing de HTTPS redirect
- [ ] Testing de headers seguridad
- [ ] Verificar backups automáticos funcionan
- [ ] Documentar credenciales admin en lugar seguro (1Password, Bitwarden)

---

## 📋 Checklist Completo Pre-Deploy

### Seguridad
- [ ] Autenticación JWT implementada
- [ ] HTTPS activo con certificado válido
- [ ] Rate limiting configurado
- [ ] Validación robusta de inputs
- [ ] CORS restrictivo (solo tu dominio)
- [ ] Secrets en variables de entorno
- [ ] No hay credenciales en código
- [ ] Testing básico de seguridad completado

### Operaciones
- [ ] Backups automáticos configurados
- [ ] Logs persistentes y rotados
- [ ] Healthcheck funcionando
- [ ] Docker restart policies configuradas
- [ ] Firewall activo (UFW)
- [ ] Fail2ban instalado
- [ ] Monitoreo uptime activo

### Testing
- [ ] Login funciona correctamente
- [ ] CRUD funciona con auth
- [ ] Calendario renderiza correctamente
- [ ] Cálculo de días hábiles correcto
- [ ] Backups se pueden restaurar
- [ ] Performance aceptable (tiempo de carga <2s)

### Documentación
- [ ] README actualizado con instrucciones de deploy
- [ ] Credenciales admin documentadas (seguro)
- [ ] Proceso de backup documentado
- [ ] Troubleshooting básico documentado

---

## 🔮 Roadmap Post-Deploy (Basado en Feedback Real)

### Features Potenciales

**Estadísticas:**
- Dashboard por persona (días usados/disponibles/pendientes)
- Vista de conflictos (quién está de vacaciones mismo día)
- Reportes mensuales/anuales

**Exportación:**
- Exportación iCal (integración con Google Calendar)
- Exportación CSV (para reportes)
- Exportación PDF (vista imprimible)

**Colaboración:**
- Notificaciones por email (cuando alguien carga vacaciones)
- Sistema de aprobaciones (admin aprueba/rechaza)
- Comentarios en vacaciones

**Roles y Permisos:**
- Admin: full access
- Manager: aprobar vacaciones de su equipo
- User: solo ver y cargar propias vacaciones

**Escalabilidad:**
- Departamentos/equipos
- Múltiples países/regiones
- Vacaciones por horas (medio día)

### Decisión de Prioridades

**NO priorizar hasta tener feedback real de usuarios:**
- Todas las features anteriores son especulativas
- Primero: deploy y uso real
- Segundo: escuchar pain points reales
- Tercero: iterar basado en datos

**Métricas a trackear post-deploy:**
- ¿Cuántos usuarios activos por semana?
- ¿Qué features usan más?
- ¿Dónde se confunden? (analytics básico)
- ¿Qué piden más?

---

## 📚 Recursos y Referencias

**Autenticación JWT:**
- [Microsoft Docs - JWT Auth](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [JWT.io](https://jwt.io) - Debugger de tokens

**Caddy:**
- [Caddy Docs](https://caddyserver.com/docs/)
- [Automatic HTTPS](https://caddyserver.com/docs/automatic-https)

**Docker:**
- [Docker Compose Production](https://docs.docker.com/compose/production/)
- [Healthchecks](https://docs.docker.com/compose/compose-file/compose-file-v3/#healthcheck)

**Seguridad:**
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Security Headers](https://securityheaders.com)

**Backup SQLite:**
- [SQLite Backup API](https://www.sqlite.org/backup.html)

---

## 🎯 Siguiente Sesión: Punto de Inicio

**Arrancar por:**
1. Leer `docs/SECURITY.md` (checklist detallado)
2. Crear branch: `feature/auth-and-deploy`
3. Empezar con autenticación backend (Día 1-2 del Sprint 1)

**Comandos útiles:**
```bash
# Crear branch
git checkout -b feature/auth-and-deploy

# Testing local durante desarrollo
docker-compose up -d

# Ver logs
docker-compose logs -f api

# Reconstruir después de cambios
docker-compose up --build

# Testing de producción (antes de deploy)
docker-compose -f docker-compose.prod.yml up
```

**Prioridad absoluta:** Auth primero, luego HTTPS, luego el resto. Sin auth no deploy.

---

**Última revisión:** 2025-11-18
**Próxima revisión:** Post-deploy (ajustar roadmap según feedback real)
