param(
    [string]$OutputDir = "",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectFile = Join-Path $projectDir "BackBorradorDev.csproj"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path (Resolve-Path (Join-Path $projectDir "..\..\dist")).Path "BackBorradorDevPortable"
}

Write-Host "Publicando BackBorradorDev portable..." -ForegroundColor Cyan
Write-Host "Destino: $OutputDir"

if (Test-Path -LiteralPath $OutputDir) {
    Remove-Item -LiteralPath $OutputDir -Recurse -Force
}

$publishArgs = @(
    "publish",
    $projectFile,
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=false",
    "-o", $OutputDir
)

if ($SkipBuild) {
    $publishArgs += "--no-build"
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "La publicacion de BackBorradorDev fallo."
}

Copy-Item -LiteralPath (Join-Path $projectDir "Levantar-BackBorradorDev.ps1") -Destination $OutputDir -Force
Copy-Item -LiteralPath (Join-Path $projectDir "Levantar-BackBorradorDev.cmd") -Destination $OutputDir -Force

$readmePath = Join-Path $OutputDir "LEEME.txt"
@"
BackBorradorDev portable
========================

Como usar en otra computadora:

1. Copia esta carpeta completa a la otra PC.
2. Edita appsettings.json si necesitas cambiar servidor SQL, base de datos, usuario o password.
3. Ejecuta Levantar-BackBorradorDev.cmd.
4. En la app del PDA entra a Parametros y coloca la IP de esa PC con el puerto 5001.

Ejemplos:

- Levantar normal:
  Levantar-BackBorradorDev.cmd

- Levantar en otro puerto:
  Levantar-BackBorradorDev.cmd -Port 5002

- Intentar abrir firewall automaticamente, ejecutando como Administrador:
  Levantar-BackBorradorDev.cmd -OpenFirewall

Notas:

- El paquete incluye el runtime de .NET para Windows x64.
- No hace falta instalar Visual Studio ni SDK de .NET en la otra PC.
- Si el PDA no conecta, revisa firewall de Windows y que ambos equipos esten en la misma red.
"@ | Set-Content -LiteralPath $readmePath -Encoding UTF8

Write-Host ""
Write-Host "Publicacion completada." -ForegroundColor Green
Write-Host "Carpeta para copiar:" -ForegroundColor Yellow
Write-Host $OutputDir
Write-Host ""
Write-Host "Ejecuta en la otra PC:" -ForegroundColor Yellow
Write-Host "Levantar-BackBorradorDev.cmd"

