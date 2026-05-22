# TESTS KNOWLEDGE BASE

**Generated:** 2026-05-23  
**Commit:** cb1269a  
**Branch:** develop

## OVERVIEW

xUnit test project for Primo.MIA class library targeting .NET Framework 4.6.2.

## STRUCTURE

```
Primo.MIA.Tests/
├── Browser/       # Browser activity tests (mirrors Browser/)
├── Calendar/      # Calendar parsing tests (mirrors Calendar/)
├── Common/        # Shared test infrastructure, TestHelpers.cs
├── Config/        # Configuration-related tests
├── Database/      # ADO.NET tests (mirrors Database/)
├── Dictionary/    # Dictionary CRUD tests (mirrors Dictionary/)
├── Excel/         # Excel helpers tests (mirrors Utilities/)
├── Files/         # File operations tests (mirrors Files/)
├── Generators/    # Data generators tests (mirrors Utilities/)
├── Helpers/       # Test Helpers, base classes, utilities
├── HttpWeb/       # HTTP request tests (mirrors HttpWeb/)
├── List/          # List operations tests (mirrors List/)
├── Text/          # Text processing tests (mirrors Text/)
├── Tuple/         # Tuple operations tests (mirrors Tuple/)
├── Utilities/     # Utility tests (mirrors Utilities/)
├── AGENTS.md      # This file
└── Primo.MIA.Tests.csproj
```

## WHERE TO LOOK

| Task | Location |
|------|----------|
| Test infrastructure | `Helpers/TestHelpers.cs` |
| Shared fixtures | `Helpers/*Fixture.cs` |
| Logic tests | `*LogicTests.cs` (not Back.cs) |
| Integration tests | `[Trait("Category", "Integration")]` |
| Run tests | `scripts/run-tests-vs.ps1` |

## CONVENTIONS

- **Naming**: `[Feature]LogicTests.cs` for pure logic `[Feature]Tests.cs` for activity integration
- **Method naming**: `MethodName_Scenario_ExpectedBehavior`
- **AAA pattern**: Use `#region Arrange`, `#region Act`, `#region Assert` comment blocks
- **Traits**: `[Trait("Category", "Fast")]` (default) or `[Trait("Category", "Integration")]`
- **Logic extraction**: Test `*Logic.cs` classes, avoid `*Back.cs` (Primo SDK dependency)
- **Frameworks**: xUnit 2.4.2, FluentAssertions 6.x, Microsoft.NET.Test.Sdk 17.3.2
- **Target**: net462 (WPF/XAML requires full Framework)

## ANTI-PATTERNS

- **Using dotnet test**: Breaks on WPF/XAML dependencies — use `scripts/run-tests-vs.ps1` (MSBuild + vstest.console.exe)
- **Testing Back.cs directly**: Avoid Primo SDK references in tests — prefer Logic.cs
- **Inline assertions**: Use FluentAssertions `Should().Be()` instead of `Assert.AreEqual()`
- **Hidden dependencies**: All test dependencies declared in csproj — no loose references
- **Hard-coded paths**: Test data uses relative paths, no absolute paths
- **Large test files**: Split into `*LogicTests.cs` + `*IntegrationTests.cs` if >200 lines
