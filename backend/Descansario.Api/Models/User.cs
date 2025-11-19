namespace Descansario.Api.Models;

/// <summary>
/// Representa un usuario del sistema con autenticación
/// </summary>
public class User
{
    public int Id { get; set; }

    /// <summary>
    /// Email único del usuario (usado para login)
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Hash BCrypt del password (NUNCA almacenar password en texto plano)
    /// </summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Nombre completo del usuario
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Rol del usuario (Admin, User)
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// ID de la persona vinculada (nullable, auto-vinculado en registro por email)
    /// </summary>
    public int? PersonId { get; set; }

    /// <summary>
    /// Persona vinculada (navigation property)
    /// </summary>
    public Person? Person { get; set; }

    /// <summary>
    /// Fecha de creación del usuario
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de último login (opcional, para auditoría)
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Roles de usuario en el sistema
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Usuario regular - puede ver y gestionar sus propias vacaciones
    /// </summary>
    User,

    /// <summary>
    /// Administrador - acceso completo al sistema
    /// </summary>
    Admin
}
