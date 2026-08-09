# Build Coalesce.ERP.CRM (.NET Framework 4.8) binaries and ALL three Inno Setup installers:
#   CoalesceSetup.exe / CoalesceClientSetup.exe / CoalesceServerSetup.exe
#   Each includes Client + Server payloads and opens with a loud
#   Client / Server / Both chooser (default selection differs by package name).
# Requires: .NET SDK + net48 targeting pack, Inno Setup 6 (ISCC.exe).
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) {
    throw "Inno Setup 6 not found at $iscc. Install from https://jrsoftware.org/isinfo.php"
}

$serverOut = Join-Path $root "dist\CoalesceServer"
$clientOut = Join-Path $root "dist\CoalesceClient"
$installerOut = Join-Path $root "dist\installers"
$publishDir = Join-Path $root "installers"
$names = @("CoalesceSetup.exe", "CoalesceClientSetup.exe", "CoalesceServerSetup.exe")

Write-Host "==> Cleaning previous packaging outputs"
Remove-Item -Recurse -Force $serverOut, $clientOut -ErrorAction SilentlyContinue
foreach ($n in $names) {
    Remove-Item -Force (Join-Path $installerOut $n) -ErrorAction SilentlyContinue
    Remove-Item -Force (Join-Path $publishDir $n) -ErrorAction SilentlyContinue
}
# Remove legacy Ledgerly installer names from publish folder
foreach ($n in @("LedgerlySetup.exe", "LedgerlyClientSetup.exe", "LedgerlyServerSetup.exe")) {
    Remove-Item -Force (Join-Path $publishDir $n) -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Force -Path $serverOut, $clientOut, $installerOut, $publishDir | Out-Null

Write-Host "==> Publishing Coalesce.Server (net48 / x64 Release)"
dotnet publish "src\Ledgerly.Server\Ledgerly.Server.csproj" -c Release -o $serverOut --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw "Server publish failed" }

Write-Host "==> Publishing Coalesce.Client (net48-windows / x64 Release)"
dotnet publish "src\Ledgerly.Client\Ledgerly.Client.csproj" -c Release -o $clientOut --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw "Client publish failed" }

if (-not (Test-Path (Join-Path $serverOut "Coalesce.Server.exe"))) {
    throw "Missing Coalesce.Server.exe in $serverOut"
}
if (-not (Test-Path (Join-Path $clientOut "Coalesce.Client.exe"))) {
    throw "Missing Coalesce.Client.exe in $clientOut"
}

foreach ($package in @("combined", "client", "server")) {
    Write-Host "==> Compiling $package installer"
    & $iscc "/DPackage=$package" "installer\coalesce.iss"
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compile failed for Package=$package" }
}

foreach ($n in $names) {
    $src = Join-Path $installerOut $n
    if (-not (Test-Path $src)) { throw "Installer not produced: $src" }
    Copy-Item $src (Join-Path $publishDir $n) -Force
}

Write-Host ""
Write-Host "Installers ready (Windows 7 SP1+ / .NET Framework 4.8):"
Get-ChildItem $publishDir -Filter "Coalesce*Setup.exe" | Sort-Object Name | ForEach-Object {
    Write-Host ("  {0}  ({1:N1} MB)" -f $_.FullName, ($_.Length / 1MB))
}
Write-Host "  Server payload: $serverOut"
Write-Host "  Client payload: $clientOut"
