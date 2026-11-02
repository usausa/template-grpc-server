namespace Template.GrpcServer.Host.Application;

using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;

using MiniDataProfiler;
using MiniDataProfiler.Listener.Logging;
using MiniDataProfiler.Listener.OpenTelemetry;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

using Smart.Data;

using Template.GrpcServer.Accessors;
using Template.GrpcServer.Host.Application.Context;
using Template.GrpcServer.Host.Application.ExceptionHandling;
using Template.GrpcServer.Host.Application.HealthChecks;
using Template.GrpcServer.Host.Application.Telemetry;
using Template.GrpcServer.Host.Handlers;
using Template.GrpcServer.Host.Settings;
using Template.GrpcServer.Host.Subscriptions;
using Template.GrpcServer.Host.Workers;
using Template.GrpcServer.Infrastructure.Storage;

public static class ApplicationExtensions
{
    private const string SchemaPath = "Assets/Data/Schema.sql";

    //--------------------------------------------------------------------------------
    // System
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureSystem(this WebApplicationBuilder builder)
    {
        // Path
        builder.Configuration.SetBasePath(AppContext.BaseDirectory);

        // Encoding
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Host
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureHost(this WebApplicationBuilder builder)
    {
        // Service
        builder.Services
            .AddWindowsService()
            .AddSystemd();

        // Feature management
        builder.Services.AddFeatureManagement();

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Logging
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureLogging(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = builder.Configuration.IsOtelExporterEnabled();

        // Application log
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(options => options.ReadFrom.Configuration(builder.Configuration), writeToProviders: useOtlpExporter);

        return builder;
    }

    //--------------------------------------------------------------------------------
    // gRPC
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureGrpc(this IHostApplicationBuilder builder)
    {
        var setting = builder.Configuration.GetSection("Grpc").Get<GrpcSetting>()!;

        // gRPC
        builder.Services.AddGrpc(options =>
        {
            // Loggingを外側にし、Exceptionが変換した後の状態を記録する
            options.Interceptors.Add<LoggingInterceptor>();
            options.Interceptors.Add<ExceptionInterceptor>();
            options.Interceptors.Add<ServiceContextInterceptor>();

            // 応答の圧縮。クライアントがgzipを受け付けるときだけ効く。圧縮済みのバイナリを流すメソッドはcontext.WriteOptionsのWriteFlags.NoCompressで個別に外す
            if (setting.Compression.Enable)
            {
                options.ResponseCompressionAlgorithm = "gzip";
                options.ResponseCompressionLevel = setting.Compression.Level;
            }
        });

        // gRPC Reflection
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddGrpcReflection();
        }

        // gRPC Health
        builder.Services.AddGrpcHealthChecks()
            .AddCheck("self", static () => HealthCheckResult.Healthy())
            .AddCheck<DatabaseHealthCheck>("database");

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Authentication
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureAuthentication(this IHostApplicationBuilder builder)
    {
        var setting = builder.Configuration.GetSection("Auth").Get<AuthSetting>()!;

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = setting.Issuer,
                    ValidateAudience = true,
                    ValidAudience = setting.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(setting.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Telemetry
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureTelemetry(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = builder.Configuration.IsOtelExporterEnabled();

        var prometheusSection = builder.Configuration.GetSection("Prometheus");
        var prometheusUri = prometheusSection.GetValue<string>("Uri")!;
        var usePrometheusExporter = !String.IsNullOrEmpty(prometheusUri);

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(config =>
            {
                config.AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(),
                    serviceInstanceId: Environment.MachineName);
            });

        // Log
        if (useOtlpExporter)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
            builder.Services.Configure<OpenTelemetryLoggerOptions>(static logging =>
            {
                logging.AddOtlpExporter();
            });
        }

        // Metrics
        if (useOtlpExporter || usePrometheusExporter)
        {
            telemetry
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddRuntimeInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddAspNetCoreInstrumentation()
                        .AddApplicationInstrumentation();

                    if (useOtlpExporter)
                    {
                        metrics.AddOtlpExporter();
                    }

                    if (usePrometheusExporter)
                    {
                        var prometheusEndpoint = new Uri(prometheusUri);
                        metrics.AddPrometheusHttpListener(config =>
                        {
                            config.Host = prometheusEndpoint.Host;
                            config.Port = prometheusEndpoint.Port;
                        });
                    }
                });
        }

        // Trace
        if (useOtlpExporter)
        {
            telemetry
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(builder.Environment.ApplicationName)
                        .AddAspNetCoreInstrumentation()
                        .AddGrpcClientInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddMiniDataProfilerInstrumentation()
                        .AddApplicationInstrumentation();

                    tracing.AddOtlpExporter();
                });
        }

        // Custom instrument
        builder.Services.AddApplicationInstrument();

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Components
    //--------------------------------------------------------------------------------

    public static IHostApplicationBuilder ConfigureComponents(this IHostApplicationBuilder builder)
    {
        // System
        builder.Services.AddSingleton(TimeProvider.System);

        // Data
        builder.Services.AddSingleton<IDbProvider>(static p =>
        {
            var configuration = p.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Default");

            var settings = p.GetRequiredService<ProfilerSetting>();
            if (settings.SqlTrace)
            {
                var logListener = new LoggingListener(p.GetRequiredService<ILogger<LoggingListener>>(), new LoggingListenerOption());
                var telemetryListener = new OpenTelemetryListener(new OpenTelemetryListenerOption());
                var listener = new ChainListener(logListener, telemetryListener);
                return new DelegateDbProvider(() => new ProfileDbConnection(listener, new SqliteConnection(connectionString)));
            }

            return new DelegateDbProvider(() => new SqliteConnection(connectionString));
        });
        builder.Services.AddSingleton<IDialect>(new DelegateDialect(
            static ex => ex is SqliteException { SqliteErrorCode: 19 } or SqliteException { SqliteExtendedErrorCode: 1555 or 2067 },
            static x => Regex.Replace(x, @"[%_\\]", @"\$0")));
        builder.Services.AddDataAccessors(typeof(DataAccessor).Assembly);

        // Storage
        builder.Services.AddOptions<FileStorageOption>().BindConfiguration("Storage").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<FileStorageOption>>().Value);
        builder.Services.AddSingleton<IStorage, FileStorage>();
        builder.Services.AddOptions<StorageHandlerOption>().BindConfiguration("FileTransfer").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<StorageHandlerOption>>().Value);

        // Service
        builder.Services.AddSingleton<ApplicationServiceContextProvider>();
        builder.Services.AddSingleton<ServiceContextProvider>(static p => p.GetRequiredService<ApplicationServiceContextProvider>());
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<DataService>();

        // Notify
        builder.Services.AddSingleton<BroadcastSubscription>();
        builder.Services.AddOptions<BroadcastSubscriptionOption>().BindConfiguration("Broadcast").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<BroadcastSubscriptionOption>>().Value);
        builder.Services.AddOptions<TickWorkerOption>().BindConfiguration("Tick").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<TickWorkerOption>>().Value);
        if (builder.Configuration.GetSection("Tick").Get<TickWorkerOption>()!.Enable)
        {
            builder.Services.AddHostedService<TickWorker>();
        }

        // Setting
        builder.Services.AddOptions<AuthSetting>().BindConfiguration("Auth").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<AuthSetting>>().Value);
        builder.Services.AddOptions<GrpcSetting>().BindConfiguration("Grpc").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<GrpcSetting>>().Value);
        builder.Services.AddOptions<ProfilerSetting>().BindConfiguration("Profiler").ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton(static p => p.GetRequiredService<IOptions<ProfilerSetting>>().Value);

        return builder;
    }

    //--------------------------------------------------------------------------------
    // Information
    //--------------------------------------------------------------------------------

    public static void LogStartupInformation(this WebApplication app)
    {
        ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);

        var prometheusSection = app.Configuration.GetSection("Prometheus");
        var prometheusUri = prometheusSection.GetValue("Uri", string.Empty);

        app.Logger.InfoServiceStart();
        app.Logger.InfoServiceSettingsRuntime(RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);
        app.Logger.InfoServiceSettingsEnvironment(typeof(Program).Assembly.GetName().Version, Environment.CurrentDirectory);
        app.Logger.InfoServiceSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
        app.Logger.InfoServiceSettingsThreadPool(workerThreads, completionPortThreads);
        app.Logger.InfoServiceSettingsTelemetry(app.Configuration.GetOtelExporterEndpoint(), prometheusUri);
    }

    //--------------------------------------------------------------------------------
    // End point
    //--------------------------------------------------------------------------------

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        // gRPC
        app.MapGrpcService<DataHandler>().RequireAuthorization();
        app.MapGrpcService<StorageHandler>().RequireAuthorization();
        app.MapGrpcHealthChecksService();
        if (app.Environment.IsDevelopment())
        {
            app.MapGrpcReflectionService();
        }

        // Root
        app.MapGet("/", static () => "gRPC Server");

        return app;
    }

    //--------------------------------------------------------------------------------
    // Startup
    //--------------------------------------------------------------------------------

    public static async ValueTask InitializeApplicationAsync(this WebApplication app)
    {
        // Prepare instrument
        app.Services.GetRequiredService<ApplicationInstrument>();

        // Prepare database
        await app.Services.GetRequiredService<DatabaseService>().InitializeAsync(SchemaPath, CancellationToken.None);

        // Prepare storage
        Directory.CreateDirectory(app.Services.GetRequiredService<FileStorageOption>().Root);
    }

    //--------------------------------------------------------------------------------
    // Configuration
    //--------------------------------------------------------------------------------

    private static bool IsOtelExporterEnabled(this IConfiguration configuration) =>
        !String.IsNullOrWhiteSpace(configuration.GetOtelExporterEndpoint());

    private static string GetOtelExporterEndpoint(this IConfiguration configuration) =>
        configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? string.Empty;
}
