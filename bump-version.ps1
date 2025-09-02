param()

$projFull = Join-Path -Path $PSScriptRoot -ChildPath 'LmSharp.csproj'
[xml]$xml = Get-Content -LiteralPath $projFull
$xml.PreserveWhitespace = $true  # important: keep existing whitespace

# Find all <Version> elements
$versionNodes = $xml.SelectNodes('/Project/PropertyGroup/Version')

if (-not $versionNodes -or $versionNodes.Count -eq 0) {
    $propertyGroups = @($xml.Project.PropertyGroup)
    if ($propertyGroups.Count -eq 0) {
        $pg = $xml.CreateElement('PropertyGroup')
        $xml.Project.AppendChild($pg) | Out-Null
    } else {
        $pg = $propertyGroups | Select-Object -First 1
    }
    $newVersion = $xml.CreateElement('Version')
    $newVersion.InnerText = '0.0.0'
    $pg.AppendChild($newVersion) | Out-Null
    $versionNodes = ,$newVersion
}

# Deduplicate if multiple <Version> tags exist
if ($versionNodes.Count -gt 1) {
    for ($i = 1; $i -lt $versionNodes.Count; $i++) {
        [void]$versionNodes[$i].ParentNode.RemoveChild($versionNodes[$i])
    }
}
$versionElement = $versionNodes[0]

# Bump patch version
$current = [string]$versionElement.InnerText
$match = [regex]::Match($current.Trim(), '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?<suffix>.*)$')
if ($match.Success) {
    $major = [int]$match.Groups['major'].Value
    $minor = [int]$match.Groups['minor'].Value
    $patch = ([int]$match.Groups['patch'].Value) + 1
    $suffix = $match.Groups['suffix'].Value
    $versionElement.InnerText = "$major.$minor.$patch$suffix"
} else {
    $versionElement.InnerText = '0.0.1'
}

# Save with pretty formatting
$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true
$settings.IndentChars = "  "   # 2 spaces (can change to "`t" for tabs)
$settings.NewLineChars = "`r`n"
$settings.NewLineHandling = "Replace"

$writer = [System.Xml.XmlWriter]::Create($projFull, $settings)
$xml.Save($writer)
$writer.Close()

Push-Location -LiteralPath $PSScriptRoot
dotnet build
$code = $LASTEXITCODE

if ($code -eq 0) {
    Write-Host "Build succeeded. Running move-package.ps1..."
    $moveScript = Join-Path -Path $PSScriptRoot -ChildPath 'move-package.ps1'
    if (Test-Path -LiteralPath $moveScript) {
        & $moveScript
        $code = $LASTEXITCODE
    } else {
        Write-Error "move-package.ps1 not found at $moveScript"
        $code = 1
    }
} else {
    Write-Host "Build failed. Skipping move-package.ps1."
}

Pop-Location
exit $code