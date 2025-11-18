# 🔧 Configuración de Nginx como Reverse Proxy

**Última actualización:** 2025-11-18
**Contexto:** Descansario corre en Docker en la misma red que nginx

---

## 📋 Prerequisitos

- Nginx instalado y funcionando
- Docker Compose levantado (`docker-compose -f docker-compose.prod.yml up -d`)
- Descansario corriendo en:
  - **API**: `http://localhost:5000`
  - **Web**: `http://localhost:3000`

---

## 🔐 Configuración Nginx con HTTPS

### Opción 1: Con Dominio y Let's Encrypt (Recomendado)

```nginx
# /etc/nginx/sites-available/descansario

# Redirect HTTP → HTTPS
server {
    listen 80;
    listen [::]:80;
    server_name descansario.tudominio.com;

    # Certbot challenge (para renovación automática de SSL)
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    # Redirect todo lo demás a HTTPS
    location / {
        return 301 https://$server_name$request_uri;
    }
}

# HTTPS Server
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name descansario.tudominio.com;

    # SSL Certificates (Let's Encrypt)
    ssl_certificate /etc/letsencrypt/live/descansario.tudominio.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/descansario.tudominio.com/privkey.pem;

    # SSL Configuration (Mozilla Intermediate)
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers 'ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384';
    ssl_prefer_server_ciphers off;

    # Security Headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Logging
    access_log /var/log/nginx/descansario-access.log;
    error_log /var/log/nginx/descansario-error.log;

    # API Proxy (Backend)
    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;

        # Headers
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;

        # Buffer settings
        proxy_buffering off;
        proxy_request_buffering off;
    }

    # Health check (exponer sin auth)
    location /health {
        proxy_pass http://localhost:5000/health;
        proxy_set_header Host $host;
    }

    # Frontend (SPA)
    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;

        # Headers
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Upgrade headers (para SSE/WebSockets si fuera necesario en el futuro)
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_cache_bypass $http_upgrade;
    }

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types text/plain text/css text/xml text/javascript application/json application/javascript application/xml+rss;
}
```

### Paso a paso para configurar SSL con Let's Encrypt:

```bash
# 1. Instalar certbot
sudo apt install certbot python3-certbot-nginx -y

# 2. Crear directorio para challenges
sudo mkdir -p /var/www/certbot

# 3. Copiar configuración nginx (primero SIN SSL)
sudo nano /etc/nginx/sites-available/descansario
# (Copiar solo el bloque del puerto 80 por ahora)

# 4. Habilitar sitio
sudo ln -s /etc/nginx/sites-available/descansario /etc/nginx/sites-enabled/

# 5. Test configuración
sudo nginx -t

# 6. Reload nginx
sudo systemctl reload nginx

# 7. Obtener certificado SSL
sudo certbot certonly --webroot -w /var/www/certbot -d descansario.tudominio.com

# 8. Actualizar configuración nginx con bloque HTTPS completo
sudo nano /etc/nginx/sites-available/descansario

# 9. Test y reload
sudo nginx -t && sudo systemctl reload nginx

# 10. Verificar renovación automática
sudo certbot renew --dry-run
```

---

### Opción 2: Sin Dominio (IP directa con HTTPS autofirmado)

**⚠️ Solo para testing interno. Navegadores mostrarán advertencia de seguridad.**

```bash
# 1. Generar certificado autofirmado
sudo mkdir -p /etc/nginx/ssl
sudo openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout /etc/nginx/ssl/descansario.key \
  -out /etc/nginx/ssl/descansario.crt \
  -subj "/C=AR/ST=BuenosAires/L=BuenosAires/O=Descansario/CN=192.168.1.100"

# 2. Configuración nginx (usar certificados autofirmados)
```

```nginx
# /etc/nginx/sites-available/descansario

server {
    listen 443 ssl http2;
    server_name 192.168.1.100;  # Tu IP

    # Certificados autofirmados
    ssl_certificate /etc/nginx/ssl/descansario.crt;
    ssl_certificate_key /etc/nginx/ssl/descansario.key;

    # Resto de la configuración igual que arriba...
    # (Security headers, proxies, etc)
}

server {
    listen 80;
    server_name 192.168.1.100;
    return 301 https://$server_name$request_uri;
}
```

---

### Opción 3: Solo HTTP (NO recomendado para producción)

**⚠️ Solo para desarrollo/testing. NO usar en producción.**

```nginx
# /etc/nginx/sites-available/descansario

server {
    listen 80;
    server_name localhost 192.168.1.100;

    access_log /var/log/nginx/descansario-access.log;
    error_log /var/log/nginx/descansario-error.log;

    # API
    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }

    # Health
    location /health {
        proxy_pass http://localhost:5000/health;
    }

    # Frontend
    location / {
        proxy_pass http://localhost:3000;
        proxy_set_header Host $host;
    }
}
```

---

## 🧪 Testing de Configuración

### 1. Test configuración nginx

```bash
# Verificar sintaxis
sudo nginx -t

# Ver logs en tiempo real
sudo tail -f /var/log/nginx/descansario-error.log
```

### 2. Test de endpoints

```bash
# Healthcheck
curl https://descansario.tudominio.com/health

# API
curl https://descansario.tudominio.com/api/persons

# Frontend
curl -I https://descansario.tudominio.com/
```

### 3. Verificar headers de seguridad

```bash
curl -I https://descansario.tudominio.com/ | grep -i "strict-transport\|x-frame\|x-content"
```

### 4. Testing SSL (si usas dominio)

```bash
# Verificar grado SSL
curl https://www.ssllabs.com/ssltest/analyze.html?d=descansario.tudominio.com

# O usar testssl.sh
testssl descansario.tudominio.com
```

---

## 🔧 Troubleshooting

### Problema: 502 Bad Gateway

```bash
# Verificar que Docker Compose está corriendo
docker-compose -f docker-compose.prod.yml ps

# Verificar que API responde
curl http://localhost:5000/health

# Verificar logs de nginx
sudo tail -f /var/log/nginx/descansario-error.log

# Verificar conectividad
telnet localhost 5000
telnet localhost 3000
```

### Problema: Certificado SSL no se renueva

```bash
# Verificar status de certbot
sudo certbot certificates

# Test renovación
sudo certbot renew --dry-run

# Forzar renovación
sudo certbot renew --force-renewal

# Verificar timer de renovación automática
sudo systemctl status certbot.timer
```

### Problema: CORS errors

```bash
# Verificar que CORS está configurado en el backend
# El backend debe permitir el origen de nginx

# Ver logs del backend
docker-compose -f docker-compose.prod.yml logs -f api

# Verificar headers CORS
curl -I -X OPTIONS https://descansario.tudominio.com/api/persons \
  -H "Origin: https://descansario.tudominio.com" \
  -H "Access-Control-Request-Method: POST"
```

---

## 📊 Monitoreo

### Logs de nginx

```bash
# Access log
sudo tail -f /var/log/nginx/descansario-access.log

# Error log
sudo tail -f /var/log/nginx/descansario-error.log

# Analizar errores 5xx
sudo grep " 5" /var/log/nginx/descansario-access.log | tail -n 20
```

### Logs de aplicación

```bash
# API logs
docker-compose -f docker-compose.prod.yml logs -f api

# Web logs
docker-compose -f docker-compose.prod.yml logs -f web
```

---

## 🔐 Mejoras de Seguridad Opcionales

### Rate Limiting en nginx

```nginx
# Agregar al bloque http {} en /etc/nginx/nginx.conf

# Zona de rate limiting
limit_req_zone $binary_remote_addr zone=descansario_general:10m rate=10r/s;
limit_req_zone $binary_remote_addr zone=descansario_auth:10m rate=5r/m;

# En server block:
location / {
    limit_req zone=descansario_general burst=20;
    # ... resto de config
}

location /api/auth/login {
    limit_req zone=descansario_auth burst=3;
    # ... resto de config
}
```

### Fail2ban para nginx

```bash
# Instalar fail2ban
sudo apt install fail2ban -y

# Crear filtro para descansario
sudo nano /etc/fail2ban/filter.d/nginx-descansario.conf
```

```ini
[Definition]
failregex = ^<HOST> -.*"(GET|POST) /api/auth/login HTTP.*" 401
ignoreregex =
```

```bash
# Configurar jail
sudo nano /etc/fail2ban/jail.local
```

```ini
[nginx-descansario]
enabled = true
port = http,https
filter = nginx-descansario
logpath = /var/log/nginx/descansario-access.log
maxretry = 5
bantime = 1h
```

```bash
# Reiniciar fail2ban
sudo systemctl restart fail2ban

# Verificar status
sudo fail2ban-client status nginx-descansario
```

---

## 📝 Comandos Útiles

```bash
# Reload nginx (sin downtime)
sudo systemctl reload nginx

# Restart nginx
sudo systemctl restart nginx

# Verificar configuración
sudo nginx -t

# Ver status de nginx
sudo systemctl status nginx

# Habilitar nginx en boot
sudo systemctl enable nginx

# Ver puertos en uso
sudo netstat -tulpn | grep nginx

# Ver conexiones activas
sudo ss -t | grep :443
```

---

**Última actualización:** 2025-11-18
**Próxima revisión:** Post-deploy (ajustar según problemas encontrados)
