# One-click package: build a single-file, self-contained LincoFarmTool.exe (no .NET needed).
# Usage: run  ./publish.ps1  from the repo root.
$ErrorActionPreference = "Stop"

$proj = Join-Path $PSScriptRoot "src/LincoFarmTool/LincoFarmTool.csproj"
$out  = Join-Path $PSScriptRoot "dist"

Write-Host "Publishing single-file exe (win-x64, self-contained)..." -ForegroundColor Cyan

dotnet publish $proj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=none `
  -o $out

$exe = Join-Path $out "LincoFarmTool.exe"
if (Test-Path $exe) {
    $mb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
    Write-Host ""
    Write-Host "Done. -> $exe ($mb MB)" -ForegroundColor Green
    Write-Host "Send this single exe; double-click to run, no .NET install required." -ForegroundColor Green
} else {
    Write-Error "Publish failed: $exe not found"
}
