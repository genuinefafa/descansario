namespace Descansario.Api.Models;

public class Vacation
{
    public int Id { get; set; }

    public int PersonId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int WorkingDaysCount { get; set; } // Calculado automáticamente

    public VacationStatus Status { get; set; } = VacationStatus.Pending;

    // Navigation property
    public Person? Person { get; set; }
}

public enum VacationStatus
{
    Pending,
    Approved,
    Rejected
}
