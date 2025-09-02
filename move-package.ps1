param(
    [string]$Project = (Get-ChildItem -Path . -Filter *.csproj | Select-Object -First 1).FullName,
    [string]$Output = "$env:USERPROFILE\.nuget\custom-packages",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

New-Item -ItemType Directory -Path $Output -Force | Out-Null
$arguments = @("pack", $Project, "-c", $Configuration, "-o", $Output)
$process = Start-Process -FilePath "dotnet" -ArgumentList $arguments -NoNewWindow -Wait -PassThru
exit $process.ExitCode