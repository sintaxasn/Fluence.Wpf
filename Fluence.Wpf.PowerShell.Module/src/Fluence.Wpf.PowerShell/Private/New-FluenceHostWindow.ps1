function New-FluenceHostWindow
{
    <#
    .SYNOPSIS
        Builds a FluenceWindow with module baseline defaults and the caller's explicitly-bound chrome.
    .DESCRIPTION
        Creates a FluenceWindow, seeds the module baseline (title, sizing, startup location, backdrop),
        applies the explicitly-bound chrome over those defaults, and parents the window to an owner when
        one is supplied. The host window is the surface a -Content block fills or a non-Window parsed
        XAML payload is hosted in.
    .PARAMETER Spec
        The normalized window specification hashtable from Show-FluenceWindow.
    .OUTPUTS
        Fluence.Wpf.Controls.FluenceWindow
    .NOTES
        Must run on a UI (STA) thread; call it through Show-FluenceWindowCore. Builds an in-memory
        window object only and changes no external state.
    #>
    [CmdletBinding()]
    [OutputType([Fluence.Wpf.Controls.FluenceWindow])]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Builds a WPF window object in memory; changes no external system state.')]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Spec
    )

    $window = [Fluence.Wpf.Controls.FluenceWindow]::new()
    # Module baseline defaults; explicitly-bound chrome below overrides these.
    $window.Title = 'Fluence'
    $window.SizeToContent = [System.Windows.SizeToContent]::Manual
    $window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::CenterScreen
    $window.SystemBackdropType = [Fluence.Wpf.BackdropType]$Spec.Backdrop

    Set-FluenceWindowChrome -Window $window -ChromeBound $Spec.ChromeBound

    if ($null -ne $Spec.Owner)
    {
        $window.Owner = $Spec.Owner
        $window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::CenterOwner
    }
    return $window
}
