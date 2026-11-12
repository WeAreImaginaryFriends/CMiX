# Makes icons.js of the UI kit: the icon keys of CMiX with the shape of each icon.
# The keys come from CMiX.UI/Themes/Icons.cs and CMiX.Studio.Avalonia/Themes/StudioIcons.cs.
# The shapes come from the Material.Icons package of the version in CMiX.UI.csproj.
# Run it after a change of an icon key:  powershell -File docs/ui-kit/tools/Update-Icons.ps1
# Restore the packages of the solution first, so that the package is in the NuGet folder.

$ErrorActionPreference = 'Stop'

$kit = Split-Path -Parent $PSScriptRoot
$repo = Split-Path -Parent (Split-Path -Parent $kit)

$project = [IO.File]::ReadAllText((Join-Path $repo 'CMiX.UI\CMiX.UI.csproj'))
$version = [regex]::Match($project, 'Include="Material\.Icons\.Avalonia"\s+Version="([^"]+)"').Groups[1].Value
if (-not $version) { throw 'The version of Material.Icons.Avalonia is not in CMiX.UI.csproj.' }

$packages = $env:NUGET_PACKAGES
if (-not $packages) { $packages = Join-Path $env:USERPROFILE '.nuget\packages' }
$dll = Join-Path $packages "material.icons\$version\lib\netstandard2.0\Material.Icons.dll"
if (-not (Test-Path -LiteralPath $dll)) { throw "Not found: $dll" }

$assembly = [Reflection.Assembly]::LoadFrom($dll)
$kindType = $assembly.GetType('Material.Icons.MaterialIconKind')
$provider = $assembly.GetType('Material.Icons.MaterialIconDataProvider')
$getData = $provider.GetMethod('GetData', [Type[]]@($kindType))

$sources = @(
    @{ file = 'CMiX.UI\Themes\Icons.cs'; pattern = '\{\s*"(\w+)"\s*,\s*MaterialIconKind\.(\w+)\s*\}'; source = 'CMiX.UI/Themes/Icons.cs' },
    @{ file = 'CMiX.Studio.Avalonia\Themes\StudioIcons.cs'; pattern = 'Icons\.Register\(\s*"(\w+)"\s*,\s*MaterialIconKind\.(\w+)\s*\)'; source = 'CMiX.Studio.Avalonia/Themes/StudioIcons.cs' }
)

$icons = New-Object System.Collections.ArrayList
foreach ($s in $sources) {
    $text = [IO.File]::ReadAllText((Join-Path $repo $s.file))
    foreach ($m in [regex]::Matches($text, $s.pattern)) {
        $kind = [Enum]::Parse($kindType, $m.Groups[2].Value)
        $path = $getData.Invoke($null, @($kind))
        [void]$icons.Add([ordered]@{ key = $m.Groups[1].Value; kind = $m.Groups[2].Value; source = $s.source; path = $path })
    }
}

$json = [ordered]@{ package = "Material.Icons $version"; icons = $icons } | ConvertTo-Json -Depth 4
$js = "/* Made by tools/Update-Icons.ps1. Do not edit. */`r`nwindow.CMIX_ICONS = " + $json + ";`r`n"
[IO.File]::WriteAllText((Join-Path $kit 'icons.js'), $js, (New-Object System.Text.UTF8Encoding($false)))
Write-Output ("Icons: " + $icons.Count)
