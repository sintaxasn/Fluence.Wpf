param(
    [ValidateSet('fluence-window', 'fluence-nav-mvvm', 'fluence-settings')]
    [string[]]$Template = @('fluence-window', 'fluence-nav-mvvm', 'fluence-settings'),
    [switch]$KeepArtifacts,
    [string]$ReportPath,
    [string]$InspectGeneratedPath
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$version = '0.9.4-starters-dev.gb3879f16.r2'
$selectedTemplates = @($Template)
$names = @{
    'fluence-window' = 'window'
    'fluence-nav-mvvm' = 'navigation-mvvm'
    'fluence-settings' = 'settings-shell'
}
$identities = @{
    'fluence-window' = 'Fluence.Wpf.Templates.Window.CSharp'
    'fluence-nav-mvvm' = 'Fluence.Wpf.Templates.NavigationMvvm.CSharp'
    'fluence-settings' = 'Fluence.Wpf.Templates.SettingsShell.CSharp'
}
$tempRoot = (Resolve-Path $env:TEMP).Path
$run = Join-Path $tempRoot ('fluence-consumer-template-check-' + [guid]::NewGuid().ToString('N'))
$feed = Join-Path $run 'feed'
$packages = Join-Path $run 'packages'
$generated = Join-Path $run 'generated'
$hive = Join-Path $run 'hive'
$logs = Join-Path $run 'logs'
$nugetConfig = Join-Path $run 'NuGet.Config'
$result = [ordered]@{
    status = 'FAIL'
    exitCode = 1
    version = $version
    templates = @($selectedTemplates)
    run = $run
    feed = $feed
    packages = $packages
    generated = $generated
    hive = $hive
    logs = $logs
    nugetConfig = $nugetConfig
    packageFiles = @()
    checkedTemplates = @()
    commandResults = @()
    error = $null
    retained = $true
}
$installed = $false
$success = $false

function Invoke-Dotnet {
    param([string]$Name, [string[]]$Arguments)

    $stdout = Join-Path $logs ($Name + '.stdout.log')
    $stderr = Join-Path $logs ($Name + '.stderr.log')
    $quoted = foreach ($argument in $Arguments) {
        '"' + $argument.Replace('"', '\"') + '"'
    }
    $process = Start-Process -FilePath 'dotnet' -ArgumentList ($quoted -join ' ') -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $process.WaitForExit()
    $result.commandResults += [ordered]@{
        name = $Name
        exitCode = $process.ExitCode
        stdout = $stdout
        stderr = $stderr
    }
    if ($process.ExitCode -ne 0) {
        throw "$Name failed with exit code $($process.ExitCode). See $stdout and $stderr"
    }
    return (Get-Content -Raw $stdout)
}

function Assert-TextContains {
    param([string]$Text, [string]$Pattern, [string]$Description)

    if ($Text -notmatch $Pattern) { throw "Structural check failed: $Description" }
}

function Assert-GeneratedIgnore {
    param([string]$Output)

    $ignorePath = Join-Path $Output '.gitignore'
    if (-not (Test-Path -LiteralPath $ignorePath -PathType Leaf)) {
        throw "Generated .gitignore is missing: $Output"
    }
    $lines = @(Get-Content -LiteralPath $ignorePath | ForEach-Object { $_.Trim() })
    foreach ($entry in @('/.nuget-packages/', '/bin/', '/obj/', '/NuGet.Config')) {
        if ($lines -cnotcontains $entry) { throw "Generated .gitignore is missing $entry in $Output" }
    }
}

function Assert-NoStarterBrand {
    param([string]$Output)

    $files = @(Get-ChildItem -LiteralPath $Output -Recurse -File)
    $icons = @($files | Where-Object { $_.Extension -ieq '.ico' })
    if ($icons.Count -gt 0) { throw "Bundled .ico is not allowed in a starter: $($icons[0].FullName)" }
    foreach ($file in $files) {
        if ($file.Extension -ieq '.csproj') {
            [xml]$project = Get-Content -Raw -LiteralPath $file.FullName
            if (@($project.SelectNodes('//*[local-name()="ApplicationIcon"]')).Count -gt 0) {
                throw "ApplicationIcon is not allowed in a starter: $($file.FullName)"
            }
        } elseif ($file.Extension -ieq '.xaml') {
            [xml]$xaml = Get-Content -Raw -LiteralPath $file.FullName
            $windows = @($xaml.SelectNodes('//*[local-name()="Window" or local-name()="FluenceWindow"]'))
            foreach ($window in $windows) {
                if (@($window.Attributes | Where-Object { $_.LocalName -eq 'Icon' }).Count -gt 0) {
                    throw "Window Icon attribute is not allowed in a starter: $($file.FullName)"
                }
            }
            if (@($xaml.SelectNodes('//*[local-name()="Window.Icon" or local-name()="FluenceWindow.Icon"]')).Count -gt 0) {
                throw "Window Icon property is not allowed in a starter: $($file.FullName)"
            }
        } elseif ($file.Extension -ieq '.cs') {
            if ((Get-Content -Raw -LiteralPath $file.FullName) -match 'FluenceWindow\.DefaultIcon') {
                throw "Fluence default icon assignment is not allowed in a starter: $($file.FullName)"
            }
        }
    }
}

function Assert-WorkflowContract {
    $agentGuide = Get-Content -Raw -LiteralPath (Join-Path $repo 'AGENTS.md')
    $skill = Get-Content -Raw -LiteralPath (Join-Path $repo 'skills/fluence-consumer-starter/SKILL.md')
    $readme = Get-Content -Raw -LiteralPath (Join-Path $repo 'templates/README.md')
    Assert-TextContains $agentGuide 'skills/fluence-consumer-starter/SKILL\.md' 'root AGENTS.md skill route'
    Assert-TextContains $skill 'dotnet new install' 'skill installation guidance'
    Assert-TextContains $skill 'Restore and build' 'skill restore/build guidance'
    Assert-TextContains $readme 'Check-ConsumerTemplates\.ps1' 'templates README checker'
    Assert-TextContains $readme '--debug:custom-hive' 'templates README isolated hive'
    Assert-TextContains $readme 'dotnet new install' 'templates README installation'
    Assert-TextContains $readme 'dotnet new list fluence' 'templates README discovery'
    Assert-TextContains $readme 'dotnet restore' 'templates README restore'
    Assert-TextContains $readme 'dotnet build' 'templates README build'
    Assert-TextContains $readme 'dotnet run' 'templates README run'
    Assert-TextContains $readme '\.gitignore' 'templates README cache ignore guidance'
    Assert-TextContains $skill '\.nuget-packages/' 'skill cache ignore guidance'
    foreach ($shortName in $names.Keys) {
        Assert-TextContains $skill ([regex]::Escape($shortName)) "skill route $shortName"
        Assert-TextContains $readme ([regex]::Escape($shortName)) "templates README route $shortName"
    }
}

function Assert-GeneratedStructure {
    param([string]$Output, [string]$ShortName)

    $projectPath = Join-Path $Output 'Contoso.Sample.csproj'
    [xml]$project = Get-Content -Raw -LiteralPath $projectPath
    $properties = $project.Project.PropertyGroup
    if ($properties.TargetFramework -ne 'net10.0-windows10.0.26100.0' -or $properties.UseWPF -ne 'true' -or $properties.OutputType -ne 'WinExe') {
        throw "Generated project framework/WPF/output contract failed: $projectPath"
    }
    $references = @($project.Project.ItemGroup.PackageReference)
    $expectedCount = if ($ShortName -eq 'fluence-nav-mvvm') { 2 } else { 1 }
    if ($references.Count -ne $expectedCount) { throw "Unexpected generated PackageReference set: $projectPath" }
    if ($ShortName -eq 'fluence-nav-mvvm') {
        $toolkit = @($references | Where-Object { $_.Include -eq 'CommunityToolkit.Mvvm' -and $_.Version -eq '8.4.2' })
        if ($toolkit.Count -ne 1) { throw "CommunityToolkit.Mvvm 8.4.2 reference missing: $projectPath" }
    }
    $appXaml = Get-Content -Raw -LiteralPath (Join-Path $Output 'App.xaml')
    $startup = Get-Content -Raw -LiteralPath (Join-Path $Output 'App.xaml.cs')
    Assert-TextContains $startup 'ApplicationThemeManager\.Apply\(ApplicationTheme\.Auto\)' "theme startup for $ShortName"
    Assert-TextContains $startup 'ApplicationAccentColorManager\.ApplySystemAccent\(\)' "accent startup for $ShortName"
    if ($appXaml -match 'Generic\.xaml') { throw "App.xaml duplicates Fluence Generic.xaml: $Output" }
    Assert-GeneratedIgnore $Output
    $generatedReadme = Get-Content -Raw -LiteralPath (Join-Path $Output 'README.md')
    Assert-TextContains $generatedReadme '\.gitignore' "generated cache ignore guidance for $ShortName"
    Assert-TextContains $generatedReadme '\.nuget-packages/' "generated package cache guidance for $ShortName"

    if ($ShortName -eq 'fluence-window') {
        $window = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml')
        $code = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml.cs')
        Assert-TextContains $window 'DisplayMemberPath="Name"' 'window typed ComboBox display'
        Assert-TextContains $window 'x:Name="StatusText"' 'window visible status'
        Assert-TextContains $code 'SelectedItem as SampleOption' 'window typed selection'
    } elseif ($ShortName -eq 'fluence-nav-mvvm') {
        [xml]$window = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml')
        $titleBar = @($window.SelectNodes('//*[local-name()="FluenceWindow.TitleBar"]/*[local-name()="TitleBar"]'))
        if ($titleBar.Count -ne 1 -or $titleBar[0].GetAttribute('Title') -ne 'Contoso.Sample' -or
            $titleBar[0].GetAttribute('IsPaneToggleButtonVisible') -ne 'True' -or
            @($titleBar[0].SelectNodes('./*[local-name()="TitleBar.Icon"]//*[local-name()="Path"]')).Count -eq 0 -or
            @($titleBar[0].SelectNodes('./*[local-name()="TitleBar.Icon"]//*[local-name()="Ellipse" and @Stroke="{DynamicResource TextFillColorPrimaryBrush}"]')).Count -eq 0 -or
            $window.DocumentElement.GetAttribute('ExtendsContentIntoTitleBar') -ne 'True') {
            throw 'Navigation title bar or neutral vector icon contract failed.'
        }
        $navigation = @($window.SelectNodes('//*[local-name()="NavigationView" and @*[local-name()="Name" and .="AppNavigation"]]'))
        if ($navigation.Count -ne 1 -or $navigation[0].GetAttribute('PaneDisplayMode') -ne 'LeftCompact' -or
            $navigation[0].GetAttribute('IsPaneOpen') -ne '{Binding IsPaneOpen, Mode=TwoWay}') {
            throw 'Navigation compact pane binding contract failed.'
        }
        foreach ($route in @('home', 'activity', 'settings')) {
            $item = @($window.SelectNodes("//*[local-name()='NavigationViewItem' and @Tag='$route']"))
            if ($item.Count -ne 1 -or @($item[0].SelectNodes('.//*[local-name()="FontIcon" and @Glyph]')).Count -ne 1) {
                throw "Navigation item route/icon contract failed: $route"
            }
        }
        if (@($window.SelectNodes("//*[local-name()='NavigationView.FooterMenuItems']/*[local-name()='NavigationViewItem' and @Tag='settings']")).Count -ne 1) {
            throw 'Settings navigation item is not in FooterMenuItems.'
        }
        if (@($window.SelectNodes("//*[local-name()='NavigationView.Content']/*[local-name()='ContentControl' and @Content='{Binding CurrentPage}']")).Count -ne 1) {
            throw 'NavigationView.Content must host CurrentPage.'
        }
        foreach ($viewModel in @('HomeViewModel', 'ActivityViewModel', 'SettingsViewModel')) {
            if (@($window.SelectNodes("//*[local-name()='DataTemplate' and contains(@DataType, '$viewModel')]")).Count -ne 1) {
                throw "Navigation DataTemplate missing: $viewModel"
            }
        }
        $adapter = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml.cs')
        $shell = Get-Content -Raw -LiteralPath (Join-Path $Output 'ViewModels/ShellViewModel.cs')
        $activity = Get-Content -Raw -LiteralPath (Join-Path $Output 'ViewModels/ActivityViewModel.cs')
        Assert-TextContains $adapter 'InvokedItemContainer\.Tag' 'navigation container route adapter'
        Assert-TextContains $adapter 'NavigateCommand\.Execute' 'navigation command forwarding'
        Assert-TextContains $adapter 'TogglePaneCommand\.Execute' 'navigation title bar pane toggle forwarding'
        Assert-TextContains $adapter 'PART_MinimizeButton' 'navigation caption minimize part hookup'
        Assert-TextContains $adapter 'minimizeButton\.CommandTarget\s*=\s*this' 'navigation caption command owner target'
        Assert-TextContains $shell 'IRelayCommand<string> NavigateCommand' 'typed navigation command'
        Assert-TextContains $shell 'IRelayCommand TogglePaneCommand' 'navigation pane toggle command'
        Assert-TextContains $shell 'IsPaneOpen\s*=\s*!_settingsPage\.UseCompactNavigation' 'settings preference controls navigation pane'
        Assert-TextContains $shell '_settingsPage\.UseCompactNavigation\s*=\s*!value' 'manual pane toggles update settings preference'
        Assert-TextContains $shell 'ObservableObject CurrentPage' 'observable current page'
        Assert-TextContains $shell '_currentRoute' 'same-route state'
        foreach ($route in @('home', 'activity', 'settings')) {
            Assert-TextContains $shell ('"' + $route + '"') "navigation mapping $route"
        }
        Assert-TextContains $activity 'RecordAction' 'activity sample action'
        Assert-TextContains $activity 'ActionCount' 'activity sample counter'
        [xml]$activityView = Get-Content -Raw -LiteralPath (Join-Path $Output 'Views/ActivityView.xaml')
        $accentActions = @($activityView.SelectNodes('//*[local-name()="Button" and @Appearance="Accent" and @Command="{Binding RecordActionCommand}"]'))
        if ($accentActions.Count -ne 1 -or $accentActions[0].HasAttribute('Width') -or $accentActions[0].HasAttribute('Height')) {
            throw 'Activity action must use a naturally sized accent button.'
        }
    } elseif ($ShortName -eq 'fluence-settings') {
        [xml]$window = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml')
        $titleBar = @($window.SelectNodes('//*[local-name()="FluenceWindow.TitleBar"]/*[local-name()="TitleBar"]'))
        if ($titleBar.Count -ne 1 -or $titleBar[0].GetAttribute('Title') -ne 'Contoso.Sample settings' -or
            $titleBar[0].GetAttribute('IsPaneToggleButtonVisible') -ne 'True' -or
            $titleBar[0].GetAttribute('BackRequested') -ne 'AppTitleBar_BackRequested' -or
            @($titleBar[0].SelectNodes('./*[local-name()="TitleBar.Icon"]/*[local-name()="Image" and @Source="{StaticResource SettingsAppArtwork}"]')).Count -ne 1 -or
            $window.DocumentElement.GetAttribute('ExtendsContentIntoTitleBar') -ne 'True') {
            throw 'Settings title bar, Back, or vector artwork contract failed.'
        }
        $navigation = @($window.SelectNodes('//*[local-name()="NavigationView" and @*[local-name()="Name" and .="CategoryNavigation"]]'))
        if ($navigation.Count -ne 1 -or $navigation[0].GetAttribute('PaneDisplayMode') -ne 'LeftCompact' -or
            $navigation[0].GetAttribute('Margin') -ne '12,0,0,0') {
            throw 'Settings compact pane or 12-DIP inset contract failed.'
        }
        [xml]$artwork = Get-Content -Raw -LiteralPath (Join-Path $Output 'Resources/SettingsArtwork.xaml')
        $requiredArtwork = @('App', 'General', 'Appearance', 'Notifications', 'About', 'Workspace', 'Density', 'Review', 'Quiet', 'Reset')
        foreach ($name in $requiredArtwork) {
            $key = "Settings${name}Artwork"
            $image = @($artwork.SelectNodes("//*[local-name()='DrawingImage' and @*[local-name()='Key' and .='$key']]"))
            if ($image.Count -ne 1) { throw "Settings vector resource missing: $key" }
            $fillColors = @($image[0].SelectNodes('.//*[local-name()="GeometryDrawing" and starts-with(@Brush,"#")]') |
                ForEach-Object { $_.GetAttribute('Brush') } | Sort-Object -Unique)
            if ($fillColors.Count -lt 2 -or $image[0].OuterXml -notmatch 'DynamicResource TextFillColorPrimaryBrush') {
                throw "Settings vector must have multiple colors and a theme-aware outline: $key"
            }
        }
        Assert-TextContains $appXaml 'Resources/SettingsArtwork\.xaml' 'settings vector dictionary registration'
        foreach ($category in @('general', 'appearance', 'notifications', 'about')) {
            $item = @($window.SelectNodes("//*[local-name()='NavigationViewItem' and @Tag='$category']"))
            $key = 'Settings' + $category.Substring(0, 1).ToUpperInvariant() + $category.Substring(1) + 'Artwork'
            if ($item.Count -ne 1 -or
                @($item[0].SelectNodes(".//*[local-name()='Image' and @Source='{StaticResource $key}']")).Count -ne 1) {
                throw "Settings category/color vector contract failed: $category"
            }
        }
        if (@($window.SelectNodes("//*[local-name()='NavigationView.Content']/*[local-name()='ContentControl']")).Count -ne 1) {
            throw 'Settings NavigationView.Content must host a ContentControl.'
        }
        $adapter = Get-Content -Raw -LiteralPath (Join-Path $Output 'MainWindow.xaml.cs')
        Assert-TextContains $adapter 'InvokedItemContainer\.Tag' 'settings category adapter'
        Assert-TextContains $adapter 'Dictionary<string, UserControl>' 'cached settings pages'
        Assert-TextContains $adapter 'Stack<string> _categoryHistory' 'settings category Back history'
        Assert-TextContains $adapter '_categoryHistory\.Push\(_currentCategory\)' 'settings history push on category visit'
        Assert-TextContains $adapter 'ShowCategory\(_categoryHistory\.Pop\(\)\)' 'settings Back consumes history without re-push'
        Assert-TextContains $adapter 'AppTitleBar\.IsBackButtonVisible\s*=\s*_categoryHistory\.Count\s*>\s*0' 'settings root Back availability'
        Assert-TextContains $adapter 'CategoryNavigation\.SelectedItem\s*=\s*_categoryItems\[category\]' 'settings selected category synchronization'
        Assert-TextContains $adapter 'CategoryNavigation\.IsPaneOpen\s*=\s*!CategoryNavigation\.IsPaneOpen' 'settings compact pane toggle'
        Assert-TextContains $adapter 'SetCaptionCommandTarget\("PART_MinimizeButton"\)' 'settings caption minimize hookup'
        Assert-TextContains $adapter 'button\.CommandTarget\s*=\s*this' 'settings caption command owner target'
        [xml]$styles = Get-Content -Raw -LiteralPath (Join-Path $Output 'Resources/SettingRowStyles.xaml')
        if (@($styles.SelectNodes('//*[local-name()="Setter" and @Property="Padding" and @Value="12"]')).Count -eq 0) {
            throw 'Setting row padding must be 12 DIP.'
        }
        Assert-TextContains $styles.OuterXml 'DynamicResource' 'theme-aware setting row styles'
        foreach ($pageName in @('General', 'Appearance', 'Notifications', 'About')) {
            $pagePath = Join-Path $Output "Pages/$($pageName)Page.xaml"
            [xml]$page = Get-Content -Raw -LiteralPath $pagePath
            if (@($page.SelectNodes('//*[local-name()="SmoothScrollViewer"]')).Count -eq 0 -or
                @($page.SelectNodes('//*[local-name()="StackPanel" and @MaxWidth="1000" and @Margin="24"]')).Count -eq 0) {
                throw "Settings page scroll/width/outer spacing failed: $pagePath"
            }
            $markup = Get-Content -Raw -LiteralPath $pagePath
            Assert-TextContains $markup 'Margin="0,24,0,0"' "settings section spacing $pageName"
            Assert-TextContains $markup 'Margin="0,8,0,0"' "settings row spacing $pageName"
            $cards = @($page.SelectNodes('//*[local-name()="Border" and @Style="{StaticResource SettingRowCardStyle}"]'))
            $actions = 0
            foreach ($card in $cards) {
                $interactive = @($card.SelectNodes('.//*[local-name()="ToggleSwitch" or local-name()="ComboBox" or local-name()="Button"]'))
                if ($interactive.Count -eq 0) { continue }
                $actions++
                if (@($card.SelectNodes('.//*[local-name()="TextBlock" and @Style="{StaticResource SettingRowTitleStyle}"]')).Count -eq 0 -or
                    @($card.SelectNodes('.//*[local-name()="TextBlock" and @Style="{StaticResource SettingRowDescriptionStyle}"]')).Count -eq 0 -or
                    @($card.SelectNodes('.//*[local-name()="Image" and starts-with(@Source,"{StaticResource Settings")]')).Count -eq 0 -or
                    @($interactive | Where-Object { $_.Attributes['AutomationProperties.Name'] }).Count -ne $interactive.Count) {
                    throw "Interactive setting row lacks vector art, label, description, or named action: $pagePath"
                }
            }
            if ($actions -eq 0) { throw "Settings page has no interactive example: $pagePath" }
        }
        $general = Get-Content -Raw -LiteralPath (Join-Path $Output 'Pages/GeneralPage.xaml')
        $generalCode = Get-Content -Raw -LiteralPath (Join-Path $Output 'Pages/GeneralPage.xaml.cs')
        $model = Get-Content -Raw -LiteralPath (Join-Path $Output 'Models/ChoiceOption.cs')
        $readme = Get-Content -Raw -LiteralPath (Join-Path $Output 'README.md')
        Assert-TextContains $general 'DisplayMemberPath="Name"' 'model-backed settings ComboBox'
        Assert-TextContains $generalCode 'new ChoiceOption\(' 'typed settings choices'
        Assert-TextContains $generalCode 'DensityChoice\.ItemsSource' 'settings choices ItemsSource'
        Assert-TextContains $model 'public string Name' 'ChoiceOption name'
        if ($model -match 'ToString\s*\(') { throw 'ChoiceOption must not override ToString.' }
        Assert-TextContains $readme '(?i)reset when you restart' 'settings reset semantics'
        Assert-TextContains $readme '(?i)do not read or change Windows or OS settings' 'settings OS boundary'
    }
}

function Assert-Package {
    param([string]$Path, [string]$Id, [bool]$IsTemplate)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing packed package: $Path"
    }
    Add-Type -AssemblyName System.IO.Compression
    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entries = @($zip.Entries | ForEach-Object FullName)
        $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -eq "$Id.nuspec" } | Select-Object -First 1
        if ($null -eq $nuspecEntry) { throw "Missing .nuspec in $Path" }
        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
        try { [xml]$nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $metadata = $nuspec.package.metadata
        if ($metadata.id -ne $Id -or $metadata.version -ne $version) {
            throw "Package ID/version mismatch in ${Path}: $($metadata.id) $($metadata.version)"
        }
        if ($IsTemplate) {
            if (@($metadata.packageTypes.packageType | Where-Object { $_.name -eq 'Template' }).Count -ne 1) {
                throw "Missing Template package type in $Path"
            }
            if (@($nuspec.SelectNodes('//*[local-name()="dependency"]')).Count -ne 0) {
                throw "Template package has dependency entries: $Path"
            }
            $bad = @($entries | Where-Object { $_ -match '(^|/)(bin|obj)(/|$)|(^|/)packages\.lock\.json$|(^|/)Directory\.(Build|Packages)\.props$|(^|/)LICENSE$|(^|/)BannedSymbols\.txt$|\.ico$|^lib/|^analyzers/|^build/' })
            if ($bad.Count -gt 0) { throw "Template package has unwanted entries: $($bad -join ', ')" }
            foreach ($shortName in $selectedTemplates) {
                $content = $names[$shortName]
                if ($entries -notcontains "content/content/$content/.template.config/template.json") {
                    throw "Missing hidden metadata for $shortName in $Path"
                }
                if ($entries -notcontains "content/content/$content/.gitignore") {
                    throw "Missing hidden .gitignore for $shortName in $Path"
                }
            }
        }
    } finally {
        $zip.Dispose()
    }
}

try {
    if (-not (Test-Path -LiteralPath $run)) {
        New-Item -ItemType Directory -Path $run | Out-Null
    } else {
        throw "Run directory already exists: $run"
    }
    New-Item -ItemType Directory -Path $feed, $packages, $generated, $hive, $logs | Out-Null
    if ($InspectGeneratedPath) {
        $fixture = (Resolve-Path -LiteralPath $InspectGeneratedPath).Path
        Assert-NoStarterBrand $fixture
        $success = $true
        $result.status = 'PASS'
        $result.exitCode = 0
        return
    }
    $escapedFeed = [System.Security.SecurityElement]::Escape($feed)
    $config = '<configuration><packageSources><clear /><add key="local" value="' + $escapedFeed + '" /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>'
    [System.IO.File]::WriteAllText($nugetConfig, $config, [System.Text.UTF8Encoding]::new($false))
    $fix = (& git -C $repo rev-parse b3879f16d9e560b106313e7263871e5f949b70a0).Trim()
    if ($LASTEXITCODE -ne 0 -or $fix -ne 'b3879f16d9e560b106313e7263871e5f949b70a0') { throw 'Phase 1 fix commit is unavailable.' }
    [xml]$rootProps = Get-Content -Raw (Join-Path $repo 'Directory.Build.props')
    $rootVersion = @($rootProps.Project.PropertyGroup.VersionPrefix | Where-Object { $_ })
    if ($rootVersion.Count -ne 1 -or $rootVersion[0] -ne '0.9.4') { throw 'Root VersionPrefix changed unexpectedly during Phase 2.' }
    if ($version -ne "0.9.4-starters-dev.g$($fix.Substring(0, 8)).r2") { throw 'Candidate version does not match the approved caption-fix local stamp.' }
    if ($version -eq '0.9.4') { throw 'The pre-fix 0.9.4 package is ineligible.' }
    Assert-WorkflowContract
    foreach ($shortName in $selectedTemplates) {
        $content = Join-Path $repo ("templates/Fluence.Wpf.Templates/content/" + $names[$shortName])
        $metadataPath = Join-Path $content '.template.config/template.json'
        if (-not (Test-Path -LiteralPath $metadataPath)) {
            throw "Missing template metadata for ${shortName}: $content"
        }
        $metadata = Get-Content -Raw -LiteralPath $metadataPath | ConvertFrom-Json
        if ($metadata.shortName -ne $shortName -or $metadata.identity -ne $identities[$shortName] -or
            $metadata.sourceName -ne 'FluenceStarterApp' -or $metadata.preferNameDirectory -ne $true -or
            $metadata.tags.type -ne 'project' -or $metadata.tags.language -ne 'C#') {
            throw "Template metadata contract failed: $metadataPath"
        }
        $project = Join-Path $content 'FluenceStarterApp.csproj'
        if (-not (Test-Path -LiteralPath $project)) { throw "Missing template project for ${shortName}: $project" }
        [xml]$projectXml = Get-Content -Raw $project
        $reference = @($projectXml.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq 'Fluence.Wpf' })
        if ($reference.Count -ne 1 -or $reference[0].Version -ne $version) {
            throw "Fluence.Wpf reference in $project must be exactly $version"
        }
    }
    $templateProject = Join-Path $repo 'templates/Fluence.Wpf.Templates/Fluence.Wpf.Templates.csproj'
    if (-not (Test-Path -LiteralPath $templateProject)) { throw "Missing template package project: $templateProject" }

    $libraryProject = Join-Path $repo 'Fluence.Wpf/Fluence.Wpf.csproj'
    [void](Invoke-Dotnet 'pack-library' @('pack', $libraryProject, '-c', 'Release', '-o', $feed, "-p:PackageVersion=$version"))
    [void](Invoke-Dotnet 'pack-templates' @('pack', $templateProject, '-c', 'Release', '-o', $feed, "-p:PackageVersion=$version", '-p:RestorePackagesWithLockFile=false'))
    $libraryPackage = Join-Path $feed "Fluence.Wpf.$version.nupkg"
    $templatePackage = Join-Path $feed "Fluence.Wpf.Templates.$version.nupkg"
    Assert-Package $libraryPackage 'Fluence.Wpf' $false
    Assert-Package $templatePackage 'Fluence.Wpf.Templates' $true
    $result.packageFiles = @($libraryPackage, $templatePackage)

    [void](Invoke-Dotnet 'new-install' @('new', 'install', $templatePackage, '--debug:custom-hive', $hive))
    $installed = $true
    $listing = Invoke-Dotnet 'new-list' @('new', 'list', 'fluence', '--debug:custom-hive', $hive)
    foreach ($shortName in $selectedTemplates) {
        if ($listing -notmatch [regex]::Escape($shortName)) { throw "Template short name missing from isolated hive: $shortName" }
    }

    foreach ($shortName in $selectedTemplates) {
        $output = Join-Path $generated $names[$shortName]
        [void](Invoke-Dotnet "new-$shortName" @('new', $shortName, '-n', 'Contoso.Sample', '-o', $output, '--debug:custom-hive', $hive))
        $project = Join-Path $output 'Contoso.Sample.csproj'
        if (-not (Test-Path -LiteralPath $project)) { throw "Generated project missing: $project" }
        $files = @(Get-ChildItem -LiteralPath $output -Recurse -File)
        $bad = @($files | Where-Object { $_.FullName -match '(^|[\\/])(bin|obj)([\\/]|$)|packages\.lock\.json$|Directory\.(Build|Packages)\.props$' })
        if ($bad.Count -gt 0) { throw "Generated output contains build/config pollution: $($bad.FullName -join ', ')" }
        $source = ($files | Where-Object Extension -in @('.csproj', '.cs', '.xaml') | ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"
        if ($source.Contains('FluenceStarterApp')) { throw "Source token was not replaced in $output" }
        if ($source -match 'ProjectReference|ApplicationIcon|FluenceWindow\.DefaultIcon|Window\.Icon') { throw "Generated project contains forbidden source reference or branded icon: $output" }
        if ($source -notmatch 'Contoso\.Sample\.MainWindow' -or $source -notmatch 'namespace Contoso\.Sample') { throw "Generated XAML class or namespace was not renamed: $output" }
        Assert-NoStarterBrand $output
        Assert-GeneratedStructure $output $shortName
        [xml]$generatedXml = Get-Content -Raw $project
        $reference = @($generatedXml.Project.ItemGroup.PackageReference | Where-Object { $_.Include -eq 'Fluence.Wpf' })
        if ($reference.Count -ne 1 -or $reference[0].Version -ne $version) { throw "Generated package version mismatch: $project" }
        [void](Invoke-Dotnet "restore-$shortName" @('restore', $project, '--packages', $packages, '--configfile', $nugetConfig))
        [void](Invoke-Dotnet "build-$shortName" @('build', $project, '--no-restore', '-c', 'Release'))
        $executable = Join-Path $output 'bin/Release/net10.0-windows10.0.26100.0/Contoso.Sample.exe'
        if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw "Generated executable missing after build: $executable" }
        $result.checkedTemplates += [ordered]@{
            shortName = $shortName
            project = $project
            executable = $executable
        }
    }
    $actualNames = @($result.checkedTemplates | ForEach-Object { $_.shortName } | Sort-Object)
    $expectedNames = @($selectedTemplates | Sort-Object)
    if ($actualNames.Count -ne $expectedNames.Count -or ($actualNames -join '|') -ne ($expectedNames -join '|')) {
        throw "Generated build coverage mismatch: expected $($expectedNames -join ', '); got $($actualNames -join ', ')"
    }
    $success = $true
    $result.status = 'PASS'
    $result.exitCode = 0
} catch {
    $result.error = $_.Exception.Message
} finally {
    if ($installed) {
        try { [void](Invoke-Dotnet 'new-uninstall' @('new', 'uninstall', 'Fluence.Wpf.Templates', '--debug:custom-hive', $hive)) }
        catch { $result.error = (($result.error, "Uninstall failed: $($_.Exception.Message)" | Where-Object { $_ }) -join '; '); $success = $false; $result.status = 'FAIL'; $result.exitCode = 1 }
    }
    $expectedParent = [System.IO.Path]::GetFullPath($tempRoot).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
    $actualParent = [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($run).TrimEnd([System.IO.Path]::DirectorySeparatorChar))
    if ($success -and -not $KeepArtifacts -and $actualParent -eq $expectedParent -and (Split-Path $run -Leaf) -match '^fluence-consumer-template-check-[0-9a-f]{32}$') {
        try {
            Remove-Item -LiteralPath $run -Recurse -Force
            $result.retained = $false
        } catch {
            $result.error = "Cleanup failed: $($_.Exception.Message)"
            $success = $false
            $result.status = 'FAIL'
            $result.exitCode = 1
        }
    }
    $json = $result | ConvertTo-Json -Depth 5
    if ($ReportPath) { [System.IO.File]::WriteAllText([System.IO.Path]::GetFullPath($ReportPath), $json + "`n", [System.Text.UTF8Encoding]::new($true)) }
    Write-Output $json
    if (-not $success) { exit 1 }
}
