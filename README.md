# template-grpc-server

## 動作確認用クライアント(Template.GrpcServer.TestClient)

サーバーの全 RPC を一巡できるコンソール。`Grpc.Net.ClientFactory`(`AddGrpcClient` + `ConfigureChannel`)でリトライポリシー(`Unavailable` を指数バックオフで最大 5 回)と、クライアント側インターセプター(JWT 付与 / 呼出ログ)を持つ。JWT はサーバーと同じ鍵で自前発行する(動作確認用。実運用のクライアントは認証サーバーから取得する)。

```bash
dotnet run --project src/Template.GrpcServer.TestClient -- set --name item-1 --value 10
dotnet run --project src/Template.GrpcServer.TestClient -- get --id 1
dotnet run --project src/Template.GrpcServer.TestClient -- list --name item --sort Name --desc
dotnet run --project src/Template.GrpcServer.TestClient -- delete --id 1
echo "a 1" | dotnet run --project src/Template.GrpcServer.TestClient -- upload
dotnet run --project src/Template.GrpcServer.TestClient -- chat --name me
dotnet run --project src/Template.GrpcServer.TestClient -- notify --name alice
dotnet run --project src/Template.GrpcServer.TestClient -- upload-file --file ./big.bin --name big.bin --compress
dotnet run --project src/Template.GrpcServer.TestClient -- download-file --name big.bin --output ./big.bin
```

| コマンド | RPC | 内容 |
|---|---|---|
| `get` / `set` / `delete` | Unary | 検証エラー(`BadRequest` のフィールド違反)と重複(`ErrorInfo`)は `google.rpc.Status` の詳細と `trace-id` を表示する |
| `list` | Server streaming | 名前の部分一致。`--sort`(`Id` / `Name` / `Value` / `CreatedAt`。既定は `Id`)と `--desc` で並べ替える |
| `upload` | Client streaming | 標準入力の各行(`name value`)を送る。登録できなかった行は位置と理由付きで返り(`失敗しても止めない`)、`NG [index]` で表示する |
| `chat` | Bidirectional | 標準入力の各行を送り、エコーを表示する。EOF で終了 |
| `notify` | Bidirectional(サーバー主体) | 参加して配信(`Tick` / `Broadcast`)を表示し、標準入力の各行を全員へ配信する。EOF で終了 |
| `upload-file` / `download-file` | Client / Server streaming(`Storage` サービス) | 最初の 1 通がメタデータ、以降がチャンク(`Client:ChunkSize`)。進捗を表示する。`--compress` で要求ストリームを gzip 圧縮する |

接続先・鍵・リトライは `appsettings.json` の `Client` セクション(環境変数 `Client__Address` 等で上書き可)。サーバー側の定期配信は `Tick:Enable=true` で有効になる。

## 圧縮とファイル保管

- 応答の圧縮は `Grpc:Compression`(既定 gzip / `Fastest`)。クライアントが `grpc-accept-encoding` で gzip を受け付けるときだけ効く。要求の圧縮はクライアントが `grpc-internal-encoding-request` ヘッダーで指定する
- ファイルは `Storage:Root`(既定 `storage`)配下に `FileStorage` が保存する(一時ファイルへ書いて原子的に置換)。`..` を含む名前は `Invalid path.`(`InvalidArgument`)
