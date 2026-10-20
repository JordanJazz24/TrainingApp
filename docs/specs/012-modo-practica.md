# Spec 012: Modo práctica (T3)

**Status:** Draft
**Date:** 2026-10-20 · **Approved:** —

Origen: revisión con Ernesto (2026-10-09, roadmap → Fase 8 → "Revisión con Ernesto", T3) y
Fase 8.5, fila 8. Decisiones del usuario del 2026-10-20 (P-3 a P-6).

## Problem

Hoy hay dos modos y ninguno sirve para ensayar la evaluación:

- **Guided** lleva de la mano: la guía visual señala cada clic y no deja enviar la decisión
  incorrecta, y solo está habilitado el botón del paso. El operador aprende el recorrido, pero
  no practica decidir solo.
- **Assessment** evalúa: sin ayuda, con todos los botones habilitados, penaliza los
  reintentos, fuerza el avance al 4.º fallo y deja un resultado Aprobado o Reprobado en el
  historial.

Falta el paso intermedio que pidió Ernesto: **la pantalla de Assessment, sin evaluar y con
ayuda cuando el operador la pide**, para equivocarse sin consecuencias antes de la
evaluación.

Además, lo que cambia con cada modo está repartido en `mode == …` por la página, la BLL y
`ScoreCalculator`. Un tercer modo multiplicaría esas ramas, así que antes se concentran en un
solo objeto (`ModePolicy`).

## Decisiones

- **P-1 · Tercer modo `Practice`** (`TrainingSession.Mode` = 2). Se elige en `StartSession`
  junto a los otros dos. Solo lo usa Operator, como los otros modos.
- **P-2 · Se ve y se opera como Assessment:** sin guía visual, todos los botones del POD
  habilitados y el panel del hallazgo de Log NC visible de inmediato. NC Selection no abre en
  la página del código del hallazgo.
- **P-3 · Secuencia como Assessment** (usuario): un botón fuera de secuencia es intento
  fallido del paso actual (regla S2). Lo mismo con el ícono Logout antes del último paso y
  con el Repositorio fuera de su turno en el Ingreso a MES o en la limpieza. Guided los sigue
  ignorando.
- **P-4 · Pista a demanda** (usuario): tras un intento fallido, el mensaje dice solo
  "Respuesta incorrecta." y aparece el botón **Ver pista**. Al presionarlo se muestra la
  pista del paso, la misma que Guided muestra hoy: el texto propio del control, si lo tiene
  (ej. el botón equivocado de Log NC), o si no la pista del tipo de paso. No muestra
  intentos restantes. La pista llega con la respuesta del servidor; abrirla es solo
  presentación, no es un postback ni se registra.
- **P-5 · Reintentos ilimitados** (usuario): sin penalización ni avance forzado. El
  operador sigue en el paso hasta acertar. Para salir a mitad queda Abandon.
- **P-6 · Resumen con los intentos por paso** (usuario): sin puntaje ni Aprobado/Reprobado.
  Muestra el área, el producto, el modo y una lista de los pasos de la guía con los intentos
  de cada uno (1 = acertó a la primera), resaltando los que tuvieron más de uno.
- **P-7 · Medición:** los intentos se registran como en los otros modos y quedan separados
  por el modo de la sesión. No hace falta una columna nueva. El dashboard (Fase 10) puede
  contarlos aparte de Assessment. `FinalScore` se calcula con la regla de Guided (sin
  penalización) y no se muestra.
- **P-8 · `ModePolicy`** (Entrega 1 del plan, sin cambio de comportamiento y con tests): un
  objeto de la BLL, creado a partir del `SessionMode`, que responde las capacidades de la
  tabla de abajo. La página, `PodService` y `ScoreCalculator` le preguntan a él, nunca
  `mode == …`, como `AccessPolicy` con los roles. Un modo nuevo es una fila más en
  `ModePolicy`, no ramas nuevas.

| Capacidad (`ModePolicy`) | Guided | Assessment | Practice |
|---|---|---|---|
| Guía visual (y lo que solo ella recibe: código del hallazgo en NC Selection, panel del hallazgo revelado por la guía) | sí | no | no |
| Todos los botones habilitados; fuera de secuencia = intento fallido | no (se ignora) | sí | sí |
| Penaliza reintentos | no | sí | no |
| Avance forzado al 4.º fallo | no | sí | no |
| Mensaje al fallar | pista del paso | intentos con puntaje restantes | "Respuesta incorrecta." + Ver pista |
| Resumen | puntaje (como hoy) | puntaje y Aprobado/Reprobado | intentos por paso |

## Scope

### In

1. **`ModePolicy`** (P-8): reemplaza las ramas por modo de `GuidedTest.aspx.cs`,
   `PodService` y `ScoreCalculator`. Guided y Assessment quedan exactamente igual (Entrega 1).
2. **Modo `Practice`** (P-1): enum, opción en `StartSession` y etiqueta del modo en el
   header del POD y en el resumen.
3. **Comportamiento** según las columnas de la tabla (P-2 a P-5).
4. **Ver pista** (P-4) en los mensajes de intento fallido del POD y en las pantallas que se
   califican en su propio postback (Ingreso a MES, limpieza, Split de Moldeo).
5. **Resumen de Practice** (P-6), con los intentos de `spStepAttemptGetBySession`.
6. Documentación: `docs/domain.md` → "Modos de sesión" gana Practice, y `CLAUDE.md` y
   `docs/architecture.md`, `ModePolicy`.

### Out

- Registrar cuándo se abre Ver pista (ver Deferred Decisions).
- Pistas nuevas o reescritas: se usan las de Guided tal como están.
- Mover las pistas a un catálogo en la BD.
- Detalle por paso en el resumen de Guided o Assessment (sigue diferido, spec 007).
- Exigir una Práctica antes de Assessment o un mínimo de prácticas.
- Dashboard y reportes (Fase 10).

## Domain Model

- `SessionMode { Guided, Assessment, Practice }`; `Practice` = 2. Sin cambios de esquema:
  `TrainingSessions.Mode` es `TINYINT` sin `CHECK`. Solo se actualiza el comentario de `00`.
- **`ModePolicy`** en la BLL (`BLL/Training/`, la carpeta la confirma el plan), sin
  `System.Web`. Una instancia por modo con propiedades de solo lectura (nombres
  indicativos): `ShowsGuide`, `EnforcesSequence`, `PenalizesRetries`, `ForcesAdvance`,
  `FailureFeedback` (`Hint`, `RemainingAttempts` u `OnDemandHint`) y `Summary` (`Score`,
  `ScoreAndResult` o `AttemptsPerStep`).
- Las pistas siguen siendo texto de presentación. El plan decide si salen de
  `GuidedTest.aspx.cs` a una clase propia en `Pages/Training`.
- El resumen de Practice compone las sesiones y los intentos en la BLL (como pide
  `CLAUDE.md` para las relaciones situacionales). No hay colección nueva en el Domain.

## API Contract

Web Forms, sin endpoints. `PodService` no cambia de firma: decide con
`ModePolicy.For(ctx.Session.Mode)`. El resultado de un intento fallido ya trae lo que la
página necesita para la pista. Un método nuevo de la BLL (ej. en
`TrainingSessionService`) devuelve los intentos por paso de una sesión propia y completada,
para el resumen.

## Authorization

Sin cambios: el POD opera sobre la sesión propia y en curso; solo Operator entrena. El
resumen de intentos valida el dueño en la BLL, como `LoadContext`.

## Edge Cases & Failure Modes

- **Ver pista y después otro fallo:** la pista se oculta y vuelve el botón. Cada mensaje de
  fallo empieza cerrado.
- **Error de formato:** el control lo bloquea como hoy, sin intento ni Ver pista.
- **Fuera de secuencia:** Ver pista muestra la pista del paso **actual**, no la del botón
  presionado.
- **Logout fuera de turno en Practice:** intento fallido del paso actual. Nunca cierra nada
  ni se fuerza, aunque se repita.
- **F5, botón Atrás, Session de ASP.NET perdida y reconstrucción en Serialized:** igual que
  en Assessment (spec 009). El modo sale de la sesión en la BD.
- **Una sola sesión activa por operador (D-9):** cuenta también para Practice.
- **Sesiones abiertas al desplegar:** Guided y Assessment no cambian. Practice no existía
  antes, así que no hay sesiones viejas.
- **Paso con muchos intentos:** sin límite. El resumen muestra el número tal cual.

## Non-Functional Requirements

Sin cambios de volumen. La consulta del resumen es una sola llamada por sesión completada.

## Integrations

Ninguna.

## Acceptance Criteria

**ModePolicy (Entrega 1)**
- **AC-1:** Given Guided y Assessment, Then se comportan exactamente como antes: botones,
  guía, mensajes, penalización, avance forzado y resumen. Lo verifican los tests de
  `ModePolicy`, `ScoreCalculatorTests` en verde y una regresión en el navegador.
- **AC-2:** Given el código de la BLL y de la página, Then ninguna decisión de
  comportamiento compara `SessionMode` directamente, salvo `ModePolicy`.

**Practice**
- **AC-3:** Given `StartSession`, When elige Practice e inicia, Then se crea la sesión con
  `Mode` = 2 y el header del POD dice Practice.
- **AC-4:** Given Practice, Then no hay guía visual y todos los botones del POD están
  habilitados.
- **AC-5:** Given Practice, When presiona un botón fuera de secuencia, Then es intento
  fallido del paso actual con "Respuesta incorrecta." y Ver pista.
- **AC-6:** Given un intento fallido en Practice, When presiona Ver pista, Then ve la misma
  pista que Guided para ese paso, o el texto propio del control si lo tiene, sin postback.
- **AC-7:** Given Practice y 4 o más fallos en un paso, Then el paso no avanza solo y el
  siguiente acierto lo resuelve.
- **AC-8:** Given Practice completada, Then el resumen muestra cada paso con sus intentos,
  sin puntaje ni Aprobado/Reprobado.
- **AC-9:** Given Practice, Then el Ingreso a MES, la limpieza y el Split de Moldeo
  califican como en Assessment, con Ver pista al fallar.
- **AC-10:** Given Abandon en Practice, Then funciona como en los otros modos.

## Deferred Decisions

| Decisión | Respaldo elegido | Se revisa cuando |
|---|---|---|
| Registrar las pistas vistas (métrica "pasos con más ayuda") | No se registra | Fase 10 (dashboard) la pida |
| Pistas por error concreto en lugar de una por paso | Una por tipo de paso, como Guided | Un instructor lo pida |
| Detalle por paso en el resumen de Assessment | Sigue diferido (spec 007) | Lo pida un instructor |

## Open Questions

- **Nombre en la UI:** "Practice", en inglés como "Guided" y "Assessment". En
  `StartSession`: "Practice (como Assessment, sin puntaje y con pistas)". ¿Le parece bien?
