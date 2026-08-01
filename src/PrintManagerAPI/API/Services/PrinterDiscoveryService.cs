using System.Drawing.Printing;
using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Models;

namespace PrintManagerAPI.API.Services;

public class PrinterDiscoveryService : IPrinterDiscoveryService
{
    private readonly ILogger<PrinterDiscoveryService> _logger;

    public PrinterDiscoveryService(ILogger<PrinterDiscoveryService> logger)
    {
        _logger = logger;
    }

    public string[] DiscoverPrinters()
    {
        try
        {
            var printers = PrinterSettings.InstalledPrinters.Cast<string>().ToArray();

            _logger.LogInformation("Foram detectadas {Count} impressoras instaladas", printers.Length);

            return printers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao listar as impressoras instaladas");
            return Array.Empty<string>();
        }
    }

    public bool PrinterExists(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            return false;

        return PrinterSettings.InstalledPrinters.Cast<string>().Contains(printerName);
    }

    public PrinterInfo? GetPrinterInfo(string printerName)
    {
        if (!PrinterExists(printerName))
            return null;

        try
        {
            var printerSettings = new PrinterSettings { PrinterName = printerName };

            return new PrinterInfo
            {
                Name = printerName,
                IsValid = printerSettings.IsValid,
                IsDefault = printerSettings.IsDefaultPrinter,
                // Heurística: impressoras de rede são endereçadas por caminho UNC.
                // A API do spooler não expõe o status online real; para isso seria
                // necessário consultar WMI (Win32_Printer).
                IsNetworkPrinter = printerName.StartsWith(@"\\", StringComparison.Ordinal),
                CanDuplex = printerSettings.CanDuplex,
                SupportsColor = printerSettings.SupportsColor,
                SupportedPaperSizes = printerSettings.PaperSizes
                    .Cast<System.Drawing.Printing.PaperSize>()
                    .Select(p => p.PaperName)
                    .ToArray(),
                SupportedResolutions = printerSettings.PrinterResolutions
                    .Cast<PrinterResolution>()
                    .Select(r => r.Kind == PrinterResolutionKind.Custom ? $"{r.X}x{r.Y}" : r.Kind.ToString())
                    .ToArray(),
                Status = printerSettings.IsValid ? "Available" : "Unavailable",
                MaximumPage = printerSettings.MaximumPage,
                MinimumPage = printerSettings.MinimumPage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao obter informações da impressora {PrinterName}", printerName);
            return new PrinterInfo
            {
                Name = printerName,
                IsValid = false,
                Status = "Error"
            };
        }
    }

    public PrinterInfo[] GetAllPrintersInfo()
    {
        return DiscoverPrinters()
            .Select(GetPrinterInfo)
            .Where(info => info is not null)
            .Cast<PrinterInfo>()
            .ToArray();
    }
}
