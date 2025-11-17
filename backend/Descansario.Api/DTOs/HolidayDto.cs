namespace Descansario.Api.DTOs;

using Descansario.Api.Models;

public record HolidayDto
{
    public int Id { get; init; }
    public DateTime Date { get; init; }
    public required string Name { get; init; }
    public required string Country { get; init; }
    public string? Region { get; init; }
}

public record CreateHolidayDto
{
    public required DateTime Date { get; init; }
    public required string Name { get; init; }
    public required string Country { get; init; }
    public string? Region { get; init; }
}

public record UpdateHolidayDto
{
    public required DateTime Date { get; init; }
    public required string Name { get; init; }
    public required string Country { get; init; }
    public string? Region { get; init; }
}

public record SyncHolidaysRequest
{
    public required int Year { get; init; }
    public required string Country { get; init; }
}

public record SyncHolidaysResponse
{
    public required int Added { get; init; }
    public required int Updated { get; init; }
    public required int Total { get; init; }
    public required List<string> Holidays { get; init; }
}
