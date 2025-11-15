using System.Text.RegularExpressions;

namespace Descansario.Api.Helpers;

public static partial class ValidationHelper
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    public static (bool isValid, string? errorMessage) ValidatePersonDto(
        string name,
        string email,
        int availableDays)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "El nombre es requerido");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "El email es requerido");
        }

        if (!EmailRegex().IsMatch(email))
        {
            return (false, "El formato del email no es válido");
        }

        if (availableDays < 0)
        {
            return (false, "Los días disponibles no pueden ser negativos");
        }

        return (true, null);
    }
}
