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

Describe 'Select-VisualCaptures' {
    BeforeAll {
        $selector = Join-Path $PSScriptRoot 'Select-VisualCaptures.ps1'
        $fixture = Join-Path $TestDrive 'source'
        New-Item -ItemType Directory -Path $fixture -Force | Out-Null
        $script:page = Join-Path $fixture 'page.xaml'
        $script:other = Join-Path $fixture 'other.xaml'
        $script:shared = Join-Path $fixture 'shared.xaml'
        $script:mapPath = Join-Path $TestDrive 'map.psd1'
        $script:manifestPath = Join-Path $TestDrive 'manifest.json'
        $script:root = $fixture
        @'
@{ SchemaVersion = 1; Captures = @(
  @{ Id='gallery/page-light.png'; Path='docs/screenshots/gallery/page-light.png'; SourceRelativePath='screenshots/gallery/page-light.png'; Inputs=@('page.xaml','shared.xaml'); Family='gallery/page'; Theme='light'; State='default'; Mode='WpfRender' },
  @{ Id='gallery/page-dark.png'; Path='docs/screenshots/gallery/page-dark.png'; SourceRelativePath='screenshots/gallery/page-dark.png'; Inputs=@('page.xaml','shared.xaml'); Family='gallery/page'; Theme='dark'; State='default'; Mode='WpfRender' },
  @{ Id='gallery/page-light-disabled.png'; Path='docs/screenshots/gallery/page-light-disabled.png'; SourceRelativePath='screenshots/gallery/page-light-disabled.png'; Inputs=@('page.xaml','shared.xaml'); Family='gallery/page'; Theme='light'; State='disabled'; Mode='WpfRender' },
  @{ Id='gallery/other-light.png'; Path='docs/screenshots/gallery/other-light.png'; SourceRelativePath='screenshots/gallery/other-light.png'; Inputs=@('other.xaml','shared.xaml'); Family='gallery/other'; Theme='light'; State='default'; Mode='WpfRender' }
) }
'@ | Set-Content -LiteralPath $mapPath -Encoding utf8
        function Get-Candidates { & $selector -RepositoryRoot $script:root -MapPath $script:mapPath -WebsiteManifestPath $script:manifestPath }
        function Save-CheckedManifest {
            $result = Get-Candidates
            @{ schemaVersion = 1; files = @($result.candidates | ForEach-Object { @{ path = $_.path; checkedVisualInputSha256 = $_.checkedVisualInputSha256 } }) } |
                ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $script:manifestPath -Encoding utf8
        }
    }

    BeforeEach {
        '<Root><TextBlock>Page</TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        '<Root><TextBlock>Other</TextBlock></Root>' | Set-Content -LiteralPath $other -Encoding utf8
        '<Root><Style TargetType="Button" /></Root>' | Set-Content -LiteralPath $shared -Encoding utf8
        if (Test-Path $manifestPath) { Remove-Item -LiteralPath $manifestPath }
    }

    It 'selects exactly one page pair when that page changes' {
        Save-CheckedManifest
        '<Root><TextBlock>Changed</TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        @((Get-Candidates).candidates.id | Sort-Object) | Should -Be @('gallery/page-dark.png','gallery/page-light-disabled.png','gallery/page-light.png')
    }

    It 'selects bounded shared template dependents' {
        Save-CheckedManifest
        '<Root><Style TargetType="Button" /><Setter /></Root>' | Set-Content -LiteralPath $shared -Encoding utf8
        (Get-Candidates).candidates.Count | Should -Be 4
    }

    It 'skips unchanged content and a timestamp-only touch' {
        Save-CheckedManifest
        (Get-Candidates).candidates.Count | Should -Be 0
        [System.IO.File]::SetLastWriteTimeUtc($page, [DateTime]::UtcNow.AddMinutes(5))
        (Get-Candidates).candidates.Count | Should -Be 0
    }

    It 'selects changed content with the same modified time' {
        Save-CheckedManifest
        $before = [System.IO.File]::GetLastWriteTimeUtc($page)
        '<Root><TextBlock>Changed</TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        [System.IO.File]::SetLastWriteTimeUtc($page, $before)
        (Get-Candidates).candidates.Count | Should -Be 3
    }

    It 'ignores XML comments and attribute formatting' {
        Save-CheckedManifest
        '<Root><!-- safe --><TextBlock>Page</TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        '<Root><Style TargetType = "Button" /></Root>' | Set-Content -LiteralPath $shared -Encoding utf8
        (Get-Candidates).candidates.Count | Should -Be 0
    }

    It 'preserves xml:space and inline whitespace changes' {
        '<Root xml:space="preserve"><TextBlock><Run>A</Run> <Run>B</Run></TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        Save-CheckedManifest
        '<Root xml:space="preserve"><TextBlock><Run>A</Run>  <Run>B</Run></TextBlock></Root>' | Set-Content -LiteralPath $page -Encoding utf8
        (Get-Candidates).candidates.Count | Should -Be 3
    }

    It 'expands a single requested ID to its Light Dark and declared state family' {
        $result = & $selector -RepositoryRoot $root -MapPath $mapPath -WebsiteManifestPath $manifestPath -CaptureId 'gallery/page-light.png'
        @($result.candidates).Count | Should -Be 3
        @($result.candidates.id) | Should -Contain 'gallery/page-dark.png'
        @($result.candidates.id) | Should -Contain 'gallery/page-light-disabled.png'
    }

    It 'includes actual shared Generic and Typography XAML in every authored WPF capture' {
        $realMap = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'VisualCaptureMap.psd1') -SkipLimitCheck
        @($realMap.Captures | Where-Object { $_.Inputs -notcontains 'Fluence.Wpf/Themes/Generic.xaml' -or
            $_.Inputs -notcontains 'Fluence.Wpf/Themes/Typography/Typography.xaml' }).Count | Should -Be 0
        $realMap.Captures.Count | Should -BeGreaterThan 0
        @($realMap.Captures | Where-Object { $_.Id -like 'controls/*' -and
            $_.Inputs -notcontains 'Fluence.Wpf.Tests/Tools/GalleryScreenshotHarness.cs' }).Count | Should -Be 0
        foreach ($family in @('auto-suggest-box', 'combo-box', 'date-picker', 'drop-down-button', 'menu', 'split-button', 'time-picker', 'toggle-split-button')) {
            foreach ($theme in @('light', 'dark')) {
                @($realMap.Captures.Id) | Should -Contain "controls/$family-$theme-open.png"
            }
        }
        foreach ($case in @(@('navigation-view', 'compact'), @('tree-view', 'expanded'))) {
            foreach ($theme in @('light', 'dark')) {
                @($realMap.Captures.Id) | Should -Contain "controls/$($case[0])-$theme-$($case[1]).png"
            }
        }
    }

    It 'matches every catalog route and XAML sample in both themes without obsolete default IDs' {
        $repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
        $catalog = Get-Content -LiteralPath (Join-Path $repo 'Fluence.Wpf.Demo/DemoNavigationCatalog.cs') -Raw
        $matches = [regex]::Matches($catalog, 'DemoNavigationItem\.Create\("[^"]+", "(?<route>[^"]+)"[^\r\n]*?new (?<page>Gallery\w+Page)\(')
        $matches.Count | Should -BeGreaterThan 0
        $expected = [Collections.Generic.List[string]]::new()
        foreach ($match in $matches) {
            $route = $match.Groups['route'].Value.Replace(' ', '-')
            $page = Join-Path $repo "Fluence.Wpf.Demo/Pages/$($match.Groups['page'].Value).xaml"
            $page | Should -Exist
            [xml]$xaml = Get-Content -LiteralPath $page -Raw
            $cards = @($xaml.SelectNodes("//*[local-name()='DemoSampleControl']"))
            foreach ($theme in @('light','dark')) {
                $expected.Add("gallery/$route-$theme.png")
                for ($ordinal = 1; $ordinal -le $cards.Count; $ordinal++) {
                    $expected.Add(('gallery/{0}-sample-{1:00}-{2}.png' -f $route,$ordinal,$theme))
                }
            }
        }
        $map = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'VisualCaptureMap.psd1') -SkipLimitCheck
        $ids = @($map.Captures | Where-Object { $_.Id -like 'gallery/*' } | ForEach-Object { [string]$_.Id })
        $defaults = @($map.Captures | Where-Object { $_.Id -like 'gallery/*' -and $_.State -ceq 'default' } |
            ForEach-Object { [string]$_.Id })
        @($ids | Group-Object -CaseSensitive | Where-Object Count -gt 1 | ForEach-Object Name) | Should -Be @()
        @($expected | Where-Object { $defaults -cnotcontains $_ }) | Should -Be @()
        @($defaults | Where-Object { $expected -cnotcontains $_ }) | Should -Be @()
    }

    It 'declares acrylic software fallback and native material scenes separately' {
        $map = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'VisualCaptureMap.psd1') -SkipLimitCheck
        foreach ($theme in @('light', 'dark')) {
            foreach ($id in @("gallery/acrylic-$theme.png", "gallery/acrylic-sample-01-$theme.png", "gallery/acrylic-sample-02-$theme.png", "controls/acrylic-$theme-fallback.png")) {
                $entry = @($map.Captures | Where-Object Id -CEQ $id)
                $entry.Count | Should -Be 1
                $entry[0].Mode | Should -Be 'WpfRenderFallback'
            }
            foreach ($id in @("controls/acrylic-page-$theme-native.png", "controls/acrylic-direct-$theme-native.png", "controls/acrylic-host-$theme-native.png")) {
                $entry = @($map.Captures | Where-Object Id -CEQ $id)
                $entry.Count | Should -Be 1
                $entry[0].Mode | Should -Be 'NativeScreen'
            }
        }
    }

    It 'maps the Controls Tour categories and both themes from the real sidebar' {
        $repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
        $tourPath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module/examples/06-ControlsTour.ps1'
        $tour = [System.Management.Automation.Language.Parser]::ParseFile($tourPath, [ref]$null, [ref]$null)
        $xamlAssignment = $tour.Find({
            param($node)
            $node -is [System.Management.Automation.Language.AssignmentStatementAst] -and $node.Left.Extent.Text -ceq '$xaml'
        }, $true)
        [xml]$xaml = $xamlAssignment.Right.Expression.Value
        $categories = @($xaml.SelectNodes("//*[local-name()='ListBox' and @*[local-name()='Name']='CategoryList']/*[local-name()='ListBoxItem']") |
            ForEach-Object { [string]$_.Content })
        $categories.Count | Should -Be 10
        ($categories[-2..-1] -join ',') | Should -BeExactly 'Acrylic,Layout'

        $capturePath = Join-Path $repo 'Fluence.Wpf.PowerShell.Module/build/Capture-ControlsTour.ps1'
        $capture = [System.Management.Automation.Language.Parser]::ParseFile($capturePath, [ref]$null, [ref]$null)
        $category = $capture.ParamBlock.Parameters | Where-Object { $_.Name.VariablePath.UserPath -ceq 'Category' }
        $validateSet = $category.Attributes | Where-Object { $_.TypeName.FullName -ceq 'ValidateSet' }
        ($validateSet.PositionalArguments.Value -join ',') | Should -BeExactly ('All,' + ($categories -join ','))

        $harness = Get-Content -LiteralPath (Join-Path $repo 'Fluence.Wpf.Tests/Tools/GalleryScreenshotHarness.cs') -Raw
        $harnessList = [regex]::Match($harness, 'string\[\] categories = \[(?<names>[^\]]+)\];')
        $harnessCategories = [regex]::Matches($harnessList.Groups['names'].Value, '"(?<name>\w+)"') |
            ForEach-Object { $_.Groups['name'].Value }
        ($harnessCategories -join ',') | Should -BeExactly ($categories -join ',')

        $map = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'VisualCaptureMap.psd1') -SkipLimitCheck
        $tourEntries = @($map.Captures | Where-Object Family -CEQ 'powershell')
        $tourEntries.Count | Should -Be ($categories.Count * 2)
        foreach ($theme in @('light', 'dark')) {
            foreach ($name in $categories) {
                $id = if ($name -ceq 'Inputs') { "powershell-$theme.png" } else { "powershell-$($name.ToLowerInvariant())-$theme.png" }
                $entry = @($tourEntries | Where-Object Id -CEQ $id)
                $entry.Count | Should -Be 1
                $entry[0].Theme | Should -BeExactly $theme
                $expectedMode = if ($name -ceq 'Acrylic') { 'WpfRenderFallback' } else { 'WpfRender' }
                $entry[0].Mode | Should -BeExactly $expectedMode
                $entry[0].Inputs | Should -Contain 'Fluence.Wpf.PowerShell.Module/examples/06-ControlsTour.ps1'
            }
        }
    }

    It 'tracks the Gallery Colors code that creates the live acrylic tiles' {
        $map = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'VisualCaptureMap.psd1') -SkipLimitCheck
        $colors = @($map.Captures | Where-Object Family -CEQ 'gallery/colors')
        $colors.Count | Should -Be 2
        foreach ($entry in $colors) {
            $entry.Inputs | Should -Contain 'Fluence.Wpf.Demo/Pages/GalleryColorsPage.xaml.cs'
        }
    }

    It 'tracks the material controller, source map, budget, policy, and recipe inputs in every Acrylic scene' {
        $map = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'VisualCaptureMap.psd1') -SkipLimitCheck
        $acrylic = @($map.Captures | Where-Object { $_.Id -like 'controls/acrylic-*' -or $_.Id -like 'gallery/acrylic-*' })
        $acrylic.Count | Should -Be 14
        $required = @(
            'Fluence.Wpf/AcrylicMaterialRecipe.cs',
            'Fluence.Wpf/Theming/AcrylicRecipeFactory.cs',
            'Fluence.Wpf/Theming/ColorMap.cs',
            'Fluence.Wpf/ApplicationThemeManager.cs',
            'Fluence.Wpf/Theming/ThemeResolver.cs',
            'Fluence.Wpf/Theming/BaseColorTables.cs',
            'Fluence.Wpf/Theming/PublishFingerprint.cs',
            'Fluence.Wpf/Theming/BrushFactory.cs',
            'Fluence.Wpf/Theming/SpecialBrushes.cs',
            'Fluence.Wpf/Helpers/AcrylicRecipeSnapshot.cs',
            'Fluence.Wpf/Helpers/AcrylicMaterialController.cs',
            'Fluence.Wpf/Helpers/AcrylicSourceMap.cs',
            'Fluence.Wpf/Helpers/AcrylicSourceMapResult.cs',
            'Fluence.Wpf/Helpers/AcrylicMaterialBudget.cs',
            'Fluence.Wpf/Helpers/IAcrylicBudgetParticipant.cs',
            'Fluence.Wpf/Helpers/AcrylicEnvironmentMonitor.cs',
            'Fluence.Wpf/Helpers/RegistryHelper.cs',
            'Fluence.Wpf/Helpers/AcrylicMaterialPolicy.cs',
            'Fluence.Wpf/Helpers/AcrylicPolicySnapshot.cs',
            'Fluence.Wpf/Helpers/AcrylicPolicyResult.cs',
            'Fluence.Wpf/Helpers/AcrylicBlendEffect.cs',
            'Fluence.Wpf/Helpers/AcrylicNoiseHelper.cs',
            'Fluence.Wpf/Helpers/AcrylicMaterialMath.cs',
            'Fluence.Wpf/Helpers/AcrylicSourceReferenceWatcher.cs',
            'Fluence.Wpf/Helpers/FreezableChangedEventManager.cs',
            'Fluence.Wpf/Helpers/AcrylicSourceRegistration.cs',
            'Fluence.Wpf/Native/NativeMethods.cs',
            'Fluence.Wpf/NativeMethods.txt',
            'Fluence.Wpf/Theming/FluenceThemeEngine.cs',
            'Fluence.Wpf/Helpers/AcrylicFallbackReason.cs',
            'Fluence.Wpf/Helpers/IAcrylicPowerNative.cs',
            'Fluence.Wpf/Native/NonOwningWindowHandle.cs'
        )
        foreach ($entry in $acrylic) {
            foreach ($input in $required) {
                @($entry.Inputs) | Should -Contain $input -Because "$($entry.Id) must be recaptured when $input changes"
            }
        }
    }

    It 'nominates only the Acrylic family when a core file or binary shader changes' {
        $miniRoot = Join-Path $TestDrive 'acrylic-inputs'
        New-Item -ItemType Directory -Path $miniRoot -Force | Out-Null
        $controller = Join-Path $miniRoot 'AcrylicMaterialController.cs'
        $shader = Join-Path $miniRoot 'AcrylicBlend.ps'
        'controller-v1' | Set-Content -LiteralPath $controller -Encoding utf8
        [IO.File]::WriteAllBytes($shader, [byte[]]@(0, 255, 254, 1))
        '<Root />' | Set-Content -LiteralPath (Join-Path $miniRoot 'other.xaml') -Encoding utf8
        $miniMap = Join-Path $TestDrive 'acrylic-map.psd1'
        $miniManifest = Join-Path $TestDrive 'acrylic-manifest.json'
        @'
@{ SchemaVersion=1; Captures=@(
  @{ Id='controls/acrylic-light-fallback.png'; Path='docs/screenshots/controls/acrylic-light-fallback.png'; SourceRelativePath='screenshots/controls/acrylic-light-fallback.png'; Inputs=@('AcrylicMaterialController.cs','AcrylicBlend.ps'); Family='controls/acrylic'; Theme='light'; State='forced-fallback'; Mode='WpfRenderFallback' },
  @{ Id='controls/acrylic-dark-fallback.png'; Path='docs/screenshots/controls/acrylic-dark-fallback.png'; SourceRelativePath='screenshots/controls/acrylic-dark-fallback.png'; Inputs=@('AcrylicMaterialController.cs','AcrylicBlend.ps'); Family='controls/acrylic'; Theme='dark'; State='forced-fallback'; Mode='WpfRenderFallback' },
  @{ Id='gallery/other-light.png'; Path='docs/screenshots/gallery/other-light.png'; SourceRelativePath='screenshots/gallery/other-light.png'; Inputs=@('other.xaml'); Family='gallery/other'; Theme='light'; State='default'; Mode='WpfRender' }
) }
'@ | Set-Content -LiteralPath $miniMap -Encoding utf8
        $getMini = { & $selector -RepositoryRoot $miniRoot -MapPath $miniMap -WebsiteManifestPath $miniManifest }
        $baseline = & $getMini
        @{ schemaVersion=1; files=@($baseline.candidates | ForEach-Object { @{ path=$_.path; checkedVisualInputSha256=$_.checkedVisualInputSha256 } }) } |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $miniManifest -Encoding utf8
        'controller-v2' | Set-Content -LiteralPath $controller -Encoding utf8
        @((& $getMini).candidates.id | Sort-Object) | Should -Be @('controls/acrylic-dark-fallback.png','controls/acrylic-light-fallback.png')
        Remove-Item -LiteralPath $miniManifest
        $baseline = & $getMini
        @{ schemaVersion=1; files=@($baseline.candidates | ForEach-Object { @{ path=$_.path; checkedVisualInputSha256=$_.checkedVisualInputSha256 } }) } |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $miniManifest -Encoding utf8
        [IO.File]::WriteAllBytes($shader, [byte[]]@(0, 255, 254, 2))
        @((& $getMini).candidates.id | Sort-Object) | Should -Be @('controls/acrylic-dark-fallback.png','controls/acrylic-light-fallback.png')
    }

    It 'selects the real Acrylic map with its binary shader and noise inputs' {
        $repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
        $map = Join-Path $PSScriptRoot 'VisualCaptureMap.psd1'
        $result = & $selector -RepositoryRoot $repo -MapPath $map -CaptureId 'controls/acrylic-direct-light-native.png'
        @($result.candidates).Count | Should -Be 8
        @($result.candidates | Where-Object { $_.family -cne 'controls/acrylic' }).Count | Should -Be 0
        @($result.candidates | Where-Object { $_.mode -cne 'NativeScreen' -and $_.mode -cne 'WpfRenderFallback' }).Count | Should -Be 0
    }

    It 'nominates a mapped family after a shared XAML edit' {
        $sharedRoot = Join-Path $root 'Fluence.Wpf/Themes'
        New-Item -ItemType Directory -Path $sharedRoot -Force | Out-Null
        $generic = Join-Path $sharedRoot 'Generic.xaml'
        '<Root><Style /></Root>' | Set-Content -LiteralPath $generic -Encoding utf8
        $mapText = Get-Content -LiteralPath $mapPath -Raw
        $mapText.Replace("'page.xaml','shared.xaml'", "'Fluence.Wpf/Themes/Generic.xaml','page.xaml','shared.xaml'") |
            Set-Content -LiteralPath $mapPath -Encoding utf8
        Save-CheckedManifest
        '<Root><Style /><Setter /></Root>' | Set-Content -LiteralPath $generic -Encoding utf8
        @((Get-Candidates).candidates | Where-Object family -eq 'gallery/page').Count | Should -Be 3
        @((Get-Candidates).candidates | Where-Object family -eq 'gallery/other').Count | Should -Be 0
    }

    It 'rejects an input path through a junction when supported' {
        $outside = Join-Path $TestDrive 'outside'
        New-Item -ItemType Directory -Path $outside -Force | Out-Null
        Copy-Item -LiteralPath $page -Destination (Join-Path $outside 'page.xaml')
        try { New-Item -ItemType Junction -Path (Join-Path $root 'linked') -Target $outside -ErrorAction Stop | Out-Null }
        catch { Set-ItResult -Skipped -Because 'Junction creation unavailable'; return }
        $mapText = Get-Content -LiteralPath $mapPath -Raw
        $mapText.Replace("'page.xaml'", "'linked/page.xaml'") | Set-Content -LiteralPath $mapPath -Encoding utf8
        { Get-Candidates } | Should -Throw '*reparse point*'
    }
}
