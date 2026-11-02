namespace Template.GrpcServer.TestClient.Infrastructure.Tokens;

using System.Security.Claims;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Template.GrpcServer.TestClient.Settings;

public sealed class TokenProvider
{
    private readonly ClientSetting setting;

    private readonly TimeProvider timeProvider;

    private readonly Lock sync = new();

    private string? token;

    private DateTimeOffset expires;

    public TokenProvider(ClientSetting setting, TimeProvider timeProvider)
    {
        this.setting = setting;
        this.timeProvider = timeProvider;
    }

    public string GetToken()
    {
        lock (sync)
        {
            var now = timeProvider.GetUtcNow();
            if ((token is null) || (now >= expires))
            {
                expires = now.AddHours(1);
                token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
                {
                    Issuer = setting.Auth.Issuer,
                    Audience = setting.Auth.Audience,
                    Subject = new ClaimsIdentity([new Claim(ClaimTypes.Name, setting.Auth.User)]),
                    Expires = expires.UtcDateTime,
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(setting.Auth.SecretKey)), SecurityAlgorithms.HmacSha256)
                });
            }

            return token;
        }
    }
}
