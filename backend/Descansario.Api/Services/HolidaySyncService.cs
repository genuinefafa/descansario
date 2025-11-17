using System.Text.Json;
using Descansario.Api.Data;
using Descansario.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Descansario.Api.Services;

public class HolidaySyncService
{
    private readonly DescansarioDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HolidaySyncService> _logger;

    public HolidaySyncService(
        DescansarioDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<HolidaySyncService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(int added, int updated, List<string> holidayNames)> SyncHolidaysAsync(int year, Country country)
    {
        if (country == Country.AR)
        {
            // Primero intentar con API externa, si falla usar seeds locales
            try
            {
                return await SyncArgentinaHolidaysFromApiAsync(year);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API externa falló, usando feriados locales como fallback");
                return await SyncArgentinaHolidaysFromLocalAsync(year);
            }
        }
        else if (country == Country.ES)
        {
            return await SyncSpainHolidaysAsync(year);
        }

        throw new ArgumentException($"País no soportado: {country}");
    }

    /// <summary>
    /// Sincroniza feriados de Argentina desde seeds locales
    /// </summary>
    private async Task<(int added, int updated, List<string> holidayNames)> SyncArgentinaHolidaysFromLocalAsync(int year)
    {
        _logger.LogInformation("Sincronizando feriados de Argentina (local) para el año {Year}", year);

        var allHolidays = HolidaySeeds.GetArgentinaHolidays();
        var holidaysForYear = allHolidays.Where(h => h.Date.Year == year).ToList();

        if (holidaysForYear.Count == 0)
        {
            _logger.LogWarning("No hay feriados locales disponibles para el año {Year}", year);
            throw new Exception($"No hay feriados disponibles para el año {year}. Solo disponible: 2025-2026");
        }

        int added = 0;
        int updated = 0;
        var holidayNames = new List<string>();

        foreach (var holiday in holidaysForYear)
        {
            // Buscar si ya existe este feriado
            var existing = await _db.Holidays
                .FirstOrDefaultAsync(h => h.Date == holiday.Date && h.Country == Country.AR);

            if (existing != null)
            {
                // Actualizar si cambió el nombre
                if (existing.Name != holiday.Name)
                {
                    existing.Name = holiday.Name;
                    updated++;
                }
            }
            else
            {
                // Crear nuevo
                _db.Holidays.Add(new Holiday
                {
                    Date = holiday.Date,
                    Name = holiday.Name,
                    Country = Country.AR,
                    Region = null
                });
                added++;
            }

            holidayNames.Add(holiday.Name);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Sincronización local completada: {Added} agregados, {Updated} actualizados",
            added, updated);

        return (added, updated, holidayNames);
    }

    private async Task<(int added, int updated, List<string> holidayNames)> SyncArgentinaHolidaysFromApiAsync(int year)
    {
        _logger.LogInformation("Sincronizando feriados de Argentina para el año {Year}", year);

        var client = _httpClientFactory.CreateClient();
        var url = $"https://nolaborables.com.ar/api/v2/feriados/{year}";

        try
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var holidays = JsonSerializer.Deserialize<List<ArgentinaHolidayDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (holidays == null || holidays.Count == 0)
            {
                _logger.LogWarning("No se encontraron feriados para el año {Year}", year);
                return (0, 0, new List<string>());
            }

            int added = 0;
            int updated = 0;
            var holidayNames = new List<string>();

            foreach (var dto in holidays)
            {
                // Parsear fecha: formato ISO YYYY-MM-DD
                if (!DateTime.TryParse(dto.Fecha, out var date))
                {
                    _logger.LogWarning("Fecha inválida: {Fecha}", dto.Fecha);
                    continue;
                }

                // Buscar si ya existe este feriado
                var existing = await _db.Holidays
                    .FirstOrDefaultAsync(h => h.Date == date && h.Country == Country.AR);

                if (existing != null)
                {
                    // Actualizar si cambió el nombre
                    if (existing.Name != dto.Nombre)
                    {
                        existing.Name = dto.Nombre;
                        updated++;
                    }
                }
                else
                {
                    // Crear nuevo
                    _db.Holidays.Add(new Holiday
                    {
                        Date = date,
                        Name = dto.Nombre,
                        Country = Country.AR,
                        Region = null // Los feriados nacionales no tienen región
                    });
                    added++;
                }

                holidayNames.Add(dto.Nombre);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Sincronización completada: {Added} agregados, {Updated} actualizados",
                added, updated);

            return (added, updated, holidayNames);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al sincronizar feriados de Argentina");
            throw new Exception("No se pudo conectar con la API de feriados de Argentina", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error al parsear respuesta de API de feriados");
            throw new Exception("Error al procesar datos de feriados", ex);
        }
    }

    private async Task<(int added, int updated, List<string> holidayNames)> SyncSpainHolidaysAsync(int year)
    {
        // Placeholder para España - se puede implementar después
        _logger.LogInformation("Sincronización de feriados de España aún no implementada");
        await Task.CompletedTask;
        return (0, 0, new List<string>());
    }

    // DTO para deserializar respuesta de nolaborables.com.ar
    private class ArgentinaHolidayDto
    {
        public string Fecha { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
    }
}
