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
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidAssignmentToAutomaticVariable', '',
        Justification = 'Parameters named sender and eventArgs match the .NET ResolveEventHandler delegate signature; they are not PS automatic variable assignments.')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'sender',
        Justification = 'sender is a required positional parameter of the ResolveEventHandler delegate; the .NET runtime passes it but the scriptblock intentionally uses only eventArgs.')]
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
    }
    else
    {
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

    # The WPF-free spec assembly ships beside Fluence.Wpf.dll and must load on both editions even
    # when a host (for example PSADT) already loaded Fluence.Wpf from its own directory: probe that
    # directory first, then the module lib folder.
    $specsLoaded = [System.AppDomain]::CurrentDomain.GetAssemblies() |
        Where-Object { $_.GetName().Name -eq 'Fluence.Wpf.Specs' } |
        Select-Object -First 1
    if ($null -eq $specsLoaded)
    {
        $fluence = [System.AppDomain]::CurrentDomain.GetAssemblies() |
            Where-Object { $_.GetName().Name -eq 'Fluence.Wpf' } |
            Select-Object -First 1
        $candidateDirs = @()
        if ($null -ne $fluence -and -not [string]::IsNullOrWhiteSpace($fluence.Location))
        {
            $candidateDirs += [System.IO.Path]::GetDirectoryName($fluence.Location)
        }
        $candidateDirs += [System.IO.Path]::GetDirectoryName((Get-FluenceLibraryPath -ModuleRoot $ModuleRoot))

        $specsPath = $null
        foreach ($dir in $candidateDirs)
        {
            $candidate = [System.IO.Path]::Combine($dir, 'Fluence.Wpf.Specs.dll')
            if (Test-Path -LiteralPath $candidate)
            {
                $specsPath = $candidate
                break
            }
        }
        if ($null -eq $specsPath)
        {
            throw "Fluence.Wpf.Specs.dll was not found beside Fluence.Wpf.dll (searched: $($candidateDirs -join '; '))."
        }
        $null = [System.Reflection.Assembly]::LoadFrom($specsPath)
    }
}
