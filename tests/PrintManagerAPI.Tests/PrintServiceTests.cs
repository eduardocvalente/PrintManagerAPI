using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PrintManagerAPI.API.Services;
using PrintManagerAPI.Configuration;
using Xunit;

namespace PrintManagerAPI.Tests;

public class PrintServiceTests
{
    private static PrintService CreateService(PrintQueueOptions? options = null)
    {
        return new PrintService(
            NullLogger<PrintService>.Instance,
            new FakePrinterDiscoveryService(),
            Options.Create(options ?? new PrintQueueOptions()));
    }

    [Fact]
    public void Enqueue_alem_da_capacidade_e_recusado()
    {
        var service = CreateService(new PrintQueueOptions { MaxQueueLength = 2 });

        Assert.True(service.EnqueuePrintJob("X", "1").Accepted);
        Assert.True(service.EnqueuePrintJob("X", "2").Accepted);

        var rejected = service.EnqueuePrintJob("X", "3");
        Assert.False(rejected.Accepted);
        Assert.NotNull(rejected.Reason);
        Assert.Equal(2, service.GetQueueStatus().PendingJobs);
        // O job recusado não deve ficar rastreado como pendente.
        Assert.Null(service.GetJobStatus(rejected.JobId));
    }

    [Fact]
    public void GetJobStatus_de_job_desconhecido_retorna_null()
    {
        var service = CreateService();
        Assert.Null(service.GetJobStatus(Guid.NewGuid()));
    }

    [Fact]
    public void GetQueueStatus_reflete_jobs_pendentes()
    {
        var service = CreateService();
        Assert.Equal(0, service.GetQueueStatus().PendingJobs);

        service.EnqueuePrintJob("X", "1");
        service.EnqueuePrintJob("X", "2");

        var status = service.GetQueueStatus();
        Assert.Equal(2, status.PendingJobs);
        Assert.False(status.IsProcessing);
        Assert.Null(status.CurrentJobId);
    }

    [Fact]
    public void GetAvailablePrinters_delega_para_o_servico_de_descoberta()
    {
        var service = CreateService();
        var printers = service.GetAvailablePrinters();
        Assert.Single(printers);
        Assert.Equal("Impressora de Teste", printers[0]);
    }
}
