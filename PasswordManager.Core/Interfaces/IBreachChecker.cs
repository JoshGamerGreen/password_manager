namespace PasswordManager.Core.Interfaces;

public interface IBreachChecker
{
    bool IsBreached(string password);
}