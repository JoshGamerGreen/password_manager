using System.Security.Cryptography;
using System.Text.Json;
using PasswordManager.Core.Exceptions;
using PasswordManager.Core.Interfaces;
using PasswordManager.Core.Models;

namespace PasswordManager.Core.Services;

public class VaultService
{
    public const int DefaultIterations = 600_000;
    private const int MaxIterations = 10_000_000;

    private readonly ICryptoService _crypto;
    private readonly IVaultRepository _repository;
    private readonly int _iterationsForNewVaults;

    private Vault? _currentVault;
    private byte[]? _sessionKey;
    private byte[]? _salt;
    private int _iterations;

    public VaultService(ICryptoService crypto, IVaultRepository repository,
                        int iterations = DefaultIterations)
    {
        _crypto = crypto;
        _repository = repository;
        _iterationsForNewVaults = iterations;
    }

    public bool IsUnlocked => _currentVault != null && _sessionKey != null;

    // ---------- Ciclo de vida de la bóveda ----------

    public void CreateVault(string masterPassword)
    {
        if (string.IsNullOrWhiteSpace(masterPassword))
            throw new ArgumentException("La contraseña maestra no puede estar vacía.");
        if (_repository.Exists())
            throw new InvalidOperationException("Ya existe una bóveda.");

        Lock();
        _salt = _crypto.GenerateSalt();
        _iterations = _iterationsForNewVaults;
        _sessionKey = _crypto.DeriveKey(masterPassword, _salt, _iterations);
        _currentVault = new Vault();

        Persist();
    }

    public void Unlock(string masterPassword)
    {
        var payload = _repository.ReadPayload();
        var (vault, key) = OpenPayload(payload, masterPassword);

        Lock(); // limpia cualquier sesión anterior
        _currentVault = vault;
        _sessionKey = key;
        _salt = payload.Salt;
        _iterations = payload.Iterations;
    }

    public void Lock()
    {
        if (_sessionKey != null)
            CryptographicOperations.ZeroMemory(_sessionKey); // borra la clave de la memoria

        _sessionKey = null;
        _currentVault = null;
    }

    // ---------- Operaciones sobre las entradas ----------

    public void AddEntry(VaultEntry entry)
    {
        RequireUnlocked();
        _currentVault!.AddEntry(entry);
        Persist();
    }

    public void UpdateEntry(VaultEntry entry)
    {
        RequireUnlocked();
        if (!_currentVault!.UpdateEntry(entry))
            throw new KeyNotFoundException("No existe una entrada con ese Id.");
        Persist();
    }

    public void DeleteEntry(Guid id)
    {
        RequireUnlocked();
        if (!_currentVault!.RemoveEntry(id))
            throw new KeyNotFoundException("No existe una entrada con ese Id.");
        Persist();
    }

    public VaultEntry? GetEntry(Guid id)
    {
        RequireUnlocked();
        return _currentVault!.GetById(id);
    }

    public List<VaultEntry> Search(string query, string? category = null)
    {
        RequireUnlocked();
        return _currentVault!.Search(query, category);
    }

    // ---------- Contraseña maestra y respaldos ----------

    public void ChangeMasterPassword(string current, string newPassword)
    {
        RequireUnlocked();
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("La nueva contraseña no puede estar vacía.");

        // Comprobar que la contraseña actual es la correcta
        var check = _crypto.DeriveKey(current, _salt!, _iterations);
        bool esCorrecta = CryptographicOperations.FixedTimeEquals(check, _sessionKey!);
        CryptographicOperations.ZeroMemory(check);
        if (!esCorrecta)
            throw new InvalidMasterPasswordException();

        // Nueva sal y nueva clave
        CryptographicOperations.ZeroMemory(_sessionKey!);
        _salt = _crypto.GenerateSalt();
        _iterations = _iterationsForNewVaults;
        _sessionKey = _crypto.DeriveKey(newPassword, _salt, _iterations);

        Persist();
    }

    public void ExportBackup(string path)
    {
        RequireUnlocked();
        _repository.ExportBackup(path);
    }

    // Reemplaza la bóveda actual por la del respaldo
    public void ImportBackup(string path, string masterPassword)
    {
        var payload = _repository.ImportBackup(path);
        var (vault, key) = OpenPayload(payload, masterPassword);

        Lock();
        _currentVault = vault;
        _sessionKey = key;
        _salt = payload.Salt;
        _iterations = payload.Iterations;

        Persist();
    }

    // ---------- Métodos internos ----------

    private void RequireUnlocked()
    {
        if (!IsUnlocked)
            throw new InvalidOperationException("La bóveda está bloqueada.");
    }

    private void Persist()
    {
        RequireUnlocked();

        var plaintext = Serialize(_currentVault!);
        try
        {
            var payload = _crypto.Encrypt(plaintext, _sessionKey!);
            payload.Salt = _salt!;
            payload.Iterations = _iterations;
            _repository.WritePayload(payload);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    // Deriva la clave, descifra y reconstruye la bóveda (lo usan Unlock e ImportBackup)
    private (Vault vault, byte[] key) OpenPayload(EncryptedPayload payload, string masterPassword)
    {
        // Si los datos básicos del archivo no tienen sentido, está dañado
        if (payload.Salt.Length == 0 || payload.Nonce.Length != 12 || payload.Tag.Length != 16
            || payload.Iterations < 1 || payload.Iterations > MaxIterations)
            throw new CorruptedVaultException();

        var key = _crypto.DeriveKey(masterPassword, payload.Salt, payload.Iterations);

        byte[] plaintext;
        try
        {
            plaintext = _crypto.Decrypt(payload, key);
        }
        catch (CryptographicException)
        {
            CryptographicOperations.ZeroMemory(key);
            throw new InvalidMasterPasswordException();
        }

        Vault vault;
        try
        {
            vault = Deserialize(plaintext);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(key);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }

        return (vault, key);
    }

    private static byte[] Serialize(Vault vault)
        => JsonSerializer.SerializeToUtf8Bytes(vault);

    private static Vault Deserialize(byte[] data)
    {
        try
        {
            return JsonSerializer.Deserialize<Vault>(data) ?? throw new CorruptedVaultException();
        }
        catch (JsonException)
        {
            throw new CorruptedVaultException();
        }
    }
}