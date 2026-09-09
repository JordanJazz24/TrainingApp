# Spec 004: Fase 6 — Dashboard Operacional y UserControls por StepType

**Status:** Approved (2026-09-09)
**Date:** 2026-09-08

---

## Problem

`GuidedTest.aspx` (Fase 4) presenta un input genérico por paso que no refleja la
interfaz real del SAP MES. Los operadores no pueden practicar en un entorno visualmente
fiel al sistema que usarán en piso. Fase 6 reimplementa el dashboard como un POD
(Production Operator Dashboard) realista, con un UserControl `.ascx` por cada tipo de
paso — sin lógica de negocio en los controles.

---

## Scope

### In

- Reescritura completa del markup de `GuidedTest.aspx` como POD fiel al SAP MES real
  (header bar, search section, action buttons, tabla POD, sidebar, footer).
- Un UserControl `.ascx` por cada `StepType`:
  `ResourceSetupControl`, `LineClearanceControl`, `SearchControl`, `AssemblyControl`,
  `DataCollectionControl`, `LogNCControl`, `CompleteControl`.
- Secuencia especial de 3 modales para el Start de Moldeo (R&C), controlada por
  `StepViewModel.CurrentSubStep`.
- Widget de score en tiempo real + badge de modo (esquina superior derecha).
- Botón Abandon fijo en esquina inferior izquierda con confirmación SweetAlert2.
- Panel de completion que reemplaza el POD al finalizar la sesión (Paso 8).
- Feedback visual de intento incorrecto vía SweetAlert2.
- Notificación de force-advance (Assessment, intento 4+) vía SweetAlert2.
- Redirección automática a `StartSession.aspx` si no hay sesión activa.
- Postback completo con re-apertura de modales vía `RegisterStartupScript`.

### Out

- Lógica funcional de Work Instructions, Reports, Activities, Tool List,
  Place Back in Queue — botones visibles pero deshabilitados.
- Cualquier cambio en BLL, DAL o Domain.
- Nuevas áreas (LoFlo u otras).
- Dashboard de instructor o reportes de progreso.
- Pruebas de integración de UI (las páginas ASPX no se prueban directamente en
  .NET FW — ver Tech Stack en `CLAUDE.md`).

---

## Domain Model

### StepViewModel

Objeto que el code-behind de `GuidedTest.aspx` construye y asigna como propiedad
pública al UserControl activo antes de cada render.

```csharp
public class StepViewModel
{
    public int     StepId          { get; set; }
    public StepType StepType       { get; set; }
    public int     StepOrder       { get; set; }
    public string? Hint            { get; set; }   // texto de ayuda mostrado en feedback
    public bool?   IsCorrect       { get; set; }   // null = primer intento aún sin responder
    public int     AttemptCount    { get; set; }
    public int     CurrentSubStep  { get; set; }   // sub-paso activo (Moldeo Start: 1|2|3)
    public Dictionary<string, object> ExtraData { get; set; } = new();
}
```

**ExtraData por StepType:**

| StepType | Clave | Tipo | Descripción |
|---|---|---|---|
| `LineClearance` | `"QuestionText"` | `string` | Texto de la pregunta LC (hardcodeado por área) |
| `Assembly` | `"Components"` | `List<DataCollectionParameter>` | Lista de componentes a escanear |
| `Assembly` | `"ExpectedCount"` | `int` | Cantidad esperada |
| `DataCollection` | `"Parameters"` | `List<DataCollectionParameter>` | Parámetros del grupo DC |
| `LogNC` | `"NCCodes"` | `List<NCCode>` | Códigos NC del área |
| `LogNC` | `"IsNonSerialized"` | `bool` | Activa sub-flujo SFC Split |
| `ResourceSetup` | — | — | Sin ExtraData (captura libre) |
| `Search` | — | — | Sin ExtraData |
| `Complete` | — | — | Sin ExtraData |

Para `StepType.Search` en Moldeo con `CurrentSubStep > 0`, `ExtraData` incluye
además los parámetros correspondientes al sub-paso activo (ver sección Moldeo).

### Contrato de cada UserControl

Cada control es un `.ascx` + code-behind `.ascx.cs`. Expone:

```csharp
// propiedad de entrada — asignada por GuidedTest.aspx antes del render
public StepViewModel ViewModel { get; set; }

// GuidedTest.aspx suscribe a este evento para recibir la respuesta
public event EventHandler<StepSubmitEventArgs> OnStepSubmit;

public class StepSubmitEventArgs : EventArgs
{
    public string Answer { get; set; }  // JSON string o valor simple
}
```

El control **nunca** llama directamente a BLL ni a DAL. Solo renderiza y emite.

---

## Layout del Dashboard Operacional (POD)

Fuente de verdad visual: `docs/ui-reference/general-flow.md` sección
"Dashboard Operacional".

```
┌─────────────────────────────────────────────────────────────┐  ← header bar
│  Production Operator Dashboard: Site - {site}, User - {user}│
│                                         [Score widget] [⌂][⏻]│
├─────────────────────────────────────────────────────────────┤  ← search section
│  ★ {operationName}  ★ {resourceName}  SFC:[______] SO: Sta: │
├──────────────────────────────────────┬──────────────────────┤
│  [Start] [Assemble] [DC Collection] [Complete]              │  ← action buttons
├──────────────────────────────────────┤  sidebar col-2       │
│                                      │  [SFC Split]   (dis) │
│  POD Work List / ActiveControl       │  [Log NC]      (dis) │
│  (col-10)                            │  [Work Instr.] (dis) │
│                                      │  [Reports]     (dis) │
│                                      │  [Activities]  (dis) │
│                                      │  [Place Back]  (dis) │
├──────────────────────────────────────┴──────────────────────┤
│  [Resource Setup]  [Tool List (dis)]                        │  ← footer fijo
└─────────────────────────────────────────────────────────────┘
   [Abandon] ← fijo esquina inferior izquierda
```

**Botones deshabilitados en esta fase:** Tool List, Work Instructions, Reports,
Activities, Place Back in Queue. Visibles con estilo `disabled` — no ocultos.

**Botones habilitados condicionalmente:**
- Action buttons (Start, Assemble, DC Collection, Complete): habilitado solo el
  correspondiente al paso activo; los demás deshabilitados.
- SFC Split y Log NC (sidebar): habilitados solo cuando el paso activo es `LogNC`.
- Resource Setup (footer): habilitado solo cuando el paso activo es `ResourceSetup`.

### Score Widget (esquina superior derecha)

```
┌────────────────────────┐
│  Score: 72%            │
│  [Assessment]          │  ← badge de modo: "Guided" | "Assessment"
└────────────────────────┘
```

Fijo `position-fixed top-right`. Actualizado en cada PostBack via `Label` de servidor.

---

## Ciclo de vida de un PostBack

```
Operador interactúa con UserControl
    ↓
PostBack → GuidedTest.aspx code-behind
    ↓
Lee Answer del EventArgs del control activo
    ↓
TrainingSessionService.SubmitStep(sessionId, stepId, answer)
    → devuelve StepSubmitResult { IsCorrect, CurrentScore, ForceAdvance, NextStepId? }
    ↓
if IsCorrect || ForceAdvance:
    if ForceAdvance: RegisterStartupScript → SweetAlert "Paso fallido — sistema avanza"
    if NextStepId == null: mostrar panel de Completion
    else: construir StepViewModel para NextStep → cargar control siguiente
else:
    construir StepViewModel con IsCorrect=false, AttemptCount++
    RegisterStartupScript → SweetAlert con Hint
    if modal estaba abierto: RegisterStartupScript → re-abrir modal
```

---

## UserControls — Especificación por tipo

### ResourceSetupControl

**Archivo:** `TrainingApp/UI/Controls/Training/ResourceSetupControl.ascx`
**Acceso:** Footer → [Resource Setup] → abre modal Bootstrap

**Estados del modal:**
1. **Antes de Retrieve**: Setup State = texto plano "Open"; toolbar deshabilitado.
2. **Después de Retrieve**: Setup State = dropdown editable ("Set Up" seleccionado);
   toolbar habilitado; tabla de filas editable.
3. **Después de Save**: banner verde "✓ Record Change" en el modal.

**Estructura de cada fila:**
```
[Setup Type ▼]  [valor texto libre]  [copy]
```
Setup Type opciones: `Shop Order`, `Tool Number`.

**Answer emitido:**
```json
{
  "setupState": "Set Up",
  "rows": [
    { "type": "Shop Order", "value": "..." },
    { "type": "Tool Number", "value": "..." }
  ]
}
```

**Validación:** secuencia pura — el servidor acepta cualquier contenido (`StepValidator`
devuelve `true` independientemente del valor).

---

### LineClearanceControl

**Archivo:** `TrainingApp/UI/Controls/Training/LineClearanceControl.ascx`
**Acceso:** Sidebar → Activities → Data Collection Standalone

**Estructura del modal (Data Collection Standalone + sub-modal Data Collection Entry):**
- Retrieve muestra la fila `DC_LC_DONE` con botón [Collect].
- [Collect] abre sub-modal con dropdown "Sí / No" para la pregunta del área.
- Save cierra sub-modal; banner "◆ 1 DC Groups saved" en Standalone.

La pregunta (`ExtraData["QuestionText"]`) es el texto del área activa:
- Flujo general: "¿La limpieza de linea se ejecuto?"
- Moldeo Step 3.1 (sub-pasos 2-3): preguntas específicas LC Moldeo
  (hardcodeadas en `StepValidator` por AreaId).

**Answer emitido:** `"true"` | `"false"`

---

### SearchControl

**Archivo:** `TrainingApp/UI/Controls/Training/SearchControl.ascx`
**Acceso:** Campo SFC en search bar (editable) + botón browse opcional

**Flujo estándar (flujo general):**
- Campo SFC editable. Browse Work List modal si no puede escanear.
- POD Work List se puebla tras submit.
- Si la orden no auto-inicia: botón [Start] en action buttons.

**Flujo Moldeo (`CurrentSubStep` ≥ 1):**

| SubStep | Modal mostrado | ExtraData extra |
|---|---|---|
| 1 | Data Collection Entry — ID del molde (DCGTM_ID_MOLDE/A) | `DCParam` del parámetro ID Molde |
| 2 | Data Collection Entry — 3 preguntas LC boolean (DCGTM_LC_MOLDEO/A) | textos de las 3 preguntas |
| 3 | Data Collection Entry — Validación IDMold & IDIMM | texto de la pregunta |

Tras completar SubStep 3: banner en zona entre header y search section:
```
✓ Start process completed successfully
```
El servidor avanza al Paso 5 (DataCollection).

**Answer emitido (SubStep = 0):** `{ "sfcCode": "..." }`
**Answer emitido (SubStep 1-3):** JSON del sub-paso activo (texto libre o boolean).

---

### AssemblyControl

**Archivo:** `TrainingApp/UI/Controls/Training/AssemblyControl.ascx`
**Acceso:** Action button [Assemble]

**Vista Component List** (reemplaza POD Work List):
- Columnas: componente/versión, Description, Qty, Rem Qty, [Assemble] [Disassemble].
- Paginación: First / Prev / Next / Last + contador "Rows X-Y of Z".

**Modal Assemble Components:**
- Estado vacío (antes de escaneo): campos `Find component` y `Barcode` vacíos.
- Estado con datos (tras escaneo): Component Details y Assembly Data autocompletan.
  Botón [Add] aparece solo cuando hay datos.
- Tras [Add]: Rem Qty decrementa; modal avanza al siguiente componente.
- Último componente → modal cierra automáticamente → POD Work List restaurado.

**Answer emitido:**
```json
{ "scannedCount": 3 }
```
El servidor valida `scannedCount == ExpectedComponentCount`.

---

### DataCollectionControl

**Archivo:** `TrainingApp/UI/Controls/Training/DataCollectionControl.ascx`
**Acceso:** Action button [DC Collection]

**Vista Data Collection List** (reemplaza POD Work List):
- Columnas: DC Group Code, Description, Collect Data At, [Collect].
- Paginación idéntica a Component List.

**Modal Data Collection Entry:**
- Header: `SFC: {sfcCode} ({current}/{total})` + DC Group.
- Por cada parámetro (`ExtraData["Parameters"]`): label ★, DataType, input correspondiente.
  - `Text` → `<input type="text">`
  - `Numeric` → `<input type="number">`
  - `Boolean` → `<select>` con opciones: (blank) | NO | Sí
- Botón [Comments] por parámetro (visual, no validado en entrenamiento).
- Footer: [💾 Save] [💾 Save Draft] [🔴 Clear] [✗ Return to POD].
- Save Draft no completa el paso — paso no se marca correcto hasta Save definitivo.
- Lotes NonSerialized: modal itera por cada SFC del batch tras cada Save (contador actualiza).

**Answer emitido:**
```json
{ "paramId1": "valor1", "paramId2": "valor2", ... }
```
Las claves son los `DataCollectionParameter.Id` del área.

---

### LogNCControl

**Archivo:** `TrainingApp/UI/Controls/Training/LogNCControl.ascx`
**Acceso:** Sidebar → [SFC Split] (si NonSerialized) → Sidebar → [Log NC]

**Sub-flujo SFC Split (solo si `ExtraData["IsNonSerialized"] == true`):**

Modal SFC Split:
```
SFC to Split:   {sfcCode}     ← readonly
Available Qty:  {qty}         ← readonly
★ Qty to Split: [_____]       ← requerido, numérico
[Split]  [Close]
```
Confirmación en modal: `✓ New SFC {newCode} created with quantity {qty}`.
Operador verifica nuevo SFC en POD → cierra → continúa a NC Selection.

**Vista NC Selection** (reemplaza POD Work List):
- Layout 2 columnas. Cada celda: `{NC_CODE}` + descripción + [Log NC].
- NC Codes desde `ExtraData["NCCodes"]` (catálogo BD por área).

**Modal NC Data Entry:**
```
SFC: {sfcCode} ({current}/{total})
★ NC Code:   {codePreFilled}
★ Component: [________________]   ← requerido
  Comments:  [______________]     ← opcional
[✓ Add]  [✓ Add-Done]  [✗ Return to POD]  [✓ Done]
```
- [Add-Done]: flujo normal (registra y cierra).
- [Add]: permite agregar múltiples NCs al mismo SFC.
- Confirmación en POD tras scrap: banner verde `✓ All of {SFCCode} scrapped`.
- Campo SFC en search bar queda vacío tras el scrap.

**Answer emitido:**
```json
{
  "sfcSplit": { "qty": 5 },
  "ncEntries": [
    { "ncCodeId": "...", "component": "...", "comments": "..." }
  ]
}
```

---

### CompleteControl

**Archivo:** `TrainingApp/UI/Controls/Training/CompleteControl.ascx`
**Acceso:** Action button [Complete]

Sin modal. El operador selecciona fila(s) en POD Work List → [Complete].
Un solo PostBack completa el paso.

**Answer emitido:** `"complete"`

---

## Panel de Completion

Reemplaza el contenido principal del POD (col-10) cuando el Paso 8 se completa.
El sidebar y el footer dejan de ser visibles.

```
┌──────────────────────────────────────────┐
│         Session Complete                 │
│                                          │
│  Score:    87%                           │
│  Mode:     Assessment                    │
│  Area:     Moldeo                        │
│  Product:  R&C                           │
│                                          │
│  Result:   ✓ APROBADO   (Assessment)     │
│            —            (Guided)         │
│                                          │
│          [Start New Session]             │
└──────────────────────────────────────────┘
```

[Start New Session] redirige a `StartSession.aspx`.

---

## Feedback con SweetAlert2

SweetAlert2 ya está disponible en el proyecto (referenciado en `master.Master`).

| Evento | Tipo SweetAlert | Mensaje |
|---|---|---|
| Intento incorrecto | `warning` | `Hint` del `StepViewModel` |
| Force-advance (Assessment intento 4+) | `error` | "Paso fallido — el sistema avanza automáticamente" |
| Abandon confirmación | `question` | "¿Abandonar la sesión? El progreso se perderá." |

Los modales Bootstrap se re-abren después del PostBack via:
```csharp
ScriptManager.RegisterStartupScript(this, GetType(), "reopenModal",
    "$(document).ready(function(){ $('#modalId').modal('show'); });", true);
```

---

## Authorization

- `GuidedTest.aspx` requiere autenticación (`FormsAuthentication`). Configurado en
  `Web.config` — no requiere cambio.
- Si el operador accede sin `Session["OperatorId"]` o sin sesión de entrenamiento activa:
  redirección automática a `StartSession.aspx` en `Page_Load`.

---

## Edge Cases & Failure Modes

| Caso | Comportamiento |
|---|---|
| Sin sesión activa al cargar GuidedTest.aspx | Redirect a `StartSession.aspx` |
| Intento incorrecto (Guided) | SweetAlert `warning`, paso permanece activo sin penalización |
| Intento incorrecto (Assessment) | SweetAlert `warning`, paso bloqueado hasta acierto o intento 4 |
| Force-advance (intento 4+ en Assessment) | SweetAlert `error` "Paso fallido", avanza al siguiente paso |
| Modal Bootstrap tras PostBack | Re-apertura via `RegisterStartupScript` |
| Save Draft en DataCollection | Paso NO se marca correcto; operador debe hacer Save definitivo |
| Abandon confirmado | `TrainingSessionService.Abandon()` → redirect a `StartSession.aspx` |
| Paso condicional ausente (ej. Assembly en Moldeo) | El motor ya no incluye el paso en la guía — no requiere manejo especial en UI |

---

## Legacy Reference

El directorio `/MES Simulator Legacy` es una fuente válida de elementos reutilizables
para esta fase. En particular:

- **CSS / clases Bootstrap**: `myStyles.css`, `bootstrap.min.css` — reutilizar estilos
  del POD (`.header-bar`, `.action-btn`, `.sidebar-btn`, `.bottom-btn`, `.search-section`)
  cuando sean equivalentes al diseño SAP real documentado.
- **Markup de modales**: los modales en `main.aspx` (#Resource-setup-modal,
  #modalColeccionDatos, #modalSplit, #log_NC_Modal, #searchModal, etc.) son referencia
  de estructura HTML — reutilizar donde el diseño coincida; adaptar donde el SAP real difiere.
- **Íconos**: directorio `master/imgs/` — todos los iconos existentes
  (`resource-setup-icon.png`, `split-icon.png`, `log_NC-Icon.png`, etc.) son reutilizables.
- **JavaScript**: `master.js` y las referencias a SweetAlert2, Select2, Bootstrap JS
  ya disponibles — no agregar librerías nuevas si el legacy ya las tiene.

**Regla:** reutilizar markup/CSS/JS del legacy cuando coincida con el diseño SAP real.
No reutilizar patrones de arquitectura (WebMethod, DataSet, sessionStorage, lógica en code-behind).

---

## Non-Functional Requirements

- Tiempo de respuesta por PostBack: < 500 ms en servidor local/intranet.
- Compatible con los navegadores internos del site (IE11+ / Edge / Chrome).
- SweetAlert2 ya referenciado — sin nuevas dependencias JS.
- Bootstrap y Bootstrap Icons ya disponibles — sin cambios de CSS framework.

---

## Integrations

Ninguna integración externa nueva. La UI consume únicamente:
- `TrainingSessionService` (ya implementado, Fase 4)
- `TrainingGuideService` para obtener el `StepViewModel` inicial
- Catálogos ya en BD: `NCCodes`, `DataCollectionParameters` (ya en DAL, Fase 3)

---

## Acceptance Criteria

**AC-1 — Layout del POD**
> Dado que el operador tiene una sesión activa y accede a GuidedTest.aspx,
> cuando la página carga,
> entonces renderiza: header bar, search section, action buttons (4), tabla POD,
> sidebar (6 ítems), footer (Resource Setup + Tool List), widget de score,
> botón Abandon.

**AC-2 — Botones deshabilitados**
> Dado el POD cargado,
> cuando el paso activo es `ResourceSetup`,
> entonces solo el botón Resource Setup (footer) está habilitado;
> Assemble, DC Collection, Complete, Tool List, Work Instructions, Reports,
> Activities, Place Back in Queue están deshabilitados visualmente.

**AC-3 — Resource Setup correcto**
> Dado que el paso activo es ResourceSetup,
> cuando el operador completa el modal (Retrieve → Set Up → filas → Save) y hace submit,
> entonces el servidor marca el paso como correcto y el POD avanza al Paso 2.

**AC-4 — Line Clearance correcto**
> Dado que el paso activo es LineClearance,
> cuando el operador selecciona "Sí" en el dropdown y hace Save,
> entonces el servidor marca el paso como correcto.

**AC-5 — Line Clearance incorrecto**
> Dado que el paso activo es LineClearance,
> cuando el operador selecciona "No" y hace Save,
> entonces aparece SweetAlert `warning` con el hint, el paso permanece activo.

**AC-6 — Search / Start Order**
> Dado que el paso activo es Search,
> cuando el operador ingresa un SFC y hace submit,
> entonces el servidor marca el paso como correcto (secuencia pura) y avanza.

**AC-7 — Moldeo Start: secuencia de 3 modales**
> Dado que el área es R&C/Moldeo y el paso activo es Search,
> cuando el operador ingresa el SFC y presiona [Start],
> entonces se abre el Modal 1 (ID del molde).
> Cuando completa Modal 1 y hace Save, se abre Modal 2 (3 preguntas LC boolean).
> Cuando completa Modal 2 y hace Save, se abre Modal 3 (IDMold & IDIMM).
> Cuando completa Modal 3 y hace Save, aparece el banner
> "✓ Start process completed successfully" y el POD avanza al Paso 5.

**AC-8 — DataCollection completo**
> Dado que el paso activo es DataCollection,
> cuando el operador completa todos los parámetros requeridos y hace Save (no Draft),
> entonces el servidor marca el paso como correcto.

**AC-9 — DataCollection Save Draft no avanza**
> Dado que el paso activo es DataCollection,
> cuando el operador hace Save Draft,
> entonces el paso NO se marca correcto y permanece activo.

**AC-10 — LogNC NonSerialized: SFC Split requerido**
> Dado que el área es NonSerialized y el paso activo es LogNC,
> cuando el operador abre Log NC sin hacer SFC Split primero,
> entonces la UI dirige primero al modal SFC Split (botón SFC Split activo en sidebar).

**AC-11 — LogNC: NC Code y Component requeridos**
> Dado el modal NC Data Entry abierto,
> cuando el operador intenta Add-Done sin completar NC Code o Component,
> entonces el formulario no permite el submit (validación client-side o server-side).

**AC-12 — Complete: panel de completion**
> Dado que el paso activo es Complete,
> cuando el operador presiona [Complete],
> entonces el servidor marca la sesión como completada y el POD es reemplazado
> por el panel de Completion con Score, Modo, Área, Producto y botón Start New Session.

**AC-13 — Score widget en tiempo real**
> Dado que el operador completa pasos correctamente,
> cuando el servidor devuelve `CurrentScore`,
> entonces el widget de score en la esquina superior derecha muestra el valor actualizado.

**AC-14 — Force-advance en Assessment**
> Dado que el modo es Assessment y el operador ha agotado 4 intentos en un paso,
> cuando el servidor devuelve `ForceAdvance = true`,
> entonces aparece SweetAlert `error` "Paso fallido — el sistema avanza automáticamente"
> y el POD carga el siguiente paso.

**AC-15 — Abandon con confirmación**
> Dado que el operador hace clic en [Abandon],
> cuando confirma en el SweetAlert de confirmación,
> entonces `TrainingSessionService.Abandon()` es llamado y el operador
> es redirigido a `StartSession.aspx`.

**AC-16 — Sin sesión activa**
> Dado que un operador accede a GuidedTest.aspx sin sesión de entrenamiento activa,
> entonces `Page_Load` redirige a `StartSession.aspx`.

---

## Deferred Decisions

| Decisión | Fallback elegido | Revisitar cuando |
|---|---|---|
| Diseño visual exacto del score widget | Widget `position-fixed` top-right con Bootstrap card pequeña | Fase 8 (validación con trainer) |
| Contenido exacto de las etiquetas de las 3 preguntas LC Moldeo | Hardcodeado en `StepValidator` por AreaId — textos confirmados en `docs/ui-reference/moldeo-flow.md` | Si el negocio cambia las preguntas |
| Resultado "Aprobado/Reprobado" en Assessment — umbral mínimo | Fuera de scope Fase 6; el panel de completion muestra el score pero no calcula aprobación | Fase 9 o cuando el negocio defina el umbral |
| Funcionalidad de Reports, Activities, Work Instructions | Deshabilitados en Fase 6 | Fases futuras post-MVP |

---

## Open Questions

_(ninguna — lista vacía requerida para aprobación)_
