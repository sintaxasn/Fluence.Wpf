# Known Issues

Track gaps and follow-ups that are **not** fixed in the rename/documentation release.

- [ ] **XML documentation coverage** — `Fluence.Wpf.csproj` suppresses CS1591/CS1574 while public API docs are expanded. Remove `<NoWarn>` entries once every public member has `///` summaries and valid `cref` targets.
- [ ] **TabView** — WinUI `TabView` is not implemented; `TabControl` receives Fluent styling only.
- [ ] **Repo folder name** — If the repository root still uses a temporary working-folder name, rename it to `Fluence.Wpf` before publishing to GitHub.
