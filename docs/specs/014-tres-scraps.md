# Spec 014: Tres tipos de scrap en Log NC y pieza defectuosa indicada

**Status:** Draft (2026-10-14)
**Date:** 2026-10-14 · **Approved:** —

Fuentes: roadmap → Fase 8 ("Revisión con SAP MES", "Revisión con Ernesto", "Validación con
técnicos de MES", "Retroalimentación de la trainer") y Fase 8.5, tarea 6 (T4); WI
QD001001524 rev. 27 (`docs/ui-reference/wi-produccion-disposables.md`, W19-W21; el archivo
vive en la rama `docs/wi-produccion-contraste` hasta que se integre en `TrainingApp`).

## Problem

1. **Tres métodos de NC en uso, el simulador entrena uno.** El MES real tiene tres métodos
   (WI, W20): **desecho rápido** (`F_FS…`, desecha la unidad), **reparación rápida**
   (`F_FR…`, quita un componente, lo devuelve o lo desecha y agrega otro; la unidad sigue en
   la operación) y **reparación rápida extendida** (`F_FX…`, la unidad vuelve a una
   operación anterior). Lo pidieron el ingeniero de SAP MES, Ernesto (que se obliguen los
   tres) y los técnicos de MES. Hoy Log NC acepta cualquier código del catálogo y siempre
   desecha.
2. **Scrapear las unidades equivocadas** es el error más grave según la trainer: el MES no lo
   detecta y les pasa también a veteranos. Hoy el simulador no dice cuál pieza está
   defectuosa: cualquier fila scrapeable es correcta.
3. Con 3-5 piezas por orden Serialized no alcanzan las piezas para los tres métodos y
   todavía dejar piezas para Complete.

## Decisiones del usuario (2026-10-14)

- **D-27 · Alcance:** spec completa con las reglas y la calificación de los tres métodos. Lo
  que no necesita pantallas nuevas se implementa primero; la UI de la reparación rápida y de
  la extendida se maqueta desde las **capturas de los tres tipos de scrap que aporta el
  usuario** (2026-10-14), sin inventarla (`docs/process.md` §8).
- **D-28 · Hallazgo con disposición:** el operador sabe qué pieza está defectuosa y qué hacer
  con ella por una **nota de hallazgo**, en los dos modos: la pieza, el defecto y la
  disposición (como la dictaría el MPI o el NCR). Se califica ejecutar el método correcto
  sobre la pieza correcta. En Guided, además, la guía señala la pieza.
- **D-29 · Tres pasos, 0.15 en total:** desecho rápido, reparación rápida y reparación
  extendida son **tres pasos** de la guía, en **orden fijo**, de **0.05** cada uno; Data
  Collection baja de 0.30 a 0.25 en las guías que los tienen.
- **D-30 · Solo Serialized:** los tres métodos se obligan en LoFlo y Redefinity. Qué métodos
  entrena un área lo deciden **los pasos de su guía** (sin flag nuevo): si T5 cambia las
  guías, no cambia el código. Moldeo sigue con SFC Split + desecho rápido, y
  `F_FR_GENERAL` y `F_FX_GENERAL` dejan de valer como desecho.
- **D-31 · Código exacto:** el hallazgo describe un defecto que corresponde a **un código del
  catálogo**; cualquier otro código, aunque sea del método correcto, es un intento fallido.
- **D-32 · Lote diferido:** en NonSerialized no se califica la cantidad del split (el
  equivalente del error 1 en Moldeo). Se revisa con T5.

## Propuestas de diseño (Claude, confirmadas por el usuario el 2026-10-14)

- **P-1 · Método en el catálogo:** `NCCodes` gana una columna `Method` (`FastScrap`,
  `FastRepair`, `ExtendedRepair`) en vez de deducirlo del prefijo (LoFlo usa `SIM_FS_…` y
  Redefinity `SC-00x`). LoFlo (`08`) y Redefinity (`05`) agregan códigos simulados de
  reparación rápida y extendida (marcados `SIM`, 2-3 de cada uno); en Moldeo (`06`),
  `F_FR_GENERAL` = `FastRepair` y `F_FX_GENERAL` = `ExtendedRepair`.
- **P-2 · La nota de hallazgo es del simulador, no del MES:** un panel con el estilo de la
  guía (no imita una pantalla del MES), visible mientras el paso actual es uno de Log NC.
  Enumera los hallazgos **en el orden de los pasos** (1. desecho, 2. reparación, 3.
  extendida), cada uno con el SFC de la pieza, el defecto (la descripción del código) y la
  disposición. No nombra el código.
- **P-3 · Se califica en Add-Done:** la pieza y el código se califican juntos al enviar el
  NC (la fila elegida ya es una decisión, `docs/domain.md` → "Qué cuenta como intento").
  Abrir Log NC con otra pieza y salir con Return to POD no cuenta: el daño ocurre al
  registrar el NC. La pieza correcta es **exactamente** la del hallazgo: otra pieza o una
  pieza de más (Apply to all SFCs) es un intento fallido.
- **P-4 · Qué pasa con cada pieza:** el desecho la saca de la orden (como hoy); la
  reparación rápida la deja en el POD y sigue a Complete; la extendida la regresa a una
  operación anterior y sale del POD. Con 4 piezas, a Complete llegan al menos 2.
- **P-5 · Avance forzado** (4.º intento en Assessment): el paso queda fallido y el servidor
  aplica la disposición del hallazgo a su pieza, para que los pasos siguientes encuentren el
  POD como si se hubiera hecho bien (igual criterio que Start forzado).
- **P-6 · Moldeo también con hallazgo:** D-31 aplica a todo Log NC. En Moldeo el hallazgo
  dice el defecto del lote y su código real (`F_FS_…`) es el único correcto; la pieza es la
  del split y la cantidad sigue libre (D-32).
- **P-7 · Tipos de paso:** `LogNC` (6) pasa a significar desecho rápido; se agregan
  `LogNCFastRepair` y `LogNCExtendedRepair`. Los tres se abren con el mismo botón [Log NC]
  del POD; la regla de secuencia (S2) acepta ese botón en cualquiera de los tres.
- ~~P-8 · Pesos de transición~~: descartada. El usuario aporta las capturas de los tres
  métodos, así que los dos pasos nuevos entran juntos a las guías con la tabla final.

## Scope

### In

**A. Sin pantallas nuevas (se implementa ya)**
1. **Piezas por orden Serialized: 4-5** (hoy 3-5). Reemplaza el rango de `docs/domain.md`
   → "Flujo estándar".
2. **Método de cada código NC** en el catálogo (P-1), con los códigos simulados nuevos de
   LoFlo y Redefinity.
3. **Hallazgos generados por la BLL** al iniciar la orden (Start): uno por cada paso de Log NC
   de la guía, cada uno sobre una pieza **distinta** elegida al azar, con un código del método
   del paso elegido al azar del catálogo del área. La reparación rápida agrega el componente a
   reemplazar (del BOM) y si se devuelve o se desecha; la extendida, la operación a la que
   vuelve (ver Deferred Decisions). Se guardan en `PodState`, no en la BD de catálogo
   (escenario, como los equipos precargados de la spec 015).
4. **Nota de hallazgo** (P-2) en Guided y Assessment.
5. **Desecho rápido calificado por pieza y código** (P-3, D-31): correcto solo con la pieza
   del hallazgo y su código exacto. En Assessment, lo demás es intento fallido con el mensaje
   genérico (no revela si falló la pieza o el código). En Guided, la guía señala la pieza y no
   deja enviar otra pieza ni otro código (como D-25 de la spec 015).
6. **Moldeo** (D-30, P-6): `F_FR_GENERAL` y `F_FX_GENERAL` siguen en NC Selection (en el MES
   real aparecen) pero nunca son correctos para el desecho.

**B. Con pantallas (se maqueta desde las capturas del usuario)**
7. **Reparación rápida** (paso `LogNCFastRepair`, LoFlo y Redefinity): flujo de la WI W21 —
   código `F_FR` → Component → OK → Add Done → confirmar la remoción → Return o Scrap →
   ¿agregar componente? → datos del nuevo → Add. Se resuelve en el cliente y se envía en un
   solo postback (`CLAUDE.md` → "POD y controles"). **Correcto** solo si la pieza, el código,
   el componente, Return/Scrap y "agregar componente" coinciden con el hallazgo. Los errores
   de formato (campo vacío, no numérico, `<x`) los bloquea el control sin intento.
8. **Reparación extendida** (paso `LogNCExtendedRepair`): código `F_FX` → componente
   opcional → Add Done → operación destino → confirmación. **Correcto** solo si la pieza, el
   código y la operación destino coinciden con el hallazgo; el componente y Comments no se
   califican.
9. **Guías** `05` y `09` con los tres pasos en orden (desecho → reparación → extendida) y los
   pesos finales, los dos pasos nuevos a la vez. `07` (Moldeo) no cambia de pesos.
10. **Lo que muestren las capturas** se documenta en `docs/ui-reference/general-flow.md` →
    "Paso 8" antes de maquetar. Si una captura contradice esta spec (orden del flujo,
    campos, qué decide el operador), se enmienda la spec antes del código.

Pesos finales de LoFlo (`09`) y Redefinity (`05`), con Logout de la spec 015:

```
Paso                         LoFlo / Redefinity
Resource Setup                   0.20
Line Clearance (apertura)        0.05
Search / Start                   0.05
Assembly                         0.15
Data Collection                  0.25   (antes 0.30)
Line Clearance (cierre)          0.05
Log NC · desecho rápido          0.05   (antes Log NC 0.10)
Log NC · reparación rápida       0.05   (nuevo)
Log NC · reparación extendida    0.05   (nuevo)
Complete                         0.05
Logout                           0.05
Total                            1.00
```

### Out

- Calificar la cantidad del split en NonSerialized (D-32).
- Los tres métodos en Moldeo (D-30).
- Código `F` (fallas de DC, DC-DC ESign Exceptions) y Unscrap.
- Comments obligatorios de `F_FX_GENERAL` (MPI y revisión o NCR): no se califican.
- Validar el componente nuevo de la reparación rápida contra el BOM más allá de lo que pida
  el hallazgo (el barcode sigue sin validarse contra el número de parte, como en Assembly).
- Que la pieza reparada con la extendida reaparezca en otra operación (no hay Operations).

## Domain Model

- **`NCCode.Method`** (enum nuevo `NCMethod`: `FastScrap`, `FastRepair`, `ExtendedRepair`).
  Columna `NOT NULL` en `NCCodes`; los scripts `05`, `06` y `08` la cargan.
- **`StepType`**: `LogNCFastRepair` y `LogNCExtendedRepair` nuevos (P-7). `LogNC` = desecho
  rápido. Cada tipo de Log NC acepta solo códigos de su método.
- **`NcFinding`** (BLL/Training, en `PodState`): paso, SFC de la pieza, código, y según el
  método: componente + Return/Scrap + agregar (reparación rápida) u operación destino
  (extendida). Lo genera un generador nuevo (como `SerialSfcGenerator`).
- **`SerialSfcGenerator.MinPieces`** = 4.
- `spTrainingGuideSync` ya valida que los pesos sumen 1.00. Propuesta para el plan: que
  rechace una guía con `LogNCFastRepair` en un área sin BOM.

## API Contract

Web Forms, sin endpoints HTTP.

- **`PodService.OpenLogNC`**: igual que hoy (secuencia y fila scrapeable); acepta el botón
  [Log NC] en los tres pasos. No califica la pieza (P-3).
- **`PodService.SubmitLogNC`** (y sus equivalentes de los pasos nuevos): recibe el código,
  las filas seleccionadas y, en la parte B, las respuestas del flujo. `StepValidator` compara
  todo con el `NcFinding` del paso. Fallo → `AttemptReason.WrongDecision` con la acción del
  envío (spec 013). Correcto o forzado → aplica la disposición a la pieza (P-4, P-5) en la
  misma transacción (`spStepAttemptRecord`).
- **`PodContext`** expone los hallazgos para la nota y para la guía (solo lectura).

## Authorization

Sin cambios: el POD opera sobre la sesión propia y en curso; solo Operator entrena.

## Edge Cases & Failure Modes

- **Pieza del hallazgo ya no está en el POD** (no puede pasar con el orden fijo: los
  hallazgos son de piezas distintas y Complete va después). Si pasara, es un error de
  invariante (excepción), no un intento.
- **Apply to all SFCs con varias piezas seleccionadas**: intento fallido (P-3).
- **Selección parcial en Complete** sigue siendo aviso sin penalización (spec 008).
- **Session de ASP.NET perdida** (spec 009): la orden se reconstruye y sus piezas pueden
  cambiar (AUD-42); los hallazgos se regeneran con las piezas nuevas. El plan lo verifica.
- **Sesiones en curso al desplegar** con la guía anterior: siguen con su guía (como en la
  spec 015).
- **Avance forzado de un paso de Log NC** (P-5).
- **Área sin BOM con paso de reparación rápida**: no se carga la guía (propuesta del Domain
  Model).
- **Guías ya cargadas en la BD del usuario:** `spTrainingGuideSync` agrega pasos al final y
  aborta si un paso existente cambia de `StepType` (`015-plan.md`). Los pasos nuevos van
  **en medio** (antes de Complete y Logout), así que Complete y Logout cambiarían de tipo en
  su posición. El plan define cómo se migra (versión nueva de la guía o un cambio acotado
  del sync) sin romper sesiones en curso ni el historial.

## Non-Functional Requirements

Sin cambios de volumen ni rendimiento. Los scripts `05`–`09` siguen siendo idempotentes.

## Integrations

Ninguna.

## Acceptance Criteria

**Parte A (sin pantallas nuevas)**
- **AC-1:** Given una orden Serialized, Then tiene entre 4 y 5 piezas.
- **AC-2:** Given Moldeo, LoFlo y Redefinity, Then cada código NC tiene su método; LoFlo y
  Redefinity tienen códigos de los tres métodos.
- **AC-3:** Given el paso de desecho rápido, Then la nota de hallazgo muestra la pieza, el
  defecto y la disposición, en Guided y en Assessment, sin nombrar el código.
- **AC-4:** Given Assessment, When registra el NC con la pieza y el código del hallazgo,
  Then el paso se resuelve y la pieza se desecha.
- **AC-5:** Given Assessment, When registra el NC sobre otra pieza, o sobre la pieza
  correcta y una más, Then es intento fallido con el mensaje genérico y no se desecha nada.
- **AC-6:** Given Assessment, When usa otro código (aunque sea `F_FS`), Then es intento
  fallido.
- **AC-7:** Given Assessment, When abre Log NC con otra pieza y sale con Return to POD, Then
  no cuenta como intento.
- **AC-8:** Given Guided, Then la guía señala la pieza del hallazgo y no deja enviar otra
  pieza ni otro código.
- **AC-9:** Given Moldeo, When elige `F_FR_GENERAL` o `F_FX_GENERAL` en el desecho, Then es
  intento fallido.
- **AC-10:** Given el 4.º intento fallido del desecho en Assessment, Then el paso queda
  fallido, la pieza del hallazgo se desecha y la sesión avanza.
- **AC-11:** Given `ScoreCalculatorTests` y los tests de `StepValidator`, Then siguen en verde
  y cubren las reglas nuevas de pieza y código.

**Parte B (con pantallas)**
- **AC-12:** Given LoFlo y Redefinity, Then sus guías tienen los tres pasos de Log NC en
  orden, con los pesos de la tabla, y suman 1.00.
- **AC-13:** Given la reparación rápida, When completa el flujo con la pieza, código,
  componente, Return/Scrap y "agregar" del hallazgo, Then el paso se resuelve y la pieza
  sigue en el POD.
- **AC-14:** Given la reparación rápida, When alguna de esas decisiones no coincide, Then en
  Assessment es intento fallido con el mensaje genérico.
- **AC-15:** Given la reparación extendida, When la pieza, el código y la operación destino
  coinciden, Then el paso se resuelve y la pieza sale del POD.
- **AC-16:** Given la reparación extendida, When la operación destino no coincide, Then en
  Assessment es intento fallido.
- **AC-17:** Given Complete después de los tres pasos, Then quedan al menos 2 piezas para
  completar.

## Deferred Decisions

| Decisión | Respaldo elegido | Se revisa cuando |
|---|---|---|
| Operaciones destino de la extendida (no hay `Operations` en uso) | Lista simulada por área marcada `SIM`, definida en la entrega de la parte B | Captura de la pantalla o catálogo real de Operations |
| Cantidad del split en NonSerialized (D-32) | No se califica | T5 |
| Tres métodos en Moldeo (D-30) | Solo desecho rápido | T5 |
| Pesos de Log NC y de DC (D-29) | 0.05 × 3 y DC 0.25 | T6 (documento de Ernesto) |
| Códigos simulados de reparación en LoFlo | `SIM_FR_…` y `SIM_FX_…` | Catálogo real de Sensores |

## Open Questions

_Ninguna._ Las capturas de los tres métodos son una entrada para la parte B, no una
pregunta abierta: si contradicen algo, se enmienda (punto 10).
