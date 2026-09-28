# Plan de implementación — Spec 008

**Spec:** `docs/specs/008-start-serialized-multi-sfc.md` (Approved 2026-09-28, con enmienda)
**Aprobado:** 2026-09-28
**Arquitectura:** N-Tier (Pages → BLL → DAL → Domain) + stored procedures.

Seis entregas en el orden en que aparecen los pasos de la guía de Redefinity
(Start → Assembly → DC → Log NC/Complete): cada una se prueba recorriendo el flujo hasta
ese paso. Cada entrega: build + precompilación de markup → checklist → el usuario prueba
→ commit `feat: ... (spec 008)`.

## Decisiones de diseño

- **Reglas en BLL, puras y testeables con NUnit:** regla de selección de filas (tabla del
  punto 6 de la spec), regla de completitud de DC, generador de SFCs.
- **Avance de DC y Assembly en Session** (objetos del servidor, como la orden simulada).
  La BLL recibe el avance, aplica la regla y devuelve el resultado; la página solo lo
  guarda.
- **DC y Assembly con eventos propios** (`OnGroupSubmit`, `OnComponentAssembled`). Lo que
  hoy hace `HandleControlSubmit` después de resolver un paso se extrae a un método común.
- **El intento correcto de DC/Assembly se registra al completar el paso;** un Save
  incorrecto usa el mismo `RecordAttempt` (penalización y force-advance intactos).
- **Selección múltiple:** `HfSelectedRowIds` (ids separados por coma); el servidor valida
  que existan en la orden.

## Entregas

### Entrega 1 — Catálogo y esquema (sin cambio visible) — implementada, pendiente de verificación
- [x] `00_Schema.sql`: `Areas.RequiresStartChecklist`; tabla `DataCollectionGroups`
      (`IsRequired`); `DataCollectionParameters.DataCollectionGroupId` reemplaza
      `DCGroup`/`GroupDescription`/`CollectDataAt`; tabla `AssemblyComponents`
      (`Qty DECIMAL(18,6)`, `OperationId` NULL).
- [x] `02_spTrainingGuide.sql`: SPs de Area con el flag; SP de parámetros con `GroupId`;
      `spDataCollectionGroupGetByArea`, `spAssemblyComponentGetByArea`.
- [x] Domain (`DataCollectionGroup`, `AssemblyComponent`, `Area.RequiresStartChecklist`,
      `DataCollectionParameter`) + DAL (2 repos nuevos, ajuste de 2). Catálogos nuevos
      expuestos por `TrainingGuideService` (la página no llama a la DAL para ellos).
- [x] Seeds: `06` Moldeo (6 grupos; **solo `DCGTM_FECHA_EXPIRACION_NIR/A` obligatorio**;
      `RequiresStartChecklist = 1`). `05` Redefinity (2 grupos obligatorios + 1 opcional,
      BOM simulado de 2 componentes con Qty `1` y `0.0001`, `RequiresStartChecklist = 0`).
- [x] `DataCollectionControl` arma la lista de grupos desde la tabla nueva (la regla de
      completitud todavía no cambia).
- [x] Script `ALTER` para la BD de desarrollo, entregado aparte (no va al repo).
- [x] CLAUDE.md → "Catálogos en BD".
- [x] **Checklist del usuario:** Moldeo completo sin cambios; la lista de grupos DC se ve
      igual. Verificado y BD confirmada (commit `9f2bd34`).

### Entrega 2 — Piezas Serialized y Start (AC-1 a AC-5) — verificada
- [x] BLL: `SerialSfcGenerator` + `PieceSelectionRule` (con tests).
- [x] `SimulatedOrderRow` con `SFCCode` y `Status` (`New`/`Active`); `SimulatedOrder.Create`
      genera piezas en Serialized.
- [x] Browse Work List multi-selección en Serialized (**sin columna Status**); `TxtSFC`
      con SFC o `[{N} SELECTED]`; POD solo con piezas `Active`, multi-selección
      (NonSerialized: máx. 1).
- [x] Start directo si `RequiresStartChecklist = 0` (todas las piezas o aviso; pasa a
      `Active`; `SubmitStep`); `StepValidator` da Search por correcto sin checklist.
      Tras el Start, las piezas iniciadas quedan seleccionadas en el POD.
- [x] S2: orden por defecto genera piezas (E8); Start forzado activa todas (E9).
- [x] Refactor de selección (`HfSelectedRowIds`, `HfBrowseRowIds`).
- [x] **Checklist del usuario** (incluye regresión de Moldeo). Corrección posterior: Resource
      Setup forzado usa siempre la Shop Order genérica (E8); pendiente de re-verificar en la
      Entrega 3.

### Entrega 3 — Assembly con BOM (AC-11 a AC-14) — verificada
- [x] BLL: `AssemblyProgress` + `AssembleComponent(...)` (con tests); se elimina
      `ExpectedComponentCount`.
- [x] `AssemblyControl`: Component List del catálogo, modal client-side con datos del
      componente y "Apply to all SFCs" fija, Add con barcode numérico →
      `OnComponentAssembled`, X / Return to POD → `OnReturnToPod`.
- [x] `GuidedTest`: sin `ExpectedCount = 3`, todas las filas, avance en Session;
      `ContinueAfterStepResolved` común a controles y Assembly.
- [x] CLAUDE.md: descripción del paso Assembly.
- [x] **Checklist del usuario** (incluye re-verificar E8 de la Entrega 2 y regresión de
      Moldeo). Correcciones de UI: modal replicado de las capturas reales (Mode/Action
      informativos, detalles al escanear) y Batch Number = Process Lot de la orden.

### Entrega 4 — DC multi-pieza con grupos obligatorios (AC-6 a AC-10) — verificada
- [x] BLL: `DataCollectionCompletionRule` (tests), `DataCollectionProgress`,
      `SubmitDataCollectionGroup(...)` (incorrecto / grupo guardado / pieza completa /
      paso completo / forzado → descarta avance, E5); `StepValidator.ValidateDataCollectionGroup`
      exige todos los parámetros del grupo y solo esos (tests).
- [x] `DataCollectionControl`: indicador `SFC: {sfc} (n/N)`, `OnGroupSubmit`, vuelve a la
      lista tras un grupo correcto (banner "Data collected for {grupo}").
- [x] `GuidedTest`: todas las filas en Serialized; guarda/descarta avance.
- [x] **Checklist del usuario** (incluye la regla nueva de Moldeo).

### Entrega 5 — Log NC y Complete multi-fila (AC-15 a AC-17) — verificada
- [x] Log NC: scrap de todas las seleccionadas; ninguna = intento fallido; todas = aviso.
      NC Data Entry muestra el SFC o `[N SELECTED]`.
- [x] Complete: todas las filas restantes; ninguna = intento fallido; algunas = aviso.
- [x] **Checklist del usuario** (Redefinity de punta a punta y regresión de Moldeo).

### Entrega 6 — Cierre
- [ ] Regresión completa: Moldeo (AC-18) y Redefinity de punta a punta, Guided y
      Assessment.
- [ ] Roadmap: bloqueantes #1, #2 y #4 resueltos (#3 sigue pendiente de negocio).

## Riesgos

- Migración de la BD dev: quitar los defaults con nombre antes de borrar columnas.
- Caché del validador (60 min por área): reiniciar IIS Express tras cambiar catálogo.
- El refactor de selección toca Moldeo → regresión incluida en la Entrega 2.
- Disciplina de postbacks: modal de Assembly client-side; avance en Session, no ViewState.
