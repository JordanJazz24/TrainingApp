# Plan · Refactor de las clases grandes (AUD-74, D-15)

**Estado:** aprobado por el usuario el 2026-11-03 (Planeación).
**Origen:** `docs/audits/2026-11-02-auditoria-pre-deploy.md` → AUD-74 y D-15 (inyectar los
repositorios en `PodService`). **Decisiones:** D-258 (alcance y cuándo) y D-259 (inyección y
tests).
**Camino:** plan por entregas sin spec (`process.md` → "1. Clasificar el pedido", fila 5: deuda
sin cambio de comportamiento; el plan es grande, así que va con entregas).

## Objetivo

Que las cuatro clases más grandes se puedan leer, revisar y cambiar sin tocar todo a la vez, y
que lo que hoy solo se prueba en el navegador (registrar el intento, el avance forzado y el
puntaje final de `PodService`) quede cubierto con tests sin BD. **Sin cambiar el
comportamiento.**

| Clase | Hoy | Qué se hace |
|---|---|---|
| `BLL/Training/PodService.cs` | 1 294 líneas; 3 repositorios y 2 servicios con `new`; sin tests | Inyección con interfaces mínimas, tests del núcleo y división por comando |
| `Pages/Training/GuidedTest.aspx.cs` | 1 903 líneas | Solo archivos parciales por área y helpers puros (liviano, D-258) |
| `Pages/Training/Guide/GuideScriptBuilder.cs` | 1 064 líneas, sin estado | Un archivo por paso y un kit común |
| `BLL/Reports/DashboardService.cs` | 818 líneas, ~280 de DTOs; ya con DI y 33 tests | DTOs aparte y un colaborador por reporte |

## Reglas para todas las entregas

- **Cero cambio de comportamiento:** mismo HTML, mismos postbacks, mismos mensajes, mismo
  puntaje, mismos datos en la BD. Sin cambios de `.aspx`, `.ascx`, `.master`, CSS, JS ni SQL.
- **Firmas públicas estables:** `PodService`, `DashboardService` y `GuideScriptBuilder.Build` /
  `AddMesErrorNote` quedan como fachadas con los mismos métodos públicos; las páginas, los
  controles, `Export.ashx` y `ExcelReports` no cambian sus llamadas. El constructor actual de
  `PodService(IPodStateStore)` sigue existiendo y crea las implementaciones con `new` (patrón de
  `DashboardService`).
- **Cada entrega compila, mantiene todos los tests en verde y se puede revertir sola.** El conteo
  de tests solo sube.
- **Un bug que aparezca al refactorizar no se arregla en silencio:** se para, se presenta como
  B-n y decide el usuario (D-12). El refactor lo conserva tal cual.
- **Rama y worktree** (`process.md` §7): `refactor-aud74` en `.claude/worktrees/refactor-aud74`,
  con BD propia de LocalDB para probar. Se integra a `TrainingApp` con `--ff-only` al cerrar la E7,
  con el "funciona" de la regresión completa; la 2.ª publicación (Fase 9, E6) sale después.
- Invariantes que no se tocan: el intento y el avance o cierre se guardan juntos
  (`spStepAttemptRecord`, AUD-05/06) y `RecordAttempt` actualiza el `PodContext` después de
  guardar; el estado del POD solo lo cambia `PodService` (`SaveState` es el único que persiste);
  `ModePolicy` decide qué hace cada modo; la guía no decide el flujo.

## E1 · DashboardService — Esfuerzo: `high`

- [ ] Los DTOs (líneas 14-295) a `BLL/Reports/DashboardModels.cs`, mismo namespace.
- [ ] Un colaborador interno por reporte (Operadores, Pasos, Resumen, Seguimiento, Export), con
  nombres que no choquen con los DTOs (`SummaryReport` ya existe). `DashboardService` queda como
  fachada: mismo constructor, mismos métodos, `RequireReports` (`CanViewReports`) se sigue
  verificando en cada método público.

**Verificación:** Rebuild, tests (los 33 de `DashboardServiceTests` sin cambios) y `/revisar`
`medium`. Checklist del usuario: Resumen, Seguimiento, Operadores, Operador (y su ficha), Pasos y
un Excel de cada uno, con el filtro de período.

## E2 · GuideScriptBuilder — Esfuerzo: `medium`

- [ ] `partial class` en `Pages/Training/Guide/`: un archivo por paso (ResourceSetup,
  CustomResourceSetup, LineClearance, Start, Assembly, DataCollection, SfcSplit, LogNC, Complete,
  Logout, MesLogin) y `GuideScriptKit` con `StepScript`, `H`, `B`, `Bold`, `Anchor` y los helpers
  compartidos. `ScriptFor` sigue despachando por `StepType`.
- [ ] `LogNC` (~133 líneas) y `DataCollection` (~84) partidos en métodos privados por pantalla,
  con el mismo texto y el mismo orden.

**Verificación:** Rebuild, tests (`MesErrorNotesTests`) y `/revisar` `low`. Checklist del
usuario: una sesión Guided completa de Moldeo, una de LoFlo y la de Wires, mirando que la guía
diga y señale lo mismo en cada paso (incluidos Split, Log NC y Custom Resource Setup).

## E3 · PodService: inyección (D-15, D-259) — Esfuerzo: `high`

- [ ] En `DAL/Interfaces/` (o donde viven `IUserRepository` e `IDashboardRepository`):
  `ITrainingSessionRepository` (`GetById`) e `IStepAttemptRepository` (`GetBySessionId`,
  `Record`), solo con lo que usa `PodService`; las clases concretas las implementan.
- [ ] En la BLL: `IGuideCatalog` (lo que `PodService` y `PodContext` piden a
  `TrainingGuideService`) e `ITrainingSessionService` (`Start`, `Abandon`); los servicios
  concretos las implementan.
- [ ] `PodService` con un constructor que recibe `IPodStateStore` y esas cinco interfaces (más
  `ScoreCalculator`, que es puro); el de hoy delega creando las concretas. `PodContext` recibe
  `IGuideCatalog`.
- [ ] Sin cambios en `GuidedTest.aspx.cs` ni en `StartSession.aspx.cs`.

**Verificación:** Rebuild, `aspnet_compiler` (copia sin `obj/Release` ni `Properties/publish`),
tests y `/revisar` `medium`. Checklist del usuario: iniciar, avanzar y terminar una sesión de
Práctica libre, y abandonar otra.

## E4 · PodService: tests del núcleo (D-259) — Esfuerzo: `high`

Tests con fakes en memoria (como `FakeDashboardRepository`), sin BD. **No cambia código de
producción.**

- [ ] `LoadContext`: devuelve contexto solo para una sesión propia y en curso; otra sesión o una
  cerrada es una excepción, no un intento.
- [ ] `RecordAttempt` (vía `SubmitStep`): el intento y el avance se registran juntos (una sola
  llamada a `Record` con el avance o el cierre) y el `PodContext` queda actualizado después.
- [ ] `SubmitStep` correcto e incorrecto en Guided, Práctica libre y Assessment: qué se registra,
  el paso actual que define el servidor y el puntaje según `ModePolicy`.
- [ ] `Skip` y el avance forzado (`RestoreAfterForcedAdvance`).
- [ ] `Complete`: cierre de la sesión y `FinalScore`.
- [ ] `docs/status.md` → "Cobertura de tests": se actualiza al cerrar.

**Verificación:** compilar `TrainingApp.Test` y correr todos los tests. `/revisar` `medium` sobre
los tests y los fakes. Sin checklist de navegador (no cambia la app).

## E5 · PodService: división por comando — Esfuerzo: `high`

- [ ] Colaboradores internos en `BLL/Training/Pod/` (o el nombre que encaje con el árbol de
  `docs/architecture.md`), uno por cluster: núcleo de intento (`RecordAttempt`, `FailedAttempt`,
  `SaveState`, `DiscardProgress`, `EnsureInProgress`, `EnsureCurrentStep`), Resource Setup / Line
  Clearance / Start, Assembly y Data Collection (`OpenDataCollection`, ~93 líneas, partido en
  métodos), Split, Log NC, Complete / Logout / acciones sin paso, y Skip / avance forzado (`Skip`,
  ~89 líneas, partido).
- [ ] `PodService` queda como fachada con los mismos métodos públicos; el `PodContext` y el
  `PodState` se pasan, no se copian.
- [ ] Los tests de la E4 pasan sin tocarlos.

**Verificación:** Rebuild, `aspnet_compiler`, tests y `/revisar` `high` sobre `BLL/Training`.
Checklist del usuario: una sesión de Assessment completa de Moldeo (NonSerialized, con Split) y
una de LoFlo (Serialized, con Log NC), más una Guided con un error a propósito y un Skip.

## E6 · GuidedTest liviano (D-258) — Esfuerzo: `medium`

- [ ] `partial class` por área, sin mover ni reordenar ninguna asignación: infraestructura del
  request, visibilidad y apertura de controles, submit de controles y su continuación, handlers
  de los botones del POD, pantalla MES Login y limpieza, pintado de barras y botones, listas y
  feedback.
- [ ] Lo puro, a helpers estáticos en `Pages/Training/` (sin controles ni `Session`):
  `NoticeText`, `AttemptFeedback`, `ModeLabel`, `FormatScore`, `FindingDisposition` y similares.
- [ ] **No** se extraen a otras clases los métodos que tocan controles del `.designer` ni se
  cambia el patrón "`Visible` persiste en ViewState: se asigna en cada request".

**Verificación:** Rebuild, `aspnet_compiler`, tests y `/revisar` `medium`. Checklist del usuario:
la regresión de la E7 en Guided.

## E7 · Regresión, integración y cierre — Esfuerzo: `medium`

- [ ] Regresión completa del usuario en el worktree: Guided, Práctica libre y Assessment en
  Moldeo, LoFlo y Wires (con Split, Log NC, Custom Resource Setup, Skip, abandono, F5 y el fin de
  sesión con "Últimas sesiones"); Ingreso a MES y limpieza al salir; login, cambio de contraseña
  y Usuarios; las páginas del dashboard y sus Excel.
- [ ] Integrar a `TrainingApp` con `git merge --ff-only refactor-aud74`, quitar el worktree y
  borrar la rama.
- [ ] Docs (Desarrollo): `docs/architecture.md` (árbol del código: `Pod/`, `DashboardModels`,
  los parciales de la guía y de `GuidedTest`, las interfaces nuevas), `docs/status.md` (deuda y
  cobertura), `docs/history.md` y la auditoría (AUD-74 ☑). Avisar a Planeación para el roadmap,
  `decisions.md` (D-15 cerrada) y la E6 de la Fase 9.

**Hecho cuando:** las cuatro clases están partidas, `PodService` tiene tests de su núcleo, todos
los tests pasan, la regresión tiene el "funciona" y el refactor está en `TrainingApp` antes de
republicar.

## No incluye

- Cambios de markup, `UserControl`s nuevos o mover a otras clases la lógica de `GuidedTest` que
  toca controles.
- Arreglar bugs que aparezcan (van como B-n, D-12) o cambiar reglas, textos o puntaje.
- Tests de Split, Log NC, Assembly y Data Collection en `PodService` (D-259: solo el núcleo).
- AUD-40 y AUD-42 (diferidos, D-16), la cobertura de `StepValidator`, CSP y las librerías de
  D-131.
- ORM, contenedor de DI o .NET moderno (`CLAUDE.md` → "Tech Stack").
