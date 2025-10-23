using System.Drawing.Printing;

namespace PrintManagerAPI.API.Models;

public class PrinterInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsValid { get; set; }
    public bool IsDefault { get; set; }
    public bool IsNetworkPrinter { get; set; }
    public bool CanDuplexing { get; set; }
    public bool CanColor { get; set; }
    public System.Drawing.Printing.PaperSize[] SupportedPaperSizes { get; set; } = Array.Empty<System.Drawing.Printing.PaperSize>();
    public PrinterResolution[] SupportedResolutions { get; set; } = Array.Empty<PrinterResolution>();
    public string Status { get; set; } = string.Empty;
    public int MaximumPage { get; set; }
    public int MinimumPage { get; set; }
    public bool SupportsColor { get; set; }
}
