using PrintManagerAPI.API.Models;
using PrintManagerAPI.Configuration;

namespace PrintManagerAPI.API.Validation;

public static class PrintRequestValidator
{
    public const int MaxPrinterNameLength = 256;
    public const int MaxFontNameLength = 64;
    public const int MinFontSize = 4;
    public const int MaxFontSize = 200;

    /// <summary>
    /// Valida uma requisição de impressão. Retorna um dicionário de erros por campo,
    /// vazio quando a requisição é válida (formato compatível com Results.ValidationProblem).
    /// </summary>
    public static Dictionary<string, string[]> Validate(PrintRequest request, PrintQueueOptions options)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.PrinterName))
            errors["printerName"] = new[] { "Nome da impressora é obrigatório." };
        else if (request.PrinterName.Length > MaxPrinterNameLength)
            errors["printerName"] = new[] { $"Nome da impressora não pode exceder {MaxPrinterNameLength} caracteres." };

        if (string.IsNullOrWhiteSpace(request.Text))
            errors["text"] = new[] { "Texto a ser impresso é obrigatório." };
        else if (request.Text.Length > options.MaxTextLength)
            errors["text"] = new[] { $"Texto excede o limite de {options.MaxTextLength} caracteres." };

        var settings = request.Settings;

        if (string.IsNullOrWhiteSpace(settings.FontName))
            errors["settings.fontName"] = new[] { "Nome da fonte é obrigatório." };
        else if (settings.FontName.Length > MaxFontNameLength)
            errors["settings.fontName"] = new[] { $"Nome da fonte não pode exceder {MaxFontNameLength} caracteres." };

        if (settings.FontSize is < MinFontSize or > MaxFontSize)
            errors["settings.fontSize"] = new[] { $"Tamanho da fonte deve estar entre {MinFontSize} e {MaxFontSize}." };

        if (settings.TextColor.R is < 0 or > 255 ||
            settings.TextColor.G is < 0 or > 255 ||
            settings.TextColor.B is < 0 or > 255)
        {
            errors["settings.textColor"] = new[] { "Componentes de cor (R, G, B) devem estar entre 0 e 255." };
        }

        return errors;
    }
}
