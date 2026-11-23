---
name: Gestión PDB
description: App de tienda para PDA Android que reúne precios, devoluciones, transferencias y toma física.
colors:
  brand: "#1E4855"
  brand-dark: "#163A45"
  primary: "#087A62"
  primary-pressed: "#06604D"
  primary-container: "#E3F2EE"
  primary-outline: "#B3DCD1"
  on-primary: "#FFFFFF"
  price: "#C2410C"
  canvas: "#F4F6F8"
  surface: "#FFFFFF"
  surface-muted: "#EAF1F4"
  outline: "#D9E1E5"
  outline-strong: "#7D8F96"
  text-primary: "#1D2E35"
  text-secondary: "#56676E"
  error: "#9C2F27"
  error-container: "#FDECEA"
  error-outline: "#F4C7C3"
  warning: "#8A5A00"
  warning-container: "#FFF4DB"
  warning-outline: "#F2D6A2"
  info: "#2E6A8E"
  info-container: "#E6EFF5"
typography:
  display:
    fontFamily: "Open Sans, Roboto, sans-serif"
    fontSize: "40sp"
    fontWeight: 700
  headline:
    fontFamily: "Open Sans, Roboto, sans-serif"
    fontSize: "22sp"
    fontWeight: 600
  title:
    fontFamily: "Open Sans, Roboto, sans-serif"
    fontSize: "18sp"
    fontWeight: 600
  subtitle:
    fontFamily: "Open Sans, Roboto, sans-serif"
    fontSize: "16sp"
    fontWeight: 400
  body:
    fontFamily: "Open Sans, Roboto, sans-serif"
    fontSize: "14sp"
    fontWeight: 400
  caption:
    fontFamily: "Open Sans, Roboto, sans-serif"
    fontSize: "12sp"
    fontWeight: 400
rounded:
  sm: "5dp"
  md: "8dp"
  pill: "24dp"
spacing:
  xs: "4dp"
  sm: "8dp"
  md: "12dp"
  lg: "16dp"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.on-primary}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    padding: "10dp 16dp"
    height: "48dp"
  button-secondary:
    backgroundColor: "{colors.surface-muted}"
    textColor: "{colors.brand}"
    typography: "{typography.body}"
    rounded: "{rounded.md}"
    padding: "10dp 16dp"
    height: "48dp"
  button-disabled:
    backgroundColor: "{colors.outline}"
    textColor: "{colors.text-secondary}"
    rounded: "{rounded.md}"
    height: "48dp"
  chip-filter:
    backgroundColor: "{colors.surface-muted}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.body}"
    rounded: "{rounded.pill}"
    padding: "0 16dp"
    height: "48dp"
  chip-filter-selected:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.on-primary}"
    rounded: "{rounded.pill}"
    height: "48dp"
  scan-field:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text-primary}"
    typography: "{typography.subtitle}"
    rounded: "{rounded.pill}"
    height: "48dp"
  card:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.md}"
    padding: "12dp"
  status-banner-error:
    backgroundColor: "{colors.error-container}"
    textColor: "{colors.error}"
    rounded: "{rounded.md}"
  status-banner-success:
    backgroundColor: "{colors.primary-container}"
    textColor: "{colors.primary}"
    rounded: "{rounded.md}"
---

# Design System: Gestión PDB

## Overview

**Creative North Star: "El mostrador de tienda"**

Una herramienta de trabajo que se usa de pie, con una mano, bajo la luz del local y con el escáner en la otra mano. La interfaz se borra para que mande el dato: el precio, la cantidad, el estado de la transferencia. El color no decora; marca dónde se actúa, qué estado tiene algo y cuál es la cifra que el operador vino a buscar.

El sistema es claro y sobrio: fondo gris muy claro, tarjetas blancas, texto pizarra y un solo verde para la acción. El azul petróleo de la marca vive en la barra superior y en los encabezados, nunca en los botones. El naranja oscuro está reservado para la cifra escaneada: el precio en Lector de Precios y la existencia en Toma física.

La app es solo de tema claro (`UserAppTheme = Light`), por decisión de uso: se trabaja en tiendas iluminadas y en PDA donde el tema oscuro no aporta. Si algún día se agrega, debe diseñarse como paleta propia, no como inversión.

**Key Characteristics:**
- Un color de acción (verde `primary`) y un color de dato (naranja `price`); todo lo demás es neutro o de estado.
- Controles táctiles de 48 dp como mínimo, pensados para PDA y guantes.
- Avisos que no interrumpen el escaneo: snackbar para éxito y validaciones, diálogo solo para errores y decisiones.
- Ningún texto por debajo de 12 sp.

## Colors

Neutros pizarra con un verde azulado de acción; los colores de estado aparecen solo con su fondo claro correspondiente.

### Primary
- **Verde mostrador** (`primary`): botón principal, chip o caja seleccionada, estado "Aprobada", mensajes de éxito. Texto blanco encima con contraste 5.29:1.
- **Petróleo de marca** (`brand`): barra superior, menú lateral, encabezados de módulo, número de ítem en las líneas. Nunca en botones.

### Secondary
- **Naranja precio** (`price`): solo para la cifra principal escaneada (precio, existencia). 5.18:1 sobre blanco.

### Neutral
- **Lienzo** (`canvas`): fondo de pantalla.
- **Superficie** (`surface`): tarjetas, campos y listas.
- **Superficie atenuada** (`surface-muted`): chips sin seleccionar, botones del selector de cantidad, fondos de bloque.
- **Contorno** (`outline`): divisores y bordes de tarjeta. **Contorno fuerte** (`outline-strong`): bordes de campos y controles (3.36:1, cumple el mínimo de 3:1 para controles).
- **Texto principal** (`text-primary`) y **texto secundario** (`text-secondary`, 5.16:1 o más sobre todos los fondos claros; también es el color de los placeholders).

### Estados
- **Error** (`error` / `error-container` / `error-outline`), **Aviso** (`warning` …) e **Info** (`info` …). En Transferencias: Guardada = aviso, Aprobada = primary, Exportada = info.

### Named Rules
**The One Action Rule.** Solo `primary` dice "toca aquí". Si algo no es una acción ni un estado aprobado, no lleva verde.

**The Price Is the Hero Rule.** `price` se usa únicamente para la cifra que el operador vino a buscar. Nunca para títulos, íconos ni decoración.

**The Token-Only Rule.** Las vistas usan `{StaticResource …}` y el código usa `AppColors`; no se escriben colores hexadecimales en pantallas ni en handlers.

## Typography

**Display Font:** Open Sans (con Roboto de respaldo)
**Body Font:** Open Sans Regular; botones en Open Sans Semibold

**Character:** Una sola familia, neutra y muy legible en pantallas chicas; la jerarquía se arma con tamaño y peso, no con más fuentes.

### Hierarchy
- **Display** (bold, `FontDisplay` 40 sp): el precio en Lector de Precios. Nada más.
- **Headline** (`FontHeadline` 22 sp): títulos de pantalla de acceso, cifras destacadas como la existencia.
- **Title** (`FontTitle` 18 sp): títulos de sección y encabezados de módulo.
- **Subtitle** (`FontSubtitle` 16 sp): texto de campos, cantidades, títulos de tarjeta.
- **Body** (`FontBody` 14 sp): texto general, botones, chips.
- **Caption** (`FontCaption` 12 sp): etiquetas de campo, metadatos, fechas. Es el piso.

### Named Rules
**The 12 sp Floor Rule.** Ningún texto baja de `FontCaption`. Si no entra, se reescribe o se reorganiza; no se achica.

## Layout

Pantallas de una columna con margen lateral de 12–16 dp, tarjetas apiladas con 8–10 dp de separación y bloques de resumen en grilla de 2 o 3 columnas (Transferencias). Las listas usan `CollectionView`. El campo de escaneo va al pie de la pantalla en Lector de Precios para quedar al alcance del pulgar. Los objetivos táctiles miden 48 dp y se separan al menos 8 dp; una acción destructiva queda a 16 dp de la acción vecina.

## Elevation & Depth

Plano. La profundidad se da por capas tonales: lienzo gris, tarjeta blanca encima, borde `outline` cuando hace falta separar. No se usan sombras; la barra superior no tiene sombra (`NavBarHasShadow = False`).

### Named Rules
**The Flat Counter Rule.** Ninguna superficie lleva sombra. Si dos zonas se confunden, cambia el tono del fondo o agrega un borde de 1 dp.

## Shapes

Esquinas suaves de 8 dp para tarjetas, botones y campos de formulario; píldora de 24 dp para chips de filtro, el campo de escaneo y los botones circulares de caja (48 dp). Las insignias de estado usan 5 dp.

## Components

### Buttons
- **Shape:** esquinas suaves (8 dp), alto mínimo 48 dp.
- **Primary:** fondo `primary`, texto `on-primary` en Open Sans Semibold 14 sp, relleno 10×16 dp. Es el estilo implícito de todos los `Button`.
- **Secondary:** fondo `surface-muted` con texto `brand` (Probar conexión, Aprobar). En una pila, la principal va primero y "Cancelar" queda como botón de texto en `text-secondary`.
- **Disabled:** fondo `outline` con texto `text-secondary`.
- **Destructivo en línea:** botón de texto "Quitar" en color `error`, sin fondo, 48 dp de alto.

### Chips
- **Style:** píldora de 48 dp; sin seleccionar `surface-muted` con texto `text-secondary`, seleccionada `primary` con texto blanco.
- **State:** filtros de estado en Historial de transferencias y selector de caja 1/2/3.

### Cards / Containers
- **Corner Style:** 8 dp.
- **Background:** `surface` sobre `canvas`.
- **Shadow Strategy:** ninguna (ver Elevation & Depth).
- **Border:** `outline` 1 dp cuando la tarjeta está sobre fondo blanco.
- **Internal Padding:** 10–12 dp.

### Inputs / Fields
- **Style:** fondo `surface`, borde `outline-strong` 1 dp, esquinas 8 dp, texto 16 sp; placeholder en `text-secondary`.
- **Campo de escaneo:** borde `primary` 2 dp, esquinas de píldora (24 dp); en pantallas con lector no abre el teclado en pantalla.

### Navigation
- Shell con menú lateral: barra `brand` con título y íconos blancos; el menú se arma según los permisos del usuario. Cerrar sesión pide confirmación con botones "Cerrar sesión" / "Cancelar".

### Feedback
- **Snackbar** (`FeedbackService.ShowAsync`): éxito y validaciones de formulario; no bloquea el escaneo.
- **Diálogo** (`DisplayAlert`): errores de red o servidor, configuración faltante, sesión vencida y decisiones (eliminar, modificar). Botón por defecto "Aceptar".

### Quantity Stepper
Control de cantidad en las líneas de transferencia: botones − y + de 48 dp en `surface-muted` dentro de un borde `outline-strong` de 8 dp, cantidad en 16 sp bold al centro.

## Do's and Don'ts

### Do:
- **Do** usar `{StaticResource …}` de `Colors.xaml` y `Typography.xaml` en XAML, y `AppColors` / `AppFonts` en código.
- **Do** dar 48 × 48 dp a todo control táctil y 16 dp de separación a una acción destructiva.
- **Do** usar `FeedbackService.ShowAsync` para éxito y validaciones, y `DisplayAlert` con "Aceptar" para errores.
- **Do** escribir en usted y con tildes: "Escanee o escriba un código de barra", no "Debe insertar codigo".

### Don't:
- **Don't** escribir colores hexadecimales en vistas, code-behind ni handlers nativos.
- **Don't** usar `price` fuera de la cifra escaneada ni `brand` en botones.
- **Don't** usar textos de menos de 12 sp.
- **Don't** usar caracteres como íconos ("o", "||||", "<->"); usar una forma (`Ellipse`) o una imagen.
- **Don't** abrir un diálogo para avisar que algo salió bien.
