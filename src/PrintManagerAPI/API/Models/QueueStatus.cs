namespace PrintManagerAPI.API.Models;

public class QueueStatus
{
    public int PendingJobs { get; set; }
    public bool IsProcessing { get; set; }
    public Guid? CurrentJobId { get; set; }
}
