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
        return
    }

    $dll = Get-FluenceLibraryPath -ModuleRoot $ModuleRoot
    $libDir = [System.IO.Path]::GetDirectoryName($dll)

    # Load the sibling dependencies (the net8 WinRT projections) eagerly from the lib folder.
    #
    # This deliberately does NOT register an AppDomain.AssemblyResolve handler. A PowerShell
    # scriptblock can never be used safely as a ResolveEventHandler: the CLR raises
    # AssemblyResolve on whatever thread triggered the load, and when that thread has no
    # PowerShell execution context, ScriptBlock.GetContextFromTLS builds an ErrorRecord before
    # the scriptblock body runs. Formatting that record reads a resource string, which probes a
    # satellite assembly, which raises AssemblyResolve again - unbounded recursion ending in
    # STATUS_STACK_OVERFLOW (0xC00000FD), a hard process kill with no catchable exception. Because
    # the recursion happens *before* the body executes, no guard inside the body can prevent it.
    #
    # Reproduction of the old behaviour: import the module, then open any runspace
    # ([runspacefactory]::CreateRunspace().Open()). Provider initialisation reads a resource
    # string and the process dies. tests/Close-FluenceUiRunspace.Tests.ps1 covers it.
    #
    # Eager loading is also strictly more predictable: LoadFrom on Fluence.Wpf.dll already probes
    # its own directory for dependencies, so the handler only ever mattered for assemblies the
    # LoadFrom context missed. Loading them up front costs one directory enumeration and removes
    # a process-wide, never-unregistered hook.
    foreach ($sibling in [System.IO.Directory]::GetFiles($libDir, '*.dll'))
    {
        if ([System.IO.Path]::GetFileName($sibling) -eq 'Fluence.Wpf.dll')
        {
            continue
        }
        try
        {
            $null = [System.Reflection.Assembly]::LoadFrom($sibling)
        }
        catch
        {
            # A sibling that will not load is not fatal: Fluence.Wpf may not need it on this
            # edition, and if it does the LoadFrom below fails with a far clearer error.
            Write-Verbose "Skipped sibling assembly '$sibling': $($_.Exception.Message)"
        }
    }

    $null = [System.Reflection.Assembly]::LoadFrom($dll)
}
