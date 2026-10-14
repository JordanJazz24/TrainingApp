# Spec 015: Ajustes de la trainer (Remove All, Logout final y grupo Trainer)

**Status:** Approved (2026-10-14)
**Date:** 2026-10-14 · **Approved:** 2026-10-14 (usuario, con las propuestas de diseño incluidas)

> **Enmienda 2026-10-14 (usuario, al probar la Entrega 1):** afecta el punto 1 de Scope y
> AC-1.
>
> - El control de Resource Setup **abre con los equipos precargados ya en la tabla**;
>   **Retrieve** solo habilita la edición (Setup State, toolbar y Save), como antes de esta
>   spec. Motivo del usuario: que aparezcan al hacer Retrieve puede confundir. Reemplaza "al
>   hacer Retrieve, la tabla aparece con los equipos…" del punto 1.
> - **AC-1 (reemplazado):** Given Resource Setup, When se abre el control, Then la tabla
>   muestra una Shop Order y 2 herramientas precargadas, distintas de `303406950`, en Guided
>   y en Assessment; Retrieve solo habilita la edición.
> - **Decisión de implementación** (desarrollo, del `/code-review` de la Entrega 1): si el
>   Save trae un precargado y no hay una Shop Order válida y propia (distinta de la
>   precargada), el intento fallido se registra pero **no se crea la orden simulada** (antes
>   podía crearse con la Shop Order del lote anterior). No cambia la calificación (AC-2).

## Problem

En la validación con la trainer de Wires (2026-10-13, roadmap → Fase 8 → "Retroalimentación
de la trainer") salieron tres prácticas de planta que el simulador no entrena:

1. **No hacer Remove All en Resource Setup** (su error 3): el operador asume que los equipos
   que ya están cargados son los de su estación y trabaja con una herramienta equivocada; se
   pierde la trazabilidad. Hoy Retrieve muestra la tabla vacía, así que no hay nada que quitar.
2. **No cerrar sesión al dejar la estación:** hubo CAPAs por Code1 compartidos y sesiones
   abiertas. Hoy Logout es navegación y nunca cuenta (`CLAUDE.md` → "Qué cuenta como intento").
3. **No dejar evidencia de quién hizo la pieza:** mientras no se certifica, la persona en
   entrenamiento trabaja con el Code1 del trainer; el grupo de DC "Trainer" (existe en toda la
   planta) es donde se escribe su nombre. El catálogo del simulador no lo tiene.

## Scope

### In

**A. Resource Setup con equipos precargados**
1. Al hacer **Retrieve**, la tabla aparece con los equipos "del turno o lote anterior": una
   **Shop Order** y **2 herramientas**, con números **generados por la BLL** en cada Retrieve
   (como los SFC de Serialized), distintos de la orden por defecto (`303406950`). En Guided y
   en Assessment.
2. El operador tiene que quitarlos (Remove All, o fila por fila con Remove Selected) y
   registrar su Shop Order y sus herramientas.
3. **Guardar con cualquier equipo precargado en la tabla es una decisión equivocada:** en
   Assessment, intento fallido del paso con el mensaje genérico (no revela el motivo); en
   Guided, la guía no deja enviarlo. Lo que se califica es que **no quede ninguno**, no el
   botón que se usó (usuario).
4. Sin cambios: Setup State = "Set Up" sigue siendo obligatorio; el avance forzado carga la
   orden por defecto.

**B. Logout como último paso**
5. **Paso nuevo `Logout`** después de Complete en todas las guías: el clic correcto es el
   ícono **Logout** del header del POD.
6. **Peso** (usuario): Logout **0.05**, que sale de Complete (0.10 → 0.05) en todas las guías.
   Revisable con T6. Pesos nuevos:

   ```
   Paso                        Moldeo (07)   LoFlo (09)   Redefinity (05, ejemplo)
   Complete                       0.05         0.05          0.05
   Logout                         0.05         0.05          0.05
   (el resto sin cambios)         —            —             —
   Total                          1.00         1.00          1.00
   ```
7. **Clic correcto** (usuario): Logout **simulado**. Registra el paso, cierra la sesión de
   entrenamiento y muestra el **resumen final** (lo que hoy aparece tras Complete). **No**
   cierra la sesión de la app y **no** hay pantalla intermedia de logout del MES (usuario: no
   hay captura). Complete deja de cerrar la sesión de entrenamiento.
8. **Antes del último paso** (usuario): en **Assessment**, el ícono Logout es un botón más del
   POD (regla S2): intento fallido del paso actual, con el mensaje genérico, y **no cierra
   nada**. En **Guided** se ignora, como cualquier botón fuera de turno. Para salir a mitad de
   una sesión queda **Abandon** en los dos modos.
9. **Guía de Guided:** el subpaso 1 de Logout señala el ícono (ancla `data-guide` nueva; hoy
   es `data-guide-free`) con esta **nota** (usuario, texto literal):
   > Cierra sesión en MES siempre que dejes de usar activamente el sistema, como pero no
   > limitado a: durante los descansos; al final de un turno; cuando te mueves a otra
   > estación de trabajo o actividad; transfiriendo la estación a otro operador; moviéndote
   > entre productos, órdenes de trabajo o líneas de fabricación que requieren diferentes
   > responsabilidades de usuario; siempre que tu sesión abierta pudiera ser accedida por
   > otra persona.

**C. Grupo de DC "Trainer"**
10. Un grupo de DC **Trainer** en **todas las áreas con Data Collection** (hoy Moldeo, LoFlo y
    Redefinity; Subensamble 1 no tiene DC), con un parámetro de texto para el **nombre de la
    persona en entrenamiento**. Datos reales del MES (usuario, 2026-10-14): código
    **`DCGTM_TRAINEE/A`**, descripción **"Entrenamiento personal"**, `CollectDataAt` =
    **`Anytime`**. El nombre del parámetro sigue provisional (ver Deferred Decisions). En Redefinity (`05`) y LoFlo (`08`) **reemplaza** el tercer
    grupo opcional; en Moldeo (`06`) se **agrega**.
11. **Opcional** (usuario), como en el MES real: no cuenta para completar Data Collection de
    la pieza. En Guided la guía **no** pasa por el grupo: solo muestra la nota "Solo cuando se
    está entrenando" (usuario).
12. **Nombre válido** (usuario): letras (con tildes y ñ) y espacios, de 2 a 60 caracteres. Lo
    que no cumple lo bloquea el control como **error de formato, sin intento**. Vacío es
    válido (es opcional). El servidor repite la validación.

### Out

- Pantalla de logout del MES (sin captura; si llega, se evalúa aparte).
- Cierre automático del MES a los 15 minutos (sin verificar: T9).
- Preparación antes de SAP (spec 016), tres tipos de scrap (spec 014) y 4-5 piezas por orden.
- Exigir el grupo Trainer o calificar el nombre escrito.
- Exigir el botón Remove All (vale cualquier forma de quitar los equipos).

## Domain Model

- **`StepType.Logout`** nuevo. Las guías `05`, `07` y `09` agregan el paso después de
  Complete y bajan Complete a 0.05 (`spTrainingGuideSync` valida que sumen 1.00).
- **Equipos precargados:** los genera la BLL (generador nuevo, como `SerialSfcGenerator`) y
  se guardan en `PodState` al hacer Retrieve, para que el servidor sepa cuáles eran al
  calificar el Save. No van a la BD de catálogo: son datos de escenario, no de negocio.
  Propuesta: Retrieve pasa a ser un postback del control **o** los valores se generan al
  abrir el control y viajan con él; lo decide el plan, respetando "un postback por paso"
  (`CLAUDE.md` → "POD y controles").
- **Grupo Trainer:** `DataCollectionGroups` con `IsRequired = 0` y un
  `DataCollectionParameter` con un **`DataType` nuevo** (propuesta: `PersonName`) que el
  control y la BLL validan como nombre. Los demás tipos no cambian.
- `TrainingSession`: el cierre (`FinalScore`, `CompletedAt`) pasa de Complete a Logout.

## API Contract

Web Forms, sin endpoints HTTP.

- **Resource Setup:** el Save envía las filas como hoy; `StepValidator` (o `PodService`) las
  compara con los equipos precargados del `PodState`. Si queda alguno → intento fallido
  (`AttemptReason.WrongDecision`, `Action` del Save; spec 013).
- **`PodService`**: un comando nuevo para el clic en Logout. En el paso Logout, registra el
  intento correcto y cierra la sesión (misma transacción de `spStepAttemptRecord`). Antes de
  ese paso, en Assessment, es un clic fuera de secuencia (`OutOfSequence`); en Guided, se
  ignora. `GuidedTest` deja de llamar `SessionHelper.Logout()` desde el ícono del POD.
- **Data Collection:** validación de `PersonName` en el control (formato) y en la BLL.

## Authorization

Sin cambios: el POD opera sobre la sesión propia y en curso; solo Operator entrena.

## Edge Cases & Failure Modes

- **Cerrar el control de Resource Setup y volver a abrirlo:** un Retrieve nuevo genera otros
  equipos precargados (reemplaza los del `PodState`).
- **Volver a escribir a mano un equipo precargado:** cuenta como precargado (se compara por
  valor) → intento fallido.
- **Avance forzado de Resource Setup** (4.º intento): sin cambios, orden por defecto.
- **Avance forzado de Logout** (4 clics fuera de turno en Assessment mientras Logout es el
  paso actual): el paso queda fallido y la sesión se cierra con su resumen.
- **Abandon en el paso Logout:** abandona la sesión, sin puntaje (como hoy).
- **Session de ASP.NET perdida en el paso Logout:** la sesión se reconstruye como hoy (spec
  009); el paso actual lo define el servidor.
- **Sesiones en curso al desplegar** (guía sin paso Logout): siguen con su guía cargada;
  `spTrainingGuideSync` versiona o reemplaza según hace hoy (el plan lo verifica).
- **Nombre con números, símbolos o `<x`:** formato, bloqueado en el control.

## Non-Functional Requirements

Sin cambios de volumen ni rendimiento. Los scripts `05`–`09` siguen siendo idempotentes.

## Integrations

Ninguna.

## Acceptance Criteria

**Resource Setup**
- **AC-1:** Given Resource Setup, When hace Retrieve, Then la tabla muestra una Shop Order y 2
  herramientas precargadas, distintas de `303406950`, en Guided y en Assessment.
- **AC-2:** Given equipos precargados en la tabla, When guarda (Set Up), Then en Assessment es
  intento fallido con el mensaje genérico.
- **AC-3:** Given que quitó los precargados (Remove All o Remove Selected) y cargó los suyos,
  When guarda con Set Up, Then el paso se resuelve como hoy.
- **AC-4:** Given que reescribió a mano un valor precargado, When guarda, Then es intento
  fallido.
- **AC-5:** Given Guided, Then la guía no deja guardar con equipos precargados.

**Logout**
- **AC-6:** Given todas las guías, Then tienen el paso Logout después de Complete, con Complete
  0.05 y Logout 0.05, y suman 1.00.
- **AC-7:** Given Complete resuelto, Then la sesión sigue en curso con Logout como paso actual
  (no aparece el resumen).
- **AC-8:** Given Logout como paso actual, When hace clic en el ícono Logout, Then se registra
  el intento correcto, la sesión se cierra con su `FinalScore` y aparece el resumen final, sin
  cerrar la sesión de la app.
- **AC-9:** Given Assessment y un paso anterior a Logout, When hace clic en Logout, Then es
  intento fallido del paso actual y no cierra nada.
- **AC-10:** Given Guided y un paso anterior a Logout, When hace clic en Logout, Then no pasa
  nada.
- **AC-11:** Given Guided en el paso Logout, Then la guía señala el ícono con la nota del
  punto 9.
- **AC-12:** Given Abandon en cualquier paso, Then funciona como hoy.
- **AC-13:** Given `ScoreCalculatorTests`, Then siguen en verde (con los pesos nuevos en sus
  datos de prueba si hace falta).

**Grupo Trainer**
- **AC-14:** Given Moldeo, LoFlo y Redefinity, Then cada una tiene el grupo Trainer opcional con
  el parámetro de nombre; Redefinity y LoFlo ya no tienen su tercer grupo opcional anterior.
- **AC-15:** Given una pieza con sus grupos obligatorios guardados y el Trainer vacío, Then la
  pieza completa Data Collection.
- **AC-16:** Given un nombre con números o símbolos, When guarda el grupo, Then el control lo
  bloquea como formato y no se registra intento.
- **AC-17:** Given "José Peña", When guarda el grupo, Then se acepta.
- **AC-18:** Given Guided, Then la guía muestra la nota "Solo cuando se está entrenando" en el
  grupo Trainer.

## Deferred Decisions

| Decisión | Respaldo elegido | Se revisa cuando |
|---|---|---|
| Pantalla de logout del MES | Sin pantalla intermedia: directo al resumen | Llegue una captura (planta o T9) |
| Peso de Logout y de Complete | 0.05 y 0.05 | T6 (documento de Ernesto) |
| Cierre automático del MES a los 15 min | No se simula | T9 lo confirme |
| Nombre del parámetro del grupo `DCGTM_TRAINEE/A` en el MES real (el grupo ya es real) | Provisional marcado `SIM` (propuesta: `NOMBRE_EN_ENTRENAMIENTO`) | La trainer o T9 den el nombre real |

## Open Questions

_Ninguna._
