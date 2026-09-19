# Input types

The `-InputType` values accepted by `New-FluencePrompt`, the Fluence control each renders, and the value the result carries. `Get-FluenceInput` accepts the same values except `List`, which selects from a set rather than capturing a typed value; `Show-FluenceListSelection` is the cmdlet for that.

| InputType | Control | Result value type | Untouched value | Notes |
| --- | --- | --- | --- | --- |
| `Text` (default) | `Fluence.Wpf.Controls.TextBox` | `string` | `DefaultValue` or `$null` | Single line. |
| `Multiline` | `Fluence.Wpf.Controls.TextBox` (AcceptsReturn, 3 lines minimum) | `string` | `DefaultValue` or `$null` | Enter inserts a newline. |
| `Password` | `System.Windows.Controls.PasswordBox` with the Fluence style and reveal button | `string` | `DefaultValue` or `$null` | The value is a plain string; handle it accordingly. |
| `Number` | `Fluence.Wpf.Controls.NumberBox` | `double` | `DefaultValue` or `0` | `DefaultValue` must convert to `double`. |
| `Checkbox` | `Fluence.Wpf.Controls.CheckBox` | `bool` | `DefaultValue` or `$false` | The prompt `Message` is the check box label; no separate label is shown. `DefaultValue` is converted with `[Convert]::ToBoolean`, so `'false'` is `$false`. |
| `Toggle` | `Fluence.Wpf.Controls.ToggleSwitch` (On / Off) | `bool` | `DefaultValue` or `$false` | Same conversion as `Checkbox`. |
| `Choice` | `Fluence.Wpf.Controls.ComboBox` (`-As Combo`, default) or a column of `RadioButton` (`-As Radio`) | `string` | `DefaultValue` or `$null` | Requires `-ValidateSet`. |
| `List` | `Fluence.Wpf.Controls.ListView`, 240 device-independent pixels high at most | `string`, or `object[]` with `-MultiSelect` | `DefaultValue`, or `@()` with `-MultiSelect` | Requires `-ValidateSet`. With `-MultiSelect` the value is always an array, even for one selected item. |
| `Date` | `Fluence.Wpf.Controls.DatePicker` | `System.DateTime` or `$null` | `DefaultValue` or `$null` | `DefaultValue` must convert to `datetime`. |
| `Time` | `Fluence.Wpf.Controls.TimePicker` | `System.TimeSpan` or `$null` | `DefaultValue` or `$null` | `DefaultValue` must convert to `timespan`. |
| `FileOpen` | `TextBox` plus a Browse button opening `Microsoft.Win32.OpenFileDialog` | `string` (path) | `DefaultValue` or `$null` | |
| `FileSave` | `TextBox` plus a Browse button opening `Microsoft.Win32.SaveFileDialog` | `string` (path) | `DefaultValue` or `$null` | |
| `FolderOpen` | `TextBox` plus a Browse button opening `System.Windows.Forms.FolderBrowserDialog` | `string` (path) | `DefaultValue` or `$null` | Works on both editions. |
| `Link` | `Fluence.Wpf.Controls.HyperlinkButton` | `string` (the URI given as `DefaultValue`) | `DefaultValue` | Display-only; `Message` is the link text, `DefaultValue` the target. No label is shown. |

## Validation

Validation runs when a non-cancel button is clicked, in prompt order, and stops at the first failure. The failure message is shown in an error `InfoBar` under the prompts and the dialog stays open. Cancel buttons, Esc and the title-bar X skip validation.

| Rule | Passes when |
| --- | --- |
| `-ValidateNotEmpty` | The value converted to a string is not empty or whitespace. For an array (a `List` with `-MultiSelect`) this means at least one item. |
| `-ValidatePattern <regex>` | The value is empty, or its string form matches the pattern. Combine with `-ValidateNotEmpty` to require a match. |
| `-ValidateScript { param($value) ... }` | The last object the scriptblock emits is truthy. An exception inside the scriptblock counts as a failure. |

Messages: `'<Name>' is required.`, `'<Name>' does not match the required format.`, `'<Name>' failed validation.`

## Default value coercion

`New-FluencePrompt` converts `-DefaultValue` at build time for `Number` (`double`), `Date` (`datetime`), `Time` (`timespan`), `Checkbox` and `Toggle` (`bool` via `[Convert]::ToBoolean`). A value that does not convert throws immediately with the input type and the conversion error in the message, before any window opens.
