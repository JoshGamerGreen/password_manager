namespace PasswordManager.Core.Services;

public class ClipboardService : IDisposable
{
    private readonly Func<string?> _getText;
    private readonly Action<string> _setText;
    private readonly Action _clear;
    private readonly object _lock = new();

    private Timer? _timer;
    private string? _lastCopied;

    public ClipboardService(Func<string?> getText, Action<string> setText, Action clear)
    {
        _getText = getText;
        _setText = setText;
        _clear = clear;
    }

    public void CopyWithAutoClear(string text, int seconds = 30)
    {
        lock (_lock)
        {
            _timer?.Dispose();

            _setText(text);
            _lastCopied = text;
            _timer = new Timer(_ => ClearIfUnchanged(), null,
                               TimeSpan.FromSeconds(seconds), Timeout.InfiniteTimeSpan);
        }
    }

    // Borra el portapapeles solo si todavía tiene lo que copiamos nosotros
    private void ClearIfUnchanged()
    {
        lock (_lock)
        {
            if (_lastCopied != null && _getText() == _lastCopied)
                _clear();

            _lastCopied = null; // no conservamos la contraseña en memoria
        }
    }

    public void Dispose()
    {
        lock (_lock) { _timer?.Dispose(); }
    }
}