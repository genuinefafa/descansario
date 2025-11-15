namespace Descansario.Api.Models;

public class Configuration
{
    public int Id { get; set; }

    public int FirstDayOfWeek { get; set; } = 1; // 0 = Domingo, 1 = Lunes

    public required string WeekendDays { get; set; } = "0,6"; // Serializado como string: "0,6" = Domingo, Sábado

    public Country DefaultCountry { get; set; } = Country.AR;

    // Helper property para deserializar
    public int[] GetWeekendDaysArray() =>
        WeekendDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(int.Parse)
                   .ToArray();

    public void SetWeekendDaysArray(int[] days) =>
        WeekendDays = string.Join(',', days);
}
