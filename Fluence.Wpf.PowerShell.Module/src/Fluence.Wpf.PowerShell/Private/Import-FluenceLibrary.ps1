function Import-FluenceLibrary
{
    <#
    .SYNOPSIS
        Loads Fluence.Wpf.dll for the current edition into the process, once.
    .NOTES
        Reuses an already-loaded Fluence.Wpf assembly (for example, when hosted by PSADT).
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([void])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]$ModuleRoot
    )

    $already = [System.AppDomain]::CurrentDomain.GetAssemblies() |
        Where-Object { $_.GetName().Name -eq 'Fluence.Wpf' } |
        Select-Object -First 1
    if ($null -ne $already)
    {
        Write-Verbose "Fluence.Wpf already loaded from: $($already.Location)"
        return
    }

    $dll = Get-FluenceLibraryPath -ModuleRoot $ModuleRoot
    $libDir = [System.IO.Path]::GetDirectoryName($dll)

    # Probe sibling dependencies (the net8 WinRT projections) from the lib folder.
    $resolver = [System.ResolveEventHandler] {
        param($sender, $eventArgs)
        $name = [System.Reflection.AssemblyName]::new($eventArgs.Name).Name
        $candidate = [System.IO.Path]::Combine($libDir, ($name + '.dll'))
        if (Test-Path -LiteralPath $candidate)
        {
            return [System.Reflection.Assembly]::LoadFrom($candidate)
        }
        return $null
    }
    [System.AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)

    $null = [System.Reflection.Assembly]::LoadFrom($dll)
}
