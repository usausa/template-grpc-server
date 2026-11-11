# 設定

## 圧縮・DB・ファイル保管

- 応答の圧縮は `Grpc:Compression`(既定 gzip / `Fastest`)。クライアントが `grpc-accept-encoding` で gzip を受け付けるときだけ効く。要求の圧縮はクライアントが `grpc-internal-encoding-request` ヘッダーで指定する
- DB は `ConnectionStrings:Default`(既定 `data.db`。Host は起動時に作業フォルダーを出力のフォルダー `bin/<構成>/net10.0` にするので、そこにできる)。表は起動時に `Assets/Data/Schema.sql` で作り、移行の仕組みは持たないので、列が変わったら `data.db` を消して作り直す
- ファイルは `Storage:Root`(既定 `storage`)配下に `FileStorage` が保存する(一時ファイルへ書いて原子的に置換)。`..` を含む名前は `Invalid path.`(`InvalidArgument`)

## 接続の保ち方

- サーバーは `appsettings.json` の `Kestrel:Limits`。`Http2:KeepAlivePingDelay`(既定 30 秒)の間に何も届かなければ ping を送り、`Http2:KeepAlivePingTimeout`(20 秒)のうちに応答が無ければ接続を切る(項目を消すと ping を送らない)。`KeepAliveTimeout`(2 分 10 秒)は要求の無い接続を閉じるまで
- TestClient は `appsettings.json` の `Client:Connection`。`ConnectTimeout`(5000 ミリ秒)のうちにつながらなければ `Unavailable` になり、`Client:Retry` のとおり再試行する。`KeepAlivePingDelay`(30000。0 で送らない)と `KeepAlivePingTimeout`(20000)は、呼び出しの途中に何も届かないときの ping
- ping の間隔は、途中のロードバランサーのアイドル(AWS NLB は 350 秒で変えられない)より短くする。長く開く `Notify` / `Chat` がアイドルで切られなくなる
