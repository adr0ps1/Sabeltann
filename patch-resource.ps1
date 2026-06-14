param([string]$dllPath)

$resPath = "$PSScriptRoot\obj\Debug\net10.0\Avalonia\resources"
if (-not (Test-Path $dllPath)) { $dllPath = "$PSScriptRoot\bin\Debug\net10.0\SabeltannDevelopment.dll" }
$resPath = "$PSScriptRoot\obj\Debug\net10.0\Avalonia\resources"

if (-not (Test-Path $resPath)) {
    $resPath = "$PSScriptRoot\obj\Release\net10.0\Avalonia\resources"
    $dllPath = "$PSScriptRoot\bin\Release\net10.0\Sabeltann.dll"
}

if (-not (Test-Path $dllPath) -or -not (Test-Path $resPath)) {
    Write-Host "Cannot find DLL or resources file"
    exit 1
}

$resBytes = [System.IO.File]::ReadAllBytes($resPath)
$dllBytes = [System.IO.File]::ReadAllBytes($dllPath)

$marker = [System.Text.Encoding]::Unicode.GetBytes("!AvaloniaResources")

for ($i = 0; $i -lt $dllBytes.Length - $marker.Length; $i++) {
    $match = $true
    for ($j = 0; $j -lt $marker.Length; $j++) {
        if ($dllBytes[$i + $j] -ne $marker[$j]) { $match = $false; break }
    }
    if (-not $match) { continue }

    $dataOffset = $i + $marker.Length
    $dataLength = [System.BitConverter]::ToInt32($dllBytes, $dataOffset)
    $dataStart = $dataOffset + 4

    if ($dataLength -eq $resBytes.Length) {
        [Array]::Copy($resBytes, 0, $dllBytes, $dataStart, $resBytes.Length)
        [System.IO.File]::WriteAllBytes($dllPath, $dllBytes)
        Write-Host "Patched $($dllPath) with correct resources ($($resBytes.Length) bytes)"
        exit 0
    }
}

Write-Host "Could not patch - resource sizes differ (DLL: $dataLength, File: $($resBytes.Length))"