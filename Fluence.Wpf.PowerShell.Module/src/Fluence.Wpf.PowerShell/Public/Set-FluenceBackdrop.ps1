function Set-FluenceBackdrop
{
    <#
    .SYNOPSIS
        Sets the Fluent system backdrop for the process (and optionally a specific window).
    .DESCRIPTION
        Runs on the UI (STA) thread. When -Window is supplied its SystemBackdropType is set, then
        the theme pipeline is re-applied with the requested backdrop and the current theme so the
        computed resources match. The accent intent is preserved.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, or None.
    .PARAMETER Window
        An optional System.Windows.Window whose SystemBackdropType is updated. When supplied it
        must be a window owned by the Fluence UI thread.
    .EXAMPLE
        Set-FluenceBackdrop -Backdrop Acrylic
    .EXAMPLE
        Set-FluenceBackdrop -Backdrop Mica -Window $window
    .NOTES
        Establishes a WPF Application on a private STA thread when none exists; reuses a host
        application (for example PSADT) when one is already running. When -Window is supplied it
        must be a window owned by the Fluence UI thread.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Mutates transient in-process WPF backdrop resources only; makes no persistent or destructive system change, so ShouldProcess prompting is not appropriate.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None')]
        [string]$Backdrop,

        [Parameter()]
        [System.Windows.Window]$Window
    )

    $null = Invoke-OnFluenceUi -Script {
        param($backdropName, $window)

        if ($null -eq [System.Windows.Application]::Current)
        {
            $app = [System.Windows.Application]::new()
            $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
        }

        $bd = [Fluence.Wpf.BackdropType]$backdropName
        if ($null -ne $window)
        {
            $window.SystemBackdropType = $bd
        }

        [Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationThemeManager]::CurrentTheme, $bd, $false)
    } -ArgumentList @($Backdrop, $Window)
}
