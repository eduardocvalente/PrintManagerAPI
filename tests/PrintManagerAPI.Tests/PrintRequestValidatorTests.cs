using PrintManagerAPI.API.Models;
using PrintManagerAPI.API.Validation;
using PrintManagerAPI.Configuration;
using Xunit;

namespace PrintManagerAPI.Tests;

public class PrintRequestValidatorTests
{
    private static readonly PrintQueueOptions Options = new() { MaxTextLength = 100 };

    private static PrintRequest ValidRequest() => new()
    {
        PrinterName = "Impressora de Teste",
        Text = "conteúdo",
        Settings = new PrintSettings()
    };

    [Fact]
    public void Requisicao_valida_nao_gera_erros()
    {
        Assert.Empty(PrintRequestValidator.Validate(ValidRequest(), Options));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Nome_de_impressora_vazio_e_invalido(string printerName)
    {
        var request = ValidRequest();
        request.PrinterName = printerName;

        var errors = PrintRequestValidator.Validate(request, Options);
        Assert.Contains("printerName", errors.Keys);
    }

    [Fact]
    public void Texto_vazio_e_invalido()
    {
        var request = ValidRequest();
        request.Text = "";

        Assert.Contains("text", PrintRequestValidator.Validate(request, Options).Keys);
    }

    [Fact]
    public void Texto_acima_do_limite_configurado_e_invalido()
    {
        var request = ValidRequest();
        request.Text = new string('x', Options.MaxTextLength + 1);

        Assert.Contains("text", PrintRequestValidator.Validate(request, Options).Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(201)]
    [InlineData(-5)]
    public void Tamanho_de_fonte_fora_da_faixa_e_invalido(int fontSize)
    {
        var request = ValidRequest();
        request.Settings.FontSize = fontSize;

        Assert.Contains("settings.fontSize", PrintRequestValidator.Validate(request, Options).Keys);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, 256, 0)]
    [InlineData(0, 0, 999)]
    public void Componentes_de_cor_fora_de_0_a_255_sao_invalidos(int r, int g, int b)
    {
        var request = ValidRequest();
        request.Settings.TextColor = new PrintColor { R = r, G = g, B = b };

        Assert.Contains("settings.textColor", PrintRequestValidator.Validate(request, Options).Keys);
    }

    [Fact]
    public void Nome_de_fonte_vazio_e_invalido()
    {
        var request = ValidRequest();
        request.Settings.FontName = " ";

        Assert.Contains("settings.fontName", PrintRequestValidator.Validate(request, Options).Keys);
    }
}
