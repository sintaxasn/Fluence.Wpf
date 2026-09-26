# PowerShell module examples

Six runnable examples cover messages, forms, progress, custom windows, appearance and control discovery. Run the scripts in numeric order for a walkthrough, or choose the task you need below. For guided learning, follow [your first dialog](../../docs/powershell/tutorial.md); for command details, use the [reference](../../docs/powershell/reference/README.md).

## Before you run

Use Windows PowerShell 5.1 or PowerShell 7.4 or later on Windows. Follow the module's [import and staging instructions](../README.md#import) first. Each script imports the local module itself.

Run the commands below from the `Fluence.Wpf.PowerShell.Module` directory. To use Windows PowerShell 5.1, replace `pwsh` with `powershell.exe`. Each example runs in its own process so theme settings and windows belong to that example.

## Start with a confirmation

[01-Message.ps1](01-Message.ps1) opens a Yes/No confirmation and branches on the result:

```powershell
pwsh -NoProfile -File .\examples\01-Message.ps1
```

Choose Yes to print `Proceeding`. Choose No, press Esc or close the dialog to print `Cancelled`. The script does not run an installation. See [dialogs and messages](../../docs/powershell/how-to/dialogs.md) for presets, images, countdowns and restart prompts.

## Use an example for your task

| Task | Example and run command | Expected result | Guide |
| --- | --- | --- | --- |
| Collect and validate input | [02-Form.ps1](02-Form.ps1): `pwsh -NoProfile -File .\examples\02-Form.ps1` | A form with text, number, choice, date and checkbox fields. OK prints the entered values; Cancel prints `Form cancelled.` | [Forms and validation](../../docs/powershell/how-to/forms-and-validation.md), including passwords and list selection |
| Report progress during work | [03-Progress.ps1](03-Progress.ps1): `pwsh -NoProfile -File .\examples\03-Progress.ps1` | A progress window moves from preparation through five simulated steps, closes, then prints `Done.` | [Progress](../../docs/powershell/how-to/progress.md) |
| Change appearance while a window is open | [04-ThemeAndAccent.ps1](04-ThemeAndAccent.ps1): `pwsh -NoProfile -File .\examples\04-ThemeAndAccent.ps1` | Light, Dark and Use system setting buttons change the theme; accent buttons switch colours; the window icon follows the theme. | [Runtime theming](../../docs/powershell/how-to/theming-at-runtime.md), including backdrops |
| Keep window layout separate from script logic | [05-LoadXamlFile.ps1](05-LoadXamlFile.ps1): `pwsh -NoProfile -File .\examples\05-LoadXamlFile.ps1` | Loads [MainWindow.xaml](MainWindow.xaml), wires theme and accent controls, preserves click state with `-Data`, and closes from its footer. | [Windows from XAML](../../docs/powershell/how-to/windows-from-xaml.md) |

`05-LoadXamlFile.ps1` is the starting point for building your own window. `04-ThemeAndAccent.ps1` focuses on appearance changes and theme-event subscriptions.

## Explore the controls

[06-ControlsTour.ps1](06-ControlsTour.ps1) is an interactive gallery of common controls:

```powershell
pwsh -NoProfile -File .\examples\06-ControlsTour.ps1
```

Scroll through the cards and try the inputs. The toggle updates an InfoBar. The introduction and Close button remain visible outside the scrolling content. Use this example to inspect controls and their XAML; use the [window guide](../../docs/powershell/how-to/windows-from-xaml.md) to learn hosting and callbacks.

## Find a smaller recipe

The how-to guides keep variations next to the task they support:

- [Dialogs and messages](../../docs/powershell/how-to/dialogs.md): one-line messages, images, text alignment, screen position and restart outcomes.
- [Forms and validation](../../docs/powershell/how-to/forms-and-validation.md): secure sign-in credentials, single or multiple selections, defaults and cancellation.
- [Windows from XAML](../../docs/powershell/how-to/windows-from-xaml.md): a minimal inline window, event handlers and state on STA or MTA hosts.
- [Runtime theming](../../docs/powershell/how-to/theming-at-runtime.md): changing the backdrop of an open window.

The [explanation](../../docs/powershell/explanation.md) covers the module's hosting and theme design.
