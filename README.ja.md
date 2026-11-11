# Template project for gRPC server

[English](README.md) | 日本語

[Template](https://usausa.github.io/template/) project for gRPC server.

# ✅ 実装

| 分類 | 機能 |
| --- | --- |
| gRPC | 単項 / サーバーストリーミング / クライアントストリーミング / 双方向ストリーミング(チャット・配信) / リフレクション / ヘルスチェック / 圧縮(gzip) / エラー詳細(`google.rpc.Status`) |
| データ | SQLite(Smart.Data.Accessor) / ページング・絞り込み・並べ替え / 一括取得 / 部分更新(フィールドマスク) / 楽観的同時実行制御(etag) / ファイル転送(ストリーミング) |
| 認証 | JWT |
| 監視 | Serilog / SQL プロファイラー(MiniDataProfiler) / OpenTelemetry(OTLP・Prometheus) / 要求メトリクス |
| 運用 | 機能フラグ / キープアライブ ping / Windows サービス / systemd / Aspire AppHost |
| クライアント | 動作確認用クライアント(リトライ・期限・インターセプター) |
| テスト | 結合(WebApplicationFactory) |

# 📚 ドキュメント

| ドキュメント | 内容 |
| --- | --- |
| [test-client.md](docs/test-client.md) | 動作確認用クライアントと grpcui |
| [configuration.md](docs/configuration.md) | 圧縮・DB・ファイル保管・接続の保ち方 |
| [tests.md](docs/tests.md) | テスト |

# 🔗 関連プロジェクト

| プロジェクト | 説明 |
| --- | --- |
| [template-web-api](https://github.com/usausa/template-web-api) | Web API のテンプレート(Minimal API) |
| [template-web-fullstack](https://github.com/usausa/template-web-fullstack) | フルスタック構成のテンプレート |
| [template-web-mvc](https://github.com/usausa/template-web-mvc) | ASP.NET Core MVC のテンプレート(Identity・パスキー) |
| [template-blazor-server](https://github.com/usausa/template-blazor-server) | Blazor Server アプリのテンプレート |
| [template-blazor-wasm](https://github.com/usausa/template-blazor-wasm) | Blazor WebAssembly + ASP.NET Core API のテンプレート |

# 📦 自作ライブラリ

| ライブラリ | 説明 |
| --- | --- |
| [Usa.Smart.Data.Accessor](https://github.com/usausa/Smart-Net-Data-Accessor) | データアクセス |
| [Usa.Smart.CommandLine.Hosting](https://github.com/usausa/Smart-Net-CommandLine) | System.CommandLine ホスティング |
| [MiniDataProfiler](https://github.com/usausa/mini-data-profiler) | SQL プロファイラー |
| [BunnyTail.DependencyInjection](https://github.com/usausa/bunnytail-dependency-injection) | DI |
