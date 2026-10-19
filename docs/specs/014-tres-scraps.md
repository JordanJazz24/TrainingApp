# Spec 014: Tres tipos de scrap en Log NC y pieza defectuosa indicada

**Status:** Approved (2026-10-16) · **Cerrada:** 2026-10-19 (E1-E3; E4 recortada, enmienda 6)
**Date:** 2026-10-14 · revisada 2026-10-15 con las notas y capturas del usuario · **Approved:** 2026-10-16 (usuario, con las propuestas P-1 a P-17 y D-27 a D-55)

> **Enmienda 2026-10-16 (usuario, captura `Split - Log NC/modal add componenet .png`):**
> afecta D-36, P-12, P-17, el punto 10 del Scope, AC-13, AC-26 y el texto de la guía del
> subpaso "Componente nuevo" (D-50).
>
> - La ventana del componente nuevo **no** es Assemble Components: es un modal propio,
>   **"Add Component"** (⤢ ✕), con la estructura de Remove component:
>   - **As-Built Configuration:** SFC, Material/Vers., Routing/Vers. | Status, Description,
>     BOM/Version.
>   - **Component Details:** ★ Component (con ⧉) y ★ Version, precargados con el componente
>     que se quitó; Description; Ref Des (opcional); Qty Required; Qty Assembled (`0`);
>     ★ Qty to Assemble, precargado con la Qty del BOM.
>   - **Add Assembly Details:** Assembly Operation y Assembly Resource (con ⧉, precargados) y
>     la parte que **cambia según el tipo de material** (W13). En la captura, un componente
>     por batch: ★ Batch Number **vacío, lo escribe el operador**.
>   - Botones: [Add] [Cancel]. **No tiene campo Barcode.**
> - **Cancel y ✕ no cuentan** (como D-35): vuelve a NC Data Entry y el flujo empieza de
>   nuevo.
> - **Component y Version** son editables pero no se califican: vacíos son formato, sin
>   intento. El componente que cuenta es el del Browse.
> - **Qty to Assemble:** obligatorio y numérico, como formato; no se califica.
> - **Add Assembly Details por material** (reemplaza la fila de P-12 y precisa P-17 para
>   este modal): `Batch` → ★ Batch Number que escribe el operador; `InventorySfc` →
>   ★ Inventory SFC que escribe el operador y ★ Batch Number en gris que llena el simulador;
>   `MaterialDocument` → ★ Material Document Number que escribe el operador. Todo es formato
>   (obligatorio, letras y números) y no se califica. En Assemble Components (Assembly),
>   P-17 no cambia: el batch del componente por batch lo llena el simulador al escanear.
> - **Sin E4 (W13 recortada):** Add Assembly Details muestra solo ★ Batch Number vacío, que
>   escribe el operador.
> - **Texto de la guía** (aprobado; reemplaza "Escanee el barcode del componente nuevo…"):
>   "Complete el **Batch Number** del componente nuevo (o los datos que pida su material) y
>   presione **Add**."

> **Enmienda 2 · 2026-10-16 (usuario, al probar la E1):** afecta P-9, D-49, AC-3 y los
> textos de la guía (D-50). D-32 no cambia.
>
> 1. **Moldeo: el hallazgo dicta la cantidad del split.** Era un bug: la nota decía N al azar
>    y el split usaba otra cantidad.
>    - N se sigue generando al dar Start, menor que el lote.
>    - En NonSerialized la nota de hallazgo se ve también durante el **SFC Split**, no solo en
>      Log NC (cambia D-49 y AC-3).
>    - Guía del subpaso de cantidad del split: "Ingrese la cantidad del hallazgo: **N**."
>      (reemplaza "por ejemplo 100").
>    - El split forzado y la reconstrucción usan N en lugar de `PodDefaults.DefaultSplitQty`.
>    - La cantidad sigue **sin calificarse** (D-32): si el operador divide otra cantidad, la
>      nota sigue diciendo N.
> 2. **Guided: un subpaso informativo que resalta la nota** (Info, con Continuar), el primero
>    del paso donde aparece la nota:
>    - **Serialized:** en cada paso de Log NC (cada uno tiene su hallazgo): "Calidad reportó
>      un hallazgo: revise el SFC, el defecto y la disposición. En este paso lo registra en el
>      MES."
>    - **Moldeo:** en el SFC Split, y no se repite en su Log NC: "Calidad reportó un hallazgo:
>      revise el defecto, la cantidad y la disposición. Primero separe esas unidades con SFC
>      Split; después las registra en Log NC."

> **Enmienda 3 · 2026-10-16 (usuario, al probar la E1 en Moldeo):** **la cantidad del split
> se califica** en Moldeo. Deja sin efecto D-32 y el "sin calificarse" de la enmienda 2;
> cambia el Out, las Deferred Decisions y AC-30 (nuevo).
>
> 1. En Guided y Assessment se valida que el split use la **cantidad N del hallazgo**. Se
>    califica al presionar **Split** (ya es un postback al servidor).
>    - **Assessment:** una cantidad distinta de N es **intento fallido** del paso SFC Split
>      (`WrongDecision`, mensaje genérico) y el split **no se aplica**: el POD no cambia y se
>      reintenta. Al 4.º intento el paso avanza forzado con un split de N.
>    - **Guided:** la guía no deja presionar Split con otra cantidad (como D-25).
>    - El Split con N sigue sin ser intento, y Close sigue igual (exige un split).
> 2. Después de un split correcto, otro Split es un **aviso de formato**, sin intento y sin
>    aplicarse: "Ya se separó la cantidad del hallazgo". Así, en Log NC queda una sola fila
>    del split.
> 3. El hallazgo nunca pide el lote completo: N va de 1 a lote − 1, y el split ya rechaza
>    dividir toda la fila (aviso `InvalidSplit`).

> **Enmienda 4 · 2026-10-19 (usuario, al probar la E2):** **el desecho rápido no lleva
> Component** en ningún área: "en serializado el fast scrap es el mismo que en no
> serializado". Cambia D-39, el punto 5 del Scope, AC-5 y los textos de la guía (D-50).
>
> 1. En el **desecho**, NC Data Entry **no muestra Component** (ni el ⧉), en LoFlo y
>    Redefinity igual que en Moldeo. La guía no tiene ese subpaso.
> 2. Component con Browse queda **solo en los métodos de reparación**: obligatorio en la
>    rápida (se califica, D-34) y opcional en la extendida. D-39 pasa de "todos los
>    códigos" a "los códigos de reparación".
> 3. `RequiresNCComponent` ya no decide si Component es obligatorio: lo decide el método del
>    código. No cambia ningún dato; si la columna queda sin uso, se anota como deuda al
>    cerrar la spec (quitarla no entra en esta spec).
> 4. **Guía del Browse** (reparación rápida), texto nuevo: "Seleccione el componente a
>    reemplazar y dele **OK**." El resaltado va primero en las filas y pasa a OK cuando se
>    selecciona la fila del componente del hallazgo.

> **Enmienda 5 · 2026-10-19 (usuario, con la aclaración de la sesión de contraste con las
> WI, W20/W21):** **las reparaciones usan los códigos generales reales**. En el MES real,
> `F_FS_{DEFECTO}` tiene un código por cada defecto posible y todos son desecho rápido;
> `F_FR_GENERAL` y `F_FX_GENERAL` son genéricos y existen en todas las áreas serializadas.
> El método no depende del defecto: lo decide el código que elige el operador. Cambia P-1,
> D-31, P-13, AC-2, los textos de la guía (D-50) y las Deferred Decisions.
>
> 1. **Catálogo:** LoFlo (`08`) y Redefinity (`05`) tienen como códigos de reparación
>    `F_FR_GENERAL` (`FastRepair`, sin Comments) y `F_FX_GENERAL` (`ExtendedRepair`, con
>    Comments obligatorio), con el código y la descripción reales de Moldeo (`06`), sin
>    prefijo `SIM`. Los de desecho siguen siendo simulados (`SIM_FS_…`).
> 2. **Se borran** los códigos de reparación simulados (`SIM_FR_…` y `SIM_FX_…`, incluido
>    `SIM_FX_GENERAL`) de `05` y `08`. Es una excepción a "el script nunca borra filas", como
>    D-23: nada referencia `NCCodes`. Antes de correr los scripts, el usuario abandona las
>    sesiones en curso de LoFlo y Redefinity (su hallazgo guarda el código borrado).
> 3. **Defecto de la nota:** en las reparaciones, el hallazgo toma al azar la descripción de
>    un código **de desecho** del área. Calidad encuentra un defecto y dispone qué hacer con
>    él. En el desecho no cambia nada: el defecto es el del código.
> 4. **Calificación (D-31):** en las reparaciones, el código correcto es el general del
>    método del paso. Cualquier otro (un `F_FS`/`SIM_FS` o el general del otro método) es
>    intento fallido, como hoy.
> 5. **Comments:** sin cambios. Solo `F_FX_GENERAL` lo exige (con el MPI simulado del
>    hallazgo, P-13); `F_FR_GENERAL` no.
> 6. **Guía del subpaso del código** (nombra el código real): reparación rápida: "Presione
>    **Log NC** en el código general de reparación rápida: **F_FR_GENERAL**."; extendida:
>    "…en el código general de reparación extendida: **F_FX_GENERAL**." El desecho no cambia.
> 7. **Términos:** solo el desecho es "scrap". La rápida y la extendida son reparaciones, así
>    que en la documentación nueva se habla de "**tres métodos de Log NC**". El título de esta
>    spec queda como registro histórico. La familia `F` a secas (fallas de DC, DC-DC ESign
>    Exceptions) no es un método de Log NC y queda fuera de esta spec.

> **Enmienda 6 · 2026-10-19 (usuario, al cerrar la E3): se recorta la E4** (Assembly Data
> por tipo de material, W13). Deja en suspenso D-46, P-17, el punto 8a del Scope y AC-25 a
> AC-29; cambia el Out y las Deferred Decisions.
>
> - **Por qué:** tal como estaba diseñada, nada se califica (los campos son formato), así que
>   no entrena el error real: saber qué dato de la etiqueta del componente escanear según
>   su control. Ningún reporte de la trainer ni de técnicos la pidió, y compite con la 016
>   y la 012.
> - **Queda como está:** Assemble Components llena el Batch Number con el Process Lot de la
>   orden. Add Component muestra solo ★ Batch Number vacío, que escribe el operador (la regla
>   "Sin E4" de la enmienda 1 queda definitiva). Remove component no cambia.
> - **Se retoma** si el documento de la prueba práctica (T6), la trainer o el sandbox de SAP
>   MES (T9) muestran errores con el Inventory SFC o el Material Document. En ese caso, con
>   otra spec: etiquetas simuladas del componente y el dato calificado.

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
- **D-30 · Solo áreas con Assembly** (vuelve a regir por D-53; D-40 la había reemplazado):
  qué métodos entrena un área lo deciden **los pasos de su guía** (sin flag nuevo), así que
  si T5 cambia las guías, no cambia el código.
- **D-31 · Código exacto:** el hallazgo describe un defecto que corresponde a **un código del
  catálogo**; cualquier otro código, aunque sea del método correcto, es un intento fallido.
  *(Enmienda 5, 2026-10-19: en las reparaciones el código correcto es el general del método,
  y el defecto de la nota sale de un código de desecho del área.)*
- ~~D-32 · Lote: en NonSerialized no se califica la cantidad del split~~: **reemplazada por
  la enmienda 3 (2026-10-16)**: la cantidad del split se califica contra la del hallazgo.

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
  Assembly**. Solo se califica en la reparación rápida. *(Enmienda 4, 2026-10-19: solo en
  los códigos de reparación; el desecho no muestra Component.)*
- ~~D-40 · Moldeo también con los tres~~: **revertida por D-53.**
- ~~D-41 · Componentes de Moldeo~~: **resuelta por D-53** (V-1).
- ~~D-42 · Split en Moldeo con tres SFC~~: **sin efecto por D-53.**
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
- ~~D-45 · Pesos de Moldeo~~: **sin efecto por D-53** (Moldeo no cambia de pesos).
- **D-46 · W13 completo en la 014:** cada componente del BOM tiene su **tipo de control del
  material**, y Assembly Data muestra sus campos en **Assemble Components**, en la ventana
  del componente nuevo de la reparación rápida y en Remove component (respuesta a P-12).
  Assembly no cambia de puntaje.
- **D-47 · Pieza reparada:** tras la reparación rápida o el Local Rework, la pieza queda
  **activa** y sigue a Complete (confirma P-4); no se repite Data Collection ni nada más.
- **D-48 · Add y Done** en NC Data Entry de los tres métodos siguen siendo botón equivocado
  (spec 007): Add-Done es el único camino.
- **D-49 · Nota de hallazgo de a uno:** el panel muestra **solo el hallazgo del paso
  actual**, como "Hallazgo n de 3" (reemplaza "enumera los hallazgos" de P-2):
  ```
  ┌─ Hallazgo 2 de 3 ─────────────────────────┐
  │ SFC 000123-048                             │
  │ Defecto: Conector dañado                   │
  │ Disposición: reemplazar el componente      │
  │ 454000132321 (se desecha)                  │
  └────────────────────────────────────────────┘
  ```
- **D-51 · La extendida sigue la captura, no la WI:** la WI (respuesta de la sesión de
  contraste, 2026-10-15) muestra "Select next step for {SFC}" con la lista de operaciones
  justo después de "NC logged" y no tiene "Select Disposition Function" ni "Local Rework".
  El usuario mantiene D-37 (su captura del sandbox): la WI queda como otra configuración de
  operación.
- **D-52 · Nombres de operación con formato real** (P-14): número + código + descripción,
  p. ej. `020 O_LOFLO_SIM_ENSAMBLE` · "Ensamble (SIM)"; el banner nombra el código.
- **D-53 · V-1 resuelta: Moldeo solo con desecho rápido** (usuario, 2026-10-15): la
  operación de Moldeo no lleva Assembly, así que **se omiten la reparación rápida y la
  extendida**. Moldeo sigue con SFC Split (un SFC, como hoy) + desecho rápido, con nota de
  hallazgo y código exacto (P-6) y sus pesos actuales. `F_FR_GENERAL` y `F_FX_GENERAL`
  siguen en su NC Selection (en el MES real aparecen) con su método, pero nunca son
  correctos para el desecho.
- **D-54 · V-2 resuelta para Return to any previous** (usuario, 2026-10-15): se cierra todo,
  vuelve al POD con el banner **"All of {SFC} will be processed at {operación anterior}"** y
  la pieza **sale del POD**. **Local Rework** muestra otro banner, con la operación actual;
  su texto exacto queda por verificar (V-5). Mientras tanto: el mismo formato con la
  operación actual, marcado **provisional**. La pieza sigue en el POD (P-4, D-47).
- **D-55 · V-3, banner provisional:** no se sabe con qué mensaje termina la reparación
  rápida. Mientras tanto: un banner verde **provisional** "Component {componente} replaced
  on {SFC}", marcado como provisional en el código y en `general-flow.md` (como el de
  Complete en la spec 015). Se cambia cuando se sepa el real.
- **D-50 · Textos de la guía de los subpasos nuevos** (Browse, Remove component, ventana del
  componente nuevo, disposición): los propone el plan y el usuario los aprueba antes de
  implementar, como en T7.

## Propuestas de diseño

**Confirmadas por el usuario (2026-10-14)**
- **P-1 · Método en el catálogo:** `NCCodes` gana la columna `Method` (`FastScrap`,
  `FastRepair`, `ExtendedRepair`). LoFlo (`08`) y Redefinity (`05`) agregan 2-3 códigos
  simulados (`SIM`) de reparación rápida y 2-3 de extendida; en Moldeo (`06`),
  `F_FR_GENERAL` = `FastRepair` y `F_FX_GENERAL` = `ExtendedRepair`. *(Enmienda 5,
  2026-10-19: LoFlo y Redefinity usan también `F_FR_GENERAL` y `F_FX_GENERAL`; sin códigos
  `SIM` de reparación.)*
- **P-2 · Nota de hallazgo:** panel del simulador con el estilo de la guía (no imita una
  pantalla del MES), visible mientras el paso actual es de Log NC, con el SFC, el defecto
  (la descripción del código) y la disposición, sin nombrar el código. Uno a la vez: D-49.
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

**Confirmadas por el usuario (2026-10-15)**; P-12 la cambió D-46
- **P-9 · Pieza correcta en NonSerialized** (ajustada por D-53): el SFC del split; el SFC
  del lote es intento fallido. El hallazgo del lote dice "N unidades con {defecto}" (N se
  califica en el SFC Split desde la enmienda 3; antes era informativo, D-32).
- ~~P-10 · SFC Split con varios SFC~~: **sin efecto por D-53**; el split no cambia.
- **P-11 · Datos de Remove component** (captura `remove compoenet modal .png`):
  - SFC, Material/Vers. y Description salen de la orden simulada; Status = `Active`.
  - Routing/Vers. y BOM/Version son simulados (`SIM`).
  - Actual Component/Vers. y su Description son el componente elegido en el Browse.
  - Qty Required y Qty Assembled son la Qty del BOM.
  - Assembly Operation es la operación simulada del área (P-14) y Assembly Resource es el
    recurso de Resource Setup.
  - Inventory SFC y Batch Number repiten la Assembly Data que se registró en Assembly para
    ese componente (D-46, P-17).
  - ★ Qty to Remove viene en 1 y es editable: obligatorio y numérico, como formato; no se
    califica.
  - Remove Operation y Remove Resource vienen precargados y solo se leen.
  - Ref Des va vacío.
- **P-12 · Ventana del componente nuevo (D-36):** reutiliza el modal de Assemble Components
  del simulador, con la Assembly Data **según el tipo de control del material** del
  componente (D-46, P-17). El dato capturado es formato y no se califica (como el barcode de
  Assembly).
- **P-13 · `NCCodes.RequiresComments`:** qué códigos exigen Comments es dato del catálogo:
  un código `SIM_FX_…` en LoFlo y en Redefinity (y `F_FX_GENERAL` en Moldeo, aunque Moldeo
  no entrena la extendida). *(Enmienda 5: `F_FX_GENERAL` en las tres áreas.)* El hallazgo de
  esos códigos trae un MPI y revisión simulados para escribir, sin calificarse (D-38). Nota
  de la guía: "Anote el MPI y su revisión, o el número de NCR."
- **P-14 · Operaciones del banner de la extendida:** no hay `Operations` en uso. Cada área
  tiene dos nombres de operación simulados (`SIM`), con formato real (D-52): la actual y una
  anterior. Local Rework →
  el banner nombra la actual; Return to any previous → la anterior. Van en los scripts de
  catálogo de cada área (dónde exactamente, lo decide el plan).
- **P-15 · Cancel en "Select Disposition Function"** es salir sin responder: no cuenta y
  vuelve a NC Data Entry (como D-35).
- **P-16 · Complete en NonSerialized:** sigue pidiendo la fila del lote, como hoy (decisión
  de la E3 de la spec 015).

**Confirmada por el usuario (2026-10-15)**
- **P-17 · Assembly Data por tipo de material (W13):** `AssemblyComponents` gana
  `MaterialControl` (`Batch`, `InventorySfc`, `MaterialDocument`), simulado. Después del
  escaneo del barcode (que no cambia), Assembly Data muestra:

  | Control | Campos | Quién llena qué |
  |---|---|---|
  | `Batch` | ★ Batch Number | El simulador, al escanear: un batch simulado **del componente** (deja de ser el Process Lot de la orden) |
  | `InventorySfc` | ★ Inventory SFC + ★ Batch Number (gris) | El operador escanea o escribe el Inventory SFC (formato: obligatorio, letras y números); el simulador llena el Batch |
  | `MaterialDocument` | ★ Material Document Number | El operador lo escribe (formato: obligatorio, letras y números) |

  Nada se califica (Assembly sigue calificándose por secuencia). El BOM simulado de LoFlo y
  de Redefinity tiene **al menos un componente de cada tipo**, para que se practiquen los
  tres. Moldeo no tiene BOM (D-53).

## Scope

### In

**Común a los tres métodos**
1. **Piezas por orden Serialized: 4-5** (hoy 3-5). Reemplaza el rango de `docs/domain.md`
   → "Flujo estándar".
2. **Catálogo:** `Method` y `RequiresComments` en `NCCodes` (P-1, P-13), con los códigos
   simulados nuevos de LoFlo y Redefinity.
3. **Hallazgos generados por la BLL** al iniciar la orden (Start): uno por cada paso de Log NC
   de la guía. En Serialized, cada uno sobre una pieza **distinta** elegida al azar; en
   NonSerialized (Moldeo, un solo hallazgo), sobre "N unidades" del lote (P-9). Cada uno lleva un código del método del
   paso, elegido al azar del catálogo del área, y según el método:
   - reparación rápida: el componente a reemplazar (del BOM);
   - extendida: la disposición (Local Rework o Return to any previous) y, si el código exige
     Comments, un MPI simulado.

   Se guardan en `PodState`, no en la BD de catálogo (son datos de escenario, como los
   equipos precargados de la spec 015).
4. **Nota de hallazgo** (P-2) en Guided y Assessment.
5. **Component con Browse** (D-39) en NC Data Entry, para todos los códigos. Obligatorio
   donde el área lo pide (`RequiresNCComponent`) y en la reparación rápida; opcional en la
   extendida. *(Enmienda 4, 2026-10-19: solo en los códigos de reparación, obligatorio en
   la rápida y opcional en la extendida; el desecho no lo muestra.)*
6. **Guía de Guided:** alerta del subpaso 1 (D-43); la guía señala la pieza del hallazgo y no
   deja enviar otra pieza, otro código ni otra decisión (como D-25 de la spec 015); notas de
   D-44 y P-13.
7. **Orden fijo** de los pasos: desecho → reparación rápida → extendida. Moldeo solo tiene
   el desecho, después de su SFC Split como hoy (D-53).

**Assembly Data por tipo de material** (D-46, P-17; W13 de la WI)
8a. Assemble Components (paso Assembly) y la ventana del componente nuevo (reparación
    rápida) muestran la Assembly Data según el `MaterialControl` del componente. Sin cambios
    de puntaje ni de secuencia en Assembly.

**Desecho rápido** (paso `LogNC`; captura `seleccion de la orden o sfc y click en LOG NC.png`)
8. Flujo sin cambios: seleccionar la fila → [Log NC] → código `F_FS…` → NC Data Entry →
   Add-Done → banner "All of {SFC} scrapped".
9. **Correcto** solo con la pieza del hallazgo (Serialized, D-33) o el SFC del split
   (NonSerialized, P-9) y el código exacto (D-31). Lo demás, en Assessment, es intento
   fallido con el mensaje genérico (no revela qué falló).

**Reparación rápida** (paso `LogNCFastRepair`, solo LoFlo y Redefinity; capturas `browse component.png`, `mensaje
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
      → Add Component (enmienda 2026-10-16) → datos → [Add]
      → vuelve al POD → banner provisional (D-55)
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

**Reparación extendida** (paso `LogNCExtendedRepair`, solo LoFlo y Redefinity; capturas `modal nc entry extended.png`
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
16. Return to any previous: banner con la operación anterior y la pieza sale del POD
    (D-54). Local Rework: banner provisional con la operación actual y la pieza sigue en el
    POD (D-54, V-5). Las operaciones son las de P-14.

**Guías y pesos**
17. Las guías `05` y `09` agregan los dos pasos nuevos después del desecho. La `07` (Moldeo)
    no cambia de pasos ni de pesos (D-53). Pesos:

```
Paso                         Moldeo (07, sin cambios)   LoFlo (09)   Redefinity (05, ejemplo)
Resource Setup                  0.20                       0.20          0.20
Line Clearance (apertura)       0.05                       0.05          0.05
Search / Start                  0.15                       0.05          0.05
Assembly                         —                         0.15          0.15
Data Collection                 0.30                       0.25          0.25   (antes 0.30)
Line Clearance (cierre)         0.05                       0.05          0.05
SFC Split                       0.05                        —             —
Log NC · desecho rápido         0.10                       0.05          0.05   (antes 0.10)
Log NC · reparación rápida       —                         0.05          0.05   (nuevo)
Log NC · reparación extendida    —                         0.05          0.05   (nuevo)
Complete                        0.05                       0.05          0.05
Logout                          0.05                       0.05          0.05
Total                           1.00                       1.00          1.00
```

18. Si una captura nueva contradice esta spec, se enmienda antes del código.

### Out

- Reparación rápida y extendida en Moldeo (D-53).
- Return y No a agregar como camino válido, y el re-ensamble posterior (D-34).
- "Select next step for {SFC}" y la lista de operaciones de la ruta (D-37).
- El flujo de Send to Routing (solo cuenta como botón equivocado).
- Código `F` (fallas de DC, DC-DC ESign Exceptions) y Unscrap.
- Calificar el contenido de Comments (D-38), los datos del componente nuevo (P-12) y la
  Assembly Data (P-17).
- Que la pieza regresada a una operación anterior reaparezca en otra operación.
- Assembly Data por tipo de material (W13, D-46, P-17, AC-25 a AC-29): recortada por la
  enmienda 6 (2026-10-19).

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
- **`AssemblyComponent.MaterialControl`** (enum nuevo, P-17), columna `NOT NULL` en
  `AssemblyComponents`; los scripts `05` y `08` la cargan (Moldeo no tiene BOM). Lo
  registrado en Assembly por componente se guarda en `PodState` (lo lee Remove component).
- **Operaciones simuladas** por área (P-14), en datos de catálogo.
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

## Non-Functional Requirements

Sin cambios de volumen ni rendimiento. Los scripts `05`–`09` siguen siendo idempotentes.

## Integrations

Ninguna.

## Acceptance Criteria

**Común**
- **AC-1:** Given una orden Serialized, Then tiene entre 4 y 5 piezas.
- **AC-2:** Given Moldeo, LoFlo y Redefinity, Then cada código NC tiene su método y si exige
  Comments; LoFlo y Redefinity tienen códigos de los tres métodos (enmienda 5: los de
  reparación son `F_FR_GENERAL` y `F_FX_GENERAL`).
- **AC-3** (enmienda 2 del 2026-10-16: en NonSerialized, también en el SFC Split): Given un paso de Log NC, Then la nota de hallazgo muestra solo el hallazgo de ese
  paso ("Hallazgo n de 3"; en Moldeo, "1 de 1"): la pieza, el defecto y la disposición, en Guided y en
  Assessment, sin nombrar el código.
- **AC-4:** Given Guided en el subpaso 1 de Log NC, Then se ve la alerta de D-43 en rojo.
- **AC-5** (enmienda 4 del 2026-10-19: solo códigos de reparación; el desecho no muestra Component): Given un código de reparación, When presiona ⧉ en Component, Then el Browse lista los
  componentes ensamblados en Assembly.
- **AC-6:** Given Assessment, When abre Log NC con otra pieza y sale con Return to POD, Then
  no cuenta como intento.
- **AC-7:** Given el 4.º intento fallido de un paso de Log NC en Assessment, Then el paso
  queda fallido, se aplica la disposición del hallazgo y la sesión avanza.
- **AC-8:** Given `ScoreCalculatorTests` y los tests de `StepValidator`, Then siguen en verde y
  cubren las reglas de pieza, código y decisiones de cada método.
- **AC-9:** Given las guías de LoFlo y Redefinity, Then tienen los pasos y los pesos de la
  tabla y suman 1.00; la de Moldeo no cambia.

**Desecho rápido**
- **AC-10:** Given Assessment, When registra el NC con la pieza (o el SFC del split)
  y el código del hallazgo, Then el paso se resuelve y la pieza se desecha.
- **AC-11:** Given Assessment, When registra el NC sobre otra pieza, sobre la correcta y una
  más, sobre el SFC del lote o con otro código (aunque sea `F_FS`), Then es intento fallido
  con el mensaje genérico y no se desecha nada.
- **AC-12:** Given Guided, Then la guía señala la pieza del hallazgo y no deja enviar otra
  pieza ni otro código.

**Reparación rápida**
- **AC-13:** Given la pieza, el código y el componente del hallazgo, When responde Yes,
  Scrap, Yes y completa la ventana del componente nuevo con Add, Then el paso se resuelve,
  aparece el banner provisional de D-55 y la pieza sigue en el POD.
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
  aparece el banner de D-54 (operación anterior con Return to any previous y la pieza sale
  del POD; operación actual, provisional, con Local Rework y la pieza sigue).
- **AC-21:** Given Assessment, When elige la otra disposición, Then es intento fallido.
- **AC-22:** Given Cancel en Select Disposition Function, Then no cuenta.

**Assembly Data (W13)**
- **AC-25:** Given un componente `Batch`, When escanea el barcode, Then ★ Batch Number se
  llena con el batch simulado del componente (no con el Process Lot).
- **AC-26:** Given un componente `InventorySfc`, When escanea y deja el Inventory SFC vacío,
  Then el control lo bloquea como formato; con un valor, el Batch se llena solo.
- **AC-27:** Given un componente `MaterialDocument`, Then pide ★ Material Document Number.
- **AC-28:** Given LoFlo y Redefinity, Then su BOM tiene al menos un componente de cada tipo.
- **AC-29:** Given Remove component, Then Inventory SFC y Batch Number muestran lo
  registrado en Assembly para ese componente.

**NonSerialized (Moldeo)**
- **AC-23:** Given Moldeo, Then su guía solo tiene el desecho rápido, con hallazgo y código
  exacto; elegir `F_FR_GENERAL` o `F_FX_GENERAL` es intento fallido.
- **AC-24:** Given Complete en Moldeo, Then pide la fila del lote, como hoy.
- **AC-30** (enmienda 3): Given Moldeo, When presiona Split con una cantidad distinta de la
  del hallazgo, Then en Assessment es intento fallido y el POD no cambia; en Guided la guía
  no lo deja. When presiona Split otra vez después de un split correcto, Then es un aviso
  de formato y no se aplica.

## Verificaciones pendientes (el usuario, en el sandbox de MES)

| # | Qué | Bloquea |
|---|---|---|
| ~~V-1~~ | ~~Componentes de Moldeo~~: **resuelta** (D-53, Moldeo no lleva Assembly) | — |
| ~~V-2~~ | ~~Return to any previous~~: **resuelta** (D-54) | — |
| V-3 | El mensaje o banner con el que termina la **reparación rápida** | Nada: banner provisional mientras tanto (D-55) |
| V-5 | El banner exacto de **Local Rework** | Nada: banner provisional mientras tanto (D-54) |
| V-4 | Captura completa de la **ventana del componente nuevo** | Opcional: hoy se construye con D-36 y P-12 |

Para capturarlas se puede usar una sesión con Claude in Chrome o Playwright sobre el
sandbox, con la aprobación del usuario (es un sistema de la empresa y entra con sus
credenciales).

## Deferred Decisions

| Decisión | Respaldo elegido | Se revisa cuando |
|---|---|---|
| Pesos de Log NC y de DC (D-29) | 0.05 × 3 y DC 0.25 | T6 (documento de Ernesto) |
| ~~Códigos simulados de reparación en LoFlo~~ | Resuelta por la enmienda 5: `F_FR_GENERAL` y `F_FX_GENERAL` reales | — |
| Operaciones del banner de la extendida (P-14) | Dos nombres simulados por área | V-2 o catálogo real de Operations |
| Assembly Data por tipo de material (W13, D-46; enmienda 6) | Batch Number = Process Lot en Assemble Components; Add Component solo con ★ Batch Number | T6, la trainer o T9 muestran errores con Inventory SFC o Material Document (otra spec, con etiquetas simuladas y el dato calificado) |

## Open Questions

_Ninguna._ V-3, V-4 y V-5 no bloquean nada: hay respaldo provisional para cada una.
