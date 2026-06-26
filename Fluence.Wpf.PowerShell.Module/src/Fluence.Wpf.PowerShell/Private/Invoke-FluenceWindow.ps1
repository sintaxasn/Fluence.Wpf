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

    # Ensure the Application exists and seed the three theme slots and accent. Mandatory before
    # showing a FluenceWindow; shared with the window host.
    Initialize-FluenceApplication -Theme $Spec.Theme -Backdrop $Spec.Backdrop -Accent $Spec.AccentColor

    $state = @{ Result = @{}; Window = $null }
    $window = New-FluenceDialogWindow -Spec $Spec -State $state
    $state.Window = $window

    $null = $window.ShowDialog()
    return $state.Result
}
