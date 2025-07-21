$extensions = @(".txt", ".log", ".csv", ".json", ".xml", ".html", ".md", ".conf", ".ini", ".dat")
$targetDir = "TODO"

# Create target directory if it doesn't exist
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir | Out-Null
}

for ($i = 1; $i -le 1000; $i++) {
    $name = [System.IO.Path]::GetRandomFileName().Split(".")[0]
    $ext = Get-Random -InputObject $extensions
    $file = Join-Path $targetDir "$name$ext"

    $sizeKB = Get-Random -Minimum 1 -Maximum 1024
    $bytes = New-Object byte[] ($sizeKB * 1024 * 600)
    [System.Random]::new().NextBytes($bytes)

    [System.IO.File]::WriteAllBytes($file, $bytes)
}

Write-Host "100 dummy files created in $targetDir"