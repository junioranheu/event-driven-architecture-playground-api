using EventDrivenArchitecturePlayground.API;
using EventDrivenArchitecturePlayground.Application;
using EventDrivenArchitecturePlayground.Infrastructure;

WebApplicationBuilder builder =  WebApplication.CreateBuilder(args);

builder.Services.
    AddApplication().
    AddInfrastructure(builder.Configuration).
    AddPresentation();

WebApplication app = builder.Build();

app.UsePresentation();

app.Run();