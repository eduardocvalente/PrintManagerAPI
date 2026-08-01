namespace PrintManagerAPI.API.Models;

public class PrintSettings
{
    public string FontName { get; set; } = "Arial";
    public int FontSize { get; set; } = 12;
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public PaperSize PaperSize { get; set; } = PaperSize.A4;
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
    public PrintMargins Margins { get; set; } = new();
    public PrintColor TextColor { get; set; } = new();
    public bool FitToPage { get; set; } = true;
}

public class PrintColor
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }

    public System.Drawing.Color ToColor()
    {
        // Clamp defensivo: a validação de entrada acontece na borda da API,
        // mas jobs podem ser criados por código interno sem passar por ela.
        return System.Drawing.Color.FromArgb(
            Math.Clamp(R, 0, 255),
            Math.Clamp(G, 0, 255),
            Math.Clamp(B, 0, 255));
    }
}

public enum TextAlignment
{
    Left = 0,
    Center = 1,
    Right = 2
}

public enum PaperSize
{
    A4 = 0,
    A3 = 1,
    A5 = 2,
    Letter = 3,
    Legal = 4,
    Thermal58mm = 6,
    Thermal80mm = 7
}

public enum PageOrientation
{
    Portrait = 0,
    Landscape = 1
}

public class PrintMargins
{
    public int Top { get; set; } = 50;
    public int Bottom { get; set; } = 50;
    public int Left { get; set; } = 50;
    public int Right { get; set; } = 50;
}
