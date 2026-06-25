function Invoke-FluenceWindow
{
    <#
    .SYNOPSIS
        Ensures the WPF Application, seeds the Fluence theme slots, builds the dialog window, and shows it.
    .NOTES
        Must run on a UI (STA) thread; call it through Invoke-OnFluenceUi. Returns the result hashtable.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [hashtable]$Spec
    )

    if ($null -eq [System.Windows.Application]::Current)
    {
        $app = [System.Windows.Application]::new()
        $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
    }

    # Seed the three theme slots. Mandatory before showing a FluenceWindow.
    $theme    = [Fluence.Wpf.ApplicationTheme]$Spec.Theme
    $backdrop = [Fluence.Wpf.BackdropType]$Spec.Backdrop
    [Fluence.Wpf.ApplicationThemeManager]::Apply($theme, $backdrop, $true)

    if ($null -ne $Spec.AccentColor)
    {
        [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent([System.Windows.Media.Color]$Spec.AccentColor)
    }
    else
    {
        [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
    }

    $state = @{ Result = @{}; Window = $null }
    $window = New-FluenceDialogWindow -Spec $Spec -State $state
    $state.Window = $window

    $null = $window.ShowDialog()
    return $state.Result
}
