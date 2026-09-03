using PDR.BuildingBlocks.WebApi;
using PDR.Sources.Api.Endpoints;
using PDR.Sources.Application;
using PDR.Sources.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPdrService("sources");
builder.Services.AddSourcesApplication();
builder.Services.AddSourcesInfrastructure(builder.Configuration);

var app = builder.Build();

app.UsePdrDefaults();
app.MapSourcesEndpoints();
app.MapSettingsEndpoints();

await app.RunAsync();

/// <summary>Entry point exposed so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
