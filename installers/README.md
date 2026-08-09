# Published Setup executables

`build_installers.ps1` copies finished Windows installers here:

- `LedgerlySetup.exe` — chooser (Both / Server / Client)
- `LedgerlyServerSetup.exe` — server only
- `LedgerlyClientSetup.exe` — client only

Rebuild on Windows with .NET Framework 4.8 targeting pack + Inno Setup 6. Checked-in `.exe` files are not required; download published builds from GitHub Releases.
