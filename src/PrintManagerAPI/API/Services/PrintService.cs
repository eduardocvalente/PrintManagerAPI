using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Models;
using PrintManagerAPI.Configuration;

namespace PrintManagerAPI.API.Services;

/// <summary>
/// Gerencia a fila de impressão. O enfileiramento usa um Channel limitado
/// (backpressure) e o consumo é feito exclusivamente pelo PrintQueueWorker,
/// o que garante FIFO e no máximo um job em processamento por vez sem
/// depender de flags de controle sujeitas a race conditions.
/// </summary>
public class PrintService : IPrintService
{
    private readonly Channel<PrintJob> _queue;
    private readonly ConcurrentDictionary<Guid, PrintJobStatusInfo> _jobs = new();
    private readonly ConcurrentQueue<Guid> _finishedOrder = new();
    private readonly ILogger<PrintService> _logger;
    private readonly IPrinterDiscoveryService _printerDiscoveryService;
    private readonly PrintQueueOptions _options;
    private volatile PrintJobStatusInfo? _currentJob;

    public PrintService(
        ILogger<PrintService> logger,
        IPrinterDiscoveryService printerDiscoveryService,
        IOptions<PrintQueueOptions> options)
    {
        _logger = logger;
        _printerDiscoveryService = printerDiscoveryService;
        _options = options.Value;
        _queue = Channel.CreateBounded<PrintJob>(new BoundedChannelOptions(_options.MaxQueueLength)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    internal ChannelReader<PrintJob> Reader => _queue.Reader;

    public EnqueueResult EnqueuePrintJob(string printerName, string text, PrintSettings? settings = null)
    {
        var printJob = new PrintJob(printerName, text, settings);

        _jobs[printJob.Id] = new PrintJobStatusInfo
        {
            Id = printJob.Id,
            PrinterName = printerName,
            State = PrintJobState.Queued,
            EnqueuedAt = DateTimeOffset.UtcNow
        };

        if (!_queue.Writer.TryWrite(printJob))
        {
            _jobs.TryRemove(printJob.Id, out _);
            _logger.LogWarning(
                "Fila de impressão cheia ({MaxQueueLength} jobs); trabalho para {PrinterName} recusado",
                _options.MaxQueueLength, printerName);
            return new EnqueueResult(false, printJob.Id,
                $"A fila de impressão atingiu a capacidade máxima de {_options.MaxQueueLength} trabalhos.");
        }

        _logger.LogInformation(
            "Trabalho de impressão {JobId} enfileirado para a impressora {PrinterName}",
            printJob.Id, printerName);

        return new EnqueueResult(true, printJob.Id);
    }

    public string[] GetAvailablePrinters()
    {
        return _printerDiscoveryService.DiscoverPrinters();
    }

    public QueueStatus GetQueueStatus()
    {
        var current = _currentJob;
        return new QueueStatus
        {
            PendingJobs = _queue.Reader.Count,
            IsProcessing = current is not null,
            CurrentJobId = current?.Id
        };
    }

    public PrintJobStatusInfo? GetJobStatus(Guid jobId)
    {
        return _jobs.TryGetValue(jobId, out var status) ? status : null;
    }

    internal void MarkPrinting(PrintJob printJob)
    {
        if (_jobs.TryGetValue(printJob.Id, out var status))
        {
            status.State = PrintJobState.Printing;
            status.StartedAt = DateTimeOffset.UtcNow;
            _currentJob = status;
        }
    }

    internal void MarkCompleted(PrintJob printJob) => FinishJob(printJob, PrintJobState.Completed, null);

    internal void MarkFailed(PrintJob printJob, string error) => FinishJob(printJob, PrintJobState.Failed, error);

    private void FinishJob(PrintJob printJob, PrintJobState finalState, string? error)
    {
        _currentJob = null;

        if (!_jobs.TryGetValue(printJob.Id, out var status))
            return;

        status.State = finalState;
        status.FinishedAt = DateTimeOffset.UtcNow;
        status.Error = error;

        _finishedOrder.Enqueue(printJob.Id);
        while (_finishedOrder.Count > _options.MaxTrackedJobs && _finishedOrder.TryDequeue(out var oldest))
        {
            _jobs.TryRemove(oldest, out _);
        }
    }
}
