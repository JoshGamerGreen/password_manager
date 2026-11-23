namespace PasswordManager.Core.Models;

public class SecurityReport
{
    public List<VaultEntry> WeakEntries { get; set; } = new();
    public List<List<VaultEntry>> ReusedGroups { get; set; } = new();
    public List<VaultEntry> OldEntries { get; set; } = new();
    public List<VaultEntry> BreachedEntries { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public int TotalIssues()
        => WeakEntries.Count + ReusedGroups.Count + OldEntries.Count + BreachedEntries.Count;
}