# PROJECT KNOWLEDGE BASE

**Generated:** 2026-05-23
**Commit:** cb1269a2
**Branch:** develop

## OVERVIEW

Primo.MIA — .NET Framework 4.8.1 class library (WPF/XAML) for the Primo RPA platform. Provides ~90 reusable automation activities (Browser, Database, HTTP, Collections, Text, Calendar, Files, Utilities). Distributed as a NuGet package.

## STRUCTURE

```
Primo.MIA/
├── Browser/          # Selenium-based browser automation (26 activities)
├── Calendar/         # Production calendar parsing (CSV/JSON/XML/TXT)
├── Common/           # Shared utilities (Guard, helpers, converters)
├── Database/         # ADO.NET database operations (16 activities)
├── Dictionary/       # Dictionary CRUD operations
├── Files/            # File search, wait, cleanup
├── HttpWeb/          # HTTP requests, OAuth2, webhooks (8 activities)
├── Json/             # JSON parse + query
├── List/             # List operations (filter, sort, aggregate, etc.)
├── Primo.MIA.Tests/  # xUnit tests (mirrors main structure)
├── Text/             # Parse, template, transliterate
├── Tuple/            # Tuple operations (create, zip, sort, etc.)
├── Utilities/        # Misc: generators, log, DataTable, Excel, TOML config
├── Xml/              # XML parse + query
└── Properties/       # AssemblyInfo
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
| Enums & constants | `Enums.cs`, `Common/ActivityStrings.cs` | 2055-line enums file |
| Test infrastructure | `Primo.MIA.Tests/` | xUnit, mirrors module structure |
| XAML styles | `Common/Styles.xaml` | Centralized WPF styles |
| NuGet packaging | `Primo.MIA.nuspec` | Auto-generates on build |

## CONVENTIONS

- **Activity pattern**: 3 files per activity: `*.xaml` (UI) + `*.xaml.cs` (code-behind) + `*Back.cs` (logic). Some also have `*Logic.cs` for testable logic extraction.
- **Base classes**: Browser activities → `BrowserActivityBase<T>`; HTTP activities → `HttpActivityBase<T>`; others → `PrimoComponentTO<T>` directly.
- **Validation**: Use `Guard.*` methods from `Primo.MIA.Common` — NOT inline null checks.
- **Properties**: Use `Prop_` prefix, backing fields with `InvokePropertyChanged()`, `[StoringProperty]` attribute for serialization.
- **Execution**: Override `TimedAction()` — get inputs from `sd.Variables`, set outputs via `SetVariableValue()`, return `ExecutionResult`.
- **XAML**: Use `{StaticResource}` from `Common/Styles.xaml`. For enum-dependent visibility: `EnumToVisibilityConverter` / `MultiEnumToVisibilityConverter` with pipe-separated params.
- **Testing**: xUnit, FluentAssertions, AAA pattern with `#region` blocks, trait categories (`"Fast"`, `"Integration"`).
- **Namespaces**: `Primo.MIA.{Module}` pattern. All XAML uses `xmlns:local="clr-namespace:Primo.MIA"`.
- **Documentation**: Russian XML comments on public APIs. Russian UI labels in XAML.

## ANTI-PATTERNS (THIS PROJECT)

- **Generic `Exception` throwing**: Use specific exception types. Calendar module has `CalendarParseException` as a model.
- **`System.Diagnostics.Debug.WriteLine` for logging**: Three TODO instances in `BrowserActivityBase`. Use Serilog/NLog instead.
- **Debug artifacts in source**: `plans/` directory is gitignored — don't add planning documents to tracked code.
- **Hard-coded absolute paths**: `C:\Program Files\Primo\Primo Studio Community x64\` in csproj — breaks cross-platform builds.
- **Global static state**: `RepoDict` is a global dictionary — avoid adding more global state.
- **Large monolith files**: `DatabaseHelper.cs` (2010 lines), `BrowserActivityBase.cs` (1201 lines) — prefer smaller focused classes.
- **No explicit activity registration**: The LTools SDK auto-discovers activities via reflection — don't add manual registration.

## UNIQUE STYLES

- **`*Back.cs` suffix**: Naming convention for activity backend logic (not ViewModel/Service/Presenter).
- **`RepoDict`**: Static global dictionary for ambient context storage (11 typed dictionaries + generic `ObjectDict`).
- **`BrowserSessionContext`**: Ambient context pattern for implicit session ID propagation.
- **Bug tracking in comments**: Calendar module uses `[БАГ-N]` / `[FIX-N]` tags directly in code comments.
- **Dual dictionary pattern**: `ProductionCalendar` uses forward + reverse dictionaries for O(1) lookup instead of O(n) `Any()`.

## COMMANDS

```bash
# Build (requires Visual Studio or MSBuild with .NET Framework 4.6.1)
msbuild Primo.MIA.csproj /p:Configuration=Debug

# Run tests
msbuild Primo.MIA.Tests\Primo.MIA.Tests.csproj /p:Configuration=Debug
# Or use: scripts\run-tests-vs.ps1 (PowerShell)

# Build NuGet package (auto-generated during build to bin\Debug\*.nupkg)
```

## NOTES

- **.NET Framework 4.6.1 only** — no .NET Core/.NET 5+ support. Tests target `net462`.
- **External SDK dependency**: Requires Primo Studio (`LTools.*.dll`) installed at `C:\Program Files\Primo\` — not available via NuGet.
- **No CI/CD**: No GitHub Actions, no Makefile, no Docker. Builds are local/developer-machine-only.
- **Russian locale**: Most UI strings and XML comments are in Russian. README is in Russian.
- **Not an application**: This is a class library loaded by Primo Studio — no `Main()` entry point.
