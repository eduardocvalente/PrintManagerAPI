using PrintManagerAPI.API.Models;

namespace PrintManagerAPI.API.Interfaces;

public interface IPrinterDiscoveryService
{
    /// <summary>
    /// Descobre todas as impressoras instaladas no sistema
    /// </summary>
    /// <returns>Lista de nomes de impressoras</returns>
    string[] DiscoverPrinters();

    /// <summary>
    /// Verifica se uma impressora específica existe
    /// </summary>
    /// <param name="printerName">Nome da impressora</param>
    /// <returns>True se existir</returns>
    bool PrinterExists(string printerName);

    /// <summary>
    /// Obtém informações detalhadas de uma impressora
    /// </summary>
    /// <param name="printerName">Nome da impressora</param>
    /// <returns>Informações da impressora ou null se não existir</returns>
    PrinterInfo? GetPrinterInfo(string printerName);

    /// <summary>
    /// Obtém informações detalhadas de todas as impressoras
    /// </summary>
    /// <returns>Array com informações de todas as impressoras</returns>
    PrinterInfo[] GetAllPrintersInfo();
}
