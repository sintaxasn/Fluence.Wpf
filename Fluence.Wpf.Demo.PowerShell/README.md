# Fluence.Wpf.Demo.PowerShell

This folder contains a self-contained Windows PowerShell 5.1 host for the `net472` Fluence.Wpf DLL. It demonstrates loading WPF assemblies, loading `Fluence.Wpf.dll`, applying Fluence resources, parsing XAML with `XamlReader`, enabling `SystemThemeWatcher`, and showing a non-modal `FluenceWindow` while the dispatcher pumps events.

## What Lives Here

- `Fluence.Wpf.dll` - copied from the current `Fluence.Wpf/bin/Debug/net472` output.
- `MainWindow.xaml` - the demo `FluenceWindow` and controls loaded at runtime.
- `Show-FluenceDemo.ps1` - the Windows PowerShell 5.1/STA launcher with console logging and a `-SmokeTest` path.

## Run

From the repository root:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\Fluence.Wpf.Demo.PowerShell\Show-FluenceDemo.ps1
```

For a non-interactive load check:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\Fluence.Wpf.Demo.PowerShell\Show-FluenceDemo.ps1 -SmokeTest
```

## Maintenance Notes

Refresh `Fluence.Wpf.dll` after rebuilding the `net472` library if the PowerShell demo must reflect new library changes. Keep the launcher on Windows PowerShell 5.1 with `-STA`; WPF and `XamlReader` are not intended to run from a non-STA PowerShell host.
