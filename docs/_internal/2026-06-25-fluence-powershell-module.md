# Fluence.Wpf.PowerShell.Module Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Author module PowerShell with the powershell-master skill loaded. Honor the repository AGENTS.md and the PSADT instruction docs (powershell.md, pester.md, csharp.md) at all times.

**Goal:** Ship a declarative, in-process PowerShell module (`Fluence.Wpf.PowerShell`) that lets a scripter produce a themed Windows 11 / Fluent dialog and get the user's input back as data, with zero WPF code, on both Windows PowerShell 5.1 and PowerShell 7+.

**Architecture:** A script module that loads the existing `Fluence.Wpf.dll` (net472 under 5.1, net8.0-windows under 7+), seeds the Fluence theme slots, and renders a `FluenceWindow` from a declarative Prompts + Buttons spec on a guaranteed-STA UI thread, returning a `PSCustomObject` of results. Spec objects are tagged `PSCustomObject`s built by `New-FluencePrompt` / `New-FluenceButton`; no first-party C#.

**Tech Stack:** PowerShell 5.1 + 7 (dual edition), WPF via Fluence.Wpf, Pester v5, PSScriptAnalyzer (desktop-5.1 compatibility profile), .NET multi-targeting (net472 / net8.0-windows / net10.0-windows).

**Design spec:** `docs/superpowers/specs/2026-06-25-fluence-powershell-module-design.md` (read it first; this plan implements it).

---

## Conventions for every task (read once, apply always)

- **Branch first.** All work happens on a feature branch, never on `main`. Do not commit until a task says to, and never push unless the user asks.
- **PowerShell 5.1 + 7 syntax only.** No ternary, null-coalescing (`??`), null-conditional (`?.`), pipeline-chain (`&&` / `||`), or other 7-only operators in module code. The analyzer gate enforces this; write it correct the first time.
- **Allman braces, 4-space indent**, one public function per file, file name equals function name. Public functions: `[CmdletBinding()]`, deliberate parameter sets, comment-based help, `[OutputType(...)]`, and a `.NOTES` line stating whether a host application is required.
- **Use `[string]::IsNullOrWhiteSpace(...)`.** The `IsNullOrEmpty` string overload is banned.
- **Encoding/text policy (the repo lints every written text file):** UTF-8 with BOM, LF line endings, no em dash or en dash characters anywhere in `.ps1` / `.psm1` / `.psd1` / `.md`. After creating or editing any such file, normalize it (a helper command is given in Task 1.1).
- **Commit messages:** conventional style, no `Co-Authored-By` trailer (repository convention).
- **Subproject root** is `F:\FRebuild\Fluence.Wpf\Fluence.Wpf.PowerShell.Module`. In commands below, `$mod` means that path and `$repo` means `F:\FRebuild\Fluence.Wpf`.

---

## Phase 0: Library net8.0-windows target (prerequisite)

The library has no .NET 8 build, so PowerShell 7 (which runs on .NET 8/9) cannot load it today. This phase adds a `net8.0-windows` target. It must land and stay green before the module's PS7 lane can work.

### Task 0.1: Create the feature branch

**Step 1: Create and switch to the branch**

Run:
```bash
cd /f/FRebuild/Fluence.Wpf && git checkout -b feature/powershell-module
```
Expected: `Switched to a new branch 'feature/powershell-module'`

**Step 2: Confirm clean tree on the new branch**

Run: `git status`
Expected: `working tree clean`, `On branch feature/powershell-module`

### Task 0.2: Add the net8.0-windows target framework

**Files:**
- Modify: `Fluence.Wpf/Fluence.Wpf.csproj`

**Step 1: Add the TFM**

Change line 4 from:
```xml
    <TargetFrameworks>net472;net10.0-windows10.0.26100.0</TargetFrameworks>
```
to:
```xml
    <TargetFrameworks>net472;net8.0-windows10.0.26100.0;net10.0-windows10.0.26100.0</TargetFrameworks>
```

**Step 2: Give net8 the System.Threading.Lock polyfill**

`System.Threading.Lock` ships in .NET 9+. The net472 build polyfills it via Meziantou; the net8 build needs the same polyfill (but not `Microsoft.Bcl.Memory`, which net8 has natively). Immediately after the existing `<ItemGroup Condition="'$(TargetFramework)' == 'net472'">` block (the one with the two PackageReferences, ending at line 46), add:

```xml
  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0-windows10.0.26100.0'">
    <PackageReference Include="Meziantou.Polyfill" Version="1.0.152">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
```

The existing `MeziantouPolyfill_IncludedPolyfills` property (line 49-51) is unconditional and already lists `T:System.Threading.Lock`, so it applies to net8 automatically once the package is referenced. Leave it as is.

**Step 3: Restore to verify the project file parses**

Run: `dotnet restore F:\FRebuild\Fluence.Wpf\Fluence.Wpf\Fluence.Wpf.csproj`
Expected: restore succeeds, no MSB errors. If the `Microsoft.Windows.SDK.NET.Ref` pack cannot satisfy `net8.0-windows10.0.26100.0`, fall back to a lower Windows SDK floor for the net8 target only (try `net8.0-windows10.0.22621.0`, then `net8.0-windows10.0.19041.0`) and update Step 1, Step 2's condition, and every later command that names the net8 TFM to match.

### Task 0.3: Build net8 clean under the strict analyzers

**Step 1: Build the net8 target in Debug**

Run: `dotnet build F:\FRebuild\Fluence.Wpf\Fluence.Wpf\Fluence.Wpf.csproj -c Debug -f net8.0-windows10.0.26100.0`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

**Step 2: Fix any analyzer or API errors**

The build treats warnings as errors with `latest-all` analyzers. Most likely zero issues (there are no `#if` TFM conditionals and the only net9+ API in use, `System.Threading.Lock`, is polyfilled). If anything fails, fix the root cause in the library source; do not add suppressions. Re-run Step 1 until clean.

**Step 3: Build net8 in Release (CI parity)**

Run: `dotnet build F:\FRebuild\Fluence.Wpf\Fluence.Wpf\Fluence.Wpf.csproj -c Release -f net8.0-windows10.0.26100.0`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

**Step 4: Confirm the existing test lanes still pass**

The library tests stay on net472 + net10 (no third test lane). Run:
```bash
dotnet test F:\FRebuild\Fluence.Wpf\Fluence.Wpf.Tests\Fluence.Wpf.Tests.csproj -c Debug -f net472
```
Expected: all pass (baseline count). Then the same for `-f net10.0-windows10.0.26100.0`.
Expected: all pass.

### Task 0.4: Confirm the pack carries net8 and commit

**Step 1: Pack the library**

Run: `dotnet pack F:\FRebuild\Fluence.Wpf\Fluence.Wpf\Fluence.Wpf.csproj -c Release`
Expected: produces `Fluence.Wpf.<version>.nupkg`.

**Step 2: Verify the net8 lib folder is in the package**

Run:
```bash
unzip -l F:/FRebuild/Fluence.Wpf/Fluence.Wpf/bin/Release/Fluence.Wpf.*.nupkg | grep -i "lib/net"
```
Expected: lists `lib/net472/`, `lib/net8.0-windows10.0.26100.0/`, and `lib/net10.0-windows10.0.26100.0/` entries.

**Step 3: Commit**

```bash
cd /f/FRebuild/Fluence.Wpf
git add Fluence.Wpf/Fluence.Wpf.csproj
git commit -m "build: add net8.0-windows target to Fluence.Wpf for PowerShell 7 consumption"
```

---

## Phase 1: Module scaffold and dual-edition loader

### Task 1.1: Create the directory structure and the normalize helper

**Files:**
- Create: `Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/` (Public, Private, Types subfolders)
- Create: `Fluence.Wpf.PowerShell.Module/tests/`
- Create: `Fluence.Wpf.PowerShell.Module/examples/`
- Create: `Fluence.Wpf.PowerShell.Module/build/Convert-ToBomLf.ps1`

**Step 1: Make the folders**

Run:
```bash
mkdir -p "F:/FRebuild/Fluence.Wpf/Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Public" \
         "F:/FRebuild/Fluence.Wpf/Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Private" \
         "F:/FRebuild/Fluence.Wpf/Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Types" \
         "F:/FRebuild/Fluence.Wpf/Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/lib/net472" \
         "F:/FRebuild/Fluence.Wpf/Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/lib/net8.0-windows" \
         "F:/FRebuild/Fluence.Wpf/Fluence.Wpf.PowerShell.Module/tests" \
         "F:/FRebuild/Fluence.Wpf/Fluence.Wpf.PowerShell.Module/examples" \
         "F:/FRebuild/Fluence.Wpf/Fluence.Wpf.PowerShell.Module/build"
```

**Step 2: Create the encoding-normalize helper** (used after writing any text file)

Create `Fluence.Wpf.PowerShell.Module/build/Convert-ToBomLf.ps1`:
```powershell
<#
.SYNOPSIS
    Normalizes a text file to UTF-8 with BOM and LF line endings, and fails on em/en dashes.
.NOTES
    Repo text policy. Does not require a host application.
#>
[CmdletBinding()]
param
(
    [Parameter(Mandatory = $true)]
    [string[]]$Path
)

foreach ($item in $Path)
{
    $text = [System.IO.File]::ReadAllText($item)
    if ($text.Contains([char]0x2014) -or $text.Contains([char]0x2013))
    {
        throw "Em/en dash found in: $item"
    }
    $text = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $enc = [System.Text.UTF8Encoding]::new($true)
    [System.IO.File]::WriteAllText($item, $text, $enc)
}
```
Then run: `pwsh -File "F:/FRebuild/Fluence.Wpf/Fluence.Wpf.PowerShell.Module/build/Convert-ToBomLf.ps1" -Path "F:/FRebuild/Fluence.Wpf/Fluence.Wpf.PowerShell.Module/build/Convert-ToBomLf.ps1"`
Expected: no output, exit 0. (Run this helper on every text file you create from here on.)

### Task 1.2: Write the dual-edition path resolver (TDD)

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Private/Get-FluenceLibraryPath.ps1`
- Test: `tests/Get-FluenceLibraryPath.Tests.ps1`

**Step 1: Write the failing test**

Create `tests/Get-FluenceLibraryPath.Tests.ps1`:
```powershell
BeforeAll {
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/Get-FluenceLibraryPath.ps1"
    $script:root = Join-Path $TestDrive 'modroot'
    New-Item -ItemType Directory -Path (Join-Path $script:root 'lib/net472') -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $script:root 'lib/net8.0-windows') -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $script:root 'lib/net472/Fluence.Wpf.dll') -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $script:root 'lib/net8.0-windows/Fluence.Wpf.dll') -Force | Out-Null
}

Describe 'Get-FluenceLibraryPath' {
    Context 'Edition selection' {
        It 'returns the net8.0-windows path for Core' {
            $p = Get-FluenceLibraryPath -ModuleRoot $script:root -Edition 'Core'
            $p | Should -Match 'net8\.0-windows'
        }
        It 'returns the net472 path for Desktop' {
            $p = Get-FluenceLibraryPath -ModuleRoot $script:root -Edition 'Desktop'
            $p | Should -Match 'net472'
        }
    }
    Context 'Missing assembly' {
        It 'throws when the dll is absent' {
            $empty = Join-Path $TestDrive 'empty'
            New-Item -ItemType Directory -Path $empty -Force | Out-Null
            { Get-FluenceLibraryPath -ModuleRoot $empty -Edition 'Core' } | Should -Throw '*not found*'
        }
    }
}
```

**Step 2: Run it and confirm it fails**

Run: `Invoke-Pester -Path "$mod/tests/Get-FluenceLibraryPath.Tests.ps1" -Output Detailed`
Expected: FAIL (function not found / file to dot-source missing).

**Step 3: Write the implementation**

Create `src/Fluence.Wpf.PowerShell/Private/Get-FluenceLibraryPath.ps1`:
```powershell
function Get-FluenceLibraryPath
{
    <#
    .SYNOPSIS
        Resolves the path to Fluence.Wpf.dll for the running PowerShell edition.
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]$ModuleRoot,

        [Parameter()]
        [string]$Edition = $PSEdition
    )

    if ($Edition -eq 'Core')
    {
        $tfm = 'net8.0-windows'
    }
    else
    {
        $tfm = 'net472'
    }

    $dll = [System.IO.Path]::Combine($ModuleRoot, 'lib', $tfm, 'Fluence.Wpf.dll')
    if (-not (Test-Path -LiteralPath $dll))
    {
        throw "Fluence.Wpf.dll not found for edition '$Edition' at: $dll"
    }

    return $dll
}
```
Normalize it (Task 1.1 helper).

**Step 4: Run the test and confirm it passes**

Run: `Invoke-Pester -Path "$mod/tests/Get-FluenceLibraryPath.Tests.ps1" -Output Detailed`
Expected: PASS (3 tests).

### Task 1.3: Write the library loader (private)

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Private/Import-FluenceLibrary.ps1`

**Step 1: Write the implementation**

```powershell
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
```
Normalize it. (No standalone unit test: this touches process-global assembly state. It is exercised by the import smoke test in Task 1.6 and the spike in Phase 2.)

### Task 1.4: Write the module manifest

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1`

**Step 1: Author the manifest**

```powershell
@{
    RootModule           = 'Fluence.Wpf.PowerShell.psm1'
    ModuleVersion        = '0.1.0'
    GUID                 = '00000000-0000-0000-0000-000000000000'
    Author               = 'Dan Cunningham'
    CompanyName          = 'Dan Cunningham'
    Copyright            = 'Copyright (c) 2026 Dan Cunningham. All rights reserved.'
    Description          = 'Declarative Fluent (Windows 11) dialogs for PowerShell, built on Fluence.Wpf.'
    PowerShellVersion    = '5.1'
    CompatiblePSEditions = @('Desktop', 'Core')
    FunctionsToExport    = @(
        'Show-FluenceDialog',
        'New-FluencePrompt',
        'New-FluenceButton',
        'Show-FluenceMessage'
    )
    CmdletsToExport      = @()
    VariablesToExport    = @()
    AliasesToExport      = @()
    FormatsToProcess     = @('Types/Fluence.Format.ps1xml')
    PrivateData          = @{
        PSData = @{
            Tags       = @('GUI', 'WPF', 'Fluent', 'Windows11', 'Dialog', 'PSADT')
            ProjectUri = 'https://github.com/sintaxasn/Fluence.Wpf'
            LicenseUri = 'https://github.com/sintaxasn/Fluence.Wpf/blob/main/LICENSE'
        }
    }
}
```
Notes: generate a real GUID with `[guid]::NewGuid()` and paste it in. Add the two optional helper functions to `FunctionsToExport` only if Phase 5 ships them. Normalize the file.

**Step 2: Validate the manifest**

Run: `Test-ModuleManifest "$mod/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1"`
Expected: it parses and reports the module (the `Types/Fluence.Format.ps1xml` may warn until Task 6.1 creates it; that is acceptable for now, or create an empty valid ps1xml first).

### Task 1.5: Write the module loader (.psm1)

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psm1`

**Step 1: Author the loader**

```powershell
$script:ModuleRoot = $PSScriptRoot
$script:ModuleManifestPath = Join-Path $PSScriptRoot 'Fluence.Wpf.PowerShell.psd1'

# Dot-source private then public functions.
$private = @(Get-ChildItem -Path (Join-Path $script:ModuleRoot 'Private') -Filter '*.ps1' -ErrorAction SilentlyContinue)
$public  = @(Get-ChildItem -Path (Join-Path $script:ModuleRoot 'Public')  -Filter '*.ps1' -ErrorAction SilentlyContinue)
foreach ($file in @($private + $public))
{
    . $file.FullName
}

# Load the Fluence.Wpf assembly for this edition (idempotent across runspaces).
Import-FluenceLibrary -ModuleRoot $script:ModuleRoot

Export-ModuleMember -Function @($public | ForEach-Object { $_.BaseName })
```
Normalize it.

### Task 1.6: Stage the libraries and smoke-test import; commit

**Files:**
- Create: `build/Build-Module.ps1`

**Step 1: Write the lib-staging build script (first cut)**

Create `build/Build-Module.ps1`:
```powershell
<#
.SYNOPSIS
    Builds Fluence.Wpf for net472 and net8.0-windows and stages the assemblies into the module.
.NOTES
    Run from any location. Does not require a host application.
#>
[CmdletBinding()]
param
(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repo = 'F:\FRebuild\Fluence.Wpf'
$lib  = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\lib'
$proj = Join-Path $repo 'Fluence.Wpf\Fluence.Wpf.csproj'

$map = @{
    'net472'          = 'net472'
    'net8.0-windows'  = 'net8.0-windows10.0.26100.0'
}

foreach ($dest in $map.Keys)
{
    $tfm = $map[$dest]
    & dotnet build $proj -c $Configuration -f $tfm
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $tfm" }

    $src = Join-Path $repo "Fluence.Wpf\bin\$Configuration\$tfm"
    $out = Join-Path $lib $dest
    New-Item -ItemType Directory -Path $out -Force | Out-Null
    Get-ChildItem -Path $out -Filter '*.dll' | Remove-Item -Force
    Get-ChildItem -Path $src -Filter '*.dll' | Copy-Item -Destination $out -Force
}

Write-Host 'Staged Fluence.Wpf assemblies into the module lib folder.'
```
Normalize it.

**Step 2: Stage the libs**

Run: `pwsh -File "$mod/build/Build-Module.ps1" -Configuration Release`
Expected: `Fluence.Wpf.dll` (and its dependencies) present in both `lib/net472` and `lib/net8.0-windows`. Verify:
Run: `ls "$mod/src/Fluence.Wpf.PowerShell/lib/net8.0-windows/Fluence.Wpf.dll"` and the net472 equivalent.
Expected: both exist.

**Step 3: Smoke-test import on the current edition**

Run:
```bash
pwsh -NoProfile -Command "Import-Module '$mod/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1' -Force -Verbose; Get-Command -Module Fluence.Wpf.PowerShell"
```
Expected: imports without error; the assembly-load verbose line appears; `Get-Command` lists the exported functions (they are stubs until later phases, so this may list nothing until at least one Public function exists; create an empty `Public/Show-FluenceDialog.ps1` placeholder returning `$null` if needed to prove export wiring, then remove it in Phase 4).

**Step 4: Commit**

```bash
cd /f/FRebuild/Fluence.Wpf
git add Fluence.Wpf.PowerShell.Module
git commit -m "feat(ps): scaffold Fluence.Wpf.PowerShell module with dual-edition loader"
```

---

## Phase 2: STA host and theme bootstrap (the load-bearing risk)

This phase proves the hardest part early: getting a single WPF `Application` and an STA dispatcher under both a 5.1 STA console and an MTA `pwsh` host, and seeding the theme slots before showing a `FluenceWindow`. Build the reference implementation, then prove it with a spike before building the command surface.

### Task 2.1: Write the UI-thread router (private)

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Private/Invoke-OnFluenceUi.ps1`
- Create: `src/Fluence.Wpf.PowerShell/Private/Invoke-InFluenceStaRunspace.ps1`

**Step 1: Author the router**

`Invoke-OnFluenceUi.ps1`:
```powershell
function Invoke-OnFluenceUi
{
    <#
    .SYNOPSIS
        Runs a script on a UI (STA) thread that owns the single WPF Application.
    .DESCRIPTION
        Three cases: (1) a host application already runs its own pumped UI thread, so marshal
        onto its dispatcher; (2) this thread is STA and no application exists, so run inline and
        own the application here; (3) MTA host, so run on a persistent STA runspace we own.
    .NOTES
        Does not itself require a host application; it establishes one. State flag $script:OwnsApplication
        keeps routing stable once we have created our own application.
    #>
    [CmdletBinding()]
    [OutputType([object])]
    param
    (
        [Parameter(Mandatory = $true)]
        [scriptblock]$Script,

        [Parameter()]
        [object[]]$ArgumentList = @()
    )

    if ($script:OwnsApplication -ne $true -and $null -ne [System.Windows.Application]::Current)
    {
        $app = [System.Windows.Application]::Current
        $func = [System.Func[object]] { & $Script @ArgumentList }
        return $app.Dispatcher.Invoke($func)
    }

    if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -eq [System.Threading.ApartmentState]::STA)
    {
        $script:OwnsApplication = $true
        return (& $Script @ArgumentList)
    }

    $script:OwnsApplication = $true
    return (Invoke-InFluenceStaRunspace -Script $Script -ArgumentList $ArgumentList)
}
```

`Invoke-InFluenceStaRunspace.ps1`:
```powershell
function Invoke-InFluenceStaRunspace
{
    <#
    .SYNOPSIS
        Runs a script on a persistent module-owned STA runspace (for MTA hosts such as pwsh).
    .NOTES
        The runspace persists for the session so the WPF Application and dispatcher survive between
        dialogs. Each call runs synchronously; ShowDialog pumps its own modal loop on the STA thread.
    #>
    [CmdletBinding()]
    [OutputType([object])]
    param
    (
        [Parameter(Mandatory = $true)]
        [scriptblock]$Script,

        [Parameter()]
        [object[]]$ArgumentList = @()
    )

    if ($null -eq $script:StaRunspace -or $script:StaRunspace.RunspaceStateInfo.State -ne 'Opened')
    {
        $script:StaRunspace = [runspacefactory]::CreateRunspace()
        $script:StaRunspace.ApartmentState = 'STA'
        $script:StaRunspace.ThreadOptions  = 'ReuseThread'
        $script:StaRunspace.Open()

        $bootstrap = [powershell]::Create()
        $bootstrap.Runspace = $script:StaRunspace
        $null = $bootstrap.AddScript("Import-Module '$($script:ModuleManifestPath)' -Force").Invoke()
        $bootstrap.Dispose()
    }

    $ps = [powershell]::Create()
    $ps.Runspace = $script:StaRunspace
    $null = $ps.AddScript($Script)
    foreach ($arg in $ArgumentList)
    {
        $null = $ps.AddArgument($arg)
    }
    try
    {
        $output = $ps.Invoke()
        if ($ps.HadErrors -and $ps.Streams.Error.Count -gt 0)
        {
            throw $ps.Streams.Error[0]
        }
        return $output
    }
    finally
    {
        $ps.Dispose()
    }
}
```
Normalize both.

### Task 2.2: Write the window renderer entry point (private)

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Private/Invoke-FluenceWindow.ps1`

**Step 1: Author the renderer skeleton**

```powershell
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
```
Normalize it. `New-FluenceDialogWindow` is built in Phase 4; for the spike (Task 2.3) provide a temporary minimal version inline or as a stub that returns a bare `FluenceWindow` with one OK button.

### Task 2.3: Spike - prove a themed window shows and returns on both editions

**Files:**
- Create: `Fluence.Wpf.PowerShell.Module/build/Spike-ShowWindow.ps1` (temporary; delete after)

**Step 1: Write the spike**

```powershell
# Temporary spike. Proves Application + STA + theme slots + FluenceWindow on the current edition.
Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force

$spec = @{ Theme = 'Auto'; Backdrop = 'Mica'; AccentColor = $null; Title = 'Spike' }

$result = Invoke-OnFluenceUi -Script {
    param($s)

    if ($null -eq [System.Windows.Application]::Current)
    {
        $app = [System.Windows.Application]::new()
        $app.ShutdownMode = [System.Windows.ShutdownMode]::OnExplicitShutdown
    }
    [Fluence.Wpf.ApplicationThemeManager]::Apply([Fluence.Wpf.ApplicationTheme]::Auto, [Fluence.Wpf.BackdropType]::Mica, $true)
    [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()

    $w = [Fluence.Wpf.Controls.FluenceWindow]::new()
    $w.Title = $s.Title
    $w.Width = 360
    $w.Height = 200
    $w.SystemBackdropType = [Fluence.Wpf.BackdropType]::Mica

    $btn = [Fluence.Wpf.Controls.Button]::new()
    $btn.Content = 'OK'
    $btn.Margin = '24'
    $btn.add_Click({ $w.Close() })
    $w.Content = $btn

    $null = $w.ShowDialog()
    return 'closed-ok'
} -ArgumentList @($spec)

"Spike result: $result"
```
Normalize it.

**Step 2: Prove on PowerShell 7 (MTA path)**

Run: `pwsh -NoProfile -File "$mod/build/Spike-ShowWindow.ps1"`
Expected: a themed Mica window titled "Spike" with an OK button appears; clicking OK closes it; console prints `Spike result: closed-ok`. This exercises the STA-runspace branch.

**Step 3: Prove on Windows PowerShell 5.1 (inline STA path)**

Run: `powershell -NoProfile -File "$mod/build/Spike-ShowWindow.ps1"`
Expected: same behavior; this exercises the inline-STA branch and the net472 assembly load.

**Step 4: If either fails, fix the host design before continuing**

Likely fixes: dependency probing in `Import-FluenceLibrary` (net8 WinRT deps), or `$script:ModuleManifestPath` not resolving inside the STA runspace. Do not proceed to Phase 4 until both editions show and return.

**Step 5: Remove the spike and commit Phase 2**

```bash
rm "$mod/build/Spike-ShowWindow.ps1"
cd /f/FRebuild/Fluence.Wpf
git add Fluence.Wpf.PowerShell.Module
git commit -m "feat(ps): STA UI host and theme bootstrap, proven on 5.1 and 7"
```

---

## Phase 3: Spec builders and validation (TDD)

### Task 3.1: New-FluencePrompt

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Public/New-FluencePrompt.ps1`
- Test: `tests/New-FluencePrompt.Tests.ps1`

**Step 1: Write the failing test**

```powershell
BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'New-FluencePrompt' {
    Context 'Output contract' {
        It 'tags the object as Fluence.Prompt' {
            $p = New-FluencePrompt -Name City -Message 'City?'
            $p.PSObject.TypeNames[0] | Should -Be 'Fluence.Prompt'
        }
        It 'defaults InputType to Text' {
            (New-FluencePrompt -Name City -Message 'City?').InputType | Should -Be 'Text'
        }
        It 'carries Name, Message and DefaultValue' {
            $p = New-FluencePrompt -Name City -Message 'City?' -DefaultValue 'Leeds'
            $p.Name | Should -Be 'City'
            $p.Message | Should -Be 'City?'
            $p.DefaultValue | Should -Be 'Leeds'
        }
    }
    Context 'Input validation' {
        It 'rejects an unknown InputType' {
            { New-FluencePrompt -Name X -Message 'x' -InputType Nope } | Should -Throw
        }
        It 'requires Choice prompts to supply a ValidateSet' {
            { New-FluencePrompt -Name X -Message 'x' -InputType Choice } | Should -Throw '*ValidateSet*'
        }
    }
}
```

**Step 2: Run and confirm failure**

Run: `Invoke-Pester -Path "$mod/tests/New-FluencePrompt.Tests.ps1" -Output Detailed`
Expected: FAIL (command not found).

**Step 3: Implement**

```powershell
function New-FluencePrompt
{
    <#
    .SYNOPSIS
        Builds a single input-prompt specification for Show-FluenceDialog.
    .DESCRIPTION
        Returns a Fluence.Prompt object describing one input field: its name (the result key),
        message, input type, default value, and optional validation rules.
    .PARAMETER Name
        The result key under which the captured value is returned. Defaults to an auto name if omitted.
    .PARAMETER Message
        The label shown above (or beside) the input control.
    .PARAMETER InputType
        One of: Text, Multiline, Password, Number, Checkbox, Toggle, Choice, Date, Time,
        FileOpen, FileSave, FolderOpen, Link.
    .PARAMETER DefaultValue
        The initial value.
    .PARAMETER ValidateSet
        For Choice prompts, the allowed values. Required when InputType is Choice.
    .PARAMETER As
        For Choice prompts, how to render the set: Combo (default) or Radio.
    .PARAMETER ValidateNotEmpty
        Require a non-whitespace value before the dialog can close on a non-cancel button.
    .PARAMETER ValidatePattern
        A regular expression the value must match.
    .PARAMETER ValidateScript
        A scriptblock that receives the value and returns $true when valid.
    .EXAMPLE
        New-FluencePrompt -Name User -Message 'Account name' -ValidateNotEmpty
    .OUTPUTS
        Fluence.Prompt
    .NOTES
        Does not require a host application; this only builds a specification object.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.Prompt')]
    param
    (
        [Parameter()]
        [string]$Name,

        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Message,

        [Parameter()]
        [ValidateSet('Text', 'Multiline', 'Password', 'Number', 'Checkbox', 'Toggle',
            'Choice', 'Date', 'Time', 'FileOpen', 'FileSave', 'FolderOpen', 'Link')]
        [string]$InputType = 'Text',

        [Parameter()]
        [object]$DefaultValue,

        [Parameter()]
        [string[]]$ValidateSet,

        [Parameter()]
        [ValidateSet('Combo', 'Radio')]
        [string]$As = 'Combo',

        [Parameter()]
        [switch]$ValidateNotEmpty,

        [Parameter()]
        [string]$ValidatePattern,

        [Parameter()]
        [scriptblock]$ValidateScript
    )

    if ($InputType -eq 'Choice' -and ($null -eq $ValidateSet -or $ValidateSet.Count -eq 0))
    {
        throw "A Choice prompt requires -ValidateSet."
    }

    if ([string]::IsNullOrWhiteSpace($Name))
    {
        $Name = 'Input_' + [guid]::NewGuid().ToString('N').Substring(0, 8)
    }

    $prompt = [pscustomobject]@{
        PSTypeName       = 'Fluence.Prompt'
        Name             = $Name
        Message          = $Message
        InputType        = $InputType
        DefaultValue     = $DefaultValue
        ValidateSet      = $ValidateSet
        As               = $As
        ValidateNotEmpty = [bool]$ValidateNotEmpty
        ValidatePattern  = $ValidatePattern
        ValidateScript   = $ValidateScript
    }

    return $prompt
}
```
Normalize it.

**Step 4: Run and confirm pass**

Run: `Invoke-Pester -Path "$mod/tests/New-FluencePrompt.Tests.ps1" -Output Detailed`
Expected: PASS (5 tests).

### Task 3.2: New-FluenceButton

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Public/New-FluenceButton.ps1`
- Test: `tests/New-FluenceButton.Tests.ps1`

**Step 1: Failing test**

```powershell
BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'New-FluenceButton' {
    It 'tags the object as Fluence.Button' {
        (New-FluenceButton -Text 'OK').PSObject.TypeNames[0] | Should -Be 'Fluence.Button'
    }
    It 'defaults Name to Text when Name omitted' {
        (New-FluenceButton -Text 'Save').Name | Should -Be 'Save'
    }
    It 'flags IsDefault and IsCancel' {
        $b = New-FluenceButton -Text 'Cancel' -IsCancel
        $b.IsCancel | Should -BeTrue
        $b.IsDefault | Should -BeFalse
    }
}
```

**Step 2: Run, confirm fail.** Run: `Invoke-Pester -Path "$mod/tests/New-FluenceButton.Tests.ps1" -Output Detailed` -> FAIL.

**Step 3: Implement**

```powershell
function New-FluenceButton
{
    <#
    .SYNOPSIS
        Builds a single button specification for Show-FluenceDialog.
    .PARAMETER Text
        The button caption (and the default result key).
    .PARAMETER Name
        The result key; defaults to Text.
    .PARAMETER IsDefault
        Mark as the default button (activated by Enter).
    .PARAMETER IsCancel
        Mark as the cancel button (activated by Esc; skips input validation).
    .OUTPUTS
        Fluence.Button
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType('Fluence.Button')]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Text,

        [Parameter()]
        [string]$Name,

        [Parameter()]
        [switch]$IsDefault,

        [Parameter()]
        [switch]$IsCancel
    )

    if ([string]::IsNullOrWhiteSpace($Name))
    {
        $Name = $Text
    }

    return [pscustomobject]@{
        PSTypeName = 'Fluence.Button'
        Name       = $Name
        Text       = $Text
        IsDefault  = [bool]$IsDefault
        IsCancel   = [bool]$IsCancel
    }
}
```
Normalize it.

**Step 4: Run, confirm pass** (3 tests). 

### Task 3.3: Validation helper Test-FluenceInput (private, TDD)

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Private/Test-FluenceInput.ps1`
- Test: `tests/Test-FluenceInput.Tests.ps1`

**Step 1: Failing test**

```powershell
BeforeAll {
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/Test-FluenceInput.ps1"
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Public/New-FluencePrompt.ps1"
}

Describe 'Test-FluenceInput' {
    It 'fails an empty required value' {
        $p = New-FluencePrompt -Name U -Message 'u' -ValidateNotEmpty
        $r = Test-FluenceInput -Prompts @($p) -Values @{ U = '' }
        $r.IsValid | Should -BeFalse
        $r.Message | Should -Match 'U'
    }
    It 'passes a present required value' {
        $p = New-FluencePrompt -Name U -Message 'u' -ValidateNotEmpty
        (Test-FluenceInput -Prompts @($p) -Values @{ U = 'abc' }).IsValid | Should -BeTrue
    }
    It 'enforces a pattern' {
        $p = New-FluencePrompt -Name Code -Message 'c' -ValidatePattern '^\d{3}$'
        (Test-FluenceInput -Prompts @($p) -Values @{ Code = '12' }).IsValid | Should -BeFalse
        (Test-FluenceInput -Prompts @($p) -Values @{ Code = '123' }).IsValid | Should -BeTrue
    }
    It 'runs a ValidateScript' {
        $p = New-FluencePrompt -Name N -Message 'n' -ValidateScript { param($v) [int]$v -gt 5 }
        (Test-FluenceInput -Prompts @($p) -Values @{ N = '3' }).IsValid | Should -BeFalse
        (Test-FluenceInput -Prompts @($p) -Values @{ N = '9' }).IsValid | Should -BeTrue
    }
}
```

**Step 2: Run, confirm fail.**

**Step 3: Implement**

```powershell
function Test-FluenceInput
{
    <#
    .SYNOPSIS
        Validates captured dialog values against their prompt rules.
    .OUTPUTS
        A hashtable with IsValid (bool) and Message (string).
    .NOTES
        Does not require a host application.
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [object[]]$Prompts,

        [Parameter(Mandatory = $true)]
        [hashtable]$Values
    )

    foreach ($p in $Prompts)
    {
        $value = $Values[$p.Name]
        $asText = [string]$value

        if ($p.ValidateNotEmpty -and [string]::IsNullOrWhiteSpace($asText))
        {
            return @{ IsValid = $false; Message = "'$($p.Name)' is required." }
        }

        if (-not [string]::IsNullOrWhiteSpace($p.ValidatePattern) -and -not [string]::IsNullOrWhiteSpace($asText))
        {
            if ($asText -notmatch $p.ValidatePattern)
            {
                return @{ IsValid = $false; Message = "'$($p.Name)' does not match the required format." }
            }
        }

        if ($null -ne $p.ValidateScript)
        {
            $ok = $false
            try
            {
                $ok = [bool](& $p.ValidateScript $value)
            }
            catch
            {
                $ok = $false
            }
            if (-not $ok)
            {
                return @{ IsValid = $false; Message = "'$($p.Name)' failed validation." }
            }
        }
    }

    return @{ IsValid = $true; Message = '' }
}
```
Normalize it.

**Step 4: Run, confirm pass** (4 tests).

**Step 5: Commit Phase 3**

```bash
cd /f/FRebuild/Fluence.Wpf
git add Fluence.Wpf.PowerShell.Module
git commit -m "feat(ps): spec builders and input validation with tests"
```

---

## Phase 4: Core dialog (Show-FluenceDialog)

This phase builds the renderer (`New-FluenceDialogWindow`), the per-input-type control factory, and the public `Show-FluenceDialog`. UI rendering cannot run in headless CI, so split: unit-test the pure pieces (normalization, result shaping, button preset mapping), and verify rendering with the examples (manual / opt-in).

### Task 4.1: Normalize prompts and buttons (private, TDD)

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Private/ConvertTo-FluencePromptList.ps1`
- Create: `src/Fluence.Wpf.PowerShell/Private/ConvertTo-FluenceButtonList.ps1`
- Test: `tests/ConvertTo-FluenceLists.Tests.ps1`

**Step 1: Failing test** (covers: a bare string becomes a Text prompt; a bare string becomes a button; Fluence.Prompt/Fluence.Button pass through unchanged):
```powershell
BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/ConvertTo-FluencePromptList.ps1"
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/ConvertTo-FluenceButtonList.ps1"
}

Describe 'ConvertTo-FluencePromptList' {
    It 'turns a bare string into a Text prompt' {
        $list = ConvertTo-FluencePromptList -InputObject @('Your name?')
        $list[0].PSObject.TypeNames[0] | Should -Be 'Fluence.Prompt'
        $list[0].InputType | Should -Be 'Text'
        $list[0].Message | Should -Be 'Your name?'
    }
    It 'passes a Fluence.Prompt through' {
        $p = New-FluencePrompt -Name A -Message 'a'
        (ConvertTo-FluencePromptList -InputObject @($p))[0].Name | Should -Be 'A'
    }
}

Describe 'ConvertTo-FluenceButtonList' {
    It 'turns a bare string into a button' {
        $list = ConvertTo-FluenceButtonList -InputObject @('OK')
        $list[0].PSObject.TypeNames[0] | Should -Be 'Fluence.Button'
        $list[0].Text | Should -Be 'OK'
    }
}
```

**Step 2: Run, confirm fail.**

**Step 3: Implement both** (each iterates `$InputObject`; a `[string]` becomes `New-FluencePrompt -Message $_` or `New-FluenceButton -Text $_`; an object already typed `Fluence.Prompt` / `Fluence.Button` passes through; otherwise throw a clear error). Keep them small and Allman-style. Normalize.

**Step 4: Run, confirm pass.**

### Task 4.2: Input control factory (private)

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Private/New-FluenceInputControl.ps1`

**Step 1: Implement the factory**

One function that, given a `Fluence.Prompt` and the shared `$State` (result hashtable + accessor), returns the WPF control and wires its change event to write into `$State.Result[$prompt.Name]`. Map each InputType to the Fluence control:

| InputType | Control (namespace `Fluence.Wpf.Controls`) | Capture event |
| --- | --- | --- |
| Text / Multiline | `TextBox` (Multiline sets `AcceptsReturn`, `TextWrapping=Wrap`) | `add_TextChanged` writes `.Text` |
| Password | `PasswordBox` | `add_PasswordChanged` writes `.SecurePassword` |
| Number | `NumberBox` | `add_ValueChanged` writes `.Value` |
| Checkbox | `CheckBox` | `add_Click` writes `.IsChecked` |
| Toggle | `ToggleSwitch` | toggled event writes `.IsOn` |
| Choice (Combo) | `ComboBox` (items from `ValidateSet`) | `add_SelectionChanged` writes selected value |
| Choice (Radio) | `StackPanel` of `RadioButton` (shared GroupName) | `add_Checked` writes content |
| Date | `DatePicker` | `add_SelectedDateChanged` writes `.SelectedDate` |
| Time | `TimePicker` | selected-time event writes the time |
| FileOpen / FileSave / FolderOpen | `TextBox` + browse `Button` using `Microsoft.Win32.OpenFileDialog` / `SaveFileDialog` / a folder dialog | browse sets `.Text`; `add_TextChanged` captures |
| Link | `HyperlinkButton` (NavigateUri = DefaultValue) | n/a (records click if needed) |

Seed each control's initial value from `$prompt.DefaultValue`, and pre-seed `$State.Result[$prompt.Name]` with the default so an untouched field still returns its default. Use `DynamicResource`-driven Fluence controls only; do not set hard-coded colors. Verify each control type name exists in the library control list (all listed controls were confirmed present in `Fluence.Wpf/Controls`). Provide complete code for Text, Password, Number, Checkbox, and Choice(Combo) inline; follow the same pattern for the rest (the AnyBox `Show-AnyBox.ps1` is the behavioral reference, but render with Fluence controls and no inline colors). Normalize.

**Step 2: No unit test** (UI construction needs STA + assembly). It is covered by the example smoke runs in Phase 8 and an opt-in render test in Task 4.6.

### Task 4.3: The window builder (private)

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Private/New-FluenceDialogWindow.ps1`

**Step 1: Implement**

Build a `FluenceWindow`: set `Title`, `SystemBackdropType` from `$Spec.Backdrop`, `SizeToContent = WidthAndHeight`, `MinWidth`/`MinHeight`, `WindowStartupLocation`, `Topmost`, and `Owner` when `$Spec.ParentWindow` is provided. Compose content as a root `Grid` (or `StackPanel`) inside a padded `Border`:

1. Optional message `TextBlock` (from `$Spec.Message`).
2. For each normalized prompt: an optional label `TextBlock` plus the control from `New-FluenceInputControl`, stacked.
3. A horizontal panel of buttons from the normalized button list.

Wire buttons: a non-cancel button runs `Test-FluenceInput`; on failure show an inline Fluence `InfoBar` with the message and keep the window open; on success set `$State.Result[$button.Name] = $true` and call `$State.Window.Close()`. A cancel button sets its own flag and closes without validating, and sets `IsCancel`/`IsDefault` on the WPF button so Esc/Enter work. Initialize `$State.Result` with `Cancelled = $false` and a `$false` entry per button. Set `Cancelled = $true` in the window `Closed` handler when no button result is `$true`. Return the window. Normalize.

### Task 4.4: Result shaping (private, TDD)

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Private/ConvertTo-FluenceResult.ps1`
- Test: `tests/ConvertTo-FluenceResult.Tests.ps1`

**Step 1: Failing test** - the renderer accumulates a result hashtable; this converts it to the public `PSCustomObject` with a stable shape:
```powershell
BeforeAll {
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/ConvertTo-FluenceResult.ps1"
}
Describe 'ConvertTo-FluenceResult' {
    It 'projects the hashtable to a PSCustomObject with the same keys' {
        $h = @{ User = 'bob'; OK = $true; Cancel = $false; Cancelled = $false }
        $o = ConvertTo-FluenceResult -Result $h
        $o.User | Should -Be 'bob'
        $o.OK | Should -BeTrue
        $o.Cancelled | Should -BeFalse
    }
    It 'tags the object as Fluence.DialogResult' {
        (ConvertTo-FluenceResult -Result @{ Cancelled = $true }).PSObject.TypeNames[0] |
            Should -Be 'Fluence.DialogResult'
    }
}
```

**Step 2: Run, confirm fail. Step 3: Implement** (build a `[pscustomobject]` with `PSTypeName = 'Fluence.DialogResult'` from the hashtable keys). **Step 4: Run, confirm pass.**

### Task 4.5: Public Show-FluenceDialog

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Public/Show-FluenceDialog.ps1`

**Step 1: Implement**

```powershell
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
    .PARAMETER Prompts
        Strings or Fluence.Prompt objects (see New-FluencePrompt). A bare string becomes a Text prompt.
    .PARAMETER Buttons
        Strings or Fluence.Button objects (see New-FluenceButton). Defaults to a single OK button.
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
        Title                 = $Title
        Message               = $Message
        Prompts               = $promptList
        Buttons               = $buttonList
        Theme                 = $Theme
        Backdrop              = $Backdrop
        AccentColor           = $accentColor
        MinWidth              = $MinWidth
        Topmost               = [bool]$Topmost
        ParentWindow          = $ParentWindow
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
```
Normalize it. Remove the temporary placeholder `Public/Show-FluenceDialog.ps1` from Phase 1 first if present.

### Task 4.6: Opt-in render test and manual verification

**Files:**
- Create: `tests/Show-FluenceDialog.Render.Tests.ps1` (tagged, skipped unless `FLUENCE_PS_UI=1`)

**Step 1: Write the opt-in test** that, when `$env:FLUENCE_PS_UI -eq '1'`, builds a dialog with one Text prompt and an OK button, auto-closes it via a `DispatcherTimer` after a short delay, and asserts the result object has the prompt key and `Cancelled = $false`. Gate every `It` with `-Skip:($env:FLUENCE_PS_UI -ne '1')`. This mirrors the library's opt-in Screenshots pattern.

**Step 2: Manual verification on both editions**

Run (PowerShell 7): `pwsh -NoProfile -Command "Import-Module '$mod/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1' -Force; Show-FluenceDialog -Title 'Test' -Prompts (New-FluencePrompt -Name City -Message 'City?' -ValidateNotEmpty) -Buttons (New-FluenceButton -Text 'OK' -IsDefault),'Cancel' | Format-List"`
Expected: themed dialog; empty City + OK shows an inline InfoBar and stays open; typing a value + OK returns an object with `City` set, `OK = $true`, `Cancelled = $false`; Cancel returns `Cancelled = $true`.
Repeat with `powershell` (5.1).

**Step 3: Commit Phase 4**

```bash
cd /f/FRebuild/Fluence.Wpf
git add Fluence.Wpf.PowerShell.Module
git commit -m "feat(ps): Show-FluenceDialog renderer, control factory, and result shaping"
```

---

## Phase 5: Show-FluenceMessage and optional helpers

### Task 5.1: Button-preset mapping (private, TDD)

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Private/Get-FluenceButtonPreset.ps1`
- Test: `tests/Get-FluenceButtonPreset.Tests.ps1`

**Step 1: Failing test**
```powershell
BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
    . "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Private/Get-FluenceButtonPreset.ps1"
}
Describe 'Get-FluenceButtonPreset' {
    It 'maps OKCancel to two buttons with OK default and Cancel cancel' {
        $b = Get-FluenceButtonPreset -Preset OKCancel
        $b.Count | Should -Be 2
        ($b | Where-Object Text -eq 'OK').IsDefault | Should -BeTrue
        ($b | Where-Object Text -eq 'Cancel').IsCancel | Should -BeTrue
    }
    It 'maps YesNo to Yes/No' {
        (Get-FluenceButtonPreset -Preset YesNo | ForEach-Object Text) | Should -Be @('Yes', 'No')
    }
}
```

**Step 2: Run, confirm fail. Step 3: Implement** (switch over `OK`, `OKCancel`, `YesNo`, `YesNoCancel` returning `New-FluenceButton` objects with the right default/cancel flags). **Step 4: Run, confirm pass.**

### Task 5.2: Public Show-FluenceMessage

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Public/Show-FluenceMessage.ps1`

**Step 1: Implement** a thin wrapper: parameters `-Message`, `-Title`, `-Icon (Info|Warning|Error|Question|Success)`, `-Buttons (OK|OKCancel|YesNo|YesNoCancel)`, `-Theme`, `-Backdrop`. It maps `-Buttons` through `Get-FluenceButtonPreset`, calls `Show-FluenceDialog` with the message and no prompts, then returns the clicked button name (the result property among the button names that is `$true`, or `'Cancel'`/`$null` when cancelled). Full comment-based help with `[OutputType([string])]` and a `.NOTES` host line. Map `-Icon` to a leading Fluence `InfoBar` severity or a `FontIcon` glyph in the message area. Normalize.

**Step 2: Unit-test the name-selection logic** if it is factored into a pure helper; otherwise verify via the manual run below.

**Step 3: Manual verify**: `Show-FluenceMessage -Message 'Proceed?' -Icon Question -Buttons YesNo` returns `'Yes'` or `'No'`. Run on 5.1 and 7.

### Task 5.3: Optional Get-FluenceInput (only if cheap)

If time allows, add `Get-FluenceInput -Message -Title -DefaultValue -InputType` that wraps a single-prompt `Show-FluenceDialog` and returns the captured value directly (or `$null` on cancel). Add it to `FunctionsToExport`. Otherwise leave it for phase 2 and note that in the CHANGELOG.

**Step 4: Commit Phase 5**

```bash
cd /f/FRebuild/Fluence.Wpf
git add Fluence.Wpf.PowerShell.Module
git commit -m "feat(ps): Show-FluenceMessage with button presets"
```

---

## Phase 6: Formatting and manifest finalization

### Task 6.1: Format view for the result and spec objects

**Files:**
- Create: `src/Fluence.Wpf.PowerShell/Types/Fluence.Format.ps1xml`

**Step 1: Author a list/table view** for `Fluence.DialogResult`, `Fluence.Prompt`, and `Fluence.Button` so they print cleanly. Keep it minimal (a table view per type). Normalize. Validate by running `Update-FormatData -PrependPath` against it in a scratch session and confirming no parse error.

### Task 6.2: Finalize the manifest

**Step 1:** Confirm `FunctionsToExport` lists exactly the shipped public functions (4, or 5 if `Get-FluenceInput` shipped), `FormatsToProcess` points at the ps1xml, the GUID is real, and `ModuleVersion = '0.1.0'`. Re-run `Test-ModuleManifest` (expect clean). Commit:
```bash
git add Fluence.Wpf.PowerShell.Module
git commit -m "feat(ps): result/spec formatting and manifest finalization"
```

---

## Phase 7: Analyzer gate, full test run, and CI

### Task 7.1: PSScriptAnalyzer settings

**Files:**
- Create: `Fluence.Wpf.PowerShell.Module/PSScriptAnalyzerSettings.psd1`

**Step 1: Author the settings**
```powershell
@{
    Severity            = @('Error', 'Warning')
    IncludeDefaultRules = $true
    Rules               = @{
        PSUseCompatibleSyntax = @{
            Enable         = $true
            TargetVersions = @('5.1', '7.0')
        }
    }
}
```
Normalize it. (The compatibility rule is the gate that catches 7-only syntax.)

### Task 7.2: Run the analyzer and fix violations

**Step 1: Run**
Run: `Invoke-ScriptAnalyzer -Path "$mod/src" -Recurse -Settings "$mod/PSScriptAnalyzerSettings.psd1"`
Expected: no Error/Warning. Fix any (root cause; suppress only with a narrow justified `SuppressMessageAttribute` per pester.md/powershell.md). Re-run until clean.

### Task 7.3: Run the full logic-lane test suite

**Step 1: Run all non-UI tests**
Run: `Invoke-Pester -Path "$mod/tests" -Output Detailed -ExcludeTagFilter UI`
Expected: all pass. (Tag the opt-in render test `UI`.)

**Step 2: Run on both editions**
Repeat Step 1 under `powershell` (5.1) and `pwsh` (7) to confirm the spec builders and validation behave identically.

### Task 7.4: Wire CI

**Files:**
- Modify: `.github/workflows/build.yml`

**Step 1:** After the existing library test steps, add a job (or steps) that: runs `build/Build-Module.ps1` (which builds the library net8 + net472 and stages libs), runs `Invoke-ScriptAnalyzer` with the settings (fail on any finding), and runs `Invoke-Pester -ExcludeTagFilter UI` with NUnit/JUnit output. Keep it Windows-only. Do not run the UI tag in CI. Mirror the existing workflow's style and caching. Commit:
```bash
git add .github/workflows/build.yml Fluence.Wpf.PowerShell.Module/PSScriptAnalyzerSettings.psd1
git commit -m "ci: build, analyze, and test the Fluence.Wpf.PowerShell module"
```

---

## Phase 8: Examples, docs, changelog

### Task 8.1: Example scripts

**Files:**
- Create: `examples/QuickStart.ps1`, `examples/SignIn.ps1`, `examples/Message.ps1`, `examples/Form.ps1`

**Step 1:** Write four runnable examples mirroring the reference modules' style:
- `QuickStart.ps1`: `Show-FluenceMessage -Message 'Hello from PowerShell!' -Icon Success`.
- `Message.ps1`: a `YesNo` confirmation that branches on the result.
- `SignIn.ps1`: account + password prompts with `ValidateNotEmpty`, Login/Cancel buttons (mirror WinUIShell's SignInForm intent).
- `Form.ps1`: a mixed form (Text, Number, Choice, Date, Checkbox) returning an object that is then `Format-List`ed.
Each begins by importing the module from the staged path. Normalize all four.

**Step 2:** Run each on 7 and on 5.1; confirm they show and return sensible objects.

### Task 8.2: Module README

**Files:**
- Create: `Fluence.Wpf.PowerShell.Module/README.md`

**Step 1:** Write a README: what it is, requirements (Windows PowerShell 5.1 or PowerShell 7+, Windows 10 1809+), install/import, the three-level quick start from the spec, the command list, and a note that it renders with Fluence.Wpf theming. No em/en dashes. Normalize.

### Task 8.3: Repo CHANGELOG and docs link

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `docs/controls.md` or `README.md` (one line pointing to the new module), as appropriate

**Step 1:** Add a CHANGELOG entry under the current section: the new `Fluence.Wpf.PowerShell` module and the `net8.0-windows` library target. Keep it one to three lines. Commit:
```bash
git add Fluence.Wpf.PowerShell.Module CHANGELOG.md docs README.md
git commit -m "docs(ps): examples, module README, and changelog entry"
```

---

## Final verification and definition of done

Run this checklist before declaring the work complete. Use @superpowers:verification-before-completion.

1. **Library:** `dotnet build F:\FRebuild\Fluence.Wpf\Fluence.Wpf.sln -c Release` succeeds with zero warnings; the library produces net472, net8.0-windows, and net10 outputs.
2. **Library tests:** net472 and net10 test lanes pass at or above the HEAD baseline count.
3. **Module import:** imports clean on both `powershell` (5.1) and `pwsh` (7); `Get-Command -Module Fluence.Wpf.PowerShell` lists the four (or five) public functions.
4. **Analyzer:** `Invoke-ScriptAnalyzer` with the compatibility settings reports nothing.
5. **Logic tests:** `Invoke-Pester -ExcludeTagFilter UI` passes on both editions.
6. **Manual UI:** the four examples show a themed dialog and return correct objects on both editions; an empty required field blocks close with an inline InfoBar; Cancel sets `Cancelled = $true`.
7. **Docs:** README, CHANGELOG, and the spec are consistent with what shipped.
8. **Branch:** all work is on `feature/powershell-module`; nothing committed to `main`; nothing pushed unless the user asked.

**Done** means items 1-8 hold. Phase 2 features (builders/escape hatch, DataGrid, tabs/groups, progress, Set-FluenceTheme if not shipped) remain documented and unbuilt.

---

## Execution notes

- Reference skills: @superpowers:executing-plans (drive the plan), @superpowers:test-driven-development (the TDD tasks), @superpowers:subagent-driven-development (if dispatching per-task subagents), and the powershell-master skill for module authoring.
- The single biggest risk is Phase 2 (STA host across editions). Do not build Phase 4+ until the Task 2.3 spike shows and returns on both 5.1 and 7.
- Keep authoring and review in separate passes per AGENTS.md: after a phase, a separate review pass (for example @superpowers:requesting-code-review) before moving on.
