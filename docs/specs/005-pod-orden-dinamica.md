# Spec 005: POD — Captura Dinámica de Orden (Resource Setup → Search → DC → Split)

**Status:** Approved (2026-09-24) — aprobada retroactivamente al cierre de la Fase 6; ya estaba implementada y en uso
**Date:** 2026-09-18

---

## Problem

Hoy `GuidedTest.aspx` (el POD) carga al iniciar sesión una fila de trabajo
100% hardcodeada (`BuildSimulatedWorkList`): SFC derivado del `TrainingSession.Id`,
Shop Order `SO-000096`, Qty fija 250, Lote de hoy. El operador nunca la ingresa —
ya está ahí desde el primer render, en la tabla principal y en el Browse Work List.

Esto no refleja el flujo real de Moldeo: en piso, el operador **crea** la orden en
Resource Setup, la **busca y selecciona** en Search/Start, y solo entonces aparece
en el POD. El simulador debe reproducir esa secuencia para ser una práctica fiel,
sin necesidad de persistir órdenes reales en base de datos (sigue siendo un mock,
pero dinámico y controlado por el operador).

## Scope

### In
- Remover el pre-llenado hardcodeado de la tabla principal del POD (`RptWorkList`)
  al cargar la página — el POD arranca vacío.
- Resource Setup: el valor ingresado en la fila tipo "Shop Order" pasa a ser la
  semilla de una orden simulada (`SimulatedOrder`), guardada en `Session` de
  ASP.NET, asociada al `TrainingSession.Id` actual.
- Derivación determinística de un SFC (`CR9A` + 8 dígitos) a partir de ese valor.
- Search/Start: el campo `TxtSFC` deja de ser editable a mano — se llena
  exclusivamente seleccionando la fila en el Browse Work List, que ahora lista
  la orden guardada en `Session` (no la fila hardcodeada).
- Bloqueo explícito de `Start` si no existe una orden guardada en `Session`.
- Al completar con éxito la secuencia de 4 modales de Search, la orden guardada
  se vuelca por primera vez a la tabla principal del POD (`RptWorkList`).
- DC Collect: gating por JavaScript — el click no dispara postback si no hay una
  fila de la tabla POD seleccionada.
- SFC Split: la fila resultante del split se agrega como una **fila nueva** en la
  tabla del POD (mismo SFC/Shop Order, distinto identificador interno), y la fila
  origen reduce su cantidad. Se valida `0 < qty a dividir < qty disponible en esa fila`.
  Cualquier fila (original o ya dividida) puede volver a dividirse mientras
  `Qty > 1`.
- Limpieza de la orden simulada al abandonar la sesión de entrenamiento (`Abandon`).

### Out
- Persistir la orden simulada en base de datos (sigue siendo 100% en memoria/Session).
- Soportar más de 1 Shop Order activo por sesión de entrenamiento — sigue siendo
  exactamente 1 (regla ya existente en `ResourceSetupControl`).
- Sincronizar el estado entre múltiples pestañas/sesiones del mismo operador.
- Cambios al motor de scoring/validación de pasos (`StepValidator`, `TrainingSessionService`) —
  el split y el nuevo flujo de orden no alteran cómo se puntúa un paso, solo cómo
  se refleja visualmente en el POD.
- Lectura por hardware real (scanner) — el readonly de `TxtSFC` es solo para forzar
  el flujo de selección vía Browse Work List dentro del simulador.

## Domain Model

### `SimulatedOrder` (nueva clase, in-memory — vive solo en `Session`)

```csharp
public class SimulatedOrder
{
    public string ShopOrderInput      { get; set; } // texto numérico crudo ingresado en Resource Setup
    public string SFCCode             { get; set; } // derivado una sola vez, compartido por todas las filas
    public string MaterialDescription { get; set; } // Product.Name + " / " + Area.Name
    public string Status              { get; set; } // "Active"
    public string ProcessLot          { get; set; } // "LOT-" + hoy (yyyyMMdd)
    public List<SimulatedOrderRow> Rows { get; set; } = new List<SimulatedOrderRow>();
}

public class SimulatedOrderRow
{
    public string RowId { get; set; } // Guid.NewGuid().ToString("N") — identidad interna, NO visible
    public int    Qty   { get; set; } // mutable — split la reduce/agrega filas
}
```

- **Clave en `Session`:** `"GDT_SimOrder_" + trainingSession.Id` (mismo patrón que
  `Constants.SessionKeys`, agregar constante nueva en `Constants.SessionKeys`
  o una clase de claves propia del POD).
- Guardar/leer siempre por esa clave — nunca por `OperatorId` — para que dos
  `TrainingSession` del mismo operador (una completada, otra nueva) no colisionen.
- `PodWorkListRow` (el modelo que ya alimenta `RptWorkList`/`RptBrowseList`) gana
  un campo nuevo `RowId` (string) que se propaga desde `SimulatedOrderRow.RowId`
  — necesario para distinguir 2 filas con el mismo SFC/Shop Order tras un split.

### Derivación del SFC

Entrada: `ShopOrderInput` (ya validado como 100% numérico en Resource Setup).

```
dígitos = ShopOrderInput (ya son solo dígitos)
si length(dígitos) < 8:  pad con ceros a la izquierda hasta 8
si length(dígitos) > 8:  tomar los primeros 8 dígitos (los de más a la izquierda)
SFCCode = "CR9A" + dígitos_de_8
```

Ejemplo: `"096091"` → `"00096091"` → `"CR9A00096091"`.

## Flujo Actualizado

### Paso 1 — Resource Setup (`ResourceSetupControl`)

1. Reglas de validación existentes se mantienen (≥2 filas, exactamente 1 fila
   "Shop Order", ≥1 "Tool Number", `Setup State = "Set Up"`).
2. **Regla nueva:** el valor de la fila "Shop Order" debe ser 100% numérico.
   Si no lo es, `Swal.fire` de advertencia ("El Shop Order debe ser numérico")
   y no se guarda — mismo patrón que las validaciones actuales (cliente en
   `rsPrepareSubmit()`, con fallback server-side en `BtnSave_Click`).
3. Al pasar todas las validaciones y hacer submit exitoso (`BtnSave_Click` →
   `OnStepSubmit`), **antes** de invocar el evento, `GuidedTest.aspx.cs` construye
   y guarda un `SimulatedOrder` nuevo en `Session`, calculado a partir de:
   - `ShopOrderInput` = valor de la fila "Shop Order".
   - `SFCCode` = derivado (regla arriba).
   - `MaterialDescription` = `Product.Name + " / " + Area.Name` (igual que hoy).
   - `Qty` de la única fila inicial = 250 si `NonSerialized`, 1 si `Serialized`
     (mismo criterio que `BuildSimulatedWorkList` actual).
   - `Status` = `"Active"`, `ProcessLot` = `"LOT-" + DateTime.Today:yyyyMMdd`.
4. Si el operador abre y guarda Resource Setup una segunda vez en la misma
   sesión de entrenamiento, el `SimulatedOrder` anterior se **reemplaza**
   (misma clave de `Session`, se sobrescribe).
5. La tabla principal del POD (`RptWorkList`) **no** se llena en este paso —
   permanece vacía. `BindWorkList` deja de llamarse incondicionalmente en
   `Page_Load`/`!IsPostBack`; solo se invoca tras completar Search/Start (ver Paso 3).

### Paso 3 — Search / Start (`SearchControl` + POD host)

1. `TxtSFC` pasa a `readonly` (no `disabled`, para que su valor siga viajando
   en el postback). El operador ya no escribe ahí.
2. `BindBrowseList` (Browse Work List, modal de selección) ahora lee el
   `SimulatedOrder` de `Session` en vez de `BuildSimulatedWorkList`. Si no hay
   orden guardada, la lista aparece vacía (mismo criterio "vacío sin mensaje"
   que la tabla principal).
3. `confirmBrowseSelection()` (JS ya existente) sigue llenando `TxtSFC` y
   `HfSelectedSFC` con el SFC de la fila elegida — sin cambios de comportamiento,
   solo cambia el origen de los datos que se listan.
4. **Validación nueva en `BtnStart_ServerClick`:** si no existe `SimulatedOrder`
   en `Session` para el `TrainingSession.Id` actual, se muestra
   `Swal.fire` de advertencia ("Debes completar Resource Setup antes de iniciar")
   y **no** se abre `SearchControl`. Esto es defensivo — en el flujo normal el
   botón `Start` solo se habilita cuando el motor de pasos llega a `StepType.Search`,
   que ya requiere haber completado `ResourceSetup` antes.
5. Si el SFC seleccionado no coincide con el `SFCCode` de la orden en `Session`,
   se trata igual que "campo vacío" (mensaje genérico ya existente) — en la
   práctica no debería ocurrir porque solo hay 1 orden y el Browse Work List
   solo la muestra a ella.
6. Al completar con éxito los 4 modales (`BtnSave4_Click` → validación OK →
   `OnStepSubmit` → `HandleControlSubmit` con `step.StepType == StepType.Search`
   y outcome `Correct`/`FailedForced`), **además** del banner de éxito actual,
   se llama a `BindWorkList(area, session)` para volcar por primera vez las
   filas del `SimulatedOrder` guardado a `RptWorkList`.

### Paso 4 — DC Collection (gating de selección)

1. La tabla principal del POD ya vincula `data-sfc` y (nuevo) `data-row-id` por
   fila vía `selectWorkListRow(row)`. Se agrega un campo oculto
   `HfSelectedRowId` (paralelo a `HfSelectedSFC`) para guardar el identificador
   interno de la fila seleccionada.
2. `BtnDCCollection` sigue habilitándose/deshabilitándose por servidor según
   `StepType` (comportamiento actual de `SetActiveButton`, sin cambios).
3. **Gating nuevo, 100% cliente:** un listener de `click` en `BtnDCCollection`
   verifica `HfSelectedRowId.value`. Si está vacío, `e.preventDefault()` +
   `Swal.fire` de advertencia ("Selecciona una fila de la tabla antes de
   continuar") — no se dispara postback. Si hay valor, el click continúa
   normalmente al postback existente (`BtnDCCollection_ServerClick`).
   No se agrega validación adicional en servidor para este gating (según
   decisión: solo cliente).

### Paso Extra — SFC Split (`LogNCControl`, modo `"split"`)

1. `BtnSFCSplit_ServerClick` pasa a incluir en `ViewModel.ExtraData` la `Qty`
   real y el `RowId` de la fila del POD actualmente seleccionada
   (`HfSelectedRowId`/`HfSelectedSFC`), en vez de que `LogNCControl.Initialize()`
   hardcodee `LitAvailableQty.Text = "250"`.
2. **Validación nueva en `BtnSplit_Click`:** la cantidad ingresada debe cumplir
   `0 < qty < AvailableQty` (la qty de la fila seleccionada). Si no cumple, error
   inline (mismo patrón que otras validaciones de este control) y no avanza a
   `PnlSplitConfirm`.
3. Al confirmar (`BtnCloseSplit_Click` → `OnStepSubmit` con
   `{"sfcSplit":{"qty": N}}`), `GuidedTest.aspx.cs` (en `HandleControlSubmit`,
   antes o después de `ProcessSubmit` — ver Deferred Decisions) aplica la
   mutación sobre el `SimulatedOrder` en `Session`:
   - Ubica la fila por `RowId` guardado al abrir el split.
   - Resta `N` a su `Qty`.
   - Agrega una `SimulatedOrderRow` nueva con `RowId` nuevo y `Qty = N`,
     compartiendo `SFCCode`/`ShopOrderInput`/`MaterialDescription`/`Status`/`ProcessLot`
     (todos son campos de cabecera del `SimulatedOrder`, no de la fila).
4. Tras la mutación, se vuelve a llamar `BindWorkList(area, session)` para que
   `RptWorkList` muestre ambas filas.
5. Cualquier fila (la original o una ya derivada de un split anterior) puede
   volver a dividirse mientras su `Qty` sea mayor a 1 — se repite el mismo
   flujo tomando esa fila como base.

## Addendum 2026-09-22 — SFC Split como TrainingStep independiente

SFC Split deja de ser un modo de `LogNCControl`. `SFCSplitControl` aplica cada split como mutación intermedia del POD sin registrar intento ni avanzar; la X permite volver al POD y seleccionar otra fila. El botón [Close] completa `StepType.SFC_Split` sólo después de al menos un split exitoso. `LogNCControl` queda dedicado exclusivamente a NC/scrap.

## Edge Cases & Failure Modes

- **Shop Order no numérico en Resource Setup:** bloqueado con aviso, no se
  guarda `SimulatedOrder` (ver Paso 1, regla 2).
- **Start sin Resource Setup completado (Session sin `SimulatedOrder`):**
  bloqueado con aviso en `BtnStart_ServerClick` (ver Paso 3, regla 4).
- **Resource Setup guardado 2 veces en la misma sesión de entrenamiento:** la
  orden nueva reemplaza a la anterior (ver Paso 1, regla 4).
- **Split sobre una fila ya dividida:** permitido, siempre que
  `0 < qty < Qty actual de esa fila` (ver Paso Extra, regla 5).
- **Split con qty = 0 o qty ≥ disponible:** error de validación, no se aplica
  la mutación ni se cierra el modal.
- **Dos filas con el mismo SFC/Shop Order tras un split:** se distinguen por
  `RowId` interno (`data-row-id`), no por SFC — la selección de fila y el
  gating de DC Collect usan ese identificador.
- **Abandon de la sesión de entrenamiento:** el `SimulatedOrder` asociado a ese
  `TrainingSession.Id` se remueve explícitamente de `Session`
  (`Session.Remove("GDT_SimOrder_" + session.Id)`) antes del redirect en
  `BtnAbandon_Click`.
- **Logout:** ya limpia toda la `Session` (`Session.Abandon()`), cubre también
  al `SimulatedOrder` sin cambios adicionales.
- **Sesión de ASP.NET expirada a mitad de flujo:** se comporta igual que "Start
  sin Resource Setup completado" — no hay `SimulatedOrder` recuperable, aviso y
  bloqueo (no crashea con `NullReferenceException`).

## Non-Functional Requirements

- Sin cambios de esquema de base de datos — todo el estado nuevo vive en
  `Session` de ASP.NET, igual que `OperatorId`/`Code1`/`Name` hoy.
- Sin nuevas dependencias/paquetes.
- El tamaño del objeto guardado en `Session` es trivial (una orden con a lo
  sumo unas pocas filas tras varios splits) — no representa riesgo de memoria
  por sesión de servidor.

## Integrations

- Ninguna integración externa nueva. Todo el cambio es interno a
  `GuidedTest.aspx(.cs)`, `ResourceSetupControl`, `SearchControl`, `LogNCControl`
  y el modelo `PodWorkListRow`.

## Acceptance Criteria

- **AC-1:** Dado que inicia una sesión de Guided Test nueva, cuando el POD
  carga por primera vez, entonces `RptWorkList` renderiza 0 filas, sin mensaje
  placeholder.
- **AC-2:** Dado que el operador ingresa un valor no numérico en la fila
  "Shop Order" de Resource Setup, cuando hace clic en Save, entonces se muestra
  un aviso y no se guarda ningún `SimulatedOrder`.
- **AC-3:** Dado que el operador ingresa `"096091"` como Shop Order y cumple el
  resto de reglas de Resource Setup, cuando guarda exitosamente, entonces se
  guarda en `Session` un `SimulatedOrder` con `SFCCode = "CR9A00096091"`,
  `ShopOrderInput = "096091"`, `Qty` inicial 250 (área NonSerialized) y
  `ProcessLot = "LOT-" + hoy`.
- **AC-4:** Dado que el operador guarda Resource Setup una segunda vez en la
  misma sesión de entrenamiento, cuando el segundo guardado es exitoso,
  entonces el `SimulatedOrder` anterior es reemplazado, no acumulado.
- **AC-5:** Dado que no existe `SimulatedOrder` en `Session`, cuando el
  operador hace clic en `Start`, entonces se muestra un aviso y no se abre
  `SearchControl`.
- **AC-6:** Dado que existe un `SimulatedOrder` en `Session`, cuando el
  operador abre Browse Work List, entonces la lista muestra las filas de esa
  orden (no la fila hardcodeada anterior).
- **AC-7:** Dado que el operador selecciona la fila del `SimulatedOrder` en
  Browse Work List, cuando confirma, entonces `TxtSFC` (readonly) y
  `HfSelectedSFC` quedan con el `SFCCode` de esa orden.
- **AC-8:** Dado que `TxtSFC`/`HfSelectedSFC` tienen el SFC seleccionado,
  cuando el operador hace clic en `Start`, entonces se abre la secuencia de 4
  modales de Search igual que hoy.
- **AC-9:** Dado que la secuencia de 4 modales se completa exitosamente,
  cuando el paso se marca `Correct`/`FailedForced`, entonces `RptWorkList` se
  llena por primera vez con las filas del `SimulatedOrder`.
- **AC-10:** Dado que `RptWorkList` tiene filas pero ninguna está seleccionada,
  cuando el operador hace clic en `DC Collect`, entonces no se dispara postback
  y se muestra un aviso pidiendo seleccionar una fila.
- **AC-11:** Dado que el operador seleccionó una fila de `RptWorkList`, cuando
  hace clic en `DC Collect`, entonces el control de Data Collection se abre
  normalmente.
- **AC-12:** Dado que la fila seleccionada tiene `Qty = 250`, cuando se abre el
  modal de SFC Split, entonces "Available Qty" muestra 250 (no un valor
  hardcodeado).
- **AC-13:** Dado `Available Qty = 250`, cuando el operador ingresa `0` o
  `≥ 250` como cantidad a dividir, entonces se muestra un error de validación y
  no se aplica el split.
- **AC-14:** Dado `Available Qty = 250`, cuando el operador confirma un split
  de `100`, entonces la fila origen queda con `Qty = 150` y se agrega una fila
  nueva con `Qty = 100`, mismo SFC/Shop Order, `RowId` distinto.
- **AC-15:** Dado que existen 2 filas con el mismo SFC tras un split, cuando el
  operador selecciona cualquiera de las 2 en `RptWorkList`, entonces
  `HfSelectedRowId` identifica inequívocamente cuál fue seleccionada.
- **AC-16:** Dado que una fila resultado de un split anterior tiene `Qty = 100`,
  cuando el operador la selecciona y hace un nuevo split válido (`0 < qty < 100`),
  entonces se agrega una tercera fila siguiendo la misma regla.
- **AC-17:** Dado que el operador hace clic en `Abandon`, cuando el abandono se
  completa, entonces el `SimulatedOrder` de esa `TrainingSession.Id` se elimina
  de `Session`.

## Deferred Decisions

- **Browse Work List antes de completar Resource Setup:** se deja vacío sin
  mensaje adicional (mismo criterio que la tabla principal), en vez de un
  mensaje explicativo tipo "Completa Resource Setup primero". *Revisar si*
  operadores reales reportan confusión al ver el modal vacío sin explicación.
- **Momento de aplicar la mutación del Split respecto al scoring del paso:**
  se aplica siempre al recibir `OnStepSubmit` del split, independientemente de
  si el intento se marca `Correct` o `Incorrect` en el scoring — el split es
  una acción de dominio sobre el POD, no un resultado de evaluación. *Revisar
  si negocio pide que un split "Incorrecto" no deba mutar la tabla.*
- **Limpieza del `SimulatedOrder` al completar la guía normalmente (no
  Abandon):** no se limpia explícitamente — queda huérfano bajo su clave
  (`GDT_SimOrder_<id>`) hasta que expire la `Session` de ASP.NET. Es inocuo
  porque cada `TrainingSession.Id` nuevo usa una clave distinta. *Revisar solo
  si se detecta crecimiento de memoria de sesión en producción (muy
  improbable dado el volumen).*

## Open Questions

_Ninguna — todas las preguntas de las rondas de especificación fueron
respondidas o movidas a Deferred Decisions._
