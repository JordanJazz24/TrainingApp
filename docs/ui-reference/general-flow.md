# UI Reference — General Training Flow

Documento acumulativo. Flujo común a todas las áreas. Se actualiza pantalla por pantalla conforme se analiza el SAP MES real.
Fuente de verdad para implementar Fase 6 (UserControls por tipo de paso).

> Documentos por área (`moldeo-flow.md`, etc.) solo registrarán lo que difiere de este flujo general.

---

## Dashboard Operacional (POD)

### Requisitos definitivos

**Header bar**
```
Production Operator Dashboard: Site - {site}, User - {userName}    [Home] [Power] [Avatar]
★ {operationName}    ★ {resourceName}    SFC: [input readonly → SearchModal]    Shop Order:    Status (SFC):
```

**Action buttons — una sola fila, 4 botones**
```
[Start]  [Assemble]  [DC Collection]  [Complete]
```

**Tabla POD — columnas (legacy, labels en inglés)**

| Column | Notes |
|---|---|
| CP | SFC identifier |
| Material Description | |
| Qty | Cantidad |
| Status | Con indicador de color (verde = Activo) |
| Process Lot | Lote proc. |
| Shop Order | Orden de fabricación |

Estado de fila seleccionada: highlight azul. Una sola fila seleccionable a la vez.

**Sidebar — orden definitivo**
1. SFC Split
2. Log NC
3. Work Instruction
4. Reports (con submenu expandible)
5. Activities (con submenu expandible)
6. Place Back in Queue

Estilo de botones: tarjetas cuadradas grandes con ícono centrado + label debajo.

**Footer (fijo, fondo)**
```
[Resource Setup]  [Tool List]
```

**Activities — submenu completo**
1. As-Built Configuration
2. Log Comment
3. SFC Place Hold
4. SFC Release Hold
5. Remove Future Hold
6. Change Equipment Status Tool
7. Time Sensitive Material Check In/Out
8. Data Collection Standalone

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
[Setup Type ▼]          [valor texto libre          ] [copy]
```
Setup Type opciones: `Shop Order`, `Tool Number`

**Estado 3 — después de Save**
- Banner verde superior izquierdo: `✓ Record Change`
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
9. Confirmar banner "Record Change"
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
- Aparece banner verde `✓ Record Change` en la parte superior del modal body
- Después de 5 segundos el banner se oculta automáticamente
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
| Record Change | Banner verde in-modal, modal queda abierto 5s | Texto flotante `position-fixed` |
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

**Chrome:** ventana popup con [↗][X]

**Estado 1 — antes de Retrieve**
```
[Data Collection Standalone]                          [↗][X]
────────────────────────────────────────  [Clear]
  Filter Operation:  {operationName}
  * Collect Data For: {resourceName}                  [Retrieve]
────────────────────────────────────────────────────────
  (tabla vacía — sin filas)
```

**Estado 2 — después de Retrieve**
```
[Data Collection Standalone]                          [↗][X]
────────────────────────────────────────  [Clear]
  Filter Operation:  {operationName}
  * Collect Data For: {resourceName}                  [Retrieve]
────────────────────────────────────────────────────────
  DC_LC_DONE                      DC Group/Vers.: DCGTM_LC/A
  Realizó la Limpieza de Linea?   {timestamp última colección}    [Collect]
────────────────────────────────────────────────────────
```

**Estado 3 — después de Save (regreso desde sub-modal)**
```
  dc group save   ← banner informativo, top del modal, desaparece a los 5s
  DC_LC_DONE      DC Group/Vers.: DCGTM_LC/A     {timestamp actualizado}    [Collect]
```

- El modal principal **permanece abierto** durante toda la interacción
- El banner "dc group save" se muestra al regresar del sub-modal tras un Save exitoso
- El operador cierra el modal principal con [X] cuando quiera

---

### Sub-modal: Data Collection Entry

Se abre **sobre** el modal principal (capas apiladas). El modal principal queda visible detrás.

```
[Data Collection Entry]                               [↗][X]
  Data Collection Entry
────────────────────────────────────────────────────────
  DC Group: DCGTM_LC/CA    Resource: {resourceName}
────────────────────────────────────────────────────────
  ★ La limpieza de linea se ejecuto?:
                                    Data Type: Boolean    0 files attached
    [ Seleccione ▼ ]
    ┌──────────────┐
    │ (blank)      │
    │ NO           │
    │ Si           │
    └──────────────┘
────────────────────────────────────────────────────────
               [Save]  [Cancel]
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
9. Confirmar banner "dc group save" (desaparece en 5s)
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
- Modal principal queda visible con banner "dc group save"
- Banner desaparece automáticamente a los 5s
- Score actualizado en el widget top-right del POD
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

**StepType:** `Search` | **StepWeight:** 15% (hereda peso de Assembly cuando RequiresAssembly=false) | **Validación:** secuencia pura (siempre true)

**Acceso:** directamente en el Dashboard — campo SFC en la search bar + botón [Start].

> En áreas Moldeo/R&C (NonSerialized, RequiresDataCollection), presionar [Start] lanza una secuencia de **4 modales secuenciales** que capturan el proceso de inicio completo. La tabla POD **permanece visible** detrás de los modales en todo momento.

### Campo SFC (search bar)

```
★ {operationName}    ★ {resourceName}    SFC: [________________________] [browse]
                                                ↑
                                    Editable. Acepta escaneo o escritura manual.
                                    Ícono browse a la derecha abre Browse Work List.
```

**Importante:** el campo SFC es **editable y acepta entrada directa** (escaneo o teclado). No es readonly.

**Validación del botón [Start]:** el botón solo permite avanzar si hay texto en el campo SFC. Si está vacío → SweetAlert warning y no avanza.

### Sub-modal: Browse Work List (ruta alternativa)

Se abre al hacer clic en el ícono browse cuando no es posible escanear.

```
[BROWSE WORK LIST - SFCS QUEUED - {operation},{resource},{userId}]    [↗][X]
─────────────────────────────────────────────────────────────────────────────
  SFC ↑    Material Description    Shop Order Schedule Start Date    Shop Order Planned Completion
  ─────────────────────────────────────────────────────────────────────────────
  {sfcCode}    {description}    {scheduleDate}    {plannedDate}
  ...
─────────────────────────────────────────────────────────────────────────────
                                                        [✓ OK]  [✗ Cancel]
```

- Un clic = selecciona fila (highlight)
- [OK] o doble clic = confirma selección, cierra modal, carga en POD
- [Cancel] = cierra sin cambios

---

### Secuencia de 4 modales (Moldeo/R&C — NonSerialized)

Se lanza al presionar [Start] con un SFC válido en el campo. Los modales son **secuenciales** — el operador no puede avanzar al siguiente sin completar el actual. La tabla POD permanece visible detrás de todos los modales.

#### Modal 1: ID del Molde

**Group CD:** `DCGTM_ID_MOLDE/A`

```
[Data Collection Entry]                                               [↗][X]
  DC Group: DCGTM_ID_MOLDE/A    Resource: {resourceName}
────────────────────────────────────────────────────────────────────────────
  ★ Ingrese el ID del molde:
    Data Type: Numeric
    [________________________________]
────────────────────────────────────────────────────────────────────────────
                                                    [Save]  [Cancel]
```

**Validaciones:**
| # | Validación | SweetAlert |
|---|---|---|
| 1 | Campo no vacío | "Debes ingresar el ID del molde." |
| 2 | Valor numérico | "El ID del molde debe ser un valor numérico." |

- [Cancel] cierra el modal y aborta la secuencia (operador regresa al POD sin avanzar el paso)
- [Save] válido → cierra Modal 1, abre Modal 2

---

#### Modal 2: Verificaciones de Etiquetas

**Group CD:** `DCGTM_VERIFICACIONES/A`

```
[Data Collection Entry]                                               [↗][X]
  DC Group: DCGTM_VERIFICACIONES/A    Resource: {resourceName}
────────────────────────────────────────────────────────────────────────────
  ★ ¿Se verificaron las etiquetas de calibración?
    Data Type: Boolean
    [ Seleccione ▼ ]  NO / Sí

  ★ ¿Se verificó D000128682 o D000207232?
    Data Type: Boolean
    [ Seleccione ▼ ]  NO / Sí

  ★ ¿Se verificó según D000128035 o D000207299?
    Data Type: Boolean
    [ Seleccione ▼ ]  NO / Sí
────────────────────────────────────────────────────────────────────────────
                                                    [Save]  [Cancel]
```

**Validaciones (se detiene en la primera que falle):**
| # | Validación | SweetAlert |
|---|---|---|
| 1 | Todas las preguntas respondidas (no en blanco) | "Debes responder todas las preguntas antes de guardar." |
| 2 | Todas las respuestas == "Sí" | "Todas las respuestas deben ser 'Sí' para continuar." |

- Responder "No" en cualquiera = SweetAlert warning, modal permanece abierto
- [Save] válido → cierra Modal 2, abre Modal 3

---

#### Modal 3: Parámetros Técnicos MSS

**Group CD:** `DCGTM_PARAMETROS_MSS_D000281359/A`

```
[Data Collection Entry]                                               [↗][X]
  DC Group: DCGTM_PARAMETROS_MSS_D000281359/A    Resource: {resourceName}
────────────────────────────────────────────────────────────────────────────
  ★ Cooling Time según MSS D000281359:          Data Type: Numeric    [___]
  ★ Velocidad de inyección según MSS D000281359: Data Type: Numeric   [___]
  ★ Intensidad de pad printing:                  Data Type: Numeric   [___]
  ★ Temperatura Zona 1:                          Data Type: Numeric   [___]
  ★ Temperatura Zona 2:                          Data Type: Numeric   [___]
  ★ Temperatura Zona 3:                          Data Type: Numeric   [___]
  ★ Temperatura Zona 4:                          Data Type: Numeric   [___]
  ★ Temperatura Zona 5:                          Data Type: Numeric   [___]
  ★ Temperatura Zona 6:                          Data Type: Numeric   [___]
  ★ Tiempo de curado de pad printing:            Data Type: Numeric   [___]
────────────────────────────────────────────────────────────────────────────
                                                    [Save]  [Cancel]
```

**Validaciones (se validan todos los campos, no solo el primero fallido):**
| # | Validación | SweetAlert |
|---|---|---|
| 1 | Todos los campos no vacíos | "Debes completar todos los parámetros antes de guardar." |
| 2 | Todos los valores numéricos | "Los valores de {campo} deben ser numéricos." |

- [Save] válido → cierra Modal 3, abre Modal 4

---

#### Modal 4: Verificación Final MSS

**Group CD:** `DCGTM_VERIFICACION_FINAL/A`

```
[Data Collection Entry]                                               [↗][X]
  DC Group: DCGTM_VERIFICACION_FINAL/A    Resource: {resourceName}
────────────────────────────────────────────────────────────────────────────
  ★ ¿Se verificó según D000128035 o D000207299?
    Data Type: Boolean
    [ Seleccione ▼ ]  NO / Sí
────────────────────────────────────────────────────────────────────────────
                                                    [Save]  [Cancel]
```

**Validaciones:**
| # | Validación | SweetAlert |
|---|---|---|
| 1 | Dropdown seleccionado (no en blanco) | "Debes seleccionar una respuesta." |
| 2 | Respuesta == "Sí" | "La respuesta debe ser 'Sí' para continuar." |

- [Save] válido → cierra Modal 4 → secuencia completa

---

### Finalización del Start

Tras cerrar Modal 4:
- La tabla POD continúa visible (nunca se ocultó)
- La orden seleccionada se carga en la tabla POD con su fila (SFC, Material Description, Qty, Status: Active, Process Lot, Shop Order)
- Banner informativo en la zona entre header y search bar: **"✓ Start process completed successfully"** (5 segundos, luego desaparece)
- El paso avanza en el servidor

---

### Flujo del operador (completo)
1. Enfocar campo SFC en la search bar
2a. Escanear SFC/etiqueta directamente
2b. (alternativa) Clic en ícono browse → Browse Work List → seleccionar fila → OK
3. Verificar que el campo SFC tenga valor
4. Clic en **[Start]** en la barra de acción
5. Modal 1 → ingresar ID del molde (numérico) → [Save]
6. Modal 2 → responder 3 preguntas de verificación (todas "Sí") → [Save]
7. Modal 3 → ingresar 10 parámetros MSS (todos numéricos) → [Save]
8. Modal 4 → responder verificación final ("Sí") → [Save]
9. Confirmar banner "Start process completed successfully"

---

### Reglas de negocio
- [Start] no permite avanzar si el campo SFC está vacío
- La tabla POD permanece visible en todo momento — los modales son overlay
- [Cancel] en cualquier modal aborta la secuencia sin avanzar el paso
- Todos los campos numéricos deben ser valores numéricos (no texto)
- Todas las preguntas booleanas deben responderse "Sí"
- La secuencia es estrictamente lineal (1→2→3→4), no se puede saltear

---

### Notas de implementación

`SearchControl.ascx` contiene los **4 modales** pre-renderizados en el DOM. La secuencia se maneja con un HiddenField `HfSubStep` que rastrean qué modal está activo.

**Modelo de PostBacks:**
- PostBack #1: `BtnStart_ServerClick` → valida SFC no vacío → carga `SearchControl.ascx` → abre Modal 1 (`showModalSafe('modalSearch1')`)
- Modal 1 [Save] = PostBack #2 → valida numérico → cierra Modal 1 → abre Modal 2
- Modal 2 [Save] = PostBack #3 → valida todas "Sí" → cierra Modal 2 → abre Modal 3
- Modal 3 [Save] = PostBack #4 → valida 10 numéricos → cierra Modal 3 → abre Modal 4
- Modal 4 [Save] = PostBack #5 → valida "Sí" → llama `OnStepSubmit` → banner éxito
- Todos los modales usan `data-bs-backdrop="static"` (no se cierran al hacer clic fuera)

---

### Resumen de validaciones

| Modal | Tipo Dato | Validación | SweetAlert |
|---|---|---|---|
| 1 — ID del Molde | Numérico | Campo no vacío + es número | Sí |
| 2 — Verificaciones | Boolean × 3 | Todas respondidas + todas "Sí" | Sí |
| 3 — Parámetros MSS | Numérico × 10 | Todos llenos + todos números | Sí |
| 4 — Verificación Final | Boolean | Respondida + "Sí" | Sí |

---

### Diferencias SAP real vs. Legacy
| Elemento | SAP real | Legacy |
|---|---|---|
| Campo SFC | Editable, acepta escaneo directo | Readonly, solo abría el modal |
| [Start] sin SFC | Bloqueado con SweetAlert | No validado |
| Modales de inicio | 4 secuenciales (Data Collection Entry) | 1 modal combinado con mezcla de preguntas |
| POD durante modales | Siempre visible (overlay) | Desaparecía al abrir modal |
| Banner éxito | "Start process completed successfully" 5s | No implementado |
| Browse Work List | Nombre · columnas en inglés | "BUSCAR POOL DE TRABAJO" en español |

---

---

## Paso 4 — Assembly (condicional: RequiresAssembly = true)

**StepType:** `Assembly` | **StepWeight:** 15% | **Validación:** conteo vs. `ExpectedComponentCount`

**Acceso:** [Assemble] en la barra de acción (solo visible/habilitado si RequiresAssembly = true para el área).

> **RequiresAssembly es independiente de SerializationType.** No derivar uno del otro.

### Trigger

El operador selecciona la fila del SFC o lote en la tabla POD → presiona **[Assemble]**.

```
Serialized:    SFC: 989600300020-451        (una pieza)
NonSerialized: SFC: [250 SELECTED]          (lote completo)
```

### Vista: Component List (reemplaza POD Work List)

Al presionar Assemble, la tabla principal cambia de título y contenido:

```
Component List                                                         [?] [X]
─────────────────────────────────────────────────────────────────────────────
  {componentCode/version}         Description: {description}
  {requiredQty}                   Rem Qty: {remaining}
                                                    [Assemble]  [Disassemble]
  ...
─────────────────────────────────────────────────────────────────────────────
  [|< First]  [<< Prev]  [Next >>]  [Last >|]       Rows X-Y of Z
```

- **Rem Qty**: decrece conforme se agregan componentes
- **[Assemble]** por fila: abre el modal para ese componente específico
- **[Disassemble]**: revierte un componente ya ensamblado (fuera del flujo de evaluación)
- Paginación si hay más componentes de los que caben en pantalla

### Modal: Assemble Components

Se abre automáticamente sobre el Component List para el primer componente pendiente.

**Estado vacío (antes de escaneo)**
```
[Assemble Components]                                                  [↗][X]
  Mode:    Choose                     Find component: [________________]
  Action:  ● Assembly  ○ Removal      Barcode:        [________________]
────────────────────────────────────────────────────────────────────────────
  Component Details                   Assembly Data
  (vacío)                             (vacío)
────────────────────────────────────────────────────────────────────────────
                              [✗ Return to POD]
```

**Estado con datos (después de escaneo)**
```
[Assemble Components]                                                  [↗][X]
  Mode:    Choose                     Find component: [________________]
  Action:  ● Assembly  ○ Removal      Barcode:        [________________]
────────────────────────────────────────────────────────────────────────────
  Component Details                   Assembly Data
  Component:   {partNumber} [copy]    ★ Batch Number: {batchNumber}
  Version:     {version}              (o ★ Material Document Number)
  Description: {description}
  Quantity:    {qty}
────────────────────────────────────────────────────────────────────────────
                      [✓ Add]    [✗ Return to POD]
```

- **Barcode**: campo de entrada del operador (escaneo del código 2D del componente)
- **Component Details**: se autocompletan desde el barcode (Part Number, Version, Description, Quantity)
- **Assembly Data**: ★ Batch Number o ★ Material Document Number — requerido, se autocompleta desde el barcode
- **[✓ Add]**: aparece solo cuando hay datos populados
- **[✗ Return to POD]**: siempre disponible como salida

### Flujo del operador
1. Seleccionar fila en POD → [Assemble]
2. Component List se muestra; Assemble Components modal abre en el primer componente
3. Escanear Barcode del código 2D → Component Details + Assembly Data autocompletan
4. [Add] → Rem Qty decrementa; modal avanza al siguiente componente
5. Repetir pasos 3-4 hasta completar todos los componentes
6. Último componente → [Add] → modal cierra automáticamente → POD Work List se restaura

### Reglas de negocio
- **No se valida el contenido del barcode** — el simulador valida solo que se complete el conteo (`ExpectedComponentCount`)
- `ExpectedComponentCount` vive en BD por área (catálogo), no hardcodeado en código
- [Add] solo disponible cuando hay datos en Component Details + Assembly Data
- El modal cierra automáticamente tras el último componente — no hay botón "Finish"
- El orden de Assembly en el flujo es después de Search/Start y antes de Data Collection, pero el motor respeta `TrainingStep.Order` — puede cambiar por área

### Diferencias SAP real vs. Legacy
| Elemento | SAP real | Legacy |
|---|---|---|
| Component List | Reemplaza POD Work List en la misma área | No implementado |
| Modal Assemble Components | Dos estados: vacío / con datos; Add condicional | No implementado |
| Paginación Component List | First / Prev / Next / Last + contador | No implementado |
| Botón Add | Solo aparece tras escaneo exitoso | No implementado |

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
- Paginación idéntica a Component List

### Modal: Data Collection Entry

```
[Data Collection Entry]                                               [↗][X]
  SFC: {sfcCode} ({current}/{total})
  DC Group: {DC_GROUP_CODE}
────────────────────────────────────────────────────────────────────────────
  ★ {pregunta/label}:
  Description: {descripción}              Data Type: {Text|Numeric|Boolean}    [Comments]
  [_____________input_____________]

  (repite por cada parámetro del grupo)
────────────────────────────────────────────────────────────────────────────
  [💾 Save]  [💾 Save Draft]  [🔴 Clear]  [✗ Return to POD]
```

**Contador `({current}/{total})`**: para lotes NonSerialized el modal itera por cada SFC del batch tras cada Save.

**Input por DataType:**
- `Text` → input libre
- `Numeric` → input numérico
- `Boolean` → dropdown (Sí / No)

**[Comments]**: botón opcional por parámetro — no validado en entrenamiento.

**Footer:**
| Botón | Comportamiento |
|---|---|
| Save | Guarda el grupo para el SFC actual; avanza al siguiente si hay más en el lote |
| Save Draft | Guarda parcialmente — no marca el grupo como completado |
| Clear | Limpia campos sin cerrar |
| Return to POD | Sale sin guardar |

### Flujo del operador
1. Seleccionar fila(s) en POD → [DC Collect]
2. Data Collection List se muestra con todos los grupos del área
3. Clic en [Collect] de un grupo → Data Collection Entry modal abre
4. Completar cada parámetro según su DataType
5. [Save] → si lote NonSerialized, modal avanza al siguiente SFC (2/29, 3/29...)
6. Tras completar todos los SFCs del grupo, modal cierra; Data Collection List actualiza el grupo
7. Repetir para cada grupo requerido

### Patrón compartido: DC Collect vs. Assembly

| Elemento | Assembly | DC Collect |
|---|---|---|
| Vista lista | Component List | Data Collection List |
| Fila | Component + Rem Qty + [Assemble][Disassemble] | DC Group + Collect Data At + [Collect] |
| Modal | Assemble Components | Data Collection Entry |
| Contexto modal | Component code / version | SFC (N/total) + DC Group |
| Footer modal | [Add] [Return to POD] | [Save] [Save Draft] [Clear] [Return to POD] |
| Iteración por SFC | No | Sí — (N/total) automático tras Save |
| Paginación en lista | Sí | Sí |

### Reglas de negocio
- Solo aplica si `RequiresDataCollection = true`
- Parámetros viven en BD (`DataCollectionParameters` por área), con `DataType`, `DisplayOrder`, `MinValue?`, `MaxValue?`
- Validación: Text = libre; Numeric = formato numérico; Boolean = true/false
- Save Draft no completa el grupo — paso no se marca como correcto hasta Save definitivo
- El paso finaliza cuando todos los grupos requeridos tienen Save (no Draft)

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

## Paso 7 — Log NC / SFC Split

**StepType:** `LogNC` | **StepWeight:** 15% | **Validación:** NC Code existe en catálogo del área

**Acceso:** Sidebar → [Log NC]. Para NonSerialized, Sidebar → [SFC Split] primero.

### Sub-flujo A: SFC Split (solo NonSerialized)

**Modal: SFC Split Plug-In (Auto ID)**
```
  SFC to Split:   {sfcCode}      ← readonly
  Available Qty:  {qty}          ← readonly
  ★ Qty to Split: [_____] [X]    ← requerido, numérico
  [Split]  [Close]
```
Confirmación en modal: `✓ New SFC {newCode} created with quantity {qty}`
Operador verifica el nuevo SFC en el POD antes de continuar.

### Vista: NC Selection (reemplaza POD Work List)

Layout de **2 columnas**. Cada celda:
```
  {NC_CODE}              [↑ Log NC]
  {descripción}
```
El botón [Log NC] es por código individual. NC Codes desde BD por área (`NCCodes`).

### Modal: NC Data Entry

```
[NC Data Entry]                                                        [↗][X]
  SFC: {sfcCode} ({current}/{total})
────────────────────────────────────────────────────────────────────────────
  ★ NC Code:   {codePreFilled}
  ★ Component: [________________] [copy]   ← requerido
    Comments:  [______________]            ← opcional
────────────────────────────────────────────────────────────────────────────
  [✓ Add]  [✓ Add-Done]  [✗ Return to POD]  [✓ Done]
```

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
- NC Code y Component requeridos; Comments opcional
- NC Codes desde catálogo BD por área — no hardcodeados
- [Add-Done] = flujo normal del paso; [Add] = múltiples NCs al mismo SFC
- El SFC field queda vacío en el POD tras el scrap

### Diferencias SAP real vs. Legacy
| Elemento | SAP real | Legacy |
|---|---|---|
| NC Selection | Reemplaza POD Work List, 2 columnas, [Log NC] por código | Modal flotante paginado (5 páginas) |
| NC Data Entry footer | Add · Add-Done · Return to POD · Done | Add · Add Done · Return POD · Finished |
| Confirmación scrap | Banner verde en POD "All of {SFC} scrapped" | Mensaje inline en modal |

---

---

## Paso 8 — Complete

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
| 7 — Log NC / SFC Split | `LogNC` | 15% | No (SFC Split solo NonSerialized) |
| 8 — Complete | `Complete` | 10% | No |

> **Sobre los pesos:** los pesos fijos definidos en `CLAUDE.md` (ResourceSetup 20% + LC 10% + Assembly 15% + DataCollection 30% + NC 15% + Complete 10% = 100%) aplican cuando todos los pasos están presentes. Cuando un paso condicional está ausente (ej. Assembly en Moldeo), su peso se redistribuye a pasos de secuencia pura como Search — esto ocurre en el seed de la guía (`TrainingStep.Weight`), no en el `ScoreCalculator`. Los pesos de cada instancia de guía siempre suman 1.00.
