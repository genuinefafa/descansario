namespace Descansario.Api.Models;

public class Vacation
{
    public int Id { get; set; }

    public int PersonId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    // WorkingDaysCount ya NO se persiste - se calcula on-demand con WorkingDaysCalculator
    // Esto asegura que siempre esté actualizado si cambian feriados

    public VacationStatus Status { get; set; } = VacationStatus.Pending;

    public string? Notes { get; set; } // Soporte para markdown

    // Navigation property
    public Person? Person { get; set; }
}

public enum VacationStatus
{
    Pending,
    Approved,
    Rejected
}
