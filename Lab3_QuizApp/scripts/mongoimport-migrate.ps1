param(
    [string]$FilePath = "$env:LOCALAPPDATA\Laboration_3\Laboration_3.json",
    [string]$ConnectionString = "mongodb://localhost:27017",
    [string]$Database = "QuizAppDb",
    [string]$Collection = "Packs",
    [switch]$Drop,
    [string]$MongoImportPath = "mongoimport"
)

function Abort([string]$msg, [int]$code=1) {
    Write-Error $msg
    exit $code
}

Write-Host "=== Mongo import helper ==="
Write-Host "Source file: $FilePath"
Write-Host "Target: $ConnectionString / $Database.$Collection"
if ($Drop) { Write-Host "Option: --drop (will remove existing collection before import)" }

# 1) Ensure source file exists
if (-not (Test-Path $FilePath)) {
    Abort "Source file not found: $FilePath" 2
}

# 2) Backup source file
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$backupPath = "$FilePath.$timestamp.bak"
try {
    Copy-Item -Path $FilePath -Destination $backupPath -ErrorAction Stop
    Write-Host "Backup created: $backupPath"
} catch {
    Abort "Failed to create backup: $_" 3
}

# 3) Validate JSON and determine if it's an array
try {
    $raw = Get-Content -Path $FilePath -Raw -ErrorAction Stop
    $parsed = $raw | ConvertFrom-Json -ErrorAction Stop
} catch {
    Abort "Invalid JSON in file. ConvertFrom-Json failed: $_" 4
}

$isArray = $parsed -is [System.Array]
$importFile = $FilePath
$tempFile = $null

if (-not $isArray) {
    Write-Host "Source JSON is a single object — wrapping in array for mongoimport..."
    $tempFile = Join-Path $env:TEMP ("laboration3_import_$timestamp.json")
    # Preserve indentation/newlines; wrap safely
    $wrapped = "[`n$raw`n]"
    try {
        Set-Content -Path $tempFile -Value $wrapped -Encoding UTF8 -Force
        Write-Host "Temporary import file created: $tempFile"
        $importFile = $tempFile
    } catch {
        Abort "Failed to write temporary import file: $_" 5
    }
} else {
    Write-Host "Source JSON is an array — using original file for import."
}

# 4) Locate mongoimport
$mongoImportExe = $null
$cmd = Get-Command -Name $MongoImportPath -ErrorAction SilentlyContinue
if ($cmd) {
    $mongoImportExe = $cmd.Source
} elseif (Test-Path $MongoImportPath) {
    $mongoImportExe = (Resolve-Path $MongoImportPath).Path
} else {
    Abort "mongoimport not found. Install MongoDB Database Tools and ensure `mongoimport` is in PATH or pass -MongoImportPath." 6
}
Write-Host "Using mongoimport: $mongoImportExe"

# 5) Build arguments
$args = @("--uri", $ConnectionString, "--db", $Database, "--collection", $Collection, "--file", $importFile, "--jsonArray")
if ($Drop) { $args += "--drop" }

Write-Host "Running mongoimport..."
Write-Host "$mongoImportExe $($args -join ' ')"

# 6) Execute
$proc = Start-Process -FilePath $mongoImportExe -ArgumentList $args -NoNewWindow -Wait -PassThru
if ($proc.ExitCode -eq 0) {
    Write-Host "Import completed successfully."
    $exitCode = 0
} else {
    Write-Error "mongoimport failed with exit code $($proc.ExitCode)."
    $exitCode = $proc.ExitCode
}

# 7) Cleanup temp file if used
if ($tempFile -and (Test-Path $tempFile)) {
    try { Remove-Item $tempFile -Force -ErrorAction SilentlyContinue } catch {}
}

exit $exitCode