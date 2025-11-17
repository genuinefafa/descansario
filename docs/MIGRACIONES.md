# Migraciones de Entity Framework Core - Guía de Referencia

## ⚠️ IMPORTANTE: Estructura de Migraciones

Cada migración de EF Core requiere **3 archivos**:

1. `{yyyyMMddHHmmss}_{Nombre}.cs` - La migración (Up/Down methods)
2. `{yyyyMMddHHmmss}_{Nombre}.Designer.cs` - Metadata (REQUERIDO)
3. `{Context}ModelSnapshot.cs` - Snapshot del modelo actual

### ❌ Error Común
```
20251115_InitialCreate.cs  ← INCORRECTO (solo fecha)
```

### ✅ Formato Correcto
```
20251115100000_InitialCreate.cs  ← CORRECTO (fecha + hora completa)
     └─ yyyyMMddHHmmss
```

## 🛠️ Comandos de Migración

### Crear Nueva Migración
```bash
cd backend/Descansario.Api
dotnet ef migrations add NombreDescriptivo
```

Esto crea automáticamente los 3 archivos necesarios.

### Ver Migraciones
```bash
dotnet ef migrations list
```

### Aplicar Migraciones
```bash
# Automático (recomendado) - al iniciar la API
dotnet run

# Manual
dotnet ef database update

# A una migración específica
dotnet ef database update NombreMigracion
```

### Revertir Última Migración
```bash
dotnet ef migrations remove
```

### Generar Script SQL
```bash
dotnet ef migrations script
dotnet ef migrations script 0 NombreMigracion  # Desde inicio hasta migración específica
```

## 🔄 Workflow Correcto

### Al Modificar Modelos:

1. **Modificar el modelo** (ej: agregar campo `Notes` a `Vacation.cs`)

2. **Crear migración**:
   ```bash
   dotnet ef migrations add AddNotesToVacations
   ```

3. **Verificar archivos generados**:
   ```
   ✅ {timestamp}_AddNotesToVacations.cs
   ✅ {timestamp}_AddNotesToVacations.Designer.cs
   ✅ DescansarioDbContextModelSnapshot.cs (actualizado)
   ```

4. **Aplicar migración**:
   - Automático: `dotnet run` (recomendado)
   - Manual: `dotnet ef database update`

5. **Commit**:
   ```bash
   git add Migrations/
   git commit -m "Agregar campo Notes al modelo Vacation"
   ```

## 🚫 NO Hacer

### ❌ Crear migraciones manualmente
```bash
# NO HACER ESTO:
touch 20251115_MyMigration.cs
```

**Problema**: Faltarán los archivos `.Designer.cs` y el formato del timestamp puede ser incorrecto.

### ❌ Modificar migraciones ya aplicadas
Si una migración ya está en producción/compartida, NO la modifiques. Crea una nueva.

### ❌ Eliminar migraciones del historial
No borres migraciones viejas que ya se aplicaron en otros ambientes.

## ✅ Buenas Prácticas

1. **Nombres descriptivos**: `AddNotesToVacations`, no `Migration1`

2. **Una migración por cambio lógico**: No mezclar múltiples features en una migración

3. **Probar Down()**: Verificar que la reversión funcione
   ```bash
   dotnet ef database update PreviousMigration
   dotnet ef database update  # volver a la última
   ```

4. **Revisar código generado**: EF Core es bueno pero no perfecto, revisa los archivos generados

5. **Documentar cambios complejos**: Agregar comentarios en migraciones complejas

## 🐛 Troubleshooting

### "No migrations were found"
**Causa**: Falta el archivo `.Designer.cs` o formato de timestamp incorrecto

**Solución**: Regenerar con `dotnet ef migrations add`

### "The migration has already been applied to the database"
**Causa**: Intentando aplicar una migración que ya existe en `__EFMigrationsHistory`

**Solución**:
```bash
# Ver qué migraciones están aplicadas
dotnet ef migrations list

# Si necesitas reaplicar, primero revertir
dotnet ef database update PreviousMigration
```

### "Could not load assembly 'Descansario.Api'"
**Causa**: Errores de compilación o dependencias faltantes

**Solución**:
```bash
dotnet restore
dotnet build
```

## 📋 Checklist para Nueva Migración

- [ ] Modelo modificado y compilando sin errores
- [ ] Ejecutar `dotnet ef migrations add NombreDescriptivo`
- [ ] Verificar que se crearon 3 archivos (o se actualizó el snapshot)
- [ ] Revisar código generado en `Up()` y `Down()`
- [ ] Probar migración: `dotnet ef database update`
- [ ] Verificar que la DB tiene el cambio esperado
- [ ] Commit de los archivos de migración
- [ ] Documentar cambios significativos en el PR

## 🔗 Referencias

- [EF Core Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core CLI Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
- Archivo: `backend/Descansario.Api/Program.cs` (líneas 35-38) - Auto-aplicación de migraciones

---

**Última actualización**: 2025-11-17
**Lecciones aprendidas**: Nunca crear migraciones manualmente, siempre usar `dotnet ef migrations add`
