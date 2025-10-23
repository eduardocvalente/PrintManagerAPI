using PrintManagerAPI.API.Interfaces;
using PrintManagerAPI.API.Services;

namespace PrintManagerAPI.Configuration;

public static class ServiceConfiguration
{
    public static IServiceCollection ConfigurePrintServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<IPrinterDiscoveryService, PrinterDiscoveryService>();
        services.AddSingleton<IPrintJobProcessor, PrintJobProcessor>();
        services.AddSingleton<IPrintService, PrintService>();

        return services;
    }

    public static IServiceCollection ConfigureApiOptions(this IServiceCollection services, Action<ApiOptions> configureOptions)
    {
        var options = new ApiOptions();
        configureOptions(options);

        services.AddSingleton(options);

        return services;
    }

    public static IServiceCollection ConfigureApiDocumentation(this IServiceCollection services, Action<SwaggerOptions> configureSwagger)
    {
        services.AddEndpointsApiExplorer();

        var swaggerOptions = new SwaggerOptions();
        configureSwagger(swaggerOptions);

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = swaggerOptions.Title,
                Version = swaggerOptions.Version,
                Description = swaggerOptions.Description,
                Contact = new()
                {
                    Name = swaggerOptions.ContactName,
                    Email = swaggerOptions.ContactEmail
                }
            });

            options.EnableAnnotations();
        });

        return services;
    }
}

public class ApiOptions
{
    public bool EnableCors { get; set; } = true;
    public bool EnableRequestLogging { get; set; } = true;
    public int MaxConcurrentPrintJobs { get; set; } = 1;
    public TimeSpan PrintJobTimeout { get; set; } = TimeSpan.FromMinutes(5);
}

public class SwaggerOptions
{
    public string Title { get; set; } = "PrintQueue API";
    public string Version { get; set; } = "v1";
    public string Description { get; set; } = "API para gerenciamento de fila de impressão com processamento FIFO";
    public string ContactName { get; set; } = "Eduardo Costa Valente";
    public string ContactEmail { get; set; } = "eduardocvalente1@hotmail.com";
}
