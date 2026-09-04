using PDR.BuildingBlocks.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddPdrService("gateway");

// Routes/clusters are configuration-driven so services can be added without changing gateway code.
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UsePdrDefaults();
app.MapReverseProxy();

await app.RunAsync();
