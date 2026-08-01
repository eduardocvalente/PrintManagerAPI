using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace PrintManagerAPI.Configuration;

/// <summary>
/// Autenticação simples por chave de API. Ativa somente quando PrintQueue:ApiKey
/// está configurada (ex.: variável de ambiente PrintQueue__ApiKey). Health checks
/// permanecem públicos para permitir monitoramento externo.
/// </summary>
public class ApiKeyMiddleware
{
    public const string HeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly IOptions<PrintQueueOptions> _options;

    public ApiKeyMiddleware(RequestDelegate next, IOptions<PrintQueueOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var configuredKey = _options.Value.ApiKey;

        if (string.IsNullOrEmpty(configuredKey) ||
            context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var providedKey = context.Request.Headers[HeaderName].ToString();

        if (!string.IsNullOrEmpty(providedKey) && FixedTimeEquals(providedKey, configuredKey))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            title = "Não autorizado",
            status = 401,
            detail = $"Header {HeaderName} ausente ou inválido."
        });
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
