using PasswordManager.Core.Exceptions;
using PasswordManager.Core.Interfaces;
using PasswordManager.Core.Models;

namespace PasswordManager.Tests;

// Repositorio falso: guarda todo en memoria para probar sin tocar el disco
public class InMemoryVaultRepository : IVaultRepository
{
    public EncryptedPayload? Stored { get; set; }
    public Dictionary<string, EncryptedPayload> Backups { get; }

    public InMemoryVaultRepository(Dictionary<string, EncryptedPayload>? backups = null)
        => Backups = backups ?? new();

    public bool Exists() => Stored != null;

    public EncryptedPayload ReadPayload()
        => Stored ?? throw new CorruptedVaultException();

    public void WritePayload(EncryptedPayload payload) => Stored = payload;

    public void ExportBackup(string path) => Backups[path] = Stored!;

    public EncryptedPayload ImportBackup(string path)
        => Backups.TryGetValue(path, out var p) ? p : throw new CorruptedVaultException();
}