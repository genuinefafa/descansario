using Descansario.Api.Models;

namespace Descansario.Api.Data;

public static class HolidaySeeds
{
    /// <summary>
    /// Feriados oficiales de Argentina 2025-2026
    /// Fuente: Ministerio del Interior - Argentina
    /// </summary>
    public static List<Holiday> GetArgentinaHolidays()
    {
        return new List<Holiday>
        {
            // 2025
            new Holiday { Date = new DateTime(2025, 1, 1), Name = "Año Nuevo", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 2, 24), Name = "Carnaval", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 2, 25), Name = "Carnaval", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 3, 24), Name = "Día Nacional de la Memoria por la Verdad y la Justicia", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 4, 2), Name = "Día del Veterano y de los Caídos en la Guerra de Malvinas", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 4, 18), Name = "Viernes Santo", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 5, 1), Name = "Día del Trabajador", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 5, 25), Name = "Día de la Revolución de Mayo", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 6, 16), Name = "Paso a la Inmortalidad del General Martín Miguel de Güemes", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 6, 20), Name = "Paso a la Inmortalidad del General Manuel Belgrano", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 7, 9), Name = "Día de la Independencia", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 8, 17), Name = "Paso a la Inmortalidad del General José de San Martín", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 10, 12), Name = "Día del Respeto a la Diversidad Cultural", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 11, 24), Name = "Día de la Soberanía Nacional", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 12, 8), Name = "Día de la Inmaculada Concepción de María", Country = Country.AR },
            new Holiday { Date = new DateTime(2025, 12, 25), Name = "Navidad", Country = Country.AR },

            // 2026
            new Holiday { Date = new DateTime(2026, 1, 1), Name = "Año Nuevo", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 2, 16), Name = "Carnaval", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 2, 17), Name = "Carnaval", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 3, 24), Name = "Día Nacional de la Memoria por la Verdad y la Justicia", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 4, 2), Name = "Día del Veterano y de los Caídos en la Guerra de Malvinas", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 4, 3), Name = "Viernes Santo", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 5, 1), Name = "Día del Trabajador", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 5, 25), Name = "Día de la Revolución de Mayo", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 6, 15), Name = "Paso a la Inmortalidad del General Martín Miguel de Güemes", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 6, 20), Name = "Paso a la Inmortalidad del General Manuel Belgrano", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 7, 9), Name = "Día de la Independencia", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 8, 17), Name = "Paso a la Inmortalidad del General José de San Martín", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 10, 12), Name = "Día del Respeto a la Diversidad Cultural", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 11, 23), Name = "Día de la Soberanía Nacional", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 12, 8), Name = "Día de la Inmaculada Concepción de María", Country = Country.AR },
            new Holiday { Date = new DateTime(2026, 12, 25), Name = "Navidad", Country = Country.AR },
        };
    }
}
