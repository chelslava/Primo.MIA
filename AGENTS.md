# PROJECT KNOWLEDGE BASE

**Generated:** 2026-05-23
**Commit:** cb1269a2
**Branch:** develop

## OVERVIEW

Primo.MIA — .NET Framework 4.8.1 class library (WPF/XAML) for the Primo RPA platform. Provides ~134 reusable automation activities (Browser, Database/Redis/MongoDB, HTTP, MQ, SFTP, FTP, Collections, Text, Calendar, Files, Archive, Crypto, Utilities). Distributed as a NuGet package.

## STRUCTURE

```
Primo.MIA/
├── Browser/          # Selenium-based browser automation (30 activities)
├── Calendar/         # Production calendar parsing (CSV/JSON/XML/TXT)
├── Common/           # Shared utilities (Guard, helpers, converters)
├── Database/         # ADO.NET database operations (15 activities)
├── Dictionary/       # Dictionary CRUD operations
├── Files/            # File search, wait, cleanup
├── HttpWeb/          # HTTP requests, OAuth2, webhooks (9 activities)
├── Json/             # JSON parse + query
├── List/             # List operations (filter, sort, aggregate, etc.)
├── Primo.MIA.Tests/  # xUnit tests (mirrors main structure)
├── Text/             # Parse, template, transliterate
├── Tuple/            # Tuple operations (create, zip, sort, etc.)
├── Utilities/        # Misc: generators, log, DataTable, Excel, TOML config
├── Xml/              # XML parse + query
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Browser automation | `Browser/*Back.cs` | Selenium WebDriver, shadow DOM, iframe |
| Database queries | `Database/*Back.cs` | ADO.NET, transactions, bulk ops |
| HTTP/API calls | `HttpWeb/*Back.cs` | RestSharp, OAuth2, webhook listener |
| List operations | `List/*.cs` | Filter, sort, group, transform, aggregate |
| Collection logic | `*/Logic.cs` | Testable logic extracted from activities |
| Shared helpers | `Common/` | Guard, FileHelper, StringHelper |
| Enums & constants | `Enums/*.cs`, `Common/ActivityStrings.cs` | Per-module enum files |
| Test infrastructure | `Primo.MIA.Tests/` | xUnit, mirrors module structure |
| XAML styles | `Common/Styles.xaml` | Centralized WPF styles |
| NuGet packaging | SDK csproj (GeneratePackageOnBuild) | No manual nuspec |

## CONVENTIONS

- **Activity pattern**: 3 files per activity: `*.xaml` (UI) + `*.xaml.cs` (code-behind) + `*Back.cs` (logic). Some also have `*Logic.cs` for testable logic extraction.
- **Base classes**: Browser activities → `BrowserActivityBase<T>`; HTTP activities → `HttpActivityBase<T>`; others → `PrimoComponentTO<T>` directly.
- **Validation**: Use `Guard.*` methods from `Primo.MIA.Common` — NOT inline null checks.
- **Properties**: Use `Prop_` prefix, backing fields with `InvokePropertyChanged()`, `[StoringProperty]` attribute for serialization.
- **Execution**: Override `TimedAction()` — get inputs from `sd.Variables`, set outputs via `SetVariableValue()`, return `ExecutionResult`.
- **XAML**: Use `{StaticResource}` from `Common/Styles.xaml`. For enum-dependent visibility: `EnumToVisibilityConverter` / `MultiEnumToVisibilityConverter` with pipe-separated params.
- **Testing**: xUnit, FluentAssertions, AAA pattern with `#region` blocks, trait categories (`"Fast"`, `"Integration"`). Tests touching real external systems (browser, DB, network) MUST carry `[Trait("Category", "Integration")]` — standard CI excludes them; they run in the separate `integration.yml` workflow (manual/weekly). Pure-logic scenario tests stay `"Fast"` even if named `Scenario_*`.
- **Namespaces**: `Primo.MIA.{Module}` pattern. All XAML uses `xmlns:local="clr-namespace:Primo.MIA"`.
- **Documentation**: Russian XML comments on public APIs. Russian UI labels in XAML.

## ANTI-PATTERNS (THIS PROJECT)

- **Generic `Exception` throwing**: Use specific exception types. Calendar module has `CalendarParseException` as a model. (Audit 1.4.0 purged all generic throws; `ArgumentException` from raw `throw` in `*Back.cs` is CI-warned — use `Guard.*`.)
- **~~`Console.WriteLine` for logging~~ FIXED (1.4.0–1.6.0)**: logging goes through `Common/ActivityFileLogger` (env-gated file logger) and platform `AddToLog`. The only remaining call site is `ActivityFileLogger.cs` itself — its own emergency fallback when file logging fails.
- **Debug artifacts in source**: `plans/` directory is gitignored — don't add planning documents to tracked code.
- **Hard-coded absolute paths**: `C:\Program Files\Primo\Primo Studio Community x64\` HintPath in csproj — intentional: CI installs the LTools SDK bundle to that location before build.
- **Global static state**: `RepoDict` is a global dictionary — avoid adding more global state. Assumes **one robot per process** (Primo Studio limitation); ambient context (browser sessions, DB transactions) is NOT isolated between parallel robot runs in a shared host.
- **Large monolith files**: `DatabaseHelper.cs` (~1120 lines), `TextExtractEntitiesBack.cs` (~1010), `ReadTomlConfigBack.cs` (~980) — prefer smaller focused classes; DatabaseHelper is partially split into `Database*Helper.cs` already.
- **No explicit activity registration**: The LTools SDK auto-discovers activities via reflection — don't add manual registration.

## UNIQUE STYLES

- **`*Back.cs` suffix**: Naming convention for activity backend logic (not ViewModel/Service/Presenter).
- **`RepoDict`**: Static global dictionary for ambient context storage (11 typed dictionaries + generic `ObjectDict`).
- **Unified activity UI (`Common/Styles.xaml`)**: all XAMLs merge the shared dictionary. Canonical keys — converters `EnumToVisConverter` / `MultiEnumToVisConverter` / `BoolToVis` / `EnumDisplayNameConverter` (no local re-declarations, no long aliases); styles `ActivityHeader`, `FieldLabel`, `FieldBox`, `ValidatingFieldBox`, `FieldComboBox`, `FieldCheckBox` / `DangerCheckBox` (orange = destructive), `FieldPasswordBox`, `BrowseButton` / `TextButton`, `HintText`; brushes `TextBrush` (#FF2C3E50), `AccentBrush` (#FF1565C0 mode accent), `DangerBrush` (#FFC55A11). Header pattern: `«Модуль: Название [Режим]»` with accent mode-Run. New activities: zero inline colors/fonts — styles only.
- **Safe helpers**: `SafeDispose.Quiet` (disposal), `SafeTry.Result` (fallback value + Debug log) — replace anonymous `catch { }` in new code.
- **`BrowserSessionContext`**: Ambient context pattern for implicit session ID propagation. `Pop(sessionId)` removes a specific session (issue #20), not the stack top. `BrowserSessionRegistry` TTL-evicts abandoned browser sessions (24h) on each new session registration — same pattern as `DatabaseTransactionManager`.
- **Bug tracking in comments**: Calendar module uses `[БАГ-N]` / `[FIX-N]` tags directly in code comments.
- **Dual dictionary pattern**: `ProductionCalendar` uses forward + reverse dictionaries for O(1) lookup instead of O(n) `Any()`.

## COMMANDS

```bash
# Build (requires Visual Studio or MSBuild with .NET Framework 4.8.1; LTools SDK DLLs in C:\Program Files\Primo\)
msbuild Primo.MIA.csproj /p:Configuration=Debug

# Run tests
msbuild Primo.MIA.Tests\Primo.MIA.Tests.csproj /p:Configuration=Debug
# Or use: scripts\run-tests-vs.ps1 (PowerShell)

# Build NuGet package (auto-generated during build to bin\Debug\*.nupkg)
```

## NOTES

- **.NET Framework 4.8.1 only** — no .NET Core/.NET 5+ support. Tests target `net481`.
- **External SDK dependency**: Requires Primo Studio (`LTools.*.dll`) installed at `C:\Program Files\Primo\` — not available via NuGet.
- **CI**: GitHub Actions (`.github/workflows/ci.yml`) — windows-latest build+tests; LTools SDK DLLs come from the private repo `chelslava/primo-ltools-sdk` (secret `PRIMO_SDK_TOKEN`).
- **Russian locale**: Most UI strings and XML comments are in Russian. README is in Russian.
- **Not an application**: This is a class library loaded by Primo Studio — no `Main()` entry point.

## VERSIONING

- **Single source of truth**: `<Version>X.Y.Z</Version>` in `Primo.MIA.csproj`. `AssemblyVersion`/`FileVersion` derive from it via `$(Version)` — never hardcode them. `InformationalVersion` additionally embeds the commit SHA.
- **SemVer**: MAJOR — breaking API changes; MINOR — new activities/features; PATCH — bugfixes.
- **Release procedure**: (1) add a `Что нового в версии X.Y.Z` section to `CHANGELOG.md`; (2) bump `<Version>` in csproj; (3) commit; (4) `git tag vX.Y.Z && git push <remote> vX.Y.Z` — the tag triggers `release.yml`, which builds with the tag version (overriding csproj) and publishes to nuget.org via trusted publishing (OIDC: `NuGet/login@v1` + policy in the nuget.org profile; job needs `id-token: write`; secret `NUGET_USER` = nuget.org profile name, not email). Release fails without a CHANGELOG section for the tagged version.
- **CI guard**: `ci.yml` fails if `AssemblyVersion`/`FileVersion` are hardcoded (must use `$(Version)`) and warns when CHANGELOG lacks a section for the current csproj version.
