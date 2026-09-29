# Embedding server (Qwen3-Embedding-0.6B)

Runs the real embedding model behind Search's `IEmbeddingClient` port
([Search/Domain/Ports/IEmbeddingClient.cs](../Domain/Ports/IEmbeddingClient.cs)) - an
OpenAI-compatible `/v1/embeddings` endpoint that turns product text into the vectors stored in
`ProductEmbeddingQwen3` (pgvector) and used for similarity search.

Not currently wired into `Search.Api`'s dependency injection - `Search/Api/Program.cs` registers
a deterministic `BagOfWordsEmbeddingClient` stand-in instead (see the comment there), because the
real Qwen3 HTTP client was scoped out when Search was split off. The existing `ProductEmbeddingQwen3`
data was produced by running this server directly against Catalog's product text, outside the
.NET solution. Wiring a real `Qwen3EmbeddingClient : IEmbeddingClient` (calling this server over
HTTP) and adding it to `docker-compose.yml` is the natural next step if/when that stand-in needs
replacing - not done here.

Two builds of the same model - **CPU is the one you'll use most of the time**:

- `Dockerfile.embed.cpu` - plain `llama-cpp-python[server]` CPU wheel. Works everywhere, no GPU
  needed.
- `Dockerfile.embed.gpu` - CUDA, llama.cpp's native `llama-server` binary compiled from source.
  Only worth it with a real GPU available - see the comment at the top of the file for why the
  simpler CPU-style approach doesn't work for GPU embeddings (the Python wrapper never actually
  dispatches `/v1/embeddings` compute to the GPU, despite reporting it as active).

Both Dockerfiles, and the reasoning behind them, originate from `D:\AI-200\Demos\ContainerHost`
(a general-purpose local-LLM container demo, also covering an unrelated chat model) - copied here
so they live alongside the bounded context that actually depends on them.

## Setup

**Download** the model into this folder (same file both Dockerfiles expect):
[Qwen3-Embedding-0.6B-Q8_0.gguf](https://huggingface.co/Qwen/Qwen3-Embedding-0.6B-GGUF/blob/main/Qwen3-Embedding-0.6B-Q8_0.gguf)

**Build (CPU):**
```
docker build -f Dockerfile.embed.cpu -t webshop-embed:cpu .
```

**Build (GPU):**
```
docker build -f Dockerfile.embed.gpu -t webshop-embed:gpu .
```

**Run:**
```
docker run -d -p 8005:8000 --cpus="4" --memory="4g" --name webshop-embed webshop-embed:cpu
```
(swap in `webshop-embed:gpu` and add `--gpus all` for the GPU build)

**Test:**
```
curl http://localhost:8005/v1/embeddings -H "Content-Type: application/json" -d "{\"input\": \"hello world\"}"
```

## OpenTelemetry

Only `Dockerfile.embed.cpu` is instrumented - it runs `opentelemetry-instrument` in front of
`python -m llama_cpp.server`, which auto-detects that it's a FastAPI/Starlette app and adds
tracing/logging without touching `llama_cpp.server`'s own source. `Dockerfile.embed.gpu` can't get
the same treatment (no Python process at all - see the comment at the top of that file); calls
into it are still visible as client-side spans from Search.Api's own HTTP client instrumentation,
just without a matching server-side span.

Point it at a collector the same way as every .NET service - `OTEL_EXPORTER_OTLP_ENDPOINT`
(`docker-compose.yml` sets this to `http://otel-collector:4318`, note the HTTP port 4318, not
gRPC's 4317 that the .NET services use - `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf` is baked
into the Dockerfile). With no collector reachable, it just logs failed export attempts every
export interval rather than failing to serve requests.
