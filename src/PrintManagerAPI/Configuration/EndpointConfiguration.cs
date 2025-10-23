using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace PrintManagerAPI.Configuration;

public static class EndpointConfiguration
{
    /// <summary>
    /// Configura todos os endpoints da API
    /// </summary>
    /// <param name="app">Aplicação web</param>
    /// <param name="configureEndpoints">Lambda de configuração dos endpoints</param>
    /// <returns>Aplicação configurada</returns>
    public static WebApplication ConfigurePrintQueueEndpoints(this WebApplication app, Action<EndpointOptions>? configureEndpoints = null)
    {
        var options = new EndpointOptions();
        configureEndpoints?.Invoke(options);

        // Grupo de endpoints para impressoras
        var printersGroup = app.MapGroup("/printers")
            .WithTags("Printers");

        // Grupo de endpoints para jobs de impressão
        var printJobsGroup = app.MapGroup("/print")
            .WithTags("Print Jobs");

        // Configurar endpoints de impressoras
        ConfigurePrinterEndpoints(printersGroup, options);

        // Configurar endpoints de jobs
        ConfigurePrintJobEndpoints(printJobsGroup, options);

        // Endpoint de status da fila
        app.MapGet("/queue/status", GetQueueStatus)
            .WithName("GetQueueStatus")
            .WithTags("Queue Management")
            .WithMetadata(new SwaggerOperationAttribute(
                "Get Queue Status",
                "Retorna informações sobre o status atual da fila de impressão"));

        return app;
    }

    private static void ConfigurePrinterEndpoints(RouteGroupBuilder group, EndpointOptions options)
    {
        // GET /printers - Lista todas as impressoras
        group.MapGet("", GetPrinters)
            .WithName("GetPrinters")
            .WithMetadata(new SwaggerOperationAttribute(
                "List Printers",
                "Retorna todas as impressoras instaladas no sistema"));

        // GET /printers/detailed - Lista todas as impressoras com detalhes
        group.MapGet("/detailed", GetPrintersDetailed)
            .WithName("GetPrintersDetailed")
            .WithMetadata(new SwaggerOperationAttribute(
                "List Printers Detailed",
                "Retorna todas as impressoras com informações detalhadas"));

        // GET /printers/{name}/info - Informações de uma impressora específica
        group.MapGet("/{name}/info", GetPrinterInfo)
            .WithName("GetPrinterInfo")
            .WithMetadata(new SwaggerOperationAttribute(
                "Get Printer Info",
                "Retorna informações detalhadas de uma impressora específica"));
    }

    private static void ConfigurePrintJobEndpoints(RouteGroupBuilder group, EndpointOptions options)
    {
        // POST /print - Envia um job para impressão
        group.MapPost("", SubmitPrintJob)
            .WithName("SubmitPrintJob")
            .WithMetadata(new SwaggerOperationAttribute(
                "Submit Print Job",
                "Envia um trabalho de impressão para a fila"));

        // POST /print/advanced - Envia um job para impressão com configurações avançadas
        group.MapPost("/advanced", SubmitAdvancedPrintJob)
            .WithName("SubmitAdvancedPrintJob")
            .WithMetadata(new SwaggerOperationAttribute(
                "Submit Advanced Print Job",
                "Envia um trabalho de impressão com configurações avançadas de formatação"));
    }

    // Implementação dos handlers
    private static IResult GetPrinters(IPrintService printService)
    {
        try
        {
            var printers = printService.GetAvailablePrinters();
            return Results.Ok(new
            {
                success = true,
                count = printers.Length,
                printers
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Erro ao obter impressoras",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static IResult GetPrinterInfo(string name, IPrinterDiscoveryService printerService)
    {
        try
        {
            var printerInfo = printerService.GetPrinterInfo(name);
            if (printerInfo == null)
            {
                return Results.NotFound(new
                {
                    success = false,
                    message = $"Impressora '{name}' não encontrada"
                });
            }

            return Results.Ok(new
            {
                success = true,
                printer = printerInfo
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Erro ao obter informações da impressora",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static IResult SubmitPrintJob(PrintRequest request, IPrintService printService)
    {
        try
        {
            // Validações
            if (string.IsNullOrWhiteSpace(request.PrinterName))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Nome da impressora é obrigatório"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Texto a ser impresso é obrigatório"
                });
            }

            // Verificar se a impressora existe
            var availablePrinters = printService.GetAvailablePrinters();
            if (!availablePrinters.Contains(request.PrinterName))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = $"Impressora '{request.PrinterName}' não encontrada",
                    availablePrinters
                });
            }

            var jobId = printService.EnqueuePrintJob(request.PrinterName, request.Text, request.Settings);

            return Results.Ok(new
            {
                success = true,
                message = "Trabalho de impressão adicionado à fila com sucesso",
                jobId,
                printerName = request.PrinterName,
                settings = request.Settings,
                queueStatus = printService.GetQueueStatus()
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Erro ao processar solicitação de impressão",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static IResult GetQueueStatus(IPrintService printService)
    {
        try
        {
            var status = printService.GetQueueStatus();
            return Results.Ok(new
            {
                success = true,
                status
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Erro ao obter status da fila",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static IResult GetPrintersDetailed(IPrinterDiscoveryService printerService)
    {
        try
        {
            var printers = printerService.GetAllPrintersInfo();
            return Results.Ok(new
            {
                success = true,
                count = printers.Length,
                printers
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Erro ao obter impressoras detalhadas",
                detail: ex.Message,
                statusCode: 500);
        }
    }

    private static IResult SubmitAdvancedPrintJob(PrintRequest request, IPrintService printService)
    {
        try
        {
            // Validações
            if (string.IsNullOrWhiteSpace(request.PrinterName))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Nome da impressora é obrigatório"
                });
            }

            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Texto a ser impresso é obrigatório"
                });
            }

            // Verificar se a impressora existe
            var availablePrinters = printService.GetAvailablePrinters();
            if (!availablePrinters.Contains(request.PrinterName))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = $"Impressora '{request.PrinterName}' não encontrada",
                    availablePrinters
                });
            }

            var jobId = printService.EnqueuePrintJob(request.PrinterName, request.Text, request.Settings);

            return Results.Ok(new
            {
                success = true,
                message = "Trabalho de impressão avançado adicionado à fila com sucesso",
                jobId,
                printerName = request.PrinterName,
                settings = request.Settings,
                queueStatus = printService.GetQueueStatus()
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                title: "Erro ao processar solicitação de impressão avançada",
                detail: ex.Message,
                statusCode: 500);
        }
    }
}

public class EndpointOptions
{
    public bool RequireAuthentication { get; set; } = false;
    public bool EnableRateLimiting { get; set; } = false;
    public string ApiPrefix { get; set; } = string.Empty;
}
