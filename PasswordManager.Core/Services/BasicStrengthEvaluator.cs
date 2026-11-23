using PasswordManager.Core.Interfaces;
using PasswordManager.Core.Models;

namespace PasswordManager.Core.Services;

public class BasicStrengthEvaluator : IStrengthEvaluator
{
    private static readonly HashSet<string> CommonPasswords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "123456", "12345678", "qwerty", "abc123",
            "admin", "contraseña", "iloveyou", "letmein", "111111"
        };

    public PasswordStrength Evaluate(string password)
    {
        if (string.IsNullOrEmpty(password)) return PasswordStrength.VeryWeak;
        if (CommonPasswords.Contains(password)) return PasswordStrength.VeryWeak;

        int points = 0;

        // Puntos por longitud (hasta 3)
        if (password.Length >= 8) points++;
        if (password.Length >= 12) points++;
        if (password.Length >= 16) points++;

        // Puntos por variedad de caracteres (hasta 2)
        int variety = new[]
        {
            password.Any(char.IsLower),
            password.Any(char.IsUpper),
            password.Any(char.IsDigit),
            password.Any(c => !char.IsLetterOrDigit(c))
        }.Count(tiene => tiene);

        if (variety >= 3) points++;
        if (variety == 4) points++;

        // Un solo carácter repetido ("aaaaaaaaaaaa") nunca es segura
        if (password.Distinct().Count() == 1) points = Math.Min(points, 1);

        return points switch
        {
            <= 1 => PasswordStrength.VeryWeak,
            2 => PasswordStrength.Weak,
            3 => PasswordStrength.Medium,
            4 => PasswordStrength.Strong,
            _ => PasswordStrength.VeryStrong
        };
    }
}