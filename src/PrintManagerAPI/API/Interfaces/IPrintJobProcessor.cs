using PrintManagerAPI.API.Models;

namespace PrintManagerAPI.API.Interfaces;

public interface IPrintJobProcessor
{
    /// <summary>
    /// Processa um trabalho de impressão. Lança exceção quando a impressora
    /// é inválida ou a impressão falha — o chamador decide como registrar a falha.
    /// </summary>
    /// <param name="printJob">Trabalho a ser processado</param>
    /// <param name="cancellationToken">Token de cancelamento (encerramento da aplicação)</param>
    Task ProcessJobAsync(PrintJob printJob, CancellationToken cancellationToken = default);
}
