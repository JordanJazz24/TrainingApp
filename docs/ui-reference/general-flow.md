# UI Reference — General Training Flow

Documento acumulativo. Flujo común a todas las áreas. Se actualiza pantalla por pantalla conforme se analiza el SAP MES real.
Fuente de verdad de **cómo se ve y se opera** cada pantalla. Las reglas de negocio y de
puntaje (pesos, qué cuenta como intento, modos) están en `docs/domain.md`; aquí no se copian.

> Documentos por área (`moldeo-flow.md`, etc.) solo registrarán lo que difiere de este flujo general.
>
> Procedimiento oficial del MES real (WI QD001001524) y su contraste con cada paso:
> `wi-produccion-disposables.md` (hallazgos W1-W23).

---

## Dashboard Operacional (POD)

### Requisitos definitivos

> **Layout y colores del MES real** (capturas `pantall central de referencia.png`,
> `mensaje de banner correcto.png` y `pod cargado con la orden selcionada.png`; Fase 6.5,
> entrega 3, 2026-09-29). Bloques en el flujo de la página, de arriba abajo:
>
> | Bloque | Color | Contenido |
> |---|---|---|
> | Header | `#DDDDDD`, título negrita oscuro | Título (con el **Code1** del empleado, no su nombre) · Score del simulador · [Home] [Logout] [Help] |
> | Banner (temporal) | blanco, ícono ✔ verde | Mensaje del MES; desaparece solo |
> | POD Search | `#DDDDDD`, igual que el header | `* operación` `* recurso` (azules, asterisco rojo) · SFC con ícono de browse dentro · Shop Order / Status (SFC) a la derecha |
> | Acciones | gris `#BBBBBB` | 4 botones grandes centrados |
> | POD Work List + panel derecho | panel `#F2F2F2` con línea azul; panel derecho gris `#BBBBBB` | Tabla con encabezado `#E5E5E5`; botones del panel con ícono arriba y texto abajo |
> | Pie | gris `#BBBBBB` | [Abandon Session] a la izquierda · [Resource Setup] [Tool List] centrados |
>
> **Header y POD Search** forman una sola franja clara, como la captura de referencia
> (corrección del usuario, 2026-10-01); **acciones, panel derecho y pie** forman la
> sección gris continua. **Score** y **Abandon** no existen en el MES real: Score va compacto en el
> header y Abandon a la izquierda del pie. **Home** y **Help** son solo visuales (no hacen
> nada ni cuentan como intento).

**Header bar**
```
Production Operator Dashboard: Site - {site}, User - {code1}    Score: {n}% [modo]  [Home] [Logout] [Help]
```

**POD Search**
```
* {operationName}   * {resourceName}   SFC: [input readonly ⧉]          Shop Order: {orden}   Status (SFC): {estado}
```

**Action buttons — una sola fila, 4 botones centrados** (ícono grande + texto)
```
[Start]  [Assemble]  [DC Collect]  [Complete]
```

**Tabla POD — columnas**

| Column | Notes |
|---|---|
| (selector) | Columna angosta sin datos; en el header, el ícono **Select All** (cuadros apilados) |
| SFC | SFC identifier (el legacy la llamaba "CP"; corregido 2026-09-29) |
| Material Description | |
| Qty | Cantidad |
| Status | Solo el ícono de color, sin texto (verde = Active), como el real |
| Process Lot | Lote proc. |
| Shop Order | Orden de fabricación |

Cuadrícula como el real: las líneas de fila (cada 32px) y la línea de la columna selectora
siguen hasta el final del panel aunque la tabla esté vacía (fondo del panel, sin filas
falsas). Fila seleccionada: fondo celeste y celda selectora azul, como el real. Un click selecciona una fila (Serialized:
alterna la fila, multi-selección). **Select All** (captura del POD real del usuario,
2026-09-29) selecciona todas las filas y, si ya estaban todas, las deselecciona; aplica en
Serialized y en NonSerialized. En NonSerialized, con más de una fila seleccionada, cada
paso de una fila aplica su regla de siempre: DC Collection y SFC Split muestran un aviso
de formato (sin intento); Log NC y Complete cuentan como intento fallido (spec 007: elegir
la fila es decisión del operador).

**Sidebar — orden definitivo** (textos como el real)
1. SFC Split
2. Log NC
3. Work Instruction
4. Reports... (triángulo de submenú)
5. Activities... (triángulo de submenú)
6. Place back in queue

Estilo de botones: tarjetas cuadradas grandes con ícono centrado + label debajo.

**Footer (fondo)**
```
[Abandon Session]                 [Resource Setup]  [Tool List]
```

**Activities — submenu completo** (orden real, sin divisor; Data Collection Standalone al final)
1. As-Built Configuration
2. Log Comment
3. SFC Place Hold
4. SFC Release Hold
5. Remove Future Hold
6. Change Equipment Status Tool
7. Time Sensitive Material Check In/Out
8. Data Collection Standalone

### Modales — header estándar (Fase 6.5, entrega 4, 2026-09-29)

Todas las ventanas del MES llevan la misma **barra de título**: `[Título]  [↗] [X]` sobre
gris claro. [↗] maximiza o restaura el modal (estado de cliente); [X] hace lo mismo que el
botón de salida de cada modal (Cancel, Close o volver a NC Selection). Debajo va el
**encabezado interno** que muestra cada captura real:

| Modal | Encabezado interno |
|---|---|
| Resource Setup | Banda oscura `#3F5161` con "Resource Setup" grande y (?) |
| SFC Split Plug-In (Auto ID) | Banda celeste en cursiva "SFC Split" |
| Assemble Components | Subencabezado "Assemble Components" con (?) y línea azul |
| Data Collection Entry (DC) | Subencabezado `SFC: {pieza} (n/N)` con (?) + fila `DC Group: …` |
| Data Collection Entry (LC y los 4 del Start de Moldeo) | Subencabezado "Data Collection Entry" con (?) + fila `DC Group: … Resource: …` |
| NC Data Entry | Subencabezado `SFC: …` con (?) |
| Data Collection Standalone, Browse Work List | Solo la barra de título |

**Mensajes del MES real** (trainer de Wires, 2026-10-13, sin captura): los errores aparecen
en un **recuadro rojo arriba**; las confirmaciones duran unos **5 segundos**.

---

## Paso 1 — Resource Setup

**StepType:** `ResourceSetup` | **Validación:** secuencia + Setup State (contenido libre)

**Acceso:** Footer → [Resource Setup]

### Modal: Resource Setup

**Chrome:** ventana popup con [↗ expand] [X close] en esquina superior derecha.

**Estado 1 — antes de Retrieve**
```
[Resource Setup]                                      [↗][X]
══════════════════════════════════════════════════════════
  Resource Setup                                         [?]
────────────────────────────────────────────────────────
  * Site:         {site}
  * Resource:     {resourceName}  [copy]  [Retrieve]
    Setup State:  Open   ← texto plano, NO editable
────────────────────────────────────────────────────────
[INSERT NEW] [INSERT BEFORE] [INSERT AFTER] [REMOVE SELECTED] [REMOVE ALL]
  ← todos deshabilitados
────────────────────────────────────────────────────────
  Setup Type                    Setup Type Value
  Shop Order                    {precargada}     ← equipos del turno anterior (spec 015)
  Tool Number                   {precargada}
  Tool Number                   {precargada}
────────────────────────────────────────────────────────
                    [Save]  [Clear]
```

**Estado 2 — después de Retrieve**
- Setup State cambia a dropdown editable con "Set Up" seleccionado
- Toolbar habilitado
- La tabla conserva las filas precargadas; Retrieve no agrega filas (enmienda de la spec 015)

**Estructura de cada fila:**
```
[Setup Type ▼]          [valor texto libre             ⧉]
```
Setup Type opciones: `Shop Order`, `Tool Number`, `Material` (spec 017, D-94: el MES real lo
ofrece y la evaluación práctica lo califica, EP-4b)

- **⧉ copia el valor al portapapeles** para pegarlo en otra fila (confirmado por el
  usuario, 2026-09-29). No hay botón de borrar por fila: se borra con REMOVE SELECTED
  (fila seleccionada con un click) o REMOVE ALL.
- **Layout real** (captura `resource setup modal con datos`, Fase 6.5, entrega 5): campos
  generales centrados con la etiqueta a la derecha sobre gris claro; franja celeste con las
  acciones juntas a la izquierda; tabla de dos columnas de ancho fijo (Setup Type 34%) con
  encabezados centrados, que no se mueven al agregar filas; [Save] [Clear] de contorno
  celeste, centrados en el pie.

**Estado 3 — después de Save**
- Mensaje `☑ Record changed` (ícono de check verde + texto) sobre el gris, justo debajo de
  la banda oscura (captura `Resource Setup/record changed correcto`)
- Modal permanece abierto — operador cierra con X manualmente

### Flujo del operador
1. [Resource Setup] desde footer
2. Retrieve
3. Verificar Setup State = "Set Up" (cambiarlo si dice "Open")
4. REMOVE ALL — limpiar tabla
5. INSERT NEW — agregar filas necesarias
6. Primera fila: Setup Type = **Shop Order**, valor = número de orden (escaneo libre)
7. Filas siguientes: Setup Type = **Tool Number**, valor = número de herramienta (escaneo libre)
8. Save
9. Confirmar el mensaje "Record changed"
10. Cerrar con X

### Reglas de la pantalla
- **Calificación** (Setup State, equipos precargados, secuencia): `docs/domain.md` → "Flujo
  estándar" (paso 1) y "Qué cuenta como intento". Aquí, solo lo que valida el control.
- **Contenido libre** — no se valida Shop Order ni Tool Numbers contra catálogo
- **El orden de las filas es libre** — no se exige que Shop Order sea la primera fila
- **Exactamente 1 Shop Order** — ni 0 ni 2+
- **Al menos 1 herramienta: Tool Number o Material** (spec 017, D-99: los dos valen; la guía
  enseña Tool Number)
- **Mínimo 2 filas en total** (se cumple implícitamente con 1 SO + ≥1 herramienta)

**Procedimiento real (trainer de Wires, 2026-10-13):** al inicio del turno y en cada cambio
de lote, el operador hace **Remove All** de los equipos que quedaron cargados (del turno o
lote anterior) y escanea los que están físicamente en la estación. Error frecuente: asumir
que los equipos cargados son los de la estación y trabajar con una herramienta equivocada.
Sin captura del Remove All.

**Equipos precargados (spec 015, 2026-10-14):** el modal abre con una Shop Order y 2
herramientas precargadas, distintas de `303406950`. Con alguna en la tabla, el control no
muestra los avisos de formato de abajo (D-26). En Guided, la captura de la Shop Order y de la
herramienta no acepta un valor precargado (D-25). Cómo se califica guardar con alguno:
`docs/domain.md` → "Qué cuenta como intento" → "Precargado antes que formato".

### Validaciones del UserControl (lado cliente + servidor)

**Precondición de Save:**
- `[Save]` arranca **deshabilitado** — se habilita únicamente después de ejecutar `[Retrieve]`
- Retrieve sin ninguna fila adicional NO completa el paso

**Validaciones de formato al presionar Save** (en el cliente, se detiene en la primera que
falle; no cuentan como intento). El Setup State **no** se valida aquí: se envía y lo califica
el servidor.

| # | Validación | Mensaje al operador |
|---|---|---|
| 1 | Al menos 1 fila existe | "No existen recursos creados. Agrega al menos un Shop Order y una herramienta (Tool Number o Material)." |
| 2 | Total filas ≥ 2 | "Se requieren mínimo 2 recursos: 1 Shop Order y al menos 1 herramienta (Tool Number o Material)." |
| 3 | Exactamente 1 Shop Order (0 filas SO) | "Debe existir exactamente 1 fila de tipo Shop Order." |
| 4 | Exactamente 1 Shop Order (>1 filas SO) | "Solo puede existir 1 fila de tipo Shop Order." |
| 5 | Shop Order numérico (semilla de la orden simulada, spec 005) | "El valor de Shop Order debe ser numérico." |
| 6 | Al menos 1 herramienta (Tool Number o Material) | "Debe existir al menos 1 fila de tipo Tool Number o Material." |
| 7 | Todas las filas con valor no vacío | "Todas las filas deben tener un valor. Ninguna fila puede estar vacía." |
| 8 | Sin `<` ni `>` (ASP.NET rechaza el postback con marcado, AUD-11) | aviso de formato |

- Cada fallo muestra un **SweetAlert** (warning) y mantiene el modal abierto
- Un fallo NO marca el paso como completado ni cierra el modal
- Si todas pasan → Save envía el paso al servidor

**Comportamiento post-Save exitoso (BUG UI-1 corregido):**
- El modal permanece abierto
- Aparece `☑ Record changed` en la parte superior del modal body, debajo de la banda
- Después de 5 segundos el mensaje se oculta automáticamente
- El operador cierra el modal con [X] cuando quiera
- El estado del paso ya avanzó en el servidor al momento del Save

**Comportamiento de overlay (BUG UI-2 corregido):**
- Al abrir Resource Setup el POD (tabla de órdenes) permanece visible detrás del modal
- El modal se renderiza como overlay, NO reemplaza la vista del POD
- Al cerrar con [X] el operador regresa exactamente al estado anterior — las órdenes cargadas siguen visibles

### Diferencias SAP real vs. Legacy
| Elemento | SAP real | Legacy |
|---|---|---|
| Setup State antes de Retrieve | Texto plano `Open` | Dropdown desde el inicio |
| Toolbar labels | REMOVE SELECTED / REMOVE ALL | DELETE SELECTED / DELETE ALL |
| Footer botones | Save · Clear | Save · Cancel |
| Record changed | Ícono de check verde + "Record changed" in-modal, modal queda abierto | Texto flotante `position-fixed` |
| Orden de filas | Libre — no requiere Shop Order primero | No validado |
| Save deshabilitado | Sí, hasta Retrieve | No |

---

## Paso 2 — Line Clearance

**StepType:** `LineClearance` | **Validación:** respuesta = "Sí" (true)

**Acceso:** Sidebar → [Activities ▼] → "Data Collection Standalone"

**Instancias en el flujo general:**
- Paso 2: LC genérica de línea (esta — una pregunta boolean)
- Paso 6 (Cierre): LC genérica de línea (misma estructura que Paso 2)

> Algunas áreas agregan instancias adicionales dentro de otros pasos (ej. LC Moldeo dentro del Start). Esas variantes van en el doc de área correspondiente.

### Jerarquía de navegación

```
Sidebar [Activities ▼]
  └─ Data Collection Standalone  → modal principal (modalDCStandalone)
                                      └─ [Collect]  → sub-modal (modalDCEntry)
                                                          └─ [Save] → cierra sub-modal
                                                                       banner en modal principal 5s
                                                                       operador cierra con [X]
```

**Activities** es un dropdown Bootstrap en el sidebar. En Fase 6 solo "Data Collection Standalone" está habilitado; el resto de las 8 opciones están deshabilitadas.

---

### Modal principal: Data Collection Standalone

**Fuente:** captura `Line Clearence/modal data collection standalone` (Fase 6.5, entrega 6).

**Chrome:** barra superior **solo con [⤢][✕]** (sin título); debajo, encabezado blanco
"Data Collection Standalone" con (?) (sin ✕, corrección del usuario) y **línea celeste** `#007DC5`.

**Estado 1 — antes de Retrieve**
```
                                                          [⤢][✕]
  Data Collection Standalone                              (?)
══════════════════════════════════════════════════════ (celeste)
        Filter Operation: [{operationName}      ⧉]                         [🧽 Clear]
    * Collect Data For:   [Resource ▼]  [{resourceName} ⧉]                 [↻ Retrieve]
```

**Estado 2 — después de Retrieve**
```
  ┌──────────────────────────────────────────────────────────────────────────────┐
  │ DC_LC_DONE                        DC Group/Vers.: DCGTM_LC/A      [  ⇥       ] │
  │ Realizó la Limpieza de Linea?     {fecha del último registro}    [ Collect  ] │
  └──────────────────────────────────────────────────────────────────────────────┘
```
(pregunta en cursiva; [Collect] grande, ícono arriba del texto)

**Estado 3 — después de Save (regreso desde sub-modal)**
```
  ⚠ 1 DC Groups saved   ← mensaje del real, top del modal, desaparece a los 5s
  DC_LC_DONE      DC Group/Vers.: DCGTM_LC/A     {fecha actualizada}    [Collect]
```

- El modal principal **permanece abierto** durante toda la interacción
- "1 DC Groups saved" se muestra al regresar del sub-modal tras un Save exitoso
- **[Clear]** limpia el formulario: oculta la fila hasta volver a hacer Retrieve (antes
  cerraba el modal)
- El operador cierra el modal principal con [✕] cuando quiera

---

### Sub-modal: Data Collection Entry

Se abre **sobre** el modal principal (capas apiladas): el principal queda detrás con un
**velo suave** (~20 %) y el sub-modal, más angosto y un poco más abajo, tiene el foco
(captura `Line Clearence/modal line clearence- sub modal de la pregunta`). Lo resuelve
el manejo genérico de modales apilados de `Training.Master`.

```
[Data Collection Entry]                               [⤢][✕]
  Data Collection Entry                                  (?)
══════════════════════════════════════════════════ (celeste)
  DC Group: DCGTM_LC/A    Resource: {resourceName}   ← fila gris
────────────────────────────────────────────────────────
  * La limpieza de linea se ejecuto?:
    [ Seleccione          ▼ ]      Data Type: Boolean    [0 files attached]
────────────────────────────────────────────────────────
               [💾 Save]  [Cancel]
```

- [Cancel] cierra el sub-modal y regresa al modal principal sin cambiar estado
- [Save] envía la respuesta al servidor (ver validaciones abajo)

---

### Flujo del operador
1. Sidebar → [Activities ▼] → "Data Collection Standalone"
2. Modal principal abre — tabla vacía
3. [Retrieve] — aparece la fila DC_LC_DONE con [Collect]
4. Clic en [Collect]
5. Sub-modal abre sobre el modal principal
6. Seleccionar "Si" en el dropdown
7. [Save]
8. Sub-modal se cierra — regresa al modal principal
9. Confirmar el mensaje "1 DC Groups saved" (desaparece en 5s)
10. Cerrar modal principal con [X]

---

### Validaciones del UserControl

**Validación en el sub-modal al presionar Save:**

| # | Validación | Dónde | Mensaje al operador |
|---|---|---|---|
| 1 | Dropdown seleccionado (no en blanco) | cliente (formato, sin intento) | "Debes seleccionar una respuesta antes de guardar." |
| 2 | Respuesta == "Si" | servidor (`StepValidator`, decisión) | mensaje de respuesta incorrecta; en Assessment, sin revelar la respuesta |

- El blanco muestra un **SweetAlert** (warning) y el sub-modal permanece abierto
- Si la respuesta es "No": se envía, el intento se registra como fallido (penalización en
  Assessment) y el paso no avanza; el operador corrige y reintenta. En Guided la guía visual
  no deja enviar el "No" (spec 010, R10)

**Comportamiento post-Save exitoso:**
- Sub-modal se cierra (JS)
- Modal principal queda visible con "1 DC Groups saved" y la fecha del registro en la fila
- Banner desaparece automáticamente a los 5s
- Score actualizado en el header del POD
- El operador cierra el modal principal con [X] cuando quiera

---

### Reglas de la pantalla
- **Calificación** (Sí/No es una decisión): `docs/domain.md` → "Qué cuenta como intento"
- Dejar dropdown en blanco = no permite guardar
- Solo hay una pregunta por instancia de LC (DC_LC_DONE)
- Retrieve es obligatorio antes de poder interactuar con la fila — sin Retrieve el [Collect] no es visible

---

### Notas de implementación (Fase 6)

`LineClearanceControl.ascx` contiene **ambos modales** en un solo UserControl:
- `modalDCStandalone` — modal principal
- `modalDCEntry` — sub-modal (oculto en DOM, JS lo abre al hacer Collect)

**Modelo de PostBacks:**
- PostBack #1: `BtnActivities_ServerClick` → carga el control, registra `showModalSafe('modalDCStandalone')`
- Retrieve = **puro JS** (`lcRetrieve()`): la fila DC_LC_DONE está pre-renderizada en el DOM pero oculta; Retrieve solo la hace visible. No se consulta BD porque el contenido de LC es fijo (no varía por área). Mismo patrón que Resource Setup.
- [Collect] = **puro JS** (`lcOpenEntry()`): abre el sub-modal pre-renderizado
- PostBack #2: [Save] en sub-modal → `BtnSave_Click` — valida HfAnswer, llama `OnStepSubmit`, registra startup script: cerrar sub-modal + re-abrir modal principal + mostrar banner + auto-hide 5s

Ambos modales se pre-renderizan en el DOM al cargar el control. Ninguno requiere PostBack adicional para abrirse — solo JS.

---

### Diferencias SAP real vs. Legacy
| Elemento | SAP real | Legacy |
|---|---|---|
| Acceso a LC | Activities dropdown → Data Collection Standalone | Botón "LINE CLEARANCE" directo |
| Modal principal | Muestra fila DC_LC_DONE con [Collect] inline | `<article>` único sin sub-modal separado |
| Sub-modal | Ventana flotante independiente sobre el padre | Sección que swapea con `d-none` |
| Confirmación post-save | Banner "dc group save" en modal principal 5s | Texto + ícono `lineClearence-save-icon.png` |
| Retrieve | Requerido — muestra la fila DC_LC_DONE (puro JS en simulador) | Botón Retrieve requerido antes de interactuar |
| Respuesta "No" | Registra intento fallido, no bloquea (Guided) | No contemplado |

---

---

## Paso 3 — Search / Start Order

**StepType:** `Search` | **Validación:** secuencia pura; en áreas con checklist (Moldeo) además las respuestas de LC Moldeo y la verificación final

**Acceso:** directamente en el Dashboard — campo SFC en la search bar.

> No abre modal propio. El operador carga la orden en el POD desde la pantalla principal.

### Campo SFC (search bar)

```
★ {operationName}    ★ {resourceName}    SFC: [________________________] [browse]
                                                ↑
                                    Editable. Acepta escaneo o escritura manual.
                                    Ícono browse a la derecha abre Browse Work List.
```

**MES real:** el campo SFC es **editable y acepta entrada directa** (escaneo o teclado). En el
simulador `TxtSFC` es readonly y el SFC se elige vía Browse Work List (spec 005, ver
"Implementación en el simulador" abajo).

### Sub-modal: Browse Work List (ruta alternativa)

Se abre al hacer clic en el ícono browse cuando no es posible escanear.

```
[BROWSE WORK LIST - SFCS QUEUED - {operation},{resource},{userId}]    [↗][X]
─────────────────────────────────────────────────────────────────────────────
  [⧉] SFC ↑    Material Description    Shop Order Schedule Start Date    Shop Order Planned Completion
  ─────────────────────────────────────────────────────────────────────────────
  [ ] {sfcCode}    {description}    {scheduleDate}    {plannedDate}
  ...
─────────────────────────────────────────────────────────────────────────────
                                                        [✓ OK]  [✗ Cancel]
```

- `[⧉]` = ícono **Select All** en la columna selectora (igual que en la tabla del POD):
  selecciona todas las filas o, si ya estaban todas, las deselecciona
- Un clic = selecciona fila (highlight)
- [OK] o doble clic = confirma selección, cierra modal, carga en POD
- [Cancel] = cierra sin cambios

### Estado del POD después de cargar la orden

```
★ {operationName}    ★ {resourceName}    SFC: {sfcCode}  [copy]    Shop Order: {shopOrder}    Status (SFC): Active
```

Tabla POD poblada con la fila del SFC cargado. Header top-right se actualiza con Shop Order + Status.

Si el sistema no auto-inicia: el operador presiona **[Start]** en la barra de acción para iniciar la orden.

### Flujo del operador
1. Enfocar campo SFC en la search bar
2a. Escanear SFC/etiqueta directamente → Enter → POD carga automáticamente
2b. (alternativa) Clic en ícono browse → Browse Work List → seleccionar fila → OK
3. Verificar que la tabla POD muestre la orden con Status: Active
4. Si la orden no inició: clic en [Start]

### Procedimiento real (revisión con SAP MES, 2026-10-09)
Fuente: el ingeniero de SAP MES (Diego), sobre Redefinity en Guided, y lo que el usuario vio
en planta. Hallazgos y propuestas: roadmap → "Fase 8".
- **La cola trae piezas de otras órdenes.** La operación casi siempre tiene en cola unidades
  de otras órdenes o batch. El procedimiento es iniciar la orden que se va a manufacturar, no
  seleccionar todo lo que hay en cola: eso puede iniciar unidades ajenas. El MES bloquea
  iniciar unidades que no son de la orden (lo controla la configuración de la operación,
  ligada al Line Clearance). El mensaje de error no tiene captura.
- **Lo habitual es escanear la orden y dar Start.** Browse Work List se usa cuando se
  trabajan varias órdenes a la vez.
- **Start manual, confirmado.** Después de seleccionar hay que presionar [Start]. Solo carga
  sola cuando el escáner está configurado para mandar el Enter (procedimientos que piden
  escanear).
- **Diferencia con el simulador:** la cola solo trae piezas de la orden y Start exige
  seleccionarlas todas (spec 008), así que "seleccionar todo" siempre es correcto: entrena
  el hábito que el procedimiento prohíbe.

### Reglas de la pantalla
- No se valida el SFC/Shop Order contra catálogo — cualquier valor es aceptado
- La tabla POD debe quedar con al menos una fila visible — estado vacío no es válido para continuar
- Bifurcación por SerializationType (`docs/domain.md` → "Ejes de variación"): ver "Implementación en el simulador"
- **Mensajes del MES** (spec 017, banner rojo del POD, sin auto-hide): Start sin SFC →
  `SFC is missing (Message 22506)`; con un número que no es de la orden (al escanearlo o al
  presionar Start) → `Invalid SFC (Message 22507)`. En Serialized, Start sin ninguna pieza
  muestra el `22506` y además el aviso del simulador "Inicia la orden completa". Cuándo sale
  cada mensaje de un botón fuera de turno: `docs/domain.md` → "Mensajes del MES".

### Implementación en el simulador (specs 005 y 008)
- `TxtSFC` es readonly: el SFC se elige solo vía Browse Work List (spec 005).
- **Con checklist** (`Areas.RequiresStartChecklist = 1`, hoy solo Moldeo): Start abre la
  secuencia de 4 modales de `moldeo-flow.md`.
- **Sin checklist** (el resto): Start es directo, sin modales; se califica solo por
  secuencia.
- **Serialized:** la orden tiene 4-5 piezas `{12NC}-{serial}` con seriales consecutivos
  (en `New`; 3-5 hasta la spec 014; `{ShopOrder}-{sufijo}` hasta la fila 12 de la Fase 8.5, W23). Browse Work
  List permite multi-selección (click alterna la fila, sin columna Status) y `TxtSFC`
  muestra el SFC o `[N SELECTED]`. Start exige **todas** las piezas (si no, aviso sin
  intento), las pasa a `Active` y las carga en el POD **sin seleccionar**: el operador las
  selecciona de una vez (Select All) para la actividad siguiente. Corrección del usuario
  (2026-10-07): en el MES real no quedan seleccionadas; antes el simulador las dejaba
  seleccionadas. Lo mismo al volver de Assembly y de Data Collection.
- **NonSerialized:** un solo SFC de lote; selección de una fila.

### Diferencias SAP real vs. Legacy
| Elemento | SAP real | Legacy |
|---|---|---|
| Campo SFC | Editable, acepta escaneo directo | Readonly, solo abría el modal |
| Nombre del modal | "BROWSE WORK LIST - SFCS QUEUED - {contexto}" | "BUSCAR POOL DE TRABAJO - SFC EN COLA" |
| Columnas del Browse | SFC · Material Desc · Schedule Start · Planned Completion | CP · Order · State · Shop Order · Material Desc |
| Footer del Browse | [✓ OK] [✗ Cancel] | [OK] [Cancelar] |
| Shop Order en header | Actualiza top-right: `Shop Order: XXXXX  Status: Active` | `Orden de fabricación: Status (NCP)` |

---

---

## Paso 4 — Assembly (condicional: RequiresAssembly = true)

**StepType:** `Assembly` | **Validación:** solo secuencia; el paso se completa con todo el BOM ensamblado (spec 008)

**Acceso:** [Assemble] en la barra de acción (paso presente solo si RequiresAssembly = true).

> **RequiresAssembly es independiente de SerializationType.** No derivar uno del otro.

> **Fuentes reales:** captura de la Component List de EEP (Catéteres) y fotos del modal
> Assemble Components (antes y después del escaneo) aportadas por el usuario el
> 2026-09-28; confirmación de un operador de Sensores: los códigos son los **números de
> parte del BOM de la operación** y varían por operación.

### Trigger

El operador tiene seleccionadas las filas del POD → presiona **[Assemble]**. En Serialized
tienen que estar **todas** las piezas (aviso sin intento si falta alguna). Tras el Start el
POD las muestra sin seleccionar (corrección 2026-10-07): el operador presiona Select All.
`TxtSFC` muestra `[N SELECTED]` (en la captura real: `[250 SELECTED]`) y queda vacío mientras
no hay selección.

### Vista: Component List (reemplaza POD Work List)

```
Component List                                                         [?] [X]
─────────────────────────────────────────────────────────────────────────────
  {PartNumber}/{Revision}         Description: {description}
  {Qty}                           Rem Qty: {remaining}
                                                    [Assemble]  [Disassemble]
  ...
─────────────────────────────────────────────────────────────────────────────
  [|< First]  [<< Prev]  [Next >>]  [Last >|]       Rows 1-N of N
```

- Filas = BOM del área (`AssemblyComponents`, **simulado por ahora**; hoy por área, a
  futuro por Operation). Ej. real: `453601027151/A`, Qty `1`; `453601048391/A`, Qty `0.001`.
- **Qty** decimal por unidad (materiales por peso o longitud), sin ceros de relleno.
- **Rem Qty** arranca igual a Qty y pasa a `0` con el escaneo; con `0`, [Assemble] se
  deshabilita.
- **[Disassemble]**: visible y siempre deshabilitado (el simulador no entrena desensamble).
- **[X]**: vuelve al POD conservando el avance.

### Modal: Assemble Components

Se abre con **[Assemble] de una fila**, para ese componente (en el cliente, sin postback).

**Estado vacío (antes de escaneo)**
```
[Assemble Components]                                                  [↗][X]
Assemble Components                                                        (?)
──────────────────────────────────────────────────────────────────────────────
  Mode:    Choose  (texto)            Find component: [________________]
  Action:  ● Assembly  ○ Removal      Barcode:        [________________]
           (informativo, gris)
────────────────────────────────────────────────────────────────────────────
  (cuerpo vacío)
────────────────────────────────────────────────────────────────────────────
                              [✖ Return to POD]
```

**Estado con datos (después de escaneo)**
```
  ...(misma franja superior)...
────────────────────────────────────────────────────────────────────────────
  Component Details                  │ Assembly Data
  Component:  [{PartNumber}] [⧉]     │ ★ Batch Number: [{Process Lot}]
  Version:    [{Revision}]           │
  Description: {description}         │
  Quantity:   [{Qty}]                │
────────────────────────────────────────────────────────────────────────────
         ☑ Apply to all SFCs    [✔ Add]    [✖ Return to POD]
```

- **☑ Apply to all SFCs**: marcado y deshabilitado, solo con más de una pieza. Va en el pie,
  a la izquierda de [Add], y aparece junto con él tras el escaneo (layout real, 2026-09-29).

- **Barcode**: numérico; el escaneo se procesa con Enter (como el lector) o al salir del
  campo. Enter no envía el formulario.
- **Component Details**: inputs de solo lectura con los datos del catálogo.
- **★ Batch Number** = **Process Lot** de la orden (columna del POD), no el barcode. En el
  MES real la Assembly Data cambia según el control del material (W13 en
  `wi-produccion-disposables.md`); el simulador no lo modela (spec 014, enmienda 6: E4
  recortada y diferida).
- **[✔ Add]** aparece recién tras un escaneo válido.
- **Lo ensamblado se conserva** (spec 014): al terminar el paso, el simulador guarda cada
  componente con su Batch Number. Lo usan el Browse de Component y Remove component en la
  reparación rápida (Paso 8).

### Flujo del operador
1. Con todas las filas seleccionadas → [Assemble] → Component List
2. [Assemble] en un componente → modal → escanear barcode (Enter)
3. [Add] → Rem Qty `0` y la lista se actualiza; **el modal queda abierto** con la
   confirmación "☑ Component {PartNumber}/{Revision} assembled" arriba a la izquierda,
   bajo la línea azul del encabezado (Fase 6.5; el texto exacto del real está por
   confirmar). El operador cierra con ✕ (vuelve a la Component List) o Return to POD.
4. Repetir por cada componente; con el último, el paso se completa y vuelve el POD
   con el banner del MES real **"All components processed at this operation"**

### Reglas de la pantalla
- **Qué cubre cada escaneo y cómo se califica:** `docs/domain.md` → "Flujo estándar" (paso 4)
  y "Qué cuenta como intento".
- El avance vive en Session: sobrevive a Return to POD / [X] y a F5.
- El orden de Assembly en el flujo es después de Search/Start y antes de Data Collection, pero el motor respeta `TrainingStep.Order` — puede cambiar por área
- **Mensajes del MES** (spec 017): [Assemble] sin la fila → banner `SFC is missing (Message
  12906)`. Un barcode no numérico → franja roja en Assemble Components `2D barcode is not
  compliant with ISO 15434 format (Message 17150)`, y se despliegan Component Details y
  Assembly Data vacíos con [Add] (AUD-69); ya no hay SweetAlert "Barcode inválido".

### Diferencias SAP real vs. Legacy
| Elemento | SAP real | Legacy |
|---|---|---|
| Component List | Reemplaza POD Work List en la misma área | No implementado (✅ implementado en spec 008) |
| Modal Assemble Components | Dos estados: vacío / con datos; Add condicional | No implementado (✅ spec 008) |
| Paginación Component List | First / Prev / Next / Last + contador | No implementado (visual en spec 008) |
| Botón Add | Solo aparece tras escaneo exitoso | No implementado (✅ spec 008) |

---

---

## Paso 5 — DC Collect (condicional: RequiresDataCollection = true)

**StepType:** `DataCollection` | **Validación:** JSON `{"<paramId>": "<valor>"}` tipado por DataType

**Acceso:** [DC Collect] en la barra de acción. Solo si `RequiresDataCollection = true` para el área.

> Mismo patrón estructural que Assembly: botón → reemplaza POD Work List por lista → cada fila tiene [Collect] → abre modal de entrada.

### Vista: Data Collection List (reemplaza POD Work List)

```
Data Collection List                                                   [?] [X]
─────────────────────────────────────────────────────────────────────────────
  {DC_GROUP_CODE}    {description}
                     Collect Data At: {timing}                       [Collect]
  ...
─────────────────────────────────────────────────────────────────────────────
  [|< First]  [<< Prev]  [Next >>]  [Last >|]       Rows X-Y of Z
```

- `{DC_GROUP_CODE}`: ej. `DCGTM_DETECTOR_BRD/A`
- `Collect Data At`: metadata del grupo (Anytime / Complete / etc.) — no bloquea en el simulador
- **[Collect] deshabilitado** en el grupo que la pieza actual ya guardó (como [Assemble] con
  Rem Qty 0). Al pasar a la pieza siguiente (Serialized) todos vuelven a habilitarse
  (2026-09-29). Solo en este paso: el Collect de Line Clearance no cambia.
- Paginación idéntica a Component List

### Modal: Data Collection Entry

Jerarquía del real (corrección del usuario, Fase 6.5, entrega 8): la barra superior
**no lleva título**, solo [⤢][✕]; el título va en el segundo encabezado.

```
                                                                      [⤢][✕]
  Data Collection Entry                                                  (?)
════════════════════════════════════════════════════════════════════════════
  SFC: {sfcCode} ({current}/{total})                       ← fila gris
  DC Group: {DC_GROUP_CODE}                                ← fila gris
────────────────────────────────────────────────────────────────────────────
  ┌──────────────────────────────────────────────────────────────────────┐
  │ * {pregunta}:                                                         │
  │ Description: {DataCollectionParameters.Description}   ← solo si hay  │
  │ [_____________input_____________]   Data Type: {tipo}    [Comments]  │
  └──────────────────────────────────────────────────────────────────────┘
  (un recuadro por parámetro del grupo)
────────────────────────────────────────────────────────────────────────────
     [💾 Save]  [💾 Save Draft]  [🧽 Clear]  [✖ Return to POD]   ← grises con ícono
```

**Description** (columna nueva `DataCollectionParameters.Description`, nullable): texto
corto del catálogo distinto de la pregunta (ej. pregunta "Documente el numero de ID del
detector board:" / Description "Detector board ID"). Sin texto, la línea no aparece.
Moldeo usa las descripciones del legacy (`06_RCMoldeoCatalog.sql`).

**Estilo** (captura `DC COLLECT/modal data collection entry`): línea celeste bajo el
título; bandas "SFC" (`#F2F2F2`) y "DC Group" (`#F4F6F7`, sombra suave arriba) en letra
grande; parámetros sobre blanco separados por una línea, dentro de un marco gris claro;
"Data Type:" a la altura de "Description:" y el campo debajo. "Unit of Measure" del real
no se replica (decisión del usuario).

**Contador `({current}/{total})`** (spec 008): las piezas seleccionadas se procesan una por
una en el orden del POD. El contador pasa a la pieza siguiente cuando la actual guardó
**todos sus DC Groups obligatorios** (si el área no tiene obligatorios, basta uno). En
NonSerialized hay una sola pieza, el lote: `(1/1)`.

**Input por DataType:**
- `Text` → input libre
- `Numeric` → input numérico
- `Boolean` → dropdown (Sí / No)

**[Comments]**: botón opcional por parámetro — no validado en entrenamiento.

**Footer:**
| Botón | Comportamiento |
|---|---|
| Save | Guarda el grupo para la pieza actual y vuelve a la Data Collection List; si completó la pieza, pasa a la siguiente |
| Save Draft | Guarda parcialmente — no marca el grupo como completado |
| Clear | Limpia campos sin cerrar |
| Return to POD | Sale sin guardar |

### Flujo del operador (implementado, spec 008)
1. Seleccionar las filas en POD (Serialized: todas) → [DC Collect]
2. Data Collection List se muestra con todos los grupos del área (sin marcar cuáles son
   obligatorios: saberlo es parte del entrenamiento)
3. Clic en [Collect] de un grupo → Data Collection Entry con `SFC: {pieza} (n/N)`
4. Completar cada parámetro según su DataType → [Save]
5. Save correcto → vuelve a la lista (banner "Data collected for {grupo}"). Al completar
   los obligatorios de la pieza → banner "SFC … completed. Next: … (n/N)"
6. Con la última pieza completa, el paso termina y vuelve el POD

**Revisión con SAP MES (2026-10-09):** recolectar por pieza es correcto, pero según la
configuración de la operación el MES también permite recolectar para todas las piezas a la
vez (con cientos de unidades es lo práctico). El simulador va pieza por pieza, como se vio
en Sensores. **Validación con técnicos (2026-10-12):** pieza por pieza se mantiene;
"aplicar a todas las piezas" queda descartado. El banner "SFC … completed. Next: …" no tiene captura y puede ser propio del
simulador: verificarlo en el ambiente de calidad de SAP MES.

### Patrón compartido: DC Collect vs. Assembly

| Elemento | Assembly | DC Collect |
|---|---|---|
| Vista lista | Component List | Data Collection List |
| Fila | Component + Rem Qty + [Assemble][Disassemble] | DC Group + Collect Data At + [Collect] |
| Modal | Assemble Components | Data Collection Entry |
| Contexto modal | Component code / version | SFC (N/total) + DC Group |
| Footer modal | [Add] [Return to POD] | [Save] [Save Draft] [Clear] [Return to POD] |
| Iteración por SFC | No — un escaneo aplica a todas las piezas | Sí — (n/N), avanza al completar los grupos obligatorios de la pieza |
| Paginación en lista | Sí | Sí |

### Reglas de la pantalla
- Solo aplica si `RequiresDataCollection = true`
- **Grupos, parámetros, cuándo termina el paso y qué cuenta como intento:** `docs/domain.md`
  → "Catálogos en BD" y "Qué cuenta como intento".
- Validación del Save: tienen que venir **todos** los parámetros del grupo y solo esos;
  Numeric dentro de rango, Boolean = Sí, Date válida.
- Un Save incorrecto es intento fallido del paso, pero **conserva** el avance (grupos y
  piezas ya guardados). Un grupo ya guardado por la pieza actual no se puede volver a
  guardar: su [Collect] está deshabilitado y el servidor ignora un segundo Save.
- Save Draft no guarda nada (solo orienta al operador)
- **Mensajes del MES** (spec 017): [DC Collect] sin la fila → banner `SFC is missing (Message
  12906)`. Un Save incorrecto muestra, en la franja roja de Data Collection Entry, el mensaje
  del **primer** parámetro con fallo en el orden de la pantalla (vacío `10015`, texto en un
  numérico `12205`, bajo o sobre el límite `15232`/`15233` con el valor escrito). Un Boolean
  en NO no tiene mensaje del MES. La franja queda hasta el siguiente Save o al cerrar.
- El intento correcto se registra cuando la última pieza completa sus grupos obligatorios.
- El avance vive en Session: sobrevive a Return to POD / [X] y a F5.

### Diferencias SAP real vs. Legacy
| Elemento | SAP real | Legacy |
|---|---|---|
| Vista Data Collection List | Reemplaza POD Work List en la misma área | Modal separado (`#dataCollection`) |
| Contexto modal | SFC (N/total) + DC Group en header | Sin contador de iteración |
| [Comments] por parámetro | Sí | No implementado |
| Iteración por SFC lote | (1/29) automático tras Save | No implementado |
| Footer | Save · Save Draft · Clear · Return to POD | Save · Save Draft · Cancel · Return to POD |

---

---

## Paso 6 — Closing Line Clearance (condicional por área)

**StepType:** `LineClearance` | **Validación:** respuesta = "Sí" (true)

> **Estructura y flujo idénticos al Paso 2.** Ver sección "Paso 2 — Line Clearance" para la documentación completa de modales, estados y reglas.

> **Trainer de Wires (2026-10-13):** en algunas operaciones no aplica; por ejemplo, Wires
> línea final no hace LC de cierre. El simulador lo mantiene y la guía lo aclara con una
> alerta (T7).

### Condicionalidad

| Escenario | Comportamiento |
|---|---|
| Área **con** Closing LC | El flujo ejecuta este paso antes de Log NC y Complete |
| Área **sin** Closing LC | El flujo omite este paso — el motor avanza directamente al Paso 7 |

La condicionalidad vive en el **catálogo de guías** (`TrainingGuide.Steps`): si el área no tiene un `TrainingStep` de tipo `LineClearance` en la posición de cierre, no existe en la secuencia. No requiere cambio de código.
Hoy todas las guías lo incluyen: por regla de negocio el Cierre es igual en todas las áreas
(`docs/domain.md` → "Flujo estándar").

---

---

## Paso 7 — SFC Split (solo NonSerialized)

**StepType:** `SFC_Split` | **Validación:** al menos un split exitoso

**Acceso:** seleccionar una fila del POD → Sidebar → [SFC Split]. Las guías Serialized omiten este paso.

**Modal: SFC Split Plug-In (Auto ID)** (captura `Split - Log NC/modal sfc split`; Fase 6.5,
entrega 9)
```
[SFC Split Plug-In (Auto ID)]                                  [⤢][✕]
████ SFC Split ████████████████████████████████████████████████  (?)   ← banda oscura
  ☑ New SFC {sfcCode} created with quantity {qty}     ← arriba, tras un split
                 SFC to Split:  {sfcCode}
                Available Qty:  {qty}
              * Qty to Split:  [_____ ✕]   ← amarillo claro; la ✕ limpia (cliente)
                       [Split]  [Close]     ← contorno celeste, centrados
```
- Formulario centrado, etiquetas en negrita a la derecha, fondo gris claro `#EBEDEF`.
  Sin mensajes de validación bajo el campo. Una cantidad que el MES rechaza **reemplaza el
  formulario** por su mensaje con [OK], y el OK cierra la ventana entera: hay que reabrir
  SFC Split (spec 017, AUD-64). Vacía → `Define Qty to Split (Message 13875)`; 0 →
  `Quantity must be greater than 0 (Message 13213)`; mayor a la disponible → `Requested qty
  to split {n} is greater than the available SFC qty {m} (Message 13885)`. La Qty entera de
  la fila o con decimales sigue como aviso del simulador (SweetAlert). Formato, sin intento.
- [SFC Split] sin fila seleccionada → banner `SFC is missing (Message 12906)` (spec 017).
- La confirmación va arriba, con el check verde y sin recuadro, con el **SFC nuevo**.
- **El split crea un SFC nuevo de la misma Shop Order**, como el real (enmienda 2026-10-01
  de la spec 005): mismo prefijo que el SFC del lote y 4 dígitos finales nuevos que no se
  repiten en la orden (ej. `CR9A30340695` → `CR9A30343832`). "SFC to Split" muestra el SFC
  de la fila elegida (una fila de un split anterior tiene el suyo).
Operador verifica la nueva fila en el POD antes de continuar.

- [Split] muta el POD sin registrar intento ni avanzar el TrainingStep. Desde la spec 014
  (enmiendas 2 y 3) la **nota de hallazgo** se ve durante el SFC Split con las N unidades
  que hay que separar, y la cantidad se califica: otra cantidad es intento fallido y no se
  aplica (regla en `docs/domain.md` → "Qué cuenta como intento").
- La X superior cierra el modal para seleccionar otra fila original o derivada.
- [Close] se habilita después del primer split exitoso y completa el paso.

---

## Paso 8 — Log NC

**StepType:** `LogNC` (desecho rápido), `LogNCFastRepair` y `LogNCExtendedRepair` (spec 014)
| **Validación:** pieza, código exacto y decisiones del método contra el hallazgo del paso
(`docs/domain.md` → "Qué cuenta como intento")

**Acceso:** Sidebar → [Log NC], el mismo botón para los tres métodos.

**Nota de hallazgo** (spec 014, simulador; no imita una pantalla del MES): panel con el
estilo de la guía, en Guided y Assessment, mientras el paso actual es de Log NC (en Moldeo,
también en el SFC Split). Muestra solo el hallazgo del paso: "Hallazgo n de N", el SFC (o
"N unidades del lote"), el defecto y la disposición, sin nombrar el código.

### Vista: NC Selection (reemplaza POD Work List)

Layout de **2 columnas**. Cada celda:
```
  {NC_CODE}              [↑ Log NC]
  {descripción}
```
El botón [Log NC] es por código individual. NC Codes desde BD por área (`NCCodes`).

### Modal: NC Data Entry

Mismo patrón que Data Collection Entry (Fase 6.5, entrega 8; captura `MODAL NC DATA ENTRY`):

```
                                                                      [⤢][✕]
  NC Data Entry                                                          (?)
════════════════════════════════════════════════════════════════════════════
  SFC: {sfcCode}                                           ← fila gris
────────────────────────────────────────────────────────────────────────────
                    * NC Code: [{codePreFilled}          ]   (solo lectura)
                    Component: [                       ⧉]   ← solo con código de reparación
                     Comments: [                         ]   ← área de texto, opcional
────────────────────────────────────────────────────────────────────────────
        [✓ Add]  [✓ Add-Done]  [✖ Return to POD]  [✓ Done]   ← grises con ícono
```

- Etiquetas a la derecha y campos a la izquierda, centrados. **Component sin asterisco**,
  como el real. Solo aparece con un código de reparación: obligatorio en la rápida, opcional en la extendida; el desecho no lo muestra en ningún área (enmienda 4 de la spec 014).
- **Indicador del SFC** (corrección del usuario, igual que Assemble Components):
  `{primer SFC seleccionado} (1/N)`, con N = piezas seleccionadas — ej. `1234-002 (1/1)`,
  `1234-002 (1/3)`. Con N > 1, **"Apply to all SFCs"** marcado y fijo a la izquierda de
  [Add]: el NC se registra en todas las piezas seleccionadas a la vez (spec 008).

| Botón | Comportamiento |
|---|---|
| Add | Registra NC, permanece en modal para agregar otro |
| **Add-Done** | Registra NC y cierra — flujo normal |
| Return to POD | Cancela sin registrar |
| Done | Confirma y cierra sin agregar datos nuevos |

**Confirmación en POD (banner verde, fuera del modal):**
```
✓  All of {SFCCode} scrapped
```
El campo SFC en la search bar queda vacío tras el scrap.

### Flujo del operador
**NonSerialized:** Sidebar [SFC Split] → Qty to Split → [Split] → confirmar → [Close] → verificar POD → Sidebar [Log NC] → NC Selection → [Log NC] del código → NC Data Entry → [Add-Done] → banner confirmación
**Serialized:** Seleccionar SFC en POD → Sidebar [Log NC] → NC Selection → [Log NC] del código → NC Data Entry → [Add-Done] → banner confirmación

### Reglas de la pantalla
- **Calificación** (pieza y código del hallazgo, Add-Done, filas, formato): `docs/domain.md`
  → "Qué cuenta como intento" → "Log NC".
- SFC Split solo para NonSerialized — Serialized selecciona directamente del POD
- **Mensajes del MES** (spec 017): [Log NC] sin ninguna fila → banner `SFC is missing
  (Message 12906)`, además del intento fallido. Done sin NC Code (`18117`) no se puede
  provocar: NC Data Entry se abre siempre con un código (D-120).
- NC Code requerido; Component solo con código de reparación (requerido en la rápida); Comments opcional, salvo con un código que lo exige (`F_FX_GENERAL`)
- NC Codes desde catálogo BD por área — no hardcodeados
- **NonSerialized:** una fila, creada por el SFC Split. **Serialized:** el MES permite
  seleccionar varias filas (spec 008); en el desecho tiene que quedar al menos una pieza para
  Complete: seleccionarlas todas es un aviso sin intento (`PieceSelectionRule.NoPieceLeft`),
  antes de calificar.
- Banner del desecho: "✓ All of {SFC} scrapped" (una) o "✓ N SFCs scrapped" (varias). En
  Serialized el SFC field queda vacío; en NonSerialized conserva el SFC del lote. Los de la
  reparación rápida y la extendida, abajo.

### Métodos de NC en el MES real (revisión con SAP MES, 2026-10-09)
Hoy se usan tres. Cuáles tiene cada operación depende de su configuración, que sigue los
documentos de manufactura (auditados) de esa operación:
- **Fast scrap:** botar unidades.
- **Scrap / return component:** devolver, cambiar o botar un componente. Lleva pasos
  adicionales; según el ingeniero, se olvidan o se hacen mal con frecuencia.
- **Extensión de la operación:** regresar unidades a ciertas operaciones para retrabajo.

El simulador entrena los tres desde la spec 014 (2026-10-19): desecho rápido, reparación
rápida y reparación extendida, un paso de la guía cada uno (Moldeo, solo el desecho). Sus
reglas y su calificación viven en `docs/domain.md`, no aquí.

### Pantallas de la reparación rápida y la extendida (usuario, 2026-10-15)

Capturas en `Screens SAP MES REAL/Split - Log NC/`. Las anotó el usuario, que validó los
flujos con una técnica de planta. En el MES real, la **disposición** la dictan el MPI o el
NCR.

**Component ⧉ → Browse** (`browse component.png`). Es el campo Component de NC Data Entry.
En el simulador aparece solo con los códigos de reparación (spec 014, enmienda 4):
obligatorio en la rápida y opcional en la extendida; el desecho no lo muestra en ningún
área.
```
  Browse                                                     [⤢][✕]
  ┌──┬────────────────┬──────────────────────────────────┐
  │  │ Component      │ Description                      │
  │  │ 454000131551   │ FISH TAPE, LASER SHEATH          │
  │■ │ 454000132321   │ DISPENSING HOOP, FISHTAPE (SMALL)│  ← fila elegida (azul)
  └──┴────────────────┴──────────────────────────────────┘
                                         [✓ OK]  [✖ Cancel]
```
Lista los componentes ensamblados en la pieza (el BOM usado en Assemble).

**Reparación rápida (`F_FR…`).** Después de Add-Done en NC Data Entry:
1. **Confirm** (`mensaje eliminar componente.png`): "Do you want to remove component from
   {SFC}?" [Yes] [No].
2. **Remove component** (`remove compoenet modal .png`), con dos secciones:
   - **As-Built Configuration** (solo lectura):
     - a la izquierda: SFC, Material/Vers., Routing/Vers., Actual Component/Vers.,
       Description, Ref Des, Qty Required y Qty Assembled;
     - a la derecha: Status, Description, BOM/Version, Assembly Operation, Assembly
       Resource, Inventory SFC y Batch Number.
   - **Remove assembly details:** ★ Qty to Remove (`1`), Remove Operation y Remove
     Resource, ambos con ⧉.
   - Botones abajo: [Scrap] [Return] [Send to Routing] [Cancel]. **Return** devuelve el
     componente a inventario (solo si el MPI lo permite; poco común); **Scrap**, por
     sobreconsumo o daño (el caso habitual).
3. **Confirm** (`confirmacion agregar compoenente .png`): "Do you want to add component to
   {SFC}?" [Yes] [No]. Con **No**, o con Return en el paso anterior, la pieza queda sin el
   componente y hay que volver a ensamblarlo.
4. Con **Yes**: modal **"Add Component"** (`modal add componenet .png`, 2026-10-16), sobre
   NC Data Entry:
   ```
     Add Component                                                  [⤢][✕]
     As-Built Configuration
     ──────────────────────────────────────────────────────────────────────
          SFC: 300014142061-42814                  Status: Active
     Material/Vers.: 300014142061/A           Description: OmniWire, 185CM…
     Routing/Vers.: PR_OMNIWIRE_STRAIGHT_SOS/A-7  BOM/Version: 303627317-…/A-1
     Component Details                     │ Add Assembly Details
     ★ Component: [300000257791    ⧉]      │ Assembly Operation: [O_OMNI_FIRST_DI… ⧉]
     ★ Version:   [A              ]        │ Assembly Resource:  [R_OMNI_FIRST_DI… ⧉]
     Description: Adhesive, Masterbond…    │ ★ Batch Number:     [               ]
     Ref Des:     [               ]        │
     Qty Required: 0.005                   │
     Qty Assembled: 0                      │
     ★ Qty to Assemble: [0.005    ]        │
                                 [Add]  [Cancel]
   ```
   Add Assembly Details **cambia según el tipo de material** (W13 en
   `wi-produccion-disposables.md`): en la captura, un adhesivo por batch con ★ Batch Number
   vacío que escribe el operador; el recorte del manual (`recorte de agregado del nuevo
   compoenente .png`) muestra un componente por inventario: ★ Inventory SFC
   (`CR9AP1700000801`, con ⧉) y ★ Batch Number en gris (`0301366528`). No hay campo
   Barcode. **En el simulador** Add Assembly Details muestra solo ★ Batch Number vacío, que
   escribe el operador (formato, no se califica): la variante por material quedó diferida
   (spec 014, enmienda 6).
5. Termina con el banner verde "Component {componente} replaced on {SFC}" (spec 014, D-55;
   `GuidedTest.ShowRepaired`) y la pieza sigue en el POD. El usuario lo confirmó como
   correcto el 2026-10-19 (V-3).

**Reparación extendida (`F_FX…`).**
1. NC Data Entry (`modal nc entry extended.png`): ★ NC Code, Component (opcional) y
   ★ Comments. Con `F_FX_GENERAL`, Comments es obligatorio y lleva el MPI y su revisión, o
   el número de NCR. Luego, [Add-Done].
2. NC Data Entry muestra "✓ NC logged" arriba del SFC y abre un sub-modal
   (`sub modal seleccion operaicon a ser devuelta .png`):
   ```
     Select Disposition Function for {SFC}                    [⤢][✕]
     ┌─────────────────────────────────────────────────────────────┐
     │ Local Rework                                    [✓ Select]  │
     │ Return to any previous                          [✓ Select]  │
     └─────────────────────────────────────────────────────────────┘
                                                        [✖ Cancel]
   ```
3. Con Select se cierra todo y vuelve al POD. Con **Return to any previous** (usuario,
   2026-10-15): banner "All of {SFC} will be processed at {operación anterior}" y la pieza
   sale del POD. Con **Local Rework**: el mismo banner con la operación actual y la pieza
   sigue en el POD (spec 014, D-54; confirmado por el usuario el 2026-10-19, V-5).
   **En el simulador (spec 014, E3):** las operaciones son las `SIM` de `Operations` (D-58),
   p. ej. `O_LOFLO_SIM_ENSAMBLE` (actual) y `O_LOFLO_SIM_PREP` (anterior); los dos banners
   los muestra `GuidedTest.ShowReworked`. Cancel y ✕ del sub-modal vuelven a NC Data Entry sin "NC logged" y sin contar
   (P-15).
4. **Sin verificar:** qué muestra cada disposición. La WI no tiene Disposition Function: tras
   "NC logged" muestra **"Select next step for {SFC}"** con las operaciones de la ruta
   (`010 O_VERR_W_ASSEMBLY_PREP` "Wire Assembly Preparation", `020 O_VERR_FIRST_DISTAL`
   "First Distal"; [Select] por fila, [Cancel]) y el banner "All of 989609001231-543988 will
   be processed at O_VERR_FIRST_DISTAL", con el campo SFC vacío. El simulador sigue la
   captura del sandbox (spec 014, D-51).
5. **Códigos:** en una figura de la WI, el código de reparación rápida es `F_FASTREPAIR`, no
   `F_FR…`. El método de un código no se deduce del prefijo.

### Diferencias SAP real vs. Legacy
| Elemento | SAP real | Legacy |
|---|---|---|
| NC Selection | Reemplaza POD Work List, 2 columnas, [Log NC] por código | Modal flotante paginado (5 páginas) |
| NC Data Entry footer | Add · Add-Done · Return to POD · Done | Add · Add Done · Return POD · Finished |
| Confirmación scrap | Banner verde en POD "All of {SFC} scrapped" | Mensaje inline en modal |

---

---

## Paso 9 — Complete

**StepType:** `Complete` | **Validación:** secuencia pura (siempre true)

**Acceso:** [Complete] en la barra de acción.

> Paso más directo del flujo — sin modal propio, sin sub-ventanas.

### Flujo del operador
1. Seleccionar fila(s) del SFC u orden de planta en el POD Work List
2. Clic en **[Complete]** en la barra de acción
3. El sistema completa la orden y los SFCs seleccionados: las filas salen del POD (pasan a la
   siguiente operación)
4. Sigue el Paso 10 — Logout (spec 015)

### Reglas de la pantalla
- **Calificación** (secuencia y filas): `docs/domain.md` → "Flujo estándar" (pasos 3 y 9) y
  "Qué cuenta como intento".
- Ya no es el último paso: la sesión sigue con Logout (spec 015)
- NonSerialized: una fila. Serialized (spec 008): todas las filas que quedan en el POD.

**Mensaje después de Complete — PROVISIONAL (spec 015, D-22):** banner genérico con el formato
de los demás banners del POD: `SFC {sfc} completed` (NonSerialized) o `{N} SFCs completed`
(Serialized). Pendiente: reemplazarlo con el texto real cuando haya captura
(`docs/process.md` §8).

> **Diferencia con legacy:** el legacy implementaba un modal "Complete Section" + overlay "Log Buyoff" con credenciales (User ID + Password). El flujo real SAP es directo. Si alguna área requiere confirmación adicional (buyoff, firma), se documenta en su archivo de área.
>
> **Simplificación aceptada (validación con técnicos, 2026-10-12):** la WI de producción
> QD001001524 rev. 27 describe variantes de Complete (Log Buyoff en operaciones de 50 SFC o
> menos, firma electrónica al mover toda la orden, supervisor que completa por otro;
> `wi-produccion-disposables.md` → W17). El simulador entrena solo el flujo directo:
> seleccionar las piezas o el lote y presionar Complete.

---

## Paso 10 — Logout (specs 015 y 016)

**StepType:** `Logout` | **Calificación:** `docs/domain.md` → "Flujo estándar" (paso 10) y
"Qué cuenta como intento"

**Acceso:** ícono **Logout** del header del POD.

### Flujo del operador
1. Clic en el ícono **Logout** del header.
2. La limpieza al salir (spec 016): "You have logged off" en Edge → cerrar Edge → el script
   con el Code1 en el escritorio. Pantallas: `pre-sap-flow.md`.

### Reglas de la pantalla
- No cierra la sesión de la app; el resumen final sale al cerrar la consola del script.
- **Con la sesión ya cerrada** (resumen final): el ícono cierra la sesión de la app (D-21).
- **Guía de Guided:** el subpaso señala el ícono con la nota de la trainer ("Cierra sesión en
  MES siempre que dejes de usar activamente el sistema…").

**Procedimiento real (trainer de Wires, 2026-10-13):** cerrar sesión siempre que se deja la
estación: hubo CAPAs por Code1 compartidos y sesiones abiertas.

---

## Flujo completo documentado ✓

| Paso | StepType | Condicional |
|---|---|---|
| 0 — Ingreso a MES | `MesLogin` | No (spec 016; pantallas en `pre-sap-flow.md`) |
| 1 — Resource Setup | `ResourceSetup` | No |
| 2 — Line Clearance apertura | `LineClearance` | No |
| 3 — Search / Start Order | `Search` | No |
| 4 — Assembly | `Assembly` | RequiresAssembly |
| 5 — DC Collect | `DataCollection` | RequiresDataCollection |
| 6 — Closing Line Clearance | `LineClearance` | No (ver Paso 6) |
| 7 — SFC Split | `SFC_Split` | Solo NonSerialized |
| 8 — Log NC | `LogNC`, `LogNCFastRepair`, `LogNCExtendedRepair` | Las reparaciones, solo en áreas con Assembly (spec 014) |
| 9 — Complete | `Complete` | No |
| 10 — Logout | `Logout` | No (spec 015) |

Pesos de cada paso por guía: `docs/domain.md` → "Ponderación de pasos" (la fuente de verdad es
`TrainingSteps.Weight` en la BD).
