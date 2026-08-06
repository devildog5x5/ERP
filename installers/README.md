# Published Setup executables

`build_installers.ps1` copies finished Windows installers here:

- `LedgerlySetup.exe` — chooser (Both / Server / Client)
- `LedgerlyServerSetup.exe` — server only
- `LedgerlyClientSetup.exe` — client only

Rebuild on Windows 10+ with a project venv and Inno Setup 6. Checked-in `.exe` files may lag the script version until that rebuild runs.
