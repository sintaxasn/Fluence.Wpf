function Initialize-FluenceApplication
{
    <#
    .SYNOPSIS
        Ensures the WPF Application exists and seeds the Fluence theme slots and accent.
    .DESCRIPTION
        Creates an Application with ShutdownMode OnExplicitShutdown when none exists, applies the
        requested theme and backdrop (seeding the three theme slots), then pins the accent to a
        custom color when one is supplied or resets to the system accent otherwise. Shared by the
        dialog path and the window host.
    .PARAMETER Theme
        The requested theme name (Auto, Light, Dark, or HighContrast).
    .PARAMETER Backdrop
        The requested backdrop name (Mica, Acrylic, Tabbed, None, or Auto).
    .PARAMETER Accent
        Optional custom accent color (System.Windows.Media.Color). When null, the system accent is applied.
    .NOTES
        Must run on a UI (STA) thread; call it through Invoke-OnFluenceUi. Does not show any window.
    #>
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]$Theme,

        [Parameter(Mandatory = $true)]
        [string]$Backdrop,

        [Parameter()]
        [object]$Accent
    )

    if ($null -eq [System.Windows.Application]::Current)
    {
        $app = [System.Windows.Application]::new()
        $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
    }

    $themeValue = [Fluence.Wpf.ApplicationTheme]$Theme
    $backdropValue = [Fluence.Wpf.BackdropType]$Backdrop
    [Fluence.Wpf.ApplicationThemeManager]::Apply($themeValue, $backdropValue, $true)

    if ($null -ne $Accent)
    {
        [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent([System.Windows.Media.Color]$Accent)
    }
    else
    {
        [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
    }
}
