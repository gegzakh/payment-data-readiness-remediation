using PDR.BuildingBlocks.WebApi;
using PDR.Validation.Api.Endpoints;
using PDR.Validation.Application;
using PDR.Validation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPdrService("validation");
builder.Services.AddValidationApplication();
builder.Services.AddValidationInfrastructure(builder.Configuration);

var app = builder.Build();

app.UsePdrDefaults();
app.MapValidationEndpoints();
app.MapSettingsEndpoints();

await app.RunAsync();

/// <summary>Entry point exposed so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
