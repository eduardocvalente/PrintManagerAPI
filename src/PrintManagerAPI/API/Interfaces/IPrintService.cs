using PrintManagerAPI.API.Models;

namespace PrintManagerAPI.API.Interfaces;

public interface IPrintService
{
    /// <summary>
    /// Enfileira um trabalho de impressão. Pode ser recusado se a fila estiver cheia.
    /// </summary>
    /// <param name="printerName">Nome da impressora</param>
    /// <param name="text">Texto a ser impresso</param>
    /// <param name="settings">Configurações de impressão (opcional)</param>
    /// <returns>Resultado com o ID do trabalho e se foi aceito</returns>
    EnqueueResult EnqueuePrintJob(string printerName, string text, PrintSettings? settings = null);

    /// <summary>
    /// Obtém todas as impressoras disponíveis no sistema
    /// </summary>
    string[] GetAvailablePrinters();

    /// <summary>
    /// Obtém o status atual da fila
    /// </summary>
    QueueStatus GetQueueStatus();

    /// <summary>
    /// Obtém o status de um trabalho específico, ou null se desconhecido
    /// (jobs concluídos são retidos até o limite configurado em MaxTrackedJobs)
    /// </summary>
    PrintJobStatusInfo? GetJobStatus(Guid jobId);
}
