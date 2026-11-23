using PasswordManager.Core.Interfaces;
using PasswordManager.Core.Models;
using PasswordManager.Core.Services;

namespace PasswordManager.Tests;

// Verificador falso: considera filtradas las contraseñas que le indiquemos
public class FakeBreachChecker : IBreachChecker
{
    private readonly HashSet<string> _filtradas;
    public FakeBreachChecker(params string[] filtradas) => _filtradas = new(filtradas);
    public bool IsBreached(string password) => _filtradas.Contains(password);
}

public class SecurityAnalyzerTests
{
    private const string Fuerte1 = "Xk9#mP2$vL7@qR4!";
    private const string Fuerte2 = "Zq8&nT5^wB3*hY6%";

    private static SecurityAnalyzer Crear(IBreachChecker? breach = null)
        => new(new BasicStrengthEvaluator(), breach);

    [Fact]
    public void Analyze_DetectaContrasenasDebiles()
    {
        var vault = new Vault();
        vault.AddEntry(new VaultEntry { Title = "Débil", Password = "123456" });
        vault.AddEntry(new VaultEntry { Title = "Fuerte", Password = Fuerte1 });

        var report = Crear().Analyze(vault);

        Assert.Single(report.WeakEntries);
        Assert.Equal("Débil", report.WeakEntries[0].Title);
    }

    [Fact]
    public void Analyze_DetectaContrasenasRepetidas()
    {
        var vault = new Vault();
        vault.AddEntry(new VaultEntry { Title = "A", Password = Fuerte1 });
        vault.AddEntry(new VaultEntry { Title = "B", Password = Fuerte1 });
        vault.AddEntry(new VaultEntry { Title = "C", Password = Fuerte2 });

        var report = Crear().Analyze(vault);

        Assert.Single(report.ReusedGroups);
        Assert.Equal(2, report.ReusedGroups[0].Count);
    }

    [Fact]
    public void Analyze_DetectaContrasenasAntiguas()
    {
        var vault = new Vault();
        vault.AddEntry(new VaultEntry
        {
            Title = "Vieja",
            Password = Fuerte1,
            PasswordChangedAt = DateTime.UtcNow.AddDays(-400)
        });
        vault.AddEntry(new VaultEntry { Title = "Reciente", Password = Fuerte2 });

        var report = Crear().Analyze(vault);

        Assert.Single(report.OldEntries);
        Assert.Equal("Vieja", report.OldEntries[0].Title);
    }

    [Fact]
    public void Analyze_FiltracionesSoloSeRevisanSiSePide()
    {
        var vault = new Vault();
        vault.AddEntry(new VaultEntry { Title = "Filtrada", Password = Fuerte1 });
        vault.AddEntry(new VaultEntry { Title = "Limpia", Password = Fuerte2 });
        var analyzer = Crear(new FakeBreachChecker(Fuerte1));

        var sinRevisar = analyzer.Analyze(vault, checkBreaches: false);
        var revisando = analyzer.Analyze(vault, checkBreaches: true);

        Assert.Empty(sinRevisar.BreachedEntries);
        Assert.Single(revisando.BreachedEntries);
        Assert.Equal("Filtrada", revisando.BreachedEntries[0].Title);
    }
}