namespace PrintManagerAPI.API.Models;

public class PrinterInfo
{
    public string Name { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool IsDefault { get; set; }
    public bool IsNetworkPrinter { get; set; }
    public bool CanDuplex { get; set; }
    public bool SupportsColor { get; set; }
    public string[] SupportedPaperSizes { get; set; } = Array.Empty<string>();
    public string[] SupportedResolutions { get; set; } = Array.Empty<string>();
    public string Status { get; set; } = string.Empty;
    public int MaximumPage { get; set; }
    public int MinimumPage { get; set; }
}
