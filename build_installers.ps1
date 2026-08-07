# Build Ledgerly Windows 10+ executables and installers:
#   LedgerlyServerSetup.exe  — server only
#   LedgerlyClientSetup.exe  — client only
#   LedgerlySetup.exe        — chooser (Server / Client / Both)
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

Write-Host "==> Compiling LedgerlyServerSetup"
& $iscc /Qp installer\server.iss
if ($LASTEXITCODE -ne 0) { throw "Server Inno Setup compile failed" }

Write-Host "==> Compiling LedgerlyClientSetup"
& $iscc /Qp installer\client.iss
if ($LASTEXITCODE -ne 0) { throw "Client Inno Setup compile failed" }

Write-Host "==> Compiling LedgerlySetup (chooser)"
& $iscc /Qp installer\ledgerly.iss
if ($LASTEXITCODE -ne 0) { throw "Chooser Inno Setup compile failed" }

Write-Host "==> Publishing installers to installers\"
Copy-Item dist\installers\LedgerlyServerSetup.exe installers\ -Force
Copy-Item dist\installers\LedgerlyClientSetup.exe installers\ -Force
Copy-Item dist\installers\LedgerlySetup.exe installers\ -Force

Write-Host ""
Write-Host "Installers ready (Windows 10+):"
Get-ChildItem installers\Ledgerly*Setup.exe | Sort-Object Name | ForEach-Object {
    Write-Host ("  {0}  ({1} MB)" -f $_.Name, [math]::Round($_.Length / 1MB, 1))
}
