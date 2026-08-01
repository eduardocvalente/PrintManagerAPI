using Microsoft.Extensions.Options;
using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Models;
using PrintManagerAPI.API.Validation;
using Swashbuckle.AspNetCore.Annotations;

namespace PrintManagerAPI.Configuration;

public static class EndpointConfiguration
{
    /// <summary>
    /// Mapeia todos os endpoints da API. Erros inesperados são tratados pelo
    /// exception handler global (ProblemDetails), sem try/catch por handler.
    /// </summary>
    public static WebApplication MapPrintQueueEndpoints(this WebApplication app)
    {
        var printersGroup = app.MapGroup("/printers")
            .WithTags("Printers");

        printersGroup.MapGet("", GetPrinters)
            .WithName("GetPrinters")
            .WithMetadata(new SwaggerOperationAttribute(
                "List Printers",
                "Retorna todas as impressoras instaladas no sistema"));

        printersGroup.MapGet("/detailed", GetPrintersDetailed)
            .WithName("GetPrintersDetailed")
            .WithMetadata(new SwaggerOperationAttribute(
                "List Printers Detailed",
                "Retorna todas as impressoras com informações detalhadas"));

        printersGroup.MapGet("/{name}/info", GetPrinterInfo)
            .WithName("GetPrinterInfo")
            .WithMetadata(new SwaggerOperationAttribute(
                "Get Printer Info",
                "Retorna informações detalhadas de uma impressora específica"));

        var printJobsGroup = app.MapGroup("/print")
            .WithTags("Print Jobs");

        // Rate limit apenas na submissão; consultas de status (polling) ficam fora da cota.
        printJobsGroup.MapPost("", SubmitPrintJob)
            .RequireRateLimiting("print")
            .WithName("SubmitPrintJob")
            .WithMetadata(new SwaggerOperationAttribute(
                "Submit Print Job",
                "Envia um trabalho de impressão para a fila. Retorna 202 com o ID do job."));

        // Mantido por compatibilidade: o payload é idêntico ao de POST /print.
        printJobsGroup.MapPost("/advanced", SubmitPrintJob)
            .RequireRateLimiting("print")
            .WithName("SubmitAdvancedPrintJob")
            .WithMetadata(new SwaggerOperationAttribute(
                "Submit Print Job (alias obsoleto)",
                "Alias de POST /print mantido por compatibilidade. Prefira POST /print."));

        printJobsGroup.MapGet("/{jobId:guid}", GetJobStatus)
            .WithName("GetPrintJobStatus")
            .WithMetadata(new SwaggerOperationAttribute(
                "Get Print Job Status",
                "Retorna o status de um trabalho de impressão (Queued, Printing, Completed ou Failed)"));

        app.MapGet("/queue/status", GetQueueStatus)
            .WithName("GetQueueStatus")
            .WithTags("Queue Management")
            .WithMetadata(new SwaggerOperationAttribute(
                "Get Queue Status",
                "Retorna informações sobre o status atual da fila de impressão"));

        return app;
    }

    private static IResult GetPrinters(IPrintService printService)
    {
        var printers = printService.GetAvailablePrinters();
        return Results.Ok(new
        {
            success = true,
            count = printers.Length,
            printers
        });
    }

    private static IResult GetPrintersDetailed(IPrinterDiscoveryService printerService)
    {
        var printers = printerService.GetAllPrintersInfo();
        return Results.Ok(new
        {
            success = true,
            count = printers.Length,
            printers
        });
    }

    private static IResult GetPrinterInfo(string name, IPrinterDiscoveryService printerService)
    {
        var printerInfo = printerService.GetPrinterInfo(name);
        if (printerInfo is null)
        {
            return Results.Problem(
                title: "Impressora não encontrada",
                detail: $"Impressora '{name}' não encontrada.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(new
        {
            success = true,
            printer = printerInfo
        });
    }

    private static IResult SubmitPrintJob(
        PrintRequest request,
        IPrintService printService,
        IOptions<PrintQueueOptions> options)
    {
        var errors = PrintRequestValidator.Validate(request, options.Value);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var availablePrinters = printService.GetAvailablePrinters();
        if (!availablePrinters.Contains(request.PrinterName))
        {
            return Results.Problem(
                title: "Impressora não encontrada",
                detail: $"Impressora '{request.PrinterName}' não encontrada.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?> { ["availablePrinters"] = availablePrinters });
        }

        var result = printService.EnqueuePrintJob(request.PrinterName, request.Text, request.Settings);
        if (!result.Accepted)
        {
            return Results.Problem(
                title: "Fila de impressão cheia",
                detail: result.Reason,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Accepted($"/print/{result.JobId}", new
        {
            success = true,
            message = "Trabalho de impressão adicionado à fila com sucesso",
            jobId = result.JobId,
            printerName = request.PrinterName,
            statusUrl = $"/print/{result.JobId}",
            queueStatus = printService.GetQueueStatus()
        });
    }

    private static IResult GetJobStatus(Guid jobId, IPrintService printService)
    {
        var status = printService.GetJobStatus(jobId);
        if (status is null)
        {
            return Results.Problem(
                title: "Trabalho não encontrado",
                detail: $"Trabalho '{jobId}' não existe ou já saiu do histórico de rastreamento.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(new
        {
            success = true,
            job = status
        });
    }

    private static IResult GetQueueStatus(IPrintService printService)
    {
        return Results.Ok(new
        {
            success = true,
            status = printService.GetQueueStatus()
        });
    }
}
