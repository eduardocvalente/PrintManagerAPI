using System.ComponentModel.DataAnnotations;

namespace PrintManagerAPI.Configuration;

/// <summary>
/// Opções da fila de impressão, vinculadas à seção "PrintQueue" do appsettings
/// e validadas na inicialização da aplicação.
/// </summary>
public class PrintQueueOptions
{
    public const string SectionName = "PrintQueue";

    /// <summary>Capacidade máxima da fila; enfileiramentos além disso são recusados com 503.</summary>
    [Range(1, 10_000)]
    public int MaxQueueLength { get; set; } = 100;

    /// <summary>Tempo máximo de processamento de um job antes de ser marcado como falho.</summary>
    [Range(1, 3_600)]
    public int PrintJobTimeoutSeconds { get; set; } = 300;

    /// <summary>Tamanho máximo (em caracteres) do texto aceito por requisição.</summary>
    [Range(1, 1_000_000)]
    public int MaxTextLength { get; set; } = 20_000;

    /// <summary>Quantidade de jobs finalizados retidos em memória para consulta de status.</summary>
    [Range(10, 100_000)]
    public int MaxTrackedJobs { get; set; } = 1_000;

    /// <summary>
    /// Chave de API opcional. Quando definida, todas as rotas (exceto health checks)
    /// exigem o header X-Api-Key. Definir via variável de ambiente PrintQueue__ApiKey,
    /// nunca em arquivo versionado.
    /// </summary>
    public string? ApiKey { get; set; }

    public TimeSpan PrintJobTimeout => TimeSpan.FromSeconds(PrintJobTimeoutSeconds);
}
