# Fluence.Wpf.PowerShell.Module - Design Specification

- Date: 2026-06-25
- Status: Approved (design); ready for implementation planning
- Author: Dan Cunningham (with Claude)
- Location of subproject: `F:\FRebuild\Fluence.Wpf\Fluence.Wpf.PowerShell.Module`
- Module id (PowerShell Gallery): `Fluence.Wpf.PowerShell`
- Command noun prefix: `Fluence`

---

## 1. Goal and non-goals

### Goal

Ship a declarative, in-process PowerShell module that lets a scripter produce a themed
Windows 11 / Fluent dialog and receive the user's input back as data, without writing any
WPF. It rebuilds the ergonomics of AnyBox on top of the Fluence.Wpf control library and its
theme engine, written to the PSAppDeployToolkit (PSADT) PowerShell conventions so it is
release-gate clean for that downstream consumer while remaining a useful standalone module.

The promise, at three levels of effort:

```powershell
# 1. One-liner message / confirmation (a MessageBox replacement)
Show-FluenceMessage -Message 'Install complete.' -Icon Success

# 2. Quick single input
$name = Show-FluenceDialog -Title 'Setup' -Prompts 'Your name?' -Buttons OK

# 3. Full declarative form, results returned as an object
$r = Show-FluenceDialog -Title 'Sign in' -Prompts @(
        New-FluencePrompt -Name User -Message 'Account'  -InputType Text
        New-FluencePrompt -Name Pass -Message 'Password' -InputType Password
    ) -Buttons (New-FluenceButton -Text 'Login' -IsDefault), 'Cancel'
if ($r.Login) { Connect -User $r.User -Password $r.Pass }
```

### Non-goals (v1)

- No control-builder cmdlet per control (the ShowUI model). Deferred to phase 2 as an
  escape hatch.
- No out-of-process server / RPC host (the WinUIShell model). WPF loads in-process in both
  PowerShell editions, so that complexity is unnecessary.
- No data binding surface, DataGrid, tabs/groups, inline images, or progress dialogs in v1.
  All deferred to phase 2.
- No first-party compiled C# in v1 (packaging decision below). The module references the
  existing `Fluence.Wpf.dll`.

---

## 2. Background: lessons from the three reference modules

| Module | Model | Lesson taken |
| --- | --- | --- |
| WinUIShell | Out-of-process `Server.exe` hosts WinUI 3; PowerShell talks JSON-RPC; source-generated object API mirrors WinUI namespaces; PowerShell 7.4+/.NET 8 only | The non-blocking, no-Dispatcher feel is the goal. The out-of-process machinery exists only because WinUI 3 cannot be hosted in-process; WPF can, so we stay in-process. |
| ShowUI | Code-generated `New-<Control>` cmdlet per WPF type, in-process, data-binding and pipeline centric | Maximum flexibility but a sprawling surface that is hard to keep clean under strict analyzers. Kept as a phase 2 escape-hatch idea only. |
| AnyBox | One `Show-AnyBox` renders a themed window from a declarative Prompts + Buttons spec; returns a result hashtable; spec builders `New-AnyBoxPrompt` / `New-AnyBoxButton`; enums and spec classes in `Types\*.cs` | This is the chosen shape. We modernize it: render on Fluence Fluent controls + theming instead of hand-rolled raw WPF with hard-coded colors, and follow PSADT conventions. |

Reference instruction documents that govern the work:

- `F:\FRebuild\Fluence.Wpf\AGENTS.md` (Fluence handbook: theme slots, DynamicResource, no hard-coded color, BSD header on C#).
- `F:\FRebuild\psadt4\.github\instructions\powershell.md` (5.1 + 7 compatibility, one function per file, CmdletBinding, comment-based help, OutputType, custom validators, banned APIs, UTF-8 BOM, Allman braces).
- `F:\FRebuild\psadt4\.github\instructions\pester.md` (Pester v5, one test file per public function, BeforeAll import, Context blocks, ForEach, TestDrive, narrow seams).
- `F:\FRebuild\psadt4\.github\instructions\csharp.md` (only relevant if a future phase adds first-party C#: multi-target, nullable, warnings as errors, defensive PSObject unwrapping).

---

## 3. Locked decisions

These five forks were decided during brainstorming and are fixed for v1:

1. **Surface:** Declarative dialogs first. Builders and a raw-window escape hatch are a
   documented phase 2.
2. **PowerShell editions:** Windows PowerShell 5.1 and PowerShell 7+ (dual edition). Strict
   PSADT compatibility rules apply (no ternary, null-coalescing, pipeline-chain, or
   null-conditional operators).
3. **Packaging:** Script module that references the existing `Fluence.Wpf.dll`. Minimal or
   no first-party C#; spec objects are PowerShell custom objects, not exported classes.
4. **Distribution:** Both standalone (PowerShell Gallery) and PSADT-consumable.
5. **PowerShell 7 runtime:** Add a `net8.0-windows` target to the Fluence.Wpf library so a
   .NET 8-loadable build exists (see section 11). This is a prerequisite work-stream.

### Why decision 5 is required

- `PSADT.UserInterface.csproj` targets `net472;net8.0`, so PSADT's PowerShell 7 path runs on
  .NET 8.
- `Fluence.Wpf.csproj` targets only `net472` and `net10.0-windows10.0.26100.0`.
- A `net10.0-windows` assembly cannot load on a .NET 8 or .NET 9 runtime (PowerShell 7.4 /
  7.5). Only PowerShell 7.6+ (currently preview, on .NET 10) could load it.
- The `net472` build is .NET Framework WPF and cannot load in any .NET (Core) process.
- Therefore, without a `net8.0-windows` build, Fluence.Wpf can only be driven from Windows
  PowerShell 5.1 today. A net8 build (which rolls forward onto .NET 8/9/10) unblocks all of
  PowerShell 7.4 and later and aligns Fluence with PSADT.UserInterface's own `net8.0` target.

---

## 4. Architecture

Four private concerns carry the module. Everything public is a thin layer over them.

### 4.1 Dual-edition assembly loader

Runs in the `.psm1` at import.

- `$PSEdition -eq 'Core'` selects `lib\net8.0-windows\Fluence.Wpf.dll`; otherwise it selects
  `lib\net472\Fluence.Wpf.dll`.
- If the host process already has `Fluence.Wpf` loaded (for example, when running inside
  PSADT), detect the loaded assembly and reuse it rather than loading a second copy.
- The net8 build pulls WinRT projection dependencies (`WinRT.Runtime.dll`,
  `Microsoft.Windows.SDK.NET.dll`) which must be staged next to it. An `AssemblyResolve`
  handler (or sibling-directory probing) resolves these so a simple `Add-Type -Path` or
  `Assembly.LoadFrom` succeeds. A dedicated load context may be required on PowerShell 7 if
  default-context probing proves insufficient; treat that as an implementation risk to verify
  early (the WinUIShell custom `AssemblyLoadContext` is the reference pattern if needed).

### 4.2 STA host and dispatcher

- Windows PowerShell 5.1 console runs in an STA apartment, so `Window.ShowDialog()` works on
  the calling thread directly.
- `pwsh` 7 runs MTA by default, and WPF requires STA. When the current apartment is not STA,
  the module starts a single dedicated STA thread that owns one `Application` and one
  `Dispatcher`, shows the dialog there, and marshals the result back to the caller's thread.
  Subsequent dialogs reuse that thread and dispatcher.
- This is the same pattern the library's own test harness `WpfTestSta` uses, so the approach
  is proven in-repo.

### 4.3 Application lifecycle and theme seeding

- Before showing any `FluenceWindow`, call `ApplicationThemeManager.Apply(...)` to seed the
  three resource-dictionary slots. Skipping this is the documented broken-`DynamicResource`
  pitfall in `AGENTS.md`.
- Default appearance: Auto theme, system accent, Mica backdrop. Each is overridable per call
  via `-Theme`, `-Accent`, and `-Backdrop`.
- Reuse `Application.Current` and its dispatcher when one already exists (inside PSADT or any
  existing WPF app). Create an `Application` with `ShutdownMode.OnExplicitShutdown` only when
  none exists, so the module never tears down a host application.

### 4.4 Result and spec object contracts

- `Show-FluenceDialog` returns a flat `PSCustomObject`: one property per named prompt (its
  captured value), one boolean per button (true if it was the button that closed the dialog),
  plus `Cancelled` and `TimedOut` flags.
- Spec builders emit `PSCustomObject` tagged with a `PSTypeName` (`Fluence.Prompt`,
  `Fluence.Button`), validated inside the builder function. We deliberately do not use
  PowerShell `class` types as parameter types, because consuming an exported class needs
  `using module` and is fragile across editions. Enumerations are expressed as
  `[ValidateSet()]` strings. This is the one intentional divergence from AnyBox's compiled
  `Types\AnyBox.cs`, driven by the minimal-C# packaging decision.

---

## 5. Public command surface (v1)

Four firm commands. Two optional helpers ship only if they prove cheap.

| Command | Role | Output |
| --- | --- | --- |
| `Show-FluenceDialog` | Core renderer. Parameters include `-Title -Icon -Message -Prompts -Buttons -DefaultButton -CancelButton -Comment -Theme -Accent -Backdrop -Timeout -Countdown -Topmost -WindowStartupLocation -ParentWindow`. | `PSCustomObject` (prompt values + button flags) |
| `New-FluencePrompt` | Build one input prompt spec. | object, `[OutputType('Fluence.Prompt')]` |
| `New-FluenceButton` | Build one button spec. | object, `[OutputType('Fluence.Button')]` |
| `Show-FluenceMessage` | One-liner message / confirmation. `-Icon Info|Warning|Error|Question|Success`, `-Buttons OK|OKCancel|YesNo|YesNoCancel`. Maps to a Fluence `InfoBar` or `ContentDialog`. | clicked-button name (string) |

Optional helpers (include only if low cost):

- `Get-FluenceInput`: wraps a single-prompt dialog and returns the value directly.
- `Set-FluenceTheme`: sets a session default theme / accent / backdrop for later dialogs.

Conventions for every public function:

- One function per file under `Public\`, file name equals function name.
- `[CmdletBinding()]`, deliberate parameter sets, comment-based help, `[OutputType()]`.
- `.NOTES` states whether an active host application or PSADT session is required.
- Interactive `Show-*` commands return data and do not declare `SupportsShouldProcess` (they
  do not mutate machine state); this is documented in their `.NOTES`.
- Use `IsNullOrWhiteSpace` (the `IsNullOrEmpty` string overload is banned).

---

## 6. Input types and validation

### v1 input types (`New-FluencePrompt -InputType`)

`Text`, `Multiline`, `Password`, `Number` (Fluence `NumberBox`), `Checkbox`, `Toggle`
(Fluence `ToggleSwitch`), `Choice` (with `-As Combo|Radio`, rendering `ComboBox` or a
`RadioButton` group), `Date` (Fluence `DatePicker`), `Time` (Fluence `TimePicker`),
`FileOpen` / `FileSave` / `FolderOpen` (a `TextBox` plus a browse button using the standard
Win32 dialogs), and `Link` (Fluence `HyperlinkButton`).

### Per-prompt validation

`-ValidateNotEmpty`, `-ValidateSet`, `-ValidatePattern`, and `-ValidateScript`. A failed
validation surfaces inline (a Fluence `InfoBar`) and blocks the dialog from closing, mirroring
AnyBox's `Test-ValidInput` behavior.

### Deferred to phase 2

`GridData` / DataGrid, tabs and groups, inline images, progress dialogs, the
control-builder and raw-window escape hatch, and `Set-FluenceTheme` if it is not trivially
cheap in v1.

---

## 7. Repository layout

```
Fluence.Wpf.PowerShell.Module/
  src/Fluence.Wpf.PowerShell/
    Fluence.Wpf.PowerShell.psd1        # manifest: dual edition, explicit FunctionsToExport
    Fluence.Wpf.PowerShell.psm1        # loader + dot-source of Public/Private
    Public/   *.ps1                    # one function per file
    Private/  *.ps1                    # loader, STA host, theme, validation, marshalling
    Types/    Fluence.Format.ps1xml    # display formatting for result and spec objects
    lib/net472/                        # staged Fluence.Wpf.dll (+ deps) for 5.1
    lib/net8.0-windows/                # staged Fluence.Wpf.dll (+ WinRT deps) for 7+
  tests/      *.Tests.ps1              # Pester v5
  examples/   QuickStart, SignIn, ...  # runnable samples mirroring the reference modules
  build.ps1                            # stage libs -> analyzer -> Pester -> package
  PSScriptAnalyzerSettings.psd1        # desktop-5.1 compatibility profile
  README.md
```

---

## 8. Build and packaging

- `build.ps1` builds Fluence.Wpf for `net472` and `net8.0-windows`, copies each output into
  `lib\<tfm>\`, runs PSScriptAnalyzer (compatibility gate), runs Pester, then stages the
  publishable module folder.
- Standalone / PowerShell Gallery: the `Fluence.Wpf.dll` and its dependencies ship inside
  `lib\`. The published artifact is self-contained.
- PSADT: the loader also accepts an already-present `Fluence.Wpf` assembly, so PSADT can
  supply the library from its own `lib` location without a duplicate load.
- CI: add a build/test step to the repository workflow `.github/workflows/build.yml`.
- Solution integration: a pure script module needs no `.csproj`. Lean default is a build
  script plus a CI step and no entry in `Fluence.Wpf.sln`. A thin wrapper project can be
  added later if Visual Studio visibility is wanted; this is an open default (section 13).

---

## 9. Testing strategy

Pester v5, one `*.Tests.ps1` per public function, with the standard `BeforeAll` import
pattern from `pester.md`. Two lanes:

- **Logic lane (CI, headless):** spec-builder output shape, validation rules, result-object
  shaping, parameter sets, and loader edition-selection (mock `$PSEdition`). This is the bulk
  of the coverage and runs on every push.
- **UI lane (opt-in, STA):** real window render and interaction, tagged and skipped in CI
  unless an environment flag is set. This mirrors the library's opt-in `Screenshots` test
  category. The `examples\` scripts double as manual smoke tests.
- **Analyzer gate:** PSScriptAnalyzer with the `desktop-5.1.14393.206-windows` compatibility
  profile is a hard CI gate that catches any PowerShell 7-only syntax.

Baseline policy: follow the repository convention that test count is a floor; add tests, do
not weaken them.

---

## 10. Convention conformance matrix

| Source | Requirement | How v1 satisfies it |
| --- | --- | --- |
| powershell.md | 5.1 + 7 syntax, no 7-only operators | Dual-edition code, analyzer compatibility gate |
| powershell.md | One function per file, CmdletBinding, comment-based help, OutputType | Public/ layout and authoring checklist |
| powershell.md | Banned `IsNullOrEmpty` string API | Use `IsNullOrWhiteSpace` |
| powershell.md | UTF-8 BOM, 4-space, Allman braces | Editor config and analyzer settings |
| pester.md | Pester v5, one file per function, narrow seams | Logic and UI test lanes |
| AGENTS.md | Theme-slot seeding before showing a window | `ApplicationThemeManager.Apply` in the private host |
| AGENTS.md | DynamicResource, no hard-coded color | Rendering goes through Fluence controls and tokens |
| csharp.md | Multi-target, nullable, defensive PSObject unwrapping | Not triggered in v1 (no first-party C#); applies if phase 2 adds C# |

---

## 11. Prerequisite work-stream: add net8.0-windows to the library

Sequenced first; the module's PowerShell 7 lane depends on it.

- Extend `Fluence.Wpf.csproj` `<TargetFrameworks>` to
  `net472;net8.0-windows10.0.x;net10.0-windows10.0.26100.0` (the exact Windows SDK floor for
  the net8 target to be chosen in the plan; the library OS baseline is Windows 10 1809).
- Keep the existing net472-only polyfills (`Meziantou.Polyfill`, `Microsoft.Bcl.Memory`)
  scoped to net472.
- The new target must build clean under the strict analyzer set (`latest-all`, warnings as
  errors, banned APIs, XML docs).
- Library tests and CI may remain net472 + net10 to avoid a third test lane, or add net8;
  the plan will recommend the minimal change consistent with the consumer build-compatibility
  gate in `AGENTS.md` section 11.
- This aligns Fluence.Wpf with `PSADT.UserInterface`'s `net8.0` target and makes genuine
  PSADT-on-PowerShell-7 consumption possible.

---

## 12. Phasing and definition of done

- **Phase 0:** library gains the `net8.0-windows` target and builds clean on all targets.
- **Phase 1:** loader + STA host + theme seeding; `Show-FluenceDialog`, `New-FluencePrompt`,
  `New-FluenceButton`, `Show-FluenceMessage`; the v1 input types; logic-lane tests green;
  analyzer gate green; three to four runnable examples; README and a CHANGELOG entry.
- **Phase 2 (documented, not built):** builders and raw-window escape hatch, DataGrid,
  tabs / groups, progress, and `Set-FluenceTheme` if it was not delivered as a cheap v1 helper.

**Done** means: both editions import the module and show a themed dialog; logic tests and the
analyzer gate are green in CI; the example scripts run on Windows PowerShell 5.1 and on
PowerShell 7.

---

## 13. Risks and open defaults

Risks to verify early in implementation:

- net8 assembly load on PowerShell 7 may need an explicit assembly-resolve handler or a
  dedicated load context for the WinRT dependencies. Prove with a minimal load test before
  building the command surface.
- STA marshalling from an MTA `pwsh` host must correctly return values and exceptions across
  the thread boundary. Cover with a focused test.
- The library's net8 target must pass the strict analyzers; budget time for analyzer fixes.

Open defaults (chosen, easily changed if you object):

- Command noun prefix `Fluence`; module id `Fluence.Wpf.PowerShell`.
- Helper naming `Show-FluenceMessage` and `Get-FluenceInput`.
- Modal-blocking dialogs (not async) in v1.
- No `Fluence.Wpf.sln` entry for the script module; build via `build.ps1` plus a CI step.

---

## 14. References

- Reference modules: `F:\FRebuild\WinUIShell`, `F:\FRebuild\ShowUI`, `F:\FRebuild\AnyBox`.
- Conventions: `AGENTS.md`; `psadt4\.github\instructions\powershell.md`, `pester.md`,
  `csharp.md`.
- In-repo proven patterns: `WpfTestSta` (STA host), `ApplicationThemeManager.Apply` (theme
  slot seeding), the opt-in `Screenshots` test category (UI test gating).
- Runtime evidence: `PSADT.UserInterface.csproj` targets `net472;net8.0`;
  `Fluence.Wpf.csproj` targets `net472;net10.0-windows10.0.26100.0`.
