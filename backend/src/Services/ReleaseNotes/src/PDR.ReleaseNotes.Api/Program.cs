using PDR.BuildingBlocks.WebApi;
using PDR.ReleaseNotes.Api.Endpoints;
using PDR.ReleaseNotes.Application;
using PDR.ReleaseNotes.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPdrService("release-notes");
builder.Services.AddReleaseNotesApplication();
builder.Services.AddReleaseNotesInfrastructure(builder.Configuration);

var app = builder.Build();

app.UsePdrDefaults();
app.MapReleaseEndpoints();
app.MapSettingsEndpoints();

await app.RunAsync();

/// <summary>Entry point exposed so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
