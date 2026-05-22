# Browser Module Knowledge Base

**Generated:** 2026-05-23  
**Commit:** cb1269a2  
**Branch:** develop

## OVERVIEW

Selenium-based browser automation module (~26 activities) with shadow DOM support, iframe auto-detection, and retry with exponential backoff.

## STRUCTURE

```
Browser/
├── AlertHandle.*, BrowserOpen.*, ElementClick.* ...  # 26 activities (flat, 3 files each)
├── Services/        # SessionManager, ElementRepository, ElementLocator, BrowserServicesFactory, ActivityLogger
├── Models/          # RetryConfiguration, CookieData, ElementPosition, WindowInfo
├── Interfaces/      # IBrowserServices — 4 small interfaces: ISessionManager, IElementLocator, IElementRepository, IActivityLogger
├── BrowserActivityBase.cs  # 1201-line base class with shadow DOM, iframe, retry
├── BrowserSessionContext.cs  # Ambient context for implicit session ID
└── SessionResolver.cs
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Browser automation activities | `Browser/*Back.cs` (flat, no subdir) |
| Session management | `Services/SessionManager.cs` |
| Element location (Selenium wrapper) | `Services/ElementLocator.cs` |
| Shared Selenium operations | `Common/SeleniumHelper.cs` (root Common/) |
| Ambient context (session ID) | `BrowserSessionContext.cs` |
| Retry configuration | `Models/RetryConfiguration.cs` |

## CONVENTIONS

- **Base classes:** All activities inherit from `BrowserActivityBase<T>` (not `PrimoComponentTO<T>` directly).
- **Execution:** Override `TimedAction()` — get inputs from `sd.Variables`, set outputs via `SetVariableValue()`, return `ExecutionResult`.
- **Services:** Use `BrowserServicesFactory` to resolve `IBrowserServices` (lazy-loaded singletons).
- **Validation:** Use `Guard.*` methods — NOT inline null checks.
- **Shadow DOM:** Access via JavaScript execution through `IElementLocator`.
- **iframe handling:** Auto-detect and switch context automatically.
- **Properties:** Use `Prop_` prefix, backing fields with `InvokePropertyChanged()`, `[StoringProperty]` for serialization.
- **Debug logging:** `Debug.WriteLine` (TODO: migrate to Serilog/NLog).
- **Session ID:** Ambient context via `BrowserSessionContext` — not passed explicitly.

## ANTI-PATTERNS (THIS MODULE)

- **Global static state:** `SessionManager` is a singleton with `lock` — avoid adding more global state.
- **Large monolith files:** `BrowserActivityBase.cs` (1201 lines), `SeleniumHelper.cs` (808 lines) — prefer smaller focused classes.
- **`System.Diagnostics.Debug.WriteLine` for logging:** 4 instances in `BrowserActivityBase.cs` — use Serilog/NLog instead.
- **Hard-coded wait times:** Use exponential backoff retry — don't add fixed `Thread.Sleep`.
- **Direct `new WebDriver()` calls:** Always use `IElementLocator` wrapper.
- **No session disposal:** Always dispose `IWebDriver` via `SessionManager`.
