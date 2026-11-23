using System.Text.Json;
using PasswordManager.Core.Exceptions;
using PasswordManager.Core.Interfaces;
using PasswordManager.Core.Models;

namespace PasswordManager.Core.Services;

public class FileVaultRepository : IVaultRepository
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public FileVaultRepository(string filePath)
    {
        _filePath = filePath;
    }

    public bool Exists() => File.Exists(_filePath);

    public EncryptedPayload ReadPayload() => ReadFromFile(_filePath);

    public void WritePayload(EncryptedPayload payload)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(_filePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        // Se escribe primero en un archivo temporal y luego se reemplaza el original,
        // así un corte de luz a mitad de escritura no deja la bóveda dañada.
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(payload, JsonOptions));
        File.Move(tempPath, _filePath, overwrite: true);
    }

    public void ExportBackup(string path)
        => File.Copy(_filePath, path, overwrite: true);

    public EncryptedPayload ImportBackup(string path) => ReadFromFile(path);

    private static EncryptedPayload ReadFromFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<EncryptedPayload>(json)
                   ?? throw new CorruptedVaultException();
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            throw new CorruptedVaultException();
        }
    }
}