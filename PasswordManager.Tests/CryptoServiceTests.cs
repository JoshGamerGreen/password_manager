using System.Security.Cryptography;
using System.Text;
using PasswordManager.Core.Services;

namespace PasswordManager.Tests;

public class CryptoServiceTests
{
    private readonly AesGcmCryptoService _crypto = new();
    private const int TestIterations = 1000; // bajo solo para que las pruebas sean rápidas

    private byte[] CrearClave(string password = "maestra123")
        => _crypto.DeriveKey(password, _crypto.GenerateSalt(), TestIterations);

    [Fact]
    public void CifrarYDescifrar_DevuelveElTextoOriginal()
    {
        var key = CrearClave();
        var original = Encoding.UTF8.GetBytes("mi contraseña secreta");

        var payload = _crypto.Encrypt(original, key);
        var resultado = _crypto.Decrypt(payload, key);

        Assert.Equal(original, resultado);
    }

    [Fact]
    public void Descifrar_ConClaveIncorrecta_Falla()
    {
        var payload = _crypto.Encrypt(Encoding.UTF8.GetBytes("secreto"), CrearClave("correcta"));
        var claveIncorrecta = CrearClave("incorrecta");

        Assert.ThrowsAny<CryptographicException>(
            () => _crypto.Decrypt(payload, claveIncorrecta));
    }

    [Fact]
    public void Descifrar_DatosAlterados_Falla()
    {
        var key = CrearClave();
        var payload = _crypto.Encrypt(Encoding.UTF8.GetBytes("secreto"), key);

        payload.CipherText[0] ^= 1; // alteramos un solo bit

        Assert.ThrowsAny<CryptographicException>(
            () => _crypto.Decrypt(payload, key));
    }

    [Fact]
    public void CifrarDosVeces_ProduceResultadosDistintos()
    {
        var key = CrearClave();
        var datos = Encoding.UTF8.GetBytes("mismo texto");

        var a = _crypto.Encrypt(datos, key);
        var b = _crypto.Encrypt(datos, key);

        Assert.NotEqual(a.CipherText, b.CipherText);
        Assert.NotEqual(a.Nonce, b.Nonce);
    }

    [Fact]
    public void DeriveKey_ConMismosDatos_DaLaMismaClave()
    {
        var salt = _crypto.GenerateSalt();

        var k1 = _crypto.DeriveKey("maestra123", salt, TestIterations);
        var k2 = _crypto.DeriveKey("maestra123", salt, TestIterations);

        Assert.Equal(k1, k2);
        Assert.Equal(32, k1.Length);
    }
}