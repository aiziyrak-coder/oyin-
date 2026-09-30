# Optional per-user URI registration. No administrator rights, no shell command evaluation of URLs.
param([string]$GamePath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Builds\LobbyV2\CraDev.exe'))
$ErrorActionPreference='Stop'
$resolved=(Resolve-Path -LiteralPath $GamePath).Path
if([IO.Path]::GetFileName($resolved) -ne 'CraDev.exe'){throw 'Expected CraDev.exe'}
$key='HKCU:\Software\Classes\newworld'
$command='"'+$resolved+'" "%1"'
New-Item -Path $key -Force | Out-Null
Set-Item -LiteralPath $key -Value 'URL:NewWorld Group'
New-ItemProperty -LiteralPath $key -Name 'URL Protocol' -Value '' -PropertyType String -Force | Out-Null
New-Item -Path "$key\shell\open\command" -Force | Out-Null
Set-Item -LiteralPath "$key\shell\open\command" -Value $command
Write-Host '[NewWorld] Group links registered for this Windows user.'
