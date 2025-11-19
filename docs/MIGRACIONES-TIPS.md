# 📋 Tips para Migraciones de Entity Framework

## ⚠️ IMPORTANTE: Siempre usar el comando de dotnet ef

Para crear migraciones **SIEMPRE** usar el comando oficial:

```bash
cd backend/Descansario.Api
dotnet ef migrations add NombreMigracion
```

Este comando crea automáticamente **DOS archivos**:
1. `TIMESTAMP_NombreMigracion.cs` - La migración en sí
2. `TIMESTAMP_NombreMigracion.Designer.cs` - El snapshot del modelo

## ❌ Evitar crear migraciones manualmente

Si se crean manualmente, es fácil olvidarse del archivo `.Designer.cs` que es **necesario** para que EF Core funcione correctamente.

## ✅ Comandos útiles

### Crear migración
```bash
cd backend/Descansario.Api
dotnet ef migrations add NombreMigracion
```

### Aplicar migraciones a la DB
```bash
dotnet ef database update
```

### Ver lista de migraciones
```bash
dotnet ef migrations list
```

### Revertir última migración
```bash
dotnet ef migrations remove
```

### Generar SQL script (para producción)
```bash
dotnet ef migrations script
```

## 🔍 Verificar que la migración esté completa

Después de crear una migración, verificar que existen **ambos archivos**:

```bash
ls -l backend/Descansario.Api/Migrations/ | grep NombreMigracion
```

Deberías ver:
- `TIMESTAMP_NombreMigracion.cs`
- `TIMESTAMP_NombreMigracion.Designer.cs`

Si falta el `.Designer.cs`, la migración está **incompleta** ⚠️

## 🐳 Migraciones con Docker

Las migraciones se aplican **automáticamente** al iniciar el contenedor (ver `Program.cs`):

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DescansarioDbContext>();
    db.Database.Migrate(); // ← Auto-migración
}
```

Por eso solo necesitás crear la migración, no aplicarla manualmente.

## 📝 Checklist antes de commit

Cuando hagas una migración, verifica:

- [ ] Existe el archivo `.cs` de la migración
- [ ] Existe el archivo `.Designer.cs` de la migración
- [ ] Se actualizó `DescansarioDbContextModelSnapshot.cs`
- [ ] Los cambios compilan sin errores
- [ ] Se agregaron ambos archivos al commit con `git add`

## 🚀 Workflow recomendado

1. Modificar modelos en `Models/`
2. Crear migración con `dotnet ef migrations add`
3. Revisar los archivos generados
4. Testear con `docker-compose up --build`
5. Commit y push

---

**Última actualización:** 2025-11-19
