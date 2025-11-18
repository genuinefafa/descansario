# JWT Claims - Troubleshooting

## Problema: Claims NameIdentifier Duplicados

### Síntomas

- Login exitoso (200 OK)
- Token JWT generado correctamente
- Endpoint `/api/auth/me` devuelve 401 Unauthorized
- El usuario está autenticado (`IsAuthenticated = true`)
- Los claims están presentes pero no se puede encontrar el user ID

### Causa Raíz

ASP.NET Core JWT Bearer middleware tiene un **mapeo automático de claims** que convierte claims JWT estándar a tipos de claims de .NET Framework:

- `sub` (Subject) → `ClaimTypes.NameIdentifier`
- `email` → `ClaimTypes.Email`
- `name` → `ClaimTypes.Name`
- etc.

**En nuestro caso:**
1. Generamos un claim `sub` con el email del usuario
2. Generamos un claim `ClaimTypes.NameIdentifier` con el user ID (int)
3. El middleware mapea automáticamente `sub` → `ClaimTypes.NameIdentifier`
4. **Resultado:** DOS claims con el mismo tipo `NameIdentifier`
   - Primero: email (string) - del mapeo automático
   - Segundo: user ID (int) - nuestro claim explícito

**Cuando usamos `FindFirst(ClaimTypes.NameIdentifier)`:**
- Encuentra el **PRIMERO** (email)
- Intenta parsearlo como `int`
- Falla → 401 Unauthorized

### Soluciones Intentadas

#### ❌ Solución 1: Desactivar mapeo automático (NO funcionó)

```csharp
// Esto NO resuelve el problema completamente
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
```

**Por qué no funciona:** El mapeo ocurre durante la validación del token, no solo durante la generación.

### ✅ Solución Final (Implementada)

Buscar **todos** los claims con tipo `NameIdentifier` y encontrar específicamente el que se puede parsear como `int`:

```csharp
// Buscar el claim NameIdentifier que sea un número
var userIdClaim = context.User.Claims
    .Where(c => c.Type == ClaimTypes.NameIdentifier)
    .FirstOrDefault(c => int.TryParse(c.Value, out _));

if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
{
    return Results.Unauthorized();
}
```

### Ubicación del Fix

- **Archivo:** `backend/Descansario.Api/Program.cs`
- **Endpoint:** `GET /api/auth/me`
- **Líneas:** ~221-228
- **Commit:** `7f6b1f3` - "fix(backend): Buscar el claim NameIdentifier numérico en lugar del primero"

### Debugging: Listar Claims

Si necesitas debuggear claims en el futuro:

```csharp
// Listar TODOS los claims presentes
foreach (var claim in context.User.Claims)
{
    Log.Information("Claim Type: {Type} | Value: {Value}", claim.Type, claim.Value);
}

// Ver qué encuentra FindFirst()
var firstClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
Log.Information("FindFirst(NameIdentifier): {Value}", firstClaim?.Value ?? "NULL");
```

### Alternativas Futuras

Si en el futuro quieres evitar este problema completamente:

#### Opción A: Usar un claim type personalizado

```csharp
// En lugar de ClaimTypes.NameIdentifier
new Claim("user_id", user.Id.ToString())

// Buscar con:
var userIdClaim = context.User.FindFirst("user_id");
```

#### Opción B: No usar 'sub' con email

```csharp
// Usar user ID en 'sub' en lugar del email
new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
new Claim(ClaimTypes.Email, user.Email),
```

Luego:
```csharp
var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
// Ahora solo habrá uno, mapeado desde 'sub'
```

### Lecciones Aprendidas

1. **ASP.NET Core mapea claims automáticamente** - estar consciente de esto
2. **`FindFirst()` devuelve el PRIMERO** - no necesariamente el que quieres
3. **JWT `sub` claim es especial** - siempre se mapea a `NameIdentifier`
4. **Logging es crítico** - sin ver los claims era imposible debuggear
5. **Tokens nuevos no resuelven problemas estructurales** - el problema persistía en cada nuevo token

### Referencias

- [ASP.NET Core JWT Claims Mapping](https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtsecuritytokenhandler.defaultinboundclaimtypemap)
- [JWT Registered Claim Names](https://www.rfc-editor.org/rfc/rfc7519#section-4.1)
- [ClaimTypes Class](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes)

---

**Última actualización:** 2025-11-18
**Problema resuelto en:** Fase 5 - Autenticación JWT
