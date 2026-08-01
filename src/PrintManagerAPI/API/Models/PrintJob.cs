namespace PrintManagerAPI.API.Models;

public class PrintJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PrinterName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public PrintSettings Settings { get; set; } = new();

    public PrintJob()
    {
    }

    public PrintJob(string printerName, string text, PrintSettings? settings = null)
    {
        PrinterName = printerName;
        Text = text;
        Settings = settings ?? new PrintSettings();
    }
}
