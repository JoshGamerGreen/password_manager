namespace PasswordManager.Core.Models;

public class EncryptedPayload
{
    public int Version { get; set; } = 1;
    public byte[] Salt { get; set; } = Array.Empty<byte>();
    public int Iterations { get; set; }
    public byte[] Nonce { get; set; } = Array.Empty<byte>();
    public byte[] Tag { get; set; } = Array.Empty<byte>();
    public byte[] CipherText { get; set; } = Array.Empty<byte>();
}