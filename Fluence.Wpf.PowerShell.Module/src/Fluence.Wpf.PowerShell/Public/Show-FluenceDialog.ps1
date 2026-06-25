function Show-FluenceDialog
{
    <#
    .SYNOPSIS
        Shows a themed Fluent dialog built from prompts and buttons, and returns the user's input.
    .DESCRIPTION
        Renders a FluenceWindow with an optional message, a stack of input prompts, and a row of
        buttons. Returns a Fluence.DialogResult object with a property per named prompt and a boolean
        per button, plus a Cancelled flag.
    .PARAMETER Title
        The window title.
    .PARAMETER Message
        One or more message lines shown above the prompts.
    .PARAMETER Icon
        Optional icon severity shown as a leading InfoBar above the message area.
        None (default) renders the message as plain text. Info, Success, Warning, Error, and
        Question map to the corresponding InfoBarSeverity (Question uses Informational).
    .PARAMETER Prompts
        Strings or Fluence.Prompt objects (see New-FluencePrompt). A bare string becomes a Text prompt.
    .PARAMETER Buttons
        Strings or Fluence.Button objects (see New-FluenceButton). Defaults to a single OK button.
        A bare 'Cancel' string is treated as a cancel button (closes on Esc, no validation); any
        other bare string (for example 'No' or 'Close') is a plain button, so build it with
        New-FluenceButton -IsCancel if you want it to act as the Esc/cancel affordance.
    .PARAMETER Theme
        Auto (default), Light, Dark, or HighContrast.
    .PARAMETER Backdrop
        Mica (default), Acrylic, Tabbed, None, or Auto.
    .PARAMETER Accent
        Optional accent color (System.Windows.Media.Color or a parseable string). Defaults to system accent.
    .PARAMETER MinWidth
        Minimum window width (default 360).
    .PARAMETER Topmost
        Show above other windows.
    .PARAMETER ParentWindow
        An owning System.Windows.Window for modal parenting.
    .EXAMPLE
        Show-FluenceDialog -Title 'Setup' -Prompts 'Your name?' -Buttons OK
    .OUTPUTS
        Fluence.DialogResult
    .NOTES
        Establishes a WPF Application on a private STA thread when none exists; reuses a host
        application (for example PSADT) when one is already running. Blocks until the dialog closes.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.DialogResult')]
    param
    (
        [Parameter()]
        [string]$Title = 'Fluence',

        [Parameter()]
        [string[]]$Message,

        [Parameter()]
        [ValidateSet('None', 'Info', 'Success', 'Warning', 'Error', 'Question')]
        [string]$Icon = 'None',

        [Parameter()]
        [object[]]$Prompts,

        [Parameter()]
        [object[]]$Buttons = @('OK'),

        [Parameter()]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme = 'Auto',

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop = 'Mica',

        [Parameter()]
        [System.Windows.Media.Color]$Accent,

        [Parameter()]
        [int]$MinWidth = 360,

        [Parameter()]
        [switch]$Topmost,

        [Parameter()]
        [System.Windows.Window]$ParentWindow
    )

    # Caller-thread work: normalize and pre-validate the specification (no UI here).
    $promptList = @()
    if ($null -ne $Prompts)
    {
        $promptList = ConvertTo-FluencePromptList -InputObject $Prompts
    }
    $buttonList = ConvertTo-FluenceButtonList -InputObject $Buttons

    $accentColor = $null
    if ($PSBoundParameters.ContainsKey('Accent'))
    {
        $accentColor = $Accent
    }

    $spec = @{
        Title        = $Title
        Message      = $Message
        Icon         = $Icon
        Prompts      = $promptList
        Buttons      = $buttonList
        Theme        = $Theme
        Backdrop     = $Backdrop
        AccentColor  = $accentColor
        MinWidth     = $MinWidth
        Topmost      = [bool]$Topmost
        ParentWindow = $ParentWindow
    }

    $result = Invoke-OnFluenceUi -Script {
        param($s)
        Invoke-FluenceWindow -Spec $s
    } -ArgumentList @($spec)

    # Invoke-InFluenceStaRunspace returns a collection; unwrap to the single hashtable.
    $hash = $result
    if ($result -is [System.Collections.IList] -and $result.Count -ge 1)
    {
        $hash = $result[$result.Count - 1]
    }

    return ConvertTo-FluenceResult -Result $hash
}
