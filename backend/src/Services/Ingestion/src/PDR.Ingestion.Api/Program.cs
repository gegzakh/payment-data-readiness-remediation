using PDR.BuildingBlocks.WebApi;
using PDR.Ingestion.Api.Endpoints;
using PDR.Ingestion.Application;
using PDR.Ingestion.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPdrService("ingestion");
builder.Services.AddIngestionApplication();
builder.Services.AddIngestionInfrastructure(builder.Configuration);

var app = builder.Build();

app.UsePdrDefaults();
app.MapIngestionEndpoints();
app.MapSettingsEndpoints();

await app.RunAsync();

/// <summary>Entry point exposed so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
