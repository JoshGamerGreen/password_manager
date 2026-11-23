using PasswordManager.Core.Models;

namespace PasswordManager.Core.Interfaces;

public interface IStrengthEvaluator
{
    PasswordStrength Evaluate(string password);
}