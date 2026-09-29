# Roadmap — MESTrainingSimulator

> Historial completo de fases, extraído de `CLAUDE.md`. El resumen operativo (fase actual + siguiente) vive en `CLAUDE.md` → sección Roadmap.

Cada fase tiene un alcance cerrado. No mezclar tareas de otras fases — la
causa principal de deuda técnica temprana es que features de fases futuras
se filtran en fases anteriores.

---

## 📍 Estado actual — dónde quedamos

> **Leer esta sección al iniciar cada sesión.** Actualizarla al cerrar una fase o
> una sesión de trabajo relevante.

**Última actualización:** 2026-09-29 · **Branch:** `TrainingApp` · **Último commit de código:** `b832e77` (`git log --oneline -5` para ver el estado real)

- **Fase 6 cerrada ✅** (2026-09-24). El flujo completo de R&C/Moldeo (NonSerialized)
  funciona de punta a punta en Guided y Assessment (penalización, force-advance y
  aprobado/reprobado), verificado en navegador por el usuario.
- **Assessment real (S1 + S4, sin spec):** al probar Assessment se vio que el 50% del
  puntaje nunca podía fallar porque los controles rechazaban las decisiones antes de
  enviarlas. Se separó formato de decisión (ver `CLAUDE.md` → Key Conventions): LC "No",
  Setup State "Open" y los "No" de Start ahora llegan a `StepValidator` y cuentan como
  intento. En Assessment el mensaje de error no revela la respuesta (muestra los intentos
  restantes). El 70% de aprobación pasó a `ScoreCalculator.PassingScore`. Los banners de
  guardado (Record change, DC group saved) solo aparecen si el servidor aceptó el paso.
  Verificado en navegador por el usuario (2026-09-25).
- **S2 — secuencia en Assessment (sin spec, decisiones cerradas con el usuario 2026-09-25):**
  - En Assessment todos los botones del POD están habilitados, incluidos los que no aplican
    al área (Assemble en Moldeo, SFC Split en Serialized). Guided sin cambios.
  - Un click en un botón que no es el del paso actual es un intento fallido **del paso
    actual** (`TrainingSessionService.RegisterOutOfSequence`), con la misma penalización y
    force-advance. El control del botón equivocado no se abre.
  - Mismo mensaje genérico que una respuesta incorrecta (no revela que fue de secuencia).
  - La secuencia se evalúa antes que los avisos de formato del botón (SFC vacío, fila sin
    seleccionar).
  - Activities → LC entra en la regla; Browse Work List, selección de filas, Abandon y
    Logout son navegación y nunca cuentan.
  - Las acciones de MES sin lógica (Work Instructions, Reports, Place Back in Queue, Tool
    List y las 7 opciones de relleno de Activities) también se habilitan en Assessment y
    su click es siempre un intento fallido del paso actual.
  - Resource Setup forzado → siempre la orden por defecto (Shop Order genérica `303406950`),
    ignorando lo capturado en intentos fallidos (spec 008, E8).
    Start forzado → el POD se carga como si Start se hubiera ejecutado.
  Verificado en navegador por el usuario (2026-09-25).
- **Spec 008 cerrada ✅** (2026-09-28, 5 entregas + cierre, plan en
  `docs/specs/008-plan.md`): el motor ya soporta áreas Serialized.
  - Resource Setup genera 3-5 piezas con SFC propio; Browse Work List y POD con
    multi-selección; `TxtSFC` con el SFC o `[N SELECTED]`; Start directo cuando el área
    no tiene checklist (`Areas.RequiresStartChecklist`, hoy solo Moldeo = 1).
  - Assembly con el BOM de la operación (`AssemblyComponents`, simulado): Component List,
    modal replicado del MES real, Batch Number = Process Lot. Se eliminó
    `ExpectedComponentCount`.
  - Data Collection por pieza con grupos obligatorios (`DataCollectionGroups.IsRequired`),
    indicador `SFC: … (n/N)` y avance conservado. **Moldeo ahora exige** su grupo de
    fechas de expiración para completar DC.
  - Log NC y Complete multi-fila. En Serialized la selección parcial es aviso sin
    penalización (el motor trabaja todas las piezas juntas; ver Deferred Decisions de la
    spec: recorrido pieza por pieza).
  - Verificado en navegador por el usuario: Redefinity de punta a punta y regresión de
    Moldeo en Guided y Assessment.
- **Fase 6.5 — Fidelidad de UI/UX con el SAP MES real 🔄 (en curso, formalizada
  2026-09-29).** Nació como mini-fase de ajustes de UI tras la spec 008. Objetivo:
  dejar cerrada la interfaz antes de los overlays guiados (Fase 6.6), que se anclan a
  ella. Detalle, alcance y criterio de cierre abajo, en "Fase 6.5".
  - **Entregas 1-4 ✅** (2026-09-29, verificadas por el usuario; plan con casillas y
    decisiones en `docs/specs/fase-6.5-plan.md`): columna SFC y Select All; Collect
    deshabilitado por pieza (enmienda de E3, spec 008); "Apply to all SFCs" en el pie;
    Shop Order / Status (SFC) del POD Search; layout y colores del MES real; header
    estándar de los 12 modales.
  - **Inventario de pantallas** (criterio de cierre): `docs/ui-reference/inventario-pantallas.md`
    — hoy ✅ 3 · 🟡 11 · ⚪ 2 (16 pantallas del MES).
  - **Ahora:** entrega 5 con la siguiente tanda de correcciones del usuario, cruzada con
    las diferencias del inventario.
- **Después — Fase 6.6 · Overlays guiados** (requiere `/spec`), sobre la interfaz ya
  cerrada. Ver "Fase 6.6" abajo.
- **Fase 7 (LoFlo)** solo espera el bloqueante #3: los flags reales de LoFlo, a
  confirmar con negocio. Con eso, la Fase 7 vuelve a ser "solo datos"; no compite con la
  6.5 ni con la 6.6.
  **Qué pedirle a negocio** para escribir el script de LoFlo:
  1. Flags: `RequiresAssembly` (BD = false, CLAUDE.md = true), `RequiresDataCollection`,
     `RequiresNCComponent`.
  2. DC Groups de la operación que se entrena, **cuáles son obligatorios**, y sus
     parámetros (tipo, unidad, rangos).
  3. Códigos NC de LoFlo.
  4. BOM (números de parte y cantidades): puede seguir **simulado** (decisión del usuario).
- **Mientras llega la respuesta de negocio** (opciones, sin orden decidido): deuda de
  `GuidedTest` → DAL directo; Fase 5 (tests de `ScoreCalculator`/`StepValidator`, el
  usuario la quiere al final). Preguntar al usuario antes de elegir.

### Cómo retomar en una sesión nueva
1. Leer esta sección y `git log --oneline -10` (branch `TrainingApp`; no hay push pendiente
   de pedir: el usuario hace push cuando quiere).
2. La BD de desarrollo del usuario ya tiene el esquema de la spec 008 (ALTER entregado y
   corrido el 2026-09-28). Una instalación limpia se arma con los scripts `00`–`07` en orden.
3. **Proceso acordado:**
   - Funcionalidad o reglas de negocio nuevas → `/spec` → aprobación explícita → `/plan`
     (guardado en `docs/specs/NNN-plan.md`, en entregas chicas) → implementación.
   - Bugs, robustez y cambios con pocas decisiones acotadas → plan directo en el chat,
     con una ronda de preguntas si hace falta (así se hicieron el arreglo de F5, S1 y S2).
   - Cada entrega: rebuild + `aspnet_compiler` + tests (`CLAUDE.md` → "Build y
     validación") → checklist → el usuario prueba en el navegador → commit.
4. Pantallas: replicar el SAP MES real. Las capturas de referencia que aporta el usuario
   están en `C:\Users\320313439\Pictures\TrainingApp\` y lo ya documentado en
   `docs/ui-reference/` (`general-flow.md` + un archivo por área). Estado de cada pantalla
   frente a su captura: `docs/ui-reference/inventario-pantallas.md` (Fase 6.5).

### Bloqueantes antes de la Fase 7 (LoFlo)

| # | Tema | Por qué bloquea | Estado |
|---|---|---|---|
| 1 | **Start de Serialized**: una fila por pieza con SFC propio, txtSFC por selección, multi-selección en el POD | LoFlo es Serialized | ✅ Resuelto (spec 008, entrega 2) |
| 2 | **Assembly no puede completarse** (`ExpectedComponentCount` nulo, `ExpectedCount = 3` hardcodeado) | LoFlo tiene Assembly según CLAUDE.md | ✅ Resuelto (spec 008, entrega 3): Assembly rediseñado con el BOM; `ExpectedComponentCount` eliminado |
| 3 | **Flags reales de LoFlo a confirmar con negocio**: `RequiresAssembly` (BD = false, CLAUDE.md = true), `RequiresNCComponent` (1 provisorio, spec 007), `RequiresDataCollection` ("variable" según CLAUDE.md) | Sin esto el catálogo de la Fase 7 no se puede escribir | **Pendiente de negocio** |
| 4 | **Huecos de UI de Assembly**: la X del Component List y Return to POD vacíos | Se ejercitan por primera vez con LoFlo | ✅ Resuelto (spec 008, entrega 3) |

### Deuda y riesgos conocidos (no bloquean la Fase 7)

- **Checklist de la spec 006 cerrado ✅ (2026-09-29):** el punto 6 (volver a guardar
  Resource Setup → AC-12) se dio por resuelto por análisis de código: el Save repetido
  en el modal ya confirmado lo descartan `ResourceSetupControl.BtnSave_Click`
  (`if (!Visible) return;`, no sobrescribe la orden) y el guard de sender de
  `HandleControlSubmit`; no hay intento ni cambio de puntaje y el modal se cierra. El
  5 (force-advance en DC) y el 7 (Assembly) quedaron cubiertos por la spec 008.
- **`GuidedTest` llama a repositorios de la DAL directamente** (`_areaRepo`,
  `_productRepo`, `_ncRepo`, `_dcParamRepo`), contra la regla de dependencias de
  CLAUDE.md. El código nuevo de la spec 008 pasa por `TrainingGuideService`; migrar lo
  existente es una tarea aparte.
- **Botón Atrás del navegador** todavía puede reenviar un POST viejo. F5 sí está
  resuelto (`history.replaceState`). Riesgo aceptado.
- **Fase 5 en pausa:** `ScoreCalculator` y `StepValidator` sin cobertura NUnit. La spec
  006 dejó los tests fuera de los criterios de aceptación.
- **`CompleteControl`** está declarado e implementa `IStepControl`, pero no tiene UI:
  Complete se resuelve directo desde `BtnComplete`.
- **Detalle por paso en el resumen final** (intentos y puntaje por paso): diferido
  (spec 007), hasta que lo pida un instructor o la Fase 8.
- **Pérdida de la Session a mitad de sesión** (spec 008, E7): la orden simulada y el
  avance de DC/Assembly viven solo en `HttpSessionState`; si se pierden, el operador
  tiene que reiniciar. No se implementa ahora; caso especial a analizar cuando
  evolucione la arquitectura de flujos (opción: persistirlos en BD por sesión).

### Scripts de BD (`Database/StoredProcedures/`)

- **Limpieza 2026-09-25:** las migraciones (`08`–`11` y los ALTER de `06`) se
  integraron al esquema original y se borraron. Quedan 8 scripts definitivos, que se
  ejecutan en orden numérico (`00` tablas → `01`–`04` SPs → `05` datos de ejemplo →
  `06`–`07` catálogo y guía reales de R&C/Moldeo). Instalación limpia y re-ejecución
  verificadas en LocalDB; el resultado es idéntico a la BD de desarrollo.
- `05_SeedData.sql` — **solo dev**: áreas de ejemplo (Redefinity, LoFlo provisoria,
  Subensamble 1) y la guía publicada de Redefinity (Serialized, con Assembly), con 2 DC
  Groups obligatorios + 1 opcional y un BOM simulado (`SIM-100001`, `SIM-100002`) para
  probar el camino Serialized de punta a punta (spec 008).
- Los cambios de esquema futuros se escriben directo en `00_Schema.sql`; el `ALTER`
  para la BD existente se le entrega al usuario aparte y no se guarda como script.

### Fase 0 — Setup ✅ (completada)
**Objetivo:** Proyecto Web Forms creado, N-Tier scaffolded, build verde.
**No incluye:** lógica de negocio, UI funcional, datos reales.

---

### Fase 1 — Auth ✅ (completada)
**Objetivo:** Login.aspx + ChangePassword.aspx funcionales, flujo completo
de primer acceso (API de empleados → Operator local → PBKDF2 → FormsAuth).
**No incluye:** Dashboard, GuidedTest, catálogo de áreas, ningún paso de entrenamiento.

---

### Fase 2 — Dominio y catálogo de R&C ✅ (completada)
**Objetivo:** Auditoría del legacy de R&C + script SQL con el catálogo real:
`Products` (R&C), `Areas` (Moldeo, NonSerialized, RequiresAssembly=false,
RequiresDataCollection=true), `NCCodes` (30 códigos), `DataCollectionParameters`
(15 parámetros: ID molde + 10 params MSS + verificación MSS + fecha kerosene +
2 first-shot). Catálogo en `06_RCMoldeoCatalog.sql`.
ExpectedComponentCount: N/A (RequiresAssembly=false).
`DataCollectionParameters` schema evolucionado: columnas `DataType`, `DisplayOrder`
añadidas; `MinValue`/`MaxValue` son ahora nullable (rangos pendientes de MSS D000281359).
**No incluye:** UI, motor de validación de pasos, TrainingGuideService,
ninguna pantalla de entrenamiento.

---

### Fase 3 — Motor de TrainingGuide ✅ (completada)
**Objetivo:** `TrainingGuideRepository` (dual result set puebla `Steps`),
`TrainingGuideService` con `BuildValidatorForGuide`, `StepValidator` completo
con constructor injection de `StepValidationContext` (Area + NCCodes + DCParams).
Validación implementada para todos los StepTypes: DataCollection (JSON por
param Id, tipado por DataType con InvariantCulture), LogNC (lookup en catálogo),
Assembly (count vs ExpectedComponentCount), LineClearance ("true"), pasos de
secuencia siempre true.
Nuevos repositorios: `AreaRepository`, `NCCodeRepository`,
`DataCollectionParameterRepository`. Nuevos SPs: `spAreaGetById`,
`spNCCodeGetByArea`, `spDataCollectionParameterGetByArea`.
Guía real de R&C/Moldeo publicada en `07_RCMoldeoGuide.sql` (8 pasos, pesos
suman 1.00; Search hereda peso 0.15 de Assembly ausente; 3 LC instancias con
pesos 0.04/0.03/0.03).
LC Moldeo modelada como paso LineClearance en el guide seed (step 4, weight 0.03);
la validación es idéntica al LC estándar — el contenido hardcodeado de sus 3
preguntas es responsabilidad de la UI (Fase 6).
`DataCollectionParameter` POCO evolucionado: añadidos `DataType`, `DisplayOrder`,
`MinValue`/`MaxValue` como `decimal?`.
Tests NUnit diferidos a Fase 5 (cobertura de ScoreCalculator y StepValidator).
**No incluye:** TrainingSession con gate-keeping real, persistencia de intentos
optimizada, UI, ScoreCalculator tests.

---

### Fase 4 — TrainingSession y modos ✅ (completada)
**Objetivo:** `TrainingSessionService` extendido con gate-keeping real Guided vs.
Assessment, `MemoryCache` de validator keyed por `AreaId` (TTL 60 min), método
`Abandon`, score en tiempo real (`CurrentScore` en `StepSubmitResult`).
`TrainingSessionRepository` extendido: `Abandon` + `DeleteActiveByOperator`.
Nuevos SPs: `spTrainingSessionAbandon`, `spTrainingSessionDeleteActiveByOperator`,
`spProductGetAll`, `spAreaGetByProduct`.
`ProductRepository` nuevo; `AreaRepository` extendido con `GetByProduct`.
`TrainingGuideService` extendido con `GetAllProducts` y `GetAreasByProduct`.
`StartSession.aspx` — selección Product → Area → Guide → Mode, inicia sesión.
`GuidedTest.aspx` — input genérico por paso, feedback + hint hardcodeado por
`StepType`, score en vivo, abandon con confirm JS, panel de completion.
Login redirige a `StartSession.aspx` tras autenticación exitosa.
Spec de referencia: `docs/specs/003-training-session-y-modos.md` (Approved 2026-09-04).
**No incluye:** UserControls por tipo de paso, ScoreCalculator tests, LoFlo.

---

### Fase 5 — ScoreCalculator: cobertura de tests ⏸️ (en pausa)
**Objetivo:** `ScoreCalculator` ya está implementado. Esta fase lo blinda
con tests NUnit exhaustivos: todos los casos de penalización, force-advance,
pasos opcionales ausentes, sesiones completadas vs. abandonadas.
Es el componente más sensible del sistema — debe tener cobertura total
antes de que usuarios reales generen puntajes.
**No incluye:** nuevas features, cambios al algoritmo.

---

### Fase 6 — UserControls por tipo de paso ✅ (completada 2026-09-24)
**Objetivo:** UserControls `.ascx` reutilizables para cada `StepType`:
`ResourceSetup`, `LineClearance`, `SearchOrder`, `Assembly`,
`DataCollection`, `NCScrap`, `Complete`. Cada control recibe el estado
del servidor y emite un submit — sin lógica de negocio interna.
**No incluye:** catálogo de LoFlo, nuevas áreas.

**Specs:**
- `004-fase6-dashboard-usercontrols.md` (Approved 2026-09-09): POD, botonera y contrato de UserControls.
- `005-pod-orden-dinamica.md` (Approved 2026-09-24, de forma retroactiva): orden simulada en Session creada
  por Resource Setup; SFC Split como paso propio (StepType 5); DC agrupado por DC Group
  (columnas `DCGroup`/`GroupDescription`/`CollectDataAt`, hoy en `00_Schema.sql`).
- `006-controles-de-paso-estaticos.md` (Approved 2026-09-22).
- `007-cierre-flujo-lognc-complete.md` (Approved 2026-09-24).

**Decisiones de arquitectura cerradas — no reabrir sin spec:**
- **Controles de paso estáticos (spec 006).** Los 8 controles están declarados en
  `GuidedTest.aspx` y se alterna `Visible`. **Prohibido volver a `LoadControl`
  dinámico**: generó cuatro bugs de ViewState en una sola sesión. Confirmado con
  negocio: los admins no crean tipos de paso desde la UI y un `StepType` no tiene UIs
  estructuralmente distintas por Area. Un `StepType` nuevo = `.ascx` + una línea de
  markup + un `case` en `GetStepControl`.
- El paso abierto vive en el ViewState de la página (`OpenStepType`).
  `ApplyStepControlVisibility` es el único punto que toca `Visible`.
- `IStepControl.Initialize()` es el **único punto de reinicio** del control. Todo
  control tiene que tolerar `ViewModel == null` y no registrar scripts cuando está
  invisible.
- **Guard de sender:** `HandleControlSubmit` y `HandleReturnToPod` descartan eventos
  de controles que no son el del paso activo, porque ASP.NET dispara eventos también
  en controles invisibles.
- **Submit incorrecto:** el control queda abierto en el modal donde se respondió y
  solo se muestra el SweetAlert. Es la única excepción aprobada a la paridad de UX
  (spec 006 E3).
- **Acción incorrecta = intento fallido** (spec 007): botón equivocado o gate de fila
  sin cumplir → `StepAnswers.Invalid` a la BLL. `StepValidator.Complete` acepta solo
  `"complete"`.
- Salir de un control sin responder (✗ / Return to POD) usa el evento
  `OnReturnToPod` y **no** registra intento.
- **F5 = GET** (`history.replaceState`). En la carga por GET, el POD y txtSFC se
  reconstruyen desde la orden simulada. F5 no restaura la selección ni el control
  abierto.
- Estado intermedio de UI en el cliente, sin postback: paginación de NC Selection,
  Save Draft de DC, navegación entre los 4 modales de Search.

**Commits de cierre:** `3567e07` (migración a controles estáticos), `51636a7` (DC
return to POD, txtSFC, resaltado de fila, score final), `f54ff50` (spec 007),
`3a38b86` (F5).

---

### Fase 6.5 — Fidelidad de UI/UX con el SAP MES real 🔄 (en curso, formalizada 2026-09-29)
**Objetivo:** que cada pantalla y modal del POD replique su captura del SAP MES real
(estructura, textos, colores y comportamiento visual), y dejar la interfaz cerrada antes
de los overlays guiados de la Fase 6.6, que se anclan a ella.
**No incluye:** cambios de reglas de negocio, del motor de pasos o del puntaje; pasos
nuevos; los overlays guiados; el catálogo de LoFlo; las páginas del simulador que no son
del MES (Login, Cambio de contraseña, StartSession). Si una corrección toca una regla (ej.
Status "In Queue" o el SFC nuevo del Split), se señala y se decide aparte.
**Criterio de cierre:** `docs/ui-reference/inventario-pantallas.md` con todas las
pantallas que tienen captura en ✅ (o con sus diferencias cerradas como *aceptadas*). Las
pantallas sin captura quedan anotadas como pendientes y no bloquean.
**Proceso:** sin spec — las capturas del MES real son la fuente. Cada tanda de
correcciones del usuario = una entrega (ronda de preguntas si hace falta + plan directo en
`docs/specs/fase-6.5-plan.md`) → build + precompilación (+ tests si cambia la BLL) →
checklist → prueba del usuario → commit → actualizar el inventario.
**Commits:** `48b67c1` (entrega 1), `fbcd132` (2), `8fde133` (3), `b832e77` (4).

---

### Fase 6.6 — Overlays guiados 📝 (planeada, requiere `/spec`)
**Objetivo:** en modo Guided, overlays que guían al operador paso a paso sobre la interfaz
ya cerrada en la 6.5 (qué botón, campo o modal usar en cada paso).
**A definir en la spec:** si aplican solo en Guided (en Assessment no pueden revelar el
camino); cómo se anclan a los elementos (anclas estables en botones y campos); que se
definan por tipo de paso y no por área, para que LoFlo los reciba sin trabajo extra.
Entrada: el inventario de pantallas de la 6.5.
**No incluye:** cambios de UI de las pantallas del MES (eso es la 6.5), reglas de negocio
nuevas.

---

### Fase 7 — Catálogo de LoFlo (segunda área) ⛔ (bloqueada solo por el bloqueante #3: flags de negocio)
**Objetivo:** Script SQL con el catálogo real de LoFlo (Sensores).
Solo carga de datos — cero código nuevo. Valida que el motor de Fases 3-4
es verdaderamente genérico y que `RequiresAssembly`/`RequiresDataCollection`
funcionan con la configuración real de LoFlo.
**El script tiene que cargar** (spec 008): flags del área (incluido
`RequiresStartChecklist = 0`), NC Codes, `DataCollectionGroups` con qué grupos son
obligatorios, sus `DataCollectionParameters`, el BOM en `AssemblyComponents` (simulado
por ahora, ≥1 componente si `RequiresAssembly = 1`) y la guía con sus pasos.
**No incluye:** cambios al motor, nuevas pantallas.

---

### Fase 8 — Validación interna
**Objetivo:** Sesión con trainer/operador real sobre R&C y LoFlo.
Correcciones de UX, bugs de flujo, ajustes de catálogo.
**No incluye:** nuevas áreas, Dashboard admin, reportes.

---

### Fase 9 — Deploy del MVP
**Objetivo:** Deploy en servidor interno con IIS. Configuración de
connection string de producción, App Pool, permisos, prueba de humo.
**No incluye:** CI/CD automatizado, monitoreo avanzado.

