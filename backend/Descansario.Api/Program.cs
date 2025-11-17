using Microsoft.EntityFrameworkCore;
using Descansario.Api.Data;
using Descansario.Api.Models;
using Descansario.Api.DTOs;
using Descansario.Api.Helpers;
using Descansario.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<DescansarioDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Business Services
builder.Services.AddScoped<WorkingDaysCalculator>();
builder.Services.AddScoped<HolidaySyncService>();

// HTTP Client for external APIs
builder.Services.AddHttpClient();

// CORS para desarrollo
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Aplicar migraciones automáticamente y poblar feriados iniciales
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DescansarioDbContext>();
    db.Database.Migrate();

    // Poblar feriados de Argentina si no existen
    if (!await db.Holidays.AnyAsync())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Poblando feriados iniciales de Argentina...");

        var holidays = HolidaySeeds.GetArgentinaHolidays();
        await db.Holidays.AddRangeAsync(holidays);
        await db.SaveChangesAsync();

        logger.LogInformation("Se agregaron {Count} feriados de Argentina (2025-2026)", holidays.Count);
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

// Endpoints básicos
app.MapGet("/", () => new
{
    name = "Descansario API",
    version = "0.1.0",
    status = "running"
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// ==============================================================
// CRUD Endpoints - Personas
// ==============================================================

// GET /api/persons - Listar todas las personas
app.MapGet("/api/persons", async (DescansarioDbContext db) =>
{
    var persons = await db.Persons
        .Select(p => new PersonDto
        {
            Id = p.Id,
            Name = p.Name,
            Email = p.Email,
            AvailableDays = p.AvailableDays
        })
        .ToListAsync();

    return Results.Ok(persons);
})
.WithName("GetPersons")
.WithTags("Persons");

// GET /api/persons/{id} - Obtener una persona por ID
app.MapGet("/api/persons/{id:int}", async (int id, DescansarioDbContext db) =>
{
    var person = await db.Persons.FindAsync(id);

    if (person == null)
        return Results.NotFound(new { message = $"Persona con ID {id} no encontrada" });

    var personDto = new PersonDto
    {
        Id = person.Id,
        Name = person.Name,
        Email = person.Email,
        AvailableDays = person.AvailableDays
    };

    return Results.Ok(personDto);
})
.WithName("GetPersonById")
.WithTags("Persons");

// POST /api/persons - Crear una nueva persona
app.MapPost("/api/persons", async (CreatePersonDto dto, DescansarioDbContext db) =>
{
    // Validar entrada
    var (isValid, errorMessage) = ValidationHelper.ValidatePersonDto(dto.Name, dto.Email, dto.AvailableDays);
    if (!isValid)
    {
        return Results.BadRequest(new { message = errorMessage });
    }

    // Validar que el email no exista
    if (await db.Persons.AnyAsync(p => p.Email == dto.Email))
    {
        return Results.BadRequest(new { message = "Ya existe una persona con ese email" });
    }

    var person = new Person
    {
        Name = dto.Name,
        Email = dto.Email,
        AvailableDays = dto.AvailableDays
    };

    db.Persons.Add(person);

    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        return Results.BadRequest(new { message = "Error al guardar: posible email duplicado" });
    }

    var personDto = new PersonDto
    {
        Id = person.Id,
        Name = person.Name,
        Email = person.Email,
        AvailableDays = person.AvailableDays
    };

    return Results.Created($"/api/persons/{person.Id}", personDto);
})
.WithName("CreatePerson")
.WithTags("Persons");

// PUT /api/persons/{id} - Actualizar una persona
app.MapPut("/api/persons/{id:int}", async (int id, UpdatePersonDto dto, DescansarioDbContext db) =>
{
    // Validar entrada
    var (isValid, errorMessage) = ValidationHelper.ValidatePersonDto(dto.Name, dto.Email, dto.AvailableDays);
    if (!isValid)
    {
        return Results.BadRequest(new { message = errorMessage });
    }

    var person = await db.Persons.FindAsync(id);

    if (person == null)
        return Results.NotFound(new { message = $"Persona con ID {id} no encontrada" });

    // Validar que el email no esté en uso por otra persona
    if (await db.Persons.AnyAsync(p => p.Email == dto.Email && p.Id != id))
    {
        return Results.BadRequest(new { message = "Ya existe otra persona con ese email" });
    }

    person.Name = dto.Name;
    person.Email = dto.Email;
    person.AvailableDays = dto.AvailableDays;

    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        return Results.BadRequest(new { message = "Error al actualizar: posible email duplicado" });
    }

    var personDto = new PersonDto
    {
        Id = person.Id,
        Name = person.Name,
        Email = person.Email,
        AvailableDays = person.AvailableDays
    };

    return Results.Ok(personDto);
})
.WithName("UpdatePerson")
.WithTags("Persons");

// DELETE /api/persons/{id} - Eliminar una persona
app.MapDelete("/api/persons/{id:int}", async (int id, DescansarioDbContext db) =>
{
    var person = await db.Persons.FindAsync(id);

    if (person == null)
        return Results.NotFound(new { message = $"Persona con ID {id} no encontrada" });

    db.Persons.Remove(person);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Persona eliminada correctamente" });
})
.WithName("DeletePerson")
.WithTags("Persons");

// ==============================================================
// CRUD Endpoints - Vacations
// ==============================================================

// GET /api/vacations - Listar todas las vacaciones
app.MapGet("/api/vacations", async (DescansarioDbContext db) =>
{
    var vacations = await db.Vacations
        .Include(v => v.Person)
        .Select(v => new VacationDto
        {
            Id = v.Id,
            PersonId = v.PersonId,
            PersonName = v.Person!.Name,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            WorkingDaysCount = v.WorkingDaysCount,
            Status = v.Status.ToString(),
            Notes = v.Notes
        })
        .ToListAsync();

    return Results.Ok(vacations);
})
.WithName("GetVacations")
.WithTags("Vacations");

// GET /api/vacations/person/{personId} - Listar vacaciones de una persona
app.MapGet("/api/vacations/person/{personId:int}", async (int personId, DescansarioDbContext db) =>
{
    var vacations = await db.Vacations
        .Include(v => v.Person)
        .Where(v => v.PersonId == personId)
        .Select(v => new VacationDto
        {
            Id = v.Id,
            PersonId = v.PersonId,
            PersonName = v.Person!.Name,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            WorkingDaysCount = v.WorkingDaysCount,
            Status = v.Status.ToString(),
            Notes = v.Notes
        })
        .ToListAsync();

    return Results.Ok(vacations);
})
.WithName("GetVacationsByPerson")
.WithTags("Vacations");

// GET /api/vacations/overlap?startDate={start}&endDate={end} - Verificar solapamiento de vacaciones
app.MapGet("/api/vacations/overlap", async (DateTime startDate, DateTime endDate, DescansarioDbContext db) =>
{
    var overlappingVacations = await db.Vacations
        .Include(v => v.Person)
        .Where(v => v.StartDate <= endDate && v.EndDate >= startDate)
        .Select(v => new VacationDto
        {
            Id = v.Id,
            PersonId = v.PersonId,
            PersonName = v.Person!.Name,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            WorkingDaysCount = v.WorkingDaysCount,
            Status = v.Status.ToString(),
            Notes = v.Notes
        })
        .ToListAsync();

    return Results.Ok(overlappingVacations);
})
.WithName("GetOverlappingVacations")
.WithTags("Vacations");

// GET /api/vacations/{id} - Obtener una vacación por ID
app.MapGet("/api/vacations/{id:int}", async (int id, DescansarioDbContext db) =>
{
    var vacation = await db.Vacations
        .Include(v => v.Person)
        .Where(v => v.Id == id)
        .Select(v => new VacationDto
        {
            Id = v.Id,
            PersonId = v.PersonId,
            PersonName = v.Person!.Name,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            WorkingDaysCount = v.WorkingDaysCount,
            Status = v.Status.ToString(),
            Notes = v.Notes
        })
        .FirstOrDefaultAsync();

    if (vacation == null)
        return Results.NotFound(new { message = $"Vacación con ID {id} no encontrada" });

    return Results.Ok(vacation);
})
.WithName("GetVacationById")
.WithTags("Vacations");

// POST /api/vacations - Crear una nueva vacación
app.MapPost("/api/vacations", async (CreateVacationDto dto, DescansarioDbContext db, WorkingDaysCalculator calculator) =>
{
    // Validar fechas
    if (dto.StartDate > dto.EndDate)
    {
        return Results.BadRequest(new { message = "La fecha de inicio debe ser anterior o igual a la fecha de fin" });
    }

    // Validar que la persona exista
    var personExists = await db.Persons.AnyAsync(p => p.Id == dto.PersonId);
    if (!personExists)
    {
        return Results.BadRequest(new { message = "La persona especificada no existe" });
    }

    // Calcular días hábiles
    var workingDays = await calculator.CalculateWorkingDaysAsync(dto.StartDate, dto.EndDate);

    // Parsear el status del DTO o usar Pending por defecto
    var status = VacationStatus.Pending;
    if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<VacationStatus>(dto.Status, out var parsedStatus))
    {
        status = parsedStatus;
    }

    var vacation = new Vacation
    {
        PersonId = dto.PersonId,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        WorkingDaysCount = workingDays,
        Status = status,
        Notes = dto.Notes
    };

    db.Vacations.Add(vacation);
    await db.SaveChangesAsync();

    // Cargar la persona para el DTO de respuesta
    await db.Entry(vacation).Reference(v => v.Person).LoadAsync();

    var vacationDto = new VacationDto
    {
        Id = vacation.Id,
        PersonId = vacation.PersonId,
        PersonName = vacation.Person!.Name,
        StartDate = vacation.StartDate,
        EndDate = vacation.EndDate,
        WorkingDaysCount = vacation.WorkingDaysCount,
        Status = vacation.Status.ToString(),
        Notes = vacation.Notes
    };

    return Results.Created($"/api/vacations/{vacation.Id}", vacationDto);
})
.WithName("CreateVacation")
.WithTags("Vacations");

// PUT /api/vacations/{id} - Actualizar una vacación
app.MapPut("/api/vacations/{id:int}", async (int id, UpdateVacationDto dto, DescansarioDbContext db, WorkingDaysCalculator calculator) =>
{
    var vacation = await db.Vacations.FindAsync(id);

    if (vacation == null)
        return Results.NotFound(new { message = $"Vacación con ID {id} no encontrada" });

    // Validar fechas
    if (dto.StartDate > dto.EndDate)
    {
        return Results.BadRequest(new { message = "La fecha de inicio debe ser anterior o igual a la fecha de fin" });
    }

    // Validar que la persona exista
    var personExists = await db.Persons.AnyAsync(p => p.Id == dto.PersonId);
    if (!personExists)
    {
        return Results.BadRequest(new { message = "La persona especificada no existe" });
    }

    // Recalcular días hábiles si las fechas cambiaron
    if (vacation.StartDate != dto.StartDate || vacation.EndDate != dto.EndDate)
    {
        vacation.WorkingDaysCount = await calculator.CalculateWorkingDaysAsync(dto.StartDate, dto.EndDate);
    }

    vacation.PersonId = dto.PersonId;
    vacation.StartDate = dto.StartDate;
    vacation.EndDate = dto.EndDate;
    vacation.Notes = dto.Notes;

    // Parsear el status
    if (Enum.TryParse<VacationStatus>(dto.Status, out var status))
    {
        vacation.Status = status;
    }

    await db.SaveChangesAsync();

    // Cargar la persona para el DTO de respuesta
    await db.Entry(vacation).Reference(v => v.Person).LoadAsync();

    var vacationDto = new VacationDto
    {
        Id = vacation.Id,
        PersonId = vacation.PersonId,
        PersonName = vacation.Person!.Name,
        StartDate = vacation.StartDate,
        EndDate = vacation.EndDate,
        WorkingDaysCount = vacation.WorkingDaysCount,
        Status = vacation.Status.ToString(),
        Notes = vacation.Notes
    };

    return Results.Ok(vacationDto);
})
.WithName("UpdateVacation")
.WithTags("Vacations");

// DELETE /api/vacations/{id} - Eliminar una vacación
app.MapDelete("/api/vacations/{id:int}", async (int id, DescansarioDbContext db) =>
{
    var vacation = await db.Vacations.FindAsync(id);

    if (vacation == null)
        return Results.NotFound(new { message = $"Vacación con ID {id} no encontrada" });

    db.Vacations.Remove(vacation);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Vacación eliminada correctamente" });
})
.WithName("DeleteVacation")
.WithTags("Vacations");

// ==============================================================
// CRUD Endpoints - Holidays
// ==============================================================

// GET /api/holidays - Listar todos los feriados
app.MapGet("/api/holidays", async (DescansarioDbContext db) =>
{
    var holidays = await db.Holidays
        .OrderBy(h => h.Date)
        .Select(h => new HolidayDto
        {
            Id = h.Id,
            Date = h.Date,
            Name = h.Name,
            Country = h.Country.ToString(),
            Region = h.Region
        })
        .ToListAsync();

    return Results.Ok(holidays);
})
.WithName("GetHolidays")
.WithTags("Holidays");

// GET /api/holidays/year/{year} - Listar feriados de un año
app.MapGet("/api/holidays/year/{year:int}", async (int year, string? country, DescansarioDbContext db) =>
{
    var query = db.Holidays.Where(h => h.Date.Year == year);

    // Filtrar por país si se especifica
    if (!string.IsNullOrEmpty(country) && Enum.TryParse<Country>(country, true, out var countryEnum))
    {
        query = query.Where(h => h.Country == countryEnum);
    }

    var holidays = await query
        .OrderBy(h => h.Date)
        .Select(h => new HolidayDto
        {
            Id = h.Id,
            Date = h.Date,
            Name = h.Name,
            Country = h.Country.ToString(),
            Region = h.Region
        })
        .ToListAsync();

    return Results.Ok(holidays);
})
.WithName("GetHolidaysByYear")
.WithTags("Holidays");

// GET /api/holidays/{id} - Obtener un feriado por ID
app.MapGet("/api/holidays/{id:int}", async (int id, DescansarioDbContext db) =>
{
    var holiday = await db.Holidays.FindAsync(id);

    if (holiday == null)
        return Results.NotFound(new { message = $"Feriado con ID {id} no encontrado" });

    var holidayDto = new HolidayDto
    {
        Id = holiday.Id,
        Date = holiday.Date,
        Name = holiday.Name,
        Country = holiday.Country.ToString(),
        Region = holiday.Region
    };

    return Results.Ok(holidayDto);
})
.WithName("GetHolidayById")
.WithTags("Holidays");

// POST /api/holidays - Crear un nuevo feriado
app.MapPost("/api/holidays", async (CreateHolidayDto dto, DescansarioDbContext db) =>
{
    // Validar país
    if (!Enum.TryParse<Country>(dto.Country, true, out var country))
    {
        return Results.BadRequest(new { message = "País inválido. Use 'AR' o 'ES'" });
    }

    // Validar que no exista un feriado duplicado
    var exists = await db.Holidays.AnyAsync(h =>
        h.Date == dto.Date && h.Country == country && h.Region == dto.Region);

    if (exists)
    {
        return Results.BadRequest(new { message = "Ya existe un feriado en esa fecha para ese país/región" });
    }

    var holiday = new Holiday
    {
        Date = dto.Date,
        Name = dto.Name,
        Country = country,
        Region = dto.Region
    };

    db.Holidays.Add(holiday);
    await db.SaveChangesAsync();

    var holidayDto = new HolidayDto
    {
        Id = holiday.Id,
        Date = holiday.Date,
        Name = holiday.Name,
        Country = holiday.Country.ToString(),
        Region = holiday.Region
    };

    return Results.Created($"/api/holidays/{holiday.Id}", holidayDto);
})
.WithName("CreateHoliday")
.WithTags("Holidays");

// PUT /api/holidays/{id} - Actualizar un feriado
app.MapPut("/api/holidays/{id:int}", async (int id, UpdateHolidayDto dto, DescansarioDbContext db) =>
{
    var holiday = await db.Holidays.FindAsync(id);

    if (holiday == null)
        return Results.NotFound(new { message = $"Feriado con ID {id} no encontrado" });

    // Validar país
    if (!Enum.TryParse<Country>(dto.Country, true, out var country))
    {
        return Results.BadRequest(new { message = "País inválido. Use 'AR' o 'ES'" });
    }

    // Validar que no exista otro feriado duplicado
    var exists = await db.Holidays.AnyAsync(h =>
        h.Id != id && h.Date == dto.Date && h.Country == country && h.Region == dto.Region);

    if (exists)
    {
        return Results.BadRequest(new { message = "Ya existe otro feriado en esa fecha para ese país/región" });
    }

    holiday.Date = dto.Date;
    holiday.Name = dto.Name;
    holiday.Country = country;
    holiday.Region = dto.Region;

    await db.SaveChangesAsync();

    var holidayDto = new HolidayDto
    {
        Id = holiday.Id,
        Date = holiday.Date,
        Name = holiday.Name,
        Country = holiday.Country.ToString(),
        Region = holiday.Region
    };

    return Results.Ok(holidayDto);
})
.WithName("UpdateHoliday")
.WithTags("Holidays");

// DELETE /api/holidays/{id} - Eliminar un feriado
app.MapDelete("/api/holidays/{id:int}", async (int id, DescansarioDbContext db) =>
{
    var holiday = await db.Holidays.FindAsync(id);

    if (holiday == null)
        return Results.NotFound(new { message = $"Feriado con ID {id} no encontrado" });

    db.Holidays.Remove(holiday);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Feriado eliminado correctamente" });
})
.WithName("DeleteHoliday")
.WithTags("Holidays");

// POST /api/holidays/sync - Sincronizar feriados desde API externa
app.MapPost("/api/holidays/sync", async (SyncHolidaysRequest request, HolidaySyncService syncService) =>
{
    // Validar país
    if (!Enum.TryParse<Country>(request.Country, true, out var country))
    {
        return Results.BadRequest(new { message = "País inválido. Use 'AR' o 'ES'" });
    }

    // Validar año
    if (request.Year < 2000 || request.Year > 2100)
    {
        return Results.BadRequest(new { message = "Año inválido. Debe estar entre 2000 y 2100" });
    }

    try
    {
        var (added, updated, holidays) = await syncService.SyncHolidaysAsync(request.Year, country);

        var response = new SyncHolidaysResponse
        {
            Added = added,
            Updated = updated,
            Total = holidays.Count,
            Holidays = holidays
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Error al sincronizar feriados"
        );
    }
})
.WithName("SyncHolidays")
.WithTags("Holidays");

app.Run();
