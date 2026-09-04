param(
    [string]$Configuration = "Debug",
    [string]$Framework = "net9.0-android",
    [switch]$SkipRestore,
    [switch]$SkipInstall,
    [switch]$SkipLaunch,
    [string]$ApplicationId = "AppPreciosPDB_.AppPreciosPDB_.maui",
    [string]$RuntimeIdentifier = "",
    [string]$AndroidAbi = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Get-SdkRoot {
    if ($env:ANDROID_SDK_ROOT -and (Test-Path $env:ANDROID_SDK_ROOT)) {
        return $env:ANDROID_SDK_ROOT
    }

    $defaultSdk = Join-Path $env:USERPROFILE "AppData\Local\Android\sdk"
    if (Test-Path $defaultSdk) {
        return $defaultSdk
    }

    throw "No se encontro ANDROID_SDK_ROOT ni el SDK en $defaultSdk."
}

function Get-LatestDirectoryByName {
    param(
        [string]$ParentPath,
        [string]$Filter
    )

    $directory = Get-ChildItem -Path $ParentPath -Directory |
        Where-Object { $_.Name -like $Filter } |
        Sort-Object Name -Descending |
        Select-Object -First 1

    if ($null -eq $directory) {
        throw "No se encontro un directorio que cumpla '$Filter' en '$ParentPath'."
    }

    return $directory.FullName
}

function Get-LatestVersionDirectory {
    param([string]$ParentPath)

    $directory = Get-ChildItem -Path $ParentPath -Directory |
        Sort-Object {
            $numericVersion = [regex]::Match($_.Name, "^\d+(\.\d+){0,3}").Value
            if ([string]::IsNullOrWhiteSpace($numericVersion)) {
                return [version]"0.0.0.0"
            }

            return [version]$numericVersion
        } -Descending |
        Select-Object -First 1

    if ($null -eq $directory) {
        throw "No se encontro una version valida en '$ParentPath'."
    }

    return $directory.FullName
}

function Get-PythonCommand {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -ne $python) {
        return $python.Source
    }

    $py = Get-Command py -ErrorAction SilentlyContinue
    if ($null -ne $py) {
        return $py.Source
    }

    throw "No se encontro python ni py para parchear el APK."
}

$projectRoot = $PSScriptRoot
$projectFile = Join-Path $projectRoot "Appinventario.Maui.csproj"
$artifactsRoot = Join-Path $projectRoot ("bin\" + $Configuration + "\" + $Framework)
$intermediateRoot = Join-Path $projectRoot ("obj\" + $Configuration + "\" + $Framework)

$sdkRoot = Get-SdkRoot
$buildToolsRoot = Get-LatestVersionDirectory -ParentPath (Join-Path $sdkRoot "build-tools")
$zipAlign = Join-Path $buildToolsRoot "zipalign.exe"
$apkSigner = Join-Path $buildToolsRoot "apksigner.bat"
$adb = Join-Path $sdkRoot "platform-tools\adb.exe"
$debugKeystore = Join-Path $env:USERPROFILE ".android\debug.keystore"

if ([string]::IsNullOrWhiteSpace($RuntimeIdentifier) -or [string]::IsNullOrWhiteSpace($AndroidAbi)) {
    $deviceAbi = ""
    if (Test-Path $adb) {
        $deviceAbi = (& $adb shell getprop ro.product.cpu.abi 2>$null).Trim()
    }

    if ([string]::IsNullOrWhiteSpace($AndroidAbi)) {
        $AndroidAbi = switch ($deviceAbi) {
            "x86_64" { "x86_64" }
            "x86" { "x86" }
            default { "arm64-v8a" }
        }
    }

    if ([string]::IsNullOrWhiteSpace($RuntimeIdentifier)) {
        $RuntimeIdentifier = switch ($AndroidAbi) {
            "x86_64" { "android-x64" }
            "x86" { "android-x86" }
            default { "android-arm64" }
        }
    }
}

$runtimeIntermediateRoot = Join-Path $intermediateRoot $RuntimeIdentifier
if (-not (Test-Path $runtimeIntermediateRoot)) {
    $runtimeIntermediateRoot = $intermediateRoot
}

$intermediateApkRoot = Join-Path $runtimeIntermediateRoot "android\bin"
$sharedLibPath = Join-Path $runtimeIntermediateRoot ("app_shared_libraries\" + $AndroidAbi + "\libxamarin-app.so")
$wrappedAssembliesRoot = Join-Path $runtimeIntermediateRoot "wrapped-disabled"
$managedAssembliesRoot = Join-Path $runtimeIntermediateRoot ("android\assets\" + $AndroidAbi)

$dotnetRoot = Split-Path -Parent (Get-Command dotnet -ErrorAction Stop).Source
$dotnetPacksRoot = Join-Path $dotnetRoot "packs"
if (-not (Test-Path $dotnetPacksRoot)) {
    throw "No se encontro la carpeta de packs de dotnet en '$dotnetPacksRoot'."
}

$androidRuntimePack = Get-LatestDirectoryByName -ParentPath $dotnetPacksRoot -Filter "Microsoft.Android.Runtime.35.$RuntimeIdentifier"
$androidRuntimeNativeRoot = Join-Path (Get-LatestVersionDirectory -ParentPath $androidRuntimePack) ("runtimes\" + $RuntimeIdentifier + "\native")

$targetRuntimeVersion = ($Framework -split "-")[0].Replace("net", "")
$monoRuntimePack = Get-LatestDirectoryByName -ParentPath $dotnetPacksRoot -Filter "Microsoft.NETCore.App.Runtime.Mono.$RuntimeIdentifier"
$monoRuntimeVersion = Get-ChildItem -Path $monoRuntimePack -Directory |
    Where-Object { $_.Name -like ($targetRuntimeVersion + ".*") } |
    Sort-Object { [version]$_.Name } -Descending |
    Select-Object -First 1
if ($null -eq $monoRuntimeVersion) {
    throw "No se encontro el runtime Mono $targetRuntimeVersion para '$RuntimeIdentifier'."
}
$monoRuntimeNativeRoot = Join-Path $monoRuntimeVersion.FullName ("runtimes\" + $RuntimeIdentifier + "\native")
if (-not (Test-Path $zipAlign)) { throw "No se encontro zipalign en '$zipAlign'." }
if (-not (Test-Path $apkSigner)) { throw "No se encontro apksigner en '$apkSigner'." }
if (-not (Test-Path $adb)) { throw "No se encontro adb en '$adb'." }
if (-not (Test-Path $debugKeystore)) { throw "No se encontro el debug.keystore en '$debugKeystore'." }
$sqliteNativePath = Get-ChildItem -Path $intermediateRoot -Recurse -Filter "libe_sqlite3.so" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -like ("*\jni\" + $AndroidAbi + "\libe_sqlite3.so") } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty FullName

$coreLibWrapperPath = ""

Write-Step "Proyecto MAUI"
Write-Host $projectFile
Write-Host "RuntimeIdentifier: $RuntimeIdentifier"
Write-Host "Android ABI: $AndroidAbi"

if (-not $SkipRestore) {
    Write-Step "Restaurando paquetes"
    & dotnet restore $projectFile --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore fallo con codigo $LASTEXITCODE."
    }
}

$buildSucceeded = $true
Write-Step "Compilando Android"
& dotnet build $projectFile -c $Configuration -f $Framework -p:RuntimeIdentifier=$RuntimeIdentifier -p:AndroidSdkDirectory=$sdkRoot --no-restore
if ($LASTEXITCODE -ne 0) {
    $buildSucceeded = $false
    Write-Warning "dotnet build termino con codigo $LASTEXITCODE. Se intentara reutilizar el APK base si fue generado."
}

$baseApk = Get-ChildItem -Path $intermediateApkRoot -Filter "*.apk" -File |
    Where-Object { $_.Name -notmatch "\.apk\." } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $baseApk) {
    throw "No se encontro el APK base en '$intermediateApkRoot'."
}

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null

$unsignedApk = Join-Path $artifactsRoot ("Appinventario.Maui-" + $RuntimeIdentifier + "-runtimepatched-unsigned.apk")
$alignedApk = Join-Path $artifactsRoot ("Appinventario.Maui-" + $RuntimeIdentifier + "-runtimepatched-aligned.apk")
$signedApk = Join-Path $artifactsRoot ("Appinventario.Maui-" + $RuntimeIdentifier + "-runtimepatched-signed.apk")

Write-Step "Creando APK parcheada"

$canPatchRuntime = (Test-Path $sharedLibPath) -and (Test-Path $managedAssembliesRoot)

if (-not $canPatchRuntime) {
    Write-Warning "No se encontraron todos los artefactos de parcheo; se firmara el APK base generado por MAUI."
    Copy-Item -Path $baseApk.FullName -Destination $unsignedApk -Force
}
else {
    $pythonCommand = Get-PythonCommand
    $patchScriptPath = Join-Path $env:TEMP "patch-maui-apk.py"
@'
import os
import sys
import zipfile

source_apk_path, target_apk_path, android_runtime_root, mono_runtime_root, shared_lib_path, wrapped_assemblies_root, managed_assemblies_root, android_abi = sys.argv[1:9]
sqlite_native_path = sys.argv[9] if len(sys.argv) > 9 else ""
corelib_wrapper_path = sys.argv[10] if len(sys.argv) > 10 else ""

entries = {}

for root in (android_runtime_root, mono_runtime_root):
    for file_name in sorted(os.listdir(root)):
        if not file_name.endswith(".so"):
            continue
        entries[f"lib/{android_abi}/{file_name}"] = os.path.join(root, file_name)

entries[f"lib/{android_abi}/libxamarin-app.so"] = shared_lib_path

debug_monodroid = os.path.join(android_runtime_root, "libmono-android.debug.so")
if os.path.exists(debug_monodroid):
    entries[f"lib/{android_abi}/libmonodroid.so"] = debug_monodroid

if wrapped_assemblies_root and os.path.exists(wrapped_assemblies_root):
    for file_name in sorted(os.listdir(wrapped_assemblies_root)):
        if not file_name.endswith(".so"):
            continue
        entries[f"lib/{android_abi}/{file_name}"] = os.path.join(wrapped_assemblies_root, file_name)

if corelib_wrapper_path and os.path.exists(corelib_wrapper_path):
    entries[f"lib/{android_abi}/lib_System.Private.CoreLib.dll.so"] = corelib_wrapper_path

for root, _, files in os.walk(managed_assemblies_root):
    for file_name in sorted(files):
        source_path = os.path.join(root, file_name)
        relative_path = os.path.relpath(source_path, managed_assemblies_root).replace("\\", "/")
        entries[f"assets/{android_abi}/{relative_path}"] = source_path

if sqlite_native_path and os.path.exists(sqlite_native_path):
    entries[f"lib/{android_abi}/libe_sqlite3.so"] = sqlite_native_path

if os.path.exists(target_apk_path):
    os.remove(target_apk_path)

with zipfile.ZipFile(source_apk_path, "r") as source_archive, zipfile.ZipFile(target_apk_path, "w", compression=zipfile.ZIP_STORED, allowZip64=True) as archive:
    for item in source_archive.infolist():
        if item.filename in entries:
            continue
        archive.writestr(item, source_archive.read(item.filename))

    for entry_name, source_path in sorted(entries.items()):
        archive.write(source_path, entry_name, compress_type=zipfile.ZIP_STORED)
'@ | Set-Content -Path $patchScriptPath -Encoding ASCII

    try {
        $sqliteNativeArgument = if ([string]::IsNullOrWhiteSpace($sqliteNativePath)) { "" } else { $sqliteNativePath }
        $coreLibWrapperArgument = if ([string]::IsNullOrWhiteSpace($coreLibWrapperPath)) { "" } else { $coreLibWrapperPath }
        & $pythonCommand $patchScriptPath $baseApk.FullName $unsignedApk $androidRuntimeNativeRoot $monoRuntimeNativeRoot $sharedLibPath $wrappedAssembliesRoot $managedAssembliesRoot $AndroidAbi $sqliteNativeArgument $coreLibWrapperArgument
        if ($LASTEXITCODE -ne 0) {
            throw "El parcheo del APK con python fallo con codigo $LASTEXITCODE."
        }
    }
    finally {
        if (Test-Path $patchScriptPath) {
            Remove-Item -LiteralPath $patchScriptPath -Force
        }
    }
}

Write-Step "Alineando y firmando APK"
if (Test-Path $alignedApk) { Remove-Item -LiteralPath $alignedApk -Force }
if (Test-Path $signedApk) { Remove-Item -LiteralPath $signedApk -Force }
if (Test-Path ($signedApk + ".idsig")) { Remove-Item -LiteralPath ($signedApk + ".idsig") -Force }

& $zipAlign -p -f 4 $unsignedApk $alignedApk
if ($LASTEXITCODE -ne 0) {
    throw "zipalign fallo con codigo $LASTEXITCODE."
}

Copy-Item -Path $alignedApk -Destination $signedApk -Force

& $apkSigner sign --ks $debugKeystore --ks-pass pass:android --key-pass pass:android --out $signedApk $alignedApk
if ($LASTEXITCODE -ne 0) {
    throw "apksigner fallo con codigo $LASTEXITCODE."
}

& $apkSigner verify --print-certs $signedApk
if ($LASTEXITCODE -ne 0) {
    throw "La verificacion de firma fallo con codigo $LASTEXITCODE."
}

if (-not $SkipInstall) {
    Write-Step "Instalando en PDA"
    & $adb devices
    & $adb install -r --no-incremental $signedApk
    if ($LASTEXITCODE -ne 0) {
        throw "adb install fallo con codigo $LASTEXITCODE."
    }

    Write-Step "Copiando assemblies FastDev"
    $deviceOverrideTemp = "/data/local/tmp/appinventario-override-$AndroidAbi"
    & $adb shell rm -rf $deviceOverrideTemp
    & $adb push $managedAssembliesRoot $deviceOverrideTemp
    if ($LASTEXITCODE -ne 0) {
        throw "adb push de assemblies fallo con codigo $LASTEXITCODE."
    }

    & $adb shell run-as $ApplicationId mkdir -p files/.__override__/$AndroidAbi
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo crear el directorio privado de assemblies."
    }
    & $adb shell run-as $ApplicationId cp -r "$deviceOverrideTemp/." "files/.__override__/$AndroidAbi/"
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudieron copiar los assemblies al directorio privado de la app."
    }

    if (-not $SkipLaunch) {
        Write-Step "Abriendo la app"
        & $adb shell monkey -p $ApplicationId -c android.intent.category.LAUNCHER 1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "No se pudo lanzar la aplicacion con adb."
        }
    }
}

Write-Step "Resultado"
Write-Host "Build limpia: $buildSucceeded"
Write-Host "APK base: $($baseApk.FullName)"
Write-Host "APK firmada: $signedApk"
