using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Models;
using System.Drawing.Printing;

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
            var printers = new List<string>();

            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printerName);
            }

            _logger.LogInformation("Foram detectadas {Count} impressoras instaladas", printers.Count);

            return printers.ToArray();
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
                IsOnline = printerSettings.IsValid,
                IsDefault = printerSettings.IsDefaultPrinter,
                IsNetworkPrinter = printerSettings.IsPlotter,
                CanDuplexing = printerSettings.CanDuplex,
                CanColor = printerSettings.SupportsColor,
                SupportedPaperSizes = printerSettings.PaperSizes.Cast<System.Drawing.Printing.PaperSize>().ToArray(),
                SupportedResolutions = printerSettings.PrinterResolutions.Cast<PrinterResolution>().ToArray(),
                Status = printerSettings.IsValid ? "Online" : "Offline",
                MaximumPage = printerSettings.MaximumPage,
                MinimumPage = printerSettings.MinimumPage,
                SupportsColor = printerSettings.SupportsColor
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao obter informações da impressora {PrinterName}", printerName);
            return new PrinterInfo
            {
                Name = printerName,
                IsOnline = false,
                IsValid = false,
                Status = "Error"
            };
        }
    }

    public PrinterInfo[] GetAllPrintersInfo()
    {
        var printers = new List<PrinterInfo>();

        foreach (string printerName in PrinterSettings.InstalledPrinters)
        {
            var printerInfo = GetPrinterInfo(printerName);
            if (printerInfo != null)
            {
                printers.Add(printerInfo);
            }
        }

        return printers.ToArray();
    }
}
