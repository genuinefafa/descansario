namespace Descansario.Api.DTOs;

public record VacationDto
{
    public int Id { get; init; }
    public int PersonId { get; init; }
    public string PersonName { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int WorkingDaysCount { get; init; }
    public string Status { get; init; } = string.Empty;
}

public record CreateVacationDto
{
    public int PersonId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? Status { get; init; }
}

public record UpdateVacationDto
{
    public int PersonId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Status { get; init; } = "Pending";
}
