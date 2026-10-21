# Spec 017: Mensajes de error del MES (fidelidad con la evaluación práctica)

> **Estado:** borrador para revisión (2026-10-21); decisiones D-92 a D-114 confirmadas, falta la aprobación de la spec. Fase 8.5, fila 10 (EP-4, EP-5).
> **Insumo:** `docs/ui-reference/errores-mes.md` (textos y capturas del ambiente de testing,
> 2026-10-20; rama `docs/errores-mes`) y la auditoría de fidelidad
> `docs/audits/2026-10-19-fidelidad-ui.md` (AUD-63, AUD-64, AUD-69; rama
> `docs/fidelidad-ui`). Los textos que usa esta spec están copiados abajo, así que la spec no
> depende de que esas ramas estén integradas.

## Problem

En el MES real, cuando el operador hace algo que el sistema no acepta, aparece un **banner
rojo en el POD** o una **franja roja dentro de la ventana**, en inglés y con número de
mensaje. Por ejemplo, `SFC is missing (Message 12906)` o `Value 99999 is above the upper
limit (Message 15233)`. La evaluación práctica QD001043644 pide que el operador **reconozca**
esos errores (EP-5).

El simulador responde a las mismas situaciones de otra forma: SweetAlert en español
("Barcode inválido", "Campo requerido") o, en Assessment, el mensaje genérico con los
intentos restantes. El operador nunca ve el mensaje que va a encontrar en producción.

Además, la evaluación califica el Setup Type de Resource Setup, que tiene la opción
**Material**, y el simulador no la tiene (EP-4).

## Decisiones del usuario (2026-10-21)

- **D-92 · Solo errores reactivos.** La 017 muestra el mensaje real del MES cuando el
  operador **provoca** la situación. **No cambia la calificación**: lo que hoy es intento
  fallido lo sigue siendo, y lo que hoy es formato también. No se inyectan escenarios (una
  etiqueta con batch inválido, un SFC activo en otro recurso…).
- **D-93 · Texto provisional cuando no hay captura.** Un error que el operador sí puede
  provocar pero del que no hay texto real entra con un texto provisional, que se reemplaza al
  capturarlo (como D-72). Con D-96, el único caso es **Start sin Resource Setup**.
- **D-94 · Resource Setup: solo el Setup Type.** Se agrega la opción **Material**. Elegir
  Material o Tool Number en una fila de herramienta no está mal, en ningún modo. Con D-100,
  el procedimiento queda como hoy.
- **D-95 · El mecanismo visual es parte de la 017.** Se aprueban **AUD-63, AUD-64 y AUD-69**
  de la auditoría de fidelidad y la 017 las absorbe: la variante de error del banner y de la
  franja de ventana, los textos reales y el OK de SFC Split que cierra la ventana.
- **D-96 · Fuera, porque el operador no puede provocarlos:** batch inválido y cantidad
  insuficiente (`22801`), material sensible al tiempo expirado y SFC iniciado en otro
  recurso. Quedan como brecha de EP-5, para una spec de escenarios inyectados si se piden.
- **D-97 · El barcode de Assembly no se valida contra el BOM.** El componente no requerido
  (`13252`) queda fuera.
- **D-98 · El mensaje real se muestra en todos los modos.** En Assessment va junto con los
  intentos restantes. Reemplaza el mensaje genérico cuando el MES tiene uno para esa
  situación, **incluidos los clics fuera de secuencia**. Esto enmienda la regla S2 de
  `docs/domain.md`: el genérico era para no revelar que el fallo fue de secuencia. Ahora el
  operador ve lo mismo que en producción, ni más ni menos.
- **D-99 · Setup Type.** En las filas de herramienta valen Material o Tool Number. La fila de
  la orden tiene que ser Shop Order (ver C-9).
- **D-100 · El procedimiento de Resource Setup no cambia.** Vale Remove All o Remove Selected
  para quitar los precargados y no se exige el Save intermedio (spec 015 vigente). EP-4(a)
  queda como brecha documentada.
- **D-101 · Práctica libre:** el botón de otro paso muestra el mensaje real del MES, si lo
  hay, **sin intento y sin abrir nada**. Si no lo hay, sigue "Ese no es el paso actual.".
- **D-102 · Explicación en español:** cuando sale un mensaje del MES, la guía de Guided
  agrega una nota en español con qué significa y qué hacer. En Práctica libre, Ver pista la
  muestra junto al último fallo. Assessment no la recibe (no recibe la guía, spec 010).
- **D-103 · Avisos de formato:** los que el MES también tiene pasan a su texto real
  (`17150`, los de SFC Split, `22506` y `22507` de Start, `10015`/`12205` de DC). Los avisos
  propios del simulador (`SelectAllPieces`, `StartWholeOrder`, `SplitRequired`…) siguen como
  SweetAlert en español.

## Decisiones propuestas por Claude (confirmadas por el usuario el 2026-10-21)

- **C-1 (D-104) · Regla de los mensajes fuera de secuencia.** Se muestra el mensaje real solo si **en
  el MES esa acción, en ese estado del POD, daría ese error**. Si en el MES la acción pasaría
  (por ejemplo, DC Collect durante el paso de Assembly, con el SFC iniciado), se queda el
  aviso de hoy: el genérico en Assessment, "Ese no es el paso actual." en Práctica libre.
  Guided sigue sin dejar presionar otro botón.
- **C-2 (D-108) · Orden de los chequeos**, el del MES (el LC se revisa antes que el SFC):
  1. Resource Setup pendiente → texto provisional (Start).
  2. Line Clearance de apertura pendiente → `22501` (Start, Assemble, DC Collect).
  3. Sin SFC iniciado → `22506` (Start, Complete) o `12906` (Assemble, DC Collect, Log NC,
     SFC Split).
  4. DC obligatorio pendiente → `15246` (Complete).
- **C-3 (D-108) · Qué texto de `22501`:** si la última respuesta del LC de apertura fue **No**, sale
  `Line Clearance status is NO…`; si no, `Line Clearance has not been done since the last
  Resource Setup…`. El LC de cierre no entra: el MES no lo revisa en Split, Log NC ni
  Complete, así que esos clics siguen con el aviso de hoy (C-1).
- **C-4 (D-105) · El Line Clearance sigue calificándose en su paso** (D-92). Responder No es intento
  fallido en el LC, como hoy, aunque el MES lo guarda sin error y lo bloquea recién en Start.
  Mover el chequeo a Start cambiaría la calificación: va a Deferred.
- **C-5 (D-106) · SFC inexistente vs. SFC de otra orden.** El simulador no tiene otras órdenes, así
  que todo SFC que no es de la orden (`SfcNotInOrder`, NonSerialized) se trata como
  **inexistente**: `22507 · Invalid SFC`. Así se corrige lo que proponía AUD-63
  (`SfcNotInOrder → 22507/13005`): el `22501` "de otra orden" solo aplicaría con órdenes
  reales. El panel `Messages (n):` queda fuera.
- **C-6 (D-109) · Data Collection: un mensaje por Save.** El MES reporta **un** problema por Save.
  El simulador muestra el del primer parámetro con fallo, en el orden de la pantalla, con
  esta prioridad por parámetro: vacío obligatorio (`10015`) → fuera de lista (`19973`, si
  el control tiene un parámetro de lista) → texto en un numérico (`12205`) → límites
  (`15232`/`15233`). La calificación no cambia: sigue siendo intento fallido. Un parámetro
  `Date` inválido conserva el genérico, porque no hay texto real. `15232`/`15233` muestran
  el valor escrito, como el MES.
- **C-7 (D-113) · `15246` lista todos los parámetros pendientes** de los grupos obligatorios no
  guardados, con el patrón del MES (`<PARAM> (<DC_GROUP>) , …`). En el simulador son pocos y
  no hace falta truncar.
- **C-8 (D-110) · Log NC:** Done **sin** NC Code muestra `18117` y sigue siendo intento fallido
  (botón equivocado, spec 007). Done **con** un código conserva el genérico, porque en el MES
  pasaría. Log NC sin fila es `12906` y sigue siendo intento fallido. Un NC Code escrito a
  mano que no existe da `12901`, como formato, si el control deja escribir el código: el plan
  lo verifica.
- **C-9 (D-107) · Setup Type sin calificación nueva.** La fila de la orden con otro tipo ya la
  bloquea el formato "exactamente 1 Shop Order" (sin intento). Con un precargado en la tabla,
  el Save lo califica el servidor como hoy (D-26). La 017 solo agrega Material como tipo
  válido para herramientas. Si el usuario quiere que una orden con otro tipo sea intento
  fallido (lectura literal de D-99), cambia la calificación: el usuario eligió el formato (D-107).
- **C-10 (D-111) · Texto provisional de Start sin Resource Setup** (D-93): `Resource Setup has not
  been done, please perform Resource Setup`, sin número. Se anota como provisional en
  "Deuda y riesgos" del roadmap, como D-72.
- **C-11 (D-112) · El banner del POD queda hasta la siguiente acción** del operador (AUD-63), no
  se oculta a los 3 s como el de éxito. La franja de ventana queda hasta el siguiente envío de
  esa ventana o hasta cerrarla.
- **C-12 (D-114) · Los textos en español de D-102 son los de la tabla de abajo**, con "usted", como
  las pistas actuales.

## Catálogo de mensajes

Fuente: `errores-mes.md` (🧪 testing, 📷 figura de la WI). **Superficie:** B = banner rojo
del POD; F = franja roja dentro de la ventana; S = mensaje que reemplaza el formulario de SFC
Split con [OK], y el OK cierra la ventana (AUD-64). **Hoy** es lo que el simulador hace ahora
y **Calificación** no cambia (D-92).

| # | Situación en el simulador | Mensaje del MES | Sup. | Hoy | Calificación |
|---|---|---|---|---|---|
| M-1 | Start sin Resource Setup | *Provisional (C-10):* `Resource Setup has not been done, please perform Resource Setup` | B | `ResourceSetupRequired` o fuera de secuencia | como hoy |
| M-2 | Start / Assemble / DC Collect con el LC de apertura sin hacer | `Line Clearance has not been done since the last Resource Setup, please perform Line Clearance (Message 22501)` | B | fuera de secuencia | como hoy |
| M-3 | Ídem, con la última respuesta del LC = No | `Line Clearance status is NO, please perform Line Clearance (Message 22501)` | B | fuera de secuencia | como hoy |
| M-4 | Start o Complete sin SFC (NonSerialized: campo vacío; Complete sin fila) | `SFC is missing (Message 22506)` | B | `SfcRequired`; `NoRowToComplete`; fuera de secuencia | como hoy |
| M-5 | Assemble, DC Collect, Log NC o SFC Split sin SFC iniciado o sin fila | `SFC is missing (Message 12906)` | B | `SelectOneRow`, `SelectRowToSplit`, Log NC sin fila; fuera de secuencia | como hoy |
| M-6 | Start con un SFC que no es de la orden (NonSerialized) | `Invalid SFC (Message 22507)` | B | `SfcNotInOrder` (provisional desde la spec 011) | como hoy (formato) |
| M-7 | Complete con grupos de DC obligatorios sin guardar | `There are missing required values for data collection parameter(s) <PARAM> (<DC_GROUP>) , … for SFC "<sfc>" (Message 15246)` | B | fuera de secuencia | como hoy |
| M-8 | Barcode no numérico en Assemble | `2D barcode is not compliant with ISO 15434 format (Message 17150)`, y se despliegan Component Details y Assembly Data vacíos con [Add] (AUD-69) | F | SweetAlert "Barcode inválido" | formato |
| M-9 | DC: parámetro obligatorio vacío | `Required value "<param>" missing; enter a value in the <param> field (Message 10015)` | F | genérico | intento fallido |
| M-10 | DC: valor que no está en la lista | `Value "<v>" does not exist for <param>; select a value from the list (Message 19973)` | F | genérico | intento fallido |
| M-11 | DC: texto en un numérico | `Field <param> must contain a number (Message 12205)` | F | genérico | intento fallido |
| M-12 | DC: sobre el límite superior | `Value <v> is above the upper limit (Message 15233)` | F | genérico | intento fallido |
| M-13 | DC: bajo el límite inferior | `Value <v> is below the lower limit (Message 15232)` | F | genérico | intento fallido |
| M-14 | SFC Split: Qty vacía | `Define Qty to Split (Message 13875)` | S | lo bloquea el control | formato |
| M-15 | SFC Split: Qty mayor a la disponible | `Requested qty to split <n> is greater than the available SFC qty <m> (Message 13885)` | S | `InvalidSplit` | formato |
| M-16 | SFC Split: Qty 0 | `Quantity must be greater than 0 (Message 13213)` | S | `InvalidSplit` | formato |
| M-17 | Log NC: Done sin NC Code | `No NC codes logged (Message 18117)` | F | genérico | intento fallido |
| M-18 | Log NC: NC Code inexistente (si se puede escribir, C-8) | `Input value "NC_CODE" is invalid ("<v>") (Message 12901)` | F | por verificar | formato |

Los números y textos de las filas sin `(Message n)` en el texto original van tal cual los
registró `errores-mes.md`. El plan confirma el formato exacto con las capturas.

**Siguen como hoy**, porque no hay mensaje del MES:
- las decisiones equivocadas (otro código NC, otra pieza, otro método, la cantidad válida
  pero equivocada del split de Moldeo, Setup State Open, precargados guardados);
- los avisos propios del simulador (D-103);
- los botones del MES sin lógica.

### Explicaciones en español (D-102; textos aprobados, D-114)

| Mensaje | Nota de la guía (Guided) y de Ver pista (Práctica libre) |
|---|---|
| M-1 | Falta el **Resource Setup** de la estación: hágalo antes de iniciar. |
| M-2 | El MES no deja continuar porque falta el **Line Clearance** después del Resource Setup. Regístrelo antes de iniciar, ensamblar o recolectar datos. |
| M-3 | El último Line Clearance quedó en **NO**: el MES bloquea el proceso hasta que la línea esté liberada. Repita el Line Clearance. |
| M-4, M-5 | El MES necesita un **SFC iniciado**: seleccione o escanee el SFC y presione **Start** antes de esta acción. |
| M-6 | Ese SFC **no existe** en el MES: revise la etiqueta y escanee el SFC del lote. |
| M-7 | Faltan parámetros de **Data Collection** obligatorios: recolecte todos los grupos antes de **Complete**. |
| M-8 | El barcode no tiene el formato del MES: escanee la etiqueta del componente. |
| M-9 | Falta un valor obligatorio. El MES avisa **un parámetro a la vez**: complete el que indica y guarde otra vez. |
| M-10 | El valor no está en la lista: elija una de las opciones. |
| M-11 | Ese parámetro es numérico: escriba solo números. |
| M-12, M-13 | El valor está **fuera de los límites** del MPI. La pantalla del MES no muestra los límites: revise la medición contra el MPI. |
| M-14 a M-16 | Revise la cantidad a separar. Al presionar **OK** el MES cierra la ventana: abra **SFC Split** otra vez. |
| M-17 | El MES no registró ningún NC: primero seleccione el **NC Code**. |
| M-18 | Ese NC Code no existe: elíjalo de la lista. |

## Scope

### In

1. **Mecanismo visual** (D-95; AUD-63, AUD-64, AUD-69): variante de error del banner del POD
   y franja roja de ventana, con las medidas de `estilos-y-mensajes-mes.md`. El mensaje de
   SFC Split reemplaza el formulario y su OK cierra la ventana.
2. **Catálogo M-1 a M-18** y su uso en Start, Assembly, DC, SFC Split, Log NC y Complete,
   incluidos los clics fuera de secuencia (C-1 a C-3), en los tres modos (D-98, D-101).
3. **Explicaciones en español** en la guía de Guided y en Ver pista (D-102), con sus anclas
   `data-guide` donde haga falta.
4. **Setup Type Material** en Resource Setup (D-94, D-99, C-9).
5. Documentación: `docs/domain.md` → "Qué cuenta como intento" (S2 enmendada por D-98, la
   regla C-1 y que el mensaje real no cambia la calificación) y el flujo de Resource Setup
   (Material); `docs/architecture.md` (catálogo y superficies); la auditoría de fidelidad,
   con AUD-63/64/69 aprobadas vía la 017 (enmienda fechada, con la corrección de C-5);
   `evaluacion-practica-mes.md` (qué de EP-4/EP-5 queda fuera); y el roadmap.

### Out

- **Escenarios inyectados** (D-92, D-96): batch inválido, cantidad insuficiente, material
  sensible al tiempo expirado, SFC iniciado en otro recurso.
- **Validar el barcode contra el BOM** y `13252`, `12001`, `13251` (D-97).
- **El procedimiento REMOVE ALL + Save → Insert New** (D-100, EP-4a).
- **Grupos de herramientas por área** (W5) y los mensajes del Custom Resource Setup y de
  Tool List.
- **Mover el chequeo del LC a Start** (C-4), y el LC por tiempo vencido o por cambio de
  usuario.
- El panel `Messages (n):`, el `13961` (SFC ya activo), "Unsaved Data" (AUD-68) y el resto
  de la auditoría de fidelidad.
- Cambios de pesos, de esquema o de catálogo en la BD.
- Traducir los mensajes del MES: van en inglés, como en producción.

## Domain Model

- **Sin cambios de esquema ni de scripts.** Los mensajes son texto de pantalla fijo, como el
  resto de la UI simulada.
- **Catálogo de mensajes en la BLL** (nombre indicativo `MesMessage`: id, número, plantilla
  del texto y superficie). La BLL elige cuál aplica, porque el estado del POD (`PodState`)
  solo lo conoce ella. La página solo lo pinta. Nada de `System.Web`. El plan define dónde
  vive (`BLL/Training/`) y si los textos están en una clase propia.
- **`PodCommandResult`** gana el mensaje del MES que corresponde (con sus parámetros: valor,
  parámetro, SFC) junto a lo que ya trae (aviso, intento, fuera de secuencia). Los
  `PodNotice` que pasan al catálogo dejan de pintarse como SweetAlert.
- Las explicaciones en español son presentación: viven con la guía (`GuideScriptBuilder`) y
  con las pistas de Práctica libre, nunca en la BLL.
- `ModePolicy` no necesita una capacidad nueva si "mostrar el mensaje real" vale para todos
  los modos: lo que cambia por modo (intentos restantes, guía, pista) ya está en
  `FailureFeedback`, `ShowsGuide` y `HintOnDemand`. El plan lo confirma.

## API Contract

Web Forms, sin endpoints. Los comandos de `PodService` y `StepValidator` devuelven, además
de lo de hoy, el mensaje del MES que aplica (o ninguno). Las firmas las define el plan.

## Authorization

Sin cambios.

## Edge Cases & Failure Modes

- **El mensaje revela la dirección del error de DC** (`above`/`below`): el usuario lo aceptó
  (D-98). Es lo que ve el operador en producción.
- **Varios parámetros de DC con fallo:** solo el primero (C-6). Tras corregirlo y guardar,
  puede salir el siguiente, y cada Save fallido cuenta un intento, como hoy.
- **Fuera de secuencia después del cierre de la LC de apertura** (Split, Log NC, Complete
  antes de su paso): si el MES no tiene error para ese estado, se queda el aviso de hoy (C-1).
- **Avance forzado en Assessment:** el 4.º intento sigue forzando el avance. El mensaje del
  MES de ese intento se muestra antes del aviso de avance forzado (el orden lo define el plan).
- **F5 o botón Atrás:** el banner es del request que lo produjo. No se reconstruye al
  recargar.
- **Texto con datos del operador** (valor de DC, NC Code escrito): se codifica como el resto
  (`<%#: %>`; hacia `<script>`, `JavaScriptStringEncode`). `<`/`>` ya los bloquea el formato.
- **Sesiones abiertas al desplegar:** sin cambio de estado ni de esquema, siguen igual.

## Non-Functional Requirements

Sin impacto: los textos son fijos y la elección se hace con el estado que ya está en memoria.

## Integrations

Ninguna.

## Acceptance Criteria

**Mecanismo**
- **AC-1:** Given cualquier mensaje del catálogo, Then se ve con la superficie indicada (B,
  F o S), en inglés, con su número y con las medidas de `estilos-y-mensajes-mes.md`. El
  banner B queda hasta la siguiente acción.
- **AC-2:** Given un mensaje S de SFC Split, When presiona OK, Then la ventana de Split se
  cierra y hay que reabrirla.

**Mensajes**
- **AC-3:** Given Assessment, When ocurre una situación M-n, Then se muestra M-n con los
  intentos restantes, y el intento se registra o no **exactamente como antes de la 017**.
  `ScoreCalculatorTests` sigue en verde.
- **AC-4:** Given Assessment, When presiona un botón de otro paso cuya acción el MES
  rechazaría en ese estado (C-1, C-2), Then ve el mensaje real con los intentos restantes y
  cuenta el intento como hoy. Si el MES no la rechazaría, ve el genérico de hoy.
- **AC-5:** Given Práctica libre, When presiona un botón de otro paso con mensaje real, Then
  ve ese mensaje sin intento y sin abrir nada. Sin mensaje real, ve "Ese no es el paso
  actual.".
- **AC-6:** Given Start / Assemble / DC Collect con el LC de apertura pendiente, Then sale
  M-3 si la última respuesta fue No y M-2 si no (C-3).
- **AC-7:** Given DC con varios parámetros con fallo, When guarda, Then ve solo el mensaje
  del primero, según C-6.
- **AC-8:** Given Complete con DC pendiente, Then M-7 lista todos los parámetros pendientes.
- **AC-9:** Given los avisos propios del simulador (D-103), Then siguen como SweetAlert en
  español.

**Explicación**
- **AC-10:** Given Guided, When sale un mensaje del catálogo, Then la guía muestra su
  explicación en español. Given Práctica libre, Ver pista la incluye junto al último fallo.
  Given Assessment, Then no aparece ninguna explicación.

**Resource Setup**
- **AC-11:** Given Resource Setup, Then el Setup Type ofrece Material, y una herramienta con
  Material o Tool Number se acepta igual en los tres modos. El procedimiento y las reglas de
  los precargados no cambian (D-100).

## Deferred Decisions

- **Escenarios inyectados** para los cuatro errores de D-96 (otra spec, si se piden). Para
  los textos de `22801` ya está la figura de la WI.
- **Chequeo del LC en Start** en vez de en su paso (C-4).
- **EP-4(a)** (REMOVE ALL + Save → Insert New) y grupos de herramientas (W5).
- **Textos provisionales:** M-1 hasta tener la captura de un Start sin setup.

## Open Questions

Ninguna bloquea la spec. Antes del plan:

1. **Integrar los insumos.** `docs/errores-mes` y `docs/fidelidad-ui` no se pueden integrar
   con fast-forward sobre `TrainingApp` (salieron de commits anteriores). Hay que traerlos
   (cherry-pick en una rama nueva desde `TrainingApp`) antes de que Desarrollo implemente,
   porque el plan apunta a sus capturas y medidas.
2. **Capturas con el Code1 del usuario y SFC de testing** (`Screens SAP MES REAL/Errores/`):
   ¿se difuminan antes de integrarlas?
