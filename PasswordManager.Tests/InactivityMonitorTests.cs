using PasswordManager.Core.Services;

namespace PasswordManager.Tests;

public class InactivityMonitorTests
{
    [Fact]
    public async Task Start_DisparaElEventoTrasElTiempo()
    {
        using var monitor = new InactivityMonitor(TimeSpan.FromMilliseconds(200));
        int disparos = 0;
        monitor.TimeoutElapsed += (_, _) => Interlocked.Increment(ref disparos);

        monitor.Start();
        await Task.Delay(600);

        Assert.Equal(1, disparos);
    }

    [Fact]
    public async Task Reset_PospoeneElEvento()
    {
        using var monitor = new InactivityMonitor(TimeSpan.FromMilliseconds(400));
        int disparos = 0;
        monitor.TimeoutElapsed += (_, _) => Interlocked.Increment(ref disparos);

        monitor.Start();
        await Task.Delay(250);
        monitor.Reset();
        await Task.Delay(250); // 500 ms desde Start, pero solo 250 desde Reset

        Assert.Equal(0, disparos);

        await Task.Delay(400);
        Assert.Equal(1, disparos);
    }

    [Fact]
    public async Task Stop_ImpideQueSeDisparee()
    {
        using var monitor = new InactivityMonitor(TimeSpan.FromMilliseconds(200));
        int disparos = 0;
        monitor.TimeoutElapsed += (_, _) => Interlocked.Increment(ref disparos);

        monitor.Start();
        monitor.Stop();
        await Task.Delay(600);

        Assert.Equal(0, disparos);
    }
}