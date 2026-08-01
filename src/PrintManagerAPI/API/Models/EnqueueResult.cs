namespace PrintManagerAPI.API.Models;

/// <summary>
/// Resultado de uma tentativa de enfileiramento. <see cref="Accepted"/> é falso
/// quando a fila atingiu a capacidade máxima configurada.
/// </summary>
public record EnqueueResult(bool Accepted, Guid JobId, string? Reason = null);
