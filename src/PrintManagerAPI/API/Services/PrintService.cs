using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Models;
using System.Collections.Concurrent;

namespace PrintManagerAPI.API.Services;

public class PrintService : IPrintService
{
    private readonly ConcurrentQueue<PrintJob> _printQueue = new();
    private volatile bool _isPrinting = false;
    private readonly ILogger<PrintService> _logger;
    private readonly IPrintJobProcessor _printJobProcessor;
    private readonly IPrinterDiscoveryService _printerDiscoveryService;
    private string? _currentJobId;

    public PrintService(ILogger<PrintService> logger, IPrintJobProcessor printJobProcessor, IPrinterDiscoveryService printerDiscoveryService)
    {
        _logger = logger;
        _printJobProcessor = printJobProcessor;
        _printerDiscoveryService = printerDiscoveryService;
    }

    public Guid EnqueuePrintJob(string printerName, string text)
    {
        var printJob = new PrintJob(printerName, text);
        _printQueue.Enqueue(printJob);

        _logger.LogInformation("Trabalho de impress�o {JobId} enfileirado para a impressora {PrinterName}", printJob.Id, printerName);

        _ = Task.Run(ProcessQueueAsync);

        return printJob.Id;
    }

    public Guid EnqueuePrintJob(string printerName, string text, PrintSettings settings)
    {
        var printJob = new PrintJob(printerName, text, settings);
        _printQueue.Enqueue(printJob);

        _logger.LogInformation("Trabalho de impress�o {JobId} enfileirado para a impressora {PrinterName}", printJob.Id, printerName);

        _ = Task.Run(ProcessQueueAsync);

        return printJob.Id;
    }

    public string[] GetAvailablePrinters()
    {
        return _printerDiscoveryService.DiscoverPrinters();
    }

    public QueueStatus GetQueueStatus()
    {
        return new QueueStatus
        {
            PendingJobs = _printQueue.Count,
            IsProcessing = _isPrinting,
            CurrentJobId = _currentJobId
        };
    }

    private async Task ProcessQueueAsync()
    {
        if (_isPrinting)
            return;

        _isPrinting = true;

        try
        {
            while (_printQueue.TryDequeue(out PrintJob? printJob))
            {
                if (printJob != null)
                {
                    _currentJobId = printJob.Id.ToString();
                    await _printJobProcessor.ProcessJobAsync(printJob);
                    _currentJobId = null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no processamento da fila");
        }
        finally
        {
            _isPrinting = false;
            _currentJobId = null;
        }
    }
}
