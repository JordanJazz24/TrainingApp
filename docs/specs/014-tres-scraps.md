# Spec 014: Tres tipos de scrap en Log NC y pieza defectuosa indicada

**Status:** Draft (2026-10-15)
**Date:** 2026-10-14 · revisada 2026-10-15 con las notas y capturas del usuario · **Approved:** —

Fuentes: roadmap → Fase 8 ("Revisión con SAP MES", "Revisión con Ernesto", "Validación con
técnicos de MES", "Retroalimentación de la trainer") y Fase 8.5, tarea 6 (T4); WI
QD001001524 rev. 27 (`docs/ui-reference/wi-produccion-disposables.md`, W13 y W19-W21);
notas del usuario y capturas del MES real en `docs/ui-reference/Screens SAP MES REAL/Split -
Log NC/` (2026-10-15), documentadas en `docs/ui-reference/general-flow.md` → "Paso 8".

## Problem

1. **Tres métodos de NC en uso, el simulador entrena uno.** El MES real tiene tres métodos
   (WI, W20): **desecho rápido** (`F_FS…`, desecha la unidad), **reparación rápida**
   (`F_FR…`, quita un componente y agrega otro; la unidad sigue en la operación) y
   **reparación rápida extendida** (`F_FX…`, retrabajo local o regreso a una operación
   anterior). Lo pidieron el ingeniero de SAP MES, Ernesto (que se obliguen los tres) y los
   técnicos de MES. Hoy Log NC acepta cualquier código del catálogo y siempre desecha.
2. **Scrapear las unidades equivocadas** es el error más grave según la trainer: el MES no lo
   detecta y les pasa también a veteranos. Hoy el simulador no dice cuál pieza está
   defectuosa: cualquier fila scrapeable es correcta.
3. Con 3-5 piezas por orden Serialized no alcanzan las piezas para los tres métodos y
   todavía dejar piezas para Complete.

## Decisiones del usuario

**2026-10-14**
- **D-27 · Alcance:** spec completa con las reglas y la calificación de los tres métodos. La
  UI de la reparación rápida y de la extendida se maqueta desde las capturas que aporta el
  usuario, sin inventarla (`docs/process.md` §8).
- **D-28 · Hallazgo con disposición:** el operador sabe qué pieza está defectuosa y qué hacer
  con ella por una **nota de hallazgo**, en los dos modos: la pieza, el defecto y la
  disposición (como la dictaría el MPI o el NCR). Se califica ejecutar el método correcto
  sobre la pieza correcta. En Guided, además, la guía señala la pieza.
- **D-29 · Tres pasos, 0.15 en total:** desecho rápido, reparación rápida y reparación
  extendida son **tres pasos** de la guía, en **orden fijo**, de **0.05** cada uno; Data
  Collection baja de 0.30 a 0.25.
- ~~D-30 · Solo Serialized~~: **reemplazada por D-40.** Sigue vigente su segunda parte: qué
  métodos entrena un área lo deciden **los pasos de su guía** (sin flag nuevo), así que si T5
  cambia las guías, no cambia el código.
- **D-31 · Código exacto:** el hallazgo describe un defecto que corresponde a **un código del
  catálogo**; cualquier otro código, aunque sea del método correcto, es un intento fallido.
- **D-32 · Lote:** en NonSerialized no se califica la cantidad del split. Se revisa con T5.

**2026-10-15** (notas y capturas del usuario)
- **D-33 · Desecho de una pieza:** el MES permite seleccionar varias filas, pero el hallazgo
  marca **una**; una pieza de más es intento fallido.
- **D-34 · Reparación rápida por un solo camino:** **Scrap** del componente y **Yes** a
  agregar el nuevo. En Assessment, **Return** o **No** son intento fallido. No se simula el
  re-ensamble (en el MES real, Return o No dejan la pieza sin el componente y hay que volver
  a ensamblarlo).
- **D-35 · Salidas y botón equivocado:** **No** en "Do you want to remove component…?" y
  **Cancel** en Remove component son salir sin responder (no cuentan); **Send to Routing**
  es botón equivocado (intento fallido, spec 007).
- **D-36 · Ventana del componente nuevo:** se construye **a partir de Assemble Components**
  (la técnica dice que es parecida, con diferencias; solo hay un recorte). La WI confirma que
  la reparación rápida reutiliza el modal de ensamble (W13).
- **D-37 · Flujo de la extendida:** Add-Done → mensaje **"NC logged"** en NC Data Entry →
  sub-modal **"Select Disposition Function for {SFC}"** con **Local Rework** y **Return to
  any previous** (botón Select en cada una, Cancel abajo) → se cierra todo y vuelve al POD →
  banner **"All of {SFC} will be processed at {operación}"**. "Select next step for {SFC}"
  **no** se simula.
- **D-37b · Disposición de la extendida:** el hallazgo dice cuál (Local Rework o Return to
  any previous); la otra es intento fallido.
- **D-38 · Comments de `F_FX_GENERAL`:** obligatorio como **formato** (vacío lo bloquea el
  control, sin intento); el contenido no se califica.
- **D-39 · Component con Browse en todos los códigos:** el ⧉ del campo Component abre un
  **Browse** (Component | Description, OK / Cancel) con los componentes **ensamblados en
  Assembly**. Solo se califica en la reparación rápida.
- **D-40 · Moldeo también con los tres** (reemplaza D-30).
- **D-41 · Componentes de Moldeo:** Moldeo no tiene Assembly ni BOM; qué muestra el Browse en
  su reparación rápida lo **verifica el usuario en el sandbox** (ver "Verificaciones
  pendientes"). Hasta entonces, la reparación rápida de Moldeo espera.
- **D-42 · Split en Moldeo:** **un solo paso SFC Split** que crea **tres SFC**, uno por
  método; después vienen los tres pasos de Log NC.
- **D-43 · Alerta del subpaso 1 de Log NC** (en rojo, texto aprobado):
  > Revise el SFC de cada pieza seleccionada contra el hallazgo: solo deben estar las
  > defectuosas. Scrapear la unidad equivocada es el error más grave de este paso: el MES no
  > lo detecta, corregirlo requiere al supervisor y a Calidad, y puede llevar a una acción
  > disciplinaria.
- **D-44 · Notas de la guía en la reparación rápida** (texto aprobado):
  - En Remove component: "Return aplica solo a ciertos componentes y según la operación (lo
    indica el MPI); no es común. En la mayoría de los casos se usa Scrap."
  - En "Do you want to add component…?": "Yes: agrega ahora el componente nuevo. No: la
    pieza queda sin el componente y hay que volver a ensamblarlo; lo mismo pasa con Return."
- **D-45 · Pesos de Moldeo:** la misma regla que en Serialized: tres pasos de 0.05 y Data
  Collection de 0.30 a 0.25.

## Propuestas de diseño

**Confirmadas por el usuario (2026-10-14)**
- **P-1 · Método en el catálogo:** `NCCodes` gana la columna `Method` (`FastScrap`,
  `FastRepair`, `ExtendedRepair`). LoFlo (`08`) y Redefinity (`05`) agregan 2-3 códigos
  simulados (`SIM`) de reparación rápida y 2-3 de extendida; en Moldeo (`06`),
  `F_FR_GENERAL` = `FastRepair` y `F_FX_GENERAL` = `ExtendedRepair`.
- **P-2 · Nota de hallazgo:** panel del simulador con el estilo de la guía (no imita una
  pantalla del MES), visible mientras el paso actual es de Log NC. Enumera los hallazgos en
  el orden de los pasos, cada uno con el SFC, el defecto (la descripción del código) y la
  disposición, sin nombrar el código.
- **P-3 · Se califica al enviar:** la pieza y el código se califican juntos al registrar el
  NC (con el resto de las decisiones del método). Abrir Log NC con otra pieza y salir con
  Return to POD no cuenta.
- **P-4 · Qué pasa con cada pieza:** el desecho la saca de la orden; la reparación rápida y
  el Local Rework la dejan en el POD (sigue a Complete); Return to any previous la saca del
  POD.
- **P-5 · Avance forzado** (4.º intento en Assessment): el paso queda fallido y el servidor
  aplica la disposición del hallazgo a su pieza.
- **P-6 · Hallazgo y código exacto en todas las áreas**, Moldeo incluido.
- **P-7 · Tipos de paso:** `LogNC` (6) = desecho rápido; nuevos `LogNCFastRepair` y
  `LogNCExtendedRepair`. Los tres se abren con el botón [Log NC]; la regla de secuencia (S2)
  acepta ese botón en cualquiera de los tres.
- ~~P-8 · Pesos de transición~~: descartada (llegaron las capturas de los tres métodos).

**Nuevas (Claude, 2026-10-15, a confirmar antes de aprobar)**
- **P-9 · Pieza correcta en NonSerialized:** cualquiera de los SFC del split **que no se
  haya usado** en otro método; el SFC del lote es intento fallido. El hallazgo del lote dice
  "N unidades con {defecto}" (N es informativo, D-32).
- **P-10 · SFC Split en NonSerialized:** el paso se resuelve cuando existen tantos SFC del
  split como pasos de Log NC tiene la guía (hoy 3). Los splits se hacen en el cliente y se
  envían en un solo postback (`CLAUDE.md` → "POD y controles").
- **P-11 · Datos de Remove component** (captura `remove compoenet modal .png`):
  - SFC, Material/Vers. y Description salen de la orden simulada; Status = `Active`.
  - Routing/Vers. y BOM/Version son simulados (`SIM`).
  - Actual Component/Vers. y su Description son el componente elegido en el Browse.
  - Qty Required y Qty Assembled son la Qty del BOM.
  - Assembly Operation es la operación simulada del área (P-14) y Assembly Resource es el
    recurso de Resource Setup.
  - Inventory SFC y Batch Number repiten lo que muestra hoy Assembly (Batch = Process Lot).
  - ★ Qty to Remove viene en 1 y es editable: obligatorio y numérico, como formato; no se
    califica.
  - Remove Operation y Remove Resource vienen precargados y solo se leen.
  - Ref Des va vacío.
- **P-12 · Ventana del componente nuevo (D-36):** reutiliza el modal de Assemble Components
  del simulador **con los mismos campos que muestra hoy Assembly**. Si W13 (Assembly Data
  por tipo de control del material) se implementa después, la reparación rápida hereda los
  campos correctos. El dato capturado es formato y no se califica (como el barcode de
  Assembly).
- **P-13 · `NCCodes.RequiresComments`:** qué códigos exigen Comments es dato del catálogo:
  `F_FX_GENERAL` en Moldeo, y un código `SIM_FX_…` en LoFlo y en Redefinity. El hallazgo de
  esos códigos trae un MPI y revisión simulados para escribir, sin calificarse (D-38). Nota
  de la guía: "Anote el MPI y su revisión, o el número de NCR."
- **P-14 · Operaciones del banner de la extendida:** no hay `Operations` en uso. Cada área
  tiene dos nombres de operación simulados (`SIM`): la actual y una anterior. Local Rework →
  el banner nombra la actual; Return to any previous → la anterior. Van en los scripts de
  catálogo de cada área (dónde exactamente, lo decide el plan).
- **P-15 · Cancel en "Select Disposition Function"** es salir sin responder: no cuenta y
  vuelve a NC Data Entry (como D-35).
- **P-16 · Complete en NonSerialized:** sigue pidiendo la fila del lote (decisión de la E3 de
  la spec 015); los SFC del split que quedan en el POD (reparación rápida, Local Rework) no
  se exigen.

## Scope

### In

**Común a los tres métodos**
1. **Piezas por orden Serialized: 4-5** (hoy 3-5). Reemplaza el rango de `docs/domain.md`
   → "Flujo estándar".
2. **Catálogo:** `Method` y `RequiresComments` en `NCCodes` (P-1, P-13), con los códigos
   simulados nuevos de LoFlo y Redefinity.
3. **Hallazgos generados por la BLL** al iniciar la orden (Start): uno por cada paso de Log NC
   de la guía. En Serialized, cada uno sobre una pieza **distinta** elegida al azar; en
   NonSerialized, sobre "N unidades" del lote (P-9). Cada uno lleva un código del método del
   paso, elegido al azar del catálogo del área, y según el método:
   - reparación rápida: el componente a reemplazar (del BOM);
   - extendida: la disposición (Local Rework o Return to any previous) y, si el código exige
     Comments, un MPI simulado.

   Se guardan en `PodState`, no en la BD de catálogo (son datos de escenario, como los
   equipos precargados de la spec 015).
4. **Nota de hallazgo** (P-2) en Guided y Assessment.
5. **Component con Browse** (D-39) en NC Data Entry, para todos los códigos. Obligatorio
   donde el área lo pide (`RequiresNCComponent`) y en la reparación rápida; opcional en la
   extendida.
6. **Guía de Guided:** alerta del subpaso 1 (D-43); la guía señala la pieza del hallazgo y no
   deja enviar otra pieza, otro código ni otra decisión (como D-25 de la spec 015); notas de
   D-44 y P-13.
7. **Orden fijo** de los pasos: desecho → reparación rápida → extendida. En NonSerialized,
   antes va **un** SFC Split con tres SFC (D-42, P-10).

**Desecho rápido** (paso `LogNC`; captura `seleccion de la orden o sfc y click en LOG NC.png`)
8. Flujo sin cambios: seleccionar la fila → [Log NC] → código `F_FS…` → NC Data Entry →
   Add-Done → banner "All of {SFC} scrapped".
9. **Correcto** solo con la pieza del hallazgo (Serialized, D-33) o un SFC del split sin usar
   (NonSerialized, P-9) y el código exacto (D-31). Lo demás, en Assessment, es intento
   fallido con el mensaje genérico (no revela qué falló).

**Reparación rápida** (paso `LogNCFastRepair`; capturas `browse component.png`, `mensaje
eliminar componente.png`, `remove compoenet modal .png`, `confirmacion agregar compoenente
.png` y `recorte de agregado del nuevo compoenente .png`)

10. Flujo:

    ```
    seleccionar la pieza → [Log NC] → código F_FR… → NC Data Entry
      → Component ⧉ → Browse (componentes ensamblados) → elegir → OK
      → Add-Done
      → Confirm "Do you want to remove component from {SFC}?"          Yes
      → Remove component (As-Built Configuration + Remove assembly details)
            [Scrap]  [Return]  [Send to Routing]  [Cancel]             Scrap
      → Confirm "Do you want to add component to {SFC}?"               Yes
      → ventana del componente nuevo (P-12) → datos → [Add]
      → vuelve al POD
    ```

11. **Correcto** solo si la pieza, el código y el componente coinciden con el hallazgo, se
    elige **Scrap** y se responde **Yes** a agregar (D-34). **Intento fallido:** Return, No a
    agregar y Send to Routing (D-35). **No cuentan:** No a remover, Cancel y la ✕ (salir sin
    responder). **Formato, sin intento:** Qty to Remove, datos del componente nuevo y
    campos obligatorios vacíos.
12. Todo el flujo se resuelve en el cliente y se envía en **un solo postback**: al dar Add
    en la ventana final o al elegir una opción que ya es una decisión equivocada (Return, No
    a agregar, Send to Routing).
13. La pieza sigue en el POD (P-4).

**Reparación extendida** (paso `LogNCExtendedRepair`; capturas `modal nc entry extended.png`
y `sub modal seleccion operaicon a ser devuelta .png`)

14. Flujo:

    ```
    seleccionar la pieza → [Log NC] → código F_FX… → NC Data Entry
      → Component (opcional) · Comments (★ si el código lo exige)
      → Add-Done → "NC logged" en NC Data Entry
      → "Select Disposition Function for {SFC}"
            Local Rework            [Select]
            Return to any previous  [Select]
            [Cancel]
      → se cierra todo → POD → banner "All of {SFC} will be processed at {operación}"
    ```

15. **Correcto** solo si la pieza, el código y la disposición coinciden con el hallazgo
    (D-37b). Comments vacío con un código que lo exige es formato (D-38). El componente no se
    califica. Cancel no cuenta (P-15).
16. Local Rework deja la pieza en el POD y Return to any previous la saca (P-4). El banner
    nombra la operación de P-14.

**Guías y pesos**
17. Las guías `05`, `07` y `09` agregan los dos pasos nuevos después del desecho, y `07`
    pasa a tener tres SFC en su SFC Split. Pesos:

```
Paso                         Moldeo (07)   LoFlo (09)   Redefinity (05, ejemplo)
Resource Setup                  0.20          0.20          0.20
Line Clearance (apertura)       0.05          0.05          0.05
Search / Start                  0.15          0.05          0.05
Assembly                         —            0.15          0.15
Data Collection                 0.25          0.25          0.25   (antes 0.30)
Line Clearance (cierre)         0.05          0.05          0.05
SFC Split                       0.05           —             —
Log NC · desecho rápido         0.05          0.05          0.05   (antes Log NC 0.10)
Log NC · reparación rápida      0.05          0.05          0.05   (nuevo)
Log NC · reparación extendida   0.05          0.05          0.05   (nuevo)
Complete                        0.05          0.05          0.05
Logout                          0.05          0.05          0.05
Total                           1.00          1.00          1.00
```

18. Si una captura nueva contradice esta spec, se enmienda antes del código.

### Out

- Calificar la cantidad del split en NonSerialized (D-32).
- Return y No a agregar como camino válido, y el re-ensamble posterior (D-34).
- "Select next step for {SFC}" y la lista de operaciones de la ruta (D-37).
- El flujo de Send to Routing (solo cuenta como botón equivocado).
- Código `F` (fallas de DC, DC-DC ESign Exceptions) y Unscrap.
- Calificar el contenido de Comments (D-38) y los datos del componente nuevo (P-12).
- Assembly Data por tipo de control del material (W13): va con su propia spec.
- Que la pieza regresada a una operación anterior reaparezca en otra operación.

## Domain Model

- **`NCCode.Method`** (enum nuevo `NCMethod`) y **`NCCode.RequiresComments`** (`bool`).
  Columnas `NOT NULL` en `NCCodes`; los scripts `05`, `06` y `08` las cargan.
- **`StepType`**: `LogNCFastRepair` y `LogNCExtendedRepair` nuevos (P-7). `LogNC` = desecho
  rápido. Cada tipo de Log NC acepta solo códigos de su método.
- **`NcFinding`** (BLL/Training, en `PodState`): paso, pieza (SFC en Serialized, "N
  unidades" en NonSerialized), código, y según el método: componente (reparación rápida),
  disposición y MPI simulado (extendida). Lo genera un generador nuevo (como
  `SerialSfcGenerator`).
- **`SerialSfcGenerator.MinPieces`** = 4.
- **Operaciones simuladas** por área (P-14), en datos de catálogo.
- **BOM de Moldeo:** según la verificación de D-41.
- `spTrainingGuideSync` ya valida que los pesos sumen 1.00. Propuesta para el plan: que
  rechace una guía con `LogNCFastRepair` en un área sin componentes.

## API Contract

Web Forms, sin endpoints HTTP.

- **`PodService.OpenLogNC`**: igual que hoy (secuencia y fila scrapeable); acepta el botón
  [Log NC] en los tres pasos. No califica la pieza (P-3).
- **Envío de cada método:** `PodService` recibe en un solo comando el código, las filas
  seleccionadas y las decisiones del flujo (componente, Scrap/Return, agregar,
  disposición). `StepValidator` lo compara todo con el `NcFinding` del paso. Si falla →
  `AttemptReason.WrongDecision`, con la acción del envío (spec 013). Si es correcto o forzado
  → aplica la disposición a la pieza (P-4, P-5) en la misma transacción
  (`spStepAttemptRecord`).
- **`PodContext`** expone los hallazgos (para la nota y la guía), el BOM ensamblado (para el
  Browse) y las operaciones simuladas (para el banner), solo lectura.

## Authorization

Sin cambios: el POD opera sobre la sesión propia y en curso; solo Operator entrena.

## Edge Cases & Failure Modes

- **La pieza del hallazgo ya no está en el POD:** no puede pasar con el orden fijo (los
  hallazgos son de piezas distintas y Complete va después). Si pasara, es un error de
  invariante (excepción), no un intento.
- **Apply to all SFCs con varias piezas seleccionadas:** intento fallido (D-33).
- **Selección parcial en Complete:** sigue siendo un aviso sin penalización (spec 008).
- **Session de ASP.NET perdida** (spec 009): la orden se reconstruye y sus piezas pueden
  cambiar (AUD-42); los hallazgos se regeneran con las piezas nuevas. El plan lo verifica.
- **Avance forzado de un paso de Log NC:** P-5. En la reparación rápida forzada, la pieza
  queda con el componente reemplazado.
- **Guías ya cargadas en la BD del usuario:** `spTrainingGuideSync` agrega pasos al final y
  aborta si un paso existente cambia de `StepType` (`015-plan.md`). Los pasos nuevos van en
  medio (antes de Complete y Logout). El plan define cómo se migra (versión nueva de la guía
  o un cambio acotado del sync) sin romper las sesiones en curso ni el historial.
- **Moldeo antes de la verificación de D-41:** su guía no puede tener la reparación rápida
  sin componentes. El plan decide si Moldeo entra después, con sus tres pasos juntos.

## Non-Functional Requirements

Sin cambios de volumen ni rendimiento. Los scripts `05`–`09` siguen siendo idempotentes.

## Integrations

Ninguna.

## Acceptance Criteria

**Común**
- **AC-1:** Given una orden Serialized, Then tiene entre 4 y 5 piezas.
- **AC-2:** Given Moldeo, LoFlo y Redefinity, Then cada código NC tiene su método y si exige
  Comments; LoFlo y Redefinity tienen códigos de los tres métodos.
- **AC-3:** Given un paso de Log NC, Then la nota de hallazgo muestra, en orden, la pieza, el
  defecto y la disposición de cada hallazgo, en Guided y en Assessment, sin nombrar el
  código.
- **AC-4:** Given Guided en el subpaso 1 de Log NC, Then se ve la alerta de D-43 en rojo.
- **AC-5:** Given cualquier código, When presiona ⧉ en Component, Then el Browse lista los
  componentes ensamblados en Assembly.
- **AC-6:** Given Assessment, When abre Log NC con otra pieza y sale con Return to POD, Then
  no cuenta como intento.
- **AC-7:** Given el 4.º intento fallido de un paso de Log NC en Assessment, Then el paso
  queda fallido, se aplica la disposición del hallazgo y la sesión avanza.
- **AC-8:** Given `ScoreCalculatorTests` y los tests de `StepValidator`, Then siguen en verde y
  cubren las reglas de pieza, código y decisiones de cada método.
- **AC-9:** Given las tres guías, Then tienen los pasos y los pesos de la tabla y suman 1.00.

**Desecho rápido**
- **AC-10:** Given Assessment, When registra el NC con la pieza (o un SFC del split sin usar)
  y el código del hallazgo, Then el paso se resuelve y la pieza se desecha.
- **AC-11:** Given Assessment, When registra el NC sobre otra pieza, sobre la correcta y una
  más, sobre el SFC del lote o con otro código (aunque sea `F_FS`), Then es intento fallido
  con el mensaje genérico y no se desecha nada.
- **AC-12:** Given Guided, Then la guía señala la pieza del hallazgo y no deja enviar otra
  pieza ni otro código.

**Reparación rápida**
- **AC-13:** Given la pieza, el código y el componente del hallazgo, When responde Yes,
  Scrap, Yes y completa la ventana del componente nuevo con Add, Then el paso se resuelve y
  la pieza sigue en el POD.
- **AC-14:** Given Assessment, When elige Return, responde No a agregar o presiona Send to
  Routing, Then es intento fallido con el mensaje genérico.
- **AC-15:** Given Assessment, When responde No a remover o presiona Cancel en Remove
  component, Then no cuenta y vuelve a NC Data Entry.
- **AC-16:** Given Assessment, When elige otro componente en el Browse, Then es intento
  fallido al enviar.
- **AC-17:** Given Guided, Then la guía muestra las notas de D-44 en Remove component y en la
  confirmación de agregar.

**Reparación extendida**
- **AC-18:** Given un código que exige Comments, When da Add-Done con Comments vacío, Then el
  control lo bloquea como formato y no cuenta.
- **AC-19:** Given Add-Done válido, Then aparece "NC logged" y "Select Disposition Function
  for {SFC}" con Local Rework y Return to any previous.
- **AC-20:** Given la disposición del hallazgo, When presiona su Select, Then se cierra todo,
  aparece "All of {SFC} will be processed at {operación}" y la pieza queda o sale del POD
  según la disposición.
- **AC-21:** Given Assessment, When elige la otra disposición, Then es intento fallido.
- **AC-22:** Given Cancel en Select Disposition Function, Then no cuenta.

**NonSerialized (Moldeo)**
- **AC-23:** Given Moldeo, Then su SFC Split se resuelve con tres SFC del split, y cada paso
  de Log NC se hace sobre uno distinto.
- **AC-24:** Given Complete en Moldeo, Then pide la fila del lote, como hoy.

## Verificaciones pendientes (el usuario, en el sandbox de MES)

| # | Qué | Bloquea |
|---|---|---|
| V-1 | Qué componentes muestra el Browse en una reparación rápida de **Moldeo** (D-41) | Reparación rápida de Moldeo |
| V-2 | Qué pasa al elegir **Local Rework** y **Return to any previous**: banner, operación que nombra y si la pieza sigue en el POD | El banner y P-4 de la extendida (se construye con P-14 y se ajusta) |
| V-3 | El mensaje o banner con el que termina la **reparación rápida** | El cierre de la reparación rápida |
| V-4 | Captura completa de la **ventana del componente nuevo** | Opcional: hoy se construye con D-36 y P-12 |

Para capturarlas se puede usar una sesión con Claude in Chrome o Playwright sobre el
sandbox, con la aprobación del usuario (es un sistema de la empresa y entra con sus
credenciales).

## Deferred Decisions

| Decisión | Respaldo elegido | Se revisa cuando |
|---|---|---|
| Cantidad del split en NonSerialized (D-32) | No se califica | T5 |
| Pesos de Log NC y de DC (D-29, D-45) | 0.05 × 3 y DC 0.25 | T6 (documento de Ernesto) |
| Códigos simulados de reparación en LoFlo | `SIM_FR_…` y `SIM_FX_…` | Catálogo real de Sensores |
| Operaciones del banner de la extendida (P-14) | Dos nombres simulados por área | V-2 o catálogo real de Operations |
| Assembly Data por tipo de material (W13) | Los campos que muestra hoy Assembly | Su propia spec |

## Open Questions

- Confirmar las propuestas nuevas **P-9 a P-16**.
- Las verificaciones V-1 a V-3 no bloquean la aprobación, pero sí las entregas que dependen
  de ellas.
