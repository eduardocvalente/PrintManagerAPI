using PrintManagerAPI.API.Models;

namespace PrintManagerAPI.API.Interfaces;

public interface IPrintJobProcessor
{
    /// <summary>
    /// Processa um trabalho de impressão específico
    /// </summary>
    /// <param name="printJob">Trabalho a ser processado</param>
    /// <returns>Task representando a operação assíncrona</returns>
    Task ProcessJobAsync(PrintJob printJob);

    /// <summary>
    /// Valida se uma impressora está disponível e funcionando
    /// </summary>
    /// <param name="printerName">Nome da impressora</param>
    /// <returns>True se a impressora for válida</returns>
    bool ValidatePrinter(string printerName);
}
