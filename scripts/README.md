# Scripts de Utilidades

Herramientas útiles para administración de Descansario.

## 📋 Scripts Disponibles

### 1. `generate-password-hash.csx`

Genera hash BCrypt para passwords de usuarios.

**Requisitos:**
```bash
# Instalar dotnet-script globalmente
dotnet tool install -g dotnet-script
```

**Uso:**
```bash
cd scripts
dotnet script generate-password-hash.csx <password>
```

**Ejemplo:**
```bash
dotnet script generate-password-hash.csx miPassword123

# Output:
# ✅ Password hash generado:
#
# Password: miPassword123
# Hash:     $2a$12$...
#
# Para usar este hash en la base de datos:
# PasswordHash = "$2a$12$..."
```

**Caso de uso:** Crear usuarios manualmente en la base de datos o agregar seed data.

---

### 2. `backup.sh`

Script de backup automatizado de SQLite con verificación de integridad.

**Uso:**
```bash
# Ejecución manual
./scripts/backup.sh

# Configurar cron (2 AM diario)
crontab -e
# Agregar:
0 2 * * * /ruta/completa/scripts/backup.sh
```

**Características:**
- Backup con `.backup` de SQLite (más seguro que `VACUUM INTO`)
- Compresión con gzip
- Verificación de integridad con `PRAGMA integrity_check`
- Retención de 30 días
- Logs de ejecución

Ver `/docs/DEPLOY.md` para más detalles.

---

### 3. `restore.sh`

Restaura backup de SQLite con confirmaciones de seguridad.

**Uso:**
```bash
# Listar backups disponibles
ls -lh backups/

# Restaurar backup específico
./scripts/restore.sh backups/descansario_20251118_020000.db.gz
```

**Características:**
- Doble confirmación antes de restaurar
- Crea backup de seguridad antes de restaurar
- Verifica integridad post-restauración
- Reinicia servicios automáticamente

---

## 🔧 Tareas Comunes

### Crear nuevo usuario

1. Generar hash del password:
   ```bash
   dotnet script scripts/generate-password-hash.csx nuevoPassword123
   ```

2. Conectar a la base de datos:
   ```bash
   # Desarrollo local
   sqlite3 backend/Descansario.Api/descansario.db

   # Producción (dentro del contenedor)
   docker exec -it descansario-api sqlite3 /data/descansario.db
   ```

3. Insertar usuario:
   ```sql
   INSERT INTO Users (Email, PasswordHash, Name, Role, CreatedAt)
   VALUES (
     'nuevo@example.com',
     '$2a$12$...hash-generado...',
     'Nombre Usuario',
     'User',  -- o 'Admin'
     datetime('now')
   );
   ```

4. Verificar:
   ```sql
   SELECT Id, Email, Name, Role, CreatedAt FROM Users;
   ```

### Cambiar password de usuario existente

1. Generar nuevo hash:
   ```bash
   dotnet script scripts/generate-password-hash.csx nuevoPassword
   ```

2. Actualizar en DB:
   ```sql
   UPDATE Users
   SET PasswordHash = '$2a$12$...nuevo-hash...'
   WHERE Email = 'usuario@example.com';
   ```

### Verificar backups

```bash
# Listar backups ordenados por fecha
ls -lht backups/

# Ver tamaño total
du -sh backups/

# Verificar integridad de un backup
gunzip -c backups/descansario_YYYYMMDD_HHMMSS.db.gz > /tmp/test.db
sqlite3 /tmp/test.db "PRAGMA integrity_check;"
rm /tmp/test.db
```

---

## 📚 Referencias

- [BCrypt.Net Docs](https://github.com/BcryptNet/bcrypt.net)
- [SQLite Backup API](https://www.sqlite.org/backup.html)
- [dotnet-script](https://github.com/filipw/dotnet-script)

---

**Última actualización:** 2025-11-18
