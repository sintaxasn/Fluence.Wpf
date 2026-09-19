# Result objects

The objects the cmdlets return. All are `PSCustomObject` instances tagged with a `PSTypeName`; none is a .NET class.

## Fluence.DialogResult

Returned by `Show-FluenceDialog`.

| Property | Type | Meaning |
| --- | --- | --- |
| `<prompt Name>` | depends on the input type (see [input types](input-types.md)) | One property per prompt, named after the prompt's `Name`. Holds the value at the moment the dialog closed, or the prompt's default when untouched. |
| `<button Name>` | `bool` | One property per button, named after the button's `Name` (its `Text` unless `-Name` was given). `$true` for the button that closed the dialog. |
| `Cancelled` | `bool` | `$true` when the dialog closed without a successful button: the cancel button, Esc, or the title-bar X. `$false` on a timeout. |
| `TimedOut` | `bool` | `$true` when `-Timeout` elapsed. No button property is `$true` in that case. |

Exactly one of these is true for a closed dialog: a button property, `Cancelled`, or `TimedOut`. When the UI returns nothing at all (a fault on the UI thread), the result is `Cancelled = $true`, `TimedOut = $false` with no other properties.

## String results

| Cmdlet | Returns |
| --- | --- |
| `Show-FluenceMessage` | The `Name` of the button that closed the dialog: `OK`, `Cancel`, `Yes` or `No`. A dismissal (Esc, X) returns the preset's cancel button, or its last button when it has none (`OK` for `OK`, `No` for `YesNo`). A timeout returns `-DefaultButton` when given, otherwise the same dismissal value. |
| `Show-FluenceRestartPrompt` | `Restart`, `Later` or `TimedOut`. |

## Fluence.Prompt

Returned by `New-FluencePrompt`; consumed by `Show-FluenceDialog -Prompts`.

| Property | Type | Meaning |
| --- | --- | --- |
| `Name` | `string` | Result key. Generated (`Input_xxxxxxxx`) when omitted. |
| `Message` | `string` | Label text. |
| `InputType` | `string` | One of the [input types](input-types.md). |
| `DefaultValue` | `object` | Coerced to the input type's value type at build time. |
| `ValidateSet` | `string[]` | Allowed values for `Choice` and `List`. |
| `As` | `string` | `Combo` or `Radio` for `Choice`. |
| `MultiSelect` | `bool` | `List` only. |
| `ValidateNotEmpty` | `bool` | Require a value. |
| `ValidatePattern` | `string` | Regular expression the value must match. |
| `ValidateScript` | `scriptblock` | Receives the value; the last object it emits is the verdict. |

## Fluence.Button

Returned by `New-FluenceButton`; consumed by `Show-FluenceDialog -Buttons`.

| Property | Type | Meaning |
| --- | --- | --- |
| `Name` | `string` | Result key. Defaults to `Text`. |
| `Text` | `string` | Caption. |
| `IsDefault` | `bool` | Activated by Enter, drawn in the accent style, laid out first, carries the countdown caption. |
| `IsCancel` | `bool` | Activated by Esc, skips validation, laid out last. |

A bare string in `-Buttons` becomes a button with `Name = Text`; the string `Cancel` (any casing) additionally gets `IsCancel`.

## Fluence.ProgressHandle

Returned by `Show-FluenceProgress`; passed to `Update-FluenceProgress` and `Close-FluenceProgress`.

| Property | Type | Meaning |
| --- | --- | --- |
| `Id` | `guid` | Identifies the window. |
| `Mode` | `string` | `Inline` (window shares the calling STA thread), `Runspace` (window lives on the module UI runspace; MTA hosts). |
| `IsOpen` | `bool` | `$false` after `Close-FluenceProgress`. |
| `State` | synchronized `hashtable` | `Message`, `Detail`, `PercentComplete` (0 to 100), `Indeterminate`, `CloseRequested`. Readable from any thread. |
| `Spec` | `hashtable` | Title, Topmost, Position, Width, Theme, Backdrop, AccentColor as passed. |
| `Parts` | `hashtable` | `Window`, `MessageText`, `DetailText`, `Bar`: the live WPF objects. Dependency properties on them can be read only on the thread that owns the window (`Inline` mode). |

Only one progress window can be open at a time.

## Fluence.WindowResult

Returned by `Show-FluenceWindow -PassThru`.

| Property | Type | Meaning |
| --- | --- | --- |
| `Result` | `object` | The value stashed with `Close-FluenceWindow -Result`, or `$null`. |
| `Closed` | `bool` | Always `$true` once the call returns. |

Without `-PassThru`, `Show-FluenceWindow` returns `Result` directly.

## Fluence.ThemeInfo

Returned by `Get-FluenceTheme`.

| Property | Type | Meaning |
| --- | --- | --- |
| `CurrentTheme` | `Fluence.Wpf.ApplicationTheme` | The requested theme: `Auto`, `Light`, `Dark`, `HighContrast`. |
| `ResolvedTheme` | `Fluence.Wpf.ApplicationTheme` | What is showing: `Auto` resolved to `Light` or `Dark`. |
| `CurrentBackdrop` | the library backdrop enum | `Mica`, `Acrylic`, `Tabbed`, `None`, `Auto`. The enum type is `WindowBackdropType` from Fluence.Wpf 0.9.0-pre and `BackdropType` on earlier builds; compare by name (`.ToString()`). |
| `IsAppInDarkMode` | `bool` | `$true` when the resolved theme is dark. |
