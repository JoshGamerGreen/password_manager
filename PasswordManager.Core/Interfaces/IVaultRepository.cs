using PasswordManager.Core.Models;

namespace PasswordManager.Core.Interfaces;

public interface IVaultRepository
{
    bool Exists();
    EncryptedPayload ReadPayload();
    void WritePayload(EncryptedPayload payload);
    void ExportBackup(string path);
    EncryptedPayload ImportBackup(string path);
}