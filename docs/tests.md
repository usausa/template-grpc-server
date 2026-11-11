# テスト

```bash
dotnet run --project tests/Template.GrpcServer.IntegrationTests
```

- xunit.v3 + Microsoft Testing Platform(テストのプロジェクトは実行ファイル。`dotnet run` で実行する)
- `IntegrationTests` = `WebApplicationFactory` のテストサーバー(テストクラスごとに一時フォルダーの DB・ストレージ)へ `GrpcChannel` でつなぎ、4 形態の RPC・ページングと一括取得・状態の条件と labels・部分更新と etag・エラーの詳細・認証・期限切れ・要求メトリクス・ファイル転送を確かめる
