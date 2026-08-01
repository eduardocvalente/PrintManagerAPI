namespace PrintManagerAPI.API.Models;

public enum PrintJobState
{
    Queued = 0,
    Printing = 1,
    Completed = 2,
    Failed = 3
}

public class PrintJobStatusInfo
{
    public Guid Id { get; init; }
    public string PrinterName { get; init; } = string.Empty;
    public PrintJobState State { get; set; }
    public DateTimeOffset EnqueuedAt { get; init; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string? Error { get; set; }
}
