# Descansario - Especificaciones del Proyecto

## 📋 Descripción

Aplicación web para gestionar y visualizar vacaciones de equipos, con calendario continuo y cálculo automático de días hábiles.

---

## 🏗️ Stack Tecnológico

### Frontend
- **SvelteKit** - Framework fullstack
- **TailwindCSS** - Estilos utility-first
- **date-fns** - Manipulación de fechas
- **TypeScript** - Type safety

### Backend
- **.NET 8** - API REST con Minimal APIs
- **Entity Framework Core** - ORM
- **SQLite** - Base de datos
- **NodaTime** - Manejo avanzado de fechas y timezones

### DevOps
- **Docker + Docker Compose** - Containerización
- **Nginx** - Servidor web para SPA
- **Target:** Raspberry Pi 5 (Debian/LibreELEC)

---

## 🎯 Features del MVP

### Calendario
- ✅ Vista continua de semanas (scroll infinito)
- ✅ Configuración de primer día de semana (lunes/domingo)
- ✅ Ocultar semanas sin vacaciones (opcional)
- ✅ Slots conectados visualmente para vacaciones consecutivas
- ✅ Grid homogéneo (altura fija por slot)
- ✅ Vista ejemplo: 2 semanas enero + febrero completo + 2 semanas marzo

### Gestión de Personas
- ✅ CRUD de personas (nombre, email)
- ✅ Asignación independiente del sistema de autenticación
- ✅ Las personas no necesitan registrarse para ser asignadas a vacaciones

### Gestión de Vacaciones
- ✅ Cargar vacaciones por intervalo de fechas o día particular
- ✅ Asignar vacaciones a personas
- ✅ Visualización en calendario

### Días Hábiles
- ✅ Cálculo automático de días hábiles (excluye fines de semana)
- ✅ Gestión manual de feriados
- ✅ Importación automática de feriados (Argentina y España)
- ✅ Contabilización de días "no trabajados" por persona

### Autenticación
- ✅ Sistema simple usuario/contraseña (JWT)
- ✅ Solo para acceder a la aplicación
- ✅ No requerido para ser asignado en el calendario

### API
- ✅ API REST para integración con calendarios externos
- ✅ Exportación iCal/ICS para Google Calendar, etc.

---

## 🎨 Diseño del Calendario

### Estructura Visual

```
┌─────────────────────────────────────────────────────────┐
│              Semana del 3 al 9 de Febrero              │
├──────┬──────┬──────┬──────┬──────┬──────┬──────────────┤
│ Lun  │ Mar  │ Mié  │ Jue  │ Vie  │ Sáb  │ Dom         │
│  3   │  4   │  5   │  6   │  7   │  8   │  9          │
├──────┴──────┴──────┴──────┴──────┼──────┼──────────────┤
│         Juan (5 días)             │      │             │
├──────┬──────┬──────────────┬──────┼──────┼──────────────┤
│ María│      │ Pedro (2d)   │      │      │             │
└──────┴──────┴──────────────┴──────┴──────┴──────────────┘
```

### Características
- Slots conectados: Vacaciones consecutivas se unen visualmente
- Grid CSS: Posicionamiento preciso con `grid-column: span N`
- Altura homogénea: Cada slot tiene altura fija
- Scroll infinito: Carga dinámica de semanas al scrollear
- Virtualización: Solo renderiza semanas visibles (performance)

---

## 🗄️ Modelo de Datos

### Person
```typescript
{
  id: number
  name: string
  email: string
  availableDays: number  // días de vacaciones disponibles al año
}
```

### Vacation
```typescript
{
  id: number
  personId: number
  startDate: Date
  endDate: Date
  workingDaysCount: number  // calculado automáticamente
  status: 'pending' | 'approved' | 'rejected'
}
```

### Holiday
```typescript
{
  id: number
  date: Date
  name: string
  country: 'AR' | 'ES'
  region?: string  // provincial/autonómico
}
```

### Configuration
```typescript
{
  firstDayOfWeek: 0 | 1  // 0=domingo, 1=lunes
  weekendDays: number[]  // [0, 6] = domingo, sábado
  defaultCountry: 'AR' | 'ES'
}
```

---

## 🐳 Deployment

### Docker Compose
```yaml
services:
  api:      # .NET 8 API
  web:      # Nginx + Svelte SPA

volumes:
  data:     # SQLite database
```

### Recursos Estimados (Pi5)
- API: ~150MB RAM
- Web: ~50MB RAM
- **Total: ~200MB RAM**

### Host
- Raspberry Pi 5 (4GB/8GB)
- Debian o LibreELEC
- Docker + Docker Compose

---

## 🔄 Workflow de Desarrollo

### Git Flow
1. **Branch por feature:** `feature/nombre-descriptivo`
2. **Pull Request** antes de merge a `main`
3. **Rama principal:** `main`
4. **Commits:** Descriptivos, en español

### Estructura del Repositorio
```
descansario/
├── backend/
│   ├── Descansario.Api/
│   │   ├── Models/
│   │   ├── Data/
│   │   ├── Services/
│   │   │   └── WorkingDaysCalculator.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   └── Dockerfile
├── frontend/
│   ├── src/
│   │   ├── lib/
│   │   │   ├── components/
│   │   │   │   ├── CalendarWeek.svelte
│   │   │   │   ├── VacationSlot.svelte
│   │   │   │   └── WeekControls.svelte
│   │   │   └── services/
│   │   │       └── api.ts
│   │   └── routes/
│   │       ├── +page.svelte
│   │       └── api/
│   ├── Dockerfile
│   └── package.json
├── docker-compose.yml
└── README.md
```

---

## 📡 API Endpoints (Planeados)

### Vacaciones
- `GET /api/vacations?year={year}` - Listar vacaciones del año
- `GET /api/vacations/{personId}` - Vacaciones de una persona
- `POST /api/vacations` - Crear vacación
- `PUT /api/vacations/{id}` - Actualizar vacación
- `DELETE /api/vacations/{id}` - Eliminar vacación

### Personas
- `GET /api/persons` - Listar personas
- `POST /api/persons` - Crear persona
- `PUT /api/persons/{id}` - Actualizar persona
- `DELETE /api/persons/{id}` - Eliminar persona

### Feriados
- `GET /api/holidays?year={year}&country={country}` - Listar feriados
- `POST /api/holidays` - Crear feriado manual
- `POST /api/holidays/import?year={year}&country={country}` - Importar feriados

### Calendario
- `GET /api/calendar/ical/{personId}` - Feed iCal para suscripción

### Estadísticas
- `GET /api/stats/working-days?personId={id}&year={year}` - Días trabajados/no trabajados

---

## 🚀 Fases de Implementación

### Fase 1: Setup y Base (Semana 1)
- [x] Setup proyecto (SvelteKit + .NET)
- [x] Configuración Docker
- [ ] Base de datos SQLite + modelos
- [ ] Estructura básica del calendario

### Fase 2: Calendario Core (Semana 2)
- [ ] Vista de semanas con scroll infinito
- [ ] Renderizado de slots conectados
- [ ] Configuración de primer día de semana
- [ ] Ocultar semanas vacías

### Fase 3: CRUD y Lógica (Semana 3)
- [ ] CRUD de personas
- [ ] CRUD de vacaciones
- [ ] Cálculo de días hábiles
- [ ] Validaciones

### Fase 4: Feriados y API (Semana 4)
- [ ] Gestión de feriados
- [ ] Importación automática (AR/ES)
- [ ] API REST completa
- [ ] Exportación iCal

### Fase 5: Auth y Deploy (Semana 5)
- [ ] Sistema de autenticación
- [ ] Testing en Pi5
- [ ] Optimización de performance
- [ ] Documentación

---

## 🎨 Consideraciones de UX

### Interacciones Planeadas
- Click en día vacío → Crear vacación
- Click en vacación → Editar/eliminar
- Arrastrar para crear rango (opcional, fase 2)
- Filtros por persona
- Vista de estadísticas por persona

### Responsive
- Desktop: Vista de 7 columnas (semana completa)
- Tablet: Vista de 5 columnas
- Mobile: Vista de 1 columna (día por día)

---

## 🔍 Consideraciones Técnicas

### Cálculo de Días Hábiles
```csharp
// Excluir:
// - Fines de semana configurables
// - Feriados del país/región
// - Considerar rangos que cruzan años
```

### Performance
- Virtualización de lista de semanas (solo renderizar visibles)
- Lazy loading de datos por rango de fechas
- Cache de cálculos de días hábiles

### Seguridad
- Rate limiting en API
- Validación de permisos
- Sanitización de inputs
- CORS configurado

---

## 📝 Notas Adicionales

- **Idioma:** Interfaz en español
- **Zonas horarias:** Usar UTC en DB, convertir a local en UI
- **Backups:** SQLite permite backup simple (copiar archivo)
- **Escalabilidad futura:** Si crece, migrar a PostgreSQL es directo con EF Core

---

## 🎯 Objetivo del MVP

Sistema funcional que permita:
1. Ver calendario anual completo con scroll
2. Cargar personas y sus vacaciones
3. Calcular automáticamente días hábiles descontados
4. Exportar a calendario externo (Google Calendar)
5. Correr en Raspberry Pi 5 con consumo mínimo de recursos

---

**Fecha de creación:** 2025-11-15
**Estado:** Especificación completa, listo para implementación
