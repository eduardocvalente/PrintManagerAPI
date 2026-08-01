using System.Drawing;
using System.Drawing.Printing;
using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Models;

namespace PrintManagerAPI.API.Services;

public class PrintJobProcessor : IPrintJobProcessor
{
    private const int MinFontSize = 4;
    private const int MaxFontSize = 200;

    private readonly ILogger<PrintJobProcessor> _logger;
    private readonly IPrinterDiscoveryService _printerDiscoveryService;

    public PrintJobProcessor(ILogger<PrintJobProcessor> logger, IPrinterDiscoveryService printerDiscoveryService)
    {
        _logger = logger;
        _printerDiscoveryService = printerDiscoveryService;
    }

    public async Task ProcessJobAsync(PrintJob printJob, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Iniciando o processamento do trabalho de impressão {JobId} para a impressora {PrinterName}",
            printJob.Id, printJob.PrinterName);

        var printerInfo = _printerDiscoveryService.GetPrinterInfo(printJob.PrinterName);
        if (printerInfo?.IsValid != true)
        {
            throw new InvalidOperationException(
                $"A impressora '{printJob.PrinterName}' não está disponível ou não é válida.");
        }

        await ExecutePrintJobAsync(printJob);
    }

    private async Task ExecutePrintJobAsync(PrintJob printJob)
    {
        using var printDocument = new PrintDocument();
        printDocument.PrinterSettings.PrinterName = printJob.PrinterName;

        if (!printDocument.PrinterSettings.IsValid)
        {
            throw new InvalidOperationException(
                $"Impressora '{printJob.PrinterName}' não está disponível ou não é válida.");
        }

        ConfigurePaperSize(printDocument, printJob.Settings);
        printDocument.DefaultPageSettings.Landscape = printJob.Settings.Orientation == PageOrientation.Landscape;
        printDocument.DefaultPageSettings.Margins = ValidateMargins(printJob.Settings.Margins);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        printDocument.PrintPage += (sender, e) =>
        {
            try
            {
                if (e.Graphics is not null)
                {
                    _logger.LogDebug("Renderizando página para impressão. Área útil: {Width}x{Height}",
                        e.MarginBounds.Width, e.MarginBounds.Height);

                    RenderTextToPaper(e.Graphics, e.MarginBounds, printJob.Text, printJob.Settings);
                }

                // Limitação conhecida: o texto é renderizado em uma única página;
                // conteúdo que exceda a área útil não gera páginas adicionais.
                e.HasMorePages = false;
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante a renderização da página de impressão para o trabalho {JobId}",
                    printJob.Id);
                tcs.TrySetException(ex);
            }
        };

        printDocument.EndPrint += (sender, e) => tcs.TrySetResult(true);

        printDocument.BeginPrint += (sender, e) =>
        {
            _logger.LogInformation("Iniciando impressão do trabalho {JobId} na impressora {PrinterName}",
                printJob.Id, printJob.PrinterName);
        };

        printDocument.Print();
        await tcs.Task;

        _logger.LogInformation("Impressão do trabalho {JobId} concluída com sucesso", printJob.Id);
    }

    private static Margins ValidateMargins(PrintMargins printMargins)
    {
        // Margens em centésimos de polegada, limitadas a 2 polegadas.
        var top = Math.Clamp(printMargins.Top, 0, 200);
        var bottom = Math.Clamp(printMargins.Bottom, 0, 200);
        var left = Math.Clamp(printMargins.Left, 0, 200);
        var right = Math.Clamp(printMargins.Right, 0, 200);

        return new Margins(left, right, top, bottom);
    }

    private static void ConfigurePaperSize(PrintDocument printDocument, PrintSettings settings)
    {
        var paperSize = settings.PaperSize switch
        {
            Models.PaperSize.A4 => new System.Drawing.Printing.PaperSize("A4", 827, 1169),
            Models.PaperSize.A3 => new System.Drawing.Printing.PaperSize("A3", 1169, 1654),
            Models.PaperSize.A5 => new System.Drawing.Printing.PaperSize("A5", 583, 827),
            Models.PaperSize.Letter => new System.Drawing.Printing.PaperSize("Letter", 850, 1100),
            Models.PaperSize.Legal => new System.Drawing.Printing.PaperSize("Legal", 850, 1400),
            Models.PaperSize.Thermal58mm => new System.Drawing.Printing.PaperSize("Thermal 58mm", 220, 3276),
            Models.PaperSize.Thermal80mm => new System.Drawing.Printing.PaperSize("Thermal 80mm", 315, 3276),
            _ => new System.Drawing.Printing.PaperSize("A4", 827, 1169)
        };

        printDocument.DefaultPageSettings.PaperSize = paperSize;
    }

    private static void RenderTextToPaper(Graphics graphics, Rectangle marginBounds, string text, PrintSettings settings)
    {
        var fontStyle = FontStyle.Regular;
        if (settings.Bold) fontStyle |= FontStyle.Bold;
        if (settings.Italic) fontStyle |= FontStyle.Italic;
        if (settings.Underline) fontStyle |= FontStyle.Underline;

        var fontSize = Math.Clamp(settings.FontSize, MinFontSize, MaxFontSize);

        using var font = new Font(settings.FontName, fontSize, fontStyle);
        using var brush = new SolidBrush(settings.TextColor.ToColor());
        using var stringFormat = new StringFormat();

        stringFormat.Alignment = settings.Alignment switch
        {
            TextAlignment.Center => StringAlignment.Center,
            TextAlignment.Right => StringAlignment.Far,
            _ => StringAlignment.Near
        };

        var isThermal = settings.PaperSize is Models.PaperSize.Thermal58mm or Models.PaperSize.Thermal80mm;
        if (isThermal)
        {
            stringFormat.FormatFlags = StringFormatFlags.LineLimit;
            stringFormat.Trimming = StringTrimming.Word;
        }

        var textRectangle = new RectangleF(marginBounds.X, marginBounds.Y, marginBounds.Width, marginBounds.Height);

        if (settings.FitToPage)
        {
            var textSize = graphics.MeasureString(text, font);
            if (textSize.Width > marginBounds.Width)
            {
                var scaleX = marginBounds.Width / textSize.Width;
                var newFontSize = fontSize * scaleX;

                if (isThermal)
                {
                    newFontSize = Math.Max(newFontSize, 6);
                }

                using var scaledFont = new Font(settings.FontName, Math.Max(newFontSize, MinFontSize), fontStyle);
                graphics.DrawString(text, scaledFont, brush, textRectangle, stringFormat);
                return;
            }
        }

        graphics.DrawString(text, font, brush, textRectangle, stringFormat);
    }
}
