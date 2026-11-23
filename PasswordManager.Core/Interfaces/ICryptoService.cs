using PasswordManager.Core.Models;

namespace PasswordManager.Core.Interfaces;

public interface ICryptoService
{
    byte[] GenerateSalt();
    byte[] DeriveKey(string masterPassword, byte[] salt, int iterations);
    EncryptedPayload Encrypt(byte[] plaintext, byte[] key);
    byte[] Decrypt(EncryptedPayload payload, byte[] key);
}