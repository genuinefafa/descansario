namespace Descansario.Api.DTOs;

public record PersonDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public int AvailableDays { get; init; }
}

public record CreatePersonDto
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public int AvailableDays { get; init; } = 20;
}

public record UpdatePersonDto
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public int AvailableDays { get; init; }
}
