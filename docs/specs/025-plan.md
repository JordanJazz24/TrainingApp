# Plan 025: Fin de sesión del POD

**Spec:** `025-fin-de-sesion.md` (aprobada 2026-11-02, `aa85d33`) · **Plan aprobado:** 2026-11-02
(usuario) · **Decisiones:** D-330 a D-339 y, por la enmienda 1 de la spec (diseño), D-340 a D-345 ·
**Sesión:** Desarrollo (local), con el ciclo de `docs/process.md` → 2. Arranca **después de cerrar
la 022** (E8b Resumen y E9); el orden lo registra Planeación en el roadmap. *2026-11-02: la 022
cerró (E9, `e1f6665`): la 025 ya puede empezar.*

## Cómo se trabaja este plan

- Una entrega por vez, en este orden: E1 deja la BLL y el SQL con sus tests, sin UI. E2 es el
  diseño, hecho por el usuario en Claude Design. E3 a E5 implementan sobre el handoff aprobado.
  E6 cierra. La página (E3) va antes de la redirección (E4): el POD conserva su resumen hasta que
  la página nueva funciona (usuario, 2026-11-02).
- Al empezar cada entrega, Desarrollo le pide al usuario el `/effort` que indica (D-246).
- Sin ORM ni SQL inline; la regla en la BLL; el code-behind solo UI. `ScoreCalculator`, los pesos,
  `PassingScore` y lo que cuenta como intento **no cambian** (AC-10).
- `Site.Master`, `Training.Master` y los tokens no se tocan. Los estilos nuevos van en `admin.css`,
  en un bloque propio "Resultado de la sesión (spec 025)".
- No se tocan `docs/roadmap.md` (Planeación). Desarrollo escribe `docs/status.md` y
  `docs/history.md` en sus commits, como siempre.
- Si algo no cierra, parar y avisar a "Planeación 025" (o a Planeación, si esa sesión ya cerró)
  para la enmienda (`process.md` → 1). Decisiones nuevas desde **D-340** (hasta D-349); bugs,
  B-50 a B-59.

## E1 · Datos y BLL, con tests — Esfuerzo: `high`

- [x] **SQL** `03_spTrainingSession.sql`: `spTrainingSessionGetRecentCompleted(@UserId INT, @Top INT)`
      (P-4) devuelve las sesiones `Status = 1` del usuario por `CompletedAt` descendente, `TOP (@Top)`,
      con `SessionId`, `TrainingGuideId`, `Mode`, `CompletedAt`, el `AreaId`, `AreaName` y
      `ProductName` de la guía. **No** filtra `IsPublished` (es su historial, P-4). Re-ejecutable
      (`CREATE OR ALTER`). Si no hay índice útil por `(UserId, Status, CompletedAt)`, Desarrollo lo
      mide con la BD de volumen y lo propone solo si hace falta.
- [x] **Domain:** un POCO para la fila de la lista (p. ej. `CompletedSessionItem`), sin lógica.
      DAL: `TrainingSessionRepository.GetRecentCompleted(userId, top)`.
- [x] **Regla pura del detalle** `BLL/Training/SessionResultRule.cs` (P-3, P-6, P-7, P-13). Con la
      sesión, los pasos de la guía con su `Weight` y los intentos, da:
      - un renglón por **cada paso de la guía**, en orden: nombre (`StepNames`), intentos, fallos
        (misma definición que `StepRuns.IsFailure`: incorrecto y no saltado; no se copia la
        regla, se reutiliza o se extrae a un lugar común), motivos de los fallos con su conteo,
        desenlace (`AtFirst`, `WithFailures`, `WithoutSuccess`, `Skipped`, `SkippedWithFailures`) y,
        solo si `Policy.ShowsScore`, puntos obtenidos y posibles;
      - **puntos obtenidos** = `Weight × Policy.PenaltyFactor(intento del primer acierto)`, 0 sin
        acierto, con el mismo cálculo que `ScoreCalculator` (se reutiliza, no se duplica);
      - **qué reforzar** = los renglones con fallos o sin acierto; un saltado sin fallos **no**
        entra (P-7). En Assessment, con los puntos perdidos (`Weight − obtenidos`);
      - el **resumen**: veredicto solo si `Policy.Summary == ScoreAndResult`
        (`ScoreCalculator.IsPassed`), puntaje solo si `ShowsScore`; en Práctica libre, "N de M a la
        primera"; duración total (`CompletedAt − StartedAt`, P-12).
      Reemplaza a `StepAttemptSummary` (se borra en la E4, al quitar su único uso).
- [x] **Sugerencia en `ModePolicy`** (D-332, P-8): por modo, el modo sugerido si salió bien y si
      no (Guided → Práctica libre en los dos casos; Práctica libre → Assessment / Práctica libre;
      Assessment → ninguno, Ir al Inicio / Práctica libre). "Salió bien" lo calcula la regla de
      arriba (Assessment aprobado; Práctica libre sin fallos **ni saltos**). Sin `mode == …` fuera
      de `ModePolicy`.
- [x] **Servicio** `BLL/Training/SessionResultService.cs` (P-2): `Get(userId, sessionId)` devuelve el
      resultado o `null` si la sesión no existe, no es del usuario o no está `Completed`. Usa
      `spTrainingSessionGetById`, `spStepAttemptGetBySession` y la guía con sus pasos (el
      repositorio de guías ya puebla `Steps`). Verifica `AccessPolicy.CanTrain`. Calcula si la guía
      **sigue siendo iniciable** con `TrainingAccessRule` (publicada y del área actual): si no, sin
      acción sugerida de modo ni Repetir (P-8). Las acciones salen como URL del Inicio con
      `?mode=&guide=` (D-337): la principal y las secundarias (Repetir y Ir al Inicio), **sin
      Repetir cuando coincide con la principal** (D-342), con un test. `GetRecent(userId)` = las últimas **5** (`const`), cada una con
      "Producto · Área" solo si el área no es la actual del usuario (P-5).
- [x] **Preselección del Inicio** (P-9): en `OperatorStartOptions` (o su servicio), una función que
      recibe el `mode` y el `guide` de la URL y devuelve los que valen, **cada uno por separado**: el
      modo si es un `SessionMode` definido (B-4); la guía si está en sus guías iniciables. Un valor
      inválido o ausente se ignora, sin aviso.
- [x] **Tests NUnit en memoria** (P-14): la regla del detalle en los tres modos (**suma de los
      puntos = `FinalScore`** exacto, con aciertos al 1.º, 2.º y 3.º intento, avance forzado al 4.º,
      saltado y saltado con fallos); qué reforzar (vacío en Guided limpio, con fallos, sin los
      saltados limpios); la sugerencia de cada modo y resultado, y con la guía no iniciable; el
      servicio con una sesión ajena, en curso, abandonada o inexistente (`null`) y un admin
      (sin `CanTrain`); la preselección (modo inválido, guía de otra área, guía no publicada, modo
      válido con guía inválida = solo el modo). `ScoreCalculatorTests` y `StepRunsTests` en verde.
- [x] `/revisar medium` sobre `SessionResultRule.cs`, `SessionResultService.cs`, `ModePolicy.cs` y la
      preselección. Foco: el dueño y el estado (invariante de P-2), que el puntaje por paso no
      duplique la fórmula, que la suma dé el `FinalScore` y que nada revele la respuesta correcta.

**Verifica Desarrollo:** Rebuild; script `03` sobre su BD; tests en verde. Sin cambios visibles.

> **Cerrada 2026-11-02 (`fc5630f`).** SP en `03` (probado en LocalDB y en la BD del usuario), `SessionResultRule`, `ModePolicy.SuggestedNextMode`, `SessionResultService`, `OperatorStartOptions.Preselect` y `TrainingAccessRule.IsStartable`; 55 tests nuevos (516 en verde). La suma de los puntos por paso da el `FinalScore` en los tres modos (los pesos reales son múltiplos de 0,05, exactos a 2 decimales). `/revisar medium`: 1 de 5 aplicado (las condiciones de inicio, en un solo lugar), 4 descartados (redondeo de `FinalScore` con pesos que no existen, paso sin intentos, dos consultas de `AreaText` y defensas baratas). Decisiones menores: un administrador provoca `UnauthorizedAccessException` en el servicio (no `null`); `FailureReasonCount` (el nombre `ReasonCount` ya existe en `BLL.Reports`). **Rondas de corrección tras la primera prueba: 0** (piloto de Sonnet, D-250; sesión de otro proveedor).

## E2 · Mockup en Claude Design — hecha (2026-11-02), antes de la E1

> **Hecha 2026-11-02:** el usuario aprobó "Fin de sesión v3" (spec → "Enmienda 1", D-340 a
> D-345), antes de que se implementara la E1. El orden de implementación no cambia: E1 → E3 → E4
> → E5 → E6. La fuente del diseño es el archivo de Claude Design. El `.md` del handoff
> (`docs/specs/025-design/fin-de-sesion.md`, con el formato de `022-design/operador.md`) lo
> guarda Desarrollo al empezar la E3, desde ese diseño, con D-340 a D-345 ya numeradas.

El usuario pega en Claude Design el "Prompt para Claude Design" de `022-briefs.md` (D-251),
con PANTALLA = "FIN DE SESIÓN y bloque del INICIO", brief = el de abajo, `<archivo>` =
`fin-de-sesion` (en `docs/specs/025-design/`, no en `022-design/`) y las rutas `Pages/Training/SessionResult.aspx` (nueva) y
`Pages/Training/StartSession.aspx`. Archivos que lee además: `Pages/Training/GuidedTest.aspx`
(el `PnlCompleted` actual), `docs/specs/025-fin-de-sesion.md` y
`docs/specs/022-design/inicio-operador.md`.

```text
Pantalla: FIN DE SESIÓN (página nueva, Site.Master) y un bloque nuevo en el INICIO DEL OPERADOR.
Quién: operadores de planta, a veces en tablet, justo al terminar una sesión del simulador.
Qué tiene que entender: su resultado, qué reforzar y qué hacer después (spec 025).
Qué ve primero: el resultado.

Fin de sesión, datos (ver spec 025 → Scope A):
- encabezado: guía ("Producto · Área"), modo (Guided, Práctica libre o Assessment), fecha y
  duración total;
- resultado según el modo: Assessment → Aprobado o Reprobado y el puntaje contra el mínimo de
  70 %; Guided → el puntaje, sin veredicto; Práctica libre → SIN puntaje, "N de M pasos a la
  primera";
- "Qué reforzar": los pasos con fallos o sin acierto, cada uno con cuántos fallos y de qué tipo
  (Fuera de secuencia, Decisión incorrecta, Acción incorrecta, Sin selección, Campo obligatorio
  vacío) y, en Assessment, los puntos perdidos. NUNCA la respuesta correcta. Sin ninguno:
  "Ningún paso con fallos.";
- detalle de todos los pasos (13 aprox.): intentos y desenlace (A la primera, Con N fallos, Sin
  acierto (avance forzado), Saltado, Saltado (N fallos)); en Assessment y Guided, puntos
  obtenidos / posibles; en Práctica libre, sin puntos;
- acciones: una principal sugerida (ej. "Practicar en Práctica libre", "Hacer el Assessment",
  "Ir al Inicio") y secundarias Repetir (misma guía y modo) e Ir al Inicio. Cerrar sesión ya está
  en la barra. Si la guía ya no se puede iniciar, solo Ir al Inicio.
Muestra los 4 casos: Assessment aprobado, Assessment reprobado con 3 pasos a reforzar, Guided
limpio, Práctica libre con fallos y 2 saltos.

Inicio del operador (ya aprobado, alternativa D "Modo primero", inicio-operador.md):
- NO se reordena ni cambia lo aprobado. Se agrega UN bloque DEBAJO: "Últimas sesiones", sus
  últimas 5 completadas, de la más reciente: modo, fecha y hora, y el enlace "Ver resultado";
  "Producto · Área" solo en la fila de una sesión de OTRA área. Sin resultado ni puntaje en la
  fila. Sin sesiones completadas, el bloque no aparece (muéstralo con y sin).

Restricciones extra: sin tokens nuevos (si hace falta uno, es desvío de alcance); sin guía
visual ni anclas data-guide en la página nueva; Iniciar sigue siendo el único botón que crea
una sesión (las acciones solo llevan al Inicio con modo y guía elegidos).
Queda fuera: historial completo, filtros, tendencias o métricas del operador, resultado de
sesiones abandonadas, vista del administrador, Excel o impresión, cambios al POD.
```

- [x] El usuario aprueba una alternativa y copia el handoff a Desarrollo, que guarda
      `docs/specs/025-design/fin-de-sesion.md` tal cual (carpeta propia de la 025; el formato es
      el de `022-design/operador.md`; usuario, 2026-11-02). Ante un desvío de alcance del handoff, Desarrollo se detiene y avisa
      (`process.md` → "Handoff local").
- [x] Las D-n del diseño, numeradas por Planeación 025 antes de implementar: D-340 a D-345
      (desde D-346 si la E3 encuentra otra cosa).

## E3 · Página de resultado — Esfuerzo: `medium`

- [x] `Pages/Training/SessionResult.aspx(.cs)` con `Site.Master`, según el handoff (P-1): título
      "Resultado de la sesión"; `Page_Load` con `EnsureUserSession` → `MustChangePassword` →
      `MustChooseArea` → `CanTrain` (sin la capacidad, `RedirectHome`, P-15); `sessionId` no
      numérico o `Get` = `null` → al Inicio, sin aviso (P-2). Solo binding: textos de motivos y
      desenlaces en una clase de presentación (como `MesErrorNotes`, P-6).
- [x] `Web.config`: la página queda bajo la protección de `Pages/Training` (revisar que la regla
      la cubra; no hace falta otra).
- [x] Según el diseño v3 (D-340 a D-345): la tarjeta de resultado; **una lista "Pasos"** con el
      selector A reforzar / Todos (M), resuelto en el cliente y sin postback, que abre en A reforzar
      (las dos vistas se renderizan desde el servidor y el JS solo las alterna; sin JS se ve A
      reforzar); "Qué hacer después" con los textos de D-343, sin aviso debajo (D-341) y sin Repetir
      cuando coincide con la principal (D-342; esa regla va en la BLL de la E1, no en la página).
- [x] `docs/specs/025-design/fin-de-sesion.md` desde el diseño de Claude Design (ver E2).
- [x] `admin.css`: bloque "Resultado de la sesión (spec 025)", sin tokens nuevos: `.res`,
      `.verdict`, `.bar`, `.meta`, `.mot`, `.oc`, `.seg`, `.pl`, `.es-next` (D-345).
- [x] Sin `/revisar` si es solo binding y markup (`process.md` → 3); `low --max-findings 5` si el
      code-behind toma decisiones.

**Verifica Desarrollo:** Rebuild; `aspnet_compiler`. Para probarla antes de la E4, abrir la URL a
mano con un `sessionId` completado propio. Checklist: AC-1 a AC-3 y AC-5 (ajena, en curso,
abandonada, inexistente, `abc`, admin), AC-9 en claro, oscuro y 768 px.

> **Cerrada 2026-11-02 (03419d9).** `SessionResult.aspx(.cs)` según el diseño v3 (D-340 a D-345), `SessionResultText` (motivos, desenlaces y los textos de D-343), bloque «Resultado de la sesión» en `admin.css` sin tokens nuevos y `docs/specs/025-design/fin-de-sesion.md` escrito desde el v3 antes de implementar (el proyecto de Claude Design solo trae el prototipo). `Web.config` ya cubría `Pages/Training`. `SessionResult.UserAreaText` en el servicio para la barra. Rebuild, `aspnet_compiler` (sobre una copia sin `obj` ni `Properties/publish`, que tienen restos de una publicación del 22 de oct.) y 516 tests en verde. Sin `/revisar` (solo binding, markup y textos). Probada por el usuario con sesiones reales (Assessment aprobado y reprobado, Guided, Práctica libre y las no válidas). **Rondas de corrección tras la primera prueba: 0** (piloto de Sonnet, D-250; sesión de otro proveedor). Decisión menor: la meta «Guía» muestra el título de la guía; «Producto · Área» va bajo el título de la página.

## E4 · El POD redirige — Esfuerzo: `high` (borra código de la BLL y toca el cierre del motor)

- [x] `GuidedTest.aspx.cs` (P-10, D-338): cuando la sesión se completa, sin panel:
      - limpieza de Logout: al cerrar la consola, el cliente navega a
        `SessionResult.aspx?sessionId=N` (hoy esa acción muestra el resumen, `CloseToSummary`);
      - salto que cierra la sesión (`BtnSkip_ServerClick`) y avance forzado de Logout: redirect en el
        mismo request;
      - la sesión ya completada (F5, pestaña vieja): si `LoadContext` no la devuelve y la sesión es
        **propia y `Completed`** (`SessionResultService.Get` ≠ `null`), redirige al resultado; el
        resto, como hoy.
- [x] Se borran `PnlCompleted` y su markup, `ShowCompletionPanel`, `LnkNewSession`, `RptAttemptSummary`,
      `PodService.AttemptSummary`, `StepAttemptSummary` (y `StepAttemptSummaryRow`) y la rama del
      ícono Logout con la sesión cerrada (D-21). Los tests de `StepAttemptSummary` pasan a la regla de
      la E1 si cubren algo que ella no cubre; si no, se borran.
- [x] Abandon y abandonar al iniciar otra guía, sin cambios (D-333, AC-7).
- [x] Guía de Guided (P-11): ajustar en `GuideScriptBuilder` el globo de `SessionCompleted` que
      apuntaba al resumen del POD; el de la consola de la limpieza sigue. Anclas `data-guide` del
      panel que se borra, fuera.
- [x] `/revisar low --max-findings 5` sobre `GuidedTest.aspx.cs` y `GuideScriptBuilder.cs`. Foco: los
      tres caminos de cierre llegan al resultado, Abandon no cambia, nada del POD lee un panel que ya
      no existe, el redirect solo con una sesión propia y completada.

**Verifica Desarrollo:** Rebuild; `aspnet_compiler`; tests. Checklist: AC-1 (Assessment completo
con la limpieza), AC-2 (Práctica libre cerrada con Saltar Logout; Guided completo), AC-6 (F5 en el
resultado y en el POD de una sesión cerrada), AC-7 (Abandon), AC-10 (el POD igual, ningún globo
de la guía colgado).

> **Cerrada 2026-11-02 (87c8bb2).** Los tres cierres llevan al resultado: el último paso o el avance forzado y el salto redirigen en el mismo request (`CompleteSession`); la limpieza de Logout cerrada por el script deja la consola con su salida y, al cerrarla, el cliente navega (`MesLoginView.CloseUrl`, antes `CloseToSummary`); `LoadContext` lleva al resultado una sesión propia y completada (`SessionResultService.Get`, F5 o pestaña vieja). Se borraron `PnlCompleted` y su markup, `ShowCompletionPanel`, `LnkNewSession`, `RptAttemptSummary`, `PodService.AttemptSummary`, `StepAttemptSummary` (con su test, cubierto por `SessionResultRuleTests`), el CSS `.pod-completion*` y el modo `final` de la guía (C#, `pod-guide.js` y el ancla `pod-final`); la guía de Guided conserva solo el paso de cerrar la consola (P-11). Abandon sin cambios. Rebuild, `aspnet_compiler` y 515 tests en verde. `/revisar low`: 1 de 2 aplicado (la URL del resultado en un solo helper); descartado: Logout en una pestaña vieja ya cerrada lleva al resultado (D-338 deja sin efecto D-21). **Rondas de corrección tras la primera prueba: 0** (piloto de Sonnet, D-250; sesión de otro proveedor).

## E5 · Últimas sesiones y preselección en el Inicio — Esfuerzo: `medium`

- [x] `StartSession.aspx(.cs)`, según el diseño (D-335, D-344): el bloque "Últimas sesiones"
      debajo del bloque de inicio, **plegado** (`<details class="steps-card steps-ranking">` con
      "Mostrar ▾", sin JS) y con filas `.last-*`, sin reordenar "Modo primero" (D-315); oculto sin
      sesiones (P-5).
- [x] Preselección (P-9): `?mode=&guide=` se pasan a la función de la E1 y se aplican a las tarjetas
      de modo y a `DdlGuide` solo en la primera carga (no en postback). Iniciar, sin cambios.
- [x] `admin.css`: en el mismo bloque de la spec 025; `.start-*` de la 022, sin tocar.
- [x] Sin `/revisar` si es binding y markup; `low --max-findings 5` si la página decide algo.

**Verifica Desarrollo:** Rebuild; `aspnet_compiler`. Checklist: AC-4 (cada acción sugerida de la
tabla A.5 llega al Inicio con modo y guía elegidos y no crea una sesión; guía no iniciable = solo
Ir al Inicio; `?mode=9&guide=abc` se ignora; modo válido con guía de otra área = solo el modo),
AC-8 (con 0, con 3 y con más de 5 sesiones; una de otra área con su "Producto · Área"), AC-9.

> **Cerrada 2026-11-02 (e322e4e).** `StartSession.aspx(.cs)`: el bloque «Últimas sesiones» (`<details class="steps-card steps-ranking">` plegado, filas `.last-*`) debajo del bloque de inicio, que no se reordena; oculto sin sesiones completadas; cada fila con el modo, la fecha y hora de la planta y «Ver resultado» (`SessionResultService.GetRecent`, `SessionResultText.Recent`). La preselección de `?mode=&guide=` pasa por `OperatorStartOptions.Preselect` y se aplica a las tarjetas de modo y a `DdlGuide` solo en la primera carga. Estilos `.last-*` en el bloque de la spec 025 de `admin.css`, sin tokens nuevos. Rebuild, `aspnet_compiler` y 515 tests en verde. Sin `/revisar` (binding y markup; la preselección ya está probada desde la E1). **Rondas de corrección tras la primera prueba: 0** (piloto de Sonnet, D-250; sesión de otro proveedor). Sin datos reales en la prueba para «más de 5 sesiones» ni «una de otra área»: los cubren los tests de `GetRecent`.

## E6 · Cierre — Esfuerzo: `medium`

- [x] `docs/domain.md` → "Modos de sesión" (el resumen ya no está en el POD: va a la página de
      resultado, con sus reglas de qué reforzar y la sugerencia) y → "Qué cuenta como intento" →
      "Nunca cuentan" (sale la frase de D-21).
- [x] `docs/architecture.md`: el fin de sesión (la página, `SessionResultService`, el redirect del
      POD y la URL del Inicio con preselección); quitar lo de `ShowCompletionPanel` y D-86.
- [x] `docs/decisions.md`: D-330 a D-339 pasan a "vigente"; D-21 y D-86, a "sin efecto (spec 025)";
      las D-n del diseño (E2), con su fila.
- [x] Spec: "Status" a Implemented con los commits; casillas de este plan.
- [x] `/revisar low` de todo el diff de la spec, foco en consistencia con la spec y AC-10.
- [x] Aviso a Planeación: spec cerrada (para el roadmap y sus candidatas).

**Verifica Desarrollo:** que los docs coincidan con lo implementado; `docs/status.md` y
`docs/history.md` en el commit.

> **Cerrada 2026-11-02 (e7d3b58).** `domain.md` ("Fin de sesión", y sin la frase de D-21), `architecture.md` (nueva sección «Fin de sesión», el árbol y el Logout simulado; sin `ShowCompletionPanel` ni D-86), `decisions.md` (D-330 a D-345 «vigente»; D-21 y D-86 «sin efecto») y `ui-reference/general-flow.md`; la spec pasa a Implemented. `/revisar low` de la spec: 2 de 5 aplicados (`SessionResultService.ResultUrl` como único lugar de la URL, `OwnCompleted` que tolera un rol cambiado); descartados: la fecha (corregida a 2026-11-02), «Cerrar sesión» en D-332 (es la de la barra), el redondeo del puntaje (`DECIMAL(5,2)`) y el costo de `Get` en la ruta rara de F5. Pendiente de Planeación: el roadmap y las candidatas.
