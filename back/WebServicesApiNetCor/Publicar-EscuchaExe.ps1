param(
    [string]$OutputDir = "D:\Union de precio y devoluciones\app-unificada\dist\EscuchaServicioInventario"
)

$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $projectDir "WebServicesApiNetCor.csproj"

Write-Host "Publicando escucha en $OutputDir" -ForegroundColor Cyan

if (Test-Path $OutputDir) {
    Remove-Item -LiteralPath $OutputDir -Recurse -Force
}

dotnet publish $projectFile `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    throw "La publicacion del escucha fallo."
}

Write-Host ""
Write-Host "Publicacion completada." -ForegroundColor Green
Write-Host "Ejecutable:" -ForegroundColor Yellow
Write-Host (Join-Path $OutputDir "ServicioInventario.exe")
Write-Host ""
Write-Host "Config editable:" -ForegroundColor Yellow
Write-Host (Join-Path $OutputDir "config\\escucha.json")
Write-Host ""
Write-Host "Este paquete ya incluye el runtime de .NET." -ForegroundColor Yellow
Write-Host "Pase la carpeta completa a la otra PC." -ForegroundColor Yellow
