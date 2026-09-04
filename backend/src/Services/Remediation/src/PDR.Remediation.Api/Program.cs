using PDR.BuildingBlocks.WebApi;
using PDR.Remediation.Api.Endpoints;
using PDR.Remediation.Application;
using PDR.Remediation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPdrService("remediation");
builder.Services.AddRemediationApplication();
builder.Services.AddRemediationInfrastructure(builder.Configuration);

var app = builder.Build();

app.UsePdrDefaults();
app.MapRemediationEndpoints();
app.MapWriteBackEndpoints();
app.MapCampaignEndpoints();
app.MapSettingsEndpoints();

await app.RunAsync();

/// <summary>Entry point exposed so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
