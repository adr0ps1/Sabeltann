$setupDir = Split-Path $MyInvocation.MyCommand.Path -Parent
$publishDir = "$setupDir\..\bin\Release\net10.0\win-x64\publish"
if (-not (Test-Path $publishDir)) {
  $publishDir = "$setupDir\..\bin\Release\net10.0\publish"
  if (-not (Test-Path $publishDir)) { Write-Error "Publish directory not found"; exit 1 }
}
$publishDir = (Resolve-Path $publishDir).Path
$productVersion = $env:PRODUCT_VERSION
if (-not $productVersion) { $productVersion = "1.0.0" }
$productVersion = $productVersion.TrimStart('v')

$outDir = "$setupDir\..\installer"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Generate Components.wxs from publish directory
@"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="SabeltannFiles" Directory="INSTALLFOLDER">
"@ | Set-Content "$setupDir\Components.wxs"

Get-ChildItem "$publishDir\*.exe", "$publishDir\*.dll" | ForEach-Object {
    $id = "f" + (Get-Random -Maximum 99999999)
    @(
        "      <Component Id='$id' Guid='*' Bitness='always64'>",
        "        <File Source='`$(var.PublishDir)\$($_.Name)' />",
        "      </Component>"
    ) | Add-Content "$setupDir\Components.wxs"
}

@"
    </ComponentGroup>
  </Fragment>
</Wix>
"@ | Add-Content "$setupDir\Components.wxs"

# Build MSI
wix build "$setupDir\Setup.wxs" "$setupDir\Components.wxs" `
  -d ProductVersion="$productVersion" `
  -d PublishDir="$publishDir" `
  -arch x64 `
  -o "$outDir\Sabeltann-$productVersion.msi"

if ($LASTEXITCODE -eq 0) {
    Write-Host "MSI created: $outDir\Sabeltann-$productVersion.msi"
} else {
    Write-Error "WiX build failed"
    exit 1
}