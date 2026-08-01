using Microsoft.Extensions.Diagnostics.HealthChecks;
using PrintManagerAPI.API.Interfaces;

namespace PrintManagerAPI.Configuration;

/// <summary>
/// Verifica se o subsistema de impressão responde e se há impressoras instaladas.
/// Falha de comunicação com o spooler → Unhealthy; nenhuma impressora → Degraded.
/// </summary>
public class PrinterHealthCheck : IHealthCheck
{
    private readonly IPrinterDiscoveryService _printerDiscoveryService;

    public PrinterHealthCheck(IPrinterDiscoveryService printerDiscoveryService)
    {
        _printerDiscoveryService = printerDiscoveryService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var printers = _printerDiscoveryService.DiscoverPrinters();
            var data = new Dictionary<string, object> { ["installedPrinters"] = printers.Length };

            return Task.FromResult(printers.Length > 0
                ? HealthCheckResult.Healthy($"{printers.Length} impressora(s) instalada(s).", data)
                : HealthCheckResult.Degraded("Nenhuma impressora instalada no sistema.", data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Falha ao consultar o subsistema de impressão.", ex));
        }
    }
}
