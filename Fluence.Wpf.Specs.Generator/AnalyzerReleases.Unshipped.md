; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
FLSPEC001 | SpecSurface | Error | Manifest control type not found in the Fluence.Wpf compilation
FLSPEC002 | SpecSurface | Error | Manifest member not found on the control type or its bases
FLSPEC003 | SpecSurface | Error | Manifest member type is incompatible with the control property type
FLSPEC004 | SpecSurface | Error | Manifest enum values drift from the mirrored CLR enum
FLSPEC005 | SpecSurface | Error | SpecSurface.xml manifest missing, unreadable, or invalid
