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

### Level 4 - interactive window

Host a full, persistent themed window from a XAML string (or a content scriptblock, or a `.xaml`
file) and wire its named controls in `-Initialize`:

```powershell
Show-FluenceWindow -Xaml $xaml -WatchSystemTheme -Data @{ Tick = 0 } -Initialize {
    param($Window, $Data)
    $Window.FindName('CycleButton').add_Click({
            $Data.Tick++
            Set-FluenceBackdrop -Backdrop 'Acrylic' -Window $Window
        }.GetNewClosure())
}
```

`Set-FluenceTheme`, `Set-FluenceAccent`, and `Set-FluenceBackdrop` switch the theme, accent, and
backdrop at runtime; `-WatchSystemTheme` follows the OS light/dark setting while the window is open;
and `Close-FluenceWindow -Result <value>` closes the window and sets its return value.

> **Note - multi-threaded-apartment hosts.** On `pwsh -mta`, the `-Content`, `-Initialize`, and
> handler scriptblocks run on a separate module-owned UI runspace and cannot reference caller-defined
> functions, variables, or closures. Pass values in through the `-Data` hashtable, hold mutable
> cross-click state in `-Data` (not `$script:` variables), and keep each block self-contained.

---

## Commands

| Command | Description |
|---|---|
| `Show-FluenceDialog` | Show a dialog built from prompt and button specs; returns a `Fluence.DialogResult` object. |
| `Show-FluenceMessage` | Show a message or confirmation dialog; returns the clicked button name as a string. |
| `Get-FluenceInput` | Show a single-prompt dialog; returns the captured value, or `$null` on cancel. |
| `New-FluencePrompt` | Build a prompt specification (label, input type, validation) for use with `Show-FluenceDialog`. |
| `New-FluenceButton` | Build a button specification (caption, default/cancel flags) for use with `Show-FluenceDialog`. |
| `Show-FluenceWindow` | Host an arbitrary themed FluenceWindow from a content scriptblock, a XAML string, or a XAML file; blocks until closed and returns the stashed result. |
| `Set-FluenceTheme` | Switch the process-wide theme (Auto, Light, Dark, HighContrast) at runtime, optionally with a backdrop. |
| `Set-FluenceAccent` | Set the accent color to a custom color, or reset it to the system accent. |
| `Set-FluenceBackdrop` | Set the system backdrop (Mica, Acrylic, Tabbed, None) for the process and optionally a window. |
| `Close-FluenceWindow` | Close a hosted window on its UI thread, optionally stashing a result for the host to return. |
| `Get-FluenceTheme` | Read the current theme state (current theme, resolved theme, backdrop, dark-mode flag). |

---

## Theming

Dialogs and windows render using the Fluence.Wpf theme engine: Auto theme (follows Windows light/dark), system accent color, and Mica backdrop by default. Pass `-Theme`, `-Backdrop`, or `-Accent` to `Show-FluenceDialog` or `Show-FluenceWindow` to override. While a window is open, `Set-FluenceTheme`, `Set-FluenceAccent`, and `Set-FluenceBackdrop` change the theme, accent, and backdrop at runtime, and `Get-FluenceTheme` reads the current state.

---

## Examples

The `examples\` directory contains ready-to-run scripts. The first four are dialog examples; the
last four host a persistent interactive window with `Show-FluenceWindow`:

| Script | What it shows |
|---|---|
| `QuickStart.ps1` | One-liner success message |
| `Message.ps1` | YesNo confirmation with branch logic |
| `SignIn.ps1` | Account and password form with validation |
| `Form.ps1` | Mixed form: Text, Number, Choice, Date, Checkbox |
| `HelloWindow.ps1` | A Mica window whose button cycles the backdrop and rotates a greeting |
| `ThemeAndAccent.ps1` | Switch Light/Dark/Auto themes and cycle custom accent colors at runtime |
| `ControlsTour.ps1` | Common controls in scrolling cards; a toggle drives an InfoBar message |
| `LoadXamlFile.ps1` | Load the window UI from `MainWindow.xaml` on disk and wire its named controls |

Run any example:

```powershell
pwsh -File examples\QuickStart.ps1
# or
powershell.exe -File examples\QuickStart.ps1
```

---

## License

BSD 3-Clause. See [LICENSE](../LICENSE) at the repository root.
