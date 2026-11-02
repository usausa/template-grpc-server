namespace Template.GrpcServer.TestClient.Settings;

#pragma warning disable CA1034
public sealed class ClientSetting
{
    [Required]
    public string Address { get; set; } = default!;

    [Required]
    public AuthSetting Auth { get; set; } = default!;

    [Required]
    public RetrySetting Retry { get; set; } = default!;

    [Range(1, 600_000)]
    public int Deadline { get; set; }

    [Range(1024, 1_048_576)]
    public int ChunkSize { get; set; }

    // 動作確認用にサーバーと同じ鍵でトークンを自前発行する(実運用のクライアントは認証サーバーから取得する)
    public sealed class AuthSetting
    {
        [Required]
        [MinLength(32)]
        public string SecretKey { get; set; } = default!;

        [Required]
        public string Issuer { get; set; } = default!;

        [Required]
        public string Audience { get; set; } = default!;

        [Required]
        public string User { get; set; } = default!;
    }

    public sealed class RetrySetting
    {
        // grpc-dotnetは2未満を拒否し、既定では5を上限に丸める(GrpcChannelOptions.MaxRetryAttempts)
        [Range(2, 5)]
        public int MaxAttempts { get; set; }

        [Range(1, 60_000)]
        public int InitialBackoff { get; set; }

        [Range(1, 60_000)]
        public int MaxBackoff { get; set; }

        [Range(1.0, 10.0)]
        public double BackoffMultiplier { get; set; }
    }
}
#pragma warning restore CA1034
