<# 
Usage:
  .\New-ReleaseTagAtCommit.ps1 -Tag v1.0.0 [-Commit <sha>] [-Message "Release v1.0.0"] [-ShowLog 20]
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Tag,
    [string]$Commit,
    [string]$Message = "Release $Tag",
    [int]$ShowLog = 20
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-Git { param([Parameter(Mandatory=$true)][string[]]$Args)
git @Args | Write-Output
if ($LASTEXITCODE -ne 0) { throw "git $($Args -join ' ') failed with exit code $LASTEXITCODE" }
}

Invoke-Git @('--version') | Out-Null
Invoke-Git @('rev-parse','--git-dir') | Out-Null

if (-not $Commit) {
    Write-Host "Recent commits:"
    git log --oneline -n $ShowLog
    $Commit = Read-Host "Enter the commit SHA (or leave blank for HEAD)"
    if ([string]::IsNullOrWhiteSpace($Commit)) { $Commit = (git rev-parse HEAD).Trim() }
}

# validate commit
Invoke-Git @('rev-parse','--verify', "$Commit^{commit}") | Out-Null

# create and push tag
Invoke-Git @('tag','-a', $Tag, $Commit, '-m', $Message)
Invoke-Git @('push','origin', $Tag)

Write-Host "✅ Created and pushed tag '$Tag' at $Commit."