using Microsoft.EntityFrameworkCore;
using Descansario.Api.Data;
using Descansario.Api.Models;
using Descansario.Api.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<DescansarioDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Aplicar migraciones automáticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DescansarioDbContext>();
    db.Database.EnsureCreated();
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
    await db.SaveChangesAsync();

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

    await db.SaveChangesAsync();

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

    return Results.NoContent();
})
.WithName("DeletePerson")
.WithTags("Persons");

app.Run();
