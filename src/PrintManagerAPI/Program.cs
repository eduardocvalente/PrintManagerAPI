using PrintManagerAPI.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configuração de serviços com lambdas
builder.Services
    .ConfigurePrintServices(builder.Configuration)
    .ConfigureApiOptions(options =>
    {
        options.EnableCors = true;
        options.EnableRequestLogging = true;
        options.MaxConcurrentPrintJobs = 1;
        options.PrintJobTimeout = TimeSpan.FromMinutes(5);
    })
    .ConfigureApiDocumentation(swagger =>
    {
        swagger.Title = "PrintQueue API";
        swagger.Version = "v1.0";
        swagger.Description = "API para gerenciamento de fila de impressão com processamento FIFO";
        swagger.ContactName = "Equipe de Desenvolvimento";
        swagger.ContactEmail = "dev@printqueue.com";
    });

var app = builder.Build();

// Configuração do pipeline de requisições
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PrintQueue API v1.0");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "PrintQueue API Documentation";
    });
}

app.UseHttpsRedirection();

// Configuração dos endpoints com lambdas
app.ConfigurePrintQueueEndpoints(endpoints =>
{
    endpoints.RequireAuthentication = false;
    endpoints.EnableRateLimiting = false;
    endpoints.ApiPrefix = string.Empty;
});

// Endpoint de saúde da aplicação
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithTags("Health");

app.Run();
