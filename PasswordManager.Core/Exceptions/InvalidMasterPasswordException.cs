namespace PasswordManager.Core.Exceptions;

public class InvalidMasterPasswordException : Exception
{
    public InvalidMasterPasswordException()
        : base("La contraseña maestra es incorrecta.") { }
}