# Spec 025: Fin de sesión del POD

**Status:** Approved 2026-11-02 (usuario) · **enmendada 2026-11-02** (enmienda 1, diseño de la E2:
D-340 a D-345) · plan `025-plan.md` (aprobado 2026-11-02, 6 entregas) · **Date:** 2026-11-02 · **Origen:** D-253 (`022-mockups-por-pantalla.md` →
"Candidatas para otra spec") · **Sesión:** Planeación 025 (segunda sesión de Arquitectura y
Planeación, local, a pedido de Planeación)
**Decisiones:** D-330 a D-338, del usuario; las propuestas de Claude (P-1 a P-16), confirmadas
por el usuario, se registran juntas como **D-339**. Rango de este frente: D-330 a D-349 y
B-50 a B-59.
**Orden:** se implementa después de cerrar la 022 (E8b Resumen y E9). El orden lo registra
Planeación en el roadmap.

## Problem

Al terminar una sesión, el operador ve el `PnlCompleted` del POD (`GuidedTest.aspx`): el
puntaje y APROBADO/REPROBADO en Assessment, solo el puntaje en Guided y la tabla de intentos
por paso en Práctica libre, con un único enlace, "Start New Session". No sabe **qué pasos le
costaron ni por qué**, no tiene una recomendación de qué hacer después y, como el resumen vive
en el request que cerró la sesión, **no lo puede volver a ver**: F5 lo lleva al Inicio.

El resumen tampoco usa el sistema de diseño ni el tema: está dentro del POD, que no cambia de
diseño (D-154).

## Scope

### In

**A. Página de fin de sesión** (D-253, D-330 a D-334, D-336)
1. Una página propia de Training con `Site.Master` (sistema de diseño y tema, como el Inicio):
   `Pages/Training/SessionResult.aspx?sessionId=N` (P-1). Solo para el rol que entrena
   (`CanTrain`) y solo con una sesión **propia y completada** (P-2).
2. **Primero el resultado** (D-330): guía ("Producto · Área"), modo, fecha y **duración total**
   (D-336), y según el modo (D-334):
   - **Assessment:** el veredicto, Aprobado o Reprobado, y el puntaje contra el mínimo de 70 %.
   - **Guided:** el puntaje, sin veredicto.
   - **Práctica libre:** sin puntaje (D-90 sigue): cuántos pasos acertó a la primera, de cuántos.
3. **Qué reforzar** (D-331): los pasos con algún intento fallido o sin acierto (no los saltados
   sin fallos, P-7), en el orden del flujo, cada uno con cuántos fallos tuvo y de qué tipo (motivo del intento:
   fuera de secuencia, decisión incorrecta, acción incorrecta…), **sin revelar la respuesta
   correcta**. En Assessment, también los puntos que perdió. Sin ninguno: un mensaje positivo
   (P-7).
4. **Detalle por paso, en todos los modos** (D-253, D-334): cada paso de la guía con sus
   intentos y su desenlace (a la primera, con N fallos, sin acierto por avance forzado,
   saltado). En Assessment y Guided, los puntos obtenidos de los posibles; en Práctica libre,
   sin puntos.
5. **Qué hacer después** (D-332, D-337): una **acción principal sugerida** según el resultado,
   por progresión de modos, y como secundarias **Repetir** (misma guía y modo), **Ir al Inicio**
   y **Cerrar sesión** (la del `Site.Master`):

   | Sesión terminada | Acción sugerida |
   |---|---|
   | Guided | Práctica libre de la misma guía |
   | Práctica libre sin fallos ni saltos | Assessment de la misma guía |
   | Práctica libre con fallos o saltos | Práctica libre de nuevo |
   | Assessment reprobado | Práctica libre de la misma guía |
   | Assessment aprobado | Ir al Inicio |

   Las acciones **no inician** la sesión: llevan al Inicio con el modo y la guía ya elegidos, y
   el operador presiona Iniciar (D-337, P-9).
6. Se puede volver a ver por su URL: con F5, Atrás o el enlace de la lista del Inicio (P-2).

**B. El POD redirige al cerrar** (D-253, D-333, D-338)
7. Cuando la sesión se **completa**, el POD lleva a la página: al cerrar la consola de la
   limpieza de Logout, o en el mismo request si el cierre llega por un salto o un avance forzado
   (P-10). El `PnlCompleted`, su tabla y "Start New Session" salen del POD.
8. **Abandonar no cambia** (D-333): Abandon, y abandonar la sesión en curso al iniciar otra
   (D-9), siguen yendo al Inicio, sin página de resultado.
9. El ícono Logout del POD con la sesión ya cerrada (D-21) deja de existir: tras el cierre ya no
   se ve el POD. Para salir de la app está "Cerrar sesión" del `Site.Master` (P-10).

**C. Últimas sesiones en el Inicio** (D-335)
10. En el Inicio del operador (`StartSession.aspx`), un bloque aparte **debajo** del de inicio,
    que no reordena "Modo primero" (precisa D-315): sus **últimas 5 sesiones completadas**, de la
    más reciente a la más vieja, con modo, fecha y el enlace a su fin de sesión (el área, solo si
    es otra que la actual, P-5). Sin
    resultado en la fila, sin "ver todas", sin tendencias ni métricas (P-5).
11. Componentes de `admin.css`, **sin tokens nuevos**; si el mockup necesita uno, es un desvío
    de alcance y se avisa a Planeación.

**D. Diseño**
12. El mockup de la página y del bloque del Inicio, en Claude Design con el prompt del handoff
    local (D-251), con las restricciones fijas de la 022 (claro y oscuro, AA, ningún estado solo
    por color, desde 768 px sin scroll horizontal de página).

### Out (no incluye)

- Historial completo de sesiones (filtros, abandonadas, "ver todas"), tendencias, curvas o
  métricas por guía para el operador: es un dashboard del operador, sin fase (candidata).
- Resultado en las filas de la lista del Inicio (D-335).
- Página de resultado para sesiones **abandonadas** (D-333).
- Acceso del administrador a esta página o un enlace desde el dashboard (el admin ya tiene el
  detalle del Operador; candidata).
- Pistas o respuestas correctas en el resultado (D-331): ni en Assessment ni en los otros
  modos.
- Cambios al diseño del POD (D-154), a cómo se califica, a los pesos, a `PassingScore` o a qué
  cuenta como intento.
- Iniciar una sesión desde la página de resultado (D-337).
- Excel o impresión del resultado.

## Decisiones del usuario (2026-11-02)

| ID | Decisión |
|---|---|
| D-330 | Lo primero que ve: **el resultado y el puntaje** (en Assessment, el veredicto contra el 70 %); debajo, qué reforzar y el detalle por paso |
| D-331 | **Qué reforzar:** los pasos con fallo, cuántos y de qué tipo (el motivo del intento), sin revelar la respuesta correcta; en Assessment, los puntos perdidos |
| D-332 | **Qué hacer después:** una acción principal según el resultado, por **progresión de modos** (tabla de "Scope" → A.5), con la regla en la BLL y sus tests; secundarias Repetir, Ir al Inicio y Cerrar sesión |
| D-333 | La página aplica **solo al completar** una sesión; abandonar sigue yendo al Inicio |
| D-334 | **Puntaje solo donde ya se ve:** Assessment y Guided con puntos por paso; Práctica libre con intentos, fallos y "Saltado", sin puntaje (D-90 sigue) |
| D-335 | **Lista mínima en el Inicio:** las **últimas 5** sesiones completadas propias, con guía, modo, fecha y el enlace, **sin resultado**; bloque aparte debajo de "Modo primero", sin tokens nuevos (precisa D-315; condiciones de Planeación, 2026-11-02) |
| D-336 | **Duración:** solo la total de la sesión |
| D-337 | La acción sugerida y Repetir llevan al **Inicio con el modo y la guía elegidos** (en la URL, validados por la BLL); no inician la sesión |
| D-338 | El POD **redirige al cerrar la consola** de la limpieza (o en el mismo request si el cierre es por salto o avance forzado); el `PnlCompleted` sale del POD y D-21 queda sin efecto |

## Propuestas de Claude, confirmadas por el usuario (2026-11-02)

El usuario las confirmó una por una el 2026-11-02; cambió P-7 (sin los saltados) y precisó P-5
(el área solo si es otra) y P-9 (cada valor por separado). Se
registran juntas como **D-339**.

| # | Propuesta |
|---|---|
| P-1 | **Página:** `Pages/Training/SessionResult.aspx?sessionId=N`, título "Resultado de la sesión", con `Site.Master`. `Page_Load` con la cadena de siempre: `EnsureUserSession` → `MustChangePassword` → `MustChooseArea`, y `CanTrain` (sin la capacidad, `RedirectHome`) |
| P-2 | **Invariante en la BLL:** un servicio de Training (p. ej. `SessionResultService.Get(userId, sessionId)`) devuelve el resultado solo si la sesión **es del usuario autenticado y está completada**; si no (otra persona, en curso, abandonada, inexistente o un `sessionId` que no es un número), la página va al Inicio sin mostrar nada ni decir por qué. El `sessionId` de la URL nunca se usa sin esa validación |
| P-3 | **Datos del detalle sin SP nuevo:** la sesión (`spTrainingSessionGetById`), sus intentos (`spStepAttemptGetBySession`) y la guía con sus pasos y pesos ya existen. El detalle por paso se calcula en la BLL con una **regla pura con tests**: por paso, intentos, fallos (`IsCorrect = 0`, sin contar el salto), motivos, desenlace y, si el modo muestra puntaje, puntos obtenidos (`Weight × PenaltyFactor` del intento que acertó, la misma fórmula de `ScoreCalculator`, sin copiarla) y posibles (`Weight`). Invariante con test: la suma de los puntos obtenidos es el `FinalScore` guardado |
| P-4 | **Lista del Inicio, un SP nuevo** `spTrainingSessionGetRecentCompleted(@UserId, @Top)` en `03_spTrainingSession.sql`: las sesiones completadas del usuario por `CompletedAt` descendente, con el título de la guía, el área, el producto, el modo y `CompletedAt`. No reusa `spDashboardSessions`: es del dashboard, filtra guías publicadas y `Role = 1` y no trae nombres. Incluye las sesiones de guías que ya no están publicadas (es su propio historial; una versión vieja de la guía, D-56, también) |
| P-5 | **Fila de la lista:** el modo, la fecha y hora de cierre y el enlace "Ver resultado". La guía se identifica por su área (hoy una guía por área, D-268) y, como el área ya está en la barra (D-316), **"Producto · Área" solo aparece si la sesión es de otra área** que la actual (entrenamiento cruzado) (usuario, 2026-11-02). **Sin sesiones completadas, el bloque no aparece** (el Inicio queda como hoy) |
| P-6 | **Motivos en español**, como presentación (como `MesErrorNotes`): fuera de secuencia, decisión incorrecta, acción incorrecta, sin selección, campo obligatorio vacío. Saltado no es un fallo, va aparte ("Saltado" o "Saltado (N fallos)", como hoy). Solo el tipo y el conteo, nunca el detalle de la decisión (qué valor puso o cuál era el correcto) |
| P-7 | **Qué reforzar** = pasos con al menos un intento fallido o que terminaron sin acierto (avance forzado); en el orden de la guía. **Un paso saltado sin fallos no entra** (saltar es explorar; se ve en el detalle); uno saltado con fallos entra por sus fallos (usuario, 2026-11-02). La regla reutiliza la de "Pasos" del dashboard (`StepRuns`: fallo, sin acierto) en vez de una segunda definición. Sin ninguno: "Ningún paso con fallos." En Guided, donde la guía no deja enviar lo incorrecto, lo esperado es que la lista salga vacía. Los saltos sí cuentan para la sugerencia de Práctica libre (A.5) |
| P-8 | **La sugerencia vive en `ModePolicy`**, como capacidad del modo (no `mode == …`): el modo sugerido si la sesión salió bien y si no. "Salió bien" = Assessment aprobado (`ScoreCalculator.IsPassed`); en Práctica libre, sin fallos ni saltos; Guided sugiere Práctica libre siempre. Assessment aprobado no sugiere modo: la acción es Ir al Inicio. Si la guía ya no se puede iniciar (no publicada o de otra área, D-173), la acción principal es Ir al Inicio y no hay Repetir |
| P-9 | **Inicio con todo elegido:** `StartSession.aspx?mode=<0-2>&guide=<Id>`. La BLL valida que la guía sea iniciable por el usuario (publicada y de su área); un valor inválido se ignora sin aviso (como un filtro inválido, P-4 de la 024). Cada valor se valida por separado: con un modo válido y una guía no iniciable, queda elegido solo el modo (usuario, 2026-11-02). Solo preselecciona: Iniciar sigue siendo el único camino que crea una sesión |
| P-10 | **Redirección del POD:** al cerrar la consola de la limpieza, el cliente navega a la URL del resultado (hoy esa misma acción muestra el panel, `CloseToSummary`); si el cierre es por un salto (Práctica libre) o un avance forzado de Logout, el servidor redirige en ese request. `GuidedTest.aspx` con una sesión **propia y completada** (F5 o una pestaña vieja) redirige al resultado; el resto sigue como hoy. Se quitan `PnlCompleted`, `ShowCompletionPanel`, `LnkNewSession` y la rama de D-21 en el ícono Logout |
| P-11 | **Guía de Guided:** la página de resultado no es parte del POD y no lleva guía visual ni anclas `data-guide`. El momento de la guía en la consola de la limpieza ("cierra la consola…") sigue; el que apuntaba al resumen del POD se ajusta en `GuideScriptBuilder` |
| P-12 | **Fechas** en la hora de la planta, como el dashboard; duración como la del detalle del Operador (`CompletedAt − StartedAt`) |
| P-13 | **Lo que hoy hace `StepAttemptSummary`** (la tabla de Práctica libre del POD, D-86) lo absorbe la regla de P-3; el código viejo se borra, no queda duplicado |
| P-14 | **Tests (NUnit, en memoria):** la regla del detalle en los tres modos (suma = `FinalScore`, avance forzado, saltado, saltado con fallos), qué reforzar (vacío y con fallos), la sugerencia de cada modo y resultado (incluida la guía no iniciable), el invariante de P-2 (ajena, en curso, abandonada, inexistente → nada) y la validación de la preselección de P-9. El SP de P-4 y las páginas se prueban en el navegador. `ScoreCalculatorTests` sigue en verde |
| P-15 | **El administrador no ve esta página** (no entrena, spec 013): sin la capacidad vuelve a su inicio, como en el resto de Training |
| P-16 | **Diseño por entregas:** primero la BLL y el SQL (sin UI), después el mockup en Claude Design (página y bloque del Inicio en un solo brief) y después las páginas; el brief lo escribe esta sesión en el plan |

## Enmienda 1 (2026-11-02, diseño de la E2)

El usuario aprobó en Claude Design el diseño "Fin de sesión v3" (ronda 3, "E con la tarjeta de
A"; proyecto "Fin de sesión del POD (spec 025)",
<https://claude.ai/design/p/e5a1ad16-3f88-4db5-8175-3f4e96a95b77?file=Fin+de+sesi%C3%B3n+v3.dc.html>;
las rondas 1 y 2, en "Fin de sesión v1" y "v2"). La sesión de diseño señaló cinco desvíos de
esta spec y el usuario los aceptó uno por uno:

| ID | Decisión |
|---|---|
| D-340 | **Una sola lista "Pasos"** con un selector "A reforzar (N) / Todos (M)", que abre en A reforzar; M es la cantidad de pasos de la guía. Reemplaza los dos bloques de A.3 y A.4: "Qué reforzar" es la vista A reforzar y el detalle por paso es la vista Todos. Las reglas de D-331, D-334 y P-7 no cambian. El selector se resuelve en el cliente, sin postback (como en la 022, "Comportamiento intacto"); sin JavaScript se ve A reforzar. Con 0 a reforzar, "Ningún paso con fallos." |
| D-341 | **Sin aviso** "te llevamos al Inicio…" debajo de los botones. D-337 sigue igual: las acciones no inician la sesión |
| D-342 | **Repetir no aparece** cuando coincide con la acción principal (Práctica libre con fallos o saltos). Precisa D-332 |
| D-343 | **Textos de "Qué hacer después", aprobados por el usuario tal como están** (no pasan por negocio). Acción principal y frase: Guided → "Practicar en Práctica libre" / "Ahora sin la guía visual, para afianzar lo aprendido."; Práctica libre sin fallos ni saltos → "Hacer el Assessment" / "Sin fallos ni saltos: ya puedes evaluarte."; Práctica libre con fallos o saltos → "Practicar de nuevo en Práctica libre" / "Aún hay pasos por afianzar."; Assessment aprobado → "Ir al Inicio" / "Aprobaste este Assessment."; Assessment reprobado → "Practicar en Práctica libre" / "Repasa los pasos marcados sin la presión del puntaje."; guía no iniciable → "Ir al Inicio" / "Esta guía ya no se puede iniciar.". Secundarias: "Repetir <modo>" e "Ir al Inicio" |
| D-344 | En el Inicio, **"Últimas sesiones" aparece plegado**: un `<details class="steps-card steps-ranking">` con "Mostrar ▾", como el Ranking completo de Pasos, y filas `.last-*`. Sin sesiones completadas, no aparece. Precisa D-335 y D-315 |
| D-345 | **Diseño aprobado** (v3): título "Resultado de la sesión" con "Producto · Área" debajo; tarjeta de resultado con el veredicto, la cifra y la barra contra el 70 % a la izquierda, y Guía, Modo, Fecha y Duración a la derecha; la lista "Pasos" (D-340); "Qué hacer después" al final. Componentes nuevos en `admin.css`, bloque "Resultado de la sesión": `.res`, `.verdict`, `.bar`, `.meta`, `.mot`, `.oc`, `.seg`, `.pl`, `.es-next` y `.last-*`. **Sin tokens nuevos**, masters sin cambio y JavaScript solo para el selector |

**Notas para Desarrollo** (sin decisión nueva):
- El mockup usa pesos y pasos de ejemplo. Los reales salen de la BD (`docs/domain.md` →
  "Ponderación"), y "Todos (M)" es la cantidad de pasos de la guía, no 13 fijo.
- "Producto · Área" va con el texto estándar de la 021 (`AreaLabel`, como la barra). En el mockup,
  el título y la barra lo muestran en distinto orden.
- La verificación del diseño fue parcial: hubo capturas, pero no se midió el contraste ni se
  probaron los clics en un navegador real. AC-9 se verifica en la E3 y la E5.
- **Orden:** la 022 ya cerró (E9 en `e1f6665`), así que la 025 puede arrancar. Ya no hay riesgo
  de cruce en `StartSession.aspx` ni en `admin.css`.

## Acceptance criteria

- **AC-1** Given un Assessment completado, When se cierra la consola de la limpieza, Then el
  operador llega a la página de resultado con el veredicto, el puntaje contra el 70 %, la
  duración, qué reforzar con sus motivos y los puntos perdidos, y el detalle por paso.
- **AC-2** Given una Práctica libre completada, Then la página no muestra puntaje (ni total ni
  por paso) y sí intentos, fallos y "Saltado". Given una Guided, Then muestra el puntaje sin
  veredicto.
- **AC-3** Given cualquier sesión completada, Then la suma de los puntos por paso es el
  `FinalScore` guardado (test), y ningún texto revela la respuesta correcta de un paso.
- **AC-4** Given cada fila de la tabla de A.5, Then la acción sugerida es la de la tabla y lleva
  al Inicio con el modo y la guía elegidos, sin crear una sesión. Given una guía que ya no se
  puede iniciar, Then la acción es Ir al Inicio.
- **AC-5** Given la URL del resultado con una sesión ajena, en curso, abandonada o inexistente,
  Then la página va al Inicio sin mostrar nada. Given un administrador, Then vuelve a su inicio.
- **AC-6** Given el resultado, When F5 o Atrás, Then se vuelve a ver. Given `GuidedTest.aspx`
  de una sesión propia completada, Then lleva al resultado.
- **AC-7** Given Abandon, Then el operador vuelve al Inicio como hoy, sin página de resultado.
- **AC-8** Given un operador con sesiones completadas, Then el Inicio muestra sus últimas 5 (de
  la más reciente), debajo del bloque de inicio sin cambiarlo, cada una con su enlace. Sin
  sesiones completadas, el bloque no aparece.
- **AC-9** Página y bloque en claro y en oscuro, contraste AA, ningún estado solo por color,
  sin scroll horizontal de página desde 768 px, sin tokens nuevos.
- **AC-10** El POD no cambia de diseño; los tests de `ScoreCalculator` siguen en verde y nada
  de lo que cuenta como intento cambia.

## Riesgos y coordinación

- **Inicio de la 022 (E8, D-315 a D-318):** el bloque nuevo es una enmienda de un diseño recién
  aprobado (D-335 precisa D-315). Se implementa después de la E9, así que nadie más toca
  `StartSession.aspx` ni sus estilos a la vez.
- **`admin.css`:** los estilos nuevos van en un bloque propio "Resultado de la sesión", sin
  tokens nuevos; `Site.Master` no se toca.
- **`GuidedTest.aspx(.cs)`:** quitar el panel y redirigir toca la limpieza de Logout (spec 016,
  P-12) y el cierre por salto (spec 012); se prueba con los tres modos y las dos formas de
  cierre.
- **`docs/domain.md` y `docs/architecture.md`:** al cerrar, Desarrollo actualiza "Práctica
  libre" (el resumen ya no está en el POD), "Nunca cuentan" (D-21) y el mecanismo del resumen
  (D-86).

## Candidatas para otra spec

- **Historial y métricas del operador (2026-11-02):** todas sus sesiones (con filtros y
  abandonadas), tendencias y curvas por guía en el Inicio. Es un dashboard del operador, sin
  fase.
- **Resultado visible para el administrador (2026-11-02):** abrir esta página, o un enlace a
  ella, desde las sesiones del Operador en el dashboard.

## Open Questions

_Ninguna._
