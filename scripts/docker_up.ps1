param(
    [Parameter(Position = 0)]
    [ValidateSet("prod", "dev", "production", "development")]
    [string]$Mode = "prod",

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ExtraArgs
)

$rootDir = Split-Path -Parent $PSScriptRoot
Set-Location $rootDir

if ($Mode -eq "dev" -or $Mode -eq "development") {
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host "Starting Stream of Consciousness in DEVELOPMENT mode..." -ForegroundColor Cyan
    Write-Host "Backend:  .NET 8 SDK + Hot Reload (dotnet watch)"
    Write-Host "Frontend: Node 20 + Vite HMR"
    Write-Host "Database: MySQL 8.0"
    Write-Host "Proxy:    Nginx Dev Gateway"
    Write-Host "==========================================================" -ForegroundColor Cyan
    docker compose -f docker-compose.dev.yml up -d --build @ExtraArgs
    Write-Host "`nServices running:" -ForegroundColor Green
    Write-Host "-> Web Application:   http://localhost/"
    Write-Host "-> Vite Dev Server:   http://localhost:5173/"
    Write-Host "-> Backend API:       http://localhost:5000/swagger"
    Write-Host "-> Static Posts Disk: http://localhost/posts/{id}.html"
} else {
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host "Starting Stream of Consciousness in PRODUCTION mode..." -ForegroundColor Green
    Write-Host "Backend:  .NET 8 Compiled Release Runner (aspnet runtime)"
    Write-Host "Frontend: Production Vite Static Build"
    Write-Host "Database: MySQL 8.0"
    Write-Host "Proxy:    Nginx Gateway & Fast Disk Static Serving"
    Write-Host "==========================================================" -ForegroundColor Green
    docker compose -f docker-compose.yml up -d --build @ExtraArgs
    Write-Host "`nServices running:" -ForegroundColor Green
    Write-Host "-> Web Application:   http://localhost/"
    Write-Host "-> Backend API:       http://localhost/api/"
    Write-Host "-> Swagger UI:        http://localhost/swagger"
    Write-Host "-> Static Posts Disk: http://localhost/posts/{id}.html"
}
