# Build Ledgerly client and server Windows installers
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$python = Join-Path $root ".venv\Scripts\python.exe"
$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if (-not (Test-Path $python)) {
    throw "Virtual environment not found. Create it and install requirements first."
}
if (-not (Test-Path $iscc)) {
    throw "Inno Setup 6 not found at $iscc"
}

Write-Host "==> Ensuring PyInstaller is installed"
& $python -m pip install -q pyinstaller

Write-Host "==> Cleaning previous build outputs"
Remove-Item -Recurse -Force dist\LedgerlyServer, dist\LedgerlyClient, build -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path dist\installers | Out-Null

Write-Host "==> Building LedgerlyServer executable"
& $python -m PyInstaller --noconfirm --clean --distpath dist --workpath build\server bundle\ledgerly-server.spec
if ($LASTEXITCODE -ne 0) { throw "Server PyInstaller build failed" }

Write-Host "==> Building LedgerlyClient executable"
& $python -m PyInstaller --noconfirm --clean --distpath dist --workpath build\client bundle\ledgerly-client.spec
if ($LASTEXITCODE -ne 0) { throw "Client PyInstaller build failed" }

Write-Host "==> Compiling server installer"
& $iscc installer\server.iss
if ($LASTEXITCODE -ne 0) { throw "Server Inno Setup compile failed" }

Write-Host "==> Compiling client installer"
& $iscc installer\client.iss
if ($LASTEXITCODE -ne 0) { throw "Client Inno Setup compile failed" }

Write-Host "==> Copying installers to installers\"
New-Item -ItemType Directory -Force -Path installers | Out-Null
Copy-Item dist\installers\LedgerlyServerSetup.exe installers\ -Force
Copy-Item dist\installers\LedgerlyClientSetup.exe installers\ -Force

Write-Host ""
Write-Host "Installers ready:"
Get-ChildItem installers\*.exe | ForEach-Object { Write-Host "  $($_.FullName)  ($([math]::Round($_.Length/1MB,1)) MB)" }
