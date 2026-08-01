using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Models;

namespace PrintManagerAPI.Tests;

/// <summary>
/// Processador falso que registra a ordem de processamento e o grau máximo de
/// concorrência observado — usado para provar FIFO e processamento serial.
/// </summary>
public sealed class FakePrintJobProcessor : IPrintJobProcessor
{
    private int _concurrent;
    private int _maxConcurrent;
    private readonly List<Guid> _processedOrder = new();
    private readonly object _lock = new();

    public TimeSpan Delay { get; set; } = TimeSpan.Zero;
    public Func<PrintJob, Exception?>? FailWith { get; set; }

    public IReadOnlyList<Guid> ProcessedOrder
    {
        get { lock (_lock) return _processedOrder.ToList(); }
    }

    public int MaxObservedConcurrency => Volatile.Read(ref _maxConcurrent);

    public async Task ProcessJobAsync(PrintJob printJob, CancellationToken cancellationToken = default)
    {
        var current = Interlocked.Increment(ref _concurrent);
        InterlockedExtensions.Max(ref _maxConcurrent, current);

        try
        {
            if (Delay > TimeSpan.Zero)
                await Task.Delay(Delay, cancellationToken);

            var exception = FailWith?.Invoke(printJob);
            if (exception is not null)
                throw exception;

            lock (_lock)
            {
                _processedOrder.Add(printJob.Id);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _concurrent);
        }
    }
}

public sealed class FakePrinterDiscoveryService : IPrinterDiscoveryService
{
    public string[] Printers { get; set; } = { "Impressora de Teste" };

    public string[] DiscoverPrinters() => Printers;

    public bool PrinterExists(string printerName) => Printers.Contains(printerName);

    public PrinterInfo? GetPrinterInfo(string printerName) =>
        PrinterExists(printerName)
            ? new PrinterInfo { Name = printerName, IsValid = true, Status = "Available" }
            : null;

    public PrinterInfo[] GetAllPrintersInfo() =>
        Printers.Select(p => new PrinterInfo { Name = p, IsValid = true, Status = "Available" }).ToArray();
}

internal static class InterlockedExtensions
{
    public static void Max(ref int location, int value)
    {
        int current;
        while (value > (current = Volatile.Read(ref location)))
        {
            if (Interlocked.CompareExchange(ref location, value, current) == current)
                break;
        }
    }
}
