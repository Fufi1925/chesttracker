param([string]$Destination = '.local/ocr/tessdata')
$ErrorActionPreference = 'Stop'
$revision = '87416418657359cb625c412a48b6e1d6d41c29bd'
$expected = @{
  eng = '7d4322bd2a7749724879683fc3912cb542f19906c83bcc1a52132556427170b2'
  deu = '19d219bbb6672c869d20a9636c6816a81eb9a71796cb93ebe0cb1530e2cdb22d'
}
New-Item -ItemType Directory -Force $Destination | Out-Null
foreach ($language in $expected.Keys) {
  $path = Join-Path $Destination "$language.traineddata"
  if (!(Test-Path $path)) { Invoke-WebRequest "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/$revision/$language.traineddata" -OutFile $path }
  if ((Get-FileHash $path -Algorithm SHA256).Hash.ToLower() -ne $expected[$language]) { throw "OCR checksum mismatch: $language" }
}
Write-Host 'Official OCR models downloaded and SHA256 verified.'
$licenses = Split-Path $Destination -Parent
Invoke-WebRequest 'https://www.apache.org/licenses/LICENSE-2.0.txt' -OutFile (Join-Path $licenses 'APACHE-2.0.txt')
Invoke-WebRequest 'https://raw.githubusercontent.com/DanBloomberg/leptonica/1.82.0/leptonica-license.txt' -OutFile (Join-Path $licenses 'LEPTONICA-LICENSE.txt')
