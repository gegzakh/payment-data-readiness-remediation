using PDR.BuildingBlocks.WebApi;
using PDR.Rules.Api.Endpoints;
using PDR.Rules.Application;
using PDR.Rules.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPdrService("rules");
builder.Services.AddRulesApplication();
builder.Services.AddRulesInfrastructure(builder.Configuration);

var app = builder.Build();

app.UsePdrDefaults();
app.MapRulesEndpoints();
app.MapSettingsEndpoints();

await app.RunAsync();

/// <summary>Entry point exposed so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
