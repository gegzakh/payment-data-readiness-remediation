using PDR.BuildingBlocks.WebApi;
using PDR.Notifications.Api.Endpoints;
using PDR.Notifications.Application;
using PDR.Notifications.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddPdrService("notifications");
builder.Services.AddNotificationsApplication();
builder.Services.AddNotificationsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UsePdrDefaults();
app.MapNotificationEndpoints();
app.MapSettingsEndpoints();

await app.RunAsync();

/// <summary>Entry point exposed so integration tests can host the API with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
