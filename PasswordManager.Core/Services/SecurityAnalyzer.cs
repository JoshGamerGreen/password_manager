using PasswordManager.Core.Interfaces;
using PasswordManager.Core.Models;

namespace PasswordManager.Core.Services;

public class SecurityAnalyzer
{
    private readonly IStrengthEvaluator _evaluator;
    private readonly IBreachChecker? _breachChecker;
    private readonly int _oldAfterDays;

    public SecurityAnalyzer(IStrengthEvaluator evaluator,
                            IBreachChecker? breachChecker = null,
                            int oldAfterDays = 365)
    {
        _evaluator = evaluator;
        _breachChecker = breachChecker;
        _oldAfterDays = oldAfterDays;
    }

    public SecurityReport Analyze(Vault vault, bool checkBreaches = false)
    {
        var report = new SecurityReport();

        // Débiles
        report.WeakEntries = vault.Entries
            .Where(e => _evaluator.Evaluate(e.Password) <= PasswordStrength.Weak)
            .ToList();

        // Repetidas
        report.ReusedGroups = vault.FindReusedPasswords();

        // Antiguas
        report.OldEntries = vault.Entries
            .Where(e => e.AgeInDays() > _oldAfterDays)
            .ToList();

        // Filtradas (solo si se pide y hay un verificador disponible)
        if (checkBreaches && _breachChecker != null)
        {
            report.BreachedEntries = vault.Entries
                .Where(e => !string.IsNullOrEmpty(e.Password)
                            && _breachChecker.IsBreached(e.Password))
                .ToList();
        }

        return report;
    }
}