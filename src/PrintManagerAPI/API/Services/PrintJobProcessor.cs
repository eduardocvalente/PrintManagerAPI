using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Models;
using System.Drawing.Printing;

namespace PrintManagerAPI.API.Services;

public class PrintJobProcessor : IPrintJobProcessor
{
    private readonly ILogger<PrintJobProcessor> _logger;
    private readonly IPrinterDiscoveryService _printerDiscoveryService;

    public PrintJobProcessor(ILogger<PrintJobProcessor> logger, IPrinterDiscoveryService printerDiscoveryService)
    {
        _logger = logger;
        _printerDiscoveryService = printerDiscoveryService;
    }

    public async Task ProcessJobAsync(PrintJob printJob)
    {
        try
        {
            _logger.LogInformation("Iniciando o processamento do trabalho de impress�o {JobId} para a impressora {PrinterName}", printJob.Id, printJob.PrinterName);

            if (!ValidatePrinter(printJob.PrinterName))
            {
                _logger.LogError("A impressora {PrinterName} n�o � v�lida para o trabalho {JobId}", printJob.PrinterName, printJob.Id);
                return;
            }

            await ExecutePrintJobAsync(printJob);

            _logger.LogInformation("Trabalho de impress�o {JobId} conclu�do com sucesso", printJob.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar o trabalho de impress�o {JobId}", printJob.Id);
            throw;
        }
    }

    public bool ValidatePrinter(string printerName)
    {
        var printerInfo = _printerDiscoveryService.GetPrinterInfo(printerName);
        return printerInfo?.IsValid == true;
    }

    private async Task ExecutePrintJobAsync(PrintJob printJob)
    {
        try
        {
            using var printDocument = new PrintDocument();
            printDocument.PrinterSettings.PrinterName = printJob.PrinterName;

            // Verificar se a impressora existe
            if (!printDocument.PrinterSettings.IsValid)
            {
                throw new InvalidOperationException($"Impressora '{printJob.PrinterName}' n�o est� dispon�vel ou n�o � v�lida.");
            }

            // Configurar tamanho do papel com valida��o
            ConfigurePaperSize(printDocument, printJob.Settings);

            // Configurar orienta��o
            printDocument.DefaultPageSettings.Landscape = printJob.Settings.Orientation == PageOrientation.Landscape;

            // Configurar margens com valida��o
            var margins = ValidateMargins(printJob.Settings.Margins);
            printDocument.DefaultPageSettings.Margins = margins;

            var tcs = new TaskCompletionSource<bool>();
            var hasError = false;

            printDocument.PrintPage += (sender, e) =>
            {
                try
                {
                    if (e.Graphics != null)
                    {
                        _logger.LogDebug("Renderizando página para impressão. Área útil: {Width}x{Height}",
                        e.MarginBounds.Width, e.MarginBounds.Height);

                        RenderTextToPaper(e.Graphics, e.MarginBounds, printJob.Text, printJob.Settings);
                    }

                    e.HasMorePages = false;
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante a renderiza��o da p�gina de impress�o para o trabalho {JobId}", printJob.Id);
                    hasError = true;
                    tcs.SetException(ex);
                }
            };

            printDocument.EndPrint += (sender, e) =>
            {
                if (!hasError && !tcs.Task.IsCompleted)
                {
                    tcs.SetResult(true);
                }
            };

            printDocument.BeginPrint += (sender, e) =>
            {
                _logger.LogInformation("Iniciando impress�o do trabalho {JobId} na impressora {PrinterName}",
                    printJob.Id, printJob.PrinterName);
            };

            printDocument.Print();
            await tcs.Task;

            _logger.LogInformation("Impress�o do trabalho {JobId} conclu�da com sucesso", printJob.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar trabalho de impress�o {JobId}", printJob.Id);
            throw;
        }
    }

    private static Margins ValidateMargins(PrintMargins printMargins)
    {
        // Validar e ajustar margens para evitar valores inv�lidos
        var top = Math.Max(0, Math.Min(printMargins.Top, 200));
        var bottom = Math.Max(0, Math.Min(printMargins.Bottom, 200));
        var left = Math.Max(0, Math.Min(printMargins.Left, 200));
        var right = Math.Max(0, Math.Min(printMargins.Right, 200));

        return new Margins(left, right, top, bottom);
    }

    private static void ConfigurePaperSize(PrintDocument printDocument, PrintSettings settings)
    {
        var paperSize = settings.PaperSize switch
        {
            Models.PaperSize.A4 => new System.Drawing.Printing.PaperSize("A4", 827, 1169),
            Models.PaperSize.A3 => new System.Drawing.Printing.PaperSize("A3", 1169, 1654),
            Models.PaperSize.Letter => new System.Drawing.Printing.PaperSize("Letter", 850, 1100),
            Models.PaperSize.Legal => new System.Drawing.Printing.PaperSize("Legal", 850, 1400),
            Models.PaperSize.Thermal58mm => new System.Drawing.Printing.PaperSize("Thermal 58mm", 220, 3276),
            Models.PaperSize.Thermal80mm => new System.Drawing.Printing.PaperSize("Thermal 80mm", 315, 3276),
            _ => new System.Drawing.Printing.PaperSize("A4", 827, 1169)
        };

        printDocument.DefaultPageSettings.PaperSize = paperSize;
    }

    private void RenderTextToPaper(Graphics graphics, Rectangle marginBounds, string text, PrintSettings settings)
    {
        var fontStyle = FontStyle.Regular;
        if (settings.Bold) fontStyle |= FontStyle.Bold;
        if (settings.Italic) fontStyle |= FontStyle.Italic;
        if (settings.Underline) fontStyle |= FontStyle.Underline;

        using var font = new Font(settings.FontName, settings.FontSize, fontStyle);
        using var brush = new SolidBrush(settings.TextColor.ToColor());

        var stringFormat = new StringFormat();
        stringFormat.Alignment = settings.Alignment switch
        {
            TextAlignment.Center => StringAlignment.Center,
            TextAlignment.Right => StringAlignment.Far,
            TextAlignment.Justify => StringAlignment.Center,
            _ => StringAlignment.Near
        };

        // Para impressoras térmicas, ajustar o texto para quebrar corretamente
        if (settings.PaperSize == Models.PaperSize.Thermal58mm || settings.PaperSize == Models.PaperSize.Thermal80mm)
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
                var newFontSize = settings.FontSize * scaleX;

                // Não reduzir muito a fonte para impressoras térmicas
                if (settings.PaperSize == Models.PaperSize.Thermal58mm || settings.PaperSize == Models.PaperSize.Thermal80mm)
                {
                    newFontSize = Math.Max(newFontSize, 6); // Tamanho mínimo 6
                }

                using var scaledFont = new Font(settings.FontName, newFontSize, fontStyle);
                graphics.DrawString(text, scaledFont, brush, textRectangle, stringFormat);
            }
            else
            {
                graphics.DrawString(text, font, brush, textRectangle, stringFormat);
            }
        }
        else
        {
            graphics.DrawString(text, font, brush, textRectangle, stringFormat);
        }
    }
}
