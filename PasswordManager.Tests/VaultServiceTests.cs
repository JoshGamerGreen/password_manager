using System.Text;
using PasswordManager.Core.Exceptions;
using PasswordManager.Core.Models;
using PasswordManager.Core.Services;

namespace PasswordManager.Tests;

public class VaultServiceTests
{
    private const string Maestra = "maestra123";

    private static VaultService CrearServicio(InMemoryVaultRepository repo)
        => new(new AesGcmCryptoService(), repo, iterations: 1000);

    [Fact]
    public void CreateVault_DejaLaBovedaDesbloqueadaYGuardada()
    {
        var repo = new InMemoryVaultRepository();
        var service = CrearServicio(repo);

        service.CreateVault(Maestra);

        Assert.True(service.IsUnlocked);
        Assert.True(repo.Exists());
        Assert.Equal(1000, repo.Stored!.Iterations);
        Assert.Equal(16, repo.Stored.Salt.Length);
    }

    [Fact]
    public void CreateVault_SiYaExiste_LanzaExcepcion()
    {
        var repo = new InMemoryVaultRepository();
        CrearServicio(repo).CreateVault(Maestra);

        var otro = CrearServicio(repo);

        Assert.Throws<InvalidOperationException>(() => otro.CreateVault(Maestra));
    }

    [Fact]
    public void Unlock_ConContrasenaCorrecta_RecuperaLasEntradas()
    {
        var repo = new InMemoryVaultRepository();
        var s1 = CrearServicio(repo);
        s1.CreateVault(Maestra);
        s1.AddEntry(new VaultEntry { Title = "Gmail" });

        var s2 = CrearServicio(repo);
        s2.Unlock(Maestra);

        Assert.Single(s2.Search(""));
    }

    [Fact]
    public void Unlock_ConContrasenaIncorrecta_LanzaExcepcion()
    {
        var repo = new InMemoryVaultRepository();
        CrearServicio(repo).CreateVault(Maestra);

        var service = CrearServicio(repo);

        Assert.Throws<InvalidMasterPasswordException>(() => service.Unlock("incorrecta"));
        Assert.False(service.IsUnlocked);
    }

    [Fact]
    public void Unlock_ConDatosAlterados_LanzaExcepcion()
    {
        var repo = new InMemoryVaultRepository();
        var s1 = CrearServicio(repo);
        s1.CreateVault(Maestra);
        s1.AddEntry(new VaultEntry { Title = "Gmail" });

        repo.Stored!.CipherText[0] ^= 1; // alteramos un bit del archivo

        var s2 = CrearServicio(repo);

        Assert.Throws<InvalidMasterPasswordException>(() => s2.Unlock(Maestra));
    }

    [Fact]
    public void Unlock_ConMetadatosInvalidos_LanzaCorruptedVault()
    {
        var repo = new InMemoryVaultRepository();
        CrearServicio(repo).CreateVault(Maestra);

        repo.Stored!.Iterations = 0;

        var service = CrearServicio(repo);

        Assert.Throws<CorruptedVaultException>(() => service.Unlock(Maestra));
    }

    [Fact]
    public void Lock_ImpideOperarConLaBoveda()
    {
        var service = CrearServicio(new InMemoryVaultRepository());
        service.CreateVault(Maestra);

        service.Lock();

        Assert.False(service.IsUnlocked);
        Assert.Throws<InvalidOperationException>(() => service.AddEntry(new VaultEntry()));
    }

    [Fact]
    public void AddEntry_GuardaLosDatosCifrados()
    {
        var repo = new InMemoryVaultRepository();
        var service = CrearServicio(repo);
        service.CreateVault(Maestra);

        service.AddEntry(new VaultEntry { Title = "Gmail", Password = "MiClaveSecreta123" });

        var textoGuardado = Encoding.UTF8.GetString(repo.Stored!.CipherText);
        Assert.DoesNotContain("MiClaveSecreta123", textoGuardado);
        Assert.DoesNotContain("Gmail", textoGuardado);
    }

    [Fact]
    public void UpdateEntry_PersisteElCambio()
    {
        var repo = new InMemoryVaultRepository();
        var s1 = CrearServicio(repo);
        s1.CreateVault(Maestra);
        var entry = new VaultEntry { Title = "Gmail" };
        s1.AddEntry(entry);

        entry.Title = "Gmail personal";
        s1.UpdateEntry(entry);

        var s2 = CrearServicio(repo);
        s2.Unlock(Maestra);
        Assert.Equal("Gmail personal", s2.Search("")[0].Title);
    }

    [Fact]
    public void DeleteEntry_EliminaYPersiste()
    {
        var repo = new InMemoryVaultRepository();
        var s1 = CrearServicio(repo);
        s1.CreateVault(Maestra);
        var entry = new VaultEntry { Title = "Gmail" };
        s1.AddEntry(entry);

        s1.DeleteEntry(entry.Id);

        var s2 = CrearServicio(repo);
        s2.Unlock(Maestra);
        Assert.Empty(s2.Search(""));
    }

    [Fact]
    public void ChangeMasterPassword_LaAnteriorDejaDeFuncionar()
    {
        var repo = new InMemoryVaultRepository();
        var s1 = CrearServicio(repo);
        s1.CreateVault("vieja1234");
        s1.AddEntry(new VaultEntry { Title = "Gmail" });

        s1.ChangeMasterPassword("vieja1234", "nueva5678");

        var s2 = CrearServicio(repo);
        Assert.Throws<InvalidMasterPasswordException>(() => s2.Unlock("vieja1234"));
        s2.Unlock("nueva5678");
        Assert.Single(s2.Search(""));
    }

    [Fact]
    public void ChangeMasterPassword_ConActualIncorrecta_LanzaExcepcion()
    {
        var service = CrearServicio(new InMemoryVaultRepository());
        service.CreateVault(Maestra);

        Assert.Throws<InvalidMasterPasswordException>(
            () => service.ChangeMasterPassword("incorrecta", "nueva5678"));
    }

    [Fact]
    public void ExportarEImportar_RestauraLasEntradas()
    {
        var repoA = new InMemoryVaultRepository();
        var a = CrearServicio(repoA);
        a.CreateVault(Maestra);
        a.AddEntry(new VaultEntry { Title = "Gmail" });
        a.ExportBackup("respaldo.vault");

        var repoB = new InMemoryVaultRepository(repoA.Backups);
        var b = CrearServicio(repoB);
        b.ImportBackup("respaldo.vault", Maestra);

        Assert.True(b.IsUnlocked);
        Assert.True(repoB.Exists());
        Assert.Single(b.Search(""));
    }

    [Fact]
    public void ImportBackup_ConContrasenaIncorrecta_LanzaExcepcion()
    {
        var repoA = new InMemoryVaultRepository();
        var a = CrearServicio(repoA);
        a.CreateVault(Maestra);
        a.ExportBackup("respaldo.vault");

        var b = CrearServicio(new InMemoryVaultRepository(repoA.Backups));

        Assert.Throws<InvalidMasterPasswordException>(
            () => b.ImportBackup("respaldo.vault", "incorrecta"));
        Assert.False(b.IsUnlocked);
    }
}