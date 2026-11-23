using PasswordManager.Core.Models;
using PasswordManager.Core.Services;

namespace PasswordManager.Tests;

public class StrengthEvaluatorTests
{
    private readonly BasicStrengthEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ContrasenaVacia_EsMuyDebil()
        => Assert.Equal(PasswordStrength.VeryWeak, _evaluator.Evaluate(""));

    [Fact]
    public void Evaluate_ContrasenaComun_EsMuyDebil()
        => Assert.Equal(PasswordStrength.VeryWeak, _evaluator.Evaluate("password"));

    [Fact]
    public void Evaluate_SoloNumerosCortos_EsMuyDebil()
        => Assert.Equal(PasswordStrength.VeryWeak, _evaluator.Evaluate("20052005"));

    [Fact]
    public void Evaluate_LargaYVariada_EsFuerte()
        => Assert.Equal(PasswordStrength.Strong, _evaluator.Evaluate("Tr0ub4dor&3xyz"));

    [Fact]
    public void Evaluate_MuyLargaYVariada_EsMuyFuerte()
        => Assert.Equal(PasswordStrength.VeryStrong, _evaluator.Evaluate("Xk9#mP2$vL7@qR4!"));
}