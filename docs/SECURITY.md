# 🔒 Seguridad - Descansario

**Última actualización:** 2025-11-18
**Contexto:** Preparación para deploy público en VM Debian

---

## ⚠️ Contexto de Amenaza

**Escenario:** Aplicación "casi pública" con URL privada pero accesible desde internet.

**Amenazas:**
- ✅ **Bots y crawlers** van a encontrar la URL (escaneo de rangos IP)
- ✅ **Acceso no autorizado** si no hay autenticación
- ✅ **Abuso de recursos** sin rate limiting
- ✅ **Man-in-the-middle** sin HTTPS
- ✅ **Inyección** (SQL, XSS, command injection)
- ✅ **Brute force** en login sin protección
- ✅ **Data leakage** por logs o error messages

**Conclusión:** URL "secreta" NO es seguridad suficiente. Auth es mandatoria.

---

## 🛡️ Modelo de Seguridad

### Principios

1. **Defense in depth** (múltiples capas)
2. **Least privilege** (usuarios con mínimos permisos necesarios)
3. **Fail secure** (ante error, denegar acceso)
4. **Security by default** (configuración segura out-of-the-box)

### Capas de Seguridad

```
Internet
   ↓
[Firewall UFW] ← Solo puertos 80/443/22
   ↓
[Fail2ban] ← Bloquea IPs abusivas
   ↓
[Caddy] ← HTTPS + Security Headers + Rate Limiting
   ↓
[JWT Middleware] ← Validación de autenticación
   ↓
[API Validation] ← Validación de inputs
   ↓
[EF Core] ← Parameterized queries (anti SQL injection)
   ↓
[SQLite]
```

---

## 🔐 Autenticación JWT

### Implementación

**Flujo:**
1. Usuario envía email + password a `/api/auth/login`
2. Backend valida credenciales (BCrypt)
3. Backend genera JWT firmado
4. Frontend guarda token en localStorage
5. Todas las requests incluyen `Authorization: Bearer <token>`
6. Backend valida token en cada request

**Token Structure:**
```json
{
  "sub": "user@example.com",
  "userId": "1",
  "role": "admin",
  "iat": 1700000000,
  "exp": 1700604800
}
```

### Configuración Backend

**appsettings.Production.json:**
```json
{
  "Jwt": {
    "Secret": "${JWT_SECRET}",
    "Issuer": "descansario-api",
    "Audience": "descansario-web",
    "ExpirationHours": 168
  }
}
```

**Variables de entorno (.env):**
```bash
# Generar con: openssl rand -base64 32
JWT_SECRET=<tu-secret-aquí>
JWT_ISSUER=descansario-api
JWT_AUDIENCE=descansario-web
JWT_EXPIRATION_HOURS=168  # 7 días
```

### Password Hashing

**Usar BCrypt con work factor 12:**
```csharp
// NO hacer esto:
password == storedPassword  // ❌ Plaintext

// Hacer esto:
BCrypt.Net.BCrypt.Verify(password, storedPasswordHash)  // ✅
BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)
```

### Endpoints de Auth

```
POST /api/auth/register
  Body: { email, password, name }
  Response: { userId, email, role }
  Rate limit: 5/hour por IP

POST /api/auth/login
  Body: { email, password }
  Response: { token, refreshToken, user }
  Rate limit: 10/minute por IP

POST /api/auth/refresh
  Body: { refreshToken }
  Response: { token, refreshToken }
  Rate limit: 20/minute por IP

POST /api/auth/logout
  Body: { refreshToken }
  Response: 204 No Content
```

### Token Storage (Frontend)

**localStorage vs sessionStorage:**
- ✅ localStorage: Token persiste entre sesiones (UX mejor)
- ⚠️ Vulnerable a XSS (mitigado con validación de inputs)
- ❌ httpOnly cookies: Más seguro pero complica CORS

**Implementación:**
```typescript
// lib/services/auth.ts
export function saveToken(token: string) {
  localStorage.setItem('auth_token', token)
}

export function getToken(): string | null {
  return localStorage.getItem('auth_token')
}

export function clearToken() {
  localStorage.removeItem('auth_token')
}

// Interceptor para agregar header
fetch(url, {
  headers: {
    'Authorization': `Bearer ${getToken()}`
  }
})
```

---

## 🚦 Rate Limiting

### Configuración Recomendada

**Global:**
- 100 requests/minute por IP

**Por endpoint:**
```
/api/auth/login: 10/minute (prevenir brute force)
/api/auth/register: 5/hour (prevenir spam)
/api/persons: 50/minute
/api/vacations: 50/minute
/api/holidays/sync: 5/hour (costosa)
```

### Implementación con AspNetCoreRateLimit

**appsettings.Production.json:**
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      },
      {
        "Endpoint": "POST:/api/auth/login",
        "Period": "1m",
        "Limit": 10
      },
      {
        "Endpoint": "POST:/api/auth/register",
        "Period": "1h",
        "Limit": 5
      }
    ]
  }
}
```

### Testing

```bash
# Verificar rate limiting funciona
for i in {1..15}; do
  curl -X POST https://tu-dominio.com/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@test.com","password":"wrong"}'
done

# Debe devolver 429 Too Many Requests después de 10 intentos
```

---

## 🔒 HTTPS con Caddy

### Ventajas de Caddy

- ✅ Certificados SSL automáticos (Let's Encrypt)
- ✅ Renovación automática
- ✅ Configuración simple (vs nginx)
- ✅ HTTP/2 por defecto
- ✅ Security headers integrados

### Caddyfile Completo

```caddyfile
{
  email tu-email@example.com

  # Staging para testing (evita rate limits Let's Encrypt)
  # acme_ca https://acme-staging-v02.api.letsencrypt.org/directory
}

# Redirect www → apex
www.tu-dominio.com {
  redir https://tu-dominio.com{uri}
}

tu-dominio.com {
  # Frontend (SPA)
  reverse_proxy /api/* api:5000
  reverse_proxy web:80

  # Security headers
  header {
    # HSTS
    Strict-Transport-Security "max-age=31536000; includeSubDomains; preload"

    # XSS Protection
    X-Content-Type-Options "nosniff"
    X-Frame-Options "DENY"
    X-XSS-Protection "1; mode=block"

    # CSP (ajustar según necesites)
    Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';"

    # Referrer
    Referrer-Policy "strict-origin-when-cross-origin"

    # Permissions
    Permissions-Policy "geolocation=(), microphone=(), camera=()"

    # No exponer server version
    -Server
  }

  # Logging
  log {
    output file /var/log/caddy/access.log {
      roll_size 100mb
      roll_keep 5
    }
  }

  # Rate limiting (requiere plugin)
  # rate_limit {
  #   zone dynamic 10r/s
  # }
}
```

### Testing HTTPS

```bash
# Verificar certificado SSL
curl -vI https://tu-dominio.com 2>&1 | grep -i "SSL\|TLS"

# Verificar headers de seguridad
curl -I https://tu-dominio.com

# Verificar redirect HTTP → HTTPS
curl -I http://tu-dominio.com

# Testing completo con Mozilla Observatory
# https://observatory.mozilla.org
```

---

## 🛠️ Validación de Inputs

### Vectores de Ataque

**SQL Injection:**
- ✅ Mitigado por EF Core (parameterized queries)
- ⚠️ Verificar no hay raw SQL queries

**XSS (Cross-Site Scripting):**
- ⚠️ Campo `notes` en Vacation permite texto libre
- ✅ Frontend debe sanitizar antes de renderizar
- ✅ Backend debe validar caracteres peligrosos

**Command Injection:**
- ⚠️ Si usas `Process.Start()` con input usuario
- ✅ No hay casos actualmente en el código

### Validación Backend

**DataAnnotations:**
```csharp
public class PersonDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Range(0, 365)]
    public int AvailableDays { get; set; }
}

public class VacationDto
{
    [Required]
    public int PersonId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(1000)]
    [RegularExpression(@"^[^<>]*$", ErrorMessage = "HTML tags not allowed")]
    public string? Notes { get; set; }
}
```

**Validación manual en endpoints:**
```csharp
app.MapPost("/api/persons", async (PersonDto dto, DescansarioDbContext db) =>
{
    // Validar email único
    if (await db.Persons.AnyAsync(p => p.Email == dto.Email))
        return Results.BadRequest("Email already exists");

    // Validar email format
    if (!IsValidEmail(dto.Email))
        return Results.BadRequest("Invalid email format");

    // Sanitizar inputs
    var person = new Person
    {
        Name = SanitizeString(dto.Name),
        Email = dto.Email.ToLowerInvariant(),
        AvailableDays = dto.AvailableDays
    };

    db.Persons.Add(person);
    await db.SaveChangesAsync();
    return Results.Created($"/api/persons/{person.Id}", person);
});
```

### Sanitización Frontend

**Para campo notes (Markdown):**
```typescript
import DOMPurify from 'dompurify'
import { marked } from 'marked'

function renderMarkdown(markdown: string): string {
  // 1. Convertir Markdown a HTML
  const html = marked(markdown)

  // 2. Sanitizar HTML (eliminar scripts, eventos, etc)
  const clean = DOMPurify.sanitize(html, {
    ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'ul', 'ol', 'li', 'a'],
    ALLOWED_ATTR: ['href']
  })

  return clean
}
```

---

## 🔍 Error Handling Seguro

### ❌ NO Exponer Stack Traces

**Malo:**
```csharp
catch (Exception ex)
{
    return Results.Problem(ex.ToString());  // ❌ Expone rutas, código
}
```

**Bueno:**
```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Error creating person");  // Log interno
    return Results.Problem("An error occurred");   // Mensaje genérico
}
```

### Logging Seguro

**NO loguear:**
- ❌ Passwords (ni siquiera hasheados)
- ❌ Tokens completos
- ❌ Datos sensibles de usuarios

**SÍ loguear:**
- ✅ Intentos de login fallidos (con IP)
- ✅ Cambios en datos críticos (quién, qué, cuándo)
- ✅ Errores de aplicación
- ✅ Rate limit violations

**Ejemplo:**
```csharp
// ❌ Malo
logger.LogInformation("User login: {Email} {Password}", email, password);

// ✅ Bueno
logger.LogInformation("Login attempt for {Email} from {IP}",
    email, httpContext.Connection.RemoteIpAddress);

// ✅ Bueno para auditoría
logger.LogWarning("Failed login attempt for {Email} from {IP}",
    email, httpContext.Connection.RemoteIpAddress);
```

---

## 🔥 Firewall y Fail2ban

### UFW (Uncomplicated Firewall)

```bash
# Configuración inicial
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Permitir servicios necesarios
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS

# Activar
sudo ufw enable

# Verificar
sudo ufw status verbose
```

### Fail2ban

**Detecta y bloquea IPs abusivas automáticamente.**

**Instalación:**
```bash
sudo apt install fail2ban -y
sudo systemctl enable fail2ban
sudo systemctl start fail2ban
```

**Configuración básica (/etc/fail2ban/jail.local):**
```ini
[DEFAULT]
bantime = 1h
findtime = 10m
maxretry = 5

[sshd]
enabled = true
port = 22

# Custom jail para API
[descansario-api]
enabled = true
port = 80,443
filter = descansario-api
logpath = /var/log/caddy/access.log
maxretry = 10
bantime = 1h
```

**Filter (/etc/fail2ban/filter.d/descansario-api.conf):**
```ini
[Definition]
failregex = ^.*"POST /api/auth/login.*" 401.*$
            ^.*"POST /api/auth/login.*" 400.*$
ignoreregex =
```

**Testing:**
```bash
# Ver bans activos
sudo fail2ban-client status descansario-api

# Unban una IP
sudo fail2ban-client set descansario-api unbanip 1.2.3.4
```

---

## 📊 Monitoreo y Alertas

### Healthcheck Endpoint

```csharp
app.MapGet("/health", async (DescansarioDbContext db) =>
{
    try
    {
        // Verificar conectividad DB
        await db.Database.CanConnectAsync();

        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem("Database unavailable", statusCode: 503);
    }
});
```

### Monitoreo Externo

**UptimeRobot (gratis):**
1. Crear cuenta en https://uptimerobot.com
2. Agregar monitor HTTP(S) a `https://tu-dominio.com/health`
3. Intervalo: 5 minutos
4. Alertas: Email cuando status != 200

**Healthchecks.io (alternativa):**
- Similar a UptimeRobot
- Puede hacer ping desde tu app también

### Docker Healthcheck

```yaml
# docker-compose.prod.yml
services:
  api:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

---

## 💾 Backup y Disaster Recovery

### Estrategia de Backups

**Qué respaldar:**
- ✅ Base de datos SQLite (`descansario.db`)
- ✅ Variables de entorno (`.env`)
- ✅ Certificados SSL (si no son auto-renovables)
- ⚠️ Logs (opcional, ocupan espacio)

**Frecuencia:**
- Diario: 2:00 AM (baja actividad)
- Retención: 30 días

**Ubicaciones:**
- Local: `/backups` (mismo servidor)
- Remoto: S3, Google Drive, Dropbox, rsync a otro servidor

### Script de Backup

Ver `scripts/backup.sh` y `scripts/restore.sh`.

### Restauración

```bash
# 1. Detener servicios
docker-compose down

# 2. Restaurar DB
gunzip -c backups/descansario_20251118_020000.db.gz > data/descansario.db

# 3. Verificar permisos
chown descansario:descansario data/descansario.db

# 4. Reiniciar servicios
docker-compose up -d

# 5. Verificar
docker-compose logs -f api
```

### Testing de Backups

**IMPORTANTE:** Backups no probados = no backups

```bash
# Cada mes, verificar:
1. Script de backup ejecuta correctamente
2. Archivo .db.gz no está corrupto
3. Restauración funciona en entorno de testing
```

---

## 📋 Security Checklist Completo

### Pre-Deploy

**Autenticación:**
- [ ] JWT implementado
- [ ] Passwords hasheados con BCrypt (work factor 12+)
- [ ] Tokens expiran (7 días máximo)
- [ ] Refresh tokens implementados
- [ ] Endpoints protegidos con `[Authorize]`
- [ ] Login rate limited (10/min)
- [ ] Registro rate limited (5/hour)

**HTTPS:**
- [ ] Certificado SSL válido
- [ ] HTTP redirect a HTTPS
- [ ] HSTS header configurado
- [ ] Certificados se renuevan automáticamente

**Headers de Seguridad:**
- [ ] X-Content-Type-Options: nosniff
- [ ] X-Frame-Options: DENY
- [ ] Strict-Transport-Security
- [ ] Content-Security-Policy
- [ ] Referrer-Policy

**Validación:**
- [ ] Todos los inputs validados
- [ ] Email format validado
- [ ] Fechas validadas (start <= end)
- [ ] SQL injection mitigado (EF Core)
- [ ] XSS mitigado (sanitización)
- [ ] Rate limiting global (100/min)

**Secrets:**
- [ ] JWT_SECRET en variable de entorno
- [ ] .env en .gitignore
- [ ] No hay credenciales hardcodeadas
- [ ] ConnectionStrings securizados

**Firewall:**
- [ ] UFW habilitado
- [ ] Solo puertos necesarios abiertos (22, 80, 443)
- [ ] Fail2ban instalado y configurado

**Logging:**
- [ ] Logs estructurados (Serilog)
- [ ] Logs no contienen secrets
- [ ] Logs rotados (no llenan disco)
- [ ] Login failures logueados

**Backups:**
- [ ] Script de backup funcionando
- [ ] Cron job configurado
- [ ] Backups probados (restauración funciona)
- [ ] Retención configurada (30 días)

**Monitoreo:**
- [ ] Healthcheck endpoint funcional
- [ ] Uptime monitoring configurado
- [ ] Alertas configuradas (email)
- [ ] Docker healthcheck configurado

---

### Post-Deploy

**Verificación:**
- [ ] Testing de login desde cliente externo
- [ ] Testing de rate limiting
- [ ] Testing de HTTPS (SSLLabs.com)
- [ ] Testing de headers (securityheaders.com)
- [ ] Testing de backup restore
- [ ] Performance testing (tiempo de carga <2s)

**Documentación:**
- [ ] Credenciales admin documentadas (seguro)
- [ ] Proceso de backup documentado
- [ ] Troubleshooting común documentado
- [ ] Contactos de emergencia documentados

---

## 🚨 Incident Response

### Si hay brecha de seguridad:

1. **Contener:**
   - [ ] Tomar app offline inmediatamente
   - [ ] Cambiar todas las credenciales
   - [ ] Rotar JWT secret
   - [ ] Revisar logs para entender scope

2. **Investigar:**
   - [ ] ¿Cómo entraron?
   - [ ] ¿Qué datos accedieron?
   - [ ] ¿Cuándo empezó?
   - [ ] ¿Hay backdoors?

3. **Remediar:**
   - [ ] Patchear vulnerabilidad
   - [ ] Restaurar desde backup limpio
   - [ ] Implementar prevención

4. **Notificar:**
   - [ ] Usuarios afectados
   - [ ] Stakeholders
   - [ ] Legal (si aplica GDPR)

5. **Aprender:**
   - [ ] Post-mortem
   - [ ] Actualizar procedimientos
   - [ ] Training

---

## 📚 Referencias

**OWASP:**
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [JWT Security](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)

**Testing:**
- [SSL Labs](https://www.ssllabs.com/ssltest/)
- [Security Headers](https://securityheaders.com)
- [Mozilla Observatory](https://observatory.mozilla.org)

**Tools:**
- [BCrypt Calculator](https://bcrypt-generator.com)
- [JWT Debugger](https://jwt.io)
- [OpenSSL](https://www.openssl.org)

---

**Última actualización:** 2025-11-18
**Próxima revisión:** Post-deploy (ajustar según incidentes)
