param (
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
Set-Location -Path (Resolve-Path "$($PSScriptRoot)/..")

$zipUrl = "https://script.google.com/macros/s/AKfycbyVEQ30eVxP-zVWqGXuiPfuVEZYtJp4PYWuArJZxNiSxthxX3FqYssAvUDsrVPQuHt6/exec"
$zipPath = "translations.zip"
$folder = Join-Path -Path $Version -ChildPath "Languages\English\Keyed"
$syncFile = ".localization"

if (-not (Test-Path -Path $folder -PathType Container)) {
    Write-Error "Folder $($folder) not found"
    exit 1
}

Write-Output "Checking localization for updates."
$url = $zipUrl
if (Test-Path $syncFile){
  $lastSynced = Get-Content $syncFile -Raw
  $url += "?lastSynced=$([uri]::EscapeDataString($lastSynced))"
}
$response = Invoke-WebRequest -Uri $url -UseBasicParsing

try {
    $json = $response.Content | ConvertFrom-Json
} catch {
    Write-Error "Unexpected response: $($response.Content)"
    exit 1
}

if ($json.status -eq 'OK'){
  Write-Output "Translations up to date. No changes needed."
  exit 0
}

Write-Output "Extracting localization files."
Invoke-WebRequest -Uri $json.url -OutFile $zipPath -UseBasicParsing

$contents = [string](Get-Content -Raw -Encoding Unknown -Path $zipPath).ToCharArray();
$byte = [convert]::tostring([convert]::toint32($contents[0]),16);
if ($byte -ne '4b50'){
  Write-Output "FAILED: Downloaded zip file which is not a zip. Url=$($json.url)"
  Remove-Item -Path $zipPath
  exit 1
}

$filesToDelete = Get-ChildItem -Path $folder -File -Include *.xml
if ($filesToDelete.Count -gt 0) {
    $filesToDelete | Remove-Item -Force
}

Write-Output "Unpacking zip in $(Get-Location)\$($folder)"
Expand-Archive -Path $zipPath -DestinationPath $folder -Force
Remove-Item -Path $zipPath

$json.lastUpdated | Out-File -FilePath $syncFile -Encoding utf8 -NoNewline

Write-Output "Translations downloaded."