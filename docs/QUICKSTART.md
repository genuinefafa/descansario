# ⚡ Quickstart - Próxima Sesión

**Fecha de creación:** 2025-11-18
**Objetivo:** Implementar autenticación JWT para deploy público

---

## 🎯 Contexto Rápido

**Situación:** Fases 1-4 completas (95%). Listo para implementar seguridad y deploy.

**Objetivo del Sprint:** Deploy en VM Debian con acceso "casi público" (URL privada pero accesible desde internet).

**Prioridad absoluta:** Autenticación JWT antes que nada.

---

## 📖 Documentos a Leer Antes de Empezar

**Orden recomendado:**

1. **`docs/ROADMAP.md`** - Plan completo Sprint 1 y 2 (15 min lectura)
2. **`docs/SECURITY.md`** - Checklist de seguridad (10 min lectura)
3. **`.env.example`** - Variables de entorno necesarias (2 min)
4. **`docs/DEPLOY.md`** - Instrucciones de deploy (referencia, no leer todo ahora)

---

## 🚀 Punto de Inicio: Implementar Auth JWT

### Step 1: Crear Branch

```bash
git checkout -b feature/auth-and-deploy
```

### Step 2: Instalar Paquetes Backend

```bash
cd backend/Descansario.Api

# Instalar paquetes para JWT y BCrypt
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package BCrypt.Net-Next

# Restaurar
dotnet restore
```

### Step 3: Crear Modelos

**Archivos a crear:**
- `backend/Descansario.Api/Models/User.cs`
- `backend/Descansario.Api/Models/RefreshToken.cs`
- `backend/Descansario.Api/Models/Dtos/LoginDto.cs`
- `backend/Descansario.Api/Models/Dtos/RegisterDto.cs`
- `backend/Descansario.Api/Models/Dtos/AuthResponseDto.cs`

### Step 4: Crear Servicio de Auth

**Archivos a crear:**
- `backend/Descansario.Api/Services/AuthService.cs`
- `backend/Descansario.Api/Services/IAuthService.cs`

### Step 5: Actualizar DbContext

**Archivo a modificar:**
- `backend/Descansario.Api/Data/DescansarioDbContext.cs`

Agregar DbSets:
```csharp
public DbSet<User> Users { get; set; }
public DbSet<RefreshToken> RefreshTokens { get; set; }
```

### Step 6: Crear Migración

```bash
cd backend/Descansario.Api

# Crear migración
dotnet ef migrations add AddAuthentication

# Verificar archivos generados
ls -la Migrations/

# NO aplicar todavía (se auto-aplica en runtime)
```

### Step 7: Configurar JWT en Program.cs

**Archivo a modificar:**
- `backend/Descansario.Api/Program.cs`

Agregar:
- Configuración JWT desde appsettings
- Middleware de autenticación
- Endpoints de auth (login, register, refresh, logout)

### Step 8: Proteger Endpoints Existentes

**Archivos a modificar:**
- `backend/Descansario.Api/Program.cs`

Agregar `.RequireAuthorization()` a todos los endpoints existentes:
- `/api/persons/*`
- `/api/vacations/*`
- `/api/holidays/*`

Dejar públicos:
- `/health`
- `/api/auth/*`

### Step 9: Crear Seed de Usuario Admin

**Archivo a modificar:**
- `backend/Descansario.Api/Program.cs` (método para seed)

O crear:
- `backend/Descansario.Api/Data/DbSeeder.cs`

### Step 10: Testing Backend

```bash
# Levantar API
dotnet run

# Testing con curl
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Test123!","name":"Admin"}'

curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Test123!"}'

# Copiar token de la respuesta

curl -H "Authorization: Bearer <TOKEN>" \
  http://localhost:5000/api/persons
```

---

## 🎨 Frontend: Agregar Auth UI

### Step 11: Crear Servicio de Auth

**Archivos a crear:**
- `frontend/src/lib/services/auth.ts`
- `frontend/src/lib/stores/auth.ts` (Svelte store para estado)

### Step 12: Crear Página de Login

**Archivo a crear:**
- `frontend/src/routes/login/+page.svelte`

### Step 13: Proteger Rutas

**Archivo a crear/modificar:**
- `frontend/src/routes/+layout.svelte` (agregar verificación de auth)
- `frontend/src/routes/+layout.server.ts` (server-side redirect)

### Step 14: Agregar Interceptor HTTP

**Archivo a modificar:**
- `frontend/src/lib/services/api.ts`

Agregar header `Authorization: Bearer <token>` automáticamente.

### Step 15: Testing Frontend

```bash
cd frontend
npm run dev

# Abrir http://localhost:5173
# Verificar:
# 1. Redirect a /login si no hay token
# 2. Login funciona
# 3. Redirect a / después de login
# 4. CRUD funciona con token
# 5. Logout limpia token y redirect a /login
```

---

## 📋 Checklist de Progreso

### Backend Auth
- [ ] Paquetes instalados
- [ ] Modelos creados (User, RefreshToken, DTOs)
- [ ] AuthService implementado
- [ ] DbContext actualizado
- [ ] Migración creada
- [ ] Program.cs configurado (JWT middleware)
- [ ] Endpoints de auth implementados
- [ ] Endpoints existentes protegidos
- [ ] Seed de usuario admin
- [ ] Testing con curl exitoso

### Frontend Auth
- [ ] Servicio auth.ts creado
- [ ] Store auth.ts creado
- [ ] Página login creada
- [ ] Rutas protegidas
- [ ] Interceptor HTTP configurado
- [ ] Testing UI exitoso

### Validación
- [ ] `npm run validate` pasa (frontend)
- [ ] `dotnet build` pasa (backend)
- [ ] No hay errores de compilación
- [ ] Testing manual completo

---

## 🐛 Troubleshooting Común

### Error: "No authenticationScheme was specified"
**Solución:** Verificar que `builder.Services.AddAuthentication()` esté antes de `builder.Build()`

### Error: "401 Unauthorized" en todos los endpoints
**Solución:** Verificar que el token se está enviando correctamente en el header `Authorization: Bearer <token>`

### Error: "Migrations pending"
**Solución:** Las migraciones se auto-aplican en startup. Ver logs de API.

### Error: Frontend redirect loop
**Solución:** Verificar lógica de redirect en +layout. No hacer redirect a /login si ya estás en /login.

### Error: CORS en login
**Solución:** Verificar que `/api/auth/*` esté permitido en CORS policy.

---

## 📚 Referencias Rápidas

**Implementación de Auth JWT en .NET:**
- https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn

**BCrypt para Passwords:**
- https://github.com/BcryptNet/bcrypt.net

**Svelte Stores:**
- https://svelte.dev/docs/svelte-store

**SvelteKit Hooks:**
- https://kit.svelte.dev/docs/hooks

---

## ⏱️ Estimación de Tiempo

**Backend (Día 1-2):**
- Setup y modelos: 2-3 horas
- AuthService: 2-3 horas
- Endpoints y protección: 2-3 horas
- Testing: 1 hora
- **Total:** 7-10 horas

**Frontend (Día 2-3):**
- Servicio y store: 1-2 horas
- UI de login: 2-3 horas
- Protección de rutas: 1-2 horas
- Testing: 1 hora
- **Total:** 5-8 horas

**Total estimado:** 2-3 días de trabajo (12-18 horas)

---

## 🎯 Siguiente Paso Después de Auth

Una vez que Auth esté funcionando:

1. Crear `docker-compose.prod.yml` (ver ROADMAP.md)
2. Configurar Caddy para HTTPS (ver ROADMAP.md)
3. Deploy en VM (ver DEPLOY.md)
4. Configurar backups (ver DEPLOY.md)

**No avanzar a deploy sin auth funcionando.**

---

## 💡 Tips

- **Commitear frecuentemente:** No esperar a tener todo listo
- **Testing incremental:** Testear cada paso antes de avanzar
- **Logs son tus amigos:** Cuando algo falla, leer logs completos
- **Stack Overflow:** Muy probable que otros hayan tenido el mismo error
- **No copiar/pegar sin entender:** Leer código antes de usarlo

---

## ✅ Criterio de Éxito

**Auth está completa cuando:**
- [ ] Usuario puede registrarse
- [ ] Usuario puede hacer login
- [ ] Token JWT se genera correctamente
- [ ] Endpoints protegidos rechazan requests sin token (401)
- [ ] Endpoints protegidos aceptan requests con token válido
- [ ] Token se persiste en frontend (localStorage)
- [ ] Logout limpia token correctamente
- [ ] Testing manual completo funciona
- [ ] `npm run validate` pasa sin errores
- [ ] `dotnet build` pasa sin errores

**Cuando esto funcione, estás listo para deploy.**

---

**Próximo documento a crear después de auth:**
- `docs/POST_DEPLOY.md` - Verificaciones post-deploy

**¡Éxito con la implementación!** 🚀
