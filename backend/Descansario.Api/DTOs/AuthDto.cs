namespace Descansario.Api.DTOs;

/// <summary>
/// Request para login de usuario
/// </summary>
public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

/// <summary>
/// Response del login exitoso
/// </summary>
public record LoginResponse
{
    public required string Token { get; init; }
    public required UserDto User { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Request para registro de nuevo usuario
/// </summary>
public record RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Name { get; init; }
}

/// <summary>
/// DTO de usuario (sin información sensible)
/// </summary>
public record UserDto
{
    public int Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string Role { get; init; }
    public DateTime CreatedAt { get; init; }
}
