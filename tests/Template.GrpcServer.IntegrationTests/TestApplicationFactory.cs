namespace Template.GrpcServer;

using System.Security.Claims;
using System.Text;

using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Template.GrpcServer.Host.Settings;

public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "template-grpc-tests", Guid.NewGuid().ToString("N"));

    public string DatabasePath => Path.Combine(root, "data.db");

    public TestApplicationFactory()
    {
        Directory.CreateDirectory(root);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("http_ports", string.Empty);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={DatabasePath};Cache=Shared;Pooling=True;Default Timeout=1");
        builder.UseSetting("Storage:Root", Path.Combine(root, "storage"));
        builder.UseSetting("Prometheus:Uri", string.Empty);
        builder.UseSetting("Profiler:SqlTrace", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, true);
        }
    }

    public GrpcChannel CreateChannel() =>
        GrpcChannel.ForAddress(Server.BaseAddress, new GrpcChannelOptions { HttpHandler = Server.CreateHandler() });

    public CallInvoker CreateAuthorizedInvoker(GrpcChannel channel)
    {
        var token = CreateToken();
        return channel.Intercept(metadata =>
        {
            metadata.Add("Authorization", $"Bearer {token}");
            return metadata;
        });
    }

    private string CreateToken()
    {
        var setting = Services.GetRequiredService<AuthSetting>();
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = setting.Issuer,
            Audience = setting.Audience,
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(setting.SecretKey)), SecurityAlgorithms.HmacSha256)
        });
    }
}
