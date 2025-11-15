namespace Descansario.Api.Models;

public class Holiday
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public required string Name { get; set; }

    public Country Country { get; set; }

    public string? Region { get; set; } // Provincial/autonómico (opcional)
}

public enum Country
{
    AR, // Argentina
    ES  // España
}
