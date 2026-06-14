$dllPath = "$PSScriptRoot\bin\Release\net10.0\Sabeltann.dll"
$resPath = "$PSScriptRoot\obj\Release\net10.0\Avalonia\resources"

if (!(Test-Path $dllPath) -or !(Test-Path $resPath)) { exit 0 }

$resBytes = [System.IO.File]::ReadAllBytes($resPath)
$dllBytes = [System.IO.File]::ReadAllBytes($dllPath)

# Find the !AvaloniaResources resource header in the DLL
$marker = [System.Text.Encoding]::Unicode.GetBytes("!AvaloniaResources")
$found = $false

for ($i = 0; $i -lt $dllBytes.Length - $marker.Length; $i++) {
    $match = $true
    for ($j = 0; $j -lt $marker.Length; $j++) {
        if ($dllBytes[$i + $j] -ne $marker[$j]) { $match = $false; break }
    }
    if ($match) {
        # Found the resource name marker. The data follows after the name + align + size.
        $nameEnd = $i + $marker.Length
        while ($nameEnd -lt $dllBytes.Length - 4) {
            $len = [System.BitConverter]::ToInt32($dllBytes, $nameEnd)
            if ($len -eq $resBytes.Length) {
                # Replace the resource data
                $dataStart = $nameEnd + 4
                if ($dataStart + $resBytes.Length -le $dllBytes.Length) {
                    [Array]::Copy($resBytes, 0, $dllBytes, $dataStart, $resBytes.Length)
                    [System.IO.File]::WriteAllBytes($dllPath, $dllBytes)
                    Write-Host "Patched !AvaloniaResources in DLL"
                    $found = $true
                    break
                }
            }
            $nameEnd += 4
        }
        break
    }
}

if (!$found) { Write-Host "Could not find !AvaloniaResources to patch" }
