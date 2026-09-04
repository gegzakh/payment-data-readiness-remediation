using PDR.BuildingBlocks.WebApi;
using PDR.Simulation.Api.Endpoints;
using PDR.Simulation.Application;
using PDR.Simulation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPdrService("simulation");
builder.Services.AddSimulationApplication();
builder.Services.AddSimulationInfrastructure(builder.Configuration);

var app = builder.Build();

app.UsePdrDefaults();
app.MapScenarioEndpoints();
app.MapTestingEndpoints();
app.MapCutoverEndpoints();
app.MapSettingsEndpoints();

await app.RunAsync();

/// <summary>Entry point exposed so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
