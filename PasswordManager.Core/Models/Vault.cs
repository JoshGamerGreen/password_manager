namespace PasswordManager.Core.Models;

public class Vault
{
    public List<VaultEntry> Entries { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void AddEntry(VaultEntry entry) => Entries.Add(entry);

    public bool RemoveEntry(Guid id)
        => Entries.RemoveAll(e => e.Id == id) > 0;

    public VaultEntry? GetById(Guid id)
        => Entries.FirstOrDefault(e => e.Id == id);

    public List<VaultEntry> Search(string query, string? category = null)
    {
        return Entries
            .Where(e => string.IsNullOrWhiteSpace(query)
                        || e.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || e.Username.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Where(e => string.IsNullOrWhiteSpace(category) || e.Category == category)
            .ToList();
    }
}