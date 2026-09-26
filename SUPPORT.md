# Support

Fluence.Wpf is a WPF control library that recreates the Windows 11 Fluent / WinUI 3 visual language. Use these paths for help.

## Read the docs first

The full documentation lives in the Markdown guides under [`docs/`](docs/):

- [Getting started](docs/getting-started.md) - reference the library, the startup `ApplicationThemeManager.Apply(...)` call, and a local pack.
- [Theming](docs/theming.md) - merge slots, accent ramp, backdrop, and the `SystemThemeWatcher`.
- [Controls](docs/controls.md) - the control catalog, aligned with the demo gallery.
- [PowerShell module](docs/powershell/README.md) - the primary scripting interface for dialogs, prompts, progress and full windows on Windows PowerShell 5.1 and PowerShell 7.4 or later.
- [Migration guide](docs/migration-guide.md) - moving from other Fluent-style WPF stacks.

Before filing anything, also check [KNOWN_ISSUES.md](KNOWN_ISSUES.md) for deliberate non-features and tracked follow-ups, and the current [CHANGELOG.md](CHANGELOG.md) for recent fixes.

## See it running

Most "how do I do X" questions are answered fastest by the demos:

- **Gallery** (`Fluence.Wpf.Demo`) - every control, theme, accent, and backdrop, with embedded XAML/C# source per example. Run `dotnet run --project Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj`.
- **MVVM Task Manager** (`Fluence.Wpf.Demo.Mvvm`) - `FluenceWindow` plus CommunityToolkit.Mvvm with no page code-behind.
- **PowerShell module examples** ([`Fluence.Wpf.PowerShell.Module/examples/`](Fluence.Wpf.PowerShell.Module/examples/)) - runnable scripts for Windows PowerShell 5.1 and PowerShell 7.4 or later. Follow the [module tutorial](docs/powershell/tutorial.md) to build or install the module first.

## Ask a question or report a problem

- **Bug reports**: open a GitHub issue using the Bug report template. Include the target framework (`net472`, `net8.0-windows10.0.26100.0`, or `net10.0-windows10.0.26100.0`), Windows build, the theme / accent / backdrop in effect, a minimal reproduction, and whether it also reproduces in the gallery demo. Attach screenshots for visual bugs (Light, Dark, or High Contrast as relevant).
- **Feature requests**: open a GitHub issue using the Feature request template. Describe the WinUI 3 control or behavior you want and cite WinUI CommonStyles, .NET WPF theme sources, or in-tree precedent where possible.
- **Security reports**: do not open a public issue. Follow [SECURITY.md](SECURITY.md).

Questions are welcome as issues as well; check the docs map above first so the answer is not already written down.
