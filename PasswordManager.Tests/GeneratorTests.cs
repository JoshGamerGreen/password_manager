using PasswordManager.Core.Models;
using PasswordManager.Core.Services;

namespace PasswordManager.Tests;

public class GeneratorTests
{
    private readonly RandomPasswordGenerator _generator = new();

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void Generate_RespetaLaLongitud(int longitud)
    {
        var password = _generator.Generate(new PasswordOptions { Length = longitud });

        Assert.Equal(longitud, password.Length);
    }

    [Fact]
    public void Generate_SoloDigitos_ContieneSoloDigitos()
    {
        var options = new PasswordOptions
        {
            Length = 20,
            UseLowercase = false,
            UseUppercase = false,
            UseSymbols = false,
            UseDigits = true
        };

        var password = _generator.Generate(options);

        Assert.All(password, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void Generate_IncluyeUnCaracterDeCadaTipoElegido()
    {
        var options = new PasswordOptions { Length = 12 };

        for (int i = 0; i < 50; i++)
        {
            var password = _generator.Generate(options);

            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsDigit);
            Assert.Contains(password, c => !char.IsLetterOrDigit(c));
        }
    }

    [Fact]
    public void Generate_SinTiposSeleccionados_LanzaExcepcion()
    {
        var options = new PasswordOptions
        {
            UseLowercase = false,
            UseUppercase = false,
            UseDigits = false,
            UseSymbols = false
        };

        Assert.Throws<ArgumentException>(() => _generator.Generate(options));
    }

    [Fact]
    public void Generate_ExcluyeCaracteresAmbiguos()
    {
        var options = new PasswordOptions { Length = 30, ExcludeAmbiguous = true };

        for (int i = 0; i < 100; i++)
        {
            var password = _generator.Generate(options);

            Assert.DoesNotContain(password, c => "O0oIl1|".Contains(c));
        }
    }

    [Fact]
    public void Generate_DosLlamadas_DanResultadosDistintos()
    {
        var a = _generator.Generate(new PasswordOptions());
        var b = _generator.Generate(new PasswordOptions());

        Assert.NotEqual(a, b);
    }
}