# Plan — Spec 012: Práctica libre

**Spec:** `012-modo-practica.md` (aprobada 2026-10-20) · **Estado:** **aprobado por el usuario el 2026-10-20**, con las
decisiones D-82 a D-90 y los "Puntos confirmados" del final.
**Autoría:** sesión de Arquitectura y Planeación, verificado contra el código. Lo ejecuta
Desarrollo (`docs/process.md` → "Tres sesiones de trabajo").

> **Enmienda 1 del plan (2026-10-20, usuario, a pedido de Desarrollo durante la E3):**
>
> - **D-84, repintado tras un salto:** reemplaza "reusa `RepaintAfterForcedAdvance`". Ese
>   repintado sirve para un solo paso forzado, e Ir a… adelanta varios de tipos distintos.
>   Si el salto cierra la sesión, el resumen sale en el mismo request, como en el plan. Si
>   no, `Response.Redirect` a la misma URL (el GET de un F5, que ya arma el POD desde
>   `PodState`), sin la alerta "Paso fallido". Además, un F5 posterior no reenvía el salto.
> - **D-91 · Ver pista con un modal abierto** (P-8 "siempre" no cambia): en Resource Setup,
>   Line Clearance, el checklist de Start y el Split, el backdrop de Bootstrap tapa la barra.
>   La barra de práctica queda **por encima del backdrop** (`z-index` en `pod.css`), y se
>   verifica que el foco del modal no bloquee el SweetAlert de la pista ni el `select` de
>   Ir a…. Si no se puede, respaldo: un botón de pista dentro de esos 4 controles, con el
>   estilo del simulador. Assembly, DC y Log NC ya funcionan.

Cuatro entregas: primero el refactor sin cambio de comportamiento y después el modo, sus
herramientas y su resumen. Cada una con el ciclo de `docs/process.md` §2: rebuild,
`aspnet_compiler` si toca markup, tests si toca la BLL, `/code-review` calibrado, checklist
en el navegador, "funciona" y commit con el roadmap.

## Lo que se verificó en el código antes de planear

- **Decisiones por modo** (las reemplaza `ModePolicy`):
  - `ScoreCalculator.GetPenaltyFactor` y `ShouldForceAdvance`.
  - `PodService.CheckSequence` (`:846`): fuera de secuencia, intento solo en Assessment.
  - `PodService.MesFailure` (`:211`): el Repositorio fuera de turno se ignora salvo en
    Assessment.
  - `GuidedTest.aspx.cs`: `LitMesMode` (`:277`), panel del hallazgo con `data-guide-reveal`
    (`:327`), `RegisterGuide` (`:400`), `BuildIncorrectFeedback` (`:854`), Aprobado/Reprobado
    del resumen (`:1263`), `GuideNCCode` (`:1311`), badge del modo (`:1526`) y
    `SetActiveButton` (`:1536`, todos habilitados solo en Assessment).
  - `StartSession.aspx.cs:63`: radios Guided y Assessment.
  - Ningún control (`UI/Controls`) ni `pod-guide.js` decide por modo. El `state.Mode` de
    `MesLoginControl` es otra cosa: entrada o limpieza.
- **Avance y cierre:** `PodService.RecordAttempt` (`:1023`) ya resuelve un intento no
  correcto cuando hay avance forzado. Si quedan pasos, `nextStepOrder`; si es el último,
  `finalScore`. Siempre en una sola llamada a `spStepAttemptRecord`. El SP no valida
  `@Reason` ni el modo, así que **un paso saltado no necesita cambios en la BD**: es el
  mismo registro que el avance forzado, con `Reason = 6`.
- **Restaurar el POD:** `RestoreAfterForcedAdvance` (`:860`) cubre Resource Setup (orden por
  defecto), Start (inicia todo y genera los hallazgos de Log NC), SFC Split, Log NC y
  Complete. `DiscardProgress` (`:997`) completa lo ensamblado al resolver Assembly y descarta
  el avance de DC, del Ingreso y de la limpieza. Por eso Ir a… = saltar paso por paso, en
  orden, deja el mismo estado que un Assessment forzado. Los hallazgos existen desde el Start,
  así que un Log NC saltado aplica la disposición del suyo.
- **Resuelto = `CurrentStepOrder` mayor que el orden del paso** (`PodContext.IsResolved`) y
  la reconstrucción en Serialized (`RebuildState`) se basa en eso: un paso saltado se
  reconstruye igual que uno forzado.
- **Mensajes:** el intento fallido llega por `ApplySubmitResult` → SweetAlert "Incorrect"
  con `BuildIncorrectFeedback(mode, …, incorrectFeedback)`. `incorrectFeedback` lo trae el
  evento del control (hoy solo `LogNCControl`, `WrongButtonFeedback`). Las pantallas del MES
  (`HandleMesAction`) y el Split pasan por el mismo `ApplySubmitResult`. Fuera de secuencia
  sin intento (`result.Step == null`), `HandleOutOfSequence` (`:758`) hoy no muestra nada; en
  el MES, `HandleMesAction` muestra un aviso propio del Repositorio.
- **Pistas:** `GetHint(StepType)` es un `static` privado de `GuidedTest.aspx.cs` (`:1708`),
  con una pista para cada tipo de paso.
- **Puntaje en pantalla:** `UpdateScoreWidget` (`:1521`) pinta "Score: x%" en el header
  (`GuidedTest.aspx:45`) en todos los modos.
- **Resumen:** `ShowCompletionPanel` (`:1244`) se arma en el mismo request que cierra la
  sesión, con el `PodContext` cargado. `ctx.Attempts` ya trae todos los intentos (los carga
  `LoadContext` con `spStepAttemptGetBySession`), así que **no hace falta otra consulta**.
- **Lugar de la barra:** Abandon ya es "del simulador". Está en el pie del POD
  (`GuidedTest.aspx:288-293`, `confirmAbandon` con SweetAlert) y en la barra del escritorio
  (`mes-simbar`, `:317-323`). Ahí van las herramientas de práctica.
- **Tests:** `ScoreCalculatorTests` y `MesLoginRuleTests` llaman a
  `ScoreCalculator.GetPenaltyFactor` y `ShouldForceAdvance` con `SessionMode`. El csproj de
  tests lista cada archivo (`<Compile Include>`).

## Decisiones (propuestas para el usuario)

- **D-82 · `ModePolicy` en `BLL/Scoring/`:** cruza features (puntaje, secuencia y POD), como
  pide `CLAUDE.md` → "Domain". Clase inmutable con una instancia por modo,
  `ModePolicy.For(SessionMode)`, con las capacidades de la tabla de la spec (P-12).
  `ScoreCalculator` conserva sus firmas con `SessionMode` y delega en la política, así que
  los tests existentes no cambian.
- **D-83 · Saltar = avance forzado voluntario:** `RecordAttempt` recibe si el intento
  resuelve el paso (hoy lo decide solo `ShouldForceAdvance`). `PodService.Skip` registra
  `IsCorrect = false`, `Reason = Skipped` y `Action = "Practice.Skip"` (en `AttemptActions`)
  y aplica `RestoreAfterForcedAdvance`. **Ir a…** repite eso paso por paso hasta el destino,
  cada uno en su propia transacción. Si uno falla a la mitad (por ejemplo, un doble envío que
  ganó el otro), la sesión queda coherente en un paso intermedio y la página la vuelve a
  cargar. Sin cambios en el SP.
- **D-84 · Resultado del salto:** `PodCommandResult` con `Skipped = true` y el `Step` del
  último paso saltado (`FailedForced` o `SessionCompleted`). La página reusa
  `RepaintAfterForcedAdvance` y `HandleCorrectSubmit` **sin** la alerta "Paso fallido". Si el
  salto cierra la sesión, muestra el resumen.
- **D-85 · Nombres de los pasos con variante:** `StepNames.Display(guide, step)` en
  `BLL/Training/`, una función pura. Line Clearance es "(apertura)" o "(cierre)" según haya
  otro antes; Log NC, por tipo. Lo usan Ir a… y el resumen.
- **D-86 · Resumen desde el `PodContext`:** `PodService.AttemptSummary(ctx)` (o un builder
  puro en `BLL/Training/`) devuelve una fila por paso: nombre, intentos y si fue saltado. Usa
  `ctx.Guide.Steps` y `ctx.Attempts`, sin consulta nueva. Esto ajusta lo que la spec dice en
  "API Contract" y en el punto 6 de "Scope".
- **D-87 · Pistas fuera del code-behind:** `GetHint` pasa a `Pages/Training/StepHints.cs`
  (presentación, sin cambiar los textos). En Práctica libre, cada render deja en la página la
  pista del paso actual con `JavaScriptStringEncode`. La respuesta de un fallo agrega además
  el texto propio del control, si lo trajo. Ver pista abre un SweetAlert en el cliente. El
  texto del último fallo **no** se guarda entre requests: después de otro postback, Ver pista
  muestra solo la del paso.
- **D-88 · Aviso de otro paso:** SweetAlert `info` "Ese no es el paso actual." para los
  botones del POD, el ícono Logout y el Repositorio fuera de turno (P-4). En Guided, el aviso
  propio del Repositorio no cambia.
- **D-89 · Barra de práctica:** Ver pista, Saltar paso e Ir a… a la derecha de Abandon, en el
  pie del POD y en `mes-simbar`, con su estilo (no del MES). Solo se pintan en Práctica
  libre.
  - Saltar paso: botón de servidor con confirmación en SweetAlert, como `confirmAbandon`.
  - Ir a…: SweetAlert con un `select` de los pasos siguientes (la lista llega en JSON
    codificado). El `Order` elegido va en un `HiddenField` y se envía con el botón de
    servidor.
  - Un postback por acción. El anti-CSRF lo pone `Training.Master` (`AntiXsrfMasterPage`).
- **D-90 · Puntaje oculto en Práctica libre:** el header no muestra "Score: x%" (`PnlScore`
  oculto), por coherencia con P-10 y P-11 (confirmado por el usuario).

## Entrega 1 — `ModePolicy` sin cambio de comportamiento (AC-1, AC-2) ✅

> **Cerrada el 2026-10-20**, probada por el usuario. Review `high`: equivalencia confirmada;
> se aplicaron 2 de 8 hallazgos (`MaxScoredAttempts` vive en `ModePolicy` y `ScoreCalculator`
> lo reexpone; `ModeLabel` sin "Guided" por defecto). Fuera de `ModePolicy` solo quedan la
> elección del modo en `StartSession` y la etiqueta de `ModeLabel`. Tests: 169 (18 nuevos).

- [x] `BLL/Scoring/ModePolicy.cs` (D-82): instancias de Guided y Assessment con las
      capacidades de la spec (`ShowsGuide`, `EnablesAllButtons`, `OutOfSequence`,
      `NoLogicButtons`, `PenalizesRetries`, `ForcesAdvance`, `FailureFeedback`,
      `HintOnDemand`, `AllowsSkip`, `Summary`) y `PenaltyFactor(attempt)` /
      `ForcesAdvanceAt(attempt)`. Un `SessionMode` desconocido es una excepción.
- [x] `ScoreCalculator.GetPenaltyFactor` / `ShouldForceAdvance` delegan en
      `ModePolicy.For(mode)`, con las mismas firmas.
- [x] `PodService.CheckSequence` y `MesFailure` preguntan `OutOfSequence`. `ClickNonStepAction`
      pregunta `NoLogicButtons`.
- [x] `GuidedTest.aspx.cs`: los 8 puntos de "Lo que se verificó" pasan a la política
      (`ShowsGuide` para la guía, el reveal y `GuideNCCode`; `EnablesAllButtons`;
      `FailureFeedback`; `Summary`). La etiqueta y el badge del modo salen de un solo método,
      `ModeLabel`.
- [x] `TrainingApp.Test/ModePolicyTests.cs` (+ `<Compile Include>`): la tabla de capacidades
      de Guided y Assessment, y las penalizaciones y el avance forzado iguales a los de hoy.
      `ScoreCalculatorTests` y `MesLoginRuleTests` en verde sin tocarlos.
- [x] Verificación: rebuild, `aspnet_compiler` (no debería cambiar el markup), tests y
      regresión en el navegador de Guided y Assessment de una área Serialized y de Moldeo:
      - botones;
      - guía;
      - fuera de secuencia;
      - mensaje de fallo;
      - avance forzado;
      - Repositorio fuera de turno;
      - resumen.
- **Review:** `high` (toca el puntaje, la secuencia y todo el POD), foco en la equivalencia
  de comportamiento.

## Entrega 2 — El modo Práctica libre (P-1 a P-7; AC-3 a AC-8, AC-14) ✅

> **Cerrada el 2026-10-20**, probada por el usuario. Review `medium`: 1 error corregido (en el
> Ingreso, Log On sin Hora a Hora también llega como `OutOfSequence`: en Práctica libre es
> intento fallido del paso y solo el Repositorio fuera de turno da el aviso de D-88); además,
> `ShowsScore` como capacidad propia, `OutOfSequence` recibe el `OutOfSequenceHandling` y el
> título del aviso es "Otro paso". El aviso viaja en `PodCommandResult.NotCurrentStepNotice`.
> El resumen de esta entrega oculta el puntaje; la tabla por paso es de la E4. Tests: 176.

- [x] `Domain/TrainingSession.cs`: `SessionMode.FreePractice = 2`. Columna de Práctica libre
      en `ModePolicy`. `00_Schema.sql`: comentario `Mode: … 2=FreePractice` (solo comentario,
      el usuario no necesita correr nada).
- [x] `StartSession.aspx(.cs)`: tercer radio `RbFreePractice`, " Práctica libre (sin guía ni
      puntaje, con pistas)". Guided sigue preseleccionado.
- [x] Etiqueta "Práctica libre" en el header (badge `badge-practice` en `pod.css`), en
      `LitMesMode` y en el resumen (`LitFinalMode` deja de usar `Mode.ToString()`).
- [x] Comportamiento por política, sin ramas nuevas:
  - sin guía;
  - todos los botones habilitados;
  - panel del hallazgo visible de inmediato;
  - sin `GuideNCCode`;
  - botón de otro paso, Logout fuera de turno y Repositorio fuera de turno con el aviso de
    D-88, sin intento;
  - botones sin lógica inertes;
  - fallo con "Respuesta incorrecta.", sin intentos restantes;
  - sin penalización ni avance forzado.
- [x] D-90 (si se confirma): `PnlScore` oculto.
- [x] Tests: `ModePolicyTests` con la columna de Práctica libre (penalización 1.00 en todo
      intento y nunca fuerza el avance).
- [x] Verificación: rebuild, `aspnet_compiler` y tests. Checklist en el navegador con una
      sesión de LoFlo y una de Moldeo de punta a punta:
  - botón de otro paso (también en el Ingreso y la limpieza);
  - Logout antes de tiempo;
  - Work Instructions;
  - 5 fallos seguidos en DC;
  - el Split;
  - Abandon;
  - Guided y Assessment sin cambios.
- **Review:** `medium`, foco en que la BLL no registra intento en `Notice` y en que Práctica
  libre nunca fuerza el avance.

## Entrega 3 — Ver pista, Saltar paso e Ir a… (P-8, P-9; AC-9 a AC-11b, AC-13)

- [ ] `Domain/TrainingSessionStepAttempt.cs`: `AttemptReason.Skipped = 6`. `00_Schema.sql`:
      comentario de `Reason`. `AttemptActions.Skip = "Practice.Skip"`.
- [ ] `PodService.Skip(ctx, targetOrder)` (D-83):
  - `EnsureInProgress` y `AllowsSkip`; si no, excepción (AC-13);
  - `targetOrder` dentro de la guía (si no, excepción);
  - un destino que ya no está adelante se ignora (punto 2);
  - por cada paso desde el actual hasta el anterior al destino: `RecordAttempt` resuelto con
    `Skipped` + `RestoreAfterForcedAdvance`;
  - Saltar paso = `targetOrder` actual + 1 (en el último paso, cierra la sesión).
- [ ] `RecordAttempt`: parámetro de resolución explícito (D-83) sin cambiar el camino del
      avance forzado.
- [ ] `BLL/Training/StepNames.cs` (D-85) y `Pages/Training/StepHints.cs` (D-87).
- [ ] `GuidedTest.aspx(.cs)`: barra de práctica (D-89) en el pie y en `mes-simbar`, con la
      pista del paso actual, la lista de Ir a… y los diálogos de confirmación ("¿Saltar
      {paso}? No se puede volver." / "¿Ir a {paso}? Se saltarán N pasos."). Handler del salto
      con D-84, incluido el Ingreso a MES (muestra el POD) y la limpieza (resumen).
- [ ] Tests: `StepNames` (las 3 guías: LC apertura/cierre, los 3 Log NC). Tests de `Skip`
      solo si el usuario los pide: `PodService` crea sus repositorios y no se prueba sin BD
      (roadmap → "Deuda").
- [ ] Verificación: rebuild, `aspnet_compiler` y tests. Checklist:
  - Ver pista antes y después de un fallo de Log NC (botón equivocado);
  - Saltar cada tipo de paso al menos una vez (Ingreso, Resource Setup, Start, Assembly a
    medias, DC a medias, Split, los 3 Log NC, Complete, Logout);
  - Ir a… Log NC · reparación rápida desde el Ingreso;
  - cancelar los diálogos;
  - doble clic en Saltar;
  - F5 después de saltar;
  - Serialized reconstruido (Session perdida) después de saltar.
- **Review:** `high` (registra intentos y avanza la sesión fuera del flujo normal).

## Entrega 4 — Resumen por paso y documentación (P-10, P-11; AC-12)

- [ ] `PodService.AttemptSummary(ctx)` (D-86): por paso, nombre (`StepNames`), cantidad de
      intentos y si fue saltado (punto 4).
- [ ] `ShowCompletionPanel`: con `Summary = AttemptsPerStep`, tabla de pasos (resalta más de
      1 intento y marca "Saltado"), sin puntaje ni Aprobado/Reprobado. Solo "volver al
      inicio".
- [ ] Verificación: rebuild, `aspnet_compiler` y checklist con una práctica completa con
      fallos y saltos, y otra cerrada saltando Logout.
- [ ] **Al cerrar la spec:**
  - `docs/domain.md`: "Modos de sesión" (Práctica libre), "Qué cuenta como intento" (aviso
    sin intento, `Skipped`) y "Avance forzado" (Saltar lo reusa);
  - `CLAUDE.md`: `ModePolicy` en "Capas", como `AccessPolicy`;
  - `docs/architecture.md`: `ModePolicy`, salto y barra de práctica;
  - `docs/glossary.md`: fila `SessionMode`;
  - roadmap: fila 8 de la Fase 8.5, "Estado actual" e "Historial".
- **Review:** `medium`.

## Base de datos

Sin scripts que correr: solo cambian los comentarios de `Mode` y `Reason` en `00_Schema.sql`.
No hay esquema nuevo ni SP nuevo o modificado.

## Puntos confirmados (usuario, 2026-10-20)

1. **Puntaje del header** (D-90): oculto en Práctica libre.
2. **Destino de Saltar / Ir a…:** se **ignora** un destino igual o anterior al paso actual
   (doble clic, F5); es **excepción** si no existe en la guía o el modo no permite saltar
   (enmienda 1 de la spec).
3. **Origen del resumen** (D-86): con los intentos del `PodContext`, sin método ni consulta
   nueva (enmienda 1 de la spec).
4. **Paso saltado en el resumen:** "Saltado" y, si hubo fallos antes, "Saltado (2 fallos)".
5. **Repositorio fuera de turno** (D-88): el aviso genérico "Ese no es el paso actual.",
   como dice P-4.
