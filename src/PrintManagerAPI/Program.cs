using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using PrintManagerAPI.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Enums aceitos e serializados como string ("A4", "Center"), conforme a documentação.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();

builder.Services
    .AddPrintQueue(builder.Configuration)
    .AddApiDocumentation();

builder.Services.AddHealthChecks()
    .AddCheck<PrinterHealthCheck>("printers");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("print", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 30;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

// Erros não tratados viram ProblemDetails (RFC 9457) sem vazar stack trace.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PrintManager API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "PrintManager API Documentation";
    });
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapPrintQueueEndpoints();

// Liveness: o processo responde. Readiness: o subsistema de impressão está OK.
app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
app.MapHealthChecks("/health");

app.Run();

// Exposto para testes de integração com WebApplicationFactory.
public partial class Program { }
