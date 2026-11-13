$ErrorActionPreference = 'Stop'
./scripts/fetch-ocr-models.ps1
dotnet publish apps/windows-client/ChestTracker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/windows
if ($LASTEXITCODE -ne 0) { throw 'Windows build failed' }
if (!(Test-Path artifacts/windows/ChestTracker.exe)) { throw 'Missing ChestTracker.exe' }
Copy-Item .local/ocr/tessdata artifacts/windows/tessdata -Recurse -Force
$ocrCache = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget/packages' }
New-Item -ItemType Directory -Force artifacts/windows/x64 | Out-Null
Copy-Item (Join-Path $ocrCache 'tesseract/5.2.0/x64/*.dll') artifacts/windows/x64 -Force
# Tesseract's redistributable native DLLs need the app-local Microsoft VC runtime.
foreach ($dll in @('msvcp140.dll','vcruntime140.dll','vcruntime140_1.dll')) {
  $source = Join-Path $env:WINDIR "System32/$dll"
  if (!(Test-Path $source)) { throw "Microsoft Visual C++ runtime missing on build machine: $dll" }
  Copy-Item $source artifacts/windows/$dll -Force
  Copy-Item $source artifacts/windows/x64/$dll -Force
}
Copy-Item tests/fixtures artifacts/windows/replays -Recurse -Force
Copy-Item docs/WINDOWS.md artifacts/windows/README.md -Force
Copy-Item docs/DETECTION_REFERENCE.md artifacts/windows/DETECTION_REFERENCE.md -Force
Copy-Item docs/ERSTE_SCHRITTE.md artifacts/windows/START-HIER.txt -Force
Copy-Item docs/THIRD_PARTY.md artifacts/windows/THIRD_PARTY.md -Force
New-Item -ItemType Directory -Force artifacts/windows/licenses | Out-Null
Copy-Item .local/ocr/APACHE-2.0.txt,.local/ocr/LEPTONICA-LICENSE.txt artifacts/windows/licenses -Force
$verifyInfo = [System.Diagnostics.ProcessStartInfo]::new((Resolve-Path artifacts/windows/ChestTracker.exe).Path, '--verify-ocr')
$verifyInfo.UseShellExecute = $false
$verifyInfo.RedirectStandardError = $true
$verifyInfo.RedirectStandardOutput = $true
$verify = [System.Diagnostics.Process]::Start($verifyInfo)
$verifyOutput = $verify.StandardOutput.ReadToEnd()
$verifyError = $verify.StandardError.ReadToEnd()
$verify.WaitForExit()
Write-Host "Published OCR verification exit code: $($verify.ExitCode)"
if ($verify.ExitCode -ne 0) { Write-Host $verifyOutput; Write-Host $verifyError; throw 'Published EXE could not load bundled OCR engine/models' }
Compress-Archive -Path artifacts/windows/* -DestinationPath artifacts/ChestTracker-win-x64.zip -Force
Get-FileHash artifacts/ChestTracker-win-x64.zip -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLower())  ChestTracker-win-x64.zip" } | Set-Content artifacts/SHA256SUMS.txt
