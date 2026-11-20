# Reliability and accessibility investigation briefs

Read the matching issue in [Known issues](../../KNOWN_ISSUES.md) for impact, status, and the interim workaround.

## An embedded PowerShell UI host can fail during shutdown

**Context and cause.** WPF's Application belongs to its original dispatcher and may be reused across imports. Runspace disposal while an owned WPF dispatcher is active can produce a terminal shutdown assertion. Register-FluenceRunspaceExit only retires the secondary UI dispatcher on orderly exit of the primary, unpushed ConsoleHost session. A child or custom host owns its own cleanup. [Exit hook](../../Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Private/Register-FluenceRunspaceExit.ps1) · [Runspace cleanup](../../Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Private/Close-FluenceUiRunspace.ps1)

**Previous work.** A standalone PowerShell 7 STA runner reproduced the assertion after its assertions passed. Its finally block checks for leaked windows and shuts down the dispatcher it owns. Module removal preserves the shared WPF application so another import can reuse it. [Module test runner](../../Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1)

**Investigate.**

1. Read [PowerShell guidance](../../Fluence.Wpf.PowerShell.Module/AGENTS.md). Reproduce in a separate process. Record host type, apartment, caller runspace, Application dispatcher thread, and how that process exits.
2. Compare primary ConsoleHost, inline STA, and child or custom host. Record which code created the dispatcher and closes its windows. Observe dispatcher shutdown before disposing that runspace. [Dispatcher shutdown](https://learn.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcher.invokeshutdown)
3. Verify orderly exit and a remove/import cycle. Require both passing assertions and a zero process exit code. Preserve shared owners when disposing a child runspace.

**Done when.** The supported owned-dispatcher exit path closes cleanly, and removing or reimporting from a non-owning session leaves the shared application usable.

## A screen reader can announce ContentDialog as a window

**Context and cause.** The overlay uses a shared peer that exposes the Window control type. ContentDialog cycles focus inside the modal overlay and raises an assertive live-region event on open so its title is available to assistive technology. The dialog marker is available on newer WPF reference assemblies. The library's .NET Framework 4.7.2 reference set lacks that property. [Peer](../../Fluence.Wpf/Automation/ContentDialogAutomationPeer.cs) · [Overlay and announcement](../../Fluence.Wpf/Controls/ContentDialog.cs)

**Previous work.** The live region gives legacy WPF a title announcement and the focus loop provides modal behavior. Existing tests check the automation peer and open/close behavior. They establish UI Automation structure and control behavior. Screen-reader speech depends on the client and runtime. [ContentDialogTests.cs](../../Fluence.Wpf.Tests/Control/ContentDialogTests.cs)

**Investigate.**

1. Open the existing Gallery ContentDialog example on a supported modern WPF build and a Framework host. Record the UI Automation Window role, IsDialog metadata if available, title and live event, focused element, and Tab cycle.
2. Try explicit dialog metadata on a supported target. Compare the event and Narrator output with the same title and focus order. Keep accessible naming and the shared live-region route separate from the visual smoke/sizing issue.
3. Verify Enter, Escape, close, and focus restoration after changes. Test compile and runtime availability of any proposed marker on the exact target before introducing it to shared source.

**Done when.** Supported WPF exposes the intended dialog semantic and the legacy title, modal focus behavior, and close routes keep passing. Record any screen-reader validation separately from peer tests.

## Screen readers can miss structured item positions on older WPF

**Context and cause.** WPF added the PositionInSet and SizeOfSet attached properties and automatic ItemsControl metadata in .NET Framework 4.8. The compile target and the installed runtime can differ. The application can opt into newer behavior when running an eligible Framework runtime. PipsPager creates button children in a panel, so an ordinary ItemsControl metadata check is a poor proxy for its peers. [Microsoft's Framework accessibility guidance](https://learn.microsoft.com/en-us/dotnet/framework/whats-new/whats-new-in-accessibility#whats-new-in-accessibility-in-net-framework-48) · [PipsPager.cs](../../Fluence.Wpf/Controls/PipsPager.cs)

**Previous work.** Tests already verify the names of generated pager buttons, including the ordinal in Page N. A spoken ordinal in the name does not establish separate PositionInSet and SizeOfSet values. The earlier suggestion to set those attached properties directly from 4.7.2 XAML was removed after checking framework availability. [PipsPagerTests.cs](../../Fluence.Wpf.Tests/Control/PipsPagerTests.cs)

**Investigate.**

1. Record compile target, runtime, accessibility switches, and the actual UI Automation client. Inspect the peer of a realized item in a standard ItemsControl, then inspect a pager button as a separate case.
2. Compare metadata after reorder, item count changes, and overflow or virtualization. Determine whether WPF computes it on the real peer or whether the owner supplies it on newer targets.
3. Run peer/name tests and inspect at least one host per supported behavior. Treat the literal words a screen reader speaks as native evidence.

**Done when.** Where the framework supports it, item metadata reflects the current order and count. Existing accessible names and legacy compilation keep working.

## TextBox and PasswordBox need a separate field label

**Context and cause.** Native PasswordBox is sealed and Fluent decorates its template. Fluent TextBox has a visible HelperText slot. The existing fields use a separate visual label and WPF's LabeledBy relationship. Visible text, automation name, and automation HelpText serve distinct purposes. [TextBox template](../../Fluence.Wpf/Themes/Controls/TextBox.xaml) · [PasswordBox template](../../Fluence.Wpf/Themes/Controls/PasswordBox.xaml)

**Previous work.** The recent TextBox validation fix preserves help supplied as a local value, binding, dynamic resource, or style. Its validation fallback applies only while the control owns that value. Gallery examples document the explicit label association. [TextBox.cs](../../Fluence.Wpf/Controls/TextBox.cs) · [TextBoxTests.cs](../../Fluence.Wpf.Tests/Control/TextBoxTests.cs) · [Control guide](../controls.md)

**Investigate.**

1. Start with a TextBox and a native PasswordBox in the same form. Inspect the visible label, inner editor, HelperText, validation message, and UI Automation name and help separately.
2. If adding a field presentation, test the established consumer label and help values through template rebuild and validation. Include bound help and a style applied after load. Keep PasswordBox decoration and protected entry behavior intact.
3. Use the label association tests in the matching Gallery or consuming sample as a control. Verify keyboard focus lands on the editor and retains the intended name.

**Done when.** A field has a visible label linked to its editor, helpful validation is announced, and an application-provided name or help value survives validation and retemplating.
