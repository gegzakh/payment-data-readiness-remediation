using PDR.BuildingBlocks.WebApi;
using PDR.Reporting.Api.Endpoints;
using PDR.Reporting.Application;
using PDR.Reporting.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPdrService("reporting");
builder.Services.AddReportingApplication();
builder.Services.AddReportingInfrastructure(builder.Configuration);

var app = builder.Build();

app.UsePdrDefaults();
app.MapDashboardEndpoints();
app.MapSettingsEndpoints();

await app.RunAsync();

/// <summary>Entry point exposed so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
