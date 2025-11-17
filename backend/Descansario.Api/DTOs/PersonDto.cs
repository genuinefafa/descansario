namespace Descansario.Api.DTOs;

public record PersonDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public int AvailableDays { get; init; }
}

public record PersonFormData
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public int AvailableDays { get; init; }
}

public record CreatePersonDto : PersonFormData;
public record UpdatePersonDto : PersonFormData;
