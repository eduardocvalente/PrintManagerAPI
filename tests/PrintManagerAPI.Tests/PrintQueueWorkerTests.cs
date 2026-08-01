using PrintManagerAPI.API.Models;
using PrintManagerAPI.Configuration;
using Xunit;

namespace PrintManagerAPI.Tests;

public class PrintQueueWorkerTests
{
    [Fact]
    public async Task Jobs_enfileirados_sequencialmente_sao_processados_em_ordem_FIFO()
    {
        var processor = new FakePrintJobProcessor { Delay = TimeSpan.FromMilliseconds(5) };
        await using var harness = new PrintQueueTestHarness(processor);
        await harness.StartAsync();

        var jobIds = new List<Guid>();
        for (var i = 0; i < 10; i++)
        {
            var result = harness.Service.EnqueuePrintJob("Impressora de Teste", $"documento {i}");
            Assert.True(result.Accepted);
            jobIds.Add(result.JobId);
        }

        await PrintQueueTestHarness.WaitUntilAsync(() => processor.ProcessedOrder.Count == 10);

        Assert.Equal(jobIds, processor.ProcessedOrder);
    }

    [Fact]
    public async Task Enfileiramento_concorrente_nunca_processa_mais_de_um_job_por_vez()
    {
        var processor = new FakePrintJobProcessor { Delay = TimeSpan.FromMilliseconds(10) };
        await using var harness = new PrintQueueTestHarness(processor);
        await harness.StartAsync();

        const int producers = 8;
        const int jobsPerProducer = 5;

        var tasks = Enumerable.Range(0, producers).Select(p => Task.Run(() =>
        {
            for (var i = 0; i < jobsPerProducer; i++)
            {
                var result = harness.Service.EnqueuePrintJob("Impressora de Teste", $"produtor {p} job {i}");
                Assert.True(result.Accepted);
            }
        }));

        await Task.WhenAll(tasks);
        await PrintQueueTestHarness.WaitUntilAsync(
            () => processor.ProcessedOrder.Count == producers * jobsPerProducer, 15_000);

        Assert.Equal(1, processor.MaxObservedConcurrency);
        Assert.Equal(producers * jobsPerProducer, processor.ProcessedOrder.Count);
    }

    [Fact]
    public async Task Nenhum_job_e_perdido_mesmo_com_enfileiramento_durante_o_processamento()
    {
        // Regressão da race condition original: um job enfileirado no instante em que
        // o worker terminava a fila ficava parado até o próximo enqueue.
        var processor = new FakePrintJobProcessor();
        await using var harness = new PrintQueueTestHarness(processor);
        await harness.StartAsync();

        const int total = 200;
        var accepted = 0;
        for (var i = 0; i < total; i++)
        {
            var result = harness.Service.EnqueuePrintJob("Impressora de Teste", $"job {i}");
            if (result.Accepted) accepted++;
            if (i % 10 == 0)
                await Task.Delay(1);
        }

        await PrintQueueTestHarness.WaitUntilAsync(() => processor.ProcessedOrder.Count == accepted, 15_000);

        Assert.Equal(accepted, processor.ProcessedOrder.Count);
        var status = harness.Service.GetQueueStatus();
        Assert.Equal(0, status.PendingJobs);
    }

    [Fact]
    public async Task Job_com_falha_e_marcado_como_Failed_e_a_fila_continua()
    {
        var processor = new FakePrintJobProcessor
        {
            FailWith = job => job.Text.Contains("falhar") ? new InvalidOperationException("Impressora explodiu") : null
        };
        await using var harness = new PrintQueueTestHarness(processor);
        await harness.StartAsync();

        var bad = harness.Service.EnqueuePrintJob("Impressora de Teste", "vai falhar");
        var good = harness.Service.EnqueuePrintJob("Impressora de Teste", "vai funcionar");

        await PrintQueueTestHarness.WaitUntilAsync(() =>
            harness.Service.GetJobStatus(good.JobId)?.State == PrintJobState.Completed);

        var badStatus = harness.Service.GetJobStatus(bad.JobId);
        Assert.NotNull(badStatus);
        Assert.Equal(PrintJobState.Failed, badStatus!.State);
        Assert.Contains("explodiu", badStatus.Error);
        Assert.NotNull(badStatus.FinishedAt);
    }

    [Fact]
    public async Task Job_que_excede_o_timeout_e_marcado_como_Failed()
    {
        var options = new PrintQueueOptions { MaxQueueLength = 10, PrintJobTimeoutSeconds = 1 };
        var processor = new FakePrintJobProcessor { Delay = TimeSpan.FromSeconds(30) };
        await using var harness = new PrintQueueTestHarness(processor, options);
        await harness.StartAsync();

        var result = harness.Service.EnqueuePrintJob("Impressora de Teste", "documento lento");

        await PrintQueueTestHarness.WaitUntilAsync(() =>
            harness.Service.GetJobStatus(result.JobId)?.State == PrintJobState.Failed, 10_000);

        var status = harness.Service.GetJobStatus(result.JobId);
        Assert.Contains("Tempo limite", status!.Error);
    }

    [Fact]
    public async Task Status_transita_de_Queued_para_Completed()
    {
        var processor = new FakePrintJobProcessor { Delay = TimeSpan.FromMilliseconds(50) };
        await using var harness = new PrintQueueTestHarness(processor);

        var result = harness.Service.EnqueuePrintJob("Impressora de Teste", "documento");
        Assert.Equal(PrintJobState.Queued, harness.Service.GetJobStatus(result.JobId)!.State);

        await harness.StartAsync();
        await PrintQueueTestHarness.WaitUntilAsync(() =>
            harness.Service.GetJobStatus(result.JobId)?.State == PrintJobState.Completed);

        var status = harness.Service.GetJobStatus(result.JobId)!;
        Assert.NotNull(status.StartedAt);
        Assert.NotNull(status.FinishedAt);
        Assert.Null(status.Error);
    }
}
