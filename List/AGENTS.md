# LIST MODULE

**Generated:** 2026-05-23  
**Commit:** cb1269a2

## OVERVIEW

Provides 9 list manipulation activities with pure C# logic extracted into testable classes.

## STRUCTURE

```
List/
├── ListAggregate/      # Aggregate operations (Sum, Count, Min, Max, Average, Join)
├── ListConvert/        # Conversion (ToDict, ToCSV, FromCSV, Zip, Chunk)
├── ListFilter/         # Filtering (Contains, Regex, Length range, Numeric)
├── ListGroup/          # Grouping (First char, Length, Prefix, Regex group, Top frequent)
├── ListInspect/        # Diagnostics (Duplicates, length stats, search indexing)
├── ListSet/            # Set operations (Union, Intersect, Except, Symmetric diff)
├── ListSlice/          # Slicing (First/Last N, Page, Range, Every Nth)
├── ListSort/           # Sorting (Alphabetical, Natural, Length, Random, Reverse)
├── ListTransform/      # Transformation (Case, Trim, Replace, Pad, Truncate)
└── *Logic.cs           # Pure C# logic, no Primo SDK dependency
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Aggregation logic | `ListAggregateLogic.cs` | Sum, Min, Max, Average, Join modes |
| Sorting logic | `ListSortLogic.cs` | NaturalComparer for "file2" < "file10" |
| Filtering logic | `ListFilterLogic.cs` | Regex, Contains, Length, Numeric modes |
| Set operations | `ListSetLogic.cs` | Union, Intersect, Except, Symmetric diff |
| Grouping logic | `ListGroupLogic.cs` | By prefix, regex groups, top frequent |
| Inspection back | `ListInspectBack.cs` | Full diagnostics, no pure logic class |

## CONVENTIONS

- **Logic pattern**: `*Back.cs` (activity) + `*Logic.cs` (pure C# testable logic)
- **Base class**: All activities inherit `PrimoComponentTO<T>` directly
- **Validation**: Use `Guard.*` methods from `Primo.MIA.Common`
- **Properties**: `Prop_` prefix with `InvokePropertyChanged()`, `[StoringProperty]` attribute
- **Execution**: Override `TimedAction()`, read via `GetPropertyValue<T>()`, write via `SetVariableValue()`
- **Case sensitivity**: Use `ComparisonHelper.GetStringComparison()` / `GetStringComparer()` for all string ops
- **Natural sorting**: `NaturalComparer.Instance` enables "file2" < "file10" human-friendly ordering
- **Namespace**: `Primo.MIA` for all List module types
