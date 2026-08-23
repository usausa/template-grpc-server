using Microsoft.Extensions.Hosting.WindowsServices;

//--------------------------------------------------------------------------------
// Configure builder
//--------------------------------------------------------------------------------
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
});

// System
builder.ConfigureSystem();

// Host
builder.ConfigureHost();

// Logging
builder.ConfigureLogging();

// gRPC
builder.ConfigureGrpc();

// Authentication
builder.ConfigureAuthentication();

// Metrics
builder.ConfigureTelemetry();

// Components
builder.ConfigureComponents();

//--------------------------------------------------------------------------------
// Configure the HTTP request pipeline.
//--------------------------------------------------------------------------------
var app = builder.Build();

// Startup information
app.LogStartupInformation();

// Authentication
app.UseAuthentication();
app.UseAuthorization();

// End point
app.MapEndpoints();

// Initialize
await app.InitializeApplicationAsync();

// Run
await app.RunAsync();
