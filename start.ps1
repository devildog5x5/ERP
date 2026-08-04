# Start Ledgerly API server + web client
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

if (-not (Test-Path ".\.venv\Scripts\python.exe")) {
    Write-Error "Virtual environment not found. Run: python -m venv .venv && .\.venv\Scripts\Activate.ps1 && pip install -r requirements.txt"
    exit 1
}

$python = (Resolve-Path ".\.venv\Scripts\python.exe").Path

Write-Host "Starting API server on http://127.0.0.1:8000 ..."
Start-Process -FilePath $python -ArgumentList "run_server.py" -WorkingDirectory $root -WindowStyle Normal

Start-Sleep -Seconds 2

Write-Host "Starting web client on http://127.0.0.1:3000 ..."
Start-Process -FilePath $python -ArgumentList "run_client.py" -WorkingDirectory $root -WindowStyle Normal

Write-Host ""
Write-Host "Ledgerly is starting:"
Write-Host "  Client: http://127.0.0.1:3000"
Write-Host "  API:    http://127.0.0.1:8000"
Write-Host "  Docs:   http://127.0.0.1:8000/docs"
