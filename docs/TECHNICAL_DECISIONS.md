# Decisiones Técnicas - Descansario

Este documento registra las decisiones técnicas importantes tomadas durante el desarrollo del proyecto.

## Sprint 4 - Estabilización

### Warning de SvelteKit Fetch

**Fecha:** 2025-11-21

**Problema:**
SvelteKit muestra un warning cuando se usa `window.fetch` en lugar del `fetch` que se pasa a las funciones `load`:
```
Loading using `window.fetch`. For best results, use the `fetch` that is passed to your `load` function
```

**Contexto:**
- La aplicación usa autenticación JWT almacenada en `localStorage`
- `localStorage` no está disponible durante SSR (Server-Side Rendering)
- Las rutas protegidas requieren el token JWT para todas las peticiones API

**Decisión:**
Mantener el uso de `window.fetch` en el servicio `api.ts` por las siguientes razones:

1. **Incompatibilidad con SSR**: El token JWT se almacena en `localStorage`, que no existe en el servidor
2. **Simplicidad**: Todas las páginas autenticadas necesitan el token, lo que requeriría lógica adicional para manejar SSR vs CSR
3. **Rendimiento aceptable**: La aplicación es principalmente CSR (Client-Side Rendering) después del login
4. **Trade-off consciente**: Perdemos los beneficios de deduplicación de SvelteKit, pero ganamos simplicidad

**Alternativas consideradas:**

1. **Usar cookies en lugar de localStorage**
   - ✅ Pros: Compatible con SSR, fetch de SvelteKit funciona
   - ❌ Contras: Requiere cambios en backend y frontend, más complejo para CORS

2. **Implementar fetch condicional**
   - ✅ Pros: Usa SvelteKit fetch cuando está disponible
   - ❌ Contras: Código más complejo, difícil de mantener

3. **Pre-renderizar páginas públicas**
   - ✅ Pros: Mejor SEO para login/register
   - ❌ Contras: Páginas autenticadas siguen siendo CSR

**Conclusión:**
Para una futura iteración, considerar migrar a autenticación basada en cookies HTTP-only si el SEO o el rendimiento inicial se vuelven críticos.

---

## Sprint 4 - AvailableDays por Año

**Fecha:** 2025-11-21

**Problema:**
Actualmente, `Person.AvailableDays` es un número fijo (ej: 20 días), pero en realidad los días disponibles pueden variar por año (ej: 20 en 2025, 25 en 2026).

**Contexto:**
- El cálculo de estadísticas asume que los días disponibles son constantes
- En algunas organizaciones, los días de vacaciones aumentan con antigüedad
- El campo actual es suficiente para MVP pero limitado a largo plazo

**Decisión:**
Documentar la limitación y postergar la implementación por las siguientes razones:

1. **Complejidad vs Valor**: Implementar tabla `PersonYearlyAllowance` es ~2-3 horas de trabajo
2. **Prioridad**: El Sprint 4 se enfoca en estabilización, no en features nuevas
3. **MVP suficiente**: La mayoría de organizaciones tienen días fijos por año
4. **Fácil de agregar después**: El cambio es backward-compatible (se puede migrar el dato existente como default)

**Plan futuro (Sprint posterior):**

```sql
-- Nueva tabla para días por año
CREATE TABLE PersonYearlyAllowance (
    Id INTEGER PRIMARY KEY,
    PersonId INTEGER NOT NULL,
    Year INTEGER NOT NULL,
    AvailableDays INTEGER NOT NULL,
    FOREIGN KEY (PersonId) REFERENCES Persons(Id),
    UNIQUE(PersonId, Year)
);

-- Migrar datos existentes
INSERT INTO PersonYearlyAllowance (PersonId, Year, AvailableDays)
SELECT Id, 2025, AvailableDays FROM Persons;
```

**Workaround temporal:**
- Admins pueden ajustar manualmente `Person.AvailableDays` al cambiar de año
- Documentar esta limitación en la UI o docs de usuario

---

## Sprint 4 - Refactor a Rutas Independientes

**Fecha:** 2025-11-21

**Decisión:**
Migrar de arquitectura de tabs (`/?tab=persons`) a rutas independientes (`/persons`, `/vacations`, etc.).

**Razones:**
1. **URLs semánticas**: Mejor para compartir y bookmarking
2. **Historial del navegador**: El botón "Volver" funciona correctamente
3. **Mejor UX**: Navegación más intuitiva
4. **SEO**: Aunque la app es privada, mejor estructura de URLs

**Implementación:**
- `/` → Dashboard con resumen
- `/persons` → Lista de personas
- `/vacations` → Lista de vacaciones
- `/holidays` → Lista de feriados
- `/calendar` → Vista de calendario
- `/stats` → Estadísticas (ya existía)

**Beneficios observados:**
- Código más mantenible (cada ruta en su archivo)
- Mejor separación de responsabilidades
- Layout compartido con navegación global
- Edición desde calendario ahora navega a `/vacations?highlight={id}`
