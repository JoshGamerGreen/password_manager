using System.Security.Cryptography;
using PasswordManager.Core.Interfaces;
using PasswordManager.Core.Models;

namespace PasswordManager.Core.Services;

public class AesGcmCryptoService : ICryptoService
{
    private const int SaltSize = 16;   // 16 bytes de sal
    private const int NonceSize = 12;  // 12 bytes, el tamaño estándar para AES-GCM
    private const int TagSize = 16;    // 16 bytes de etiqueta de autenticación
    private const int KeySize = 32;    // 32 bytes = 256 bits (AES-256)

    public byte[] GenerateSalt()
        => RandomNumberGenerator.GetBytes(SaltSize);

    public byte[] DeriveKey(string masterPassword, byte[] salt, int iterations)
        => Rfc2898DeriveBytes.Pbkdf2(
            masterPassword, salt, iterations, HashAlgorithmName.SHA256, KeySize);

    public EncryptedPayload Encrypt(byte[] plaintext, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize); // nuevo en CADA cifrado
        var cipherText = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, cipherText, tag);

        return new EncryptedPayload
        {
            Nonce = nonce,
            Tag = tag,
            CipherText = cipherText
        };
    }

    public byte[] Decrypt(EncryptedPayload payload, byte[] key)
    {
        var plaintext = new byte[payload.CipherText.Length];

        using var aes = new AesGcm(key, TagSize);
        // Lanza CryptographicException si la clave es incorrecta o los datos fueron alterados
        aes.Decrypt(payload.Nonce, payload.CipherText, payload.Tag, plaintext);

        return plaintext;
    }
}