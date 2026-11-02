using BunnyTail.DependencyInjection;

using Grpc.Core;
using Grpc.Net.Client.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Smart.CommandLine.Hosting;

using Template.GrpcServer.TestClient.Commands;
using Template.GrpcServer.TestClient.Filters;
using Template.GrpcServer.TestClient.Infrastructure.Interceptors;
using Template.GrpcServer.TestClient.Infrastructure.Tokens;
using Template.GrpcServer.TestClient.Settings;

var builder = CommandHost.CreateBuilder(args)
    .UseDefaults();

builder.ConfigureContainer(new GeneratedServiceProviderFactory(static options => options.TrackTransientDisposables = false));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddCommandComponents();
builder.Services.AddFilterComponents();

// Setting
var setting = builder.Configuration.GetSection("Client").Get<ClientSetting>()!;
builder.Services.AddSingleton(setting);

// gRPC client (Grpc.Net.ClientFactory)
builder.Services.AddSingleton<TokenProvider>();
builder.Services.AddSingleton<AuthorizationInterceptor>();
builder.Services.AddSingleton<LoggingInterceptor>();
builder.Services
    .AddGrpcClient<Data.DataClient>(options =>
    {
        options.Address = new Uri(setting.Address);
    })
    .ConfigurePrimaryHttpMessageHandler(() => CreateHandler(setting.Connection))
    .ConfigureChannel(options =>
    {
        // Unavailable(接続不可・再起動中)だけを指数バックオフで再試行する。ストリーミングも最初の応答までは対象
        options.ServiceConfig = new ServiceConfig
        {
            MethodConfigs =
            {
                new MethodConfig
                {
                    Names = { MethodName.Default },
                    RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = setting.Retry.MaxAttempts,
                        InitialBackoff = TimeSpan.FromMilliseconds(setting.Retry.InitialBackoff),
                        MaxBackoff = TimeSpan.FromMilliseconds(setting.Retry.MaxBackoff),
                        BackoffMultiplier = setting.Retry.BackoffMultiplier,
                        RetryableStatusCodes = { StatusCode.Unavailable }
                    }
                }
            }
        };
    })
    .AddInterceptor<LoggingInterceptor>()
    .AddInterceptor<AuthorizationInterceptor>();
builder.Services
    .AddGrpcClient<Storage.StorageClient>(options =>
    {
        options.Address = new Uri(setting.Address);
    })
    .ConfigurePrimaryHttpMessageHandler(() => CreateHandler(setting.Connection))
    .AddInterceptor<LoggingInterceptor>()
    .AddInterceptor<AuthorizationInterceptor>();

builder.ConfigureCommands(commands =>
{
    commands.ConfigureRootCommand(root =>
    {
        root.WithDescription("gRPC test client");
    });

    commands.AddGlobalFilters();
    commands.AddCommands();
});

var host = builder.Build();
#if DEBUG
if (host.Services is GeneratedServiceProvider generatedProvider)
{
    foreach (var line in BunnyTail.DependencyInjection.Diagnostics.ServiceFactoryReportExtensions.DescribeRuntimeFallbacks(generatedProvider).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
    {
        System.Diagnostics.Debug.WriteLine(line);
    }
}
#endif
return await host.RunAsync();

static SocketsHttpHandler CreateHandler(ClientSetting.ConnectionSetting setting) => new()
{
    EnableMultipleHttp2Connections = true,
    ConnectTimeout = TimeSpan.FromMilliseconds(setting.ConnectTimeout),
    KeepAlivePingDelay = setting.KeepAlivePingDelay > 0 ? TimeSpan.FromMilliseconds(setting.KeepAlivePingDelay) : Timeout.InfiniteTimeSpan,
    KeepAlivePingTimeout = TimeSpan.FromMilliseconds(setting.KeepAlivePingTimeout)
};
