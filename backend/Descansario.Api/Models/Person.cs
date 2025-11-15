namespace Descansario.Api.Models;

public class Person
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }

    public int AvailableDays { get; set; } = 20; // Días de vacaciones disponibles al año

    // Navigation property
    public ICollection<Vacation> Vacations { get; set; } = new List<Vacation>();
}
