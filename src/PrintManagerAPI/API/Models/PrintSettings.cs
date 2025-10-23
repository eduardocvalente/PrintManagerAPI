namespace PrintManagerAPI.API.Models;

public class PrintSettings
{
    public string FontName { get; set; } = "Arial";
    public int FontSize { get; set; } = 12;
    public FontStyle FontStyle { get; set; } = FontStyle.Regular;
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    public bool Bold { get; set; } = false;
    public bool Italic { get; set; } = false;
    public bool Underline { get; set; } = false;
    public PaperSize PaperSize { get; set; } = PaperSize.A4;
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
    public PrintMargins Margins { get; set; } = new();
    public PrintColor TextColor { get; set; } = new();
    public double LineSpacing { get; set; } = 1.0;
    public bool FitToPage { get; set; } = true;
    public bool WrapText { get; set; } = true;
    public int MaxLinesPerPage { get; set; } = 0;
}

public class PrintColor
{
    public int R { get; set; } = 0;
    public int G { get; set; } = 0;
    public int B { get; set; } = 0;

    public System.Drawing.Color ToColor()
    {
        return System.Drawing.Color.FromArgb(R, G, B);
    }
}

public enum TextAlignment
{
    Left = 0,
    Center = 1,
    Right = 2,
    Justify = 3
}

public enum PaperSize
{
    A4 = 0,
    A3 = 1,
    A5 = 2,
    Letter = 3,
    Legal = 4,
    Custom = 5,
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
