# Plan — Spec 017: Mensajes de error del MES

**Spec:** `017-mensajes-del-mes.md` (aprobada el 2026-10-21, D-92 a D-116) · **Estado:**
**aprobado por el usuario el 2026-10-21**, con D-117 a D-119 ("Puntos confirmados").
**Autoría:** sesión de Arquitectura y Planeación, verificado contra el código en
`cd3cb78`. Lo ejecuta Desarrollo (`docs/process.md` → "Tres sesiones de trabajo").

> **Enmienda 1 del plan (2026-10-21, Desarrollo durante la E1, aceptada por Arquitectura y
> Planeación; no cambia la spec ni D-92):**
>
> - **Banner B:** es un elemento aparte (`#podMesError`), no una variante de
>   `PnlBannerZone`. Lo llena una función JS de `Training.Master`, porque también lo usan los
>   chequeos del cliente (DC y Split sin fila → `12906`; escaneo ajeno en Start → `22507`), y
>   en la E2 admite varias filas (M-7, D-119). Al mostrarse oculta el banner de éxito: no se
>   apilan. Los chequeos del cliente toman el texto del catálogo `MesMessage` renderizado por
>   el servidor (como `ScanNoticeTextJs`), sin copias en el JS, y el servidor sigue validando
>   lo mismo.
> - **Ancla:** `data-guide="pod-mes-error"`, no `pod-banner`. Los subpasos `banner`
>   (`hide: true`) de la guía se le pegarían. En la E4 la nota del banner se ancla a
>   `pod-mes-error`.
> - **Split, por casos:** la Qty llega como decimal a la BLL. Vacía → M-14, ≤ 0 → M-16, mayor
>   a la disponible → M-15 (también un número enorme). Una Qty no entera es el aviso del
>   simulador `InvalidSplitQty` (SweetAlert, sin intento), porque el MES no tiene texto
>   capturado para ese caso.
> - **"La siguiente acción" (D-112)** incluye elegir una fila del POD: quita el banner de
>   error.
>
> **Enmienda 2 del plan (2026-10-21, Arquitectura y Planeación, tras la optimización de la
> documentación; no cambia la spec ni el comportamiento):** el cierre de la E4 se ajusta a
> los documentos nuevos. "Deuda y riesgos" y "Estado actual" pasan a `docs/status.md` y
> "Historial" a `docs/history.md` (los escribe Desarrollo). La fila 10 sigue en
> `docs/roadmap.md`: Desarrollo avisa y Arquitectura y Planeación la marca ✅. Las D-n de
> la 017 ya están en `docs/decisions.md`; al cerrar, se actualiza el estado de las que
> cambien (ej. D-111 provisional). Además, la E4 actualiza `docs/ui-reference/general-flow.md`
> → "Reglas de la pantalla" de Search/Start, Assembly, Data Collection, SFC Split y Log NC
> con los mensajes del MES que reemplazan a los SweetAlert (hoy dice, por ejemplo, que la
> Qty vacía del Split se avisa con SweetAlert), y la de Resource Setup con Material.
>
> **Enmienda 3 del plan (2026-10-21, Desarrollo durante la E2, aceptada por Arquitectura y
> Planeación; no cambia la spec ni D-92):** no se agrega `PodState.LineClearanceAnsweredNo`.
> El "No" del LC de apertura (C-3, M-3) se deduce de los **intentos de la sesión**: algún
> intento del primer `LineClearance` de la guía con `Reason = WrongDecision`. Los clics fuera
> de secuencia no cuentan, y el Sí resuelve el paso. La marca en `PodState` se perdía al
> reconstruir el estado (D-10) y daba M-2 en vez de M-3; desde los intentos, sobrevive. El
> estado que lee la regla se arma con una función pura (`MesSequenceState.Build`) con sus
> tests, y los catálogos de DC solo se leen para Complete (M-7).
>
> **Enmienda 4 del plan (2026-10-21, correcciones del usuario al probar la E2, `7b27967`; no
> cambia la spec ni D-92):**
>
> - **"SFC iniciado"** = el paso de Start está resuelto **y** hay filas en `Active`. En
>   NonSerialized, la fila del lote está `Active` desde Resource Setup, así que Complete
>   antes del Start daba `15246` en vez de `22506`.
> - **Sin ninguna fila seleccionada, el MES no tiene SFC:** fuera de turno, Complete da M-4,
>   y Assemble, DC Collect, SFC Split y Log NC dan M-5. El usuario lo pidió para Complete y
>   Desarrollo lo extendió a los demás, con la misma lógica de C-1.
> - **M-7 (D-119, enmendada)** es uno por pieza **seleccionada**. Sin selección ya no se
>   lista uno por fila activa: sale M-4.
> - **Serialized, Start sin ninguna pieza:** el banner `22506` **y** el SweetAlert
>   `StartWholeOrder` del simulador, los dos (pedido del usuario; ajusta D-103 para este
>   caso).
>
> **Enmienda 5 del plan (2026-10-21, consulta de Desarrollo durante la E3, decisión del
> usuario D-120; no cambia la spec ni D-92):** **M-17 (`18117`) queda fuera**, como M-18
> (D-118). En el simulador no se puede provocar: NC Data Entry solo se abre al elegir un
> código en NC Selection (`BtnLogNCCode_Click`) y `TxtNCCode` es `ReadOnly`, así que Done
> siempre tiene un código. Done con código sigue con el feedback de hoy (D-110, enmendada).
> Abrir NC Data Entry vacío, como en el MES, sería alcance nuevo. La E3 queda solo con DC
> (M-9, M-11, M-12, M-13).

Hay cuatro entregas, en el orden del flujo: primero el mecanismo y los avisos de formato,
después los clics fuera de secuencia, después DC y Log NC, y al final Setup Type, las
explicaciones y el cierre. Cada una sigue el ciclo de `docs/process.md` §2:
- rebuild;
- `aspnet_compiler`, porque todas tocan markup;
- tests, porque todas tocan la BLL;
- `/code-review` calibrado;
- checklist en el navegador;
- "funciona" y commit con el roadmap.

**Regla de toda la spec (D-92):** ninguna entrega cambia qué cuenta como intento ni el
puntaje. `ScoreCalculatorTests` y `ModePolicyTests` siguen en verde sin tocarlos. Si algo
del plan obliga a cambiar una calificación, Desarrollo para y avisa (enmienda).

## Lo que se verificó en el código antes de planear

- **Banner del POD:**
  - `GuidedTest.aspx:78` (`PnlBannerZone`, solo la variante de éxito, ícono verde);
  - `ShowBanner` (`GuidedTest.aspx.cs:1747`), que se oculta con `podAutoHide`;
  - estilos en `pod.css:102`.
- **Avisos de formato:** `PodNotice` (`PodCommandResult.cs:6`) → `ShowNotice`/`NoticeText`
  (`GuidedTest.aspx.cs:1766`), como SweetAlert en español. SFC Split manda el texto del
  aviso a su propio mensaje (`e.ErrorMessage`, `GuidedTest.aspx.cs:~759` →
  `SFCSplitControl.ascx.cs:66`, `ShowWarning`).
- **Fuera de secuencia:**
  - `PodService.CheckSequence` (`:884`) y `OutOfSequence` (`:898`) deciden con
    `ctx.Policy.OutOfSequence` (Ignore / RecordAttempt / Notice);
  - en la página, `HandleOutOfSequence` (`GuidedTest.aspx.cs:832`) y
    `ShowNotCurrentStepNotice` (`:824`, "Ese no es el paso actual.");
  - los botones sin lógica pasan por `ClickNonStepAction` (`:837`).
- **Feedback de un intento fallido:** `ApplySubmitResult` → `BuildIncorrectFeedback`
  (`GuidedTest.aspx.cs:~933`): en Assessment, SweetAlert "Incorrect" con los intentos
  restantes; en Práctica libre, "Respuesta incorrecta.". `_lastFailureFeedback` (`:38`,
  `:263`, `:803`) es lo que Ver pista muestra como último fallo, solo en el mismo request.
- **DC:** `PodService.SubmitDataCollectionGroup` (`:482`) →
  `StepValidator.ValidateDataCollectionGroup` (`:159`) y `ValidateParamValue`, que hoy
  devuelven un solo `bool`. `HasMissingDataCollectionValue` (`:195`) ya distingue el vacío
  para el registro de actividad. Tipos: Numeric, Boolean (un `DropDownList` "( Select ) /
  NO / Si", `DataCollectionControl.ascx.cs:213`), Date, Text y PersonName.
  → **No hay parámetros de lista libre:** el `19973` (M-10) no aplica y queda fuera
  (C-6 ya lo condicionaba).
- **Log NC:** `TxtNCCode` es `ReadOnly` (`LogNCControl.ascx:90`): el código solo sale de
  NC Selection. → **El `12901` (M-18) no aplica** y queda fuera (C-8 ya lo condicionaba).
  Add y Done registran un intento fallido (`LogNCControl.ascx.cs:197`).
- **Assembly:** el barcode se valida como numérico en el cliente (`AssemblyControl.ascx:223`,
  con SweetAlert "Barcode inválido") y en el servidor (`AssemblyControl.ascx.cs:99`).
- **Resource Setup:**
  - el Setup Type solo ofrece Shop Order y Tool Number (`ResourceSetupControl.ascx:198`);
  - el formato es del cliente (`rsFormatOk`, `:285`: mínimo 2 filas, "exactamente 1 Shop
    Order", "al menos 1 Tool Number" en el texto);
  - la BLL no compara el tipo de las filas de herramienta;
  - la guía enseña Tool Number (`GuideScriptBuilder.cs:27`, `AnswerToolNumber`).
- **Estado disponible para la regla de secuencia:**
  - `PodContext.IsResolved(StepType)` (`:84`);
  - `PodState.Order` con `SimulatedOrderRow.Status` (`New`/`Active`);
  - `PodState.DataCollection` (avance por grupo).
  - `PodContext` **no trae los intentos**, así que "la última respuesta del LC fue No"
    (C-3) necesita un dato nuevo en `PodState`.
- **Guía:** `GuideMoment` (`Pages/Training/Guide/GuideMoment.cs`; `Incorrect` con
  `detail`) y `GuideScriptBuilder` (`IncorrectName` / `IncorrectByDetail` por paso). El
  ancla `data-guide="pod-banner"` ya existe.
- **Tests:** `TrainingApp.Test` tiene `StepValidatorLogNCTests`, `ModePolicyTests`,
  `StepHintsTests` y `GuideScriptReader`. No hay tests de `PodService`, que crea sus
  repositorios: las reglas nuevas van en clases puras para poder probarlas.

## Diseño

- **`MesMessage`** (`BLL/Training/MesMessage.cs`), con:
  - `MesMessageId` (enum, M-1…M-17 sin M-10);
  - `Number` (`int?`; M-1 sin número);
  - `Template` en inglés, con marcadores (`{param}`, `{value}`, `{sfc}`, `{list}`, `{n}`,
    `{m}`);
  - `Surface` (`PodBanner`, `WindowStrip`, `SplitForm`);
  - `Format(args)`, que arma el texto final con `(Message n)` al final cuando hay número.

  Los textos se copian **literales** de la spec (tabla del catálogo). Es BLL pura, sin
  `System.Web`.
- **`PodCommandResult.MesError`** (`MesError`: id + texto ya formateado + superficie) junto a
  lo que trae hoy. `DataCollectionSubmitResult` y el resultado del split ganan lo mismo.
  La página **solo pinta** lo que trae el resultado: nunca decide qué mensaje toca.
- **`MesSequenceRule`** (`BLL/Training/MesSequenceRule.cs`, pura, como `PieceSelectionRule`):
  recibe el botón presionado, el paso actual, `PodState` y lo resuelto de la guía, y devuelve
  el `MesMessageId?` que daría el MES (C-1, C-2, C-3). La llama `PodService.OutOfSequence`
  con el `StepType` del botón. Como la llama en todos los modos, Assessment y Práctica libre
  reciben el mismo mensaje. Guided sigue ignorando el clic.
- **Pintado en la página** (`GuidedTest.aspx.cs`): `ShowMesError(MesError)`.
  - **Banner:** la variante de error de `PnlBannerZone` (ícono rojo "!" 24×24, misma fila de
    25 px, sin `podAutoHide`: D-112). Un postback nuevo ya lo oculta, porque el panel arranca
    con `Visible="false"`.
  - **Franja:** cada control con ventana (Assemble Components, Data Collection Entry, NC Data
    Entry) gana una franja de error propia con el mismo ícono, arriba de su contenido, y una
    propiedad o método para mostrarla. Cada una con su ancla `data-guide` (`asm-mes-error`,
    `dc-mes-error`, `nc-mes-error`).
  - **Split:** el mensaje reemplaza el formulario con [OK], y el OK cierra el modal (AUD-64).
    Reusa el canal que ya existe (`e.ErrorMessage`), ahora con el `MesError`.
- **Assessment (D-98):** el mensaje del MES va **en su superficie** y el feedback del
  simulador de hoy (SweetAlert "Incorrect" con los intentos restantes, o "Paso fallido…" en
  el 4.º intento) **sigue igual**. Si el intento fuerza el avance, el MES se pinta antes que
  la alerta de avance. Práctica libre: el banner reemplaza "Ese no es el paso actual."
  (D-101), y en un fallo dentro del paso se queda "Respuesta incorrecta." además del mensaje.
- **Ver pista y guía (D-102):** `_lastFailureFeedback` = texto del MES + explicación en
  español. En Guided, `GuideMoment.MesError(step, MesMessageId)` muestra la nota del
  mensaje anclada a su superficie. Las explicaciones viven en una clase de presentación de
  `Pages/Training/Guide/` (ej. `MesErrorNotes`), nunca en la BLL, con los textos literales
  de D-114. Assessment no recibe ni la guía ni las notas.

## Entrega 1 · Mecanismo y avisos de formato con texto del MES

Mensajes M-4 (Start sin SFC), M-5 (Assembly/DC sin fila y Split sin fila), M-6, M-8 y M-14
a M-16. Todos siguen sin intento (son formato).

- [x] `MesMessage`, `MesMessageId`, `MesError` y `Format`, con los 17 textos de la spec.
- [x] Tests `MesMessageTests`: cada id formatea con su número, `(Message n)` al final y los
      marcadores reemplazados; M-1 sin número; un texto con `"` (M-9) queda intacto.
- [x] `PodCommandResult.MesError`. En `PodService`, reemplazar estos avisos por su `MesError`
      sin cambiar el camino de cada uno:
  - `SfcRequired` → M-4 (`22506`);
  - `SfcNotInOrder` → M-6 (`22507 · Invalid SFC`, D-106);
  - `SelectOneRow` y `SelectRowToSplit` → M-5 (`12906`);
  - `InvalidSplit` y la Qty vacía del Split → M-14 / M-15 / M-16, según la causa. El
    servidor distingue vacía, 0 y mayor a la disponible; si hoy no lo hace, se separa
    `InvalidSplit` sin cambiar que no cuenta.
- [x] Banner de error (markup, `pod.css`, `ShowMesError`). Sin auto-hide.
- [x] Split: el mensaje S reemplaza el formulario y el OK cierra el modal.
- [x] Assembly (M-8): el barcode no numérico muestra la franja `17150` y despliega Component
      Details y Assembly Data vacíos con [Add] (AUD-69), en el cliente y en el servidor. Ya
      no hay SweetAlert "Barcode inválido".
- [x] Quitar de `NoticeText` los casos que pasaron al catálogo. Los avisos propios del
      simulador siguen ahí como SweetAlert (D-103).
- [x] `ScanNoticeTitleJs` / `ScanNoticeTextJs` (`GuidedTest.aspx.cs:459`, el escaneo en el
      cliente de la spec 011) usan el texto de M-6.

**Checklist en el navegador:**
- NonSerialized:
  - Start vacío → banner `SFC is missing (Message 22506)`;
  - Start con un número ajeno → `Invalid SFC (Message 22507)`;
  - Assembly o DC sin fila → `12906`;
  - Split sin fila → `12906`;
  - Split con Qty vacía / 0 / de más → el mensaje reemplaza el formulario, y el OK cierra
    la ventana.
- Assembly con barcode `abc` → franja `17150` y los detalles vacíos.
- El banner no se oculta solo y desaparece con la siguiente acción.
- En los tres modos, ninguno registra intento: el header de Assessment no cambia.
- Los avisos del simulador (`StartWholeOrder`, `SelectAllPieces`, `SplitRequired`…) siguen
  en SweetAlert.

## Entrega 2 · Clics fuera de secuencia con el mensaje del MES

Mensajes M-1 a M-5 y M-7, con la regla C-1 / C-2 / C-3.

- [ ] ~~`PodState.LineClearanceAnsweredNo`~~ → reemplazado por la enmienda 3: el "No" del LC
      de apertura sale de los intentos de la sesión (`MesSequenceState.Build`).
- [x] `MesSequenceRule.For(clickedStep, ctx)`, en el orden de C-2, con la tabla de abajo.
      Desarrollo verifica cómo identificar el LC **de apertura** (el primer `LineClearance`
      de la guía) con lo que ya da `PodContext`.

  | Botón | 1. RS sin resolver | 2. LC de apertura sin resolver | 3. Sin SFC iniciado | 4. DC obligatorio pendiente | Si no |
  |---|---|---|---|---|---|
  | Start | M-1 | M-3 si el LC de apertura tiene un intento `WrongDecision` (enmienda 3), si no M-2 | — | — | ninguno |
  | Assemble, DC Collect | — | ídem | M-5 | — | ninguno |
  | Log NC, SFC Split | — | — | M-5 | — | ninguno |
  | Complete | — | — | M-4 | M-7 (área con DC) | ninguno |
  | Resource Setup, Activities → LC, Logout, botones sin lógica | — | — | — | — | ninguno |

  "Sin SFC iniciado" = el paso de Start sin resolver, ninguna fila de `PodState.Order` en
  `Active`, `Order` nulo o ninguna fila seleccionada (enmienda 4). "DC
  obligatorio pendiente" = algún grupo obligatorio de DC sin guardar en
  `PodState.DataCollection`.
- [x] M-7 lista **todos** los parámetros de los grupos obligatorios no guardados, con el
      patrón `<PARAM> (<DC_GROUP>) , …` y el SFC (D-113). **Un M-7 por pieza** (D-119): uno
      por cada SFC seleccionado y activo; sin selección, M-4 (enmienda 4). El
      resultado lleva una **lista** de `MesError` y el banner pinta una fila de 25 px por
      mensaje, una debajo de la otra. Es el único caso con varios mensajes.
- [x] `PodService.OutOfSequence` llama a la regla y deja el `MesError` en el resultado en
      los tres modos. La calificación no cambia: RecordAttempt sigue registrando el intento,
      Notice sigue sin intento e Ignore se sigue ignorando sin pintar nada.
- [x] Página: `HandleOutOfSequence` pinta el `MesError` si viene. En Práctica libre, en vez de
      "Ese no es el paso actual."; en Assessment, antes del feedback de hoy.
- [x] Complete sin fila dentro de su paso (`NoRowToComplete`, intento fallido) → M-4 cuando
      **no hay ninguna** fila seleccionada. La selección parcial de Serialized conserva su
      texto. Log NC sin fila (`NoScrappableRow`) → M-5 cuando no hay ninguna fila.
- [x] Tests `MesSequenceRuleTests`: cada fila de la tabla, la prioridad (Start con RS y LC
      pendientes → M-1; Assemble con LC pendiente y sin SFC → M-2), M-3 con un intento `WrongDecision` del LC de apertura (y no con un clic fuera de secuencia), el LC
      de cierre sin resolver que no produce mensaje, DC Collect durante Assembly con el SFC
      iniciado → ninguno, y Complete en un área sin DC → M-4 o ninguno.

**Checklist en el navegador:**
- **Assessment:**
  - en Resource Setup, Start → banner M-1, más "Incorrect" con los intentos restantes;
  - en el LC de apertura, responder No y después Start → `Line Clearance status is NO…`;
  - sin responder el LC, Assemble → `has not been done since the last Resource Setup…`;
  - en Search, Complete → `22506`;
  - en DC, Complete → `15246` con la lista, una fila por pieza en Serialized;
  - en Assembly, DC Collect → solo el genérico.
- El 4.º intento fuera de secuencia sigue forzando el avance.
- **Práctica libre:** los mismos clics muestran el banner, sin intento y sin abrir nada; uno
  sin mensaje muestra "Ese no es el paso actual.".
- **Guided:** sin cambios.

## Entrega 3 · Data Collection y Log NC

Mensajes M-9, M-11, M-12 y M-13. M-17 quedó fuera (enmienda 5, D-120).

- [x] `StepValidator.FirstDataCollectionError(groupId, answer)` → `(param, value,
      MesMessageId)?`. Recorre los parámetros **en el orden de la pantalla**
      (`DisplayOrder`) y, por parámetro, aplica la prioridad de C-6:
  - vacío obligatorio → M-9;
  - texto en un Numeric → M-11;
  - menor que `MinValue` → M-13;
  - mayor que `MaxValue` → M-12.

  Los mismos casos que hoy hacen fallar `ValidateParamValue`, sin cambiar cuáles fallan. Un
  Date o un Boolean inválido no da mensaje: se queda el genérico. El nombre de la plantilla
  es el `Name` del parámetro, como en el MES.
- [x] `PodService.SubmitDataCollectionGroup` deja ese `MesError` en el resultado **solo
      cuando el grupo es incorrecto**. El intento se registra igual que hoy, con el mismo
      `Reason` (`EmptyRequiredField` o `WrongDecision`).
- [x] La franja de Data Collection Entry muestra el mensaje y la ventana queda abierta, como
      hoy tras un fallo.
- [ ] ~~Log NC: Done con `TxtNCCode` vacío → franja M-17 (`18117`)~~ → fuera (enmienda 5,
      D-120): no se puede provocar. Done y Add siguen con el feedback de hoy.
- [x] Tests en `StepValidator` (archivo nuevo `StepValidatorDataCollectionTests`):
  - el primero en orden gana;
  - vacío antes que límites en el mismo parámetro;
  - texto en un numérico;
  - `above` y `below`;
  - el valor tal cual en el texto;
  - PersonName vacío no da error;
  - un grupo correcto no da mensaje;
  - los mismos grupos que hoy son correctos o incorrectos siguen así.

**Checklist en el navegador:**
- **Assessment:** DC con un valor `99999` → franja `Value 99999 is above the upper limit
  (Message 15233)` más los intentos restantes. Con dos vacíos, solo el primero (`10015`).
  Con texto en un numérico → `12205`.
- **Práctica libre:** los mismos mensajes con "Respuesta incorrecta.".
- **Guided:** si la guía deja llegar a ese estado, la franja también sale.

## Entrega 4 · Setup Type Material, explicaciones en español y cierre

- [ ] Resource Setup:
  - `<option value="Material">Material</option>` en `rs-type`;
  - en `rsFormatOk`, "al menos 1 herramienta" cuenta Tool Number **o** Material (actualizar
    el texto del aviso);
  - "exactamente 1 Shop Order" no cambia (D-107);
  - los precargados y su calificación no cambian (D-100);
  - la guía sigue enseñando Tool Number.
- [ ] `MesErrorNotes` (presentación) con los textos de la tabla "Explicaciones en español"
      de la spec, **literales**.
- [ ] Guided: `GuideMoment.MesError` y su nota en `GuideScriptBuilder`, anclada al banner
      (`pod-mes-error`, enmienda 1) o a la franja de la ventana (`asm-mes-error`, `dc-mes-error`,
      `nc-mes-error`, Split). Si la guía ya estaba mostrando un paso, la nota del error tiene
      prioridad en ese request.
- [ ] Práctica libre: Ver pista incluye el texto del MES y su explicación como "último
      fallo" (`_lastFailureFeedback`), también para un clic fuera de secuencia sin intento.
- [ ] Assessment: verificar que ninguna nota ni explicación llega a la página (no recibe la
      guía).
- [ ] Tests: `StepHintsTests` / `GuideScriptReader`, para que cada `MesMessageId` del
      catálogo tenga su nota.
- [ ] **Cierre (documentación):**
  - `docs/domain.md`:
    - "Qué cuenta como intento": S2 enmendada por D-98 (el mensaje del MES se muestra
      también en Assessment) y la regla C-1;
    - Práctica libre (D-101);
    - Resource Setup, con Material (D-94, D-107).
  - `docs/architecture.md`: `MesMessage`, `MesSequenceRule`, superficies y dónde viven las
    notas.
  - `CLAUDE.md` → "POD y controles": una línea, "los errores del MES salen del catálogo de
    la BLL; la página solo los pinta".
  - `docs/ui-reference/evaluacion-practica-mes.md`: enmienda fechada de EP-4 y EP-5, con lo
    que cubre la 017 y lo que queda fuera (D-96, D-97, D-100).
  - `docs/ui-reference/general-flow.md` → "Reglas de la pantalla" (enmienda 2): Search/Start,
    Assembly, Data Collection, SFC Split y Log NC con sus mensajes del MES, y Resource Setup
    con Material.
  - `docs/status.md` (enmienda 2):
    - "Deuda y riesgos": M-1 provisional (D-111) y, **al integrar `docs/fidelidad-ui`**,
      marcar AUD-63/64/69 ☑ vía la 017 y anotar la corrección de `SfcNotInOrder` (D-106);
    - "Estado actual".
  - `docs/history.md`: la entrada de la 017.
  - `docs/decisions.md`: el estado de las D-n de la 017 que cambien al cerrar.
  - Aviso a Arquitectura y Planeación para marcar ✅ la fila 10 en `docs/roadmap.md`.

**Checklist en el navegador:**
- Resource Setup en los tres modos: una herramienta con Material se acepta como con Tool
  Number. Sin herramientas, el aviso de formato sigue.
- **Guided:** cada mensaje provocable muestra su nota en español junto a su superficie.
- **Práctica libre:** Ver pista, tras un mensaje, muestra el texto del MES y la explicación.
- **Assessment:** sin notas.
- Regresión completa de una guía de cada área (Moldeo NonSerialized, LoFlo Serialized) en
  los tres modos.

## Fuera de este plan

Lo que la spec dejó fuera (D-96, D-97, D-100, C-4) y, por lo verificado en el código, M-10
(`19973`: no hay parámetros de lista libre), M-18 (`12901`: el NC Code no se escribe) y M-17
(`18117`: NC Data Entry no se abre sin código; enmienda 5, D-120). Si un área futura hace
posible alguno de ellos, entra con su propio plan.

## Puntos confirmados por el usuario (2026-10-21)

- **D-117 · Assessment:** el mensaje del MES en su superficie y el SweetAlert de hoy con los
  intentos restantes, los dos (ver "Diseño").
- **D-118 · M-10 y M-18 fuera** por lo verificado en el código. Sin enmienda: la spec ya los
  condicionaba (C-6, C-8).
- **D-119 · M-7 uno por pieza** (Entrega 2): el banner admite varias filas solo para este
  caso.
