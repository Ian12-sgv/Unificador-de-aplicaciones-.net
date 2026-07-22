# Unificador de aplicaciones .NET

Aplicacion unificada para consulta de precios, borradores de devolucion y transferencias.

## Estructura

- `front`
  Contiene el frontend actual en MAUI y el frontend Xamarin legado de referencia.
- `back`
  Contiene los dos backends .NET usados por la app unificada.
- `legacy-referencia`
  Contiene la app Flutter original de `Borrador Dev` y notas antiguas que se conservaron solo como referencia.

## Rutas Principales

- `front/Appinventario.Maui`
  Frontend actual y objetivo principal.
- `front/Appinventario`
  Frontend Xamarin legado, conservado como referencia.
- `front/Appinventario.sln`
  Solucion del frontend.
- `back/WebServicesApiNetCor`
  Backend de precios.
- `back/BackBorradorDev`
  Backend de borradores de devolucion.
- `back/WebServicesApiNetCor.sln`
  Solucion existente del backend de precios.
- `legacy-referencia/borrador_dev_app`
  Implementacion Flutter original usada como referencia funcional y visual.

## Nota

La idea a partir de ahora es trabajar siempre dentro de `app-unificada` para evitar seguir manejando dos carpetas principales separadas.

## Configuracion local

Antes de ejecutar los backends, copia cada `appsettings.example.json` como `appsettings.json` y completa la cadena de conexion local. Los archivos con credenciales reales no se versionan.

Para el inicio de sesion del frontend MAUI, copia `front/Appinventario.Maui/auth-users.example.json` como `auth-users.json`, completa las cuentas locales y coloca el archivo en el directorio de datos de la aplicacion. En desarrollo tambien puedes indicar su ruta con la variable de entorno `APP_AUTH_USERS_FILE`.

Las cuentas antiguas se conservaron localmente en `front/Appinventario.Maui/Services/AuthService.Local.cs.disabled` solo como apoyo para migrarlas. Ese archivo no se compila ni se sube a GitHub.
