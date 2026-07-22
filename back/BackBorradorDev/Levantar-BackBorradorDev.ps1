param(
    [int]$Port = 5001,
    [string]$HostAddress = "0.0.0.0",
    [switch]$OpenFirewall
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$exePath = Join-Path $scriptDir "BackBorradorDev.exe"
$appSettingsPath = Join-Path $scriptDir "appsettings.json"
$url = "http://$HostAddress`:$Port"

if (-not (Test-Path -LiteralPath $exePath)) {
    Write-Host "No se encontro BackBorradorDev.exe en:" -ForegroundColor Red
    Write-Host $scriptDir
    Write-Host ""
    Write-Host "Ejecuta primero Publicar-BackPortable.ps1 o copia la carpeta publicada completa." -ForegroundColor Yellow
    pause
    exit 1
}

if (-not (Test-Path -LiteralPath $appSettingsPath)) {
    Write-Host "No se encontro appsettings.json en la carpeta del ejecutable." -ForegroundColor Red
    Write-Host "Copia la carpeta publicada completa para conservar la conexion SQL." -ForegroundColor Yellow
    pause
    exit 1
}

if ($OpenFirewall) {
    try {
        $ruleName = "BackBorradorDev puerto $Port"
        $existingRule = netsh advfirewall firewall show rule name="$ruleName" 2>$null
        if ($LASTEXITCODE -ne 0 -or ($existingRule -join "`n") -match "No rules match") {
            netsh advfirewall firewall add rule name="$ruleName" dir=in action=allow protocol=TCP localport=$Port | Out-Null
            Write-Host "Regla de firewall creada para el puerto $Port." -ForegroundColor Green
        }
    }
    catch {
        Write-Host "No se pudo abrir el firewall automaticamente. Ejecuta PowerShell como Administrador o abre el puerto $Port manualmente." -ForegroundColor Yellow
    }
}

$env:Urls = $url
$env:ASPNETCORE_URLS = $url

Write-Host "Levantando BackBorradorDev..." -ForegroundColor Cyan
Write-Host "URL local:     http://localhost:$Port"
Write-Host "URL red/PDA:   http://IP-DE-ESTA-PC:$Port"
Write-Host "Escuchando en: $url"
Write-Host ""
Write-Host "Para cambiar SQL o credenciales edita appsettings.json en esta misma carpeta." -ForegroundColor Yellow
Write-Host "Para cerrar el back presiona Ctrl+C o cierra esta ventana."
Write-Host ""

& $exePath

