param(
    [Parameter(Position = 0)]
    [ValidateSet("prod", "dev", "production", "development", "all")]
    [string]$Mode = "prod",

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ExtraArgs
)

$rootDir = Split-Path -Parent $PSScriptRoot
Set-Location $rootDir

if ($Mode -eq "dev" -or $Mode -eq "development") {
    Write-Host "Stopping DEVELOPMENT containers..." -ForegroundColor Yellow
    docker compose -f docker-compose.dev.yml down @ExtraArgs
} elseif ($Mode -eq "all") {
    Write-Host "Stopping ALL containers (dev & prod)..." -ForegroundColor Yellow
    docker compose -f docker-compose.dev.yml down @ExtraArgs
    docker compose -f docker-compose.yml down @ExtraArgs
} else {
    Write-Host "Stopping PRODUCTION containers..." -ForegroundColor Yellow
    docker compose -f docker-compose.yml down @ExtraArgs
}

Write-Host "Containers successfully stopped." -ForegroundColor Green
