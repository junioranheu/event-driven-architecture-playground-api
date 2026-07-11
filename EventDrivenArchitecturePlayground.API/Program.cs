using EventDrivenArchitecturePlayground.API;
using EventDrivenArchitecturePlayground.Application;
using EventDrivenArchitecturePlayground.Infrastructure;

Console.Title = "Event Driven Architecture Playground";

WebApplicationBuilder builder =  WebApplication.CreateBuilder(args);

builder.Services.
    AddApplication().
    AddInfrastructure(builder.Configuration).
    AddPresentation(builder);

WebApplication app = builder.Build();

app.UsePresentation();

app.Run();