# HttpWeb Module

## OVERVIEW

HTTP automation with 8 activities: basic requests, batch parallel execution, downloads, uploads, retry with linear/exponential backoff, OAuth2 (ClientCredentials/Password/AuthorizationCode), refresh tokens, and webhook send/receive via HttpListener.

## STRUCTURE

```
HttpWeb/
├── HttpActivityBase.cs     (259 lines) - HttpClient/SSL/cert management
├── HttpHelper.cs           (424 lines) - Header parsing, retry logic, file utilities
├── HttpLogic.cs            (144 lines) - Testable request logic
├── HttpBack.cs             (~500 lines) - Basic GET/POST/PUT/DELETE/PATCH
├── HttpBatchBack.cs        (~600 lines) - Parallel requests with SemaphoreSlim
├── HttpDownloadBack.cs     (~400 lines) - File download with progress
├── HttpUploadBack.cs       (~400 lines) - Multipart/form-data upload
├── HttpRetryBack.cs        (~500 lines) - Configurable retry strategy
├── HttpOAuth2TokenBack.cs  (~450 lines) - OAuth2 grants
├── HttpOAuth2RefreshBack.cs (~300 lines) - Token refresh
├── WebhookBack.cs          (~350 lines) - POST with template substitution
├── WebhookListenBack.cs    (~600 lines) - HttpListener on local port
└── XAML files for each activity (~20 total)
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Base HTTP logic | `HttpActivityBase.cs`, `HttpHelper.cs`, `HttpLogic.cs` | HttpClient creation, cert/SSL handling, header parsing |
| Basic request | `HttpBack.cs` | All HTTP methods, headers, body, timeout |
| Batch parallel | `HttpBatchBack.cs` | `SemaphoreSlim` throttling, table input, `StopOnError` |
| OAuth2 | `HttpOAuth2TokenBack.cs`, `HttpOAuth2RefreshBack.cs` | Three grants, token refresh, cache |
| Retry logic | `HttpRetryBack.cs` | Fixed/Linear/Exponential, configurable status codes |
| Webhook send | `WebhookBack.cs` | Template variable substitution via Regex.Replace |
| Webhook receive | `WebhookListenBack.cs` | `HttpListener` with secret validation, single request |
| Retry delay | `HttpHelper.CalculateRetryDelay()` | Strategy enum switch: Fixed/Linear/Exponential |
| File MIME type | `HttpHelper.GetMimeType()` | Extension → MIME dictionary |

## CONVENTIONS

- **Base class**: `HttpActivityBase<T>` inherits `PrimoComponentTO<T>`. Manages HttpClient creation, client certificates (.pfx), and SSL error ignoring.
- **Default timeout**: 100 seconds (100000 ms) — overridden in each activity's `sdkTimeOut`.
- **Properties**: Use `Prop_` prefix with `[StoringProperty]` and `InvokePropertyChanged()`. XAML bind to `Prop_` names.
- **Validation**: Use `Guard.*` from `Primo.MIA.Common`. Certificate path validation checks existence and `.pfx` extension.
- **Headers**: Parse JSON input (`{}` default), serialize response headers to JSON output.
- **Retry**: `ShouldRetry()` checks status code against configurable list (default: 500,502,503,504,408,429).
- **Webhook template**: Case-insensitive regex `{$VAR}` substitution (e.g., `{$userId}` → actual value).
- **WebhookListen**: Requires `netsh urlacl` registration for non-localhost prefixes on Windows; one-request per execution.

## ANTI-PATTERNS (THIS MODULE)

- **Hard-coded port in UrlAcl**: `netsh http add urlacl` examples reference fixed ports — configure per environment.
- **Global state in HttpRetryBack**: Retry history stored in `RepoDict` instead of pass-through variables — avoid adding more global state.
- **No activity configuration UI**: All retry/OAuth2 settings exposed via XAML properties, not configuration files.
- **No connection pooling customization**: HttpClient uses default pooling — configurable pool size not exposed.
- **Regex.Replace for templates**: `WebhookBack` uses `Regex.Replace(..., RegexOptions.IgnoreCase)` — safe for known templates, avoid untrusted input.
