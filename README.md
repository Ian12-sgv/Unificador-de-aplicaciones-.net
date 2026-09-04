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

## Docker

El repositorio incluye un `Dockerfile` y un `docker-compose.yml` en la raíz para construir y levantar el backend `WebServicesApiNetCor` junto con una instancia opcional de SQL Server.

- Archivos relevantes: [Dockerfile](Dockerfile), [docker-compose.yml](docker-compose.yml), [.env.example](.env.example)

Pasos rápidos:

1. Copia la plantilla de ejemplo y ajusta credenciales locales:

```bash
cp .env.example .env
# editar .env y colocar DB_SA_PASSWORD, DB_NAME, DB_USER
```

2. Construir y levantar los contenedores:

```bash
docker-compose build
docker-compose up -d
docker-compose logs -f servicioinventario
```

3. Detener y eliminar recursos:

```bash
docker-compose down
```

Notas:

- Cambia `DB_SA_PASSWORD` en `.env` por una contraseña segura antes de usar en entornos compartidos.
- La cadena de conexión se pasa a la app via la variable `ConnectionStrings__SqlServer` definida en `docker-compose.yml`.
- El frontend MAUI no está contenido aquí; `docker-compose` orquesta sólo el backend y la base de datos.

