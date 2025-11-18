using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Descansario.Api.Data;
using Descansario.Api.DTOs;
using Descansario.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Descansario.Api.Services;

/// <summary>
/// Servicio de autenticación para login, registro y gestión de tokens JWT
/// </summary>
public class AuthService
{
    private readonly DescansarioDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(DescansarioDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Autenticar usuario con email y password
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // Validar entrada
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning("Login attempt with empty credentials");
            return null;
        }

        // Buscar usuario por email
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());

        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent user: {Email}", request.Email);
            return null;
        }

        // Verificar password con BCrypt
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            return null;
        }

        // Actualizar último login
        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Generar token JWT
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(GetJwtExpirationHours());

        _logger.LogInformation("Successful login for user: {Email}", request.Email);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt
            }
        };
    }

    /// <summary>
    /// Registrar nuevo usuario
    /// </summary>
    public async Task<UserDto?> RegisterAsync(RegisterRequest request)
    {
        // Validar entrada
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Name))
        {
            _logger.LogWarning("Register attempt with empty fields");
            return null;
        }

        // Validar formato de email
        if (!IsValidEmail(request.Email))
        {
            _logger.LogWarning("Register attempt with invalid email format: {Email}", request.Email);
            return null;
        }

        // Validar longitud de password
        if (request.Password.Length < 6)
        {
            _logger.LogWarning("Register attempt with weak password for: {Email}", request.Email);
            return null;
        }

        // Verificar que el email no exista
        var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant());
        if (emailExists)
        {
            _logger.LogWarning("Register attempt with existing email: {Email}", request.Email);
            return null;
        }

        // Hash del password con BCrypt (work factor 12)
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        // Crear usuario
        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Name = request.Name,
            Role = UserRole.User, // Por defecto todos son User, no Admin
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email}", user.Email);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    /// Generar token JWT para un usuario
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var jwtSecret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var jwtIssuer = _config["Jwt:Issuer"] ?? "descansario-api";
        var jwtAudience = _config["Jwt:Audience"] ?? "descansario-web";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(GetJwtExpirationHours()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Obtener tiempo de expiración de JWT desde configuración
    /// </summary>
    private int GetJwtExpirationHours()
    {
        var expirationHours = _config.GetValue<int>("Jwt:ExpirationHours");
        return expirationHours > 0 ? expirationHours : 168; // Default: 7 días
    }

    /// <summary>
    /// Validar formato de email (simple)
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
