function Set-FluenceAccent
{
    <#
    .SYNOPSIS
        Sets the Fluent accent color, either to a custom color or back to the system accent.
    .DESCRIPTION
        Runs the Fluence accent resolver on the UI (STA) thread and re-runs the theme pipeline so
        every accent-derived brush is recomputed. Use -Color to pin the accent ramp to a custom
        color, or -System to reset the accent intent to the OS palette.
    .PARAMETER Color
        The custom accent color (System.Windows.Media.Color or a parseable string).
    .PARAMETER System
        Reset the accent intent to the system (OS) accent.
    .EXAMPLE
        Set-FluenceAccent -Color '#0078D4'
    .EXAMPLE
        Set-FluenceAccent -System
    .NOTES
        Establishes a WPF Application on a private STA thread when none exists; reuses a host
        application (for example PSADT) when one is already running.
    #>
    [CmdletBinding(DefaultParameterSetName = 'Custom')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '',
        Justification = 'Mutates transient in-process WPF accent resources only; makes no persistent or destructive system change, so ShouldProcess prompting is not appropriate.')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'System',
        Justification = 'Selects the System parameter set; its presence is read via $PSCmdlet.ParameterSetName, which PSScriptAnalyzer cannot statically trace.')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, ParameterSetName = 'Custom')]
        [System.Windows.Media.Color]$Color,

        [Parameter(Mandatory = $true, ParameterSetName = 'System')]
        [switch]$System
    )

    $null = Invoke-OnFluenceUi -Script {
        param($parameterSetName, $color)

        if ($null -eq [System.Windows.Application]::Current)
        {
            $app = [System.Windows.Application]::new()
            $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
        }

        if ($parameterSetName -eq 'System')
        {
            [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
        }
        else
        {
            [Fluence.Wpf.ApplicationAccentColorManager]::ApplyCustomAccent([System.Windows.Media.Color]$color)
        }
    } -ArgumentList @($PSCmdlet.ParameterSetName, $Color)
}
