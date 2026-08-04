# Build Ledgerly Windows 10+ executables and combined installer
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
Remove-Item -Force dist\installers\*.exe -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path dist\installers, installers | Out-Null

Write-Host "==> Building LedgerlyServer executable (Windows 10+)"
& $python -m PyInstaller --noconfirm --clean --distpath dist --workpath build\server bundle\ledgerly-server.spec
if ($LASTEXITCODE -ne 0) { throw "Server PyInstaller build failed" }

Write-Host "==> Building LedgerlyClient executable (Windows 10+)"
& $python -m PyInstaller --noconfirm --clean --distpath dist --workpath build\client bundle\ledgerly-client.spec
if ($LASTEXITCODE -ne 0) { throw "Client PyInstaller build failed" }

Write-Host "==> Compiling combined LedgerlySetup installer"
& $iscc installer\ledgerly.iss
if ($LASTEXITCODE -ne 0) { throw "Combined Inno Setup compile failed" }

Write-Host "==> Publishing installer to installers\"
Remove-Item -Force installers\LedgerlyServerSetup.exe, installers\LedgerlyClientSetup.exe -ErrorAction SilentlyContinue
Copy-Item dist\installers\LedgerlySetup.exe installers\ -Force

Write-Host ""
Write-Host "Installer ready (Windows 10+):"
Get-ChildItem installers\LedgerlySetup.exe | ForEach-Object {
    Write-Host "  $($_.FullName)  ($([math]::Round($_.Length/1MB,1)) MB)"
}
