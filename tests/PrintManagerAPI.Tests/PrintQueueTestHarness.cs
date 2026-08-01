using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Services;
using PrintManagerAPI.Configuration;

namespace PrintManagerAPI.Tests;

/// <summary>
/// Monta PrintService + PrintQueueWorker reais (sem impressora) para
/// exercitar a semântica da fila de ponta a ponta.
/// </summary>
public sealed class PrintQueueTestHarness : IAsyncDisposable
{
    public PrintService Service { get; }
    public PrintQueueWorker Worker { get; }
    public FakePrinterDiscoveryService Discovery { get; }

    public PrintQueueTestHarness(IPrintJobProcessor processor, PrintQueueOptions? options = null)
    {
        options ??= new PrintQueueOptions { MaxQueueLength = 100, PrintJobTimeoutSeconds = 5 };
        Discovery = new FakePrinterDiscoveryService();

        Service = new PrintService(
            NullLogger<PrintService>.Instance,
            Discovery,
            Options.Create(options));

        Worker = new PrintQueueWorker(
            Service,
            processor,
            Options.Create(options),
            NullLogger<PrintQueueWorker>.Instance);
    }

    public Task StartAsync() => Worker.StartAsync(CancellationToken.None);

    public async Task StopAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await Worker.StopAsync(cts.Token);
    }

    /// <summary>Aguarda uma condição com timeout, sem sleeps arbitrários longos.</summary>
    public static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5_000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!condition())
        {
            if (Environment.TickCount64 > deadline)
                throw new TimeoutException($"Condição não satisfeita em {timeoutMs}ms.");
            await Task.Delay(10);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        Worker.Dispose();
    }
}
