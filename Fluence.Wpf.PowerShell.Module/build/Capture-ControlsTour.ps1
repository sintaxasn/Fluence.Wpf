<#
Copyright (c) 2026, Dan Cunningham. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>

<#
.SYNOPSIS
    Captures all nine Controls Tour categories in Light and Dark themes.
.DESCRIPTION
    Opens the actual 06-ControlsTour.ps1 XAML and initializer, selects each category,
    waits for WPF layout, and writes opaque WPF RenderTargetBitmap PNGs. Each theme
    runs in a fresh STA process. The capture does not contain native DWM pixels.
.PARAMETER Theme
    Both, Light, or Dark. Both is the default.
.PARAMETER Category
    All categories or one named category. All is the default.
.PARAMETER OutputDirectory
    Candidate PNG destination. Defaults to ignored artifacts/docs/screenshots.
.PARAMETER WindowWidth
    WPF window width in device-independent pixels. Defaults to the tour's 1120.
.PARAMETER WindowHeight
    WPF window height in device-independent pixels. Defaults to the tour's 900.
.EXAMPLE
    powershell.exe -NoProfile -STA -File .\build\Capture-ControlsTour.ps1
.NOTES
    Requires a staged Fluence.Wpf.PowerShell module and a Windows desktop.
#>
[CmdletBinding()]
param(
    [ValidateSet('Both', 'Light', 'Dark')]
    [string]$Theme = 'Both',

    [ValidateSet('All', 'Inputs', 'Color', 'Selection', 'Commands', 'Data', 'Navigation', 'Status', 'Surfaces', 'Layout')]
    [string]$Category = 'All',

    [string]$OutputDirectory,

    [ValidateRange(1000, 3000)]
    [int]$WindowWidth = 1120,

    [ValidateRange(880, 3000)]
    [int]$WindowHeight = 900
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$tourPath = Join-Path $repoRoot 'Fluence.Wpf.PowerShell.Module\examples\06-ControlsTour.ps1'
$modulePath = Join-Path $repoRoot 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
if ([string]::IsNullOrWhiteSpace($OutputDirectory))
{
    $OutputDirectory = Join-Path $repoRoot 'artifacts\docs\screenshots'
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

function Get-CaptureFileSha256
{
    param([Parameter(Mandatory)][string]$Path)

    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    $stream = [System.IO.File]::OpenRead($Path)
    try
    {
        return [System.BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-', '')
    }
    finally
    {
        $stream.Dispose()
        $algorithm.Dispose()
    }
}

if ($Theme -eq 'Both')
{
    $hostExecutable = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    foreach ($singleTheme in @('Light', 'Dark'))
    {
        & $hostExecutable -NoProfile -STA -ExecutionPolicy Bypass -File $PSCommandPath -Theme $singleTheme -Category $Category -OutputDirectory $OutputDirectory -WindowWidth $WindowWidth -WindowHeight $WindowHeight
        if ($LASTEXITCODE -ne 0)
        {
            throw "$singleTheme Controls Tour capture failed with exit code $LASTEXITCODE."
        }
    }
    return
}

if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne [System.Threading.ApartmentState]::STA)
{
    throw 'Run a single-theme Controls Tour capture in an STA PowerShell host.'
}
if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf))
{
    throw "Module manifest is missing: $modulePath"
}
if (-not (Test-Path -LiteralPath $tourPath -PathType Leaf))
{
    throw "Controls Tour is missing: $tourPath"
}
[void](New-Item -ItemType Directory -Path $OutputDirectory -Force)

$tokens = $null
$parseErrors = $null
$tourAst = [System.Management.Automation.Language.Parser]::ParseFile($tourPath, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0)
{
    throw ($parseErrors | Out-String)
}
$xamlAssignment = $tourAst.Find({
        param($node)
        $node -is [System.Management.Automation.Language.AssignmentStatementAst] -and
        $node.Left.Extent.Text -eq '$xaml'
    }, $true)
$showCall = $tourAst.Find({
        param($node)
        $node -is [System.Management.Automation.Language.CommandAst] -and
        $node.GetCommandName() -eq 'Show-FluenceWindow'
    }, $true)
if ($null -eq $xamlAssignment -or $null -eq $showCall)
{
    throw 'The tour XAML or Show-FluenceWindow call could not be found.'
}
$xaml = $xamlAssignment.Right.Expression.Value
$initializeIndex = -1
for ($index = 0; $index -lt $showCall.CommandElements.Count; $index++)
{
    if ($showCall.CommandElements[$index] -is [System.Management.Automation.Language.CommandParameterAst] -and
        $showCall.CommandElements[$index].ParameterName -eq 'Initialize')
    {
        $initializeIndex = $index + 1
        break
    }
}
if ($initializeIndex -lt 0)
{
    throw 'The tour initializer could not be found.'
}
$initializeText = $showCall.CommandElements[$initializeIndex].ScriptBlock.GetScriptBlock().ToString()
$allCategories = @('Inputs', 'Color', 'Selection', 'Commands', 'Data', 'Navigation', 'Status', 'Surfaces', 'Layout')
$categories = if ($Category -eq 'All') { $allCategories } else { @($Category) }
$state = [hashtable]::Synchronized(@{
        InitializeText = $initializeText
        OutputDirectory = $OutputDirectory
        Theme = $Theme.ToLowerInvariant()
        Categories = @($categories)
        AllCategories = @($allCategories)
        WindowWidth = $WindowWidth
        WindowHeight = $WindowHeight
        Index = 0
        Pending = $false
        WaitTicks = 0
        DataSelectionReady = $false
        Started = $false
        Failure = $null
        Captures = [System.Collections.ArrayList]::new()
    })

$applicationType = 'System.Windows.Application' -as [type]
$applicationBeforeCapture = if ($null -ne $applicationType) { $applicationType::Current } else { $null }
try
{
    Import-Module $modulePath -Force
    $null = Show-FluenceWindow -Xaml $xaml -Theme $Theme -Backdrop None -Data $state -Initialize {
        param($Window, $Data)

        # The original initializer supplies real PowerShell objects and interaction handlers.
        & ([scriptblock]::Create($Data.InitializeText)) $Window $Data
        $Window.Width = $Data.WindowWidth
        $Window.Height = $Data.WindowHeight
        $Window.WindowStartupLocation = [System.Windows.WindowStartupLocation]::Manual
        $Window.Left = -10000
        $Window.Top = -10000
        $Window.ShowInTaskbar = $false
        $Window.ShowActivated = $false
        $Window.ResizeMode = [System.Windows.ResizeMode]::NoResize
        $Window.SizeToContent = [System.Windows.SizeToContent]::Manual
        [System.Windows.Application]::Current.Properties['ControlsTourCaptureState'] = $Data
        $Data.Window = $Window

        $timer = [System.Windows.Threading.DispatcherTimer]::new()
        $timer.Interval = [System.TimeSpan]::FromMilliseconds(250)
        $Data.Timer = $timer
        $timer.add_Tick({
                $capture = [System.Windows.Application]::Current.Properties['ControlsTourCaptureState']
                try
                {
                    if ($capture.Index -ge $capture.Categories.Count)
                    {
                        $capture.Timer.Stop()
                        $capture.Window.FindName('CloseButton').RaiseEvent(
                            [System.Windows.RoutedEventArgs]::new([System.Windows.Controls.Primitives.ButtonBase]::ClickEvent))
                        return
                    }
                    $categoryName = $capture.Categories[$capture.Index]
                    if (-not $capture.Pending)
                    {
                        $selectedIndex = [System.Array]::IndexOf($capture.AllCategories, $categoryName)
                        if ($selectedIndex -lt 0) { throw "Unknown category $categoryName." }
                        $capture.Window.FindName('CategoryList').SelectedIndex = $selectedIndex
                        $capture.Pending = $true
                        $capture.WaitTicks = 0
                        return
                    }

                    $capture.WaitTicks++
                    $capture.Window.UpdateLayout()
                    $page = $capture.Window.FindName("Page$categoryName")
                    if ($page.Visibility -ne [System.Windows.Visibility]::Visible -or
                        -not $page.IsLoaded -or $page.ActualWidth -le 0 -or $page.ActualHeight -le 0)
                    {
                        if ($capture.WaitTicks -le 12) { return }
                        throw "$categoryName page did not reach a visible layout."
                    }
                    # Allow a render pass after Loaded; do not photograph a category switch in progress.
                    if ($capture.WaitTicks -lt 2) { return }
                    if ($categoryName -eq 'Color')
                    {
                        $picker = $capture.Window.FindName('TourColorPicker')
                        if (-not $picker.IsLoaded -or $picker.ActualHeight -lt 400 -or
                            -not $picker.IsColorSpectrumVisible -or -not $picker.IsColorSliderVisible -or
                            -not $picker.IsColorChannelTextInputVisible)
                        {
                            throw 'The full ColorPicker was not visible.'
                        }
                    }
                    if ($categoryName -eq 'Data')
                    {
                        $taskGrid = $capture.Window.FindName('TaskGrid')
                        if ($taskGrid.Items.Count -ne 5)
                        {
                            throw 'The tour data grid did not contain five PowerShell rows.'
                        }
                        if (-not $capture.DataSelectionReady)
                        {
                            $taskGrid.SelectedIndex = 1
                            $capture.DataSelectionReady = $true
                            # Give DataGrid selection visuals and the status binding a render pass.
                            return
                        }
                        if ($capture.Window.FindName('StatusBar').Message -notlike "Selected 'Patch servers'*")
                        {
                            throw 'Selecting a PowerShell task did not update the shared status.'
                        }
                    }

                    # Every page owns its full content in the two-column grid. Measure the
                    # direct column hosts/cards against that viewport before rendering.
                    $contentRight = 0.0
                    $contentBottom = 0.0
                    $hostMetrics = @()
                    foreach ($child in $page.Children)
                    {
                        if ($child -isnot [System.Windows.FrameworkElement] -or
                            $child.Visibility -ne [System.Windows.Visibility]::Visible)
                        {
                            continue
                        }
                        $childRect = $child.TransformToAncestor($page).TransformBounds(
                            [System.Windows.Rect]::new(0, 0, $child.ActualWidth, $child.ActualHeight))
                        $contentRight = [System.Math]::Max($contentRight, $childRect.Right)
                        $contentBottom = [System.Math]::Max($contentBottom, $childRect.Bottom)
                        $hostMetrics += "column $([System.Windows.Controls.Grid]::GetColumn($child)): bottom $([math]::Round($childRect.Bottom, 1))"
                    }
                    $horizontalOverflow = [System.Math]::Max(0.0, $contentRight - $page.ActualWidth)
                    $verticalOverflow = [System.Math]::Max(0.0, $contentBottom - $page.ActualHeight)
                    if ($horizontalOverflow -gt 0.5 -or $verticalOverflow -gt 0.5)
                    {
                        throw "$categoryName content exceeds its page viewport by $([math]::Round($horizontalOverflow, 1)) x $([math]::Round($verticalOverflow, 1)) DIP at $($capture.WindowWidth) x $($capture.WindowHeight). Viewport height $([math]::Round($page.ActualHeight, 1)); $($hostMetrics -join '; ')."
                    }

                    $fileName = if ($categoryName -eq 'Inputs')
                    {
                        "powershell-$($capture.Theme).png"
                    }
                    else
                    {
                        "powershell-$($categoryName.ToLowerInvariant())-$($capture.Theme).png"
                    }
                    $window = $capture.Window
                    $width = [int][System.Math]::Ceiling($window.ActualWidth)
                    $height = [int][System.Math]::Ceiling($window.ActualHeight)
                    $background = $window.TryFindResource('ApplicationBackgroundBrush')
                    if ($background -isnot [System.Windows.Media.Brush])
                    {
                        throw 'ApplicationBackgroundBrush is unavailable for the opaque capture.'
                    }
                    $bounds = [System.Windows.Rect]::new(0, 0, $window.ActualWidth, $window.ActualHeight)
                    $visual = [System.Windows.Media.DrawingVisual]::new()
                    $context = $visual.RenderOpen()
                    try
                    {
                        $context.DrawRectangle($background, $null, $bounds)
                        $context.DrawRectangle([System.Windows.Media.VisualBrush]::new($window), $null, $bounds)
                    }
                    finally
                    {
                        $context.Close()
                    }
                    $bitmap = [System.Windows.Media.Imaging.RenderTargetBitmap]::new(
                        $width, $height, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
                    $bitmap.Render($visual)
                    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
                    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
                    $stream = [System.IO.MemoryStream]::new()
                    try
                    {
                        $encoder.Save($stream)
                        $png = $stream.ToArray()
                    }
                    finally
                    {
                        $stream.Dispose()
                    }
                    $destination = Join-Path $capture.OutputDirectory $fileName
                    $temporary = "$destination.tmp-$([System.Guid]::NewGuid().ToString('N'))"
                    $backup = "$destination.backup-$([System.Guid]::NewGuid().ToString('N'))"
                    $replaced = $false
                    try
                    {
                        [System.IO.File]::WriteAllBytes($temporary, $png)
                        if ([System.IO.File]::Exists($destination))
                        {
                            [System.IO.File]::Replace($temporary, $destination, $backup)
                            $replaced = $true
                        }
                        else
                        {
                            [System.IO.File]::Move($temporary, $destination)
                        }
                    }
                    finally
                    {
                        if ([System.IO.File]::Exists($temporary))
                        {
                            [System.IO.File]::Delete($temporary)
                        }
                        if ($replaced -and [System.IO.File]::Exists($backup))
                        {
                            [System.IO.File]::Delete($backup)
                        }
                    }
                    [void]$capture.Captures.Add([ordered]@{
                            category = $categoryName
                            theme = $capture.Theme
                            file = $fileName
                            width = $width
                            height = $height
                            pageViewportWidth = [math]::Round($page.ActualWidth, 1)
                            pageViewportHeight = [math]::Round($page.ActualHeight, 1)
                            contentRight = [math]::Round($contentRight, 1)
                            contentBottom = [math]::Round($contentBottom, 1)
                            horizontalOverflow = [math]::Round($horizontalOverflow, 1)
                            verticalOverflow = [math]::Round($verticalOverflow, 1)
                        })
                    $capture.Index++
                    $capture.Pending = $false
                }
                catch
                {
                    $capture.Failure = $_.Exception.ToString()
                    $capture.Timer.Stop()
                    $capture.Window.Close()
                }
            }.GetNewClosure())
        $Window.add_Loaded({
                $capture = [System.Windows.Application]::Current.Properties['ControlsTourCaptureState']
                if (-not $capture.Started)
                {
                    $capture.Started = $true
                    $capture.Timer.Start()
                }
            }.GetNewClosure())
    }
}
finally
{
    # This standalone STA capture owns only the Application it caused the module to create.
    # Finish its inline dispatcher before PowerShell tears down the pipeline thread.
    $applicationType = 'System.Windows.Application' -as [type]
    $application = if ($null -ne $applicationType) { $applicationType::Current } else { $null }
    if ($null -ne $application -and $application.Dispatcher.CheckAccess() -and
        [object]::ReferenceEquals($application.Properties['ControlsTourCaptureState'], $state))
    {
        if ($null -ne $state.Timer) { $state.Timer.Stop() }
        $application.Properties.Remove('ControlsTourCaptureState')
    }
    $createdApplication = $null -eq $applicationBeforeCapture -and $null -ne $application
    Write-Output "Controls Tour cleanup: created WPF Application = $createdApplication."
    if ($createdApplication)
    {
        if (-not $application.Dispatcher.CheckAccess())
        {
            throw 'Controls Tour cleanup cannot shut down an Application on a different dispatcher thread.'
        }
        if ($application.Windows.Count -gt 0)
        {
            throw "Controls Tour cleanup found $($application.Windows.Count) registered WPF window(s)."
        }
        if (-not $application.Dispatcher.HasShutdownStarted)
        {
            $application.Dispatcher.InvokeShutdown()
        }
        if (-not $application.Dispatcher.HasShutdownFinished)
        {
            throw 'Controls Tour cleanup did not finish the inline WPF dispatcher shutdown.'
        }
        Write-Output 'Controls Tour cleanup: inline WPF dispatcher shutdown finished.'
    }
}

if ($state.Failure)
{
    throw "Controls Tour capture failed: $($state.Failure)"
}
if ($state.Captures.Count -ne $categories.Count)
{
    throw "Captured $($state.Captures.Count) categories; expected $($categories.Count)."
}
foreach ($captureEntry in $state.Captures)
{
    $captureEntry['sha256'] = Get-CaptureFileSha256 (Join-Path $OutputDirectory $captureEntry['file'])
}

$libraryPath = [Fluence.Wpf.Controls.FluenceWindow].Assembly.Location
$head = (& git -C $repoRoot rev-parse HEAD 2>$null | Select-Object -First 1)
$trackedStatus = @(& git -c core.excludesFile= -C $repoRoot status --porcelain --untracked-files=no 2>$null)
$manifest = [ordered]@{
    captureMethod = 'WPF RenderTargetBitmap over ApplicationBackgroundBrush'
    limitation = 'WPF pixels only; native DWM backdrop, caption, and shadow are absent.'
    capturedUtc = [System.DateTime]::UtcNow.ToString('o')
    theme = $Theme.ToLowerInvariant()
    requestedWindowWidth = $WindowWidth
    requestedWindowHeight = $WindowHeight
    sourceScript = 'Fluence.Wpf.PowerShell.Module/examples/06-ControlsTour.ps1'
    sourceSha256 = (Get-CaptureFileSha256 $tourPath)
    renderer = 'Fluence.Wpf.PowerShell.Module/build/Capture-ControlsTour.ps1'
    rendererSha256 = (Get-CaptureFileSha256 $PSCommandPath)
    stagedLibraryPath = $libraryPath
    stagedLibrarySha256 = (Get-CaptureFileSha256 $libraryPath)
    gitHead = $head
    gitTrackedDirty = $trackedStatus.Count -gt 0
    captures = @($state.Captures)
}
$manifestName = if ($Category -eq 'All')
{
    "powershell-controls-tour-$($Theme.ToLowerInvariant()).json"
}
else
{
    "powershell-controls-tour-$($Category.ToLowerInvariant())-$($Theme.ToLowerInvariant()).json"
}
$manifestPath = Join-Path $OutputDirectory $manifestName
$manifestJson = ($manifest | ConvertTo-Json -Depth 6) + "`n"
[System.IO.File]::WriteAllText($manifestPath, $manifestJson, [System.Text.UTF8Encoding]::new($false))
Write-Output "Captured $($state.Captures.Count) $Theme Controls Tour PNGs in $OutputDirectory"
Write-Output "Provenance: $manifestPath"
