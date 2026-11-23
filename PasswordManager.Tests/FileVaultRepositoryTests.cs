using PasswordManager.Core.Exceptions;
using PasswordManager.Core.Models;
using PasswordManager.Core.Services;

namespace PasswordManager.Tests;

public class FileVaultRepositoryTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".vault");
    private readonly string _backupPath =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".backup");

    public void Dispose()
    {
        foreach (var p in new[] { _path, _path + ".tmp", _backupPath })
            if (File.Exists(p)) File.Delete(p);
    }

    private static EncryptedPayload CrearPayload() => new()
    {
        Salt = new byte[] { 1, 2, 3 },
        Iterations = 1000,
        Nonce = new byte[] { 4, 5, 6 },
        Tag = new byte[] { 7, 8, 9 },
        CipherText = new byte[] { 10, 11, 12 }
    };

    [Fact]
    public void Exists_SinArchivo_DevuelveFalse()
        => Assert.False(new FileVaultRepository(_path).Exists());

    [Fact]
    public void WriteYRead_DevuelveElMismoPayload()
    {
        var repo = new FileVaultRepository(_path);
        var original = CrearPayload();

        repo.WritePayload(original);
        var leido = repo.ReadPayload();

        Assert.True(repo.Exists());
        Assert.Equal(original.Salt, leido.Salt);
        Assert.Equal(original.Iterations, leido.Iterations);
        Assert.Equal(original.CipherText, leido.CipherText);
    }

    [Fact]
    public void Read_ArchivoCorrupto_LanzaCorruptedVault()
    {
        File.WriteAllText(_path, "esto no es json valido");
        var repo = new FileVaultRepository(_path);

        Assert.Throws<CorruptedVaultException>(() => repo.ReadPayload());
    }

    [Fact]
    public void ExportarEImportar_CopianElContenido()
    {
        var repo = new FileVaultRepository(_path);
        repo.WritePayload(CrearPayload());

        repo.ExportBackup(_backupPath);
        var importado = repo.ImportBackup(_backupPath);

        Assert.Equal(CrearPayload().CipherText, importado.CipherText);
    }

    [Fact]
    public void FlujoCompleto_ConArchivoRealYCifradoReal()
    {
        var crypto = new AesGcmCryptoService();

        var s1 = new VaultService(crypto, new FileVaultRepository(_path), iterations: 1000);
        s1.CreateVault("maestra123");
        s1.AddEntry(new VaultEntry { Title = "Gmail", Password = "abc" });
        s1.Lock();

        var s2 = new VaultService(crypto, new FileVaultRepository(_path), iterations: 1000);
        s2.Unlock("maestra123");

        Assert.Equal("Gmail", s2.Search("")[0].Title);
        Assert.DoesNotContain("Gmail", File.ReadAllText(_path));
    }
}