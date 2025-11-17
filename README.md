# 🏖️ Descansario

Aplicación web para gestionar y visualizar vacaciones de equipos, con calendario continuo y cálculo automático de días hábiles.

## 🎯 Características Principales

- **Calendario continuo** con scroll infinito por semanas
- **Cálculo automático** de días hábiles (excluye fines de semana y feriados)
- **Gestión de personas** y asignación de vacaciones
- **Importación de feriados** de Argentina y España
- **Exportación a iCal** para integración con calendarios externos
- **Optimizado para Raspberry Pi 5** (~200MB RAM total)

## 🏗️ Stack Tecnológico

### Frontend
- **SvelteKit** - Framework fullstack con TypeScript
- **TailwindCSS** - Estilos utility-first
- **date-fns** - Manipulación de fechas

### Backend
- **.NET 8** - API REST con Minimal APIs
- **Entity Framework Core** - ORM
- **SQLite** - Base de datos
- **NodaTime** - Manejo avanzado de fechas y timezones

### DevOps
- **Docker + Docker Compose** - Containerización
- **Nginx** - Servidor web para SPA

## 🚀 Inicio Rápido

### Prerequisitos

- Docker y Docker Compose
- .NET 8 SDK (para desarrollo local)
- Node.js 20+ (para desarrollo local)

### Desarrollo con Docker

```bash
# Levantar toda la aplicación
docker-compose up -d

# Ver logs
docker-compose logs -f

# Detener
docker-compose down
```

La aplicación estará disponible en:
- **Frontend**: http://localhost:3000
- **API**: http://localhost:5000

### Desarrollo Local

#### Backend

```bash
cd backend
dotnet restore
dotnet run --project Descansario.Api
```

#### Frontend

```bash
cd frontend
npm install
npm run dev
```

#### Migraciones de Base de Datos

La aplicación usa **Entity Framework Core** con migraciones para gestionar el esquema de la base de datos.

**¿Qué son las migraciones?**
- Son archivos que describen cambios en el esquema de la base de datos
- Permiten versionar y aplicar cambios de forma incremental
- Se aplican automáticamente al iniciar la API (ver `Program.cs`)

**En Docker** (recomendado):
```bash
# Las migraciones se aplican automáticamente al iniciar
docker-compose up -d
```

**En desarrollo local**:
```bash
cd backend/Descansario.Api

# Ver migraciones pendientes
dotnet ef migrations list

# Aplicar migraciones manualmente (no necesario, se auto-aplican)
dotnet ef database update

# Crear nueva migración (solo si modificaste los modelos)
dotnet ef migrations add NombreDeLaMigracion
```

**Archivos importantes**:
- `backend/Descansario.Api/Migrations/` - Carpeta con todas las migraciones
- `backend/Descansario.Api/Program.cs` - Línea 35-38: Auto-aplicación de migraciones
- `backend/descansario.db` - Base de datos SQLite (se crea automáticamente)

**Nota**: No necesitas ejecutar migraciones manualmente, la API las aplica automáticamente al iniciar. Solo debes ejecutar comandos si estás creando nuevas migraciones.

### ✅ Validación de Código

**Antes de hacer commit**, ejecuta en el frontend:

```bash
cd frontend
npm run validate
```

Este comando ejecuta:
1. **ESLint**: Validación de estilo y problemas de código
2. **Type Check**: Validación de tipos TypeScript + Svelte
3. **Build**: Compilación del proyecto

Si falla, arregla los errores antes de commitear. El CI ejecuta las mismas validaciones.

Ver [DEVELOPMENT.md](frontend/DEVELOPMENT.md) para más detalles.

## 📁 Estructura del Proyecto

```
descansario/
├── backend/              # API .NET 8
│   ├── Descansario.Api/
│   │   ├── Models/      # Modelos de datos
│   │   ├── Data/        # DbContext y migraciones
│   │   ├── Services/    # Lógica de negocio
│   │   └── Program.cs   # Minimal APIs
│   └── Dockerfile
├── frontend/            # SPA SvelteKit
│   ├── src/
│   │   ├── lib/
│   │   │   ├── components/
│   │   │   └── services/
│   │   └── routes/
│   ├── Dockerfile
│   └── package.json
├── docker/              # Configuraciones Docker
├── docs/                # Documentación
└── docker-compose.yml
```

## 🗄️ Modelo de Datos

### Person
- `id`: Identificador único
- `name`: Nombre completo
- `email`: Email
- `availableDays`: Días de vacaciones disponibles al año

### Vacation
- `id`: Identificador único
- `personId`: Referencia a Person
- `startDate`: Fecha de inicio
- `endDate`: Fecha de fin
- `workingDaysCount`: Días hábiles (calculado automáticamente)
- `status`: Estado (pending, approved, rejected)
- `notes`: Notas adicionales (opcional, soporte Markdown)

### Holiday
- `id`: Identificador único
- `date`: Fecha del feriado
- `name`: Nombre del feriado
- `country`: País (AR, ES)
- `region`: Provincia/Comunidad autónoma (opcional)

## 🎨 Capturas

_(Próximamente)_

## 📝 Estado del Proyecto

**Fase actual**: Setup inicial

- [x] Estructura de carpetas
- [x] Configuración Docker
- [ ] Backend base (.NET 8)
- [ ] Frontend base (SvelteKit)
- [ ] CRUD de personas
- [ ] Calendario básico
- [ ] Cálculo de días hábiles
- [ ] Sistema de feriados
- [ ] Autenticación
- [ ] Exportación iCal

## 🤝 Contribuir

Este es un proyecto personal, pero sugerencias y feedback son bienvenidos.

## 📄 Licencia

MIT

---

**Desarrollado con ❤️ para gestionar vacaciones de forma simple y eficiente**
