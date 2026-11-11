# Template project for gRPC server

[Template](https://usausa.github.io/template/) project for gRPC server.

# ✅ Implement

| Category | Feature |
| --- | --- |
| gRPC | Unary / Server streaming / Client streaming / Bidirectional streaming (chat, broadcast) / Reflection / Health checks / Compression (gzip) / Error details (`google.rpc.Status`) |
| Data | SQLite (Smart.Data.Accessor) / Paging, filtering, and sorting / Batch get / Partial update (field mask) / Optimistic concurrency (etag) / File transfer (streaming) |
| Auth | JWT |
| Observability | Serilog / SQL profiler (MiniDataProfiler) / OpenTelemetry (OTLP, Prometheus) / Request metrics |
| Operation | Feature flags / Keepalive ping / Windows service / systemd / Aspire AppHost |
| Client | Test client (retry, deadline, interceptors) |
| Test | Integration (WebApplicationFactory) |

# 📚 Docs

The documents in `docs` are in Japanese.

| Document | Contents |
| --- | --- |
| [test-client.md](docs/test-client.md) | Test client and grpcui |
| [configuration.md](docs/configuration.md) | Compression, database, file storage, and keeping connections |
| [tests.md](docs/tests.md) | Tests |

# 🔗 Related Projects

| Project | Description |
| --- | --- |
| [template-web-api](https://github.com/usausa/template-web-api) | Web API template (Minimal API) |
| [template-web-fullstack](https://github.com/usausa/template-web-fullstack) | Full-stack template |
| [template-web-mvc](https://github.com/usausa/template-web-mvc) | ASP.NET Core MVC template with Identity and passkeys |
| [template-blazor-server](https://github.com/usausa/template-blazor-server) | Blazor Server application template |
| [template-blazor-wasm](https://github.com/usausa/template-blazor-wasm) | Blazor WebAssembly + ASP.NET Core API template |

# 📦 My Libraries

| Library | Description |
| --- | --- |
| [Usa.Smart.Data.Accessor](https://github.com/usausa/Smart-Net-Data-Accessor) | Source generator based data accessor |
| [Usa.Smart.CommandLine.Hosting](https://github.com/usausa/Smart-Net-CommandLine) | System.CommandLine hosting |
| [MiniDataProfiler](https://github.com/usausa/mini-data-profiler) | SQL profiler |
| [BunnyTail.DependencyInjection](https://github.com/usausa/bunnytail-dependency-injection) | Source generator based DI |
