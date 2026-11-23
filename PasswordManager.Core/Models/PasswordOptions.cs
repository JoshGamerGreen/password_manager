namespace PasswordManager.Core.Models;

public class PasswordOptions
{
    public int Length { get; set; } = 16;
    public bool UseUppercase { get; set; } = true;
    public bool UseLowercase { get; set; } = true;
    public bool UseDigits { get; set; } = true;
    public bool UseSymbols { get; set; } = true;
    public bool ExcludeAmbiguous { get; set; } = false; // evita caracteres confusos como O, 0, l, 1
}