namespace PasswordManager.Core.Services;

public class InactivityMonitor : IDisposable
{
    private readonly TimeSpan _timeout;
    private readonly Timer _timer;
    private readonly object _lock = new();
    private bool _running;

    public event EventHandler? TimeoutElapsed;

    public InactivityMonitor(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        _timeout = timeout;
        _timer = new Timer(OnTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        lock (_lock)
        {
            _running = true;
            _timer.Change(_timeout, Timeout.InfiniteTimeSpan);
        }
    }

    // Reinicia la cuenta, pero solo si el monitor está corriendo
    public void Reset()
    {
        lock (_lock)
        {
            if (_running)
                _timer.Change(_timeout, Timeout.InfiniteTimeSpan);
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _running = false;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private void OnTick(object? state)
    {
        lock (_lock)
        {
            if (!_running) return;
            _running = false;
        }
        TimeoutElapsed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() => _timer.Dispose();
}