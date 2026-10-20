# Spec 012: Práctica libre (T3)

**Status:** Approved
**Date:** 2026-10-20 · **Approved:** 2026-10-20 (usuario)

> **Enmienda 1 (2026-10-20, usuario, al revisar el plan):**
>
> - **Destino de Saltar / Ir a…** (aclara "API Contract" frente a "Edge Cases"): un destino
>   igual o anterior al paso actual se **ignora** (doble clic, F5); un destino que no existe
>   en la guía, o un salto en un modo que no lo permite, es **excepción**.
> - **Resumen:** se arma con los intentos que ya trae el `PodContext` del cierre, sin método
>   ni consulta nueva (reemplaza el método de `TrainingSessionService` de "API Contract" y la
>   mención de `spStepAttemptGetBySession` del punto 6 de Scope).
> - **Puntaje del header:** en Práctica libre no se muestra "Score: x%" (P-10).
> - **Paso saltado en el resumen:** "Saltado" y, si hubo fallos antes, "Saltado (N fallos)".

> **Enmienda 2 (2026-10-20, usuario, al probar la Entrega 3):**
>
> - **Ver pista nombra el paso actual** ("Paso actual: {nombre con variante}"). Ajusta P-2
>   solo dentro de la pista: la pantalla sigue sin mostrar el paso.
> - **Estilo de la barra del simulador:** "Fiori Industrial", con íconos Lucide. Lo llevan
>   Abandon (en los tres modos), la cápsula Ver pista / Saltar / Ir a… y los botones de la
>   barra del escritorio, que conserva su fondo amarillo.
> - **Pistas por tramo en el Ingreso a MES y en Logout** (reemplaza "Pistas nuevas o
>   reescritas" de Out solo para estos dos pasos): la pista sale del avance que ya guarda
>   `PodState`, el mismo que usa la guía para retomar. No hay controles ni estado nuevos. Vale
>   también para la pista de Guided tras un fallo. Los demás pasos no cambian. Textos
>   aprobados:
>
>   | Paso | Tramo | Pista |
>   |---|---|---|
>   | Ingreso | antes del script | Haga doble clic en **MES Limpiar historial y cache**, escriba su Code1, presione Enter y luego cualquier tecla para cerrar la ventana. |
>   | Ingreso | script hecho, sin Edge | Cierre la consola con cualquier tecla y abra el **Repositorio** con doble clic. |
>   | Ingreso | en el Repositorio | Seleccione **Hora-Hora (3.0)**, escriba su Code1 y presione **Iniciar**. |
>   | Ingreso | Hora a Hora hecha | Vuelva a la pestaña del Repositorio, seleccione **Recursos MES**, escanee la etiqueta de su estación, escriba su Code1 y presione **Buscar**. |
>   | Ingreso | Recursos hecho | En el login de MES ingrese su usuario (Code1) y su contraseña y presione **Log On**. |
>   | Logout | antes del clic | Presione el ícono **Logout** para cerrar su sesión en MES. |
>   | Logout | después del clic | Cierre el navegador con la **✕**. |
>   | Logout | Edge cerrado | Haga doble clic en **MES Limpiar historial y cache**, escriba su Code1, presione Enter y luego cualquier tecla para terminar. |

Origen: revisión con Ernesto (2026-10-09, roadmap → Fase 8 → "Revisión con Ernesto", T3) y
Fase 8.5, fila 8. Decisiones del usuario del 2026-10-20, en dos rondas de preguntas y la
consulta sobre guías por tramo (P-1 a P-11).

## Problem

Hoy hay dos modos y ninguno sirve para practicar solo:

- **Guided** lleva de la mano: la guía visual señala cada clic, no deja enviar la decisión
  incorrecta y solo está habilitado el botón del paso. El operador aprende el recorrido, pero
  no decide ni explora.
- **Assessment** evalúa: sin ayuda, penaliza los reintentos, fuerza el avance al 4.º fallo,
  un botón fuera de turno es un intento fallido y el resultado queda como Aprobado o
  Reprobado.

Falta lo que pidió Ernesto: un **modo libre** en que el operador practique **sin
penalización y sin guía, solo con pistas**, y pueda **explorar la interfaz** sin que cada
clic cuente en su contra. También quiere ir directo a la parte que necesita practicar (por
ejemplo, Log NC).

Además, lo que cambia con cada modo está repartido en `mode == …` por la página, la BLL y
`ScoreCalculator`. Un tercer modo multiplicaría esas ramas, así que antes se concentran en un
solo objeto (`ModePolicy`).

## Decisiones

- **P-1 · Tercer modo "Práctica libre"** (`SessionMode.FreePractice`, `Mode` = 2), con ese
  nombre en español en la UI (usuario). Se elige en `StartSession` junto a Guided y
  Assessment. Solo Operator entrena, como en los otros modos.
- **P-2 · Sin guía visual**, como Assessment: el panel del hallazgo de Log NC se ve de
  inmediato y NC Selection no abre en la página del código del hallazgo. **No se muestra en
  qué paso va** (usuario): lo deduce, y la pista lo orienta.
- **P-3 · Todos los botones del POD habilitados**, para explorar.
- **P-4 · Botón de otro paso** (usuario): no cuenta como intento ni abre su control. Muestra
  el aviso "Ese no es el paso actual." Lo mismo con el ícono Logout antes del último paso y
  con el Repositorio fuera de su turno en el Ingreso a MES y en la limpieza.
- **P-5 · Botones del MES sin lógica** (Work Instructions, Reports, Place Back in Queue, Tool
  List, opciones de relleno de Activities): no hacen nada, sin aviso ni intento (usuario).
- **P-6 · Dentro de un paso se califica sin penalizar** (usuario): la decisión equivocada y
  el botón equivocado dentro de un control (ej. Add en vez de Add-Done) son intentos
  fallidos registrados, con "Respuesta incorrecta." y sin intentos restantes. Los errores de
  formato siguen como hoy: los bloquea el control, sin intento. Abrir un control y salir sin
  responder no cuenta, como en todos los modos.
- **P-7 · Reintentos ilimitados:** sin penalización y sin avance forzado. El paso se resuelve
  cuando acierta o cuando lo salta (P-9).
- **P-8 · Ver pista, siempre disponible** (usuario): un botón que muestra la pista del paso
  actual en cualquier momento, aunque no se haya equivocado. Es la misma pista que muestra
  Guided tras un fallo. Si el último fallo trajo un texto propio del control (ej. el botón
  equivocado de Log NC), lo muestra antes de la pista del paso. La pista llega con la
  página; abrirla es solo presentación, sin postback ni registro.
- **P-9 · Saltar paso e Ir a…** (usuario, en vez de guías por tramo; ver "Guías por tramo,
  descartadas"):
  - **Saltar paso** adelanta el paso actual como si se hubiera hecho.
  - **Ir a…** lista los pasos que siguen al actual y adelanta todos hasta el elegido.
  - Solo hacia adelante. Cada paso adelantado queda registrado como **saltado** y el POD
    queda como lo deja hoy el avance forzado de Assessment (`docs/domain.md` → "Avance
    forzado"): orden por defecto, Start ejecutado, hallazgo aplicado, etc. Saltar Logout
    cierra la sesión con su resumen.
  - **Piden confirmación** (usuario), con un diálogo corto en el cliente: "¿Saltar Resource
    Setup? No se puede volver." / "¿Ir a Log NC · desecho rápido? Se saltarán 6 pasos."
  - **Nombres de los pasos** en Ir a… y en el resumen (usuario): con su variante cuando el
    tipo se repite, como en `docs/domain.md` → "Ponderación de pasos": "Line Clearance
    (apertura)" y "(cierre)", "Log NC · desecho rápido", "· reparación rápida" y
    "· reparación extendida".
- **P-10 · Resumen con los intentos por paso** (usuario): sin puntaje ni Aprobado/Reprobado.
  Muestra el área, el producto, el modo y los pasos de la guía con sus intentos (1 = acertó
  a la primera), resaltando los que tuvieron más de uno y marcando los saltados. Al terminar
  solo se vuelve al inicio, como en los otros modos: no sugiere un Assessment (usuario).
- **P-11 · Medición:** los intentos se registran como en los otros modos y quedan separados
  por el modo de la sesión, sin columna nueva. El dashboard (Fase 10) puede contar los de
  Práctica libre aparte. `FinalScore` se calcula sin penalización (un paso saltado vale 0)
  y no se muestra.
- **P-12 · `ModePolicy`** (Entrega 1 del plan, sin cambio de comportamiento y con tests): un
  objeto de la BLL, creado a partir del `SessionMode`, que responde las capacidades de la
  tabla. La página, `PodService` y `ScoreCalculator` le preguntan a él, nunca `mode == …`,
  como `AccessPolicy` con los roles. Un modo nuevo es una columna más de `ModePolicy`, no
  ramas nuevas.

| Capacidad (`ModePolicy`) | Guided | Assessment | Práctica libre |
|---|---|---|---|
| Guía visual (y lo que solo ella recibe: código del hallazgo en NC Selection, panel del hallazgo revelado por la guía) | sí | no | no |
| Botones del POD habilitados | solo el del paso | todos | todos |
| Botón de otro paso | se ignora | intento fallido del paso actual | aviso, sin intento |
| Botones sin lógica | deshabilitados | intento fallido del paso actual | no hacen nada |
| Penaliza reintentos | no | sí | no |
| Avance forzado al 4.º fallo | no | sí | no |
| Mensaje al fallar | pista del paso | intentos con puntaje restantes | "Respuesta incorrecta." |
| Ver pista siempre disponible | no | no | sí |
| Saltar paso / Ir a… | no | no | sí |
| Resumen | puntaje (como hoy) | puntaje y Aprobado/Reprobado | intentos por paso |

### Guías por tramo, descartadas (usuario, 2026-10-20)

Se evaluó tener varias guías por área (completa, Pre-MES, apertura de orden, cierre de
orden). Se descartó porque:

- Una guía que empieza a la mitad necesita su propio escenario precargado (orden iniciada,
  ensamblada, con DC), que hoy solo construye el motor del POD paso a paso.
- Multiplica el catálogo (3 áreas × 4 guías), con sus pesos que suman 1.00 y sus versiones
  (D-56).
- Contradice T5 (guías por área).

Saltar e Ir a… reusan el avance forzado, no tocan la BD ni el catálogo y el operador ve
siempre el flujo completo de su área. Lo que se pierde: tras saltar, el POD tiene los datos
por defecto y no lo que el operador habría escrito. Si se quieren tramos con nombre, se
agregan como grupos dentro de Ir a…, sin guías nuevas.

## Scope

### In

1. **`ModePolicy`** (P-12): reemplaza las ramas por modo de `GuidedTest.aspx.cs`,
   `PodService` y `ScoreCalculator`. Guided y Assessment quedan exactamente igual
   (Entrega 1).
2. **Modo Práctica libre** (P-1): enum, opción en `StartSession` y etiqueta del modo en el
   header del POD, en la barra del simulador (spec 016, P-11) y en el resumen.
3. **Comportamiento del POD** según la columna de Práctica libre (P-2 a P-7), incluidas las
   pantallas del Ingreso a MES, la limpieza y el Split de Moldeo.
4. **Barra de práctica** con Ver pista, Saltar paso e Ir a… (P-8, P-9): visible en el POD y
   en el escritorio, con el estilo de la barra del simulador y no del MES (las herramientas
   del simulador no imitan la UI real, `docs/process.md` §8). Su lugar exacto lo define el
   plan.
5. **Saltar paso / Ir a…** en `PodService` (P-9), con el motivo de intento nuevo `Skipped`.
6. **Resumen de Práctica libre** (P-10), con los intentos de `spStepAttemptGetBySession`.
7. Documentación: `docs/domain.md` → "Modos de sesión", "Qué cuenta como intento" y "Avance
   forzado" ganan Práctica libre; `CLAUDE.md` y `docs/architecture.md`, `ModePolicy`.

### Out

- Guías por tramo (ver arriba).
- Volver a un paso anterior o rehacer uno ya resuelto (para eso, Abandon y una sesión nueva).
- Abrir el control de otro paso para mirarlo en solo lectura.
- Registrar cuándo se abre Ver pista (ver Deferred Decisions).
- Pistas nuevas o reescritas: se usan las de Guided tal como están.
- Mover las pistas a un catálogo en la BD.
- Detalle por paso en el resumen de Guided o Assessment (sigue diferido, spec 007).
- Exigir una práctica antes de Assessment o un mínimo de prácticas.
- Dashboard y reportes (Fase 10).

## Domain Model

- `SessionMode { Guided, Assessment, FreePractice }`; `FreePractice` = 2.
- `AttemptReason.Skipped` = 6: intento no correcto que **resuelve** el paso, como el avance
  forzado.
- **Sin cambios de esquema:** `TrainingSessions.Mode` y `TrainingSessionStepAttempts.Reason`
  son `TINYINT` sin `CHECK`. Se actualizan los comentarios de `00`. El plan verifica que
  `spStepAttemptRecord` pueda registrar un paso saltado y avanzar (hoy avanza al forzar).
- **`ModePolicy`** en la BLL (`BLL/Training/`, lo confirma el plan), sin `System.Web`. Una
  instancia por modo con propiedades de solo lectura (nombres indicativos): `ShowsGuide`,
  `EnablesAllButtons`, `OutOfSequence` (`Ignore`, `RecordAttempt`, `Notice`),
  `NoLogicButtons` (`Disabled`, `RecordAttempt`, `Inert`), `PenalizesRetries`,
  `ForcesAdvance`, `FailureFeedback` (`Hint`, `RemainingAttempts`, `Incorrect`),
  `HintOnDemand`, `AllowsSkip` y `Summary` (`Score`, `ScoreAndResult`, `AttemptsPerStep`).
- Las pistas siguen siendo texto de presentación. El plan decide si salen de
  `GuidedTest.aspx.cs` a una clase propia en `Pages/Training`.
- El resumen compone la sesión y sus intentos en la BLL (relación situacional, `CLAUDE.md` →
  "Domain"). No hay colección nueva.

## API Contract

Web Forms, sin endpoints.

- **`PodService`** decide con `ModePolicy.For(ctx.Session.Mode)`. Un botón de otro paso
  devuelve `OutOfSequence` sin intento cuando la política dice `Notice`, y la página muestra
  el aviso.
- **Comando nuevo de salto:** recibe el `Order` del paso destino (Saltar = el siguiente).
  Solo si la política lo permite, la sesión es propia y está en curso, y el destino está
  después del paso actual (si no, excepción: es un invariante, no un intento). Registra
  `Skipped` en cada paso adelantado y aplica la misma restauración del avance forzado.
- **Resumen:** un método de la BLL (ej. en `TrainingSessionService`) devuelve los intentos
  por paso de una sesión propia y completada.

## Authorization

Sin cambios: el POD opera sobre la sesión propia y en curso; solo Operator entrena. Saltar
y el resumen validan el dueño en la BLL, como `LoadContext`. Un postback de salto en una
sesión de Guided o Assessment se rechaza en la BLL.

## Edge Cases & Failure Modes

- **Ver pista tras varios fallos:** muestra el texto del último fallo (si el control lo
  trajo) y la pista del paso. Después de acertar, la pista es la del paso nuevo.
- **Fuera de secuencia:** el aviso no abre nada. Ver pista muestra la del paso **actual**.
- **Saltar con un paso a medias** (Assembly o DC con avance parcial): se descarta el
  parcial, como en el avance forzado.
- **Ir a… un paso de Log NC:** el hallazgo de ese paso se genera como hoy. Los Log NC
  saltados aplican la disposición de su hallazgo.
- **Saltar el Ingreso a MES:** el POD aparece como si hubiera entrado (spec 016, P-5).
- **Saltar Logout o Ir a… más allá del último:** solo existe "hasta Logout". Saltarlo cierra
  la sesión con su resumen.
- **Doble clic o F5 en Saltar:** el destino viaja con el postback y el servidor ignora un
  destino que ya no está adelante (la sesión ya avanzó). Así no se salta dos veces.
- **Botón Atrás, Session de ASP.NET perdida y reconstrucción en Serialized:** igual que en
  Assessment (spec 009). El modo sale de la sesión en la BD.
- **Una sola sesión activa por operador (D-9):** cuenta también para Práctica libre.
- **Sesiones abiertas al desplegar:** Guided y Assessment no cambian. Práctica libre no
  existía antes.

## Non-Functional Requirements

Sin cambios de volumen. Ir a… hasta el final hace a lo sumo un registro por paso de la guía
(hoy ≤ 12, en LoFlo y Redefinity), en el mismo request.

## Integrations

Ninguna.

## Acceptance Criteria

**ModePolicy (Entrega 1)**
- **AC-1:** Given Guided y Assessment, Then se comportan exactamente como antes: botones,
  guía, mensajes, penalización, avance forzado y resumen. Lo verifican los tests de
  `ModePolicy`, `ScoreCalculatorTests` en verde y una regresión en el navegador.
- **AC-2:** Given la BLL y la página, Then ninguna decisión de comportamiento compara
  `SessionMode` directamente, salvo `ModePolicy`.

**Práctica libre**
- **AC-3:** Given `StartSession`, When elige Práctica libre e inicia, Then se crea la sesión
  con `Mode` = 2 y el header dice "Práctica libre".
- **AC-4:** Given Práctica libre, Then no hay guía visual, no se muestra el paso actual y
  todos los botones del POD están habilitados.
- **AC-5:** Given Práctica libre, When presiona el botón de otro paso (o Logout antes del
  último paso, o el Repositorio fuera de turno), Then ve "Ese no es el paso actual.", no se
  abre nada y no se registra intento.
- **AC-6:** Given Práctica libre, When presiona un botón del MES sin lógica, Then no pasa
  nada y no se registra intento.
- **AC-7:** Given una decisión equivocada o un botón equivocado dentro de un control, Then se
  registra un intento fallido y ve "Respuesta incorrecta.", sin intentos restantes.
- **AC-8:** Given 4 o más fallos en un paso, Then el paso no avanza solo y el siguiente
  acierto lo resuelve.
- **AC-9:** Given cualquier momento de un paso, When presiona Ver pista, Then ve la pista
  del paso actual (la de Guided), sin postback ni registro.
- **AC-10:** Given un paso en curso, When presiona Saltar paso, Then el paso queda saltado,
  el POD queda como tras el avance forzado y sigue el paso siguiente.
- **AC-11:** Given Ir a… con un paso posterior, Then todos los pasos intermedios y el actual
  quedan saltados y el paso elegido es el actual. Ir a… no ofrece pasos anteriores.
- **AC-11b:** Given Saltar paso o Ir a…, When lo presiona, Then aparece un diálogo de
  confirmación con el nombre del paso (y en Ir a…, cuántos se saltan). Cancelar no hace nada.
- **AC-12:** Given Práctica libre completada, Then el resumen muestra cada paso con sus
  intentos, marca los saltados y no muestra puntaje ni Aprobado/Reprobado.
- **AC-13:** Given un postback de salto en Guided o Assessment, Then la BLL lo rechaza.
- **AC-14:** Given Abandon en Práctica libre, Then funciona como en los otros modos.

## Deferred Decisions

| Decisión | Respaldo elegido | Se revisa cuando |
|---|---|---|
| Registrar las pistas vistas (métrica "pasos con más ayuda") | No se registra | Fase 10 (dashboard) la pida |
| Tramos con nombre en Ir a… (Pre-MES, apertura, cierre) | Lista plana de pasos | Lo pida un instructor |
| Pistas por error concreto en lugar de una por paso | Una por tipo de paso, como Guided | Lo pida un instructor |
| Detalle por paso en el resumen de Assessment | Sigue diferido (spec 007) | Lo pida un instructor |

## Open Questions

_Ninguna._
