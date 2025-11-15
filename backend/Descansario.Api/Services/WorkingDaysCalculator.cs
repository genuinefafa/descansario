using Descansario.Api.Data;
using Descansario.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Descansario.Api.Services;

public class WorkingDaysCalculator
{
    private readonly DescansarioDbContext _db;

    public WorkingDaysCalculator(DescansarioDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Calcula los días hábiles entre dos fechas (inclusive)
    /// </summary>
    public async Task<int> CalculateWorkingDaysAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException("La fecha de inicio debe ser anterior o igual a la fecha de fin");
        }

        // Obtener configuración de weekends
        var config = await _db.Configurations.FirstOrDefaultAsync();
        var weekendDays = config?.GetWeekendDaysArray() ?? new[] { 0, 6 }; // Default: Domingo y Sábado

        // Obtener feriados en el rango de fechas
        var holidays = await _db.Holidays
            .Where(h => h.Date >= startDate.Date && h.Date <= endDate.Date)
            .Select(h => h.Date.Date)
            .ToListAsync();

        var holidaySet = new HashSet<DateTime>(holidays);

        int workingDays = 0;
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            int dayOfWeek = (int)currentDate.DayOfWeek;

            // Si no es weekend y no es feriado, es día hábil
            if (!weekendDays.Contains(dayOfWeek) && !holidaySet.Contains(currentDate))
            {
                workingDays++;
            }

            currentDate = currentDate.AddDays(1);
        }

        return workingDays;
    }

    /// <summary>
    /// Calcula días hábiles para múltiples rangos de fechas de forma eficiente
    /// </summary>
    public async Task<Dictionary<(DateTime, DateTime), int>> CalculateWorkingDaysBatchAsync(
        IEnumerable<(DateTime startDate, DateTime endDate)> dateRanges)
    {
        var result = new Dictionary<(DateTime, DateTime), int>();

        if (!dateRanges.Any())
            return result;

        // Obtener configuración de weekends
        var config = await _db.Configurations.FirstOrDefaultAsync();
        var weekendDays = config?.GetWeekendDaysArray() ?? new[] { 0, 6 };

        // Obtener el rango total de fechas
        var minDate = dateRanges.Min(r => r.startDate.Date);
        var maxDate = dateRanges.Max(r => r.endDate.Date);

        // Obtener todos los feriados en el rango total
        var holidays = await _db.Holidays
            .Where(h => h.Date >= minDate && h.Date <= maxDate)
            .Select(h => h.Date.Date)
            .ToListAsync();

        var holidaySet = new HashSet<DateTime>(holidays);

        // Calcular para cada rango
        foreach (var (startDate, endDate) in dateRanges)
        {
            if (startDate > endDate)
            {
                result[(startDate, endDate)] = 0;
                continue;
            }

            int workingDays = 0;
            var currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                int dayOfWeek = (int)currentDate.DayOfWeek;

                if (!weekendDays.Contains(dayOfWeek) && !holidaySet.Contains(currentDate))
                {
                    workingDays++;
                }

                currentDate = currentDate.AddDays(1);
            }

            result[(startDate, endDate)] = workingDays;
        }

        return result;
    }
}
