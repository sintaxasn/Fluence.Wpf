## Summary

- 

## Verification

- [ ] `dotnet build Fluence.Wpf.sln -c Release --no-restore -v minimal`
- [ ] `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Release -f net472 --no-build`
- [ ] `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Release -f net10.0-windows10.0.26100.0 --no-build`
- [ ] Visual pass completed in `Fluence.Wpf.Demo` for Light, Dark, High Contrast, accent swap, and relevant backdrop.

## Checklist

- [ ] Public API changes have XML docs and tests.
- [ ] Template or visual changes use canonical theme keys and `DynamicResource` where theme-bound.
- [ ] `CHANGELOG.md` is updated under `Unreleased`.
- [ ] Public docs are updated when consumer behavior changes.
- [ ] No unrelated files or local tool state are included.
