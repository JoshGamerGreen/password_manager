using System.Security.Cryptography;
using PasswordManager.Core.Interfaces;
using PasswordManager.Core.Models;

namespace PasswordManager.Core.Services;

public class RandomPasswordGenerator : IPasswordGenerator
{
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{};:,.?";
    private const string Ambiguous = "O0oIl1|";

    public string Generate(PasswordOptions options)
    {
        // 1. Armar los grupos de caracteres que el usuario eligió
        var sets = new List<string>();
        if (options.UseLowercase) sets.Add(Lower);
        if (options.UseUppercase) sets.Add(Upper);
        if (options.UseDigits) sets.Add(Digits);
        if (options.UseSymbols) sets.Add(Symbols);

        if (options.ExcludeAmbiguous)
            sets = sets.Select(s => new string(s.Where(c => !Ambiguous.Contains(c)).ToArray())).ToList();

        if (sets.Count == 0)
            throw new ArgumentException("Debes seleccionar al menos un tipo de carácter.");
        if (options.Length < sets.Count)
            throw new ArgumentException("La longitud es muy corta para los tipos de carácter elegidos.");

        // 2. Garantizar al menos un carácter de cada grupo elegido
        var chars = new List<char>();
        foreach (var set in sets)
            chars.Add(set[RandomNumberGenerator.GetInt32(set.Length)]);

        // 3. Completar el resto con caracteres de cualquier grupo
        var all = string.Concat(sets);
        while (chars.Count < options.Length)
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

        // 4. Mezclar (Fisher-Yates), para que los obligatorios no queden siempre al inicio
        for (int i = chars.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }
}