# Spec 009: Motor del POD en la BLL

**Status:** Approved (2026-10-05)
**Date:** 2026-10-02
**Plan:** `docs/specs/009-plan.md`
**Origen:** auditoría 2026-10-01 — AUD-16, AUD-25, AUD-26 (`docs/roadmap.md` → "Auditoría 2026-10-01").

## Problem

`GuidedTest.aspx.cs` (~1,400 líneas) es hoy el motor real del simulador, contra la regla
"la lógica de negocio vive en la BLL" de `CLAUDE.md`:

- Crea y modifica la orden simulada: Resource Setup (la crea `ResourceSetupControl`
  directamente en Session), Start, SFC Split, scrap tras Log NC y orden por defecto en el
  avance forzado.
- Decide cuándo una selección de piezas es un intento fallido (Log NC y Complete, distinto
  en Serialized y NonSerialized) y cuándo es solo un aviso de formato.
- Restaura el POD tras un avance forzado por dos caminos distintos (submit y fuera de
  secuencia).
- Maneja el avance de Assembly y Data Collection (la BLL lo muta por referencia).

Consecuencias:
1. Los overlays de la 6.6 se anclarían a una página que mezcla UI y motor.
2. Cada click del POD abre unas 21 conexiones a SQL. `GetContext` recarga sesión, guía y
   área varias veces, y `StepSubmitResult.CurrentScore` se calcula y se descarta (AUD-25).
3. Si la Session se pierde sin que el operador salga (IIS recicla la aplicación y la cookie
   de login sigue vigente), la orden simulada desaparece y la sesión queda **sin salida**:
   Start pide un Resource Setup que ya pasó (AUD-26).
4. Una sesión que el operador deja no siempre queda `Abandoned`. Solo pasa al iniciar
   **la misma** guía. Si inicia otra guía, la anterior queda `InProgress` para siempre.
   Logout no marca nada, y con el historial del navegador se puede volver a la URL vieja.

## Scope

### In

- **Servicio de la BLL dueño del POD** (`BLL/Training/PodService`, nombre a confirmar en
  el plan). Es el único que lee y escribe el estado del POD (orden simulada, avance de
  Assembly y de DC) y el que decide:
  - el contenido de la orden en cada comando: crear, Start, Split, scrap y orden por defecto;
  - qué selección de piezas es intento, aviso o válida (absorbe la lógica de la página y
    usa `PieceSelectionRule`);
  - la restauración tras avance forzado, en **un solo camino** para submit y fuera de
    secuencia;
  - si Close de SFC Split está permitido (hay un split hecho).
- **Comandos** (uno por acción del POD; cada uno recibe `sessionId` y `operatorId`):
  SetupResource, Start, AssembleComponent, SubmitDataCollectionGroup, Split, CloseSplit,
  LogNC, Complete, RegisterOutOfSequence y los submits de Line Clearance y del checklist
  de Start. Cada comando devuelve un resultado con: desenlace del paso (o aviso de
  formato), feedback, orden actualizada, avance y **puntaje actual** (`CurrentScore`, sin
  volver a calcularlo).
- **Contexto cargado una vez por request**: sesión, guía, área y estado del POD. La página
  y los comandos lo comparten; el catálogo del validador se arma solo en los comandos que
  califican.
- **Estado del POD sigue en Session** (`HttpSessionState`), pero solo lo escribe y lee la
  BLL, detrás de una interfaz de almacenamiento (`IPodStateStore`). La implementación de
  Session vive en la capa de presentación. Ni la página ni los controles tocan sus claves.
- **Reconstrucción** cuando el estado falta en una sesión `InProgress` que ya pasó Resource
  Setup (decisión D-10): la BLL arma la orden por defecto en el estado del paso actual, sin
  aviso al operador, sin intento y sin tocar el puntaje (ver Domain Model → Reconstrucción).
- **Una sola sesión activa por operador** (decisión D-9): al iniciar **cualquier** guía y
  al hacer **logout** se marcan `Abandoned` todas las sesiones `InProgress` del operador.
  La URL de una sesión abandonada ya rebota a `StartSession` (sin cambios).
- **Datos de simulación** que hoy están en la página (Shop Order por defecto `303406950`,
  split por defecto de 100, lote de 250, Qty 1 por pieza Serialized, Site `CR9A`, recurso
  = área + `-01`, `LOT-yyyyMMdd`, descripción de material) → **constantes en un solo lugar
  de la BLL** (decisión D-11), con los mismos valores.
- `SimulatedOrder` / `SimulatedOrderRow` salen de `Pages/Training/Models` hacia la BLL.
  `GuidedTest` y los controles quedan solo con UI: visibilidad, modales, binding, banners,
  SweetAlert, header y hints.

### Out

- **Cambios visibles** (decisión D-12): mismo flujo, mensajes, puntaje, pantallas y
  habilitación de botones. Si al mover la lógica aparece un bug, se anota y se decide
  aparte, como en la 6.5. La única excepción es D-9, porque solo cambia el estado
  guardado de sesiones que el operador ya no puede usar.
- Persistir el estado del POD en BD. Se descartó frente a la reconstrucción (D-10).
- Retomar una sesión (decisión del usuario: salir de una sesión la abandona).
- Abandonar en el login o al cerrar la ventana: cerrar la ventana no avisa al servidor y
  la sesión queda `InProgress` hasta el siguiente inicio de guía o logout (decisión del
  usuario).
- Separar `IStepControl` / `CompleteControl` sin UI (deuda conocida). Solo entra si el
  plan muestra que estorba.
- Tests (Fase 5 en pausa, decisión del usuario).
- Overlays (6.6), LoFlo (7), endurecimiento del deploy (AUD-28).

## Domain Model

### Estado del POD (por `TrainingSession`, en Session)

| Elemento | Forma | Hoy | Después |
|---|---|---|---|
| Orden simulada | `ShopOrderInput`, `SFCCode`, `MaterialDescription`, `Status`, `ProcessLot`, `Rows[]` {`RowId`, `SFCCode`, `Qty`, `Status` New/Active, `IsFromSplit`} | `Pages/.../Models`, escrita por `ResourceSetupControl` y la página | BLL; solo la escribe el servicio |
| Avance de Assembly | ids de componentes ensamblados | página + BLL (muta por referencia) | solo el servicio |
| Avance de DC | piezas fijadas, índice actual, grupos guardados por pieza | página + BLL (muta por referencia) | solo el servicio |

Ciclo de vida: nace en Resource Setup (también con Setup State "Open", como hoy), cambia
con Start, Split y scrap, y se elimina al abandonar. Al completar, hoy queda huérfana en
Session (spec 005); se mantiene así.

### Reconstrucción (D-10)

Si el estado falta y el paso actual es posterior a Resource Setup, el servicio aplica
sobre la orden por defecto (Shop Order genérica `303406950`) los mismos efectos de un
avance forzado, en orden:

| Paso ya resuelto | Efecto aplicado |
|---|---|
| Resource Setup | orden por defecto (en Serialized, con 3-5 piezas nuevas) |
| Search/Start | todas las piezas en Active (como el Start forzado) |
| SFC Split (NonSerialized) | split por defecto de 100 (D-1) |
| Log NC (NonSerialized) | se scrapea la fila del split (queda el lote con su Qty restante) |
| Log NC (Serialized) | sin efecto: no se sabe qué piezas se scrapearon; quedan todas para Complete |

- Assembly o DC **a medias** en el paso actual: el avance se pierde y el paso se rehace
  desde el primer componente o la primera pieza. Sin intento extra y sin penalización; los
  intentos ya registrados en BD se conservan.
- Es silencioso: no hay banner ni aviso.
- Sin Resource Setup resuelto no hay nada que reconstruir: el estado vacío es el normal.

### Sesiones (D-9)

- `TrainingSessionService.Start` abandona **todas** las sesiones `InProgress` del operador,
  no solo las de esa guía. SP nuevo o modificado:
  `spTrainingSessionAbandonActiveByOperator` sin `@TrainingGuideId`. Lo corre el usuario
  (`03`).
- Logout (`GuidedTest.BtnLogout_ServerClick`) llama a la BLL para abandonar las sesiones
  `InProgress` del operador antes de `Session.Abandon()`. Es el único logout de la app.

## API Contract

No hay endpoints HTTP. El contrato es el del servicio de la BLL hacia `GuidedTest`:

| Comando | Entrada (además de `sessionId`, `operatorId`) | Resultado |
|---|---|---|
| SetupResource | Shop Order capturada, recursos y herramientas, Setup State | desenlace + orden |
| SubmitLineClearance | Sí/No | desenlace |
| Start | ids de filas seleccionadas; respuestas del checklist si `RequiresStartChecklist` | aviso de formato, o desenlace + orden con piezas Active |
| AssembleComponent | id del componente | componente aceptado/rechazado; desenlace al completar el BOM |
| SubmitDataCollectionGroup | pieza, grupo, valores | grupo guardado; desenlace al completar todas las piezas |
| Split | fila, cantidad | aviso de formato, o orden con la fila nueva (sin intento, como hoy) |
| CloseSplit | — | aviso (sin split) o desenlace |
| LogNC | filas seleccionadas, código NC, datos | aviso, intento fallido por selección, o desenlace + orden sin las filas scrapeadas |
| Complete | filas seleccionadas | aviso, intento fallido por selección, o desenlace final |
| RegisterOutOfSequence | botón pulsado | desenlace; si es forzado, orden restaurada |
| Abandon / AbandonAllActive | — | — |

**Desenlace** = `Correct` / `Incorrect` / `FailedForced` + número de intento + puntaje
actual + si la sesión terminó (y si aprobó). Los nombres exactos los define el plan.

## Authorization

Sin cambios de modelo: cada comando valida dueño, estado `InProgress` y paso actual
(AUD-05/06). Violarlo es una excepción, no un intento. El estado del POD se indexa por
`sessionId` dentro de la Session del propio operador.

## Edge Cases & Failure Modes

- **Session perdida a mitad de un paso** → reconstrucción (arriba). F5 sigue
  reconstruyendo el POD desde la orden.
- **Doble click o postback repetido**: se mantienen los guards de sender actuales y el
  `if (!Visible) return;` de Resource Setup.
- **Atomicidad**: el intento y el avance se guardan juntos en BD (`spStepAttemptRecord`,
  sin cambios). El estado del POD vive en Session y se actualiza **después** de que el
  intento quedó registrado. Si la escritura en Session fallara, la reconstrucción cubre el
  hueco.
- **Varias pestañas con la misma sesión**: comparten la Session, como hoy (el último gana).
- **Catálogo cambiado a mitad de sesión**: igual que hoy, sin caché del validador (AUD-13).
- **Sesión abandonada con la pestaña abierta** (otro inicio de guía en otra pestaña): el
  siguiente postback rebota a `StartSession`, como hoy.

## Non-Functional Requirements

- Un click del POD carga sesión, guía y área **una vez** por request. Objetivo: pasar de
  unas 21 conexiones a SQL a unas 8 o menos en un submit (se mide antes y después en el
  plan, contando las llamadas a `Database`).
- `GuidedTest.aspx.cs` queda sin reglas de negocio. La meta orientativa es menos de la
  mitad de sus líneas actuales; no es un criterio de aceptación.
- Build en 0 warnings, como dejó la auditoría.

## Integrations

Ninguna nueva. Cambia un SP (`spTrainingSessionAbandonActiveByOperator`, script `03`) y no
cambia el esquema.

## Acceptance Criteria

**Regresión (D-12): cero cambios visibles**

- **AC-1** — Given R&C/Moldeo (NonSerialized, con checklist de Start), When se recorre la
  guía completa en **Guided**, Then el flujo, los modales, los banners, el Browse Work List,
  el split, el scrap y el resultado final son iguales a antes de la spec.
- **AC-2** — Given Moldeo en **Assessment**, When se aciertan todos los pasos al primer
  intento, Then el puntaje final es 100 y "Aprobado".
- **AC-3** — Given Moldeo en Assessment, When se falla un paso 1, 2 y 4 veces (y fuera de
  secuencia en otro), Then la penalización (80/60/0%), los mensajes con intentos restantes
  y el avance forzado son iguales a antes. Cada avance forzado deja el POD usable: Resource
  Setup → orden por defecto, Start → piezas cargadas, Split → fila de 100.
- **AC-4** — Given Redefinity (Serialized, Assembly, sin checklist), When se recorre en
  Guided y en Assessment, Then la multiselección, `[N SELECTED]`, el BOM, la DC por pieza,
  Log NC y Complete se comportan igual que antes, incluidos los avisos de selección parcial
  sin penalización.
- **AC-5** — Given Log NC o Complete con una selección de piezas inválida, When se envía,
  Then cuenta como intento exactamente en los mismos casos que antes (NonSerialized: sin la
  fila del split o sin fila; Serialized: ninguna pieza).

**Capas (AUD-16)**

- **AC-6** — Given el código tras la spec, When se revisan `GuidedTest.aspx.cs` y los
  controles de paso, Then ninguno lee ni escribe las claves de Session del POD ni modifica
  la orden simulada. Toda decisión de orden, selección, avance o restauración está en la
  BLL, y `SimulatedOrder` ya no está en `Pages/`.

**Rendimiento (AUD-25)**

- **AC-7** — Given un submit correcto de Line Clearance, When se cuentan las llamadas a la
  BD del request, Then sesión, guía y área se cargan una vez y el puntaje mostrado es el
  `CurrentScore` del resultado, sin recalcularlo. Total ≤ 8 conexiones.

**Reconstrucción (AUD-26, D-10)**

- **AC-8** — Given una sesión de Moldeo en Assessment detenida en Log NC (después del
  split), When se pierde la Session (reciclo de IIS o `Session.Clear` en dev) y se recarga
  el POD, Then el POD muestra la orden por defecto con el lote y la fila de split de 100,
  sin mensaje. El operador puede hacer Log NC y Complete, y el puntaje es el mismo que
  antes de perder la Session.
- **AC-9** — Given Redefinity detenido a mitad de Data Collection (2 de 4 piezas
  guardadas), When se pierde la Session, Then el POD muestra piezas nuevas en Active y DC
  empieza desde la primera pieza. Completarla no agrega intentos ni penalización.
- **AC-10** — Given una sesión en Resource Setup (paso 1), When se pierde la Session, Then
  el POD sigue vacío, como al empezar.

**Sesión única (D-9)**

- **AC-11** — Given una sesión de Moldeo `InProgress`, When el operador inicia una guía
  **distinta**, Then la de Moldeo queda `Abandoned` con sus intentos y su URL rebota a
  `StartSession`.
- **AC-12** — Given una sesión `InProgress`, When el operador hace logout y vuelve a entrar,
  Then esa sesión está `Abandoned` y volver por el historial a su URL rebota a
  `StartSession`.
- **AC-13** — Given una sesión `InProgress`, When el operador cierra la ventana y vuelve a
  entrar, Then la sesión sigue `InProgress` hasta que inicie una guía (comportamiento
  aceptado).

## Deferred Decisions

| Decisión | Fallback elegido | Se revisa cuando |
|---|---|---|
| Persistir el estado del POD en BD | Reconstrucción desde la orden por defecto (D-10) | Se pida retomar sesiones o la reconstrucción no alcance en la Fase 8 |
| Datos de simulación por área | Constantes únicas en la BLL (D-11) | LoFlo u otra área necesite valores distintos (Fase 7) |
| Abandonar en el login / al cerrar la ventana | No; solo al iniciar guía y en logout | Aparezcan sesiones `InProgress` huérfanas en reportes |
| Separar `IStepControl` / `CompleteControl` | Se deja como está | El plan muestre que estorba al servicio |

## Decisiones (registrar en `docs/roadmap.md` al aprobar)

- **D-9** (usuario, 2026-10-02): una sola sesión activa por operador. Iniciar cualquier
  guía o hacer logout abandona las `InProgress`. No en el login.
- **D-10** (usuario, 2026-10-02): el estado del POD sigue en Session. Si falta, se
  reconstruye en silencio con la orden por defecto, y Assembly/DC a medias se rehacen desde
  cero sin penalización. No se persiste en BD.
- **D-11** (usuario, 2026-10-02): los datos de simulación de la página pasan a constantes
  de la BLL, sin cambiar valores.
- **D-12** (usuario, 2026-10-02): refactor con cero cambios visibles. Los bugs encontrados
  se deciden aparte.
- Trabajo directo en `TrainingApp`, un commit por entrega verificada (usuario).

### Propuestas mías (confirmadas por el usuario al aprobar, 2026-10-05)

1. La interfaz `IPodStateStore` en la BLL, implementada sobre `HttpSessionState` en la
   capa de presentación, para que la BLL no dependa de `System.Web`.
2. Que Log NC en Serialized no se reconstruya (quedan todas las piezas para Complete).
3. Que el objetivo de rendimiento sea ≤ 8 conexiones por submit de Line Clearance (AC-7).
4. Orden de entregas para el plan: (E1) mover `SimulatedOrder` y crear el servicio con
   contexto único y `CurrentScore` → (E2) Resource Setup, Start, Split → (E3) Assembly y
   DC → (E4) Log NC, Complete, fuera de secuencia y restauración única → (E5)
   reconstrucción → (E6) sesión única (D-9, con el script `03`).

## Open Questions

_(ninguna)_
