# PowerShell examples

These six scripts demonstrate the packaged module from a source checkout. Each imports the local module, so [stage the library](../README.md#use-the-module-from-a-source-checkout) first. Run commands below from the `Fluence.Wpf.PowerShell.Module` directory with Windows PowerShell 5.1 or PowerShell 7.4 or later on Windows.

```powershell
pwsh -NoProfile -File .\examples\01-Message.ps1
# Windows PowerShell 5.1:
powershell.exe -NoProfile -STA -File .\examples\01-Message.ps1
```

Run each script in a fresh process when comparing appearance, because theme and accent settings belong to the process.

To run the complete controls tour from the `Fluence.Wpf.PowerShell.Module` directory, use:

```powershell
pwsh -NoProfile -File .\examples\06-ControlsTour.ps1
# Windows PowerShell 5.1:
powershell.exe -NoProfile -STA -File .\examples\06-ControlsTour.ps1
```

The tour opens on Inputs. Select one of the nine labeled categories in the left rail to switch pages; each page uses two main content columns. The Data page binds a grid to sample PowerShell objects, and selecting a row updates the shared status bar. See the [Windows from XAML guide](https://fluencewpf.com/docs/powershell/how-to/windows-from-xaml) for the `-Initialize` and `-Data` patterns used to build interactive windows. This source checkout still needs the [library staged](../README.md#use-the-module-from-a-source-checkout) before the example can load it.

| Example | What to try | Related guide |
| --- | --- | --- |
| [01-Message.ps1](01-Message.ps1) | Choose Yes to print `Proceeding`; choose No or dismiss to print `Cancelled`. | [Dialogs and messages](https://fluencewpf.com/docs/powershell/how-to/dialogs) |
| [02-Form.ps1](02-Form.ps1) | Edit text, number, choice, date, and check box fields. Submit to print the values. | [Forms and validation](https://fluencewpf.com/docs/powershell/how-to/forms-and-validation) |
| [03-Progress.ps1](03-Progress.ps1) | Watch five simulated steps update a progress window. | [Progress](https://fluencewpf.com/docs/powershell/how-to/progress) |
| [04-ThemeAndAccent.ps1](04-ThemeAndAccent.ps1) | Switch Light, Dark, system theme, and accent while the window is open. | [Runtime appearance](https://fluencewpf.com/docs/powershell/how-to/theming-at-runtime) |
| [05-LoadXamlFile.ps1](05-LoadXamlFile.ps1) and [MainWindow.xaml](MainWindow.xaml) | Load layout from a XAML file and wire named controls through `-Initialize` and `-Data`. | [Windows from XAML](https://fluencewpf.com/docs/powershell/how-to/windows-from-xaml) |
| [06-ControlsTour.ps1](06-ControlsTour.ps1) | Switch between nine two-column category pages to try every public Fluence control and common variants, including a full color picker. Open flyouts, a dialog, and a teaching tip; add and close tabs; sort the PowerShell-backed grid and select a row to update the shared status. | [Windows from XAML](https://fluencewpf.com/docs/powershell/how-to/windows-from-xaml) |

For a guided first script, follow [Your first dialog](https://fluencewpf.com/docs/powershell/usage). For parameter details, use the [command reference](https://fluencewpf.com/docs/powershell/reference/).
