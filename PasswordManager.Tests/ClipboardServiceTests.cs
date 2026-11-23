using PasswordManager.Core.Services;

namespace PasswordManager.Tests;

public class ClipboardServiceTests
{
    // Portapapeles falso: una simple variable
    private string? _contenido;

    private ClipboardService Crear()
        => new(() => _contenido, t => _contenido = t, () => _contenido = null);

    [Fact]
    public void CopyWithAutoClear_CopiaElTexto()
    {
        using var service = Crear();

        service.CopyWithAutoClear("secreto", 30);

        Assert.Equal("secreto", _contenido);
    }

    [Fact]
    public async Task CopyWithAutoClear_BorraElPortapapelesAlTerminar()
    {
        using var service = Crear();

        service.CopyWithAutoClear("secreto", 1);
        await Task.Delay(1500);

        Assert.Null(_contenido);
    }

    [Fact]
    public async Task CopyWithAutoClear_NoBorraSiElUsuarioCopioOtraCosa()
    {
        using var service = Crear();

        service.CopyWithAutoClear("secreto", 1);
        _contenido = "otra cosa que copió el usuario";
        await Task.Delay(1500);

        Assert.Equal("otra cosa que copió el usuario", _contenido);
    }
}