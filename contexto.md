# Contexto del Proyecto

## 1. Información general

### Nombre del proyecto
**App Unificada PDB** (carpeta `app-unificada`). En el dispositivo la app se muestra como **"Gestión"** (`ApplicationId: AppPreciosPDB_.AppPreciosPDB_.maui`).

### Descripción
Aplicación móvil Android (pensada para PDA con lector de código de barras) que reúne en una sola app varias herramientas de tienda que antes eran apps separadas:

- **Lector de Precios**
- **Borrador de Devolución (Borrador Dev)**
- **Transferencias** entre sucursales
- **Toma física** de inventario (dos modalidades: Toma 1 y Toma 2)
- **Parámetros** (configuración del servidor de escucha)

La app se comunica por HTTP en la red local con un backend .NET (el "escucha") que lee y escribe directamente en la base de datos SQL Server de la empresa.

### Objetivo
Unificar en una sola app y un solo backend lo que antes estaba repartido en:

- Una app Xamarin de precios/inventario con su backend en .NET Core 2.1 (`WebServicesApiNetCor`).
- Una app Flutter de borradores de devolución (`borrador_dev_app`) con su propio backend (`BackBorradorDev`).

Así el personal de tienda usa un solo dispositivo y una sola app para consultar precios, registrar devoluciones, armar transferencias y hacer conteos de inventario, y solo hay que configurar una IP de escucha.

### Estado actual
- **En desarrollo / pruebas (piloto).**
- Versión de la app: `1.0.10` (versionCode `13`). Backend activo `BackBorradorDev` `1.0.1`.
- El backend unificado (`BackBorradorDev`) apunta en la configuración local a la base de **pruebas** (`GDC_PRUEBAS`).
- El backend de precios (`WebServicesApiNetCor`) apunta a la base de **producción** (`GDC_CASA_MATRIZ`).
- Hay cambios sin confirmar en git (diagnóstico de SQL al arrancar, login con contraseñas en formato legacy, icono del backend) y carpetas de publicación/APK generadas localmente.

---

## 2. Alcance del proyecto

El sistema debe permitir:

- **Inicio de sesión en la app** con usuarios locales (`auth-users.json` embebido en el APK) y permisos por módulo (`price`, `borrador`, `transferencias`, `toma_fisica`, `parameters`). Los usuarios `ADMIN` / `SUPER_ADMIN` o con `canManageModules` tienen acceso completo y pueden activar o desactivar módulos (`ModuleAdminPage`).
- **Lector de precios**: escanear un código de barras y mostrar nombre, existencia, marca, precios detal / mayor / afiliado / promoción con impuesto incluido, precio en dólares según la última `TASA_CAMBIO`, y si hay promoción activa con su % de descuento.
- **Borrador de devolución**:
  - Crear borradores.
  - Escanear artículos (se valida que existan en `INVENTARIO` y el costo se toma de ahí).
  - Listar y filtrar por estado, ver y editar el detalle.
  - Aprobar ejecutando el procedimiento `dbo.Borrador_Dev`.
  - Un borrador aprobado (`Status = 1`) queda bloqueado y no se puede editar.
- **Transferencias entre sucursales**:
  - Crear y consultar transferencias y su historial.
  - Filtrar por estado y ver el resumen por estado: `0` Guardada, `1` Aprobada, `2` Exportada.
  - Modificar una transferencia solo si sigue en estado `0`.
  - Consultar catálogos de sucursales, tipos de despacho y lotes.
- **Toma física de inventario**:
  - Login contra `dbo.USUARIOS` (solo usuarios activos del grupo `INVE`), que devuelve un token Bearer válido 12 horas.
  - Dos modalidades, **Toma 1** y **Toma 2**, autorizadas por usuario (`allowedTomaFisicaModes`).
  - Registrar conteos por número de página e IMEI/estación, y consultar el detalle y los totales.
  - El parámetro `ValorLectorScaner` (`TABLA_VALORES`) define si se puede escribir la cantidad o hay que escanear unidad por unidad.
- **Parámetros**: configurar la IP y el puerto del escucha (compartidos por todos los módulos) y probar la conexión con el endpoint `health`.
- **Diagnóstico**:
  - Endpoints `ping` / `health` que validan la conexión a SQL Server.
  - Al arrancar, el backend valida la IP configurada y la conexión SQL.
  - En el dispositivo se guarda un registro local de errores (`CrashReport.log`).

### Fuera de alcance

- Mantener los datos maestros (artículos, precios, impuestos, marcas, tasa de cambio, sucursales, usuarios). La app solo los lee; se gestionan en el sistema administrativo que usa la misma base SQL Server.
- Aprobar y exportar transferencias (`Status` 1 y 2). La app siempre crea transferencias en estado `0` y solo consulta los otros estados.
- La lógica de negocio dentro de los procedimientos almacenados (`Borrador_Dev`, `ApiProcesarToma*`, `ApiBuscar*`). Viven en la base de datos y no se versionan en este repositorio.
- Clientes iOS, Windows y web: la app solo compila para Android.
- Acceso desde Internet: todo funciona dentro de la red local, sin dominio ni HTTPS.

---

## 3. Arquitectura

```text
[PDA Android - App MAUI "Gestión"]
        │  HTTP (red local, sin TLS)
        ▼
[BackBorradorDev - "ServicioUnificado" :5001]  ← backend activo de todos los módulos
        │  ADO.NET (Microsoft.Data.SqlClient)
        ▼
[SQL Server - tablas + procedimientos almacenados]

[WebServicesApiNetCor - "ServicioInventario" :8000]  ← backend legado de precios (Docker raíz)
```

La app guarda una sola dirección de escucha en SQLite (tabla `Parameter`, valor por defecto `192.168.0.135:5001`). Todos los módulos derivan de ella la URL `http://<host>:5001`.

### Frontend
- **Tecnología:** C# + XAML, aplicación móvil Android.
- **Framework:** .NET MAUI (`net9.0-android`, Android mínimo API 21 / 5.0).
- **Versión:** app `1.0.10` (build `13`). Dependencias: `Newtonsoft.Json 13.0.3`, `sqlite-net-pcl 1.9.172`, `Microsoft.Extensions.Logging.Debug 9.0.8`.
- **Persistencia local:** SQLite (`Inventary.db3`) para la configuración del escucha, y `Preferences` para la sesión y los módulos habilitados.
- **Particularidades:**
  - Controles propios para PDA: `SoftkeyboardDisabledEntry` (lector sin teclado en pantalla) y `RoundedEntry`.
  - Sonido de error `no-valido.mp3`.
  - `usesCleartextTraffic=true` para permitir HTTP.
- **Legado (solo referencia, no se compila para producción):**
  - `front/Appinventario`: Xamarin.Forms `5.0.0.2012` (netstandard2.0 + Android v10.0).
  - `legacy-referencia/borrador_dev_app`: Flutter (Dart `^3.9.2`, `http`, `shared_preferences`, `flutter_dotenv`).

### Backend

**1. `back/BackBorradorDev`, el backend activo ("ServicioUnificado")**
- **Tecnología:** C#, ASP.NET Core Web API (controladores), acceso a datos con ADO.NET puro.
- **Framework:** .NET 8 (`net8.0`), `Microsoft.Data.SqlClient 6.1.4`, `Microsoft.AspNetCore.OpenApi 8.0.22`.
- **Puerto:** `5001` (clave `Urls` en `appsettings.json`).
- **Endpoints:**
  - `borrador-dev/*` (ping, health, list, create, scan, run, detalle)
  - `/api/Articulo/{codigoBarra}`
  - `/api/Login` y `/api/Login/cerrar`
  - `/api/Sucursales`
  - `/api/Transferencia` (CRUD, resumen-estados, despachos, lotes)
  - `/api/TomaFisica`, `/api/TomaFisica2`
  - `/api/TomaFisicaDet`, `/api/TomaFisicaDet2`
  - `/api/Valores`, `/api/Valores2`
- **Autenticación:** token Bearer en memoria (`TomaFisicaSessionStore`, 12 h). Solo se exige en los endpoints de toma física.
- **Otros:**
  - CORS abierto (`AllowAllDev`) y JSON con nombres en PascalCase (sin camelCase).
  - Al arrancar, valida que la IP configurada sea la IP local del equipo y cierra instancias anteriores que usen el mismo puerto.
  - Se publica como ejecutable portable `win-x64` (`Publicar-BackPortable.ps1`) y se levanta con `Levantar-BackBorradorDev.ps1/.cmd`.

**2. `back/WebServicesApiNetCor`, el backend legado de precios ("ServicioInventario")**
- **Tecnología:** C#, ASP.NET Core MVC, Dapper `2.0.78`, `System.Data.SqlClient 4.5.3`, log4net `2.0.13`, Swagger (Swashbuckle `6.1.3`, solo en Development).
- **Framework:** .NET Core 2.1 (`netcoreapp2.1`, fuera de soporte), ensamblado `ServicioInventario` `1.0.285.0`.
- **Puerto:** `8000`. Se configura en `appsettings.json` o en `config/escucha.json`.
- **Endpoints:**
  - `/api/Articulo`, `/api/Health`, `/api/Login`
  - `/api/Sucursales`, `/api/SucursalesAll`
  - `/api/TomaFisica`, `/api/TomaFisica2`, `/api/TomaFisicaDet`
  - `/api/Transferencia`, `/api/Valores`
  - `borrador-dev/*` (copia anterior)
- Se publica como `.exe` autocontenido (`Publicar-EscuchaExe.ps1`). Es el servicio que construyen el `Dockerfile` y el `docker-compose.yml` de la raíz.

### Base de datos
- **Motor:** Microsoft SQL Server.
- **Versión:** no se indica en el código. El contenedor Docker usa la imagen `mcr.microsoft.com/mssql/server:2019-latest`.
- **Nombre de la base de datos:**
  - `GDC_PRUEBAS` (instancia `SERVERDOS\EXTENSIONES`): la usa `BackBorradorDev` en la configuración local actual.
  - `GDC_CASA_MATRIZ` (instancia `SERVERDOS\SERVERSQL_DOS`): la usa `WebServicesApiNetCor`.
  - `ServicioDb`: valor por defecto en `.env.example` para el contenedor Docker.
- **Tablas utilizadas:**
  - Artículos y precios: `INVENTARIO`, `IMPUESTOS`, `MARCAS`, `TASA_CAMBIO`.
  - Borradores de devolución: `DEVBORRADOR`, `MOVDEVBORRADOR`.
  - Transferencias: `TRANSFERENCIAS`, `movTRANSFERENCIAS`, `TIPO_DESPACHO`, `LOTES`, `sucursales`.
  - Usuarios: `USUARIOS`, `USUARIOGRUPO`.
  - Toma física: `TOMAFISICA1`, `TOMAFISICA2`, `TABLA_VALORES`.
- **Procedimientos almacenados:**
  - `Borrador_Dev`
  - `ApiProcesarToma` / `ApiProcesarToma_DOS`
  - `ApiBuscarTomaFisicaMae[_DOS]`, `ApiBuscarTomaFisicaPost[_DOS]`
  - `ApiBuscarTomaFisicaDetalle[_DOS]`, `ApiBuscarTomaFisicaTotal[_DOS]`
  - `ApiBuscarValores[_DOS]`
  - El sufijo `_DOS` corresponde a la Toma 2.
- **Contraseñas:** `USUARIOS.Pasword` admite texto plano y el formato legacy cifrado. Este último se valida con `PasswordLegacyCodec`.

### Infraestructura
- **Sistema operativo:**
  - Backends: Windows. Se distribuyen como `.exe` portables `win-x64`, usan `netstat` y `netsh` para gestionar puertos y firewall, y tienen scripts PowerShell.
  - Clientes: PDA y teléfonos Android ARM (`android-arm` / `android-arm64`).
- **Docker:**
  - `Dockerfile` + `docker-compose.yml` en la raíz: construyen solo `WebServicesApiNetCor` (SDK/ASP.NET 2.1, puerto `8000`) más un contenedor SQL Server 2019 (puerto `1433`, volumen `mssqldata`). Las variables se toman de `.env`.
  - `back/BackBorradorDev/Dockerfile`: imagen .NET 8 en el puerto `5001`. No está incluida en el `docker-compose`.
- **Servidor:**
  - El escucha corre en un PC Windows de la red local (IPs `192.168.0.x`; la app usa `192.168.0.135:5001` por defecto).
  - SQL Server está en el servidor `SERVERDOS`.
- **Proxy:** ninguno. Kestrel atiende directamente por HTTP.
- **Dominio:** ninguno. Se accede por IP privada dentro de la LAN.

---

## 4. Estructura del proyecto

```text
app-unificada/
│
├── front/
│   ├── Appinventario.Maui/              # Frontend ACTUAL (.NET MAUI, Android)
│   │   ├── Views/                       # Páginas XAML: Login, Precio, Transferencias, Toma física, Parámetros…
│   │   │   └── BorradorDev/             # Lista, búsqueda y detalle de borradores de devolución
│   │   ├── ViewModels/                  # MVVM (Precio, Transferencia, TomaFisica, Parameter…)
│   │   ├── Models/                      # DTOs (Articulo, Transferencia, TomaFisica, BorradorDev/…)
│   │   ├── Services/                    # AuthService, TomaFisicaAuthService, Util (HttpClient), CrashReport
│   │   │   └── BorradorDev/             # Cliente HTTP y configuración de URL del escucha
│   │   ├── Data/DatabaseQuery.cs        # SQLite local (tabla Parameter = IP del escucha)
│   │   ├── Controls/                    # Entries personalizados para PDA
│   │   ├── Platforms/Android/           # Manifest, handlers, audio, lectura de IMEI
│   │   ├── Resources/                   # Iconos, fuentes, splash, sonidos (Raw/)
│   │   ├── auth-users.example.json      # Plantilla de usuarios locales (auth-users.json no se versiona)
│   │   └── Build-MauiPda.ps1            # Compila, instala y lanza en la PDA vía adb
│   ├── Appinventario/                   # Frontend Xamarin.Forms LEGADO (referencia)
│   └── Appinventario.sln
│
├── back/
│   ├── BackBorradorDev/                 # Backend ACTIVO .NET 8 (puerto 5001)
│   │   ├── Controllers/                 # BorradorDev, Login, Sucursales, Transferencia, TomaFisica(1/2), Valores(1/2)
│   │   ├── Util/PasswordLegacyCodec.cs
│   │   ├── Program.cs                   # Validación de IP, diagnóstico SQL, CORS
│   │   ├── SqlStoredProcedureReader.cs  # Ejecuta SPs derivando sus parámetros
│   │   ├── SqlConnectionDiagnostics.cs
│   │   ├── TomaFisicaSessionStore.cs    # Tokens de sesión en memoria
│   │   ├── appsettings.example.json
│   │   ├── Dockerfile
│   │   ├── Publicar-BackPortable.ps1 / Levantar-BackBorradorDev.ps1 / .cmd
│   │   └── publish-*/ , app unificada/  # Publicaciones locales (ignoradas por git)
│   ├── WebServicesApiNetCor/            # Backend LEGADO de precios .NET Core 2.1 (puerto 8000)
│   │   ├── Controllers/
│   │   ├── Data/                        # Repositorios Dapper
│   │   ├── Models/
│   │   ├── Util/                        # Modulo (cadena de conexión), SesionUsuarioStore, PasswordLegacyCodec
│   │   ├── config/escucha.example.json
│   │   ├── log4net.config
│   │   └── Publicar-EscuchaExe.ps1
│   └── WebServicesApiNetCor.sln
│
├── legacy-referencia/
│   └── borrador_dev_app/                # App Flutter original de Borrador Dev (referencia funcional/visual)
│
├── respaldo-antes-usuarios-toma-fisica-20260916/   # Respaldo de backends antes del cambio de usuarios de toma física
├── trabajo de compañero/                # Copia del repositorio aportada por otro desarrollador (sin integrar)
├── Nueva carpeta/                       # Capturas sueltas (sin versionar)
│
├── .env / .env.example                  # Variables para docker-compose (DB_SA_PASSWORD, DB_NAME, DB_USER…)
├── .dockerignore / .gitignore
├── Dockerfile                           # Imagen de WebServicesApiNetCor
├── docker-compose.yml                   # WebServicesApiNetCor + SQL Server 2019
├── README.md
└── contexto.md
```

> **Configuración local requerida:** copiar cada `appsettings.example.json` a `appsettings.json` (y `config/escucha.example.json` a `escucha.json`). Copiar también `auth-users.example.json` a `auth-users.json`. Ninguno de estos archivos se versiona porque contienen credenciales.
