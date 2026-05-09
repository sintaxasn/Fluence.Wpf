# Fluence.Wpf - PowerShell demos

Three standalone Windows PowerShell 5.1 scripts showing how to build Fluent-styled WPF UIs without writing C#.

## Requirements

- Windows PowerShell 5.1 (built into Windows - `powershell.exe`, **not** `pwsh.exe`)
- .NET SDK (for the optional auto-build step)
- `Fluence.Wpf.dll` built for `net472` - the scripts build it automatically if missing

## Running a demo

Open a terminal and run with the STA apartment flag:

- powershell.exe -STA -File .\Show-ThemeDemo.ps1
- powershell.exe -STA -File .\Show-ControlsDemo.ps1
- powershell.exe -STA -File .\Show-ProgressDemo.ps1

## What each demo shows

| Script                  | Demonstrates                                                                                              |
|-------------------------|-----------------------------------------------------------------------------------------------------------|
| `Show-ThemeDemo.ps1`    | FluenceWindow + Mica backdrop, Light/Dark/Auto theme switching, accent colour cycling, SystemThemeWatcher |
| `Show-ControlsDemo.ps1` | Button variants, ToggleSwitch, CheckBox, RadioButton, TextBox, NumberBox                                  |
| `Show-ProgressDemo.ps1` | ProgressBar, ProgressRing, InfoBar with a PowerShell-wired click handler                                  |

## How it works

Each script:

1. Checks for `Fluence.Wpf.dll` at `..\Fluence.Wpf\bin\Release\net472\` - builds it with `dotnet build` if absent.
2. Loads WPF assemblies via `Add-Type`.
3. Calls `ApplicationThemeManager.Apply()` to seed Fluence resources.
4. Parses an inline XAML here-string with `XamlReader::Parse()`.
5. Wires event handlers directly in PowerShell.
6. Calls `Window.Show()` and enters the WPF dispatcher loop.

The window icon is loaded from `..\assets\fluence-wpf-appicon-256.ico` when present.
