# Descansario Tests

Este proyecto contiene los tests unitarios y de integración para Descansario.

## Estructura

```
Descansario.Tests/
├── Services/
│   └── WorkingDaysCalculatorTests.cs
└── README.md
```

## Ejecutar tests

### Todos los tests

```bash
cd backend
dotnet test
```

### Tests específicos

```bash
# Tests del WorkingDaysCalculator
dotnet test --filter "FullyQualifiedName~WorkingDaysCalculatorTests"
```

### Con cobertura

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Tests implementados

### WorkingDaysCalculatorTests

Tests para el servicio crítico de cálculo de días hábiles:

1. **CalculateWorkingDays_ShouldReturnCorrectCount_WhenNoHolidays** - Verifica cálculo básico sin feriados
2. **CalculateWorkingDays_ShouldExcludeWeekends** - Verifica exclusión de fines de semana
3. **CalculateWorkingDays_ShouldExcludeHolidays** - Verifica exclusión de feriados
4. **CalculateWorkingDays_ShouldExcludeBothWeekendsAndHolidays** - Verifica exclusión combinada
5. **CalculateWorkingDays_ShouldReturnOne_WhenSameDay** - Verifica cálculo para mismo día (laborable)
6. **CalculateWorkingDays_ShouldReturnZero_WhenSameDayIsWeekend** - Verifica cálculo para mismo día (weekend)
7. **CalculateWorkingDays_ShouldThrowException_WhenStartDateAfterEndDate** - Verifica validación de fechas
8. **CalculateWorkingDays_ShouldHandleMultipleHolidays** - Verifica múltiples feriados
9. **CalculateWorkingDaysBatch_ShouldReturnCorrectCounts_ForMultipleRanges** - Verifica batch calculation
10. **CalculateWorkingDaysBatch_ShouldOptimizeHolidayQuery** - Verifica optimización de consultas
11. **CalculateWorkingDaysBatch_ShouldReturnEmpty_WhenNoRanges** - Verifica manejo de array vacío
12. **CalculateWorkingDaysBatch_ShouldReturnZero_WhenStartDateAfterEndDate** - Verifica batch con rango inválido
13. **CalculateWorkingDays_ShouldHandleLongRange** - Verifica cálculo de rango largo (1 año)
14. **CalculateWorkingDays_ShouldHandleLeapYear** - Verifica manejo de años bisiestos

## Tecnologías

- **xUnit**: Framework de testing
- **Microsoft.EntityFrameworkCore.InMemory**: Base de datos en memoria para tests
- **coverlet**: Herramienta de cobertura de código

## Notas

- Los tests utilizan una base de datos en memoria (InMemory) para aislar las pruebas
- Cada test limpia su contexto de DB al finalizar usando `IDisposable`
- La configuración por defecto de weekends es Domingo (0) y Sábado (6)
