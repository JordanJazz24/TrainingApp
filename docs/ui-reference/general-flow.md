# UI Reference — General Training Flow

Documento acumulativo. Flujo común a todas las áreas. Se actualiza pantalla por pantalla conforme se analiza el SAP MES real.
Fuente de verdad para implementar Fase 6 (UserControls por tipo de paso).

> Documentos por área (`moldeo-flow.md`, etc.) solo registrarán lo que difiere de este flujo general.

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

Fila seleccionada: fondo celeste y celda selectora azul, como el real. Un click selecciona una fila (Serialized:
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

---

## Paso 1 — Resource Setup

**StepType:** `ResourceSetup` | **StepWeight:** 20% | **Validación:** secuencia (contenido libre)

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
  (tabla vacía)
────────────────────────────────────────────────────────
                    [Save]  [Clear]
```

**Estado 2 — después de Retrieve**
- Setup State cambia a dropdown editable con "Set Up" seleccionado
- Toolbar habilitado
- Tabla muestra filas existentes (si las hay)

**Estructura de cada fila:**
```
[Setup Type ▼]          [valor texto libre             ⧉]
```
Setup Type opciones: `Shop Order`, `Tool Number`

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

### Reglas de negocio
- **Contenido libre** — no se valida Shop Order ni Tool Numbers contra catálogo
- **El orden de las filas es libre** — no se exige que Shop Order sea la primera fila
- **Setup State DEBE ser "Set Up"** antes de guardar
- **Exactamente 1 Shop Order** — ni 0 ni 2+
- **Al menos 1 Tool Number**
- **Mínimo 2 filas en total** (se cumple implícitamente con 1 SO + ≥1 TN)

### Validaciones del UserControl (lado cliente + servidor)

**Precondición de Save:**
- `[Save]` arranca **deshabilitado** — se habilita únicamente después de ejecutar `[Retrieve]`
- Retrieve sin ninguna fila adicional NO completa el paso

**Orden de validaciones al presionar Save (se detiene en la primera que falle):**

| # | Validación | Mensaje al operador |
|---|---|---|
| 1 | Setup State == "Set Up" | "El Setup State debe estar en 'Set Up' antes de guardar." |
| 2 | Al menos 1 fila existe | "No existen recursos creados. Agrega al menos un Shop Order y un Tool Number." |
| 3 | Total filas ≥ 2 | "Se requieren mínimo 2 recursos: 1 Shop Order y al menos 1 Tool Number." |
| 4 | Exactamente 1 Shop Order (0 filas SO) | "Debe existir exactamente 1 fila de tipo Shop Order." |
| 5 | Exactamente 1 Shop Order (>1 filas SO) | "Solo puede existir 1 fila de tipo Shop Order." |
| 6 | Al menos 1 Tool Number | "Debe existir al menos 1 fila de tipo Tool Number." |
| 7 | Todas las filas con valor no vacío | "Todas las filas deben tener un valor. Ninguna fila puede estar vacía." |

- Cada fallo muestra un **SweetAlert** (warning) y mantiene el modal abierto
- Un fallo NO marca el paso como completado ni cierra el modal
- Si todas las validaciones pasan → Save ejecuta el guardado

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

**StepType:** `LineClearance` | **StepWeight:** 10% | **Validación:** respuesta = "Sí" (true)

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
- [Save] solo procede si la selección es "Si" (ver validaciones abajo)

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

**Validación en el sub-modal al presionar Save (cliente + servidor):**

| # | Validación | Mensaje al operador |
|---|---|---|
| 1 | Dropdown seleccionado (no en blanco) | "Debes seleccionar una respuesta antes de guardar." |
| 2 | Respuesta == "Si" | "La respuesta debe ser 'Sí' para continuar. Seleccionar 'No' no permite avanzar el paso." |

- Cada fallo muestra un **SweetAlert** (warning); el sub-modal permanece abierto
- Si la respuesta es "No": el intento se registra como fallido en el servidor (penalización en modo Assessment), el paso no avanza — el operador debe corregir y reintentar
- Validación es idéntica en cliente (`OnClientClick`) y servidor (`BtnSave_Click`) — belt-and-suspenders

**Comportamiento post-Save exitoso:**
- Sub-modal se cierra (JS)
- Modal principal queda visible con "1 DC Groups saved" y la fecha del registro en la fila
- Banner desaparece automáticamente a los 5s
- Score actualizado en el header del POD
- El operador cierra el modal principal con [X] cuando quiera

---

### Reglas de negocio
- Seleccionar "No" = respuesta incorrecta, registra intento fallido
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

**StepType:** `Search` | **StepWeight:** 15% (hereda peso de Assembly cuando RequiresAssembly=false) | **Validación:** secuencia pura; en áreas con checklist (Moldeo) además las respuestas de LC Moldeo y la verificación final

**Acceso:** directamente en el Dashboard — campo SFC en la search bar.

> No abre modal propio. El operador carga la orden en el POD desde la pantalla principal.

### Campo SFC (search bar)

```
★ {operationName}    ★ {resourceName}    SFC: [________________________] [browse]
                                                ↑
                                    Editable. Acepta escaneo o escritura manual.
                                    Ícono browse a la derecha abre Browse Work List.
```

**Importante:** el campo SFC es **editable y acepta entrada directa** (escaneo o teclado). No es readonly. El legacy lo tenía readonly — corrección para el nuevo sistema.

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

### Reglas de negocio
- No se valida el SFC/Shop Order contra catálogo — cualquier valor es aceptado
- La tabla POD debe quedar con al menos una fila visible — estado vacío no es válido para continuar
- Bifurcación por SerializationType: Serialized = escaneo pieza por pieza; NonSerialized = selección de lote/batch (documentar en área específica)

### Implementación en el simulador (specs 005 y 008)
- `TxtSFC` es readonly: el SFC se elige solo vía Browse Work List (spec 005).
- **Con checklist** (`Areas.RequiresStartChecklist = 1`, hoy solo Moldeo): Start abre la
  secuencia de 4 modales de `moldeo-flow.md`.
- **Sin checklist** (el resto): Start es directo, sin modales; se califica solo por
  secuencia.
- **Serialized:** la orden tiene 3-5 piezas `{ShopOrder}-{sufijo}` (en `New`). Browse Work
  List permite multi-selección (click alterna la fila, sin columna Status) y `TxtSFC`
  muestra el SFC o `[N SELECTED]`. Start exige **todas** las piezas (si no, aviso sin
  intento), las pasa a `Active`, las carga en el POD y quedan seleccionadas.
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

**StepType:** `Assembly` | **StepWeight:** 15% | **Validación:** solo secuencia; el paso se completa con todo el BOM ensamblado (spec 008)

**Acceso:** [Assemble] en la barra de acción (paso presente solo si RequiresAssembly = true).

> **RequiresAssembly es independiente de SerializationType.** No derivar uno del otro.

> **Fuentes reales:** captura de la Component List de EEP (Catéteres) y fotos del modal
> Assemble Components (antes y después del escaneo) aportadas por el usuario el
> 2026-09-28; confirmación de un operador de Sensores: los códigos son los **números de
> parte del BOM de la operación** y varían por operación.

### Trigger

El operador tiene seleccionadas las filas del POD → presiona **[Assemble]**. En Serialized
tienen que estar **todas** las piezas (aviso sin intento si falta alguna; tras el Start ya
quedan seleccionadas). `TxtSFC` muestra `[N SELECTED]` (en la captura real: `[250 SELECTED]`).

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
- **★ Batch Number** = **Process Lot** de la orden (columna del POD), no el barcode.
- **[✔ Add]** aparece recién tras un escaneo válido.

### Flujo del operador
1. Con todas las filas seleccionadas → [Assemble] → Component List
2. [Assemble] en un componente → modal → escanear barcode (Enter)
3. [Add] → Rem Qty `0`; el modal cierra y la lista se actualiza
4. Repetir por cada componente; con el último, el paso se completa y vuelve el POD
   (banner "✓ Assembly completed successfully")

### Reglas de negocio
- **Un escaneo por componente** cubre toda su Qty y se aplica a todas las piezas
  seleccionadas ("Apply to all SFCs").
- **No se valida el barcode** contra el número de parte; barcode vacío o no numérico es
  aviso de formato, sin intento.
- Se califica **solo por secuencia** (S2). No existe `ExpectedComponentCount` (eliminado).
- El avance vive en Session: sobrevive a Return to POD / [X] y a F5.
- El orden de Assembly en el flujo es después de Search/Start y antes de Data Collection, pero el motor respeta `TrainingStep.Order` — puede cambiar por área

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

**StepType:** `DataCollection` | **StepWeight:** 30% | **Validación:** JSON `{"<paramId>": "<valor>"}` tipado por DataType

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

### Reglas de negocio
- Solo aplica si `RequiresDataCollection = true`
- Grupos en BD (`DataCollectionGroups` por área, con `IsRequired`); parámetros en
  `DataCollectionParameters` (con `DataCollectionGroupId`, `DataType`, `DisplayOrder`,
  `MinValue?`, `MaxValue?`)
- Validación del Save: tienen que venir **todos** los parámetros del grupo y solo esos;
  Numeric dentro de rango, Boolean = Sí, Date válida. Un campo vacío es intento fallido.
- Un Save incorrecto es intento fallido del paso, pero **conserva** el avance (grupos y
  piezas ya guardados). Un grupo ya guardado por la pieza actual no se puede volver a
  guardar: su [Collect] está deshabilitado y el servidor ignora un segundo Save.
- Save Draft no guarda nada (solo orienta al operador)
- El paso finaliza cuando todas las piezas guardaron sus grupos obligatorios (Moldeo:
  solo `DCGTM_FECHA_EXPIRACION_NIR/A`). El intento correcto se registra en ese momento.
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

**StepType:** `LineClearance` | **StepWeight:** 10% | **Validación:** respuesta = "Sí" (true)

> **Estructura y flujo idénticos al Paso 2.** Ver sección "Paso 2 — Line Clearance" para la documentación completa de modales, estados y reglas.

### Condicionalidad

| Escenario | Comportamiento |
|---|---|
| Área **con** Closing LC | El flujo ejecuta este paso antes de Log NC y Complete |
| Área **sin** Closing LC | El flujo omite este paso — el motor avanza directamente al Paso 7 |

La condicionalidad vive en el **catálogo de guías** (`TrainingGuide.Steps`): si el área no tiene un `TrainingStep` de tipo `LineClearance` en la posición de cierre, no existe en la secuencia. No requiere cambio de código.

---

---

## Paso 7 — SFC Split (solo NonSerialized)

**StepType:** `SFC_Split` | **StepWeight:** 5% | **Validación:** al menos un split exitoso

**Acceso:** seleccionar una fila del POD → Sidebar → [SFC Split]. Las guías Serialized omiten este paso.

**Modal: SFC Split Plug-In (Auto ID)**
```
  SFC to Split:   {sfcCode}      ← readonly
  Available Qty:  {qty}          ← readonly
  ★ Qty to Split: [_____] [X]    ← requerido, numérico
  [Split]  [Close]
```
Confirmación en modal: `✓ New SFC {newCode} created with quantity {qty}`
Operador verifica la nueva fila en el POD antes de continuar.

- [Split] muta el POD sin registrar intento ni avanzar el TrainingStep.
- La X superior cierra el modal para seleccionar otra fila original o derivada.
- [Close] se habilita después del primer split exitoso y completa el paso.

---

## Paso 8 — Log NC

**StepType:** `LogNC` | **StepWeight:** 10% | **Validación:** NC Code existe en catálogo del área

**Acceso:** Sidebar → [Log NC].

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
                    Component: [                       ⧉]   ← requerido si RequiresNCComponent
                     Comments: [                         ]   ← área de texto, opcional
────────────────────────────────────────────────────────────────────────────
        [✓ Add]  [✓ Add-Done]  [✖ Return to POD]  [✓ Done]   ← grises con ícono
```

- Etiquetas a la derecha y campos a la izquierda, centrados. **Component sin asterisco**,
  como el real; sigue siendo obligatorio en áreas con `RequiresNCComponent = 1`.
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

### Reglas de negocio
- SFC Split solo para NonSerialized — Serialized selecciona directamente del POD
- NC Code y Component requeridos (Component solo si `RequiresNCComponent`); Comments opcional
- NC Codes desde catálogo BD por área — no hardcodeados
- [Add-Done] = flujo normal del paso; [Add] y [Done] son botones equivocados (intento fallido, spec 007)
- **NonSerialized:** una fila, creada por el SFC Split. **Serialized (spec 008):** ≥1 fila;
  todas las seleccionadas se scrapean con el mismo código; tiene que quedar al menos una
  pieza para Complete (seleccionar todas = aviso sin intento). Sin fila = intento fallido.
- Banner: "✓ All of {SFC} scrapped" (una) o "✓ N SFCs scrapped" (varias). En Serialized
  el SFC field queda vacío; en NonSerialized conserva el SFC del lote.

### Diferencias SAP real vs. Legacy
| Elemento | SAP real | Legacy |
|---|---|---|
| NC Selection | Reemplaza POD Work List, 2 columnas, [Log NC] por código | Modal flotante paginado (5 páginas) |
| NC Data Entry footer | Add · Add-Done · Return to POD · Done | Add · Add Done · Return POD · Finished |
| Confirmación scrap | Banner verde en POD "All of {SFC} scrapped" | Mensaje inline en modal |

---

---

## Paso 9 — Complete

**StepType:** `Complete` | **StepWeight:** 10% | **Validación:** secuencia pura (siempre true)

**Acceso:** [Complete] en la barra de acción.

> Paso más directo del flujo — sin modal propio, sin sub-ventanas.

### Flujo del operador
1. Seleccionar fila(s) del SFC u orden de planta en el POD Work List
2. Clic en **[Complete]** en la barra de acción
3. El sistema completa la orden y los SFCs seleccionados
4. **Fin del proceso**

### Reglas de negocio
- No valida contenido — se califica por secuencia
- El operador debe saber que este es el paso final del cierre
- Todos los pasos anteriores deben estar completos antes de llegar aquí
- Sin fila seleccionada = intento fallido (spec 007). NonSerialized: una fila.
  **Serialized (spec 008):** todas las filas que quedan en el POD (algunas = aviso sin intento).

> **Diferencia con legacy:** el legacy implementaba un modal "Complete Section" + overlay "Log Buyoff" con credenciales (User ID + Password). El flujo real SAP es directo. Si alguna área requiere confirmación adicional (buyoff, firma), se documenta en su archivo de área.

---

## Flujo completo documentado ✓

| Paso | StepType | Weight estándar | Condicional |
|---|---|---|---|
| 1 — Resource Setup | `ResourceSetup` | 20% | No |
| 2 — Line Clearance apertura | `LineClearance` | 10% | No |
| 3 — Search / Start Order | `Search` | 0% (secuencia pura) | No — absorbe peso de pasos ausentes a nivel de guía |
| 4 — Assembly | `Assembly` | 15% | RequiresAssembly |
| 5 — DC Collect | `DataCollection` | 30% | RequiresDataCollection |
| 6 — Closing Line Clearance | `LineClearance` | 10% | Condicional por área |
| 7 — SFC Split | `SFC_Split` | 5% | Solo NonSerialized |
| 8 — Log NC | `LogNC` | 10% | No |
| 9 — Complete | `Complete` | 10% | No |

> **Sobre los pesos:** los pesos fijos definidos en `CLAUDE.md` (ResourceSetup 20% + LC 10% + Assembly 15% + DataCollection 30% + NC 15% + Complete 10% = 100%) aplican cuando todos los pasos están presentes. Cuando un paso condicional está ausente (ej. Assembly en Moldeo), su peso se redistribuye a pasos de secuencia pura como Search — esto ocurre en el seed de la guía (`TrainingStep.Weight`), no en el `ScoreCalculator`. Los pesos de cada instancia de guía siempre suman 1.00.
