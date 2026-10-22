# Estilos medidos y mensajes del SAP MES real

Medido en el **ambiente de testing** del SAP MES (2026-10-19, sesión de fidelidad con el
navegador), workstation `L_TP_SC_ASSYC_DC_TL_PL_LC_ESIG`, operación `O_OMNI_FIRST_DISTAL`,
recurso `R_OMNI_FIRST_DISTAL_2`, sitio CR9A. Valores en **px de CSS** (estilos computados), no
en px de captura: el zoom del navegador no los cambia.

Es la referencia de **cómo se ve** el MES. Las diferencias con el simulador y cuáles se
corrigen están en `docs/audits/2026-10-19-fidelidad-ui.md`.

> Varía por workstation: los botones del panel derecho y del pie (aquí además File Upload, DC
> File Upload, Place back in queue, View Document, Enlabel Report), el (?) del POD Work List
> y la firma electrónica de DC (`_ESIG`). Lo de abajo es lo común.

---

## Tipografía y color base

| Qué | Valor |
|---|---|
| Fuente | SAP **"72"** (cargada; respaldo Arial, Helvetica) |
| Texto general (botones, celdas, links, valores) | **12 px**, peso 400 |
| Etiquetas de formulario (`SFC:`, `NC Code:`, `Mode:`…) | 12.8 px, `#444444` |
| Títulos de panel y de ventana (`POD Work List`, `NC Data Entry`) | 12.8 px negrita, `#333333` |
| Links (operación, recurso, paginación) | `#00689E` |
| Azul de selección y estado activo | `#007CBF` (borde `#00689E`) |

## POD

| Bloque | Medida |
|---|---|
| Header | 29 px de alto, `#DDDDDD`; título 18 px negrita `#333`; a la derecha solo **Home** y **Change user** (power), sin (?) |
| Franja de búsqueda | 40 px, `#DDDDDD`; campo SFC **189×32 px fijo**, fondo blanco, borde 1 px `#BFBFBF` (`#007CBF` con foco) con ⧉ adentro; Shop Order y Status (SFC) en peso **normal** |
| Banner | Entre el header y la búsqueda, empuja el contenido; fila de 25 px sobre blanco, ícono 24×24, texto 12 px negro. **Queda hasta la siguiente acción** (no se oculta solo) |
| Franjas grises (acciones, panel derecho, pie) | `#BBBBBB` |
| Botón grande (acciones y pie) | **55 px** de alto, ancho según el texto, separación 8 px; fondo `#E2E2E2` con brillo blanco en el 2 % superior; borde 1 px `#A3A3A3`, radio 4.8 px, sin sombra; ícono **40×40** a la izquierda; texto 12 px `#222` |
| Botón del panel derecho | **133×70**, columna de 156 px; ícono 40×40 arriba |
| Botón activo / hover | Degradado `#66B1D9` (2 % superior) → `#007CBF`, borde `#00689E`, **texto blanco**. Aplica al lateral abierto, al de acción recién presionado y al hover de los botones de las listas |
| Encabezado de panel | 37 px, `#F2F2F2`, línea azul abajo; algunos paneles con **×** a la derecha (NC Selection, Component List, Data Collection List) |

### POD Work List

| Qué | Medida |
|---|---|
| Encabezado de columnas | `#E5E5E5`, 12 px `#333` |
| Fila | **26 px**; celdas 12 px negro; líneas 1 px `#DDDDDD` |
| Fila con datos | Fondo **`#EAEDF0`** (las vacías, blancas) |
| Fila seleccionada | **`#C0D8E4`** |
| Columna selectora | **18 px**, fondo `#F6F6F6`; seleccionada `#007CBF` |
| Al seleccionar | Llena el campo SFC, Shop Order y Status (SFC) |

## Listas del panel (Component List, Data Collection List, NC Selection)

| Qué | Medida |
|---|---|
| Fila | Fondo **blanco**, separador 1 px `#DDDDDD` (Component List 119 px; DC List 77 px) |
| Código | 16 px negrita negro |
| Cantidad / descripción | 16 px (NC: descripción en itálica) |
| Etiquetas (`Description:`, `Rem Qty:`, `Collect Data At:`) | 12 px `#666666`, valor 12 px negro |
| Botón de fila (Assemble, Disassemble, Collect) | **128×45**, ícono arriba; deshabilitado: borde `#BFBFBF`, texto `#999` |
| Botón "Log NC" de NC Selection | Chico: 74×21, `#E2E2E2`, borde `#888`, radio 4 px |
| Paginación de Component List y DC List | [First] [Prev] [Next] [Last] + "Rows 1-N of N", sobre `#F2F2F2` |
| Paginación de NC Selection | Links: `1 ‹ Back [1] 2 3 4 5 Forward › 7`; links `#00689E`, deshabilitados `#999`, página actual blanca sobre gris oscuro. **Las NC por página dependen del alto disponible** (4 → 2 al aparecer un banner) |

## Ventanas (modales)

| Qué | Medida |
|---|---|
| Ventana | Fondo `#F2F2F2`, radio 4 px; barra de título 12.8 px negrita `#333` con ⤢ y × |
| Encabezado interno | 37 px, línea azul abajo |
| Botón de ventana (Add, Done, Save, Clear, Retrieve, Return to POD, OK, Cancel) | **21 px** de alto, fondo **sólido** `#E2E2E2`, borde 1 px `#888`, radio 4 px, 12 px `#222`, ícono a la izquierda |
| Campo | **32 px** de alto, fondo blanco, borde 1 px `#BFBFBF` (`#007CBF` con foco) |
| Ventana apilada | Velo suave sobre la de abajo (ej. Data Collection Entry sobre Data Collection Standalone) |

**NC Data Entry (variante simple de esta workstation):** banda `SFC: <sfc> (1/1)` en 16 px negro;
etiquetas a la derecha; NC Code y Component con ⧉; Comments de 48 px; [Add] [Add-Done]
[Return to POD] [Done].

**Assemble Components:** franja superior con Mode: Choose, Action: Assembly/Removal (radios
deshabilitados), Find component y Barcode (≈207×32, foco en Barcode). Al escanear (aunque
falle) se despliegan Component Details (Component, Version, Description, Quantity) y
Assembly Data, y aparece [Add].

**SFC Split Plug-In (Auto ID):** banda `#3F5161` de 48 px con título blanco de 20.8 px;
formulario sobre `rgba(63,81,97,.1)` (≈ `#ECEEEF`); etiquetas 12.8 px negrita **negras**;
Qty to Split de 153×21, borde `#BFBFBF`, **amarillo `#FAFDE2` solo con foco** (abre con el foco
ahí); [Split] [Close] **blancos con texto y borde `#0854A0`**, 14 px, radio 4 px, 26 px de alto
(estilo UI5, distinto del resto).

**Data Collection Standalone (Line Clearance):** Filter Operation y Collect Data For =
Resource prellenados; [Clear] y [Retrieve] de 132×21. Filas `DC_LC_DONE` (16 px negrita) /
pregunta (16 px itálica) / DC Group/Vers. y fecha (16 px `#808080`), con [Collect] de
**184×45**. En testing Retrieve trajo **dos filas iguales**. El submodal Data Collection Entry
tiene la pregunta booleana con opciones **vacío, `NO`, `SI`**, `Data Type: Boolean`,
`0 files attached` y [Save] [Close]. Al guardar: "⚠ 1 DC Groups saved" (triángulo amarillo)
dentro de la ventana, que sigue abierta.

**Data Collection Entry de DC Collect (workstation `_ESIG`):** antes de los parámetros pide
**Data Collection Authentication** (Site, User ID prellenado, Password, [OK] [Cancel]). No se
pudo ver por dentro sin la contraseña.

**Menú Activities…:** se abre a la izquierda del botón (que se pinta de azul). Lista blanca,
borde 1 px `#BFBFBF`, **sin sombra ni radio**, ítems de 32 px con texto 12 px negro, 217 px
de ancho. **Todos los ítems se ven habilitados.** Ítems: As-Built Configuration, Log Comment,
SFC Place Hold, SFC Release Hold, Remove Future Hold, Change Equipment Status Tool, Time
Sensitive Material Check In/Out, Asynchronous Print Job Interface, Data Collection Standalone.

**Resource Setup:** no es parte del POD. Es una app de SAP MII
(`/XMII/CM/CUSTOM_GLOBAL/CustomResourceSetup/index.irpt`) que se abre con `window.open` en
una **ventana emergente aparte** (no medida; omitida por el usuario).

**Fin de sesión:** página de SAP NetWeaver "You have logged off" con [Log On Again].

---

## Mensajes del MES

Siempre en inglés, con número de mensaje. **Nunca son popups:** en el POD van en el banner (ícono
rojo "!") y a veces en una sección `Messages (n):` arriba del panel; dentro de una ventana, en su
propia franja con el mismo ícono.

| # | Texto exacto | Dónde | Cuándo |
|---|---|---|---|
| 22501 | `Line Clearance has not been done since the last Resource Setup, please perform Line Clearance` | Banner del POD | Assemble o DC Collect sin LC desde el último Resource Setup |
| 22506 | `SFC is missing` | Banner del POD | Start con el campo SFC vacío |
| 12906 | `SFC is missing` | Banner del POD | SFC Split sin SFC seleccionado |
| 22507 | `Invalid SFC` | Banner del POD | Start con un SFC que no existe |
| 15049 | `No valid SFCs meet the specified criteria` | `Messages (2):` del panel | Junto con 22507 |
| 13005 | `SFC <sfc> does not exist` | `Messages (2):` del panel | Junto con 22507 |
| 13875 | `Define Qty to Split` | SFC Split: reemplaza el formulario, con [OK] | Split con Qty vacío |
| 13885 | `Requested qty to split <n> is greater than the available SFC qty <m>` | SFC Split, ídem | Qty mayor a la disponible |
| 13213 | `Quantity must be greater than 0` | SFC Split, ídem | Qty 0 |
| 10015 | `Required value "<param>" missing; enter a value in the <param> field` | Franja de Data Collection Entry | Save sin responder un parámetro obligatorio |
| 17150 | `2D barcode is not compliant with ISO 15434 format` | Franja de Assemble Components | Barcode con formato inválido |

En SFC Split, el [OK] del mensaje **cierra la ventana entera**: hay que volver a abrir SFC Split.

**Confirmaciones y avisos que no son error:**

| Texto | Dónde |
|---|---|
| Ventana `Unsaved Data`: `Do you want to return to POD without saving record?` [OK] [Cancel] | Return to POD de Assemble Components con datos escritos |
| `⚠ 1 DC Groups saved` | Data Collection Standalone, tras guardar el LC |
