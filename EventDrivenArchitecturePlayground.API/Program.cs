using EventDrivenArchitecturePlayground.API;
using EventDrivenArchitecturePlayground.Application;
using EventDrivenArchitecturePlayground.Infrastructure;
using System.Diagnostics;

Console.Title = "Event Driven Architecture Playground";

WebApplicationBuilder builder =  WebApplication.CreateBuilder(args);

builder.Services.
    AddApplication().
    AddInfrastructure(builder.Configuration).
    AddPresentation(builder);

WebApplication app = builder.Build();

app.UsePresentation();

// Em ambiente de desenvolvimento, inicia-se o projeto ConsoleConsumer para consumir mensagens do RabbitMQ,
// Simulando um outro serviço (real) que consome eventos publicados pela API.
if (app.Environment.IsDevelopment())
{
    Process.Start(new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "run --project ../EventDrivenArchitecturePlayground.ConsoleConsumer/EventDrivenArchitecturePlayground.ConsoleConsumer.csproj",
        UseShellExecute = true
    });
}

app.Run();