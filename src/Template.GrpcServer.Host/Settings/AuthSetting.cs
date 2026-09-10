namespace Template.GrpcServer.Host.Settings;

public sealed class AuthSetting
{
    [Required]
    [MinLength(32)]
    public string SecretKey { get; set; } = default!;

    [Required]
    public string Issuer { get; set; } = default!;

    [Required]
    public string Audience { get; set; } = default!;
}
