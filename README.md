# template-grpc-server

## 動作確認用クライアント(Template.GrpcServer.TestClient)

サーバーの全 RPC を一巡できるコンソール。`Grpc.Net.ClientFactory`(`AddGrpcClient` + `ConfigureChannel`)でリトライポリシー(`Unavailable` を指数バックオフで最大 5 回)と、クライアント側インターセプター(JWT 付与 / 呼出ログ)を持つ。Unary(`get` / `batch-get` / `set` / `update` / `delete` / `list`)には期限を付ける(リトライも含めた呼出全体の上限)。JWT はサーバーと同じ鍵で自前発行する(動作確認用。実運用のクライアントは認証サーバーから取得する)。

```bash
dotnet run --project src/Template.GrpcServer.TestClient -- set --name item-1 --value 10
dotnet run --project src/Template.GrpcServer.TestClient -- set --name item-2 --value 20 --status Inactive --labels env=dev,owner=me
dotnet run --project src/Template.GrpcServer.TestClient -- get --id 1
dotnet run --project src/Template.GrpcServer.TestClient -- batch-get --ids 1,2
dotnet run --project src/Template.GrpcServer.TestClient -- update --id 1 --value 30 --mask value
dotnet run --project src/Template.GrpcServer.TestClient -- update --id 2 --status Active --etag '"1"'
dotnet run --project src/Template.GrpcServer.TestClient -- list --name item --sort Name --desc --size 2
dotnet run --project src/Template.GrpcServer.TestClient -- list --name item --sort Name --desc --size 2 --token <next>
dotnet run --project src/Template.GrpcServer.TestClient -- list --from 2026-01-01T00:00:00Z --all
dotnet run --project src/Template.GrpcServer.TestClient -- list --status Active
dotnet run --project src/Template.GrpcServer.TestClient -- export --name item --sort Name --desc
dotnet run --project src/Template.GrpcServer.TestClient -- delete --id 1 --etag '"2"'
echo "a 1" | dotnet run --project src/Template.GrpcServer.TestClient -- upload
dotnet run --project src/Template.GrpcServer.TestClient -- chat --name me
dotnet run --project src/Template.GrpcServer.TestClient -- notify --name alice
dotnet run --project src/Template.GrpcServer.TestClient -- upload-file --file ./big.bin --name big.bin --compress
dotnet run --project src/Template.GrpcServer.TestClient -- download-file --name big.bin --output ./big.bin
dotnet run --project src/Template.GrpcServer.TestClient -- token
```

| コマンド | RPC | 内容 |
|---|---|---|
| `get` / `set` | Unary | `set` の `--status` は `Active`(既定)か `Inactive`、`--labels` は `k=v` のカンマ区切り(16 個まで・キーは 1〜63 文字・値は 63 文字まで)。検証エラー(`BadRequest` のフィールド違反)と重複(`ErrorInfo`)は `google.rpc.Status` の詳細と `trace-id` を表示する |
| `update` | Unary | `--mask`(`name` / `value` / `status` / `labels` のカンマ区切り。`*` は全部)に書いた項目だけを変え、変えた後の項目を表示する。`--mask` を省けば値を入れた項目を変え、`--mask ""` は何も変えない。`--etag` を付けると版が合うときだけ変え、違えば `Aborted` |
| `delete` | Unary | 無ければ `NotFound`。`--etag` を付けると版が合うときだけ消し、違えば `Aborted` |
| `batch-get` | Unary | `--ids` の順に返す(重複はそのまま)。無い ID が 1 件でもあれば `NotFound` で、無い ID を表示する |
| `list` | Unary(ページング) | 名前の部分一致・作成日時の範囲(`--from` 以上 `--to` 未満)・状態(`--status`)・並べ替え(`--sort` は `Id` / `Name` / `Value` / `CreatedAt`。既定は `Id`。`--desc`)で `--size` 件ずつ(既定 20・上限 100)。続きは表示された `next` を `--token` に渡す。`--all` で最後までたどる |
| `export` | Server streaming | `list` と同じ条件で、全件を 1 件ずつ受け取る |
| `upload` | Client streaming | 標準入力の各行(`name value`)を送る。登録できなかった行は位置と理由付きで返り(`失敗しても止めない`)、`NG [index]` で表示する |
| `chat` | Bidirectional | 標準入力の各行を送り、エコーを表示する。EOF で終了 |
| `notify` | Bidirectional(サーバー主体) | 参加して配信(`Tick` / `Broadcast`)を表示し、標準入力の各行を全員へ配信する。EOF で終了 |
| `upload-file` / `download-file` | Client / Server streaming(`Storage` サービス) | 最初の 1 通がメタデータ、以降がチャンク(`Client:ChunkSize`)。進捗を表示する。`--compress` で要求ストリームを gzip 圧縮する |
| `token` | — | 動作確認用の JWT を表示する(grpcui などから呼ぶときに付ける) |

項目は `id name value created_at status [labels] etag` の形で表示する。etag は版を引用符で囲んだもの(作ると `"1"`、変えるたびに 1 つ進む)。接続先・鍵・リトライ・期限(`Client:Deadline`。既定 30 秒)は `appsettings.json` の `Client` セクション(環境変数 `Client__Address` 等で上書き可)。サーバー側の定期配信は `Tick:Enable=true` で有効になる。

## grpcui で試す

Development では Reflection が有効なので、[grpcui](https://github.com/fullstorydev/grpcui) でサービスの一覧を見て RPC を試せる。`Data` と `Storage` は JWT が要るので、TestClient の `token` で表示したトークンを付ける。

```bash
grpcui -plaintext -rpc-header "authorization: Bearer <token>" localhost:5000
```

双方向ストリーミング(`Chat` / `Notify`)は対話できない(要求をまとめて送り、応答をまとめて表示する)ので、TestClient の `chat` / `notify` で試す。

## 圧縮・DB・ファイル保管

- 応答の圧縮は `Grpc:Compression`(既定 gzip / `Fastest`)。クライアントが `grpc-accept-encoding` で gzip を受け付けるときだけ効く。要求の圧縮はクライアントが `grpc-internal-encoding-request` ヘッダーで指定する
- DB は `ConnectionStrings:Default`(既定 `data.db`)。表は起動時に `Assets/Data/Schema.sql` で作り、移行の仕組みは持たないので、列が変わったら `data.db` を消して作り直す
- ファイルは `Storage:Root`(既定 `storage`)配下に `FileStorage` が保存する(一時ファイルへ書いて原子的に置換)。`..` を含む名前は `Invalid path.`(`InvalidArgument`)

## テスト

```bash
dotnet run --project tests/Template.GrpcServer.IntegrationTests
```

- xunit.v3 + Microsoft Testing Platform(テストのプロジェクトは実行ファイル。`dotnet run` で実行する)
- `IntegrationTests` = `WebApplicationFactory` のテストサーバー(テストクラスごとに一時フォルダーの DB・ストレージ)へ `GrpcChannel` でつなぎ、4 形態の RPC・ページングと一括取得・状態の条件と labels・部分更新と etag・エラーの詳細・認証・期限切れ・要求メトリクス・ファイル転送を確かめる
