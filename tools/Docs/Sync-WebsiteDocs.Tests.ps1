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
CAUSES AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#>
Describe 'Website documentation sync' {
    BeforeAll {
        $sync = Join-Path $PSScriptRoot 'Sync-WebsiteDocs.ps1'
        . (Join-Path $PSScriptRoot 'SourceDocsManifest.ps1')
        function Write-Fixture([string]$path,[string]$text) {
            [IO.Directory]::CreateDirectory((Split-Path -Parent $path)) | Out-Null
            [IO.File]::WriteAllText($path,$text)
        }
        function Write-ApiFixture([string]$repo,[string[]]$types=@('Widget')) {
            $api = Join-Path $repo 'artifacts/docs/api'
            Write-Fixture (Join-Path $api 'index.md') "# C# API reference`n`nFor guides, see [controls](../controls.md) and [theming](../theming.md).`n`n## Namespaces`n`n- [Example](Example/index.md) ($($types.Count) types)`n"
            $lines = @('# Example','','## Types','')
            foreach ($type in $types) {
                $lines += "- [$type]($type.md)"
                Write-Fixture (Join-Path $api "Example/$type.md") "# $type"
            }
            Write-Fixture (Join-Path $api 'Example/index.md') ($lines -join "`n")
        }
        function Get-TreeState([string]$root) {
            return (@(Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName | ForEach-Object {
                "$([IO.Path]::GetRelativePath($root,$_.FullName))=$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
            }) -join "`n")
        }
        function Write-CaptureFixture([string]$repo,[string]$siteDocs) {
            Write-Fixture (Join-Path $repo 'tools/Docs/VisualCaptureMap.psd1') "@{ SchemaVersion=1; Captures=@(@{ Id='gallery/button.png'; Path='docs/screenshots/gallery/button.png'; SourceRelativePath='screenshots/gallery/button.png'; Inputs=@('Fluence.Wpf.Demo/Pages/Button.xaml'); Theme='light'; State='default'; Mode='WpfRender' }) }"
            Write-Fixture (Join-Path $repo 'Fluence.Wpf.Demo/Pages/Button.xaml') '<Button />'
            Write-Fixture (Join-Path $siteDocs 'screenshots/gallery/button.png') 'website image'
            Write-Fixture (Join-Path $repo 'artifacts/docs/screenshots/gallery/button.png') 'candidate image'
            Write-Fixture (Join-Path $siteDocs 'screenshots/gallery/manifest.json') '{"captureMethod":"WPF RenderTargetBitmap","limitation":"preserve","entries":[{"route":"button","kind":"route","theme":"light","state":"default","file":"button.png","title":"old"},{"route":"other","kind":"route","theme":"light","state":"default","file":"other.png","title":"keep"}]}'
        }
        function Write-Approval([string]$path,[string]$repo,[string]$siteDocs,[string]$decision,[object[]]$galleryEntries=@()) {
            $imageBytes = if ($decision -eq 'material-change') {
                [IO.File]::ReadAllBytes((Join-Path $repo 'artifacts/docs/screenshots/gallery/button.png'))
            } else {
                [IO.File]::ReadAllBytes((Join-Path $siteDocs 'screenshots/gallery/button.png'))
            }
            $sha = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($imageBytes)).ToLowerInvariant()
            $inputHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes("Fluence.Wpf.Demo/Pages/Button.xaml`0<Button />"))).ToLowerInvariant()
            $capture = [ordered]@{
                id='gallery/button.png'; path='docs/screenshots/gallery/button.png'
                sourceRelativePath='screenshots/gallery/button.png'
                sha256=$sha; pngSha256=$sha; checkedVisualInputSha256=$inputHash
                inputs=@('Fluence.Wpf.Demo/Pages/Button.xaml'); theme='light'; state='default'
                mode='WpfRender'; decision=$decision; provenance='reviewed-example'
            }
            $approved = [ordered]@{ schemaVersion=1; captures=@($capture) }
            if ($galleryEntries.Count -gt 0) { $approved.galleryEntries = $galleryEntries }
            Write-Fixture $path (($approved | ConvertTo-Json -Depth 20))
        }
        function Initialize-NewGalleryFixture([string]$repo,[string]$siteDocs,[string]$caseRoot) {
            Write-CaptureFixture $repo $siteDocs
            & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
            $source = 'Fluence.Wpf.Demo/Pages/DataGrid.xaml'
            $path = 'docs/screenshots/gallery/data-grid-light.png'
            $relative = 'screenshots/gallery/data-grid-light.png'
            Write-Fixture (Join-Path $repo $source) '<DataGrid />'
            Write-Fixture (Join-Path $repo "artifacts/docs/$relative") 'new gallery image'
            $mapPath = Join-Path $repo 'tools/Docs/VisualCaptureMap.psd1'
            Write-Fixture $mapPath "@{ SchemaVersion=1; Captures=@(@{ Id='gallery/button.png'; Path='docs/screenshots/gallery/button.png'; SourceRelativePath='screenshots/gallery/button.png'; Inputs=@('Fluence.Wpf.Demo/Pages/Button.xaml'); Theme='light'; State='default'; Mode='WpfRender' }, @{ Id='gallery/data-grid-light.png'; Path='$path'; SourceRelativePath='$relative'; Inputs=@('$source'); Family='gallery/data-grid'; Theme='light'; State='default'; Mode='WpfRender' }) }"
            Write-Fixture (Join-Path $repo 'Fluence.Wpf.Tests/Tools/GalleryScreenshotHarness.cs') 'harness source'
            Write-Fixture (Join-Path $repo 'Fluence.Wpf.Demo/DemoNavigationCatalog.cs') 'catalog source'
            $binaryRoot = Join-Path $repo 'Fluence.Wpf.Tests/bin/Debug/net10.0-windows10.0.26100.0'
            $binaryHashes = @{}
            foreach ($name in 'Fluence.Wpf.Tests.dll','Fluence.Wpf.Demo.dll','Fluence.Wpf.dll') {
                $binary = Join-Path $binaryRoot $name
                Write-Fixture $binary "producer $name"
                $binaryHashes[$binary] = (Get-FileHash -LiteralPath $binary).Hash.ToLowerInvariant()
            }
            $sourceHashes = @{}
            foreach ($input in @('Fluence.Wpf.Demo/Pages/Button.xaml',$source,
                    'tools/Docs/VisualCaptureMap.psd1','Fluence.Wpf.Tests/Tools/GalleryScreenshotHarness.cs',
                    'Fluence.Wpf.Demo/DemoNavigationCatalog.cs')) {
                $sourceHashes[$input] = (Get-FileHash -LiteralPath (Join-Path $repo $input)).Hash.ToLowerInvariant()
            }
            $candidate = Join-Path $repo "artifacts/docs/$relative"
            $sha = (Get-FileHash -LiteralPath $candidate).Hash.ToLowerInvariant()
            $inputHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData(
                [Text.Encoding]::UTF8.GetBytes("$source`0<DataGrid />"))).ToLowerInvariant()
            $entry = @{route='data-grid'; kind='route'; theme='light'; state='default'; file='data-grid-light.png';
                title='DataGrid'; outputSha256=$sha; pixelWidth=1; pixelHeight=1;
                observedState=@{contentLoaded=$true; indicatorAligned=$true}}
            $receiptPath = Join-Path $repo 'artifacts/docs/screenshots/gallery/manifest.json'
            @{status='complete'; partial=$false; failure=$null; environment=@{os='test'};
                sourceInputRawSha256=$sourceHashes; producerBinarySha256=$binaryHashes; entries=@($entry)} |
                ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $receiptPath -Encoding utf8
            $approvalPath = Join-Path $caseRoot 'new-gallery-approval.json'
            @{schemaVersion=1; captures=@(@{id='gallery/data-grid-light.png'; path=$path; sourceRelativePath=$relative;
                sha256=$sha; pngSha256=$sha; checkedVisualInputSha256=$inputHash; inputs=@($source);
                theme='light'; state='default'; mode='WpfRender'; decision='new-capture'; provenance='reviewed-gallery-state';
                galleryReceiptSha256=(Get-FileHash -LiteralPath $receiptPath).Hash.ToLowerInvariant()});
                galleryEntries=@($entry)} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $approvalPath -Encoding utf8
            return [pscustomobject]@{ path=$path; approvalPath=$approvalPath; receiptPath=$receiptPath; source=$source;
                binaryPath=(Join-Path $binaryRoot 'Fluence.Wpf.Tests.dll'); sha=$sha }
        }
        function Initialize-DirtyManifestFixture([string]$repo,[string]$siteDocs) {
            Write-CaptureFixture $repo $siteDocs
            & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs | Out-Null
            $website = Split-Path -Parent $siteDocs
            & git -C $website init -q
            & git -C $website add .
            & git -C $website -c user.name=Fixture -c user.email=fixture@example.invalid commit -q -m baseline
            $manifestPath = Join-Path $website 'source-docs-manifest.json'
            $state = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            $capture = @($state.files | Where-Object path -eq 'docs/screenshots/gallery/button.png')[0]
            $capture.provenance = 'reviewed-draft'
            [IO.File]::WriteAllBytes($manifestPath,
                (ConvertTo-SourceDocsManifestByteArray $state @('docs/screenshots/gallery/button.png')))
            Write-Fixture (Join-Path $repo 'docs/controls.md') '# changed controls'
            return [pscustomobject]@{
                website=$website; manifestPath=$manifestPath
                manifestHash=(Get-FileHash -LiteralPath $manifestPath).Hash.ToLowerInvariant()
                imageHash=(Get-FileHash -LiteralPath (Join-Path $siteDocs 'screenshots/gallery/button.png')).Hash.ToLowerInvariant()
            }
        }
        function Initialize-VerifiedDraftFollowonFixture([string]$repo,[string]$siteDocs,[string]$caseRoot) {
            $draft = Initialize-DirtyManifestFixture $repo $siteDocs
            & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $draft.manifestHash | Out-Null
            $image = Join-Path $siteDocs 'screenshots/gallery/button.png'
            Write-Fixture $image 'owned draft image'
            $state = Get-Content -LiteralPath $draft.manifestPath -Raw | ConvertFrom-Json
            $record = @($state.files | Where-Object path -eq 'docs/screenshots/gallery/button.png')[0]
            $record.sha256 = (Get-FileHash -LiteralPath $image).Hash.ToLowerInvariant()
            $record.pngSha256 = $record.sha256
            [IO.File]::WriteAllBytes($draft.manifestPath,
                (ConvertTo-SourceDocsManifestByteArray $state @('docs/screenshots/gallery/button.png')))
            $approval = Join-Path $caseRoot 'approved-followon.json'
            Write-Approval $approval $repo $siteDocs 'material-change'
            return [pscustomobject]@{draft=$draft;image=$image;approval=$approval;
                hash=(Get-FileHash -LiteralPath $draft.manifestPath).Hash.ToLowerInvariant()}
        }
    }
    BeforeEach {
        $caseRoot = Join-Path $TestDrive ([guid]::NewGuid().ToString('N'))
        $repo = Join-Path $caseRoot 'source'
        $siteDocs = Join-Path $caseRoot 'website/docs'
        [IO.Directory]::CreateDirectory($siteDocs) | Out-Null
        Write-Fixture (Join-Path $repo 'Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1') "@{ FunctionsToExport = @('Get-FluenceWidget') }"
        Write-ApiFixture $repo
        Write-Fixture (Join-Path $repo 'artifacts/docs/powershell/reference/Get-FluenceWidget.mdx') '# Function'
        foreach ($name in 'README.md','input-types.md','result-objects.md') {
            Write-Fixture (Join-Path $repo "docs/powershell/reference/$name") "# $name"
        }
        foreach ($name in 'controls.md','theming.md','winui-parity.md') {
            Write-Fixture (Join-Path $repo "docs/$name") "# $name"
        }
    }
    It 'preflights complete references then leaves a second sync byte identical' {
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $first = Get-TreeState (Split-Path -Parent $siteDocs)
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $first
        @(Get-ChildItem -LiteralPath (Join-Path $repo 'artifacts/docs') -Directory -Filter 'sync-recovery-*').Count | Should -Be 0
        $json = Get-Content -Raw (Join-Path (Split-Path -Parent $siteDocs) 'source-docs-manifest.json') | ConvertFrom-Json
        @($json.files).Count | Should -Be 10
    }
    It 'does not mutate the website when API index is missing' {
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/index.md')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -DryRun } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'does not mutate the website when an exported function is missing' {
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/powershell/reference/Get-FluenceWidget.mdx')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'syncs an approved PowerShell tour capture without generated references and preserves reference records' {
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $siteRoot = Split-Path -Parent $siteDocs
        $manifestPath = Join-Path $siteRoot 'source-docs-manifest.json'
        $referenceBefore = @((Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json).files |
            Where-Object kind -eq 'reference' | ConvertTo-Json -Depth 8)
        $pagePath = Join-Path $siteDocs 'api/index.md'
        $pageBefore = [IO.File]::ReadAllBytes($pagePath)
        $path = 'docs/screenshots/powershell-color-light.png'
        $sourcePath = 'screenshots/powershell-color-light.png'
        Write-Fixture (Join-Path $repo 'tools/Docs/VisualCaptureMap.psd1') "@{ SchemaVersion=1; Captures=@(@{ Id='powershell-color-light.png'; Path='$path'; SourceRelativePath='$sourcePath'; Inputs=@('page.xaml'); Family='powershell'; Theme='light'; State='default'; Mode='WpfRender' }) }"
        Write-Fixture (Join-Path $repo 'page.xaml') '<Root>Page</Root>'
        $candidate = Join-Path $repo "artifacts/docs/$sourcePath"
        Write-Fixture $candidate 'approved image bytes'
        $sha = (Get-FileHash -LiteralPath $candidate).Hash.ToLowerInvariant()
        $inputHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes("page.xaml`0<Root>Page</Root>"))).ToLowerInvariant()
        $approvalPath = Join-Path $caseRoot 'approved.json'
        @{schemaVersion=1; captures=@(@{id='powershell-color-light.png'; path=$path; sourceRelativePath=$sourcePath;
            sha256=$sha; pngSha256=$sha; checkedVisualInputSha256=$inputHash; inputs=@('page.xaml');
            theme='light'; state='default'; mode='WpfRender'; decision='new-capture'; provenance='visual-review'})} |
            ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $approvalPath -Encoding utf8
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api') -Recurse -Force
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/powershell/reference') -Recurse -Force
        $before = Get-TreeState $siteRoot
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -CapturesOnly -ApprovedCaptureManifest $approvalPath -DryRun } | Should -Not -Throw
        (Get-TreeState $siteRoot) | Should -BeExactly $before
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approvalPath } | Should -Throw '*Generated API index.md is missing*'
        $newTarget = Join-Path $siteRoot $path
        Write-Fixture $newTarget 'unreviewed website image'
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -CapturesOnly -ApprovedCaptureManifest $approvalPath } | Should -Throw '*New capture baseline unexpectedly exists*'
        Remove-Item -LiteralPath $newTarget
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -CapturesOnly -ApprovedCaptureManifest $approvalPath
        (Get-FileHash -LiteralPath (Join-Path $siteRoot $path)).Hash.ToLowerInvariant() | Should -BeExactly $sha
        [Convert]::ToHexString([IO.File]::ReadAllBytes($pagePath)) | Should -BeExactly ([Convert]::ToHexString($pageBefore))
        $after = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        @($after.files | Where-Object kind -eq 'reference' | ConvertTo-Json -Depth 8) | Should -BeExactly $referenceBefore
        @($after.files | Where-Object path -eq $path)[0].decision | Should -Be 'new-capture'
    }
    It 'requires an approved manifest and an existing website ownership manifest in capture-only mode' {
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -CapturesOnly } | Should -Throw '*requires an approved*'
        $approvalPath = Join-Path $caseRoot 'approved.json'
        Write-Fixture $approvalPath '{"schemaVersion":1,"captures":[]}'
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -CapturesOnly -ApprovedCaptureManifest $approvalPath } | Should -Throw '*requires an existing*'
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -CapturesOnly -InitializeManifest -ApprovedCaptureManifest $approvalPath } | Should -Throw
    }
    It 'rejects missing or modified owned website references during capture-only sync' {
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $approvalPath = Join-Path $caseRoot 'approved.json'
        Write-Fixture $approvalPath '{"schemaVersion":1,"captures":[]}'
        $siteRoot = Split-Path -Parent $siteDocs
        $page = Join-Path $siteDocs 'api/index.md'
        $original = [IO.File]::ReadAllBytes($page)
        $manifest = Join-Path $siteRoot 'source-docs-manifest.json'
        $manifestBefore = [IO.File]::ReadAllBytes($manifest)
        Remove-Item -LiteralPath $page
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -CapturesOnly -ApprovedCaptureManifest $approvalPath } |
            Should -Throw '*Owned website page is missing*'
        [IO.File]::WriteAllBytes($page,$original)
        Write-Fixture $page '# local reference change'
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -CapturesOnly -ApprovedCaptureManifest $approvalPath } |
            Should -Throw '*differs from approved ownership*'
        [Convert]::ToHexString([IO.File]::ReadAllBytes($manifest)) | Should -BeExactly ([Convert]::ToHexString($manifestBefore))
        [IO.File]::ReadAllText($page) | Should -BeExactly '# local reference change'
    }
    It 'blocks dirty owned targets before any mutation' {
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        Write-Fixture (Join-Path $siteDocs 'api/index.md') '# local edit'
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'keeps an unowned website image' {
        Write-Fixture (Join-Path $siteDocs 'screenshots/gallery/unowned.png') 'website bytes'
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        [IO.File]::ReadAllText((Join-Path $siteDocs 'screenshots/gallery/unowned.png')) | Should -BeExactly 'website bytes'
    }
    It 'removes only obsolete reference pages owned by the prior manifest' {
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/Widget.md')
        Write-ApiFixture $repo @('Old','Gadget')
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        Write-Fixture (Join-Path $siteDocs 'api/unowned.md') '# keep'
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/Old.md')
        Write-ApiFixture $repo @('Gadget')
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        Test-Path -LiteralPath (Join-Path $siteDocs 'api/Example/Old.md') | Should -BeFalse
        [IO.File]::ReadAllText((Join-Path $siteDocs 'api/unowned.md')) | Should -BeExactly '# keep'
    }
    It 'preserves a concurrently edited obsolete page instead of deleting it' {
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/Widget.md')
        Write-ApiFixture $repo @('Old','Gadget')
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs | Out-Null
        $siteRoot = Split-Path -Parent $siteDocs
        $manifestPath = Join-Path $siteRoot 'source-docs-manifest.json'
        $beforeManifest = (Get-FileHash -LiteralPath $manifestPath).Hash
        $oldIndex = Get-Content -LiteralPath (Join-Path $siteDocs 'api/index.md') -Raw
        Write-ApiFixture $repo @('Gadget')
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/Old.md')
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_TARGET_RACE_PATH = 'docs/api/Example/Old.md'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } |
                Should -Throw '*website paths were restored*'
        } finally { Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_TARGET_RACE_PATH -ErrorAction SilentlyContinue }
        (Get-Content -LiteralPath (Join-Path $siteDocs 'api/Example/Old.md') -Raw) |
            Should -BeExactly 'concurrent user edit'
        (Get-Content -LiteralPath (Join-Path $siteDocs 'api/index.md') -Raw) | Should -BeExactly $oldIndex
        (Get-FileHash -LiteralPath $manifestPath).Hash | Should -BeExactly $beforeManifest
    }
    It 'rejects a missing linked namespace before <Mode> changes website bytes' -ForEach @(
        @{ Mode='Sync' }, @{ Mode='DryRun' }, @{ Mode='Initialize' }
    ) {
        Write-ApiFixture $repo
        if ($Mode -ne 'Initialize') { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs }
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/index.md')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        $switches = switch ($Mode) {
            'DryRun' { @{ DryRun=$true } }
            'Initialize' { @{ InitializeManifest=$true } }
            default { @{} }
        }
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs @switches } | Should -Throw '*Missing generated API namespace index*'
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'rejects a missing linked type before <Mode> changes website bytes' -ForEach @(
        @{ Mode='Sync' }, @{ Mode='DryRun' }, @{ Mode='Initialize' }
    ) {
        Write-ApiFixture $repo
        if ($Mode -ne 'Initialize') { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs }
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/Widget.md')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        $switches = switch ($Mode) {
            'DryRun' { @{ DryRun=$true } }
            'Initialize' { @{ InitializeManifest=$true } }
            default { @{} }
        }
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs @switches } | Should -Throw '*Missing generated API type page*'
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'accepts an updated type inventory and removes the formerly owned type only' {
        Write-ApiFixture $repo @('Widget','Gadget')
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/Example/Widget.md')
        Write-ApiFixture $repo @('Gadget')
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        Test-Path -LiteralPath (Join-Path $siteDocs 'api/Example/Widget.md') | Should -BeFalse
        Test-Path -LiteralPath (Join-Path $siteDocs 'api/Example/Gadget.md') | Should -BeTrue
        [IO.File]::ReadAllText((Join-Path $siteDocs 'api/Example/index.md')) | Should -Not -Match 'Widget'
    }
    It 'rejects a <Case> root inventory before <Mode> changes website bytes' -ForEach @(
        @{ Case='truncated'; Mode='Sync' }, @{ Case='truncated'; Mode='DryRun' }, @{ Case='truncated'; Mode='Initialize' },
        @{ Case='no-section'; Mode='Sync' }, @{ Case='no-section'; Mode='DryRun' }, @{ Case='no-section'; Mode='Initialize' },
        @{ Case='empty'; Mode='Sync' }, @{ Case='empty'; Mode='DryRun' }, @{ Case='empty'; Mode='Initialize' }
    ) {
        if ($Mode -ne 'Initialize') { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs }
        $api = Join-Path $repo 'artifacts/docs/api'
        switch ($Case) {
            'truncated' {
                Remove-Item -LiteralPath (Join-Path $api 'Example/index.md'),(Join-Path $api 'Example/Widget.md')
                Write-Fixture (Join-Path $api 'index.md') '# API'
            }
            'no-section' {
                Write-Fixture (Join-Path $api 'index.md') "# API`n`nFor guides, see [controls](../controls.md)."
            }
            'empty' {
                Remove-Item -LiteralPath (Join-Path $api 'Example/Widget.md')
                Write-Fixture (Join-Path $api 'index.md') "# API`n`n## Namespaces`n`n- [Example](Example/index.md) (0 types)"
                Write-Fixture (Join-Path $api 'Example/index.md') "# Example`n`n## Types`n"
            }
        }
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        $switches = switch ($Mode) {
            'DryRun' { @{ DryRun=$true } }
            'Initialize' { @{ InitializeManifest=$true } }
            default { @{} }
        }
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs @switches } | Should -Throw '*Generated API * inventory is *'
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'resolves fragments and escaped relative API inventory links' {
        Write-ApiFixture $repo
        $api = Join-Path $repo 'artifacts/docs/api'
        Write-Fixture (Join-Path $api 'Example/index.md') "# Example`n`n## Types`n`n- [Widget](Widget%2Emd#members)"
        Write-Fixture (Join-Path $api 'index.md') "# C# API reference`n`n## Namespaces`n`n- [Example](Example/index\.md#types) (1 types)"
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -DryRun } | Should -Not -Throw
    }
    It 'initializes matching website references and leaves the following sync byte identical' {
        foreach ($name in 'index.md','Example/index.md','Example/Widget.md') {
            $source = Join-Path $repo "artifacts/docs/api/$name"
            Write-Fixture (Join-Path $siteDocs "api/$name") ([IO.File]::ReadAllText($source))
        }
        Write-Fixture (Join-Path $siteDocs 'powershell/reference/Get-FluenceWidget.mdx') '# Function'
        foreach ($name in 'README.md','input-types.md','result-objects.md') {
            Write-Fixture (Join-Path $siteDocs "powershell/reference/$name") "# $name"
        }
        foreach ($name in 'controls.md','theming.md','winui-parity.md') {
            Write-Fixture (Join-Path $siteDocs $name) "# $name"
        }
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -InitializeManifest
        [IO.File]::ReadAllText((Join-Path $siteDocs 'api/index.md')) | Should -BeExactly ([IO.File]::ReadAllText((Join-Path $repo 'artifacts/docs/api/index.md')))
        $state = Get-Content -Raw (Join-Path (Split-Path -Parent $siteDocs) 'source-docs-manifest.json') | ConvertFrom-Json
        (@($state.files | Where-Object path -eq 'docs/api/index.md')[0]).provenance | Should -Be 'source-generated'
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'refuses initialization with a missing API index before website writes' {
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/api/index.md')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -InitializeManifest } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'refuses initialization with a missing exported function before website writes' {
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/powershell/reference/Get-FluenceWidget.mdx')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -InitializeManifest } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'refuses a divergent website reference during initialization' {
        Write-Fixture (Join-Path $siteDocs 'api/index.md') '# website wins'
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -InitializeManifest } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'rejects a valid but dirty tracked root manifest' {
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $website = Split-Path -Parent $siteDocs
        & git -C $website init -q
        & git -C $website add source-docs-manifest.json
        & git -C $website -c user.name=Fixture -c user.email=fixture@example.invalid commit -q -m baseline
        $statePath = Join-Path $website 'source-docs-manifest.json'
        $edited = [IO.File]::ReadAllText($statePath).Replace('source-generated','local-website-edit')
        [IO.File]::WriteAllText($statePath,$edited)
        $before = Get-TreeState $website
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw '*Dirty tracked website source docs manifest*'
        (Get-TreeState $website) | Should -BeExactly $before
    }
    It 'accepts an explicitly hashed dirty manifest while preserving reviewed image records and bytes' {
        $draft = Initialize-DirtyManifestFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $draft.manifestHash -DryRun |
            Should -Match 'Dry run: 2 managed website operation'
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $draft.manifestHash | Out-Null
        (Get-Content -LiteralPath (Join-Path $siteDocs 'controls.md') -Raw) | Should -BeExactly '# changed controls'
        (Get-FileHash -LiteralPath (Join-Path $siteDocs 'screenshots/gallery/button.png')).Hash.ToLowerInvariant() |
            Should -BeExactly $draft.imageHash
        $after = Get-Content -LiteralPath $draft.manifestPath -Raw | ConvertFrom-Json
        @($after.files | Where-Object path -eq 'docs/screenshots/gallery/button.png')[0].provenance |
            Should -BeExactly 'reviewed-draft'
    }
    It 'keeps the dirty-manifest guard without an explicit hash' {
        $draft = Initialize-DirtyManifestFixture $repo $siteDocs
        $before = Get-TreeState $draft.website
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -DryRun } |
            Should -Throw '*Dirty tracked website source docs manifest*'
        (Get-TreeState $draft.website) | Should -BeExactly $before
    }
    It 'rejects malformed or incorrect expected hashes before changing a draft website' {
        $draft = Initialize-DirtyManifestFixture $repo $siteDocs
        $before = Get-TreeState $draft.website
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash 'not-a-sha256' } |
            Should -Throw '*must be a 64-character SHA-256*'
        (Get-TreeState $draft.website) | Should -BeExactly $before
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash ('0' * 64) } |
            Should -Throw '*differs from expected current SHA-256*'
        (Get-TreeState $draft.website) | Should -BeExactly $before
    }
    It 'rejects an acknowledged draft when an owned image or unchanged reference differs from its record' {
        $draft = Initialize-DirtyManifestFixture $repo $siteDocs
        $image = Join-Path $siteDocs 'screenshots/gallery/button.png'
        Write-Fixture $image 'unreviewed image bytes'
        $before = Get-TreeState $draft.website
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $draft.manifestHash } |
            Should -Throw '*Website path differs from approved ownership*'
        (Get-TreeState $draft.website) | Should -BeExactly $before
        Write-Fixture $image 'website image'
        $reference = Join-Path $siteDocs 'theming.md'
        Write-Fixture $reference '# unreviewed website edit'
        $before = Get-TreeState $draft.website
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $draft.manifestHash } |
            Should -Throw '*Website path differs from approved ownership*'
        (Get-TreeState $draft.website) | Should -BeExactly $before
    }
    It 'does not adopt a dirty owned reference even when its draft record has the same bytes' {
        $draft = Initialize-DirtyManifestFixture $repo $siteDocs
        $reference = Join-Path $siteDocs 'theming.md'
        Write-Fixture $reference '# locally edited website reference'
        $state = Get-Content -LiteralPath $draft.manifestPath -Raw | ConvertFrom-Json
        @($state.files | Where-Object path -eq 'docs/theming.md')[0].sha256 =
            (Get-FileHash -LiteralPath $reference).Hash.ToLowerInvariant()
        [IO.File]::WriteAllBytes($draft.manifestPath,
            (ConvertTo-SourceDocsManifestByteArray $state @('docs/screenshots/gallery/button.png')))
        $hash = (Get-FileHash -LiteralPath $draft.manifestPath).Hash.ToLowerInvariant()
        $before = Get-TreeState $draft.website
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $hash } |
            Should -Throw '*Dirty owned website reference*'
        (Get-TreeState $draft.website) | Should -BeExactly $before
    }
    It 'permits only approved image updates after a hashed reference draft was already applied' {
        $draft = Initialize-DirtyManifestFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $draft.manifestHash | Out-Null
        $image = Join-Path $siteDocs 'screenshots/gallery/button.png'
        Write-Fixture $image 'owned draft image'
        $state = Get-Content -LiteralPath $draft.manifestPath -Raw | ConvertFrom-Json
        $record = @($state.files | Where-Object path -eq 'docs/screenshots/gallery/button.png')[0]
        $record.sha256 = (Get-FileHash -LiteralPath $image).Hash.ToLowerInvariant()
        $record.pngSha256 = $record.sha256
        [IO.File]::WriteAllBytes($draft.manifestPath,
            (ConvertTo-SourceDocsManifestByteArray $state @('docs/screenshots/gallery/button.png')))
        $hash = (Get-FileHash -LiteralPath $draft.manifestPath).Hash.ToLowerInvariant()
        $approval = Join-Path $caseRoot 'approved-followon.json'
        Write-Approval $approval $repo $siteDocs 'material-change'
        $beforeReference = (Get-FileHash -LiteralPath (Join-Path $siteDocs 'controls.md')).Hash
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $hash -ApprovedCaptureManifest $approval } |
            Should -Throw '*Dirty owned website reference*'
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $hash -ApprovedCaptureManifest $approval -AllowVerifiedOwnedDrafts -DryRun |
            Should -Match 'Dry run: 2 managed website operation'
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $hash -ApprovedCaptureManifest $approval -AllowVerifiedOwnedDrafts | Out-Null
        (Get-FileHash -LiteralPath (Join-Path $siteDocs 'controls.md')).Hash | Should -BeExactly $beforeReference
        [IO.File]::ReadAllText($image) | Should -BeExactly 'candidate image'
        $after = Get-Content -LiteralPath $draft.manifestPath -Raw | ConvertFrom-Json
        @($after.files | Where-Object path -eq 'docs/screenshots/gallery/button.png')[0].sha256 |
            Should -BeExactly (Get-FileHash -LiteralPath $image).Hash.ToLowerInvariant()
    }
    It 'requires both opt-in inputs and rejects the wrong acknowledged hash before writes' {
        $fixture = Initialize-VerifiedDraftFollowonFixture $repo $siteDocs $caseRoot
        $before = Get-TreeState $fixture.draft.website
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -AllowVerifiedOwnedDrafts -ApprovedCaptureManifest $fixture.approval } |
            Should -Throw '*requires an expected website manifest hash*'
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -AllowVerifiedOwnedDrafts -ExpectedWebsiteManifestHash $fixture.hash } |
            Should -Throw '*approved capture manifest*'
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -AllowVerifiedOwnedDrafts -ExpectedWebsiteManifestHash ('0' * 64) -ApprovedCaptureManifest $fixture.approval } |
            Should -Throw '*differs from expected current SHA-256*'
        (Get-TreeState $fixture.draft.website) | Should -BeExactly $before
    }
    It 'rejects current owned drift and a generated reference change in image-only draft mode' {
        $fixture = Initialize-VerifiedDraftFollowonFixture $repo $siteDocs $caseRoot
        Write-Fixture $fixture.image 'unapproved current image edit'
        $before = Get-TreeState $fixture.draft.website
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -AllowVerifiedOwnedDrafts -ExpectedWebsiteManifestHash $fixture.hash -ApprovedCaptureManifest $fixture.approval } |
            Should -Throw '*differs from approved ownership*'
        (Get-TreeState $fixture.draft.website) | Should -BeExactly $before
        Write-Fixture $fixture.image 'owned draft image'
        Write-Fixture (Join-Path $repo 'docs/theming.md') '# generated reference changed'
        $before = Get-TreeState $fixture.draft.website
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -AllowVerifiedOwnedDrafts -ExpectedWebsiteManifestHash $fixture.hash -ApprovedCaptureManifest $fixture.approval } |
            Should -Throw '*reference differs from generated source*'
        (Get-TreeState $fixture.draft.website) | Should -BeExactly $before
    }
    It 'preserves a concurrent dirty owned image edit during a draft image replacement' {
        $fixture = Initialize-VerifiedDraftFollowonFixture $repo $siteDocs $caseRoot
        $env:FLUENCE_DOCS_SYNC_TEST_TARGET_RACE_PATH = 'docs/screenshots/gallery/button.png'
        try {
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -AllowVerifiedOwnedDrafts -ExpectedWebsiteManifestHash $fixture.hash -ApprovedCaptureManifest $fixture.approval } |
                Should -Throw '*changed before managed website write*'
        } finally { Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_TARGET_RACE_PATH -ErrorAction SilentlyContinue }
        [IO.File]::ReadAllText($fixture.image) | Should -BeExactly 'concurrent user edit'
        (Get-FileHash -LiteralPath $fixture.draft.manifestPath).Hash.ToLowerInvariant() | Should -BeExactly $fixture.hash
    }
    It 'rolls back an approved draft image when the ownership manifest changes before promotion' {
        $fixture = Initialize-VerifiedDraftFollowonFixture $repo $siteDocs $caseRoot
        $env:FLUENCE_DOCS_SYNC_TEST_MANIFEST_RACE_BEFORE_WRITE = '1'
        try {
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -AllowVerifiedOwnedDrafts -ExpectedWebsiteManifestHash $fixture.hash -ApprovedCaptureManifest $fixture.approval } |
                Should -Throw '*changed before managed website write*'
        } finally { Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_MANIFEST_RACE_BEFORE_WRITE -ErrorAction SilentlyContinue }
        [IO.File]::ReadAllText($fixture.image) | Should -BeExactly 'owned draft image'
        [IO.File]::ReadAllText($fixture.draft.manifestPath) | Should -Match 'concurrent manifest edit'
    }
    It 'restores text writes when the acknowledged draft manifest changes before promotion' {
        $draft = Initialize-DirtyManifestFixture $repo $siteDocs
        $oldText = Get-Content -LiteralPath (Join-Path $siteDocs 'controls.md') -Raw
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_MANIFEST_RACE_AFTER = '1'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $draft.manifestHash } |
                Should -Throw '*website paths were restored*'
        } finally { Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_MANIFEST_RACE_AFTER -ErrorAction SilentlyContinue }
        (Get-Content -LiteralPath (Join-Path $siteDocs 'controls.md') -Raw) | Should -BeExactly $oldText
        (Get-FileHash -LiteralPath (Join-Path $siteDocs 'screenshots/gallery/button.png')).Hash.ToLowerInvariant() |
            Should -BeExactly $draft.imageHash
        (Get-FileHash -LiteralPath $draft.manifestPath).Hash.ToLowerInvariant() |
            Should -Not -BeExactly $draft.manifestHash
    }
    It 'preserves a concurrent owned text edit made after preflight' {
        $draft = Initialize-DirtyManifestFixture $repo $siteDocs
        $beforeManifest = (Get-FileHash -LiteralPath $draft.manifestPath).Hash.ToLowerInvariant()
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_TARGET_RACE_PATH = 'docs/controls.md'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $draft.manifestHash } |
                Should -Throw '*changed before managed website write*'
        } finally { Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_TARGET_RACE_PATH -ErrorAction SilentlyContinue }
        (Get-Content -LiteralPath (Join-Path $siteDocs 'controls.md') -Raw) | Should -BeExactly 'concurrent user edit'
        (Get-FileHash -LiteralPath $draft.manifestPath).Hash.ToLowerInvariant() | Should -BeExactly $beforeManifest
        (Get-FileHash -LiteralPath (Join-Path $siteDocs 'screenshots/gallery/button.png')).Hash.ToLowerInvariant() |
            Should -BeExactly $draft.imageHash
    }
    It 'rolls back prior text but preserves a concurrent manifest edit at promotion' {
        $draft = Initialize-DirtyManifestFixture $repo $siteDocs
        $oldText = Get-Content -LiteralPath (Join-Path $siteDocs 'controls.md') -Raw
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_MANIFEST_RACE_BEFORE_WRITE = '1'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $draft.manifestHash } |
                Should -Throw '*website paths were restored*'
        } finally { Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_MANIFEST_RACE_BEFORE_WRITE -ErrorAction SilentlyContinue }
        (Get-Content -LiteralPath (Join-Path $siteDocs 'controls.md') -Raw) | Should -BeExactly $oldText
        (Get-Content -LiteralPath $draft.manifestPath -Raw) | Should -Match 'concurrent manifest edit'
        (Get-FileHash -LiteralPath (Join-Path $siteDocs 'screenshots/gallery/button.png')).Hash.ToLowerInvariant() |
            Should -BeExactly $draft.imageHash
    }
    It 'reports a rollback conflict and leaves a newer user edit in place' {
        $draft = Initialize-DirtyManifestFixture $repo $siteDocs
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_TARGET_RACE_AFTER = 'docs/controls.md'
            $env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER = '1'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ExpectedWebsiteManifestHash $draft.manifestHash } |
                Should -Throw '*Restoration failed*Recovery snapshot:*'
        } finally {
            Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_TARGET_RACE_AFTER -ErrorAction SilentlyContinue
            Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER -ErrorAction SilentlyContinue
        }
        (Get-Content -LiteralPath (Join-Path $siteDocs 'controls.md') -Raw) | Should -BeExactly 'concurrent user edit'
        (Get-FileHash -LiteralPath $draft.manifestPath).Hash.ToLowerInvariant() | Should -BeExactly $draft.manifestHash
    }
    It 'rejects wrong-kind prior gallery ownership before website writes' {
        Write-Fixture (Join-Path $siteDocs 'screenshots/gallery/manifest.json') '{"entries":[]}'
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $statePath = Join-Path (Split-Path -Parent $siteDocs) 'source-docs-manifest.json'
        $state = Get-Content -Raw $statePath | ConvertFrom-Json
        (@($state.files | Where-Object path -eq 'docs/screenshots/gallery/manifest.json')[0]).kind = 'reference'
        Write-Fixture $statePath ($state | ConvertTo-Json -Depth 20)
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw '*kind does not match path*'
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'restores website bytes after an injected mid-apply failure and keeps a durable snapshot' {
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER = '1'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw '*website paths were restored*'
        } finally { Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER -ErrorAction SilentlyContinue }
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
        $snapshots = @(Get-ChildItem -LiteralPath (Join-Path $repo 'artifacts/docs') -Directory -Filter 'sync-recovery-*')
        $snapshots.Count | Should -Be 1
        Test-Path -LiteralPath (Join-Path $snapshots[0].FullName 'ledger.json') | Should -BeTrue
    }
    It 'reports a durable snapshot when restoration itself fails' {
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER = '1'
            $env:FLUENCE_DOCS_SYNC_TEST_RESTORE_FAIL_PATH = 'docs/api/index.md'
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs } | Should -Throw '*Restoration failed*Recovery snapshot:*'
        } finally {
            Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER -ErrorAction SilentlyContinue
            Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_RESTORE_FAIL_PATH -ErrorAction SilentlyContinue
        }
        $snapshots = @(Get-ChildItem -LiteralPath (Join-Path $repo 'artifacts/docs') -Directory -Filter 'sync-recovery-*')
        $snapshots.Count | Should -Be 1
        Test-Path -LiteralPath (Join-Path $snapshots[0].FullName 'ledger.json') | Should -BeTrue
    }
    It 'retains website capture bytes without approval even if a candidate exists' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        [IO.File]::ReadAllText((Join-Path $siteDocs 'screenshots/gallery/button.png')) | Should -BeExactly 'website image'
        $state = Get-Content -Raw (Join-Path (Split-Path -Parent $siteDocs) 'source-docs-manifest.json') | ConvertFrom-Json
        $capture = @($state.files | Where-Object kind -eq 'capture')[0]
        $capture.checkedVisualInputSha256 | Should -BeNullOrEmpty
        $capture.decision | Should -Be 'legacy-unverified'
    }
    It 'applies a reviewed image and merges only the selected gallery entry' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $approval = Join-Path $caseRoot 'approval.json'
        $patch = [pscustomobject]@{ route='button'; kind='route'; theme='light'; state='default'; file='button.png'; title='new' }
        Write-Approval $approval $repo $siteDocs 'material-change' @($patch)
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approval
        [IO.File]::ReadAllText((Join-Path $siteDocs 'screenshots/gallery/button.png')) | Should -BeExactly 'candidate image'
        $gallery = Get-Content -Raw (Join-Path $siteDocs 'screenshots/gallery/manifest.json') | ConvertFrom-Json
        $gallery.captureMethod | Should -Be 'WPF RenderTargetBitmap'
        $gallery.limitation | Should -Be 'preserve'
        @($gallery.entries).Count | Should -Be 2
        $gallery.entries[0].title | Should -Be 'new'
        $gallery.entries[1].title | Should -Be 'keep'
    }
    It 'creates a reviewed mapped Gallery image and appends its receipt entry after a dry run' {
        $fixture = Initialize-NewGalleryFixture $repo $siteDocs $caseRoot
        $siteRoot = Split-Path -Parent $siteDocs
        $before = Get-TreeState $siteRoot
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $fixture.approvalPath -DryRun
        (Get-TreeState $siteRoot) | Should -BeExactly $before
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $fixture.approvalPath
        (Get-FileHash -LiteralPath (Join-Path $siteRoot $fixture.path)).Hash.ToLowerInvariant() | Should -BeExactly $fixture.sha
        $gallery = Get-Content -LiteralPath (Join-Path $siteDocs 'screenshots/gallery/manifest.json') -Raw | ConvertFrom-Json
        @($gallery.entries).Count | Should -Be 3
        @($gallery.entries | Where-Object file -eq 'data-grid-light.png').Count | Should -Be 1
        $state = Get-Content -LiteralPath (Join-Path $siteRoot 'source-docs-manifest.json') -Raw | ConvertFrom-Json
        @($state.files | Where-Object path -eq $fixture.path)[0].decision | Should -BeExactly 'new-capture'
    }
    It 'onboards a reviewed Gallery image while retaining a verified dirty reference draft' {
        $fixture = Initialize-NewGalleryFixture $repo $siteDocs $caseRoot
        $siteRoot = Split-Path -Parent $siteDocs
        & git -C $siteRoot init -q
        & git -C $siteRoot add .
        & git -C $siteRoot -c user.name=Fixture -c user.email=fixture@example.invalid commit -q -m baseline
        Write-Fixture (Join-Path $repo 'docs/controls.md') '# acknowledged reference draft'
        Write-Fixture (Join-Path $siteDocs 'controls.md') '# acknowledged reference draft'
        $manifest = Join-Path $siteRoot 'source-docs-manifest.json'
        $state = Get-Content -LiteralPath $manifest -Raw | ConvertFrom-Json
        @($state.files | Where-Object path -eq 'docs/controls.md')[0].sha256 =
            (Get-FileHash -LiteralPath (Join-Path $siteDocs 'controls.md')).Hash.ToLowerInvariant()
        [IO.File]::WriteAllBytes($manifest,
            (ConvertTo-SourceDocsManifestByteArray $state @('docs/screenshots/gallery/button.png',$fixture.path)))
        $hash = (Get-FileHash -LiteralPath $manifest).Hash.ToLowerInvariant()
        $referenceHash = (Get-FileHash -LiteralPath (Join-Path $siteDocs 'controls.md')).Hash
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -AllowVerifiedOwnedDrafts -ExpectedWebsiteManifestHash $hash -ApprovedCaptureManifest $fixture.approvalPath -DryRun |
            Should -Match 'Dry run: 3 managed website operation'
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -AllowVerifiedOwnedDrafts -ExpectedWebsiteManifestHash $hash -ApprovedCaptureManifest $fixture.approvalPath | Out-Null
        (Get-FileHash -LiteralPath (Join-Path $siteDocs 'controls.md')).Hash | Should -BeExactly $referenceHash
        (Get-FileHash -LiteralPath (Join-Path $siteRoot $fixture.path)).Hash.ToLowerInvariant() | Should -BeExactly $fixture.sha
        $gallery = Get-Content -LiteralPath (Join-Path $siteDocs 'screenshots/gallery/manifest.json') -Raw | ConvertFrom-Json
        @($gallery.entries | Where-Object file -eq 'data-grid-light.png').Count | Should -Be 1
    }
    It 'rejects a new Gallery image when its approval omits entries, including capture-only sync' {
        $fixture = Initialize-NewGalleryFixture $repo $siteDocs $caseRoot
        $approval = Get-Content -LiteralPath $fixture.approvalPath -Raw | ConvertFrom-Json
        $approval.PSObject.Properties.Remove('galleryEntries')
        $approval | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $fixture.approvalPath -Encoding utf8
        $siteRoot = Split-Path -Parent $siteDocs
        $before = Get-TreeState $siteRoot
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $fixture.approvalPath } |
            Should -Throw '*Gallery*entry*'
        (Get-TreeState $siteRoot) | Should -BeExactly $before
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -CapturesOnly -ApprovedCaptureManifest $fixture.approvalPath } |
            Should -Throw '*Gallery*'
        (Get-TreeState $siteRoot) | Should -BeExactly $before
        $approval | Add-Member NoteProperty galleryEntries @()
        $approval | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $fixture.approvalPath -Encoding utf8
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $fixture.approvalPath } |
            Should -Throw '*Gallery*entry*'
        (Get-TreeState $siteRoot) | Should -BeExactly $before
    }
    It 'rejects extra and duplicate new Gallery entries before website mutation' {
        $fixture = Initialize-NewGalleryFixture $repo $siteDocs $caseRoot
        $approval = Get-Content -LiteralPath $fixture.approvalPath -Raw | ConvertFrom-Json
        $entry = $approval.galleryEntries[0]
        $siteRoot = Split-Path -Parent $siteDocs
        $before = Get-TreeState $siteRoot
        $approval.galleryEntries = @($entry,$entry)
        $approval | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $fixture.approvalPath -Encoding utf8
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $fixture.approvalPath } | Should -Throw
        (Get-TreeState $siteRoot) | Should -BeExactly $before
        $extra = $entry.PSObject.Copy()
        $extra.file = 'selection-sample-06-light.png'
        $approval.galleryEntries = @($entry,$extra)
        $approval | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $fixture.approvalPath -Encoding utf8
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $fixture.approvalPath } | Should -Throw
        (Get-TreeState $siteRoot) | Should -BeExactly $before
    }
    It 'rejects an approved Gallery entry whose observed state differs from its receipt' {
        $fixture = Initialize-NewGalleryFixture $repo $siteDocs $caseRoot
        $approval = Get-Content -LiteralPath $fixture.approvalPath -Raw | ConvertFrom-Json
        $approval.galleryEntries[0].observedState.indicatorAligned = $false
        $approval | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $fixture.approvalPath -Encoding utf8
        $siteRoot = Split-Path -Parent $siteDocs
        $before = Get-TreeState $siteRoot
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $fixture.approvalPath } |
            Should -Throw '*observed state differs from capture receipt*'
        (Get-TreeState $siteRoot) | Should -BeExactly $before
    }
    It 'rejects a new Gallery approval after receipt source or producer bytes change' {
        $fixture = Initialize-NewGalleryFixture $repo $siteDocs $caseRoot
        $siteRoot = Split-Path -Parent $siteDocs
        $before = Get-TreeState $siteRoot
        Write-Fixture (Join-Path $repo 'Fluence.Wpf.Tests/Tools/GalleryScreenshotHarness.cs') 'changed harness source'
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $fixture.approvalPath } |
            Should -Throw '*receipt*'
        (Get-TreeState $siteRoot) | Should -BeExactly $before
        Write-Fixture (Join-Path $repo 'Fluence.Wpf.Tests/Tools/GalleryScreenshotHarness.cs') 'harness source'
        Write-Fixture $fixture.binaryPath 'changed producer binary'
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $fixture.approvalPath } |
            Should -Throw '*receipt*'
        (Get-TreeState $siteRoot) | Should -BeExactly $before
        Write-Fixture $fixture.binaryPath 'producer Fluence.Wpf.Tests.dll'
        $receipt = Get-Content -LiteralPath $fixture.receiptPath -Raw | ConvertFrom-Json
        $receipt.environment.os = 'changed after approval'
        $receipt | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $fixture.receiptPath -Encoding utf8
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $fixture.approvalPath } |
            Should -Throw '*receipt hash*'
        (Get-TreeState $siteRoot) | Should -BeExactly $before
    }
    It 'removes an owned partial create and preserves all prior website paths' {
        $fixture = Initialize-NewGalleryFixture $repo $siteDocs $caseRoot
        $siteRoot = Split-Path -Parent $siteDocs
        $before = Get-TreeState $siteRoot
        try {
            $env:FLUENCE_DOCS_SYNC_TEST_PARTIAL_CREATE_PATH = $fixture.path
            { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $fixture.approvalPath } |
                Should -Throw '*partial*'
        } finally {
            Remove-Item Env:FLUENCE_DOCS_SYNC_TEST_PARTIAL_CREATE_PATH -ErrorAction SilentlyContinue
        }
        (Get-TreeState $siteRoot) | Should -BeExactly $before
        Test-Path -LiteralPath (Join-Path $siteRoot $fixture.path) | Should -BeFalse
    }
    It 'advances checked inputs without replacing a visually unchanged image' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $approval = Join-Path $caseRoot 'approval.json'
        Write-Approval $approval $repo $siteDocs 'visually-unchanged'
        $oldImage = (Get-FileHash (Join-Path $siteDocs 'screenshots/gallery/button.png')).Hash
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approval
        (Get-FileHash (Join-Path $siteDocs 'screenshots/gallery/button.png')).Hash | Should -Be $oldImage
        $state = Get-Content -Raw (Join-Path (Split-Path -Parent $siteDocs) 'source-docs-manifest.json') | ConvertFrom-Json
        $capture = @($state.files | Where-Object kind -eq 'capture')[0]
        $capture.checkedVisualInputSha256 | Should -Be ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes("Fluence.Wpf.Demo/Pages/Button.xaml`0<Button />"))).ToLowerInvariant())
        $capture.decision | Should -Be 'visually-unchanged'
    }
    It 'rejects a partial approval before touching any website bytes' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $approval = Join-Path $caseRoot 'approval.json'
        Write-Approval $approval $repo $siteDocs 'material-change'
        Remove-Item -LiteralPath (Join-Path $repo 'artifacts/docs/screenshots/gallery/button.png')
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approval } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'rejects a dirty selected gallery image before touching other paths' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $approval = Join-Path $caseRoot 'approval.json'
        Write-Approval $approval $repo $siteDocs 'material-change'
        Write-Fixture (Join-Path $siteDocs 'screenshots/gallery/button.png') 'local edit'
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approval } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
    It 'rejects an unknown gallery patch before touching any website bytes' {
        Write-CaptureFixture $repo $siteDocs
        & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs
        $approval = Join-Path $caseRoot 'approval.json'
        $patch = [pscustomobject]@{ route='unknown'; kind='route'; theme='light'; state='default'; file='unknown.png'; title='invalid' }
        Write-Approval $approval $repo $siteDocs 'material-change' @($patch)
        $before = Get-TreeState (Split-Path -Parent $siteDocs)
        { & $sync -RepositoryRoot $repo -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest $approval } | Should -Throw
        (Get-TreeState (Split-Path -Parent $siteDocs)) | Should -BeExactly $before
    }
}
