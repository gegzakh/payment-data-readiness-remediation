using PDR.BuildingBlocks.WebApi;
using PDR.Audit.Api.Endpoints;
using PDR.Audit.Application;
using PDR.Audit.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPdrService("audit");
builder.Services.AddAuditApplication();
builder.Services.AddAuditInfrastructure(builder.Configuration);

var app = builder.Build();

app.UsePdrDefaults();
app.MapAuditEndpoints();
app.MapSettingsEndpoints();

await app.RunAsync();

/// <summary>Entry point exposed so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
