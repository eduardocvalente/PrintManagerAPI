using PrintManagerAPI.API.Models;

namespace PrintManagerAPI.API.Interfaces;

public interface IPrintService
{
    /// <summary>
    /// Enfileira um trabalho de impressão
    /// </summary>
    /// <param name="printerName">Nome da impressora</param>
    /// <param name="text">Texto a ser impresso</param>
    /// <returns>ID do trabalho criado</returns>
    Guid EnqueuePrintJob(string printerName, string text);

    /// <summary>
    /// Enfileira um trabalho de impressão com configurações personalizadas
    /// </summary>
    /// <param name="printerName">Nome da impressora</param>
    /// <param name="text">Texto a ser impresso</param>
    /// <param name="settings">Configurações de impressão</param>
    /// <returns>ID do trabalho criado</returns>
    Guid EnqueuePrintJob(string printerName, string text, PrintSettings settings);

    /// <summary>
    /// Obtém todas as impressoras disponíveis no sistema
    /// </summary>
    /// <returns>Array com nomes das impressoras</returns>
    string[] GetAvailablePrinters();

    /// <summary>
    /// Obtém o status atual da fila
    /// </summary>
    /// <returns>Informações sobre a fila</returns>
    QueueStatus GetQueueStatus();
}

public class QueueStatus
{
    public int PendingJobs { get; set; }
    public bool IsProcessing { get; set; }
    public string? CurrentJobId { get; set; }
}
