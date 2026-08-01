using Microsoft.Extensions.Options;
using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.Configuration;

namespace PrintManagerAPI.API.Services;

/// <summary>
/// Consumidor único da fila de impressão. Processa um job por vez (FIFO),
/// aplica o timeout configurado e registra o desfecho de cada job.
/// O encerramento da aplicação cancela o loop de forma controlada.
/// </summary>
public class PrintQueueWorker : BackgroundService
{
    private readonly PrintService _printService;
    private readonly IPrintJobProcessor _printJobProcessor;
    private readonly PrintQueueOptions _options;
    private readonly ILogger<PrintQueueWorker> _logger;

    public PrintQueueWorker(
        PrintService printService,
        IPrintJobProcessor printJobProcessor,
        IOptions<PrintQueueOptions> options,
        ILogger<PrintQueueWorker> logger)
    {
        _printService = printService;
        _printJobProcessor = printJobProcessor;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var printJob in _printService.Reader.ReadAllAsync(stoppingToken))
        {
            _printService.MarkPrinting(printJob);

            try
            {
                await _printJobProcessor
                    .ProcessJobAsync(printJob, stoppingToken)
                    .WaitAsync(_options.PrintJobTimeout, stoppingToken);

                _printService.MarkCompleted(printJob);
                _logger.LogInformation("Trabalho de impressão {JobId} concluído com sucesso", printJob.Id);
            }
            catch (TimeoutException)
            {
                // O driver pode continuar ocupando a thread original; o timeout
                // libera a fila, mas um driver travado é uma limitação conhecida.
                _printService.MarkFailed(printJob,
                    $"Tempo limite de {_options.PrintJobTimeout.TotalSeconds:0}s excedido.");
                _logger.LogError(
                    "Trabalho {JobId} excedeu o tempo limite de {Timeout}s na impressora {PrinterName}",
                    printJob.Id, _options.PrintJobTimeout.TotalSeconds, printJob.PrinterName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _printService.MarkFailed(printJob, "Aplicação encerrada durante o processamento.");
                _logger.LogWarning("Trabalho {JobId} interrompido pelo encerramento da aplicação", printJob.Id);
                throw;
            }
            catch (Exception ex)
            {
                _printService.MarkFailed(printJob, ex.Message);
                _logger.LogError(ex, "Erro ao processar o trabalho de impressão {JobId}", printJob.Id);
            }
        }
    }
}
