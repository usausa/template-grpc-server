using Serilog;

using Template.GrpcServer.Host.Services;

//--------------------------------------------------------------------------------
// Configure builder
//--------------------------------------------------------------------------------
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

// Log
builder.Logging.ClearProviders();
builder.Services.AddSerilog(option => option.ReadFrom.Configuration(builder.Configuration), writeToProviders: true);

// gRPC
builder.Services.AddGrpc();

// Component
// TODO

//--------------------------------------------------------------------------------
// Configure the HTTP request pipeline.
//--------------------------------------------------------------------------------
var app = builder.Build();

// gRPC
app.MapGrpcService<GreeterService>();

// Default
app.MapGet("/", () => "gRPC Server");

app.Run();
