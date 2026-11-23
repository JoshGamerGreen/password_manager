using PasswordManager.Core.Models;

namespace PasswordManager.Tests;

public class VaultTests
{
    [Fact]
    public void AddEntry_AgregaLaEntradaALaBoveda()
    {
        var vault = new Vault();
        var entry = new VaultEntry { Title = "Gmail" };

        vault.AddEntry(entry);

        Assert.Single(vault.Entries);
    }

    [Fact]
    public void Search_EncuentraPorTitulo()
    {
        var vault = new Vault();
        vault.AddEntry(new VaultEntry { Title = "Gmail" });
        vault.AddEntry(new VaultEntry { Title = "Netflix" });

        var resultados = vault.Search("gma");

        Assert.Single(resultados);
    }

    [Fact]
    public void FindReusedPasswords_DetectaContrasenasRepetidas()
    {
        var vault = new Vault();
        vault.AddEntry(new VaultEntry { Title = "Gmail", Password = "abc123" });
        vault.AddEntry(new VaultEntry { Title = "Netflix", Password = "abc123" });
        vault.AddEntry(new VaultEntry { Title = "Banco", Password = "otraClave!9" });

        var grupos = vault.FindReusedPasswords();

        Assert.Single(grupos);
        Assert.Equal(2, grupos[0].Count);
    }
}