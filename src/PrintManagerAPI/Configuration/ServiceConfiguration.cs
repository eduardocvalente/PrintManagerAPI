using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Services;

namespace PrintManagerAPI.Configuration;

public static class ServiceConfiguration
{
    public static IServiceCollection AddPrintQueue(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PrintQueueOptions>()
            .Bind(configuration.GetSection(PrintQueueOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IPrinterDiscoveryService, PrinterDiscoveryService>();
        services.AddSingleton<IPrintJobProcessor, PrintJobProcessor>();

        // PrintService é registrado pelo tipo concreto e exposto via interface a partir
        // da mesma instância: o worker precisa dos métodos internos de transição de estado.
        services.AddSingleton<PrintService>();
        services.AddSingleton<IPrintService>(sp => sp.GetRequiredService<PrintService>());
        services.AddHostedService<PrintQueueWorker>();

        return services;
    }

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "PrintManager API",
                Version = "v1",
                Description = "API para gerenciamento de fila de impressão com processamento FIFO (um job por vez).",
                Contact = new()
                {
                    Name = "Eduardo Costa Valente",
                    Url = new Uri("https://github.com/eduardocvalente")
                }
            });

            options.EnableAnnotations();
        });

        return services;
    }
}
