# 🗺️ Roadmap - Descansario

**Última actualización:** 2025-11-19
**Estado actual:** Sistema base completo, listo para features de productividad

---

## 📍 Estado Actual del Proyecto

### ✅ Completado (Base del Sistema)

**Backend:**
- ✅ API REST completa (.NET 8 + Minimal APIs)
- ✅ 30+ endpoints implementados
- ✅ Entity Framework Core + SQLite
- ✅ Cálculo de días hábiles (WorkingDaysCalculator)
- ✅ Sistema de feriados con sync desde API externa
- ✅ Validaciones de negocio
- ✅ Swagger/OpenAPI documentation
- ✅ **Autenticación JWT completa**
- ✅ **Rate limiting con AspNetCoreRateLimit**
- ✅ **Logging estructurado con Serilog**
- ✅ **Protección de endpoints con [Authorize]**

**Frontend:**
- ✅ SvelteKit 5 + TypeScript + TailwindCSS
- ✅ CRUD completo: Personas, Vacaciones, Feriados
- ✅ Calendario continuo con scroll infinito
- ✅ Slots conectados (vacaciones consecutivas)
- ✅ Cálculo automático de días hábiles
- ✅ Interfaz por tabs (Personas, Vacaciones, Feriados, Calendario)
- ✅ **Página de login con autenticación**
- ✅ **Servicio y store de autenticación**
- ✅ **Protección de rutas con guards**
- ✅ **Auto-logout en 401**

**DevOps:**
- ✅ Docker + Docker Compose para desarrollo
- ✅ **Docker Compose para producción (docker-compose.prod.yml)**
- ✅ Auto-migración de base de datos
- ✅ CORS configurado para desarrollo
- ✅ **Healthchecks configurados**
- ✅ **Scripts de backup/restore automatizados**
- ✅ **Configuración nginx para reverse proxy**

**Seguridad:**
- ✅ **Autenticación JWT (7 días de expiración)**
- ✅ **BCrypt para hashing de passwords (work factor 12)**
- ✅ **Rate limiting (100 req/min global, 10/min login, 5/hour registro)**
- ✅ **Todos los endpoints protegidos (excepto /health y /api/auth/*)**
- ✅ **Variables de entorno para secrets**
- ✅ **Usuario admin seed**

---

## 🎯 Próximas Features (Roadmap Post-Base)

Este roadmap se enfoca en **agregar funcionalidades de productividad** que transforman el sistema de un CRUD básico a una herramienta empresarial completa.

---

## 🚀 Sprint 1: Vinculación User ↔ Person (1-2 días)

### Objetivo
Unificar la autenticación (User) con la gestión de vacaciones (Person), permitiendo registro automático por email.

### Problema Actual
- `User` y `Person` son entidades separadas sin relación
- Un usuario autenticado puede editar vacaciones de cualquier persona
- No hay ownership de vacaciones

### Solución: Registro Mágico por Email

**Flujo:**
```
1. Admin crea Person con email: pirulo@gmail.com
2. Pirulo se registra en /register con pirulo@gmail.com
3. Sistema detecta que existe Person con ese email
4. Auto-vincula User.PersonId = Person.Id
5. Pirulo ahora ve solo SUS vacaciones
```

### Cambios en Modelos

**User.cs:**
```csharp
public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Name { get; set; }
    public UserRole Role { get; set; } = UserRole.User;

    // ⭐ NUEVO: Vinculación con Person
    public int? PersonId { get; set; }
    public Person? Person { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
```

**Person.cs:** (no cambia, relación unidireccional)

### Cambios en AuthService

**Registro con auto-vinculación:**
```csharp
public async Task<AuthResponse> RegisterAsync(RegisterDto dto)
{
    // 1. Verificar si email ya existe como User
    if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
        throw new BadRequestException("User already registered");

    // 2. Buscar si existe Person con ese email
    var person = await _db.Persons
        .FirstOrDefaultAsync(p => p.Email == dto.Email);

    // 3. Crear User (vinculado si existe Person)
    var user = new User
    {
        Email = dto.Email,
        Name = dto.Name,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
        PersonId = person?.Id,  // ⭐ Auto-vinculación mágica
        Role = person != null ? UserRole.User : UserRole.Admin  // Admin si no hay Person
    };

    _db.Users.Add(user);
    await _db.SaveChangesAsync();

    // 4. Generar token
    var token = GenerateJwtToken(user);

    return new AuthResponse { Token = token, User = user };
}
```

### Migración

```bash
dotnet ef migrations add LinkUserToPerson
dotnet ef database update
```

**Migración manual:**
```csharp
public partial class LinkUserToPerson : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "PersonId",
            table: "Users",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_PersonId",
            table: "Users",
            column: "PersonId",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Users_Persons_PersonId",
            table: "Users",
            column: "PersonId",
            principalTable: "Persons",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }
}
```

### Nuevos Endpoints

```csharp
// GET /api/auth/me (mejorado)
app.MapGet("/api/auth/me", async (HttpContext ctx, DescansarioDbContext db) =>
{
    var userId = GetUserIdFromClaims(ctx);
    var user = await db.Users
        .Include(u => u.Person)  // ⭐ Incluir Person vinculada
        .FirstOrDefaultAsync(u => u.Id == userId);

    return Results.Ok(new
    {
        user.Id,
        user.Email,
        user.Name,
        user.Role,
        PersonId = user.PersonId,
        PersonName = user.Person?.Name
    });
})
.RequireAuthorization();
```

### Frontend

**Mostrar vinculación en UI:**
```typescript
// routes/+layout.svelte (o dashboard)
{#if $authStore.user?.personId}
  <div class="alert alert-success">
    ✅ Vinculado a: {$authStore.user.personName}
  </div>
{:else}
  <div class="alert alert-warning">
    ⚠️ No vinculado a ninguna persona. Contacta al admin.
  </div>
{/if}
```

### Checklist

- [ ] Agregar `PersonId` nullable a modelo User
- [ ] Crear migración `LinkUserToPerson`
- [ ] Modificar `RegisterAsync` para auto-vinculación por email
- [ ] Actualizar endpoint `/api/auth/me` para incluir Person
- [ ] Testing: crear Person, registrar User con mismo email
- [ ] Verificar que auto-vincula correctamente
- [ ] Actualizar frontend para mostrar estado de vinculación

### Testing Manual

```bash
# 1. Crear Person
POST /api/persons
{
  "name": "Pirulo Gómez",
  "email": "pirulo@gmail.com",
  "availableDays": 20
}

# 2. Registrar User
POST /api/auth/register
{
  "email": "pirulo@gmail.com",
  "password": "Test123!",
  "name": "Pirulo Gómez"
}

# 3. Verificar vinculación
GET /api/auth/me
Response should include:
{
  "personId": 1,
  "personName": "Pirulo Gómez"
}
```

---

## 🚀 Sprint 2: Mejora Visualización Calendario (2-3 días)

### Objetivo
Mostrar en el calendario cuántos días hábiles tiene registrado cada usuario, para visibilidad inmediata de quién está utilizando más vacaciones.

### Problema Actual
El calendario muestra slots de vacaciones, pero no hay un resumen rápido de:
- Cuántos días hábiles tiene cada persona en el rango visible
- Quién está usando más/menos días

### Solución: Badge de Días Hábiles por Persona

**Vista propuesta:**
```
┌────────────────────────────────────────────────┐
│ Calendario - Julio 2025                        │
├────────────────────────────────────────────────┤
│ Juan Pérez [12 días] ▓▓▓▓▓▓▓▓░░░░░░░░          │
│ María García [5 días] ▓▓▓░░░░░░░░░░░░░░        │
│ Pedro López [0 días] ░░░░░░░░░░░░░░░░░░        │
└────────────────────────────────────────────────┘
```

### Backend: Nuevo Endpoint

```csharp
// GET /api/calendar/summary?startDate=2025-07-01&endDate=2025-07-31
app.MapGet("/api/calendar/summary", async (
    DateTime startDate,
    DateTime endDate,
    DescansarioDbContext db) =>
{
    var persons = await db.Persons
        .Include(p => p.Vacations)
        .ToListAsync();

    var summary = persons.Select(p => new
    {
        PersonId = p.Id,
        PersonName = p.Name,
        WorkingDaysInRange = p.Vacations
            .Where(v => v.Status == VacationStatus.Approved &&
                       v.StartDate <= endDate &&
                       v.EndDate >= startDate)
            .Sum(v => CalculateWorkingDaysInRange(v, startDate, endDate)),
        AvailableDays = p.AvailableDays
    })
    .OrderByDescending(x => x.WorkingDaysInRange)
    .ToList();

    return Results.Ok(summary);
})
.RequireAuthorization();

// Helper: calcular días hábiles solo en el rango visible
int CalculateWorkingDaysInRange(Vacation v, DateTime rangeStart, DateTime rangeEnd)
{
    var effectiveStart = v.StartDate > rangeStart ? v.StartDate : rangeStart;
    var effectiveEnd = v.EndDate < rangeEnd ? v.EndDate : rangeEnd;

    if (effectiveStart > effectiveEnd) return 0;

    // Usar WorkingDaysCalculator existente
    return workingDaysCalculator.Calculate(effectiveStart, effectiveEnd);
}
```

### Frontend: Componente CalendarSummary

**Ubicación:** `frontend/src/lib/components/CalendarSummary.svelte`

```svelte
<script lang="ts">
  import { onMount } from 'svelte';
  import { calendarService } from '$lib/services/calendar';

  export let startDate: Date;
  export let endDate: Date;

  let summary: Array<{
    personId: number;
    personName: string;
    workingDaysInRange: number;
    availableDays: number;
  }> = [];

  onMount(async () => {
    summary = await calendarService.getSummary(startDate, endDate);
  });

  function getUsageColor(used: number, available: number) {
    const percentage = (used / available) * 100;
    if (percentage < 30) return 'text-green-600';
    if (percentage < 70) return 'text-yellow-600';
    return 'text-red-600';
  }
</script>

<div class="calendar-summary bg-white p-4 rounded-lg shadow mb-4">
  <h3 class="text-lg font-semibold mb-3">
    Resumen del Período
  </h3>

  <div class="space-y-2">
    {#each summary as person}
      <div class="flex items-center justify-between">
        <span class="font-medium">{person.personName}</span>
        <span class={`text-sm font-bold ${getUsageColor(person.workingDaysInRange, person.availableDays)}`}>
          {person.workingDaysInRange} días
        </span>
      </div>

      <!-- Barra de progreso visual -->
      <div class="w-full bg-gray-200 rounded-full h-2">
        <div
          class="bg-blue-600 h-2 rounded-full transition-all"
          style="width: {Math.min((person.workingDaysInRange / person.availableDays) * 100, 100)}%"
        ></div>
      </div>
    {/each}
  </div>

  {#if summary.length === 0}
    <p class="text-gray-500 text-sm">No hay vacaciones en este período</p>
  {/if}
</div>
```

### Integración en Calendario

**frontend/src/routes/calendar/+page.svelte:**
```svelte
<script lang="ts">
  import CalendarSummary from '$lib/components/CalendarSummary.svelte';

  // ... código existente del calendario

  // Calcular rango visible (basado en scroll del calendario)
  $: visibleStartDate = getFirstVisibleDate();
  $: visibleEndDate = getLastVisibleDate();
</script>

<div class="calendar-page">
  <!-- ⭐ Agregar summary arriba del calendario -->
  <CalendarSummary
    startDate={visibleStartDate}
    endDate={visibleEndDate}
  />

  <!-- Calendario existente -->
  <div class="calendar-grid">
    <!-- ... -->
  </div>
</div>
```

### Alternativa: Tooltips en Slots

Otra opción es agregar tooltips al hover sobre cada slot de vacación:

```svelte
<!-- frontend/src/lib/components/VacationSlot.svelte -->
<div
  class="vacation-slot"
  title="📊 {vacation.workingDaysCount} días hábiles"
>
  {vacation.person.name}
</div>
```

### Checklist

- [ ] Crear endpoint `GET /api/calendar/summary`
- [ ] Implementar helper `CalculateWorkingDaysInRange`
- [ ] Testing backend: verificar conteo correcto
- [ ] Crear servicio `calendarService.getSummary()` en frontend
- [ ] Crear componente `CalendarSummary.svelte`
- [ ] Integrar en página de calendario
- [ ] Agregar colores según usage (verde/amarillo/rojo)
- [ ] Testing: verificar que actualiza al cambiar rango visible
- [ ] (Opcional) Agregar tooltips en slots individuales

### Testing Manual

```bash
# Backend
GET /api/calendar/summary?startDate=2025-07-01&endDate=2025-07-31

# Debe retornar:
[
  {
    "personId": 1,
    "personName": "Juan Pérez",
    "workingDaysInRange": 12,
    "availableDays": 20
  },
  {
    "personId": 2,
    "personName": "María García",
    "workingDaysInRange": 5,
    "availableDays": 20
  }
]
```

---

## 🚀 Sprint 3: Dashboard de Estadísticas (3-5 días)

### Objetivo
Vista resumen con estadísticas clave para cada persona: días disponibles, usados, pendientes, y restantes.

### Features

**Vista por Persona:**
```
┌─────────────────────────────────────────┐
│  Juan Pérez - Estadísticas 2025         │
├─────────────────────────────────────────┤
│ 📊 Total Disponible:    20 días         │
│ ✅ Aprobados:           12 días (60%)   │
│ ⏳ Pendientes:          5 días (25%)    │
│ ❌ Rechazados:          0 días (0%)     │
│ 💚 Restantes:           3 días (15%)    │
├─────────────────────────────────────────┤
│ Próximas Vacaciones:                    │
│  • 15-19 Jul: 5 días (Pendiente)        │
│  • 01-10 Ago: 7 días (Aprobado)         │
└─────────────────────────────────────────┘
```

### Backend: Endpoint de Stats

```csharp
// GET /api/persons/{id}/stats?year=2025
app.MapGet("/api/persons/{id:int}/stats", async (
    int id,
    int year,
    DescansarioDbContext db,
    WorkingDaysCalculator calculator) =>
{
    var person = await db.Persons
        .Include(p => p.Vacations)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (person == null) return Results.NotFound();

    var startOfYear = new DateTime(year, 1, 1);
    var endOfYear = new DateTime(year, 12, 31);

    var vacationsInYear = person.Vacations
        .Where(v => v.StartDate.Year == year || v.EndDate.Year == year)
        .ToList();

    var approved = vacationsInYear
        .Where(v => v.Status == VacationStatus.Approved)
        .Sum(v => v.WorkingDaysCount);

    var pending = vacationsInYear
        .Where(v => v.Status == VacationStatus.Pending)
        .Sum(v => v.WorkingDaysCount);

    var rejected = vacationsInYear
        .Where(v => v.Status == VacationStatus.Rejected)
        .Sum(v => v.WorkingDaysCount);

    var remaining = person.AvailableDays - approved - pending;

    var upcomingVacations = vacationsInYear
        .Where(v => v.StartDate >= DateTime.Today)
        .OrderBy(v => v.StartDate)
        .Take(5)
        .Select(v => new
        {
            v.Id,
            v.StartDate,
            v.EndDate,
            v.WorkingDaysCount,
            v.Status
        })
        .ToList();

    return Results.Ok(new
    {
        PersonId = person.Id,
        PersonName = person.Name,
        Year = year,
        Available = person.AvailableDays,
        Approved = approved,
        Pending = pending,
        Rejected = rejected,
        Remaining = remaining,
        UpcomingVacations = upcomingVacations
    });
})
.RequireAuthorization();

// GET /api/stats/overview?year=2025 (todas las personas)
app.MapGet("/api/stats/overview", async (
    int year,
    DescansarioDbContext db) =>
{
    var persons = await db.Persons
        .Include(p => p.Vacations)
        .ToListAsync();

    var overview = persons.Select(p => new
    {
        PersonId = p.Id,
        PersonName = p.Name,
        Available = p.AvailableDays,
        Used = p.Vacations
            .Where(v => v.Status == VacationStatus.Approved &&
                       v.StartDate.Year == year)
            .Sum(v => v.WorkingDaysCount),
        Pending = p.Vacations
            .Where(v => v.Status == VacationStatus.Pending &&
                       v.StartDate.Year == year)
            .Sum(v => v.WorkingDaysCount)
    })
    .Select(x => new
    {
        x.PersonId,
        x.PersonName,
        x.Available,
        x.Used,
        x.Pending,
        Remaining = x.Available - x.Used - x.Pending,
        UsagePercentage = (x.Used / (double)x.Available) * 100
    })
    .OrderByDescending(x => x.UsagePercentage)
    .ToList();

    return Results.Ok(overview);
})
.RequireAuthorization();
```

### Frontend: Página de Estadísticas

**frontend/src/routes/stats/+page.svelte:**
```svelte
<script lang="ts">
  import { onMount } from 'svelte';
  import { statsService } from '$lib/services/stats';
  import StatsCard from '$lib/components/StatsCard.svelte';
  import PersonStatsDetail from '$lib/components/PersonStatsDetail.svelte';

  let overview = [];
  let selectedPersonId: number | null = null;
  let year = new Date().getFullYear();

  onMount(async () => {
    overview = await statsService.getOverview(year);
  });

  async function selectPerson(personId: number) {
    selectedPersonId = personId;
  }
</script>

<div class="stats-page p-6">
  <h1 class="text-3xl font-bold mb-6">📊 Estadísticas de Vacaciones {year}</h1>

  <!-- Selector de año -->
  <div class="mb-6">
    <select bind:value={year} class="select select-bordered">
      <option value={2024}>2024</option>
      <option value={2025}>2025</option>
      <option value={2026}>2026</option>
    </select>
  </div>

  <!-- Overview de todas las personas -->
  <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-8">
    {#each overview as person}
      <StatsCard
        {person}
        on:click={() => selectPerson(person.personId)}
      />
    {/each}
  </div>

  <!-- Detalle de persona seleccionada -->
  {#if selectedPersonId}
    <PersonStatsDetail personId={selectedPersonId} {year} />
  {/if}
</div>
```

**frontend/src/lib/components/StatsCard.svelte:**
```svelte
<script lang="ts">
  export let person: {
    personName: string;
    available: number;
    used: number;
    pending: number;
    remaining: number;
    usagePercentage: number;
  };

  function getColorClass(percentage: number) {
    if (percentage < 50) return 'bg-green-100 border-green-500';
    if (percentage < 80) return 'bg-yellow-100 border-yellow-500';
    return 'bg-red-100 border-red-500';
  }
</script>

<div
  class="stats-card border-l-4 p-4 rounded-lg shadow hover:shadow-lg cursor-pointer transition {getColorClass(person.usagePercentage)}"
  on:click
>
  <h3 class="font-bold text-lg mb-2">{person.personName}</h3>

  <div class="stats-grid text-sm space-y-1">
    <div class="flex justify-between">
      <span class="text-gray-600">Disponible:</span>
      <span class="font-semibold">{person.available} días</span>
    </div>

    <div class="flex justify-between">
      <span class="text-gray-600">Usados:</span>
      <span class="font-semibold text-blue-600">{person.used} días</span>
    </div>

    <div class="flex justify-between">
      <span class="text-gray-600">Pendientes:</span>
      <span class="font-semibold text-yellow-600">{person.pending} días</span>
    </div>

    <div class="flex justify-between">
      <span class="text-gray-600">Restantes:</span>
      <span class="font-semibold text-green-600">{person.remaining} días</span>
    </div>
  </div>

  <!-- Barra de progreso -->
  <div class="mt-3">
    <div class="w-full bg-gray-200 rounded-full h-2">
      <div
        class="bg-blue-600 h-2 rounded-full"
        style="width: {person.usagePercentage}%"
      ></div>
    </div>
    <p class="text-xs text-gray-500 mt-1 text-right">
      {person.usagePercentage.toFixed(1)}% utilizado
    </p>
  </div>
</div>
```

### Checklist

- [ ] Crear endpoint `GET /api/persons/{id}/stats`
- [ ] Crear endpoint `GET /api/stats/overview`
- [ ] Testing backend: verificar cálculos correctos
- [ ] Crear servicio `statsService` en frontend
- [ ] Crear componente `StatsCard.svelte`
- [ ] Crear componente `PersonStatsDetail.svelte`
- [ ] Crear página `routes/stats/+page.svelte`
- [ ] Agregar link a Stats en navegación principal
- [ ] Testing: verificar que stats actualizan al cambiar año

---

## 🚀 Sprint 4: Vista de Conflictos/Cobertura (2-3 días)

### Objetivo
Identificar días donde muchas personas están de vacaciones simultáneamente, para prevenir falta de cobertura.

### Backend: Endpoint de Conflictos

```csharp
// GET /api/calendar/conflicts?startDate=2025-01-01&endDate=2025-12-31&threshold=2
app.MapGet("/api/calendar/conflicts", async (
    DateTime startDate,
    DateTime endDate,
    int threshold,  // Mínimo de personas para considerar conflicto
    DescansarioDbContext db) =>
{
    var vacations = await db.Vacations
        .Include(v => v.Person)
        .Where(v => v.Status == VacationStatus.Approved &&
                   v.StartDate <= endDate &&
                   v.EndDate >= startDate)
        .ToListAsync();

    var conflicts = new List<object>();

    // Iterar cada día en el rango
    for (var date = startDate; date <= endDate; date = date.AddDays(1))
    {
        var personsOnVacation = vacations
            .Where(v => date >= v.StartDate && date <= v.EndDate)
            .Select(v => new
            {
                PersonId = v.Person.Id,
                PersonName = v.Person.Name
            })
            .ToList();

        if (personsOnVacation.Count >= threshold)
        {
            conflicts.Add(new
            {
                Date = date,
                Count = personsOnVacation.Count,
                Persons = personsOnVacation,
                ConflictLevel = GetConflictLevel(personsOnVacation.Count)
            });
        }
    }

    return Results.Ok(conflicts);
})
.RequireAuthorization();

string GetConflictLevel(int count)
{
    if (count >= 5) return "high";
    if (count >= 3) return "medium";
    return "low";
}
```

### Frontend: Vista de Conflictos

**frontend/src/routes/conflicts/+page.svelte:**
```svelte
<script lang="ts">
  import { onMount } from 'svelte';
  import { conflictsService } from '$lib/services/conflicts';

  let conflicts = [];
  let threshold = 2;
  let startDate = new Date(new Date().getFullYear(), 0, 1);
  let endDate = new Date(new Date().getFullYear(), 11, 31);

  onMount(async () => {
    await loadConflicts();
  });

  async function loadConflicts() {
    conflicts = await conflictsService.getConflicts(startDate, endDate, threshold);
  }

  function getConflictColor(level: string) {
    switch(level) {
      case 'high': return 'bg-red-100 border-red-500';
      case 'medium': return 'bg-yellow-100 border-yellow-500';
      default: return 'bg-blue-100 border-blue-500';
    }
  }
</script>

<div class="conflicts-page p-6">
  <h1 class="text-3xl font-bold mb-6">⚠️ Conflictos de Cobertura</h1>

  <!-- Filtros -->
  <div class="filters mb-6 flex gap-4">
    <div>
      <label class="label">Mínimo de personas:</label>
      <input
        type="number"
        bind:value={threshold}
        min="2"
        class="input input-bordered"
        on:change={loadConflicts}
      />
    </div>

    <div>
      <label class="label">Desde:</label>
      <input
        type="date"
        bind:value={startDate}
        class="input input-bordered"
        on:change={loadConflicts}
      />
    </div>

    <div>
      <label class="label">Hasta:</label>
      <input
        type="date"
        bind:value={endDate}
        class="input input-bordered"
        on:change={loadConflicts}
      />
    </div>
  </div>

  <!-- Lista de conflictos -->
  <div class="conflicts-list space-y-3">
    {#if conflicts.length === 0}
      <div class="alert alert-success">
        ✅ No hay conflictos de cobertura en este período
      </div>
    {:else}
      {#each conflicts as conflict}
        <div class="conflict-card border-l-4 p-4 rounded-lg {getConflictColor(conflict.conflictLevel)}">
          <div class="flex justify-between items-center mb-2">
            <h3 class="font-bold">
              📅 {new Date(conflict.date).toLocaleDateString('es-ES', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}
            </h3>
            <span class="badge badge-lg">
              {conflict.count} personas ausentes
            </span>
          </div>

          <div class="persons-list">
            <p class="text-sm text-gray-600 mb-1">Personas de vacaciones:</p>
            <ul class="list-disc list-inside">
              {#each conflict.persons as person}
                <li class="text-sm">{person.personName}</li>
              {/each}
            </ul>
          </div>
        </div>
      {/each}
    {/if}
  </div>
</div>
```

### Integración con Calendario

Agregar highlights visuales en el calendario para días con conflictos:

```svelte
<!-- frontend/src/lib/components/Calendar.svelte -->
<script>
  // Cargar conflictos del mes visible
  $: loadConflictsForMonth(currentMonth);

  function getDayClass(date: Date) {
    const conflictLevel = getConflictForDate(date);
    if (conflictLevel === 'high') return 'bg-red-200';
    if (conflictLevel === 'medium') return 'bg-yellow-200';
    return '';
  }
</script>

<div class="calendar-day {getDayClass(day)}">
  <!-- contenido del día -->
</div>
```

### Checklist

- [ ] Crear endpoint `GET /api/calendar/conflicts`
- [ ] Testing backend: verificar detección de conflictos
- [ ] Crear servicio `conflictsService` en frontend
- [ ] Crear página `routes/conflicts/+page.svelte`
- [ ] Agregar filtros (fecha, threshold)
- [ ] Agregar colores según nivel de conflicto
- [ ] Integrar highlights en calendario principal
- [ ] Agregar link en navegación

---

## 🚀 Sprint 5: Exportación iCal (2-3 días)

### Objetivo
Permitir a los usuarios suscribirse a sus vacaciones desde Google Calendar, Outlook, Apple Calendar, etc.

### Backend: Generación de iCal

**iCalService.cs:**
```csharp
public class ICalService
{
    public string GenerateICalForPerson(Person person, List<Vacation> vacations)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//Descansario//Vacation Calendar//ES");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");
        sb.AppendLine($"X-WR-CALNAME:Vacaciones - {person.Name}");
        sb.AppendLine("X-WR-TIMEZONE:America/Argentina/Buenos_Aires");

        // Events
        foreach (var vacation in vacations.Where(v => v.Status == VacationStatus.Approved))
        {
            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:vacation-{vacation.Id}@descansario.com");
            sb.AppendLine($"DTSTAMP:{FormatDate(DateTime.UtcNow)}");
            sb.AppendLine($"DTSTART;VALUE=DATE:{FormatDate(vacation.StartDate)}");
            sb.AppendLine($"DTEND;VALUE=DATE:{FormatDate(vacation.EndDate.AddDays(1))}"); // +1 para exclusivo
            sb.AppendLine($"SUMMARY:Vacaciones ({vacation.WorkingDaysCount} días)");
            sb.AppendLine($"DESCRIPTION:{vacation.Notes ?? "Vacaciones programadas"}");
            sb.AppendLine("STATUS:CONFIRMED");
            sb.AppendLine("TRANSP:TRANSPARENT");
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");

        return sb.ToString();
    }

    private string FormatDate(DateTime date)
    {
        return date.ToString("yyyyMMdd");
    }

    private string FormatDateTime(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("yyyyMMddTHHmmssZ");
    }
}
```

**Endpoint:**
```csharp
// GET /api/persons/{id}/calendar.ics
app.MapGet("/api/persons/{id:int}/calendar.ics", async (
    int id,
    DescansarioDbContext db,
    ICalService icalService,
    HttpContext ctx) =>
{
    // Verificar que el usuario solo acceda a su propio calendario (o sea admin)
    var userId = GetUserIdFromClaims(ctx);
    var user = await db.Users.FindAsync(userId);

    if (user.Role != UserRole.Admin && user.PersonId != id)
        return Results.Forbid();

    var person = await db.Persons
        .Include(p => p.Vacations)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (person == null) return Results.NotFound();

    var icalContent = icalService.GenerateICalForPerson(person, person.Vacations.ToList());

    return Results.Content(icalContent, "text/calendar", Encoding.UTF8);
})
.RequireAuthorization();
```

### Frontend: Botón de Suscripción

**frontend/src/lib/components/CalendarSubscribe.svelte:**
```svelte
<script lang="ts">
  import { authStore } from '$lib/stores/auth';

  $: personId = $authStore.user?.personId;
  $: icalUrl = personId ? `${import.meta.env.VITE_API_URL}/api/persons/${personId}/calendar.ics` : null;

  function copyToClipboard() {
    if (icalUrl) {
      navigator.clipboard.writeText(icalUrl);
      alert('URL copiada al portapapeles');
    }
  }
</script>

<div class="calendar-subscribe bg-blue-50 border border-blue-200 rounded-lg p-4">
  <h3 class="font-bold mb-2">📅 Suscribirse al Calendario</h3>

  <p class="text-sm text-gray-600 mb-3">
    Agrega tus vacaciones a Google Calendar, Outlook o Apple Calendar
  </p>

  {#if icalUrl}
    <div class="space-y-2">
      <!-- Google Calendar -->
      <a
        href="https://calendar.google.com/calendar/r?cid=webcal://{icalUrl.replace('https://', '')}"
        target="_blank"
        class="btn btn-sm btn-outline w-full"
      >
        <svg class="w-4 h-4 mr-2">...</svg>
        Agregar a Google Calendar
      </a>

      <!-- URL manual -->
      <div class="flex gap-2">
        <input
          type="text"
          value={icalUrl}
          readonly
          class="input input-sm input-bordered flex-1 text-xs"
        />
        <button
          on:click={copyToClipboard}
          class="btn btn-sm btn-outline"
        >
          📋 Copiar
        </button>
      </div>

      <details class="text-xs text-gray-500">
        <summary class="cursor-pointer">¿Cómo usar esta URL?</summary>
        <ul class="list-disc list-inside mt-2 space-y-1">
          <li><strong>Google Calendar:</strong> Settings → Add calendar → From URL</li>
          <li><strong>Outlook:</strong> Calendar → Add calendar → Subscribe from web</li>
          <li><strong>Apple Calendar:</strong> File → New Calendar Subscription</li>
        </ul>
      </details>
    </div>
  {:else}
    <p class="text-sm text-yellow-600">
      ⚠️ Debes estar vinculado a una persona para suscribirte al calendario
    </p>
  {/if}
</div>
```

### Testing

```bash
# 1. Obtener URL
GET /api/persons/1/calendar.ics

# 2. Verificar formato iCal
# Debe empezar con:
BEGIN:VCALENDAR
VERSION:2.0
...

# 3. Importar en Google Calendar
# Settings → Add calendar → From URL → Pegar URL
```

### Checklist

- [ ] Crear servicio `ICalService`
- [ ] Crear endpoint `GET /api/persons/{id}/calendar.ics`
- [ ] Testing: verificar formato iCal válido
- [ ] Testing: importar en Google Calendar
- [ ] Crear componente `CalendarSubscribe.svelte`
- [ ] Agregar botón en dashboard o página de stats
- [ ] Documentar instrucciones para cada cliente de calendario
- [ ] Verificar que solo el dueño o admin puede acceder

---

## 🚀 Sprint 6: Sistema de Permisos por Rol (3-4 días)

### Objetivo
Implementar control de acceso basado en roles: Users solo ven/editan sus vacaciones, Admins tienen acceso completo.

### Backend: Middleware de Autorización

**AuthorizationService.cs:**
```csharp
public class AuthorizationService
{
    private readonly DescansarioDbContext _db;

    public async Task<bool> CanAccessPerson(int userId, int personId)
    {
        var user = await _db.Users.FindAsync(userId);

        // Admin puede acceder a todo
        if (user.Role == UserRole.Admin)
            return true;

        // User solo puede acceder a su propia Person
        return user.PersonId == personId;
    }

    public async Task<bool> CanAccessVacation(int userId, int vacationId)
    {
        var user = await _db.Users.FindAsync(userId);

        if (user.Role == UserRole.Admin)
            return true;

        var vacation = await _db.Vacations.FindAsync(vacationId);
        return vacation?.PersonId == user.PersonId;
    }
}
```

**Endpoints protegidos:**
```csharp
// GET /api/persons - Solo devuelve las personas que el user puede ver
app.MapGet("/api/persons", async (HttpContext ctx, DescansarioDbContext db) =>
{
    var userId = GetUserIdFromClaims(ctx);
    var user = await db.Users.FindAsync(userId);

    IQueryable<Person> query = db.Persons;

    // Si NO es admin, filtrar solo su Person
    if (user.Role != UserRole.Admin)
    {
        if (user.PersonId == null)
            return Results.Ok(new List<Person>());

        query = query.Where(p => p.Id == user.PersonId);
    }

    var persons = await query.ToListAsync();
    return Results.Ok(persons);
})
.RequireAuthorization();

// POST /api/vacations - Solo puede crear para su PersonId
app.MapPost("/api/vacations", async (
    VacationDto dto,
    HttpContext ctx,
    DescansarioDbContext db) =>
{
    var userId = GetUserIdFromClaims(ctx);
    var user = await db.Users.FindAsync(userId);

    // Verificar que el PersonId coincide con el del user (excepto admins)
    if (user.Role != UserRole.Admin && dto.PersonId != user.PersonId)
        return Results.Forbid();

    // ... resto de la lógica
})
.RequireAuthorization();

// PUT /api/vacations/{id}
app.MapPut("/api/vacations/{id:int}", async (
    int id,
    VacationDto dto,
    HttpContext ctx,
    DescansarioDbContext db,
    AuthorizationService authz) =>
{
    var userId = GetUserIdFromClaims(ctx);

    if (!await authz.CanAccessVacation(userId, id))
        return Results.Forbid();

    // ... resto de la lógica
})
.RequireAuthorization();

// DELETE /api/vacations/{id}
app.MapDelete("/api/vacations/{id:int}", async (
    int id,
    HttpContext ctx,
    DescansarioDbContext db,
    AuthorizationService authz) =>
{
    var userId = GetUserIdFromClaims(ctx);

    if (!await authz.CanAccessVacation(userId, id))
        return Results.Forbid();

    var vacation = await db.Vacations.FindAsync(id);
    if (vacation == null) return Results.NotFound();

    db.Vacations.Remove(vacation);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization();
```

### Frontend: Ocultar UI según Rol

**authStore.ts:**
```typescript
export const authStore = writable({
  isAuthenticated: false,
  user: null as User | null,
  isAdmin: false,
  personId: null as number | null
});

// Helper
export function setUser(user: User) {
  authStore.set({
    isAuthenticated: true,
    user,
    isAdmin: user.role === 'Admin',
    personId: user.personId
  });
}
```

**Uso en componentes:**
```svelte
<script>
  import { authStore } from '$lib/stores/auth';
</script>

{#if $authStore.isAdmin}
  <button class="btn btn-danger" on:click={deleteAllData}>
    🗑️ Eliminar Todo (Admin)
  </button>
{/if}

<!-- Dropdown de personas: solo admins ven todas -->
<select bind:value={selectedPersonId}>
  {#if $authStore.isAdmin}
    {#each allPersons as person}
      <option value={person.id}>{person.name}</option>
    {/each}
  {:else}
    <option value={$authStore.personId}>Mi Perfil</option>
  {/if}
</select>
```

### Checklist

- [ ] Crear `AuthorizationService`
- [ ] Agregar filtrado por rol en `GET /api/persons`
- [ ] Proteger `POST /api/vacations` (solo su PersonId)
- [ ] Proteger `PUT /api/vacations/{id}`
- [ ] Proteger `DELETE /api/vacations/{id}`
- [ ] Proteger `DELETE /api/persons/{id}` (solo admin)
- [ ] Testing: User NO puede editar vacaciones de otros
- [ ] Testing: Admin puede editar todo
- [ ] Actualizar `authStore` con flag `isAdmin`
- [ ] Ocultar botones admin en frontend
- [ ] Testing E2E de permisos

---

## 🚀 Sprint 7: Flujo de Aprobaciones (5-7 días)

### Objetivo
Implementar workflow de aprobación: Users crean solicitudes (Pending), Admins aprueban/rechazan.

### Backend: Endpoints de Aprobación

```csharp
// GET /api/vacations/pending - Solo Admin
app.MapGet("/api/vacations/pending", async (
    HttpContext ctx,
    DescansarioDbContext db) =>
{
    var userId = GetUserIdFromClaims(ctx);
    var user = await db.Users.FindAsync(userId);

    if (user.Role != UserRole.Admin)
        return Results.Forbid();

    var pending = await db.Vacations
        .Include(v => v.Person)
        .Where(v => v.Status == VacationStatus.Pending)
        .OrderBy(v => v.StartDate)
        .ToListAsync();

    return Results.Ok(pending);
})
.RequireAuthorization();

// PUT /api/vacations/{id}/approve
app.MapPut("/api/vacations/{id:int}/approve", async (
    int id,
    HttpContext ctx,
    DescansarioDbContext db) =>
{
    var userId = GetUserIdFromClaims(ctx);
    var user = await db.Users.FindAsync(userId);

    if (user.Role != UserRole.Admin)
        return Results.Forbid();

    var vacation = await db.Vacations.FindAsync(id);
    if (vacation == null) return Results.NotFound();

    vacation.Status = VacationStatus.Approved;
    await db.SaveChangesAsync();

    // TODO: Enviar notificación al usuario

    return Results.Ok(vacation);
})
.RequireAuthorization();

// PUT /api/vacations/{id}/reject
app.MapPut("/api/vacations/{id:int}/reject", async (
    int id,
    string? reason,  // Razón del rechazo
    HttpContext ctx,
    DescansarioDbContext db) =>
{
    var userId = GetUserIdFromClaims(ctx);
    var user = await db.Users.FindAsync(userId);

    if (user.Role != UserRole.Admin)
        return Results.Forbid();

    var vacation = await db.Vacations.FindAsync(id);
    if (vacation == null) return Results.NotFound();

    vacation.Status = VacationStatus.Rejected;

    // Agregar razón en las notas
    if (!string.IsNullOrEmpty(reason))
    {
        vacation.Notes = vacation.Notes == null
            ? $"❌ Rechazado: {reason}"
            : $"{vacation.Notes}\n\n❌ Rechazado: {reason}";
    }

    await db.SaveChangesAsync();

    // TODO: Enviar notificación al usuario

    return Results.Ok(vacation);
})
.RequireAuthorization();
```

### Agregar campo RejectionReason (opcional)

**Migration:**
```csharp
migrationBuilder.AddColumn<string>(
    name: "RejectionReason",
    table: "Vacations",
    type: "TEXT",
    maxLength: 500,
    nullable: true);
```

### Frontend: Vista de Aprobaciones

**routes/approvals/+page.svelte:**
```svelte
<script lang="ts">
  import { onMount } from 'svelte';
  import { approvalsService } from '$lib/services/approvals';
  import { authStore } from '$lib/stores/auth';
  import { goto } from '$app/navigation';

  // Redirigir si no es admin
  $: if (!$authStore.isAdmin) {
    goto('/');
  }

  let pendingVacations = [];

  onMount(async () => {
    await loadPending();
  });

  async function loadPending() {
    pendingVacations = await approvalsService.getPending();
  }

  async function approve(vacationId: number) {
    if (confirm('¿Aprobar esta solicitud?')) {
      await approvalsService.approve(vacationId);
      await loadPending();
    }
  }

  async function reject(vacationId: number) {
    const reason = prompt('Razón del rechazo (opcional):');
    if (reason !== null) {  // null = cancelado
      await approvalsService.reject(vacationId, reason);
      await loadPending();
    }
  }
</script>

<div class="approvals-page p-6">
  <h1 class="text-3xl font-bold mb-6">✅ Solicitudes Pendientes</h1>

  {#if pendingVacations.length === 0}
    <div class="alert alert-info">
      ℹ️ No hay solicitudes pendientes
    </div>
  {:else}
    <div class="vacations-list space-y-4">
      {#each pendingVacations as vacation}
        <div class="vacation-card bg-white border border-gray-300 rounded-lg p-4 shadow">
          <div class="flex justify-between items-start mb-3">
            <div>
              <h3 class="font-bold text-lg">{vacation.person.name}</h3>
              <p class="text-sm text-gray-600">
                {new Date(vacation.startDate).toLocaleDateString()} - {new Date(vacation.endDate).toLocaleDateString()}
              </p>
              <p class="text-sm font-semibold text-blue-600">
                {vacation.workingDaysCount} días hábiles
              </p>
            </div>

            <span class="badge badge-warning">Pendiente</span>
          </div>

          {#if vacation.notes}
            <div class="notes bg-gray-50 p-2 rounded mb-3">
              <p class="text-sm">{vacation.notes}</p>
            </div>
          {/if}

          <div class="actions flex gap-2">
            <button
              class="btn btn-success btn-sm"
              on:click={() => approve(vacation.id)}
            >
              ✅ Aprobar
            </button>

            <button
              class="btn btn-error btn-sm"
              on:click={() => reject(vacation.id)}
            >
              ❌ Rechazar
            </button>
          </div>
        </div>
      {/each}
    </div>
  {/if}
</div>
```

### Badges de Estado

**Componente VacationStatusBadge.svelte:**
```svelte
<script lang="ts">
  export let status: 'Pending' | 'Approved' | 'Rejected';

  function getBadgeClass(status: string) {
    switch(status) {
      case 'Approved': return 'badge-success';
      case 'Rejected': return 'badge-error';
      default: return 'badge-warning';
    }
  }

  function getIcon(status: string) {
    switch(status) {
      case 'Approved': return '✅';
      case 'Rejected': return '❌';
      default: return '⏳';
    }
  }

  function getLabel(status: string) {
    switch(status) {
      case 'Approved': return 'Aprobado';
      case 'Rejected': return 'Rechazado';
      default: return 'Pendiente';
    }
  }
</script>

<span class="badge {getBadgeClass(status)}">
  {getIcon(status)} {getLabel(status)}
</span>
```

### Modificar Creación de Vacaciones

Por defecto, crear como Pending:

```svelte
<!-- routes/vacations/+page.svelte -->
<script>
  async function createVacation(data) {
    // Ya no enviamos status, backend lo pone como Pending
    await vacationsService.create({
      personId: data.personId,
      startDate: data.startDate,
      endDate: data.endDate,
      notes: data.notes
      // status: 'Pending' <- Removido, backend lo maneja
    });
  }
</script>
```

### Checklist

- [ ] Modificar creación de vacaciones para usar Status.Pending por defecto
- [ ] Crear endpoint `GET /api/vacations/pending`
- [ ] Crear endpoint `PUT /api/vacations/{id}/approve`
- [ ] Crear endpoint `PUT /api/vacations/{id}/reject`
- [ ] (Opcional) Agregar campo `RejectionReason` a modelo
- [ ] Crear servicio `approvalsService` en frontend
- [ ] Crear página `routes/approvals/+page.svelte`
- [ ] Crear componente `VacationStatusBadge`
- [ ] Agregar badges en lista de vacaciones
- [ ] Agregar link "Aprobar Solicitudes" en nav (solo admin)
- [ ] Testing: crear solicitud como user, aprobar como admin

---

## 🚀 Sprint 8: Notificaciones por Email (4-6 días)

### Objetivo
Enviar emails automáticos cuando cambia el estado de una solicitud de vacaciones.

### Setup: Configuración SMTP

**.env:**
```bash
# Email Configuration
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=tu-email@gmail.com
SMTP_PASSWORD=tu-app-password  # NO usar password real, usar App Password
SMTP_FROM_EMAIL=noreply@descansario.com
SMTP_FROM_NAME=Sistema Descansario
```

**appsettings.Production.json:**
```json
{
  "Email": {
    "SmtpHost": "${SMTP_HOST}",
    "SmtpPort": 587,
    "SmtpUser": "${SMTP_USER}",
    "SmtpPassword": "${SMTP_PASSWORD}",
    "FromEmail": "${SMTP_FROM_EMAIL}",
    "FromName": "${SMTP_FROM_NAME}",
    "EnableSsl": true
  }
}
```

### Backend: EmailService

**Instalar paquetes:**
```bash
dotnet add package MailKit
dotnet add package MimeKit
```

**EmailService.cs:**
```csharp
using MailKit.Net.Smtp;
using MimeKit;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendVacationApprovedEmail(string toEmail, string personName, Vacation vacation)
    {
        var subject = "✅ Tu solicitud de vacaciones fue aprobada";
        var body = $@"
            <h2>¡Buenas noticias, {personName}!</h2>
            <p>Tu solicitud de vacaciones ha sido <strong>aprobada</strong>.</p>

            <h3>Detalles:</h3>
            <ul>
                <li><strong>Desde:</strong> {vacation.StartDate:dd/MM/yyyy}</li>
                <li><strong>Hasta:</strong> {vacation.EndDate:dd/MM/yyyy}</li>
                <li><strong>Días hábiles:</strong> {vacation.WorkingDaysCount}</li>
            </ul>

            <p>¡Disfruta tus vacaciones! 🌴</p>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendVacationRejectedEmail(string toEmail, string personName, Vacation vacation, string? reason)
    {
        var subject = "❌ Tu solicitud de vacaciones fue rechazada";
        var body = $@"
            <h2>Hola {personName},</h2>
            <p>Lamentamos informarte que tu solicitud de vacaciones ha sido <strong>rechazada</strong>.</p>

            <h3>Detalles:</h3>
            <ul>
                <li><strong>Desde:</strong> {vacation.StartDate:dd/MM/yyyy}</li>
                <li><strong>Hasta:</strong> {vacation.EndDate:dd/MM/yyyy}</li>
                <li><strong>Días hábiles:</strong> {vacation.WorkingDaysCount}</li>
            </ul>

            {(string.IsNullOrEmpty(reason) ? "" : $"<p><strong>Razón:</strong> {reason}</p>")}

            <p>Por favor, contacta con tu supervisor para más detalles.</p>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendNewVacationRequestEmail(string adminEmail, string personName, Vacation vacation)
    {
        var subject = $"🔔 Nueva solicitud de vacaciones - {personName}";
        var body = $@"
            <h2>Nueva solicitud pendiente de aprobación</h2>
            <p><strong>{personName}</strong> ha solicitado vacaciones.</p>

            <h3>Detalles:</h3>
            <ul>
                <li><strong>Desde:</strong> {vacation.StartDate:dd/MM/yyyy}</li>
                <li><strong>Hasta:</strong> {vacation.EndDate:dd/MM/yyyy}</li>
                <li><strong>Días hábiles:</strong> {vacation.WorkingDaysCount}</li>
                {(string.IsNullOrEmpty(vacation.Notes) ? "" : $"<li><strong>Notas:</strong> {vacation.Notes}</li>")}
            </ul>

            <p><a href='https://tu-dominio.com/approvals'>Ver solicitudes pendientes</a></p>
        ";

        await SendEmailAsync(adminEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _config["Email:FromName"],
                _config["Email:FromEmail"]
            ));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _config["Email:SmtpHost"],
                int.Parse(_config["Email:SmtpPort"]),
                MailKit.Security.SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(
                _config["Email:SmtpUser"],
                _config["Email:SmtpPassword"]
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            // NO lanzar excepción, emails no deben bloquear el flujo principal
        }
    }
}
```

### Integrar en Endpoints

**Aprobar:**
```csharp
app.MapPut("/api/vacations/{id:int}/approve", async (
    int id,
    HttpContext ctx,
    DescansarioDbContext db,
    EmailService emailService) =>
{
    // ... validaciones

    vacation.Status = VacationStatus.Approved;
    await db.SaveChangesAsync();

    // Enviar email al usuario
    var person = await db.Persons.FindAsync(vacation.PersonId);
    await emailService.SendVacationApprovedEmail(
        person.Email,
        person.Name,
        vacation
    );

    return Results.Ok(vacation);
});
```

**Rechazar:**
```csharp
app.MapPut("/api/vacations/{id:int}/reject", async (
    int id,
    string? reason,
    HttpContext ctx,
    DescansarioDbContext db,
    EmailService emailService) =>
{
    // ... validaciones

    vacation.Status = VacationStatus.Rejected;
    await db.SaveChangesAsync();

    // Enviar email al usuario
    var person = await db.Persons.FindAsync(vacation.PersonId);
    await emailService.SendVacationRejectedEmail(
        person.Email,
        person.Name,
        vacation,
        reason
    );

    return Results.Ok(vacation);
});
```

**Nueva solicitud:**
```csharp
app.MapPost("/api/vacations", async (
    VacationDto dto,
    HttpContext ctx,
    DescansarioDbContext db,
    EmailService emailService) =>
{
    // ... crear vacación

    // Notificar a todos los admins
    var adminEmails = await db.Users
        .Where(u => u.Role == UserRole.Admin)
        .Select(u => u.Email)
        .ToListAsync();

    var person = await db.Persons.FindAsync(dto.PersonId);

    foreach (var adminEmail in adminEmails)
    {
        await emailService.SendNewVacationRequestEmail(
            adminEmail,
            person.Name,
            vacation
        );
    }

    return Results.Created($"/api/vacations/{vacation.Id}", vacation);
});
```

### Background Jobs (Opcional - Hangfire)

Para enviar emails en background sin bloquear requests:

```bash
dotnet add package Hangfire
dotnet add package Hangfire.Storage.SQLite
```

```csharp
// Program.cs
builder.Services.AddHangfire(config =>
    config.UseSQLiteStorage(connectionString));

builder.Services.AddHangfireServer();

// Uso
BackgroundJob.Enqueue(() => emailService.SendVacationApprovedEmail(...));
```

### Configurar Gmail App Password

1. Ir a Google Account → Security
2. Habilitar 2-Step Verification
3. App Passwords → Generate new
4. Usar ese password en `SMTP_PASSWORD`

### Checklist

- [ ] Instalar paquetes MailKit y MimeKit
- [ ] Crear `EmailService`
- [ ] Configurar variables SMTP en `.env`
- [ ] Implementar `SendVacationApprovedEmail`
- [ ] Implementar `SendVacationRejectedEmail`
- [ ] Implementar `SendNewVacationRequestEmail`
- [ ] Integrar en endpoint `/approve`
- [ ] Integrar en endpoint `/reject`
- [ ] Integrar en endpoint `POST /vacations`
- [ ] Configurar Gmail App Password
- [ ] Testing: crear solicitud, verificar email a admin
- [ ] Testing: aprobar, verificar email a usuario
- [ ] (Opcional) Integrar Hangfire para background jobs

---

## 📋 Resumen de Sprints

| Sprint | Feature | Días | Prioridad |
|--------|---------|------|-----------|
| 1 | Unificar User ↔ Person (registro mágico) | 1-2 | 🔴 Crítico |
| 2 | Mejora visualización calendario | 2-3 | 🟡 Alta |
| 3 | Dashboard de Estadísticas | 3-5 | 🟡 Alta |
| 4 | Vista de Conflictos/Cobertura | 2-3 | 🟢 Media |
| 5 | Exportación iCal | 2-3 | 🟢 Media |
| 6 | Sistema de Permisos por Rol | 3-4 | 🟡 Alta |
| 7 | Flujo de Aprobaciones | 5-7 | 🟡 Alta |
| 8 | Notificaciones por Email | 4-6 | 🟢 Media |

**Total estimado:** 22-33 días (~4-6 semanas)

---

## 🎯 Orden de Implementación Recomendado

Basado en dependencias técnicas:

```
Sprint 1 (User ↔ Person)
    ↓
Sprint 2 (Calendario mejorado) + Sprint 3 (Stats)  ← Pueden ir en paralelo
    ↓
Sprint 6 (Permisos)  ← Debe ir ANTES de aprobaciones
    ↓
Sprint 7 (Aprobaciones)
    ↓
Sprint 4 (Conflictos) + Sprint 5 (iCal)  ← Pueden ir en paralelo
    ↓
Sprint 8 (Emails)  ← Al final para no bloquear
```

---

## 🚀 Próxima Sesión: Punto de Inicio

**Arrancar por:**
1. Leer este roadmap completo
2. Crear branch: `feature/user-person-linking`
3. Empezar con Sprint 1 (Unificar User ↔ Person)

**Comandos útiles:**
```bash
# Crear branch para cada sprint
git checkout -b feature/user-person-linking
git checkout -b feature/calendar-improvements
git checkout -b feature/stats-dashboard
# etc...

# Testing local durante desarrollo
docker-compose up --build

# Ver logs
docker-compose logs -f api

# Migración
cd backend/Descansario.Api
dotnet ef migrations add <NombreMigracion>
dotnet ef database update
```

---

**Última actualización:** 2025-11-19
**Próxima revisión:** Después de cada sprint (ajustar estimaciones según experiencia real)
