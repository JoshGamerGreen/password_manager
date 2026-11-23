using PasswordManager.Core.Models;

namespace PasswordManager.Core.Interfaces;

public interface IPasswordGenerator
{
    string Generate(PasswordOptions options);
}