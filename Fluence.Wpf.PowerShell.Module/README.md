# Fluence.Wpf.PowerShell

Declarative Fluent/Windows 11 dialogs for PowerShell, built on the [Fluence.Wpf](../README.md) control library.

Write a spec - prompts, buttons, an optional icon - and get a fully themed WPF dialog back in one call. No WPF project, no XAML, no C#.

---

## Requirements

- Windows PowerShell 5.1 (`powershell.exe`) or PowerShell 7+ (`pwsh`)
- Windows 10 1809 or later
- The native libraries staged by `build\Build-Module.ps1` (see Import below)

---

## Import

Stage the native Fluence.Wpf libraries first, then import the module:

```powershell
# Stage net472 (and net8.0-windows for PS7) libraries into the module output tree.
pwsh -File build\Build-Module.ps1

# Import from the staged output.
Import-Module .\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1 -Force
```

After staging, the module is self-contained: import it from any script with the path to `Fluence.Wpf.PowerShell.psd1`.

---

## Quick start

### Level 1 - one-liner message

```powershell
Show-FluenceMessage -Message 'Install complete.' -Icon Success
```

### Level 2 - quick single input

```powershell
$name = Get-FluenceInput -Message 'Your name?'
```

### Level 3 - full declarative form

```powershell
$r = Show-FluenceDialog -Title 'Sign in' -Prompts @(
    New-FluencePrompt -Name User -Message 'Account'  -InputType Text     -ValidateNotEmpty
    New-FluencePrompt -Name Pass -Message 'Password' -InputType Password -ValidateNotEmpty
) -Buttons (New-FluenceButton -Text 'Login' -IsDefault), 'Cancel'

if ($r.Login) { Write-Output "Signed in as: $($r.User)" }
```

---

## Commands

| Command | Description |
|---|---|
| `Show-FluenceDialog` | Show a dialog built from prompt and button specs; returns a `Fluence.DialogResult` object. |
| `Show-FluenceMessage` | Show a message or confirmation dialog; returns the clicked button name as a string. |
| `Get-FluenceInput` | Show a single-prompt dialog; returns the captured value, or `$null` on cancel. |
| `New-FluencePrompt` | Build a prompt specification (label, input type, validation) for use with `Show-FluenceDialog`. |
| `New-FluenceButton` | Build a button specification (caption, default/cancel flags) for use with `Show-FluenceDialog`. |

---

## Theming

Dialogs render using the Fluence.Wpf theme engine: Auto theme (follows Windows light/dark), system accent color, and Mica backdrop by default. Pass `-Theme`, `-Backdrop`, or `-Accent` to `Show-FluenceDialog` to override.

---

## Examples

The `examples\` directory contains four ready-to-run scripts:

| Script | What it shows |
|---|---|
| `QuickStart.ps1` | One-liner success message |
| `Message.ps1` | YesNo confirmation with branch logic |
| `SignIn.ps1` | Account and password form with validation |
| `Form.ps1` | Mixed form: Text, Number, Choice, Date, Checkbox |

Run any example:

```powershell
pwsh -File examples\QuickStart.ps1
# or
powershell.exe -File examples\QuickStart.ps1
```

---

## License

BSD 3-Clause. See [LICENSE](../LICENSE) at the repository root.
