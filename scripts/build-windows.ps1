$ErrorActionPreference = 'Stop'
dotnet publish apps/windows-client/ChestTracker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/windows
if ($LASTEXITCODE -ne 0) { throw 'Windows build failed' }
if (!(Test-Path artifacts/windows/ChestTracker.exe)) { throw 'Missing ChestTracker.exe' }
Copy-Item tests/fixtures artifacts/windows/replays -Recurse -Force
Copy-Item docs/WINDOWS.md artifacts/windows/README.md -Force
Copy-Item docs/ERSTE_SCHRITTE.md artifacts/windows/START-HIER.txt -Force
Compress-Archive -Path artifacts/windows/* -DestinationPath artifacts/ChestTracker-win-x64.zip -Force
Get-FileHash artifacts/ChestTracker-win-x64.zip -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLower())  ChestTracker-win-x64.zip" } | Set-Content artifacts/SHA256SUMS.txt
