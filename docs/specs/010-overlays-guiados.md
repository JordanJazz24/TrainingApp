# Spec 010: Overlays guiados (Fase 6.6)

**Status:** Approved (2026-10-06)
**Date:** 2026-10-06
**Plan:** `docs/specs/010-plan.md`
**Origen:** `docs/roadmap.md` → Fase 6.6. Entrada: `docs/ui-reference/inventario-pantallas.md`.

> Las respuestas del usuario se citan como R1…R7 (rondas de preguntas) y R8…R9 (rondas de
> revisión), todas del 2026-10-06.

## Problem

En modo **Guided** hoy el operador:

- ve habilitado solo el botón del paso actual (`SetActiveButton`), pero nada le dice qué
  hacer **dentro** del paso: qué campo llenar, qué valor elegir, qué botón del modal tocar;
- recibe ayuda solo **después de fallar**: un SweetAlert con una línea por tipo de paso
  (`GuidedTest.GetHint`, en inglés).

**A quién le sirve** (R1): al operador nuevo (primer contacto con el SAP MES), al operador
de otra área que practica una nueva, al que se prepara para el Assessment y al que necesita
un refrescamiento. Como incluye al operador nuevo, el nivel de detalle es **clic por clic**.

**Éxito:** un operador sin experiencia completa una sesión Guided de punta a punta sin que
el instructor le diga qué tocar.

## Scope

### In

**La guía**
- **Guía visual sobre el POD, solo en Guided:** oscurece la pantalla, resalta el elemento
  que toca usar y muestra un globo con la instrucción. Recorre cada **subpaso de la UI** del
  paso actual, también dentro de los modales. El recorrido completo está en
  "Guion por tipo de paso", abajo.
- **Siempre activa en Guided y no se puede cerrar** (R1).
- **Bloqueante** (R1): mientras la guía está activa solo responde el elemento resaltado.
  **Abandon Session y Logout quedan siempre libres** (R2). Home y Help siguen sin función.
- **Globo** (R5): paso del flujo ("Paso 1 · Resource Setup"), contador de subpasos
  ("Subpaso 3 de 13"), la instrucción y "Continuar" en los subpasos de captura,
  verificación e informativos.
- **Globo final** (R6) sobre la pantalla "Session Complete": "Completó la guía. Cuando se
  sienta listo, intente el Assessment", con OK (redacción de usted: enmienda P-8 del plan,
  2026-10-06).

**Textos**
- **En español, con los nombres del MES tal como aparecen en pantalla** (R2):
  "Presione **Retrieve**", "Seleccione **Set Up** en Setup State".
- **Con la respuesta concreta** (R1): "Seleccione **Si**", "Seleccione **Set Up**"; en Data
  Collection, el rango del catálogo de cada parámetro.
- **Valor de ejemplo realista en los campos libres** (R6): un valor fijo por tipo de paso,
  en el código (ver el guion). **Excepción** (R7): los parámetros MSS del Start de Moldeo
  van sin valor ("Ingrese los parámetros según la MSS D000281359"); el operador aprende a
  consultar la MSS.
- **En el código** (R3): un guion por tipo de paso dentro de la app. Lo que varía por área
  (rangos de DC, códigos NC, BOM) sale de los catálogos que ya están en la BD. Mismo
  criterio que la pregunta de LC (D-6). Cambiar un texto pide un deploy.
- **Los hints de `GetHint` (SweetAlert tras un intento fallido en Guided) pasan al español**
  (R7), con el mismo criterio: los nombres del MES en inglés.

**Cómo avanza**
- **Por acción del operador:** un clic en el elemento resaltado, un cambio en un
  desplegable o "Continuar" (R3). Tipos de subpaso:
  - **clic / cambio:** avanza con la acción sobre el elemento;
  - **captura:** el operador escribe o escanea y presiona "Continuar";
  - **verificación** (R8): el operador revisa un valor que ya está en pantalla (ej. Setup
    State en Set Up), lo cambia si hace falta y presiona "Continuar";
  - **informativo** (R8): explica lo que se ve en pantalla (una sección, un mensaje de
    confirmación del MES) y se avanza con "Continuar", siempre habilitado.
- **"Continuar"** se habilita cuando los campos del subpaso cumplen el **formato** que ya
  exige el control (no vacío; numérico donde el control lo pide). Nunca revisa decisiones.
  Excepción: en el bloque de Data Collection "Continuar" siempre está habilitado, porque
  ahí un campo vacío es una decisión que cuenta como intento (`CLAUDE.md`).
- **Formularios de varios campos por bloque** (R7): los 10 parámetros MSS, las 3
  verificaciones del Start de Moldeo y cada grupo de DC se resaltan como un bloque, con
  una instrucción y un "Continuar". En DC el globo lista cada parámetro con su rango.
- **Enter** en un subpaso de captura equivale a "Continuar" y nunca envía el formulario.
  **El escaneo del barcode de Assembly avanza solo** (R6): el lector termina en Enter y el
  control ya muestra Add.
- **Repeticiones con el mismo detalle** (R5): LC de cierre, cada componente del BOM y cada
  grupo de DC llevan el mismo texto y resaltado cada vez.
- **Data Collection: solo los grupos obligatorios** (R4). Si el área no tiene obligatorios,
  el primero de la lista. Moldeo: solo `DCGTM_FECHA_EXPIRACION_NIR/A`.
- **Log NC: resalta toda la lista** (R6): "Elija el código del defecto; en el simulador
  cualquiera es válido". Ver Deferred Decisions.
- **Cada mensaje de confirmación del MES tiene su subpaso informativo** (R8, R9): "Record
  changed", "1 DC Groups saved", los banners del POD (Start, Assembly, grupo y pieza de
  DC, split, scrap) y las confirmaciones dentro de los modales (Add de Assembly, "New SFC …
  created"). El operador aprende a leer los mensajes del MES.
- **Los mensajes esperan a la guía** (R9): con la guía activa, un mensaje que hoy se
  oculta solo (a los 3-5 s) queda visible hasta que el operador presiona "Continuar" en su
  subpaso informativo. En Assessment se ocultan como hoy.

**Decisiones y errores**
- **Decisiones incorrectas** (R2, aclarado en R3, lectura A): el elemento resaltado acepta
  cualquier valor; la guía no sabe si es correcto. El valor se envía, el servidor lo
  califica como hoy (intento fallido, reintentos ilimitados en Guided) y el paso solo
  avanza cuando el servidor lo da por correcto.
- **Tras un intento fallido** (R2): se mantiene el SweetAlert con el hint (ahora en
  español). Al cerrarlo, la guía vuelve a resaltar el elemento o bloque de la decisión.

**Servidor y cliente**
- **El servidor manda:** el paso actual y la calificación siguen en la BLL. El JS solo
  conoce el guion y avanza al detectar la acción del operador; nunca guarda estado de
  negocio, nunca decide si algo está bien y nunca envía nada propio al servidor.
- **Reanudar tras cada postback o F5 según el servidor** (R4): el servidor informa el paso
  actual y su avance (componentes ensamblados, grupos de DC guardados, split hecho, filas
  a seleccionar) y la guía arranca en el primer subpaso pendiente. Sin `sessionStorage`.
- **Guion por tipo de paso, no por área**, ajustado con los flags del área
  (`SerializationType`, `RequiresStartChecklist`, `RequiresNCComponent`), para que LoFlo lo
  reciba sin trabajo.
- **Anclas propias `data-guide`** en los elementos, no ids (muchos botones no tienen id y
  los `runat="server"` salen con prefijo).
- **Cubre todas las pantallas del flujo**, incluidas las ⚪ del inventario (#8, los 4
  modales del Start de Moldeo; #16, el resultado de Complete), sobre la UI actual (R3).

### Out

- **Assessment:** sin guía (revelaría el camino).
- Cambios de reglas de negocio, puntaje, intentos, avance o habilitación de botones.
- Cambios a las pantallas del MES cerradas en la 6.5: solo se agregan anclas `data-guide`.
  Los únicos cambios visibles son los hints de `GetHint` en español (R7) y, con la guía
  activa, que los mensajes no se oculten solos (R9).
- Persistir algo de la guía (R4): tablas, columnas, métricas de uso o tiempos por subpaso.
- Cerrar, pausar o reabrir la guía; dar función a Home y Help (R1, R2).
- Grupos de DC opcionales, Save Draft, Comments de NC y botones "señuelo" del POD: la guía
  solo recorre el camino correcto.
- Tablets y pantallas táctiles (R5).
- Códigos NC sugeridos (ver Deferred Decisions).

## Guion por tipo de paso

Subpasos en orden. "Captura", "verificación" e "informativo" = el globo tiene
"Continuar". Los textos finales se escriben en el plan con este contenido. Las filas
marcadas con una condición solo aparecen si el área cumple esa condición.

### Resource Setup (paso 1)

| # | Elemento | Instrucción (contenido) | Avanza con |
|---|---|---|---|
| 1 | Botón **Resource Setup** (pie del POD) | Abrir Resource Setup para asociar la estación a la orden | clic (postback) |
| 2 | **Retrieve** | Cargar el recurso | clic |
| 3 | **REMOVE ALL** | Limpiar la tabla (quitar todas las filas) | clic |
| 4 | **INSERT NEW** | Agregar una fila | clic |
| 5 | Valor de la fila (tipo Shop Order) | Shop Order, ej. **303406950** | captura |
| 6 | **INSERT NEW** | Agregar otra fila | clic |
| 7 | Tipo de la fila nueva | Cambiar a **Tool Number** | cambio |
| 8 | Valor de la fila | Herramienta, ej. **10033058** | captura |
| 9 | Tabla de recursos | En esta sección se agregan todos los recursos, herramientas y materiales asociados a su operación | informativo |
| 10 | **Setup State** | Verificar que esté en **Set Up** (cambiarlo si no) | verificación |
| 11 | **Save** | Guardar | clic (postback, califica) |
| 12 | Mensaje "Record changed" | Se muestra un mensaje que indica que se aplicó el cambio | informativo |
| 13 | **X** del modal | Cerrar Resource Setup | clic |

Cambios del usuario en la revisión (R8): REMOVE ALL después de Retrieve; Setup State como
verificación justo antes de Save; el subpaso informativo de la tabla y el del mensaje
"Record changed".

### Line Clearance (pasos 2 y 6, igual en apertura y cierre)

| # | Elemento | Instrucción (contenido) | Avanza con |
|---|---|---|---|
| 1 | **Activities...** (panel derecho) | Abrir el menú | clic |
| 2 | **Data Collection Standalone** | Abrir la limpieza de línea | clic (postback) |
| 3 | **Retrieve** | Cargar los grupos | clic |
| 4 | **Collect** de `DC_LC_DONE` | Responder la limpieza de línea | clic |
| 5 | Respuesta | Seleccionar **Si** | cambio |
| 6 | **Save** | Guardar | clic (postback, califica) |
| 7 | Mensaje "1 DC Groups saved" | Verificar que aparezca el mensaje de confirmación | informativo |
| 8 | **X** del modal | Cerrar | clic |

Cambio del usuario en la revisión (R8): el subpaso informativo del mensaje de confirmación.

### Search/Start (paso 3)

| # | Elemento | Instrucción (contenido) | Avanza con |
|---|---|---|---|
| 1 | **Browse Work List** (⧉ junto a SFC) | Buscar la orden | clic |
| 2a | NonSerialized: la fila del SFC | Seleccionar el SFC **{sfc}** | clic |
| 2b | Serialized: ícono **Select All** | Seleccionar todas las piezas | clic |
| 3 | **OK** | Confirmar la selección | clic |
| 4 | **Start** | Iniciar la orden | clic (postback; sin checklist, califica) |
| 5 | Checklist: campo ID del molde | Ingresar el ID del molde según la MSS | captura |
| 6 | Checklist: **Save** (modal 1) | Guardar | clic |
| 7 | Checklist: bloque de 3 verificaciones | Responder **Sí** a las tres | captura (bloque) |
| 8 | Checklist: **Save** (modal 2) | Guardar | clic |
| 9 | Checklist: bloque de 10 parámetros MSS | Ingresar los parámetros según la MSS D000281359 | captura (bloque) |
| 10 | Checklist: **Save** (modal 3) | Guardar | clic |
| 11 | Checklist: verificación final | Seleccionar **Sí** | cambio |
| 12 | Checklist: **Save** (modal 4) | Guardar | clic (postback, califica) |
| 13 | Banner "Start process completed successfully" | Verificar el mensaje de confirmación | informativo |

"Checklist" = solo si `RequiresStartChecklist` (hoy Moldeo). El subpaso 13 sigue al que
califica (4 sin checklist, 12 con checklist).

### Assembly (paso 4, si `RequiresAssembly`)

| # | Elemento | Instrucción (contenido) | Avanza con |
|---|---|---|---|
| 1 | Filas del POD (si no están seleccionadas) | Seleccionar todas las piezas o el SFC | clic |
| 2 | **Assemble** (POD) | Abrir la Component List | clic (postback) |
| 3 | **Assemble** de la fila del componente pendiente | Ensamblar **{número de parte}** | clic |
| 4 | **Barcode** | Escanear el componente, ej. **10087734** | escaneo (Enter) o captura |
| 5 | **Add** | Agregar | clic (postback) |
| 6 | Confirmación del Add (en el modal) | Verificar el mensaje de confirmación | informativo |
| 7 | **X** del modal | Cerrar para seguir con el siguiente componente | clic |
| 8 | Banner "All components processed at this operation" | Verificar el mensaje de confirmación | informativo |

Los subpasos 3-7 se repiten por cada componente pendiente del BOM, con el mismo detalle.
Con el último Add el servidor completa el paso y vuelve el POD: no hay 6 ni 7, sigue el 8.

### Data Collection (paso 5, si `RequiresDataCollection`)

| # | Elemento | Instrucción (contenido) | Avanza con |
|---|---|---|---|
| 1 | Filas del POD (si no están seleccionadas) | Seleccionar todas las piezas o el SFC | clic |
| 2 | **DC Collect** (POD) | Abrir la Data Collection List | clic (postback) |
| 3 | **Collect** del grupo obligatorio pendiente | Capturar **{grupo}** | clic |
| 4 | Bloque de parámetros | Lista de parámetros: Numeric "entre {min} y {max} {unidad}", Boolean "**Sí**", Date "fecha de expiración, ej. **{hoy + 1 año}**" | captura (bloque) |
| 5 | **Save** | Guardar | clic (postback, califica) |
| 6 | Banner del grupo o de la pieza | Verificar el mensaje: "All data for DC group … has been collected" o, al terminar una pieza, "SFC … completed. Next: …" | informativo |

Los subpasos 3-6 se repiten por cada grupo obligatorio pendiente y, en Serialized, por
cada pieza (el servidor lleva la pieza actual). Con 3-5 piezas, el recorrido es piezas ×
grupos obligatorios, con el mismo detalle (R5). El último Save completa el paso sin
mensaje: no hay subpaso 6.

### SFC Split (paso 7, solo NonSerialized)

| # | Elemento | Instrucción (contenido) | Avanza con |
|---|---|---|---|
| 1 | Fila del lote en el POD | Seleccionar el SFC **{sfc}** | clic |
| 2 | **SFC Split** (panel derecho) | Abrir SFC Split | clic (postback) |
| 3 | **Qty to Split** | Cantidad, ej. **100** | captura |
| 4 | **Split** | Dividir | clic (postback) |
| 5 | Mensaje "New SFC … created with quantity …" | Verificar el mensaje de confirmación | informativo |
| 6 | **Close** | Cerrar | clic (postback, califica) |
| 7 | Banner "SFC Split completed successfully" | Verificar el mensaje de confirmación | informativo |

### Log NC (paso 8)

| # | Elemento | Instrucción (contenido) | Avanza con |
|---|---|---|---|
| 1 | Fila a scrapear en el POD | NonSerialized: la fila creada por el split; Serialized: una pieza | clic |
| 2 | **Log NC** (panel derecho) | Abrir la NC Selection | clic (postback) |
| 3 | Toda la lista NC Selection | Elegir el código del defecto; cualquiera es válido | clic en un **Log NC** (postback) |
| 4 | **Component** (si `RequiresNCComponent`) | Componente, ej. **10087734** | captura |
| 5 | **Add-Done** | Registrar el NC | clic (postback, califica) |
| 6 | Banner "… scrapped" | Verificar el mensaje de confirmación | informativo |

### Complete (paso 9)

| # | Elemento | Instrucción (contenido) | Avanza con |
|---|---|---|---|
| 1 | Filas del POD | Serialized: todas las piezas (**Select All**); NonSerialized: el SFC **{sfc}** | clic |
| 2 | **Complete** (POD) | Cerrar la orden | clic (postback, califica) |
| 3 | Pantalla "Session Complete" | Globo final (R6) | OK |

## Domain Model

**Sin cambios de esquema ni de Domain** (R4): la guía no guarda nada en la BD. Los
intentos por paso que ya registra Guided siguen siendo el dato para el instructor.

**Guion:** subpasos por tipo de paso, cada uno con el ancla `data-guide`, el contenido de
la instrucción, el modo de avance (clic, cambio, captura, escaneo, verificación,
informativo) y la condición de flag si aplica. Vive en el código de la capa de presentación (es UI, no regla de negocio).

**Estado de la guía:** lo arma el servidor en cada request, solo en Guided, a partir del
`PodContext` que ya se carga una vez por request:
- tipo de paso actual, número de paso y total de la guía;
- flags del área: `SerializationType`, `RequiresStartChecklist`, `RequiresNCComponent`;
- avance del paso: componentes del BOM pendientes, grupos de DC obligatorios pendientes,
  split hecho o no, y qué filas del POD hay que seleccionar (según `PieceSelectionRule` y
  la orden simulada);
- datos de catálogo para los textos: parámetros de cada grupo pendiente (nombre, tipo,
  min, max, unidad), números de parte del BOM pendiente;
- si la sesión está completada (globo final).

No hay reglas nuevas: el estado solo expone lo que la BLL ya sabe.

## API Contract

No hay endpoints nuevos (ni `.ashx` ni `[WebMethod]`): la guía solo escucha y la página
sigue hablando con el servidor por los postbacks de siempre.

| Dirección | Qué | Cuándo |
|---|---|---|
| Servidor → página | Estado de la guía (JSON con `JavaScriptSerializer`) y guion con textos | Cada respuesta de `GuidedTest` en Guided |
| Servidor → página | Motor de la guía (JS sin textos ni respuestas) | Solo en Guided |
| Página → servidor | Nada propio de la guía | — |

Los textos que llevan datos de la BD (nombres de grupos y parámetros, unidades) se
codifican al pasar al HTML (convención de seguridad en presentación).

## Authorization

- **Assessment no recibe nada de la guía** (R4): ni el motor, ni el guion, ni los textos,
  ni el estado. Ver el código de la página en Assessment no revela respuestas. Por eso el
  guion con textos y valores lo emite el servidor en la página (solo en Guided) y no vive
  en un `.js` estático que cualquiera podría pedir por URL.
- El estado sale del `PodContext`, que `PodService.LoadContext` solo devuelve para una
  sesión propia y en curso. No hay nada nuevo que autorizar.
- En Guided la guía puede llevar respuestas (R1): es práctica.

## Edge Cases & Failure Modes

- **Elemento no encontrado** (R5, falla segura): si el elemento del subpaso no aparece en
  unos segundos (bug, modal que tarda), la guía quita el oscurecido y muestra la
  instrucción en un globo fijo, sin resaltar ni bloquear. Vuelve a bloquear en cuanto
  encuentra el elemento del subpaso siguiente. El operador nunca queda atrapado.
- **Bloqueo con mouse y teclado:** ni un clic ni Tab + Enter activan algo fuera del
  elemento resaltado (salvo Abandon y Logout).
- **SweetAlerts por encima de la guía:** los de formato, los de intento y la confirmación
  de Abandon se ven y se pueden cerrar. Al cerrar uno de formato, la guía vuelve al
  subpaso del dato que lo causó (si no se sabe, al primer subpaso de captura del modal
  abierto). Al cerrar uno de intento, al elemento de la decisión (R2).
- **Modales apilados:** el submodal de LC sobre Data Collection Standalone, los 4 modales
  del checklist en secuencia y el foco de los modales de Bootstrap: el globo y su
  "Continuar" quedan encima del modal de más arriba y se pueden usar.
- **Mensajes que se ocultan solos** (R9): con la guía activa esperan a su subpaso
  informativo. Si un mensaje no aparece (ej. el último Save de DC no muestra banner), la
  guía no lo espera: ese subpaso no existe en ese estado.
- **Animaciones y reubicación:** la guía espera a que el modal termine de abrirse y
  reubica el resaltado al redimensionar o desplazar la ventana.
- **Postback o F5 a mitad de un paso:** la guía arranca en el primer subpaso pendiente
  según el servidor (R4). Si el modal se cerró con el F5, empieza por el botón que lo abre.
- **Botón Atrás:** riesgo ya aceptado (roadmap); la guía sigue lo que el servidor pinte.
- **Session reciclada** (D-10): la guía sigue el estado reconstruido por la BLL.
- **Sin casos de Assessment:** el avance forzado y los clics fuera de secuencia no ocurren
  en Guided (reintentos ilimitados y botones de otros pasos deshabilitados).
- **Varias pestañas:** cada carga sigue al servidor; no hay estado del cliente que
  desincronizar.

## Non-Functional Requirements

- **Equipos** (R5): PC con Chrome o Edge, mouse y teclado, monitor de escritorio, y
  **lector de código de barras** conectado (termina cada escaneo con Enter).
- **Sin CDN:** cualquier librería nueva se sirve desde `~/Scripts`, como jQuery, Bootstrap
  y SweetAlert2. JS propio o librería local: lo decide el plan.
- **Sin conexiones extra:** el estado de la guía sale de los catálogos que el `PodContext`
  ya carga una vez por request; la guía no agrega consultas por subpaso.
- **Sin cambios visibles en Assessment:** misma página, mismo comportamiento.

## Integrations

Ninguna externa. Internas: Bootstrap 5 (modales, dropdown de Activities), SweetAlert2,
jQuery 3.7 (ya cargados por `Training.Master`) y los controles de paso, que reciben anclas
`data-guide` sin cambiar su comportamiento.

## Acceptance Criteria

Áreas de prueba: **Moldeo** (NonSerialized, checklist de Start, DC, sin Assembly, sin
Component en NC) y **Redefinity** (Serialized, Assembly, DC, datos de ejemplo). Entre las
dos cubren todos los flags.

**Presencia y bloqueo**
- **AC-1:** Given una sesión Guided en curso, When carga el POD, Then la guía resalta el
  primer subpaso pendiente del paso actual, con el globo (paso, contador, instrucción).
- **AC-2:** Given una sesión Assessment, When carga el POD o se mira el código de la
  página, Then no hay guía, ni motor, ni guion, ni textos, ni estado de la guía.
- **AC-3:** Given la guía activa, When el operador hace clic o usa Tab + Enter sobre otro
  elemento, Then no pasa nada; When usa **Abandon Session** o **Logout**, Then funcionan.
- **AC-4:** Given la guía activa, Then no existe forma de cerrarla.

**Recorrido por tipo de paso** (cada uno con el guion de arriba)
- **AC-5:** Resource Setup completo: Retrieve → REMOVE ALL → filas con los valores de
  ejemplo → informativo de la tabla → verificación de Setup State → Save → informativo de
  "Record changed" → X; luego la guía pasa a Line Clearance.
- **AC-6:** Line Clearance de apertura y de cierre con el mismo recorrido, incluido el
  informativo del mensaje "1 DC Groups saved".
- **AC-7:** Start en Moldeo: Browse → fila del SFC → OK → Start → los 4 modales del
  checklist por bloque (ID del molde, 3 verificaciones, 10 MSS sin valor, final).
- **AC-8:** Start en Redefinity: Browse → **Select All** → OK → Start, sin checklist.
- **AC-9:** Assembly en Redefinity: por cada componente del BOM, Assemble → escaneo (el
  escaneo con Enter avanza solo) → Add → X del modal; tras el último Add, el paso se
  completa y vuelve el POD.
- **AC-10:** Data Collection: la guía pasa solo por los grupos obligatorios (Moldeo: solo
  `DCGTM_FECHA_EXPIRACION_NIR/A`), en Redefinity por cada pieza, y el globo del bloque
  lista cada parámetro con su rango.
- **AC-11:** SFC Split en Moldeo: fila del lote → SFC Split → 100 → Split → Close.
- **AC-12:** Log NC: toda la lista resaltada; en Redefinity pide Component, en Moldeo no;
  Add-Done registra el NC.
- **AC-13:** Complete: Serialized con Select All, NonSerialized con el SFC; luego el globo
  final sobre "Session Complete", que se cierra con OK.

**Errores y reanudación**
- **AC-14:** Given Resource Setup guiado, When el operador elige **Open** y guarda, Then se
  registra el intento fallido, aparece el SweetAlert con el hint **en español** y, al
  cerrarlo, la guía resalta Setup State; el paso solo avanza al guardar con Set Up.
- **AC-15:** Given un subpaso de captura, When el campo está vacío o no cumple el formato
  del control, Then "Continuar" está deshabilitado (salvo en el bloque de DC); When el
  operador presiona Enter, Then equivale a "Continuar" y el formulario no se envía.
- **AC-16:** Given cualquier paso a medio camino (ej. Assembly con un componente hecho),
  When el operador presiona F5, Then la guía retoma en el primer subpaso pendiente según el
  servidor (el componente siguiente).
- **AC-17:** Given un subpaso cuyo elemento no aparece, When pasan unos segundos, Then la
  guía deja de bloquear y muestra la instrucción en un globo fijo; el operador puede seguir.

**Sin regresiones**
- **AC-18:** Given una sesión Guided recorrida con la guía, Then los intentos guardados,
  el puntaje y la secuencia son los mismos que sin guía para las mismas acciones; la guía
  no guarda nada en la BD.
- **AC-19:** Given los hints de Guided, Then están en español (nombres del MES en inglés).

**Mensajes del MES**
- **AC-20:** Given la guía activa, When el MES muestra un mensaje de confirmación (banner
  del POD o mensaje dentro de un modal), Then la guía lo resalta en un subpaso informativo y
  el mensaje sigue visible hasta que el operador presiona "Continuar".
- **AC-21:** Given una sesión Assessment, Then los mensajes se ocultan solos como hoy.

## Deferred Decisions

| Decisión | Fallback elegido | Se revisa cuando |
|---|---|---|
| Códigos NC sugeridos en Log NC (R6) | La guía resalta toda la lista: cualquiera es válido | El usuario confirme con negocio los códigos más comunes |
| Pantallas ⚪ #8 y #16 (R3) | Guion sobre la UI actual | Llegue la captura del MES real y la pantalla cambie |

## Open Questions

_Las 6 lecturas de la primera revisión quedaron confirmadas por el usuario (R8): "Continuar"
valida el formato (salvo el bloque de DC), ID del molde sin valor, valores de ejemplo,
guion emitido por el servidor, bloqueo con teclado y criterio de éxito._

_Las de la segunda revisión quedaron resueltas (R9): los mensajes esperan a la guía, el
subpaso informativo va en todos los mensajes de confirmación, la nota cortada de REMOVE ALL
decía "el resto sigue igual" y el resto del guion no tiene cambios._

Ninguna abierta.
