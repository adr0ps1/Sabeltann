$repoRoot = Split-Path $PSScriptRoot -Parent
$releaseDir = Join-Path $repoRoot "bin\Release"

# Find publish dir without hardcoding the TFM (e.g. net9.0 vs net10.0).
$publishDir = Get-ChildItem "$releaseDir\*\win-x64\publish" -Directory -ErrorAction SilentlyContinue |
              Sort-Object LastWriteTime -Descending |
              Select-Object -First 1

if (-not $publishDir) {
    Write-Error "Publish directory not found under $releaseDir"
    exit 1
}
$publishPath = $publishDir.FullName

$productVersion = $env:PRODUCT_VERSION
if (-not $productVersion) { $productVersion = "1.0.0" }
$productVersion = $productVersion.TrimStart('v')

$outDir = Join-Path $repoRoot "installer"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Write-Host "Publish dir : $publishPath"
Write-Host "Version     : $productVersion"
Write-Host "Output dir  : $outDir"

# Copy icon to publish dir so it gets bundled in the MSI.
Copy-Item (Join-Path $repoRoot "Assets\Sabeltann.ico") (Join-Path $publishPath "Sabeltann.ico") -Force

# Build and run the WixSharp installer project.
dotnet run --project (Join-Path $PSScriptRoot "Setup.csproj") --configuration Release -- `
    "$publishPath" "$productVersion" "$outDir"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Installer build failed"
    exit 1
}
