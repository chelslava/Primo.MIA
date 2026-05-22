## 1. Language & Framework Version

- **Target Framework:** .NET Framework 4.6.1  
- **C# Language Version:** Prefer **C# 6.0** features.  
  - C# 7.x features that do not require runtime changes (e.g., `out var`, pattern matching with `is`, tuples, local functions) may be used **only if** the project explicitly sets `<LangVersion>7.3</LangVersion>` and you have verified they are supported in the target environment.  
  - Avoid features that rely on new runtime types (e.g., `Span<T>`, `ValueTask<T>`) unless you are certain they are available or you are adding appropriate NuGet packages (e.g., `System.Memory`).

- **Do not** use .NET Core / .NET 5+ APIs that are not part of the .NET Framework 4.6.1 base class library. If you need newer functionality, search for a compatible NuGet package first.

---

## 2. Naming & Coding Conventions

Follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) with these additions:

- **Classes, Methods, Properties, Events:** PascalCase  
- **Local variables, parameters, private fields:** camelCase  
- **Private instance fields:** prefix with `_` (e.g., `_customerService`)  
- **Constants:** PascalCase (e.g., `MaxRetryCount`)  
- **Interfaces:** Prefix with `I` (e.g., `IRepository`)  
- **Use** `var` when the type is obvious from the right-hand side; otherwise use explicit type.  
- **Braces:** Always use braces `{ }` for single-line statements. Place opening brace on a new line (Allman style).  
- **Access modifiers:** Explicitly specify visibility (even `private`).