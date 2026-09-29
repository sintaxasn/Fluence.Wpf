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
    Captures the Features page animations from a live FluenceWindow.
.DESCRIPTION
    ProgressDurationTime is the ProgressBar's 2.0-second repeat period. Each
    theme, accent, or backdrop state lasts one complete period. Captures the
    visible screen pixels at 30 frames per second and encodes eight looping GIFs
    plus section-specific posters with ffmpeg. Raw frames and capture timing
    remain under artifacts/feature-animation for review.
.PARAMETER Only
    Optional animation name, for example controls-light.
.EXAMPLE
    powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File build/Capture-FeatureAnimations.ps1
#>
[CmdletBinding()]
[OutputType([void])]
param
(
    [Parameter()]
    [ValidateSet('controls-light', 'controls-dark', 'themes-light', 'themes-dark',
        'accents-light', 'accents-dark', 'backdrops-light', 'backdrops-dark')]
    [string]$Only
)

$ErrorActionPreference = 'Stop'
if ([System.Threading.Thread]::CurrentThread.GetApartmentState() -ne [System.Threading.ApartmentState]::STA)
{
    throw 'Run Capture-FeatureAnimations.ps1 with powershell.exe -STA.'
}

$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$modulePath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
$libraryPath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module\src\Fluence.Wpf.PowerShell\lib\net472\Fluence.Wpf.dll'
$iconPath = Join-Path $repo 'assets\Fluence_Icon_NoBacklground.ico'
$assetDirectory = Join-Path $repo 'website\static\images\features'
$captureDirectory = Join-Path $repo 'artifacts\feature-animation'
$null = [System.IO.Directory]::CreateDirectory($assetDirectory)
$null = [System.IO.Directory]::CreateDirectory($captureDirectory)

Import-Module $modulePath -Force
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
$ffmpeg = (Get-Command ffmpeg -ErrorAction Stop).Source

$fps = 30
$progressDurationTime = 2.0
$framesPerState = [int]($fps * $progressDurationTime)
$hues = @(
    [pscustomobject]@{ Name = 'Red'; Color = [System.Windows.Media.Color]::FromRgb(0xC4, 0x2B, 0x1C) }
    [pscustomobject]@{ Name = 'Orange'; Color = [System.Windows.Media.Color]::FromRgb(0xF7, 0x63, 0x0C) }
    [pscustomobject]@{ Name = 'Yellow'; Color = [System.Windows.Media.Color]::FromRgb(0xFF, 0xB9, 0x00) }
    [pscustomobject]@{ Name = 'LightGreen'; Color = [System.Windows.Media.Color]::FromRgb(0x6B, 0xB7, 0x00) }
    [pscustomobject]@{ Name = 'DarkGreen'; Color = [System.Windows.Media.Color]::FromRgb(0x10, 0x7C, 0x10) }
    [pscustomobject]@{ Name = 'DarkBlue'; Color = [System.Windows.Media.Color]::FromRgb(0x00, 0x57, 0xA8) }
    [pscustomobject]@{ Name = 'LightBlue'; Color = [System.Windows.Media.Color]::FromRgb(0x00, 0xA6, 0xED) }
    [pscustomobject]@{ Name = 'Purple'; Color = [System.Windows.Media.Color]::FromRgb(0x74, 0x37, 0xC9) }
    [pscustomobject]@{ Name = 'Pink'; Color = [System.Windows.Media.Color]::FromRgb(0xE3, 0x00, 0x8C) }
)

$variants = @(
    [pscustomobject]@{ Name = 'controls-light'; Theme = 'Light'; Kind = 'controls'; States = @('Light') }
    [pscustomobject]@{ Name = 'controls-dark'; Theme = 'Dark'; Kind = 'controls'; States = @('Dark') }
    [pscustomobject]@{ Name = 'themes-light'; Theme = 'Light'; Kind = 'themes'; States = @('Light', 'Dark', 'HighContrast') }
    [pscustomobject]@{ Name = 'themes-dark'; Theme = 'Dark'; Kind = 'themes'; States = @('Dark', 'Light', 'HighContrast') }
    [pscustomobject]@{ Name = 'accents-light'; Theme = 'Light'; Kind = 'accents'; States = @($hues) }
    [pscustomobject]@{ Name = 'accents-dark'; Theme = 'Dark'; Kind = 'accents'; States = @($hues) }
    [pscustomobject]@{ Name = 'backdrops-light'; Theme = 'Light'; Kind = 'backdrops'; States = @('None', 'Mica', 'Acrylic') }
    [pscustomobject]@{ Name = 'backdrops-dark'; Theme = 'Dark'; Kind = 'backdrops'; States = @('None', 'Mica', 'Acrylic') }
)

$xaml = @'
<fluence:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fluence="http://schemas.fluencewpf.com"
    Title="Fluence.Wpf"
    Width="500"
    Height="340"
    ResizeMode="NoResize"
    ExtendsContentIntoTitleBar="False"
    SystemBackdropType="None"
    Topmost="True"
    SnapsToDevicePixels="True"
    UseLayoutRounding="True">
    <Grid Margin="32">
        <StackPanel VerticalAlignment="Center">
            <TextBlock Text="Fluent controls adapt to your theme."
                       fluence:TextBlockExtensions.Typography="BodyStrong"
                       Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
            <Grid Margin="0,18,0,18">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="12" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <fluence:Button Grid.Column="0" Content="Primary action" Appearance="Accent" HorizontalAlignment="Stretch" FontSize="16" MinHeight="42" Padding="18,8" />
                <fluence:Button Grid.Column="2" Content="Secondary action" HorizontalAlignment="Stretch" FontSize="16" MinHeight="42" Padding="18,8" />
            </Grid>
            <Grid Margin="0,0,0,18">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="12" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>
                <fluence:CheckBox Grid.Column="0" Content="Remember choice" IsChecked="True" FontSize="14" />
                <fluence:ToggleSwitch Grid.Column="2" Content="Enabled" IsChecked="True" OnContent="On" OffContent="Off" FontSize="14" />
            </Grid>
            <fluence:Slider Minimum="0" Maximum="100" Value="64" AutomationProperties.Name="Accent level" Margin="0,0,0,18" />
            <fluence:ProgressBar x:Name="ProgressIndicator" IsIndeterminate="True" Height="8" AutomationProperties.Name="Progress" />
        </StackPanel>
    </Grid>
</fluence:FluenceWindow>
'@

foreach ($variant in $variants)
{
    if ($Only -and $variant.Name -ne $Only) { continue }

    $frameDirectory = Join-Path $captureDirectory $variant.Name
    $null = [System.IO.Directory]::CreateDirectory($frameDirectory)
    $data = @{
        Variant = $variant
        FramesPerState = $framesPerState
        Fps = $fps
        FrameDirectory = $frameDirectory
        NextFrame = 0
        Failure = $null
        Clock = $null
        Frames = [System.Collections.Generic.List[object]]::new()
        CurrentState = $variant.States[0]
        ProgressTranslate = $null
        ProgressTranslate2 = $null
    }
    $initialAccent = $hues[0].Color
    $initialBackdrop = 'None'
    try
    {
    $null = Show-FluenceWindow -Xaml $xaml -Theme $variant.Theme -Backdrop $initialBackdrop -Accent $initialAccent -TitleBarIcon $iconPath -Data $data -Initialize {
        param($Window, $Data)

        $periodMilliseconds = 1000.0 / $Data.Fps
        $frameCount = $Data.Variant.States.Count * $Data.FramesPerState
        $captureFrame = {
            param([int]$index)

            if ($index -gt 0 -and ($index % $Data.FramesPerState) -eq 0)
            {
                $stateIndex = [int]($index / $Data.FramesPerState)
                $state = $Data.Variant.States[$stateIndex]
                switch ($Data.Variant.Kind)
                {
                    'themes' { Set-FluenceTheme -Theme $state }
                    'accents' { Set-FluenceAccent -Color $state.Color }
                    'backdrops' { Set-FluenceBackdrop -Backdrop $state -Window $Window }
                }
                $Data.CurrentState = $state
            }
            $Window.UpdateLayout()
            if (-not $Window.IsActive) { $null = $Window.Activate() }

            if ($index -eq 0)
            {
                $progress = $Window.FindName('ProgressIndicator')
                if ($progress -isnot [System.Windows.FrameworkElement]) { throw 'ProgressIndicator was not found.' }
                $Data.ProgressTranslate = $progress.Template.FindName('PART_IndeterminateTranslate', $progress)
                $Data.ProgressTranslate2 = $progress.Template.FindName('PART_IndeterminateTranslate2', $progress)
                if ($Data.ProgressTranslate -isnot [System.Windows.Media.TranslateTransform] -or
                    $Data.ProgressTranslate2 -isnot [System.Windows.Media.TranslateTransform])
                {
                    throw 'Indeterminate ProgressBar animation transforms were not found.'
                }
            }

            $origin = $Window.PointToScreen([System.Windows.Point]::new(0, 0))
            $end = $Window.PointToScreen([System.Windows.Point]::new($Window.ActualWidth, $Window.ActualHeight))
            $width = [Math]::Max(1, [int][Math]::Round($end.X - $origin.X))
            $height = [Math]::Max(1, [int][Math]::Round($end.Y - $origin.Y))
            $framePath = Join-Path $Data.FrameDirectory ('frame-{0:D4}.png' -f $index)
            $bitmap = [System.Drawing.Bitmap]::new($width, $height)
            try
            {
                $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
                try
                {
                    $graphics.CopyFromScreen([int][Math]::Round($origin.X),
                        [int][Math]::Round($origin.Y), 0, 0,
                        [System.Drawing.Size]::new($width, $height))
                }
                finally { $graphics.Dispose() }
                $bitmap.Save($framePath, [System.Drawing.Imaging.ImageFormat]::Png)
            }
            catch
            {
                throw "Screen capture failed at ($($origin.X), $($origin.Y)), size ${width}x${height}, virtual screen $([System.Windows.Forms.SystemInformation]::VirtualScreen): $($_.Exception.Message)"
            }
            finally { $bitmap.Dispose() }

            $stateLabel = if ($Data.Variant.Kind -eq 'accents') { $Data.CurrentState.Name } else { [string]$Data.CurrentState }
            $Data.Frames.Add([ordered]@{
                index = $index
                expectedMilliseconds = [Math]::Round($index * $periodMilliseconds, 2)
                actualMilliseconds = [Math]::Round($Data.Clock.Elapsed.TotalMilliseconds, 2)
                state = $stateLabel
                progressX = [Math]::Round($Data.ProgressTranslate.X, 2)
                progress2X = [Math]::Round($Data.ProgressTranslate2.X, 2)
                width = $width
                height = $height
            })
        }.GetNewClosure()

        $timer = [System.Windows.Threading.DispatcherTimer]::new()
        $timer.Interval = [TimeSpan]::FromMilliseconds(5)
        $timer.add_Tick({
            try
            {
                if ($Data.NextFrame -lt $frameCount -and
                    $Data.Clock.Elapsed.TotalMilliseconds -ge ($Data.NextFrame * $periodMilliseconds))
                {
                    & $captureFrame $Data.NextFrame
                    $Data.NextFrame++
                }
                if ($Data.NextFrame -ge $frameCount)
                {
                    $timer.Stop()
                    $Window.Close()
                }
            }
            catch
            {
                $Data.Failure = $_.ToString()
                $timer.Stop()
                $Window.Close()
            }
        }.GetNewClosure())

        $Window.add_Loaded({
            $state = $Data
            $renderFrame = $captureFrame
            $samplingTimer = $timer
            $hostWindow = $Window
            $warmup = [System.Windows.Threading.DispatcherTimer]::new()
            $warmup.Interval = [TimeSpan]::FromMilliseconds(300)
            $warmup.add_Tick({
                $warmup.Stop()
                try
                {
                    $state.Clock = [System.Diagnostics.Stopwatch]::StartNew()
                    & $renderFrame 0
                    $state.NextFrame = 1
                    $samplingTimer.Start()
                }
                catch
                {
                    $state.Failure = $_.ToString()
                    $hostWindow.Close()
                }
            }.GetNewClosure())
            $warmup.Start()
        }.GetNewClosure())
    }
    }
    catch
    {
        if ($data.Failure) { throw "$($variant.Name): $($data.Failure)" }
        throw
    }

    if ($data.Failure) { throw "$($variant.Name): $($data.Failure)" }
    if ($data.Frames.Count -ne ($variant.States.Count * $framesPerState))
    {
        throw "$($variant.Name): Captured $($data.Frames.Count) of $($variant.States.Count * $framesPerState) frames."
    }

    $metadata = [ordered]@{
        source = 'Fluence.Wpf.PowerShell.Module/build/Capture-FeatureAnimations.ps1'
        variant = $variant.Name
        progressDurationTimeSeconds = $progressDurationTime
        framesPerSecond = $fps
        framesPerState = $framesPerState
        states = @($variant.States | ForEach-Object { if ($_ -is [string]) { $_ } else { $_.Name } })
        librarySha256 = (Get-FileHash -LiteralPath $libraryPath -Algorithm SHA256).Hash
        capture = 'Visible screen pixels of live FluenceWindow within its WPF window bounds'
        frames = $data.Frames.ToArray()
    }
    [System.IO.File]::WriteAllText((Join-Path $frameDirectory 'timeline.json'),
        (($metadata | ConvertTo-Json -Depth 7) + "`n"), [System.Text.UTF8Encoding]::new($true))

    $poster = Join-Path $assetDirectory ($variant.Name + '.png')
    $posterIndex = if ($variant.Kind -eq 'backdrops') { (2 * $framesPerState) + 30 } else { 0 }
    Copy-Item -LiteralPath (Join-Path $frameDirectory ('frame-{0:D4}.png' -f $posterIndex)) -Destination $poster -Force

    $gif = Join-Path $assetDirectory ($variant.Name + '.gif')
    $pattern = Join-Path $frameDirectory 'frame-%04d.png'
    $filter = '[0:v]split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a:diff_mode=rectangle'
    & $ffmpeg -hide_banner -loglevel error -y -framerate $fps -i $pattern -filter_complex $filter -loop 0 $gif
    if ($LASTEXITCODE -ne 0) { throw "ffmpeg failed for $($variant.Name) with exit code $LASTEXITCODE." }
    Write-Output "Captured $($variant.Name): $($data.Frames.Count) frames, $($data.Frames[0].width)x$($data.Frames[0].height), $gif"
}
