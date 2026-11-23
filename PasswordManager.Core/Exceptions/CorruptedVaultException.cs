namespace PasswordManager.Core.Exceptions;

public class CorruptedVaultException : Exception
{
    public CorruptedVaultException()
        : base("El archivo de la bóveda está dañado o fue modificado.") { }
}