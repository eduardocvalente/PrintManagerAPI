namespace PrintManagerAPI.API.Models;

public class PrintRequest
{
    public string PrinterName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public PrintSettings Settings { get; set; } = new();
}
