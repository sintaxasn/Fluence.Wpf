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
[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$ApiOutputRoot = 'artifacts/docs/api',
    [string]$PowerShellOutputRoot = 'artifacts/docs/powershell/reference',
    [string]$WebsiteDocsPath = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path 'Fluence.Wpf.Website/docs'),
    [string]$ApprovedCaptureManifest,
    [string]$ExpectedWebsiteManifestHash,
    [string]$CaptureOutputRoot = 'artifacts/docs',
    [switch]$InitializeManifest,
    [switch]$CapturesOnly,
    [switch]$AllowVerifiedOwnedDrafts,
    [switch]$DryRun
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SourceDocsManifest.ps1')
$hasExpectedManifestHash = $PSBoundParameters.ContainsKey('ExpectedWebsiteManifestHash')
if ($hasExpectedManifestHash) {
    if ($ExpectedWebsiteManifestHash -cnotmatch '^[0-9a-fA-F]{64}$') {
        throw 'Expected website manifest hash must be a 64-character SHA-256 hex value.'
    }
    $ExpectedWebsiteManifestHash = $ExpectedWebsiteManifestHash.ToLowerInvariant()
}
if ($AllowVerifiedOwnedDrafts -and (-not $hasExpectedManifestHash -or -not $ApprovedCaptureManifest -or
    $InitializeManifest -or $CapturesOnly)) {
    throw 'Verified owned draft image sync requires an expected website manifest hash and approved capture manifest, without initialization or capture-only mode.'
}

function Resolve-SourceRoot([string]$root, [string]$value) {
    if ([IO.Path]::IsPathRooted($value)) { return [IO.Path]::GetFullPath($value) }
    return [IO.Path]::GetFullPath((Join-Path $root $value))
}
function Read-DocsJson([string]$path) {
    return Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 40
}
function Get-DocsOpenStreamBytes([IO.FileStream]$stream) {
    $stream.Position = 0
    $memory = [IO.MemoryStream]::new()
    try { $stream.CopyTo($memory); return $memory.ToArray() }
    finally { $memory.Dispose() }
}
function Set-DocsOpenStreamBytes([IO.FileStream]$stream, [byte[]]$bytes) {
    $stream.Position = 0
    $stream.SetLength(0)
    $stream.Write($bytes,0,$bytes.Length)
    $stream.Flush($true)
}
function Get-ReferenceRecord([string]$path, [string]$hash, [string]$provenance = 'source-generated') {
    return [pscustomobject]@{ path=$path; kind='reference'; sha256=$hash; provenance=$provenance }
}
function Get-LegacyCaptureRecord($spec, [string]$hash) {
    return [pscustomobject]@{
        path=[string]$spec.Path; kind='capture'; sha256=$hash; provenance='legacy-unverified'
        theme=[string]$spec.Theme; state=[string]$spec.State; mode=[string]$spec.Mode
        inputs=@($spec.Inputs); checkedVisualInputSha256=$null; pngSha256=$hash
        decision='legacy-unverified'
    }
}
function Get-EntryKey($entry) {
    foreach ($name in 'route','kind','theme','state','file') {
        if ([string]::IsNullOrWhiteSpace([string]$entry.$name)) { throw "Gallery entry lacks $name" }
    }
    return (@($entry.route,$entry.kind,$entry.theme,$entry.state,$entry.file) -join [char]31)
}
function Get-WebsiteStatus([string]$root, [string]$path) {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { return '' }
    $inside = & git -C $root rev-parse --is-inside-work-tree 2>$null
    if ($LASTEXITCODE -ne 0 -or $inside -ne 'true') { return '' }
    $lines = @(& git -C $root status --porcelain=v1 --untracked-files=all -- $path)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect website Git status.' }
    return ($lines -join "`n")
}
function Get-CheckedVisualInputHash([string]$root, [string[]]$inputs) {
    if ($inputs.Count -eq 0) { throw 'Capture inputs are empty.' }
    $parts = [Collections.Generic.List[string]]::new()
    foreach ($inputPath in @($inputs | Sort-Object -CaseSensitive -Unique)) {
        $source = Get-DocsContainedPath $root $inputPath
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Visual input is missing: $inputPath" }
        $utf8 = [Text.UTF8Encoding]::new($false,$true)
        $content = $utf8.GetString([IO.File]::ReadAllBytes($source)).TrimStart([char]0xFEFF)
        $content = $content.Replace("`r`n","`n").Replace("`r","`n")
        if ($inputPath.EndsWith('.xaml',[StringComparison]::OrdinalIgnoreCase)) {
            try {
                $xml = [Xml.XmlDocument]::new()
                $xml.PreserveWhitespace = $true
                $xml.XmlResolver = $null
                $settings = [Xml.XmlReaderSettings]::new()
                $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
                $settings.XmlResolver = $null
                $settings.IgnoreWhitespace = $false
                $reader = [Xml.XmlReader]::Create([IO.StringReader]::new($content),$settings)
                try { $xml.Load($reader) } finally { $reader.Dispose() }
                foreach ($comment in @($xml.SelectNodes('//comment()'))) { [void]$comment.ParentNode.RemoveChild($comment) }
                $content = $xml.OuterXml.Replace("`r`n","`n").Replace("`r","`n")
        } catch {
                # An uncertain parse retains the normalized source content.
            Write-Verbose "Visual input XML could not be normalized: $inputPath"
            }
        }
        $parts.Add("$inputPath`0$content")
    }
    return Get-DocsSha256 ([Text.Encoding]::UTF8.GetBytes(($parts -join "`0")))
}
function Resolve-ApiInventoryLink([string]$apiRoot, [string]$indexPath, [string]$link) {
    $path = ($link -split '[?#]',2)[0]
    $path = [regex]::Replace($path, '\\([^\w\s])', '$1')
    $path = [Uri]::UnescapeDataString($path)
    if ([string]::IsNullOrWhiteSpace($path) -or [IO.Path]::IsPathRooted($path) -or $path -match '^[a-zA-Z][a-zA-Z0-9+.-]*:') {
        throw "Invalid generated API inventory link in $indexPath`: $link"
    }
    $resolved = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $indexPath) $path))
    $containedRoot = $apiRoot.TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($containedRoot,[StringComparison]::OrdinalIgnoreCase) -or
        -not $resolved.EndsWith('.md',[StringComparison]::OrdinalIgnoreCase)) {
        throw "Generated API inventory link leaves the API Markdown tree in $indexPath`: $link"
    }
    return [IO.Path]::GetRelativePath($apiRoot,$resolved).Replace('\','/')
}
function Assert-ApiInventory([string]$apiRoot, [object[]]$apiFiles) {
    $present = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in $apiFiles) { [void]$present.Add([IO.Path]::GetRelativePath($apiRoot,$file.FullName).Replace('\','/')) }
    $listed = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    [void]$listed.Add('index.md')
    $rootIndex = Join-Path $apiRoot 'index.md'
    $namespacePattern = '^- \[[^\]\r\n]+\]\((?<link>(?:\\.|[^)])+)\) \((?<count>[0-9]+) types\)$'
    $typePattern = '^- \[[^\]\r\n]+\]\((?<link>(?:\\.|[^)])+)\)$'
    $inNamespaces = $false
    $namespaceCount = 0
    $totalTypeCount = 0
    foreach ($line in ([IO.File]::ReadAllLines($rootIndex))) {
        if ($line -eq '## Namespaces') { $inNamespaces = $true; continue }
        if (-not $inNamespaces) { continue }
        if ($line.StartsWith('## ',[StringComparison]::Ordinal)) { break }
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $match = [regex]::Match($line,$namespacePattern)
        if (-not $match.Success) { throw "Invalid generated API namespace inventory entry: $line" }
        $namespaceIndex = Resolve-ApiInventoryLink $apiRoot $rootIndex $match.Groups['link'].Value
        if ($namespaceIndex -notmatch '^[^/]+/index\.md$' -or -not $listed.Add($namespaceIndex)) {
            throw "Invalid or duplicate generated API namespace index: $namespaceIndex"
        }
        if (-not $present.Contains($namespaceIndex)) { throw "Missing generated API namespace index: $namespaceIndex" }
        $namespacePath = Join-Path $apiRoot $namespaceIndex
        $typeCount = 0
        $inTypes = $false
        foreach ($typeLine in ([IO.File]::ReadAllLines($namespacePath))) {
            if ($typeLine -eq '## Types') { $inTypes = $true; continue }
            if (-not $inTypes) { continue }
            if ($typeLine.StartsWith('## ',[StringComparison]::Ordinal)) { break }
            if ([string]::IsNullOrWhiteSpace($typeLine)) { continue }
            $typeMatch = [regex]::Match($typeLine,$typePattern)
            if (-not $typeMatch.Success) { throw "Invalid generated API type inventory entry: $typeLine" }
            $typePage = Resolve-ApiInventoryLink $apiRoot $namespacePath $typeMatch.Groups['link'].Value
            if (-not $typePage.StartsWith(($namespaceIndex -replace 'index\.md$',''),[StringComparison]::OrdinalIgnoreCase) -or
                $typePage -eq $namespaceIndex -or -not $listed.Add($typePage)) {
                throw "Invalid or duplicate generated API type page: $typePage"
            }
            if (-not $present.Contains($typePage)) { throw "Missing generated API type page: $typePage" }
            $typeCount++
        }
        if (-not $inTypes -or $typeCount -ne [int]$match.Groups['count'].Value) {
            throw "Generated API type count differs from namespace index: $namespaceIndex"
        }
        $namespaceCount++
        $totalTypeCount += $typeCount
    }
    if (-not $inNamespaces -or $namespaceCount -eq 0) {
        throw 'Generated API namespace inventory is missing or empty.'
    }
    if ($totalTypeCount -eq 0) {
        throw 'Generated API type inventory is empty.'
    }
    foreach ($page in $present) {
        if (-not $listed.Contains($page)) { throw "Unlisted generated API page: $page" }
    }
}
function Get-CompleteReferenceSourceSet([string]$root, [string]$apiSetting, [string]$psSetting) {
    $apiRoot = Resolve-SourceRoot $root $apiSetting
    $psRoot = Resolve-SourceRoot $root $psSetting
    if (-not (Test-Path -LiteralPath (Join-Path $apiRoot 'index.md') -PathType Leaf)) { throw 'Generated API index.md is missing.' }
    $apiFiles = @(Get-ChildItem -LiteralPath $apiRoot -Recurse -File -Filter '*.md')
    if ($apiFiles.Count -eq 0) { throw 'Generated API page set is empty.' }
    Assert-ApiInventory $apiRoot $apiFiles
    $psd1 = Join-Path $root 'Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1'
    if (-not (Test-Path -LiteralPath $psd1 -PathType Leaf)) { throw 'PowerShell export manifest is missing.' }
    $names = @((Import-PowerShellDataFile -Path $psd1).FunctionsToExport)
    if ($names.Count -eq 0 -or $names.Count -ne @($names | Sort-Object -Unique).Count) { throw 'PowerShell export list is incomplete.' }
    $psFiles = @(Get-ChildItem -LiteralPath $psRoot -File -Filter '*.mdx' -ErrorAction SilentlyContinue)
    if ($psFiles.Count -ne $names.Count) { throw 'Generated PowerShell reference count differs from FunctionsToExport.' }
    $items = [Collections.Generic.List[object]]::new()
    foreach ($file in $apiFiles) {
        $relative = [IO.Path]::GetRelativePath($apiRoot,$file.FullName).Replace('\','/')
        $items.Add(@{ path="docs/api/$relative"; source=$file.FullName })
    }
    foreach ($name in $names) {
        $items.Add(@{ path="docs/powershell/reference/$name.mdx"; source=(Join-Path $psRoot "$name.mdx") })
    }
    foreach ($name in 'README.md','input-types.md','result-objects.md') {
        $items.Add(@{ path="docs/powershell/reference/$name"; source=(Join-Path $root "docs/powershell/reference/$name") })
    }
    foreach ($name in 'controls.md','theming.md','winui-parity.md') {
        $items.Add(@{ path="docs/$name"; source=(Join-Path $root "docs/$name") })
    }
    $paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($item in $items) {
        $path = Assert-DocsRelativePath ([string]$item.path)
        if (-not $paths.Add($path)) { throw "Duplicate source document: $path" }
        if (-not (Test-Path -LiteralPath $item.source -PathType Leaf)) { throw "Missing source document: $($item.source)" }
        if ((Get-Item -LiteralPath $item.source).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "Source document is a reparse point: $($item.source)"
        }
    }
    return $items.ToArray()
}

$repository = [IO.Path]::GetFullPath($RepositoryRoot)
$siteDocs = [IO.Path]::GetFullPath($WebsiteDocsPath)
if (-not (Test-Path -LiteralPath $siteDocs -PathType Container)) { throw "Website docs directory is missing: $siteDocs" }
if ((Get-Item -LiteralPath $siteDocs).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Website docs root is a reparse point.' }
$siteRoot = Split-Path -Parent $siteDocs
$statePath = Get-DocsContainedPath $siteRoot 'source-docs-manifest.json'
$mapPath = Join-Path $repository 'tools/Docs/VisualCaptureMap.psd1'
$map = if (Test-Path -LiteralPath $mapPath) { Import-PowerShellDataFile -LiteralPath $mapPath -SkipLimitCheck } else { @{ SchemaVersion=1; Captures=@() } }
if ($map.SchemaVersion -ne 1) { throw 'Unsupported visual capture map.' }
$specs = @{}
foreach ($spec in @($map.Captures)) {
    $path = Assert-DocsRelativePath ([string]$spec.Path)
    if ($path -cnotmatch '^docs/(screenshots/|powershell/images/).+\.png$' -or $specs.ContainsKey($path)) {
        throw "Invalid or duplicate capture map path: $path"
    }
    [void](Assert-DocsRelativePath ([string]$spec.SourceRelativePath))
    foreach ($declaredInput in @($spec.Inputs)) { [void](Assert-DocsRelativePath ([string]$declaredInput)) }
    $specs[$path] = $spec
}
$capturePaths = @($specs.Keys)
$prior = $null
$owned = @{}
$initialManifestHash = $null
if (Test-Path -LiteralPath $statePath -PathType Leaf) {
    $stateBytes = [IO.File]::ReadAllBytes($statePath)
    $initialManifestHash = Get-DocsSha256 $stateBytes
    $stateText = [Text.UTF8Encoding]::new($false,$true).GetString($stateBytes).TrimStart([char]0xFEFF)
    $prior = $stateText | ConvertFrom-Json -Depth 40
    Assert-SourceDocsManifest $prior $capturePaths
    if ($hasExpectedManifestHash -and $initialManifestHash -cne $ExpectedWebsiteManifestHash) {
        throw 'Website source docs manifest differs from expected current SHA-256.'
    }
    $stateStatus = Get-WebsiteStatus $siteRoot 'source-docs-manifest.json'
    if ($stateStatus -and -not $stateStatus.StartsWith('??',[StringComparison]::Ordinal) -and -not $hasExpectedManifestHash) {
        throw 'Dirty tracked website source docs manifest blocks sync.'
    }
    if ($stateStatus) {
        $canonicalBytes = ConvertTo-SourceDocsManifestByteArray $prior $capturePaths
        if ((Get-DocsSha256 $canonicalBytes) -cne (Get-DocsSha256 ([IO.File]::ReadAllBytes($statePath)))) {
            throw 'Untracked website source docs manifest is not canonical.'
        }
    }
    foreach ($record in @($prior.files)) { $owned[[string]$record.path] = $record }
    if ($hasExpectedManifestHash) {
        # An acknowledged draft is a known set of current files, not a new ownership baseline.
        foreach ($record in @($prior.files)) {
            $path = [string]$record.path
            $target = Get-DocsContainedPath $siteRoot $path
            if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { throw "Owned website path is missing: $path" }
            if ((Get-DocsSha256 ([IO.File]::ReadAllBytes($target))) -cne [string]$record.sha256) {
                throw "Website path differs from approved ownership: $path"
            }
            if (-not $AllowVerifiedOwnedDrafts -and $record.kind -eq 'reference' -and (Get-WebsiteStatus $siteRoot $path)) {
                throw "Dirty owned website reference blocks sync: $path"
            }
        }
    }
}
elseif ($hasExpectedManifestHash) { throw 'Expected website manifest hash requires an existing ownership manifest.' }
if ($InitializeManifest -and $prior) { throw 'Website manifest already exists.' }
if ($InitializeManifest -and $ApprovedCaptureManifest) { throw 'Initialization cannot approve captures.' }
if ($CapturesOnly -and $InitializeManifest) { throw 'Capture-only sync cannot initialize a manifest.' }
if ($CapturesOnly -and -not $ApprovedCaptureManifest) { throw 'Capture-only sync requires an approved capture manifest.' }
if ($CapturesOnly -and -not $prior) { throw 'Capture-only sync requires an existing website ownership manifest.' }
$records = [Collections.Generic.List[object]]::new()
$ops = [Collections.Generic.List[object]]::new()
$expected = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$sourceItems = if ($CapturesOnly) { @() } else { @(Get-CompleteReferenceSourceSet $repository $ApiOutputRoot $PowerShellOutputRoot) }
if ($AllowVerifiedOwnedDrafts) {
    # The acknowledged draft already contains every reference. This mode only advances
    # reviewed images, so a new, changed, or obsolete source reference is a hard stop.
    $sourceReferences = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($item in $sourceItems) {
        $path = Assert-DocsRelativePath ([string]$item.path)
        [void]$sourceReferences.Add($path)
        $old = $owned[$path]
        if (-not $old -or $old.kind -ne 'reference' -or
            (Get-DocsSha256 ([IO.File]::ReadAllBytes([string]$item.source))) -cne [string]$old.sha256) {
            throw "Verified owned draft reference differs from generated source: $path"
        }
    }
    foreach ($old in @($prior.files)) {
        if ($old.kind -eq 'reference' -and -not $sourceReferences.Contains([string]$old.path)) {
            throw "Verified owned draft cannot remove a reference: $($old.path)"
        }
    }
}

if ($CapturesOnly) {
    foreach ($old in @($prior.files)) {
        if ($old.kind -eq 'reference') {
            $path = [string]$old.path
            $target = Get-DocsContainedPath $siteRoot $path
            if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { throw "Owned website page is missing: $path" }
            if ((Get-DocsSha256 ([IO.File]::ReadAllBytes($target))) -cne $old.sha256) {
                throw "Website path differs from approved ownership: $path"
            }
            $records.Add($old)
        }
    }
} elseif ($InitializeManifest) {
    # Establish ownership only for complete, currently matching source references.
    foreach ($item in $sourceItems) {
        $path = [string]$item.path
        $target = Get-DocsContainedPath $siteRoot $path
        if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { continue }
        if (Get-WebsiteStatus $siteRoot $path) { throw "Dirty website path blocks initialization: $path" }
        $websiteHash = Get-DocsSha256 ([IO.File]::ReadAllBytes($target))
        $sourceHash = Get-DocsSha256 ([IO.File]::ReadAllBytes([string]$item.source))
        if ($websiteHash -cne $sourceHash) { throw "Website path needs pathwise reconciliation before initialization: $path" }
        $records.Add((Get-ReferenceRecord $path $websiteHash))
    }
} else {
    foreach ($item in $sourceItems) {
        $path = Assert-DocsRelativePath ([string]$item.path)
        if (-not $expected.Add($path)) { throw "Duplicate source document: $path" }
        $source = [string]$item.source
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing source document: $source" }
        if ((Get-Item -LiteralPath $source).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Source reparse point: $source" }
        $bytes = [IO.File]::ReadAllBytes($source)
        $hash = Get-DocsSha256 $bytes
        $target = Get-DocsContainedPath $siteRoot $path
        $old = $owned[$path]
        $exists = Test-Path -LiteralPath $target -PathType Leaf
        if ($exists) {
            $current = Get-DocsSha256 ([IO.File]::ReadAllBytes($target))
            if (($old -and $current -cne $old.sha256) -or (-not $old -and $current -cne $hash)) {
                throw "Website path differs from approved ownership: $path"
            }
        } elseif ($old) { throw "Owned website page is missing: $path" }
        if (-not $exists -or $current -cne $hash) {
            $ops.Add(@{ path=$path; action='write'; bytes=$bytes; expectedSha256=$(if ($exists) { $current } else { $null }) })
        }
        if ($old -and $old.sha256 -ceq $hash) { $records.Add($old) }
        else { $records.Add((Get-ReferenceRecord $path $hash)) }
    }
}

foreach ($path in $capturePaths) {
    $target = Get-DocsContainedPath $siteRoot $path
    $old = $owned[$path]
    if ($old) {
        if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { throw "Owned capture is missing: $path" }
        if ((Get-DocsSha256 ([IO.File]::ReadAllBytes($target))) -cne $old.pngSha256) { throw "Owned capture changed: $path" }
        $records.Add($old)
    } elseif (Test-Path -LiteralPath $target -PathType Leaf) {
        $records.Add((Get-LegacyCaptureRecord $specs[$path] (Get-DocsSha256 ([IO.File]::ReadAllBytes($target)))))
    }
}
$galleryPath = 'docs/screenshots/gallery/manifest.json'
$galleryTarget = Get-DocsContainedPath $siteRoot $galleryPath
if ($CapturesOnly) {
    foreach ($old in @($prior.files)) {
        if ($old.kind -eq 'gallery-manifest') {
            if (-not (Test-Path -LiteralPath $galleryTarget -PathType Leaf) -or
                (Get-DocsSha256 ([IO.File]::ReadAllBytes($galleryTarget))) -cne $old.sha256) {
                throw 'Owned gallery manifest changed or is missing.'
            }
            $records.Add($old)
        }
    }
} elseif (Test-Path -LiteralPath $galleryTarget -PathType Leaf) {
    $galleryHash = Get-DocsSha256 ([IO.File]::ReadAllBytes($galleryTarget))
    if ($owned[$galleryPath] -and $owned[$galleryPath].sha256 -cne $galleryHash) { throw 'Owned gallery manifest changed.' }
    if ($owned[$galleryPath]) { $records.Add($owned[$galleryPath].PSObject.Copy()) }
    else { $records.Add([pscustomobject]@{ path=$galleryPath; kind='gallery-manifest'; sha256=$galleryHash; provenance='website-gallery' }) }
}
if (-not $InitializeManifest -and -not $CapturesOnly) {
    foreach ($old in @($owned.Values)) {
        if ($old.kind -ne 'reference' -or $expected.Contains([string]$old.path)) { continue }
        $path = [string]$old.path
        $target = Get-DocsContainedPath $siteRoot $path
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            if ((Get-DocsSha256 ([IO.File]::ReadAllBytes($target))) -cne $old.sha256) { throw "Stale owned page changed: $path" }
            $ops.Add(@{ path=$path; action='remove'; expectedSha256=[string]$old.sha256 })
        }
    }
}

if ($ApprovedCaptureManifest) {
    $approval = Read-DocsJson $ApprovedCaptureManifest
    if ($approval.schemaVersion -ne 1 -or $null -eq $approval.captures) { throw 'Invalid approved capture manifest.' }
    if ($CapturesOnly -and $approval.PSObject.Properties.Name -contains 'galleryEntries' -and @($approval.galleryEntries).Count -gt 0) {
        throw 'Capture-only sync does not update the gallery manifest.'
    }
    $approved = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $newGalleryApproved = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $newGalleryReceiptEntries = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
    $captureRoot = Resolve-SourceRoot $repository $CaptureOutputRoot
    foreach ($capture in @($approval.captures)) {
        $path = Assert-DocsRelativePath ([string]$capture.path)
        if (-not $specs.ContainsKey($path) -or -not $approved.Add($path)) { throw "Unknown or duplicate approved capture: $path" }
        $spec = $specs[$path]
        $actualInputs = @($capture.inputs | Sort-Object -CaseSensitive) -join [char]31
        $mappedInputs = @($spec.Inputs | Sort-Object -CaseSensitive) -join [char]31
        if ($capture.id -cne $spec.Id -or $capture.sourceRelativePath -cne $spec.SourceRelativePath -or
            $capture.theme -cne $spec.Theme -or $capture.state -cne $spec.State -or
            $capture.mode -cne $spec.Mode -or $actualInputs -cne $mappedInputs) {
            throw "Approved capture differs from authored map: $path"
        }
        if ($capture.checkedVisualInputSha256 -cnotmatch '^[a-f0-9]{64}$' -or
            $capture.decision -cnotin @('material-change','visually-unchanged','new-capture') -or
            [string]::IsNullOrWhiteSpace([string]$capture.provenance)) {
            throw "Incomplete approved capture: $path"
        }
        if ((Get-CheckedVisualInputHash $repository ([string[]]@($spec.Inputs))) -cne $capture.checkedVisualInputSha256) {
            throw "Approved visual input hash differs from current source: $path"
        }
        $target = Get-DocsContainedPath $siteRoot $path
        $oldRecord = @($records | Where-Object path -eq $path)
        $exists = Test-Path -LiteralPath $target -PathType Leaf
        if ($capture.decision -eq 'new-capture') {
            if (-not (Test-DocsNewCapturePath $path)) { throw "New capture path is unsupported: $path" }
            if ($exists -or $oldRecord.Count -ne 0) { throw "New capture baseline unexpectedly exists: $path" }
            $candidate = Get-DocsContainedPath $captureRoot (Assert-DocsRelativePath ([string]$capture.sourceRelativePath))
            if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { throw "Capture candidate is missing: $path" }
            $bytes = [IO.File]::ReadAllBytes($candidate)
            $hash = Get-DocsSha256 $bytes
            if ($hash -cne $capture.sha256 -or $hash -cne $capture.pngSha256) { throw "Capture candidate hash mismatch: $path" }
            $ops.Add(@{ path=$path; action='create'; bytes=$bytes; expectedSha256=$null })
            if ($path.StartsWith('docs/screenshots/gallery/', [StringComparison]::Ordinal)) {
                $receiptPath = Get-DocsContainedPath $captureRoot 'screenshots/gallery/manifest.json'
                if (-not [IO.File]::Exists($receiptPath) -or
                    $capture.galleryReceiptSha256 -cne (Get-DocsSha256 ([IO.File]::ReadAllBytes($receiptPath)))) {
                    throw "New Gallery approval receipt hash is missing or changed: $path"
                }
                $receipt = Read-DocsJson $receiptPath
                if ($receipt.status -cne 'complete' -or $receipt.partial -ne $false -or $null -ne $receipt.failure) {
                    throw "New Gallery approval receipt is incomplete: $path"
                }
                Assert-DocsGalleryReceiptProvenance -Receipt $receipt -RepositoryRoot $repository -MapPath $mapPath
                $entryMatches = @($receipt.entries | Where-Object file -ceq ([IO.Path]::GetFileName($path)))
                if ($entryMatches.Count -ne 1 -or $entryMatches[0].theme -cne $capture.theme -or
                    $entryMatches[0].state -cne $capture.state -or $entryMatches[0].outputSha256 -cne $hash -or
                    $entryMatches[0].pixelWidth -le 0 -or $entryMatches[0].pixelHeight -le 0 -or
                    -not $entryMatches[0].observedState.contentLoaded -or
                    -not $entryMatches[0].observedState.indicatorAligned) {
                    throw "New Gallery approval lacks one coherent receipt entry: $path"
                }
                [void]$newGalleryApproved.Add($path)
                $newGalleryReceiptEntries[$path] = $entryMatches[0]
            }
        } else {
            if (-not $exists) { throw "Approved capture baseline is missing: $path" }
            if ($oldRecord.Count -ne 1) { throw "Approved capture state is missing: $path" }
            $hash = Get-DocsSha256 ([IO.File]::ReadAllBytes($target))
        }
        if ($capture.decision -eq 'material-change') {
            $sourceRel = Assert-DocsRelativePath ([string]$capture.sourceRelativePath)
            $candidate = Get-DocsContainedPath $captureRoot $sourceRel
            if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { throw "Capture candidate is missing: $path" }
            $bytes = [IO.File]::ReadAllBytes($candidate)
            $hash = Get-DocsSha256 $bytes
            if ($hash -cne $capture.sha256 -or $hash -cne $capture.pngSha256) { throw "Capture candidate hash mismatch: $path" }
            if ($hash -cne (Get-DocsSha256 ([IO.File]::ReadAllBytes($target)))) {
                $ops.Add(@{ path=$path; action='write'; bytes=$bytes; expectedSha256=[string]$oldRecord[0].sha256 })
            }
        } elseif ($capture.decision -eq 'visually-unchanged' -and ($hash -cne $capture.sha256 -or $hash -cne $capture.pngSha256)) {
            throw "Unchanged decision does not match published image: $path"
        }
        if ($oldRecord.Count -eq 1) { [void]$records.Remove($oldRecord[0]) }
        $newRecord = [pscustomobject]@{
            path=$path; kind='capture'; sha256=$hash; provenance=[string]$capture.provenance
            theme=[string]$capture.theme; state=[string]$capture.state; mode=[string]$capture.mode
            inputs=@($capture.inputs); checkedVisualInputSha256=[string]$capture.checkedVisualInputSha256
            pngSha256=$hash; decision=[string]$capture.decision
        }
        if ($capture.PSObject.Properties.Name -contains 'decodedPixelSha256' -and $null -ne $capture.decodedPixelSha256) {
            $newRecord | Add-Member NoteProperty decodedPixelSha256 ([string]$capture.decodedPixelSha256)
        }
        if ($newGalleryApproved.Contains($path)) {
            $newRecord | Add-Member NoteProperty galleryReceiptSha256 ([string]$capture.galleryReceiptSha256)
        }
        $records.Add($newRecord)
    }
    if ($newGalleryApproved.Count -gt 0) {
        if ($CapturesOnly) { throw 'New Gallery images require ordinary sync with Gallery entries.' }
        if ($approval.PSObject.Properties.Name -notcontains 'galleryEntries' -or @($approval.galleryEntries).Count -eq 0) {
            throw 'New Gallery approval is missing its receipt entry.'
        }
        $newEntries = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($entry in @($approval.galleryEntries)) {
            $entryPath = "docs/screenshots/gallery/$([string]$entry.file)"
            if ($newGalleryApproved.Contains($entryPath)) {
                if (-not $newEntries.Add($entryPath)) { throw "Duplicate new Gallery entry: $entryPath" }
                $receiptEntry = $newGalleryReceiptEntries[$entryPath]
                foreach ($name in 'route','kind','theme','state','file','title','outputSha256','pixelWidth','pixelHeight') {
                    if ([string]$entry.$name -cne [string]$receiptEntry.$name) {
                        throw "New Gallery entry differs from capture receipt: $entryPath ($name)"
                    }
                }
                $observedNames = @($receiptEntry.observedState.PSObject.Properties.Name)
                if (@($entry.observedState.PSObject.Properties.Name).Count -ne $observedNames.Count) {
                    throw "New Gallery observed state differs from capture receipt: $entryPath"
                }
                foreach ($name in $observedNames) {
                    if ([string]$entry.observedState.$name -cne [string]$receiptEntry.observedState.$name) {
                        throw "New Gallery observed state differs from capture receipt: $entryPath ($name)"
                    }
                }
            } elseif (-not $approved.Contains($entryPath)) {
                throw "Gallery entry lacks approved image: $entryPath"
            }
        }
        foreach ($path in $newGalleryApproved) {
            if (-not $newEntries.Contains($path)) { throw "New Gallery approval is missing its receipt entry: $path" }
        }
    }
    if ($approval.PSObject.Properties.Name -contains 'galleryEntries' -and @($approval.galleryEntries).Count -gt 0) {
        if (-not (Test-Path -LiteralPath $galleryTarget -PathType Leaf)) { throw 'Gallery manifest is missing.' }
        $gallery = Read-DocsJson $galleryTarget
        $patches = @{}
        foreach ($patch in @($approval.galleryEntries)) {
            $key = Get-EntryKey $patch
            if ($patches.ContainsKey($key)) { throw 'Duplicate gallery patch identity.' }
            $patches[$key] = $patch
        }
        $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $existingFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $merged = [Collections.Generic.List[object]]::new()
        foreach ($entry in @($gallery.entries)) {
            $key = Get-EntryKey $entry
            if (-not $existingFiles.Add([string]$entry.file)) { throw 'Gallery manifest has duplicate file names.' }
            if ($patches.ContainsKey($key)) {
                if (-not $approved.Contains("docs/screenshots/gallery/$($entry.file)")) { throw 'Gallery patch lacks approved image.' }
                $merged.Add($patches[$key]); [void]$seen.Add($key)
            } else { $merged.Add($entry) }
        }
        foreach ($patch in @($approval.galleryEntries | Sort-Object file -CaseSensitive)) {
            $key = Get-EntryKey $patch
            if ($seen.Contains($key)) { continue }
            $path = "docs/screenshots/gallery/$($patch.file)"
            if (-not $newGalleryApproved.Contains($path) -or -not $existingFiles.Add([string]$patch.file)) {
                throw 'Gallery patch contains unknown or duplicate identity.'
            }
            if ($patch.outputSha256 -cne @($approval.captures | Where-Object path -eq $path)[0].pngSha256 -or
                $patch.pixelWidth -le 0 -or $patch.pixelHeight -le 0 -or
                -not $patch.observedState.contentLoaded -or -not $patch.observedState.indicatorAligned) {
                throw 'New Gallery entry lacks matching capture provenance.'
            }
            $merged.Add($patch)
            [void]$seen.Add($key)
        }
        if ($seen.Count -ne $patches.Count) { throw 'Gallery patch contains unknown identity.' }
        $gallery.entries = @($merged)
        $json = ($gallery | ConvertTo-Json -Depth 40).Replace("`r`n","`n").TrimEnd("`n") + "`n"
        $bytes = [byte[]](@(0xEF,0xBB,0xBF) + [Text.Encoding]::UTF8.GetBytes($json))
        $hash = Get-DocsSha256 $bytes
        if ($hash -cne $galleryHash) {
            $ops.Add(@{ path=$galleryPath; action='write'; bytes=$bytes; expectedSha256=$galleryHash })
        }
        (@($records | Where-Object path -eq $galleryPath)[0]).sha256 = $hash
    }
}

$manifestBytes = ConvertTo-SourceDocsManifestByteArray ([pscustomobject]@{ schemaVersion=1; files=@($records) }) $capturePaths
if (-not (Test-Path -LiteralPath $statePath -PathType Leaf) -or
    (Get-DocsSha256 $manifestBytes) -cne (Get-DocsSha256 ([IO.File]::ReadAllBytes($statePath)))) {
    $ops.Add(@{ path='source-docs-manifest.json'; action='write'; bytes=$manifestBytes; expectedSha256=$initialManifestHash })
}
foreach ($op in $ops) {
    if ($AllowVerifiedOwnedDrafts -and ($op.action -eq 'remove' -or $expected.Contains([string]$op.path) -or
        ($owned[[string]$op.path] -and $owned[[string]$op.path].kind -eq 'reference'))) {
        throw "Verified owned draft image sync cannot change a reference or remove a path: $($op.path)"
    }
    $target = Get-DocsContainedPath $siteRoot ([string]$op.path)
    $status = Get-WebsiteStatus $siteRoot ([string]$op.path)
    if ($op.path -eq 'source-docs-manifest.json' -and $status -and -not $hasExpectedManifestHash -and
        -not $status.StartsWith('??',[StringComparison]::Ordinal)) {
        throw 'Dirty tracked website source docs manifest blocks sync.'
    }
    $oldOperationRecord = $owned[[string]$op.path]
    $verifiedDirtyImageWrite = $AllowVerifiedOwnedDrafts -and $oldOperationRecord -and
        $oldOperationRecord.kind -cin @('capture','gallery-manifest') -and $op.action -eq 'write' -and
        $op.expectedSha256 -ceq [string]$oldOperationRecord.sha256
    if ($op.path -ne 'source-docs-manifest.json' -and $status -and -not $verifiedDirtyImageWrite) {
        throw "Dirty website path blocks sync: $($op.path)"
    }
}
if ($hasExpectedManifestHash -and
    (Get-DocsSha256 ([IO.File]::ReadAllBytes($statePath))) -cne $ExpectedWebsiteManifestHash) {
    throw 'Website source docs manifest changed after preflight.'
}
if ($DryRun) { Write-Output "Dry run: $($ops.Count) managed website operation(s)."; return }
if ($ops.Count -eq 0) { Write-Output 'Synced 0 managed website operation(s).'; return }

# Keep a complete recovery set outside the website before its first write.
$recoveryRoot = Get-DocsContainedPath $repository 'artifacts/docs'
$recoveryPath = Get-DocsContainedPath $recoveryRoot ("sync-recovery-" + [guid]::NewGuid().ToString('N'))
$comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
if ($recoveryPath.StartsWith($siteRoot + [IO.Path]::DirectorySeparatorChar,$comparison) -or
    -not $recoveryPath.StartsWith($recoveryRoot + [IO.Path]::DirectorySeparatorChar,$comparison)) {
    throw 'Recovery snapshot would be inside the website or outside artifacts/docs.'
}
[IO.Directory]::CreateDirectory($recoveryPath) | Out-Null
$ledger = [Collections.Generic.List[object]]::new()
for ($index = 0; $index -lt $ops.Count; $index++) {
    $op = $ops[$index]
    $target = Get-DocsContainedPath $siteRoot ([string]$op.path)
    $exists = Test-Path -LiteralPath $target -PathType Leaf
    $blobName = if ($exists) { "$index.bin" } else { $null }
    $oldHash = $null
    if ($exists) {
        $oldBytes = [IO.File]::ReadAllBytes($target)
        $oldHash = Get-DocsSha256 $oldBytes
        [IO.File]::WriteAllBytes((Get-DocsContainedPath $recoveryPath $blobName),$oldBytes)
    }
    if (($exists -and $oldHash -cne $op.expectedSha256) -or
        (-not $exists -and $null -ne $op.expectedSha256)) {
        throw "Managed website path changed before recovery snapshot: $($op.path)"
    }
    $ledger.Add([pscustomobject]@{
        path=[string]$op.path; action=[string]$op.action
        existed=$exists; sha256=$oldHash; blob=$blobName
    })
}
$ledgerJson = ([ordered]@{ schemaVersion=1; websiteRoot=$siteRoot; operations=@($ledger) } |
    ConvertTo-Json -Depth 12).Replace("`r`n","`n").TrimEnd("`n") + "`n"
[IO.File]::WriteAllBytes((Get-DocsContainedPath $recoveryPath 'ledger.json'),
    [byte[]](@(0xEF,0xBB,0xBF) + [Text.Encoding]::UTF8.GetBytes($ledgerJson)))

$applied = 0
$attemptedPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
try {
    for ($index = 0; $index -lt $ops.Count; $index++) {
        $op = $ops[$index]
        $snapshot = $ledger[$index]
        if ($hasExpectedManifestHash -and
            (Get-DocsSha256 ([IO.File]::ReadAllBytes($statePath))) -cne $ExpectedWebsiteManifestHash) {
            throw 'Website source docs manifest changed before managed website write.'
        }
        $target = Get-DocsContainedPath $siteRoot ([string]$op.path)
        if ($env:FLUENCE_DOCS_SYNC_TEST_TARGET_RACE_PATH -eq $op.path) {
            [IO.File]::WriteAllText($target,'concurrent user edit')
        }
        if ($op.path -eq 'source-docs-manifest.json' -and $env:FLUENCE_DOCS_SYNC_TEST_MANIFEST_RACE_BEFORE_WRITE) {
            [IO.File]::AppendAllText($target,"`nconcurrent manifest edit")
        }
        if ($op.action -eq 'remove') {
            # Deletion needs a handle that excludes content writers but permits the delete itself.
            $stream = [IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::Delete)
            try {
                if ((Get-DocsSha256 (Get-DocsOpenStreamBytes $stream)) -cne $snapshot.sha256) {
                    throw "Managed website path changed before removal: $($op.path)"
                }
                [IO.File]::Delete($target)
                [void]$attemptedPaths.Add([string]$op.path)
            } finally { $stream.Dispose() }
        }
        elseif (-not $snapshot.existed) {
            [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
            if ($env:FLUENCE_DOCS_SYNC_TEST_CREATE_RACE_PATH -eq $op.path) {
                [IO.File]::WriteAllText($target, 'concurrent sentinel')
            }
            # Create in the target directory so the final exclusive move is on the same volume.
            # A failed write can leave only our private temporary file, never a partial published PNG.
            $temporary = Join-Path (Split-Path -Parent $target) ('.codex-create-' + [guid]::NewGuid().ToString('N') + '.tmp')
            try {
                $stream = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
                try {
                    if ($env:FLUENCE_DOCS_SYNC_TEST_PARTIAL_CREATE_PATH -eq $op.path) {
                        $stream.Write([byte[]]$op.bytes, 0, 1)
                        throw 'Injected partial capture write failure.'
                    }
                    $stream.Write([byte[]]$op.bytes, 0, $op.bytes.Length)
                } finally { $stream.Dispose() }
                if ((Get-DocsSha256 ([IO.File]::ReadAllBytes($temporary))) -cne (Get-DocsSha256 ([byte[]]$op.bytes))) {
                    throw "Private create bytes changed before publication: $($op.path)"
                }
                [IO.File]::Move($temporary,$target)
                [void]$attemptedPaths.Add([string]$op.path)
            } finally {
                if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) }
            }
        } else {
            # The comparison and write share one exclusive handle. No other writer can
            # replace a checked target between these two steps, including the manifest.
            $stream = [IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
            try {
                $beforeBytes = Get-DocsOpenStreamBytes $stream
                if ((Get-DocsSha256 $beforeBytes) -cne $snapshot.sha256) {
                    throw "Managed website path changed before managed website write: $($op.path)"
                }
                [void]$attemptedPaths.Add([string]$op.path)
                try { Set-DocsOpenStreamBytes $stream ([byte[]]$op.bytes) }
                catch {
                    # A failed in-place write is repaired while the exclusive handle is held.
                    try {
                        Set-DocsOpenStreamBytes $stream $beforeBytes
                        [void]$attemptedPaths.Remove([string]$op.path)
                    } catch { Write-Verbose "Locked repair failed for $($op.path): $_" }
                    throw
                }
            } finally { $stream.Dispose() }
        }
        $applied++
        if ($env:FLUENCE_DOCS_SYNC_TEST_TARGET_RACE_AFTER -eq $op.path) {
            [IO.File]::WriteAllText($target,'concurrent user edit')
        }
        if ($hasExpectedManifestHash -and $env:FLUENCE_DOCS_SYNC_TEST_MANIFEST_RACE_AFTER -and
            $applied -eq [int]$env:FLUENCE_DOCS_SYNC_TEST_MANIFEST_RACE_AFTER) {
            [IO.File]::AppendAllText($statePath,"`n")
        }
        if ($env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER -and
            $applied -eq [int]$env:FLUENCE_DOCS_SYNC_TEST_FAIL_AFTER) {
            throw "Injected sync apply failure after $applied operation(s)."
        }
    }
} catch {
    $failure = $_
    $restoreFailures = [Collections.Generic.List[string]]::new()
    foreach ($snapshot in $ledger) {
        try {
            if (-not $attemptedPaths.Contains([string]$snapshot.path)) { continue }
            if ($env:FLUENCE_DOCS_SYNC_TEST_RESTORE_FAIL_PATH -eq $snapshot.path) {
                throw "Injected restore failure for $($snapshot.path)."
            }
            $target = Get-DocsContainedPath $siteRoot ([string]$snapshot.path)
            $operation = @($ops | Where-Object path -eq $snapshot.path)[0]
            if ($snapshot.action -eq 'remove') {
                $oldBytes = [IO.File]::ReadAllBytes((Get-DocsContainedPath $recoveryPath ([string]$snapshot.blob)))
                if ((Get-DocsSha256 $oldBytes) -cne $snapshot.sha256) { throw 'Recovery blob hash mismatch.' }
                $stream = [IO.File]::Open($target,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
                try { $stream.Write($oldBytes,0,$oldBytes.Length); $stream.Flush($true) }
                finally { $stream.Dispose() }
            } elseif ($snapshot.existed) {
                $oldBytes = [IO.File]::ReadAllBytes((Get-DocsContainedPath $recoveryPath ([string]$snapshot.blob)))
                if ((Get-DocsSha256 $oldBytes) -cne $snapshot.sha256) { throw 'Recovery blob hash mismatch.' }
                $stream = [IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
                try {
                    if ((Get-DocsSha256 (Get-DocsOpenStreamBytes $stream)) -cne (Get-DocsSha256 ([byte[]]$operation.bytes))) {
                        throw 'Target changed after write; not overwriting concurrent data.'
                    }
                    Set-DocsOpenStreamBytes $stream $oldBytes
                } finally { $stream.Dispose() }
            } else {
                $stream = [IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::Delete)
                try {
                    if ((Get-DocsSha256 (Get-DocsOpenStreamBytes $stream)) -cne (Get-DocsSha256 ([byte[]]$operation.bytes))) {
                        throw 'Target changed after write; not overwriting concurrent data.'
                    }
                    [IO.File]::Delete($target)
                } finally { $stream.Dispose() }
            }
        } catch { $restoreFailures.Add("$($snapshot.path): $_") }
    }
    if ($restoreFailures.Count -gt 0) {
        throw "Sync failed: $failure. Restoration failed for $($restoreFailures -join '; '). Recovery snapshot: $recoveryPath"
    }
    throw "Sync failed and website paths were restored. Recovery snapshot: $recoveryPath. Cause: $failure"
}
try {
    # The resolved target was checked under artifacts/docs before creation.
    if (-not $recoveryPath.StartsWith($recoveryRoot + [IO.Path]::DirectorySeparatorChar,$comparison)) {
        throw 'Recovery cleanup target escaped artifacts/docs.'
    }
    Remove-Item -LiteralPath $recoveryPath -Recurse -Force
} catch { Write-Warning "Completed sync; retained recovery snapshot: $recoveryPath. Cleanup error: $_" }
Write-Output "Synced $($ops.Count) managed website operation(s)."
