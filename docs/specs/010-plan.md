# Plan de implementación — Spec 010

**Spec:** `docs/specs/010-overlays-guiados.md` (Approved 2026-10-06)
**Plan:** aprobado (2026-10-06), con las propuestas P-1 a P-8
**Arquitectura:** N-Tier (Pages → BLL → DAL → Domain). La guía es **solo presentación**:
no toca BLL, DAL, Domain ni la BD. Lee el `PodContext` que la página ya carga una vez por
request.

Cinco entregas. Cada una deja la app funcionando: los pasos que todavía no tienen guion
simplemente no muestran guía. Cada entrega: implementar → MSBuild Rebuild +
`aspnet_compiler` → `/code-review` del diff → checklist → el usuario prueba en el navegador
→ commit `feat: ... (spec 010, entrega N)` en `TrainingApp`. Sin cambios en la BLL no hace
falta correr `TrainingApp.Test`.

## Decisiones de diseño

### Piezas

| Pieza | Dónde | Qué hace |
|---|---|---|
| **Motor** | `UI/master/pod-guide.js` + `UI/master/pod-guide.css` | JS genérico, sin textos ni respuestas: oscurece, resalta, bloquea, muestra el globo y avanza según el modo de cada subpaso. Solo se incluye en Guided. |
| **Guion** | `Pages/Training/Guide/GuideScriptBuilder.cs` | Clase de presentación sin `System.Web`: recibe el `PodContext` y lo que pasó en el request, y devuelve los subpasos con sus textos ya armados. Los textos están en el **Anexo**. |
| **Modelo** | `Pages/Training/Guide/GuideSubstep.cs`, `GuideMoment.cs` | Un subpaso (clave, ancla, modo, texto, formato, paso y contador) y el "momento" del request (qué dejó la página en pantalla). |
| **Cableado** | `GuidedTest.aspx(.cs)` | En Guided y en `OnPreRender`: arma el guion, lo serializa con `JavaScriptSerializer` y registra `PodGuide.start(...)`. Los handlers anotan el momento. |
| **Anclas** | `GuidedTest.aspx` y los `.ascx` de paso | Atributos `data-guide="..."` en botones, campos, filas y mensajes. Sin cambios de comportamiento. |

### Cómo sabe la guía dónde empezar (P-2)

El servidor manda, en cada respuesta, la lista de subpasos y la **clave de entrada**. La
fija la página, que es la que ya decide qué queda en pantalla:

| Momento del request | Entrada |
|---|---|
| GET / F5 (sin control abierto) | primer subpaso pendiente del paso actual según el estado (componentes ensamblados, grupos guardados, split hecho, filas ya seleccionadas) |
| Se abrió un control o modal (`OpenStepControl`) | primer subpaso dentro de ese control |
| Intento incorrecto (`Incorrect`) | subpaso de la decisión (Setup State, respuesta de LC, el bloque del modal del checklist que reabre `ReopenForRetry`, el bloque de DC) |
| Mensaje de confirmación sin cerrar el paso (Add de Assembly, grupo o pieza de DC, "New SFC … created") | su subpaso informativo |
| Paso resuelto (`ContinueAfterStepResolved`) | subpaso informativo del mensaje del paso resuelto |
| Sesión completada | globo final |

**Cola + paso nuevo (P-4):** cuando un paso se resuelve en el request, el servidor ya está
en el paso siguiente pero en pantalla queda la confirmación del anterior (modal con "Record
changed", banner de Start…). La lista que se manda es **la cola del paso resuelto**
(mensaje + X si aplica) **seguida del guion del paso nuevo**, así la guía sigue sin
postback (ej. X de Resource Setup → Activities de Line Clearance).

Si un camino no fija entrada, se usa la del GET. Si el ancla no aparece, la falla segura
de la spec cubre el caso.

### Motor (P-1: JS propio, sin librería)

- **Oscurecido con hueco:** cuatro paneles alrededor del elemento resaltado bloquean el
  clic fuera de él. **Logout** y **Abandon Session** quedan por encima del oscurecido.
- **Teclado:** escucha `keydown` en fase de captura. Tab solo recorre el elemento resaltado
  y el globo; Enter y Espacio fuera de ellos (y de Logout, Abandon y los SweetAlerts) no
  hacen nada. En captura, Enter = "Continuar" y no envía el formulario.
- **Modos:** `click`, `change`, `capture` (formato: requerido o numérico, como el control),
  `block` (todos los campos del bloque; el de DC siempre habilitado), `scan` (avanza cuando
  el control muestra Add), `verify`, `info` y `final`.
- **Espera y falla segura:** busca el ancla visible cada 100 ms. A los 4 s sin encontrarla,
  pasa a globo fijo sin oscurecido y sigue buscando el ancla actual o la siguiente.
- **Capas:** por encima de los modales de Bootstrap (incluido el submodal de LC) y por
  debajo de SweetAlert2. Mientras hay un SweetAlert abierto la guía se pausa. Al cerrar uno
  de formato vuelve al primer subpaso de captura del modal abierto; uno de intento llega
  con postback y trae su entrada.
- **Globo:** dentro del modal abierto de más arriba, para que la trampa de foco de
  Bootstrap no bloquee "Continuar". Se reubica al redimensionar o desplazar la ventana y
  al abrir o cerrar un modal.
- **Selección de filas:** el subpaso se salta si las filas pedidas ya están seleccionadas.

### Mensajes que esperan a la guía (P-5)

Los tres temporizadores que ocultan mensajes pasan por un único helper de la página,
`podAutoHide(selector, ms)`:
- `ShowBanner` en `GuidedTest`;
- "Record changed" en `ResourceSetupControl`;
- "1 DC Groups saved" en `LineClearanceControl`.

Con la guía activa, el helper no programa nada y la guía oculta el mensaje al presionar
"Continuar" en su subpaso informativo. En Assessment se ocultan como hoy (AC-21).

### Seguridad (spec → Authorization)

- En Assessment no se incluye el motor y no se registra el guion (AC-2). Las anclas
  `data-guide` quedan en el markup en ambos modos: solo nombran elementos ("rs-retrieve"),
  no revelan respuestas.
- Textos: el builder codifica con `HttpUtility.HtmlEncode` todo dato de la BD (grupo,
  parámetro, unidad, número de parte, SFC) y envuelve los valores en `<strong>`. El JSON
  va con `JavaScriptSerializer`, que escapa `<` y `>`.
- La guía no envía nada al servidor: no hay endpoint, ni `.ashx`, ni `[WebMethod]`.

## Propuestas (aprobadas por el usuario con el plan, 2026-10-06)

- **P-1 · JS propio, sin librería** (como el legacy). Los modales apilados de Bootstrap, la
  trampa de foco, los SweetAlerts y la espera de elementos piden control fino. Una
  librería (Driver.js) habría que copiarla a `~/Scripts` y adaptarla igual.
- **P-2 · La entrada la fija la página** según el momento del request, no el JS mirando el
  DOM. Es determinista y sigue al servidor (R4).
- **P-3 · El guion vive en una clase de presentación pura** (`GuideScriptBuilder`, sin
  `System.Web`). No es regla de negocio y queda fuera de la BLL. Al ser pura, se podría
  testear después (Fase 5).
- **P-4 · Cola del paso resuelto + paso nuevo** en la misma lista.
- **P-5 · Un helper para ocultar mensajes**, apagado con la guía activa.
- **P-6 · Extender R7:** pasan al español también los otros textos que Guided muestra tras
  un intento en lugar del hint: "Select all rows to complete", "Select 1 row to complete"
  (`AttemptFeedback`) y "Not the correct button" (Log NC).
- **P-7 · Sin tests nuevos:** la guía es UI y la Fase 5 está en pausa. Si se quieren, los
  de `GuideScriptBuilder` (flags, repeticiones, entrada) se agregan en la Fase 5.
- **P-8 · Trato de usted en todos los textos**, como los ejemplos de R2 ("Presione",
  "Seleccione"). Implica ajustar el texto del globo final de la spec a "Completó la guía.
  Cuando se sienta listo, intente el Assessment" (enmienda menor de redacción).

## Entregas

### Entrega 1 — Motor, Resource Setup y hints en español — verificada (2026-10-06)

- `pod-guide.js` y `pod-guide.css` completos: oscurecido, bloqueo de mouse y teclado,
  Logout y Abandon libres, globo (paso, contador, texto), modos `click`, `change`,
  `capture`, `verify` e `info`, falla segura, capas sobre modales y SweetAlerts, y
  reubicación.
- `GuideSubstep`, `GuideMoment` y `GuideScriptBuilder`, por ahora solo con Resource Setup.
- `GuidedTest`:
  - `OnPreRender` con el guion solo en Guided;
  - `PlaceHolder` con los `<script>`/`<link>` del motor, visible solo en Guided;
  - momentos de Resource Setup: GET, modal abierto, Save correcto con "Record changed" e
    incorrecto.
- Anclas: botón Resource Setup del pie; Retrieve, REMOVE ALL, INSERT NEW, tipo y valor de
  la fila (en el `addRow` del JS), tabla, Setup State, Save, "Record changed" y X.
- `podAutoHide` en `GuidedTest` y "Record changed" con espera.
- `GetHint` y los textos de P-6 en español.
- **AC:** 1, 2, 3, 4, 5 (hasta la X), 14, 15 (campos de RS), 17, 19, 20 y 21 (en "Record
  changed").
- **Notas de implementación** (con el `/code-review`, 14 hallazgos aplicados):
  - **Globo fuera de los modales.** Dentro de un modal, la sombra del marco lo taparía.
    Con un modal abierto, "Continuar" se usa con el mouse o con Enter: Enter equivale a
    "Continuar" en captura, verificación e informativo.
  - **Con un modal abierto solo cuenta un ancla dentro del modal de más arriba.** Si la
    entrada queda detrás, la guía cae en la falla segura y retoma en el subpaso siguiente.
  - **El bloqueo también rige mientras busca el ancla y mientras se envía la página.** El
    envío se detecta con `beforeunload`, no con un tiempo. Logout y Abandon quedan por
    encima de todo (incluidos los modales) mientras la guía bloquea.
  - **`podAutoHide` pregunta a la guía si va a mostrar ese mensaje** (`PodGuide.holds`); si
    no, lo oculta como siempre. Por eso `ShowBanner` y "1 DC Groups saved" ya pasan por el
    helper en esta entrega, sin cambio visible hasta que sus pasos tengan guion.
  - **El guion de cada paso vive en un solo descriptor** (subpasos + entradas al abrir, tras
    un error y tras resolverse).
  - **El hint de Log NC nombra el Component solo si el área lo pide.** Los avisos de
    `AttemptFeedback` pasan a usted.
- **Checklist:**
  - Moldeo Guided: RS completo con la guía;
  - Save con Open → hint en español y vuelta a Setup State;
  - F5 con el modal abierto → entra por el botón;
  - Logout y Abandon con la guía activa;
  - Tab + Enter fuera del resaltado;
  - Assessment sin guía, con el código fuente revisado;
  - falla segura (ancla renombrada a propósito en el navegador).

### Entrega 2 — Line Clearance y Search/Start — verificada (2026-10-06)

- **Notas de implementación:** el momento lleva el modal que reabre un intento incorrecto
  del checklist (`ReopenForRetry` lo devuelve: verificaciones o final). **Realineo** en la
  entrada: si el servidor reabrió un modal sin anotar el momento, la guía entra en el primer
  subpaso del mismo paso que está dentro de ese modal. `/code-review low` (2 archivos): las
  entradas del checklist solo existen en áreas con checklist.

- Guion de LC (apertura y cierre) y de Start: NonSerialized y Serialized, y el checklist de
  4 modales por bloque cuando `RequiresStartChecklist`.
- Modo `block` (3 verificaciones y 10 MSS) y entrada al modal que reabre `ReopenForRetry`.
- Cola + paso nuevo: RS → LC → Start → paso siguiente.
- Anclas:
  - LC: Activities, Data Collection Standalone, Retrieve, Collect, respuesta, Save,
    mensaje y X;
  - Start: Browse, filas y Select All de Browse, OK, Start, campos y Save de los 4 modales.
- `ShowBanner` y "1 DC Groups saved" con espera (banner de Start).
- **AC:** 6, 7, 8 y 20 en esos mensajes.
- **Checklist:**
  - Moldeo Guided: RS → LC → Start con checklist → entra a DC (aún sin guía);
  - error en una verificación del checklist → vuelve a su bloque;
  - Redefinity Guided: Start con Select All.

### Entrega 3 — Assembly y Data Collection

- Assembly:
  - repetición por componente pendiente, modo `scan` y confirmación del Add;
  - X entre componentes y banner final.
- Data Collection:
  - solo grupos obligatorios (o el primero si no hay);
  - repetición por pieza y por grupo, bloque con rangos, Sí y fecha de ejemplo;
  - banners de grupo y de pieza;
  - el último Save sin mensaje.
- Subpaso de selección de filas que se salta si ya están seleccionadas.
- Entrada tras F5 a mitad de paso (componente o grupo siguiente).
- Anclas:
  - botones Assemble y DC Collect del POD, filas y Select All del POD;
  - Assemble de cada fila (con el id del componente), Barcode, Add, confirmación y X;
  - Collect de cada grupo (con su código), bloque de parámetros y Save.
- **AC:** 9, 10, 16 y 20 en esos mensajes.
- **Checklist:**
  - Redefinity Guided: Assembly completo con lector o con teclado + Enter;
  - DC por cada pieza;
  - F5 con un componente hecho;
  - Moldeo: DC solo con `DCGTM_FECHA_EXPIRACION_NIR/A`, y un valor fuera de rango →
    vuelve al bloque.

### Entrega 4 — SFC Split, Log NC, Complete y globo final

- SFC Split: fila del lote, Qty, Split, "New SFC … created", Close y banner.
- Log NC: fila a scrapear (la del split en NonSerialized, una pieza en Serialized), toda la
  lista NC Selection, Component si `RequiresNCComponent`, Add-Done y banner de scrap.
- Complete: Select All o el SFC, Complete y globo final sobre "Session Complete".
- **AC:** 11, 12, 13 y 20 en esos mensajes.
- **Checklist:** Moldeo y Redefinity en Guided de punta a punta.

### Entrega 5 — Regresión y cierre

- Pasada completa en el navegador:
  - Moldeo y Redefinity en Guided y en Assessment;
  - sesión única y logout;
  - Session reciclada (D-10);
  - intentos y puntaje iguales con y sin guía (AC-18).
- Docs:
  - `docs/roadmap.md`: Fase 6.6 cerrada y "Estado actual";
  - `CLAUDE.md`: árbol con `Pages/Training/Guide` y `pod-guide.js`, convención de anclas
    `data-guide` y roadmap;
  - spec 010 cerrada.
- **AC:** 18, 21 y repaso de 1-20.

## Riesgos

| Riesgo | Mitigación |
|---|---|
| Trampa de foco de Bootstrap: "Continuar" fuera del modal no recibe foco | Globo dentro del modal abierto de más arriba; se prueba en la E1 con Resource Setup |
| Capas: backdrops estáticos, submodal de LC, SweetAlert2 | z-index fijo entre los modales (1055) y SweetAlert2 (1060); el submodal se prueba en la E2 |
| Un camino de la página que no fija la entrada | Entrada por defecto como el GET + falla segura; el checklist recorre cada momento |
| Anclas en HTML creado por JS (filas de RS) o por el servidor (parámetros de DC, filas de listas) | El ancla va en la plantilla que crea la fila; selectores con el atributo de la fila (`data-component-id`, código del grupo) |
| Dropdown de Activities (Popper `fixed`) | Esperar a que el menú esté visible antes de resaltar el ítem |
| Recorridos largos en Serialized (3-5 piezas × grupos) | Aceptado en la spec (R5); el contador muestra el avance |
| Scripts de arranque en orden (modales que se abren con `showModalSafe`) | El motor espera el ancla visible; no depende del orden |

## Anexo — Textos de la guía

Trato de usted (P-8). `{x}` = dato del contexto, codificado y en negrita. "Continuar" va
en el globo de los subpasos de captura, verificación e informativos. Paso del globo:
"Paso {orden} · {Resource Setup | Line Clearance | Start | Assembly | Data Collection |
SFC Split | Log NC | Complete}".

**Resource Setup**
1. Presione **Resource Setup** para asociar la estación (recurso) a la orden de trabajo.
2. Presione **Retrieve** para cargar la configuración del recurso.
3. Presione **REMOVE ALL** para limpiar la tabla antes de agregar los recursos de esta orden.
4. Presione **INSERT NEW** para agregar una fila.
5. La fila nueva es de tipo **Shop Order**. Ingrese el número de la orden, por ejemplo **303406950**.
6. Presione **INSERT NEW** para agregar otra fila.
7. Cambie el tipo de la fila nueva a **Tool Number**.
8. Ingrese el número de la herramienta, por ejemplo **10033058**.
9. En esta sección se agregan todos los recursos, herramientas y materiales asociados a su operación.
10. Verifique que **Setup State** esté en **Set Up**. Si no, cámbielo.
11. Presione **Save** para guardar.
12. Se muestra el mensaje **Record changed**: indica que se aplicó el cambio.
13. Cierre Resource Setup con la **X**.

**Line Clearance**
1. Presione **Activities...** para abrir el menú de actividades.
2. Seleccione **Data Collection Standalone** para registrar la limpieza de línea.
3. Presione **Retrieve** para cargar los grupos de datos del recurso.
4. Presione **Collect** en **DC_LC_DONE** para responder la limpieza de línea.
5. Seleccione **Si**: la limpieza de línea se realizó.
6. Presione **Save** para guardar la respuesta.
7. Verifique que aparezca el mensaje **1 DC Groups saved**: la respuesta quedó registrada.
8. Cierre Data Collection Standalone con la **X**.

**Start**
1. Presione el ícono **Browse Work List** (⧉), junto a SFC, para buscar la orden.
2. NonSerialized: Seleccione el SFC **{sfc}**. · Serialized: Presione **Select All** para seleccionar todas las piezas de la orden.
3. Presione **OK** para cargar la selección en el POD.
4. Presione **Start** para iniciar la orden.
5. Ingrese el **ID del molde** según la MSS.
6. Presione **Save**.
7. Responda **Sí** a las tres verificaciones de etiquetas.
8. Presione **Save**.
9. Ingrese los parámetros técnicos según la **MSS D000281359**.
10. Presione **Save**.
11. Seleccione **Sí** en la verificación final.
12. Presione **Save** para iniciar la orden.
13. Verifique el mensaje **Start process completed successfully**: la orden quedó activa.

**Assembly**
1. Serialized: Presione **Select All** para seleccionar todas las piezas. · NonSerialized: Seleccione el SFC **{sfc}**.
2. Presione **Assemble** para abrir la Component List.
3. Presione **Assemble** en el componente **{número de parte}**.
4. Escanee el código de barras del componente, por ejemplo **10087734**.
5. Presione **Add** para registrar el componente.
6. Verifique el mensaje de confirmación: el componente quedó ensamblado.
7. Cierre el modal con la **X** para seguir con el siguiente componente.
8. Verifique el mensaje **All components processed at this operation**: se ensambló todo el BOM.

**Data Collection**
1. Igual que Assembly 1.
2. Presione **DC Collect** para abrir la Data Collection List.
3. Presione **Collect** en el grupo **{grupo}**. (Serialized: "… para la pieza **{sfc}**")
4. Ingrese los valores:
   - Numeric: "**{parámetro}**: entre **{min}** y **{max}** {unidad}"; solo mínimo: "mayor o igual a **{min}**"; solo máximo: "menor o igual a **{max}**"; sin rango: "un valor numérico".
   - Boolean: "**{parámetro}**: **Sí**".
   - Date: "**{parámetro}**: una fecha válida, por ejemplo **{hoy + 1 año}**".
5. Presione **Save** para guardar el grupo.
6. Grupo: Verifique el mensaje **All data for DC group {grupo} has been collected**. · Pieza: Verifique el mensaje: la pieza **{sfc}** completó Data Collection y sigue la siguiente.

**SFC Split**
1. Seleccione el SFC **{sfc}**.
2. Presione **SFC Split** para dividir el lote.
3. Ingrese la cantidad a dividir, por ejemplo **100**.
4. Presione **Split**.
5. Verifique el mensaje **New SFC … created with quantity …**: se creó un SFC nuevo con esa cantidad.
6. Presione **Close** para terminar el split.
7. Verifique el mensaje **SFC Split completed successfully**.

**Log NC**
1. NonSerialized: Seleccione el SFC creado por el split, **{sfc}**. · Serialized: Seleccione una pieza para registrar el defecto.
2. Presione **Log NC** para registrar un defecto.
3. Presione **Log NC** en el código del defecto. En el simulador cualquier código es válido.
4. Ingrese el componente, por ejemplo **10087734**.
5. Presione **Add-Done** para registrar el NC.
6. Verifique el mensaje **{…} scrapped**: las unidades se dieron de baja.

**Complete**
1. Serialized: Presione **Select All** para seleccionar todas las piezas. · NonSerialized: Seleccione el SFC **{sfc}**.
2. Presione **Complete** para cerrar la orden.
3. Completó la guía. Cuando se sienta listo, intente el Assessment. [OK]

**Hints tras un intento fallido (R7 + P-6)**

| Paso / caso | Texto |
|---|---|
| Resource Setup | Presione Retrieve, verifique que Setup State esté en 'Set Up', agregue las filas y presione Save. |
| Line Clearance | Responda 'Si' para confirmar la limpieza de línea. |
| Start (checklist) | Responda 'Sí' a las verificaciones y presione Save. |
| Data Collection | Complete todos los parámetros dentro de su rango y presione Save (no Save Draft). |
| SFC Split | Seleccione una fila del POD, divida una cantidad válida y presione Close. |
| Log NC | Seleccione un código NC, ingrese el Component y presione Add-Done. |
| Assembly | Ensamble cada componente de la Component List: escanee su código y presione Add. |
| Complete | Presione Complete para cerrar la orden. |
| Complete sin filas (Serialized / NonSerialized) | Seleccione todas las filas para completar. / Seleccione 1 fila para completar. |
| Log NC con botón equivocado | Ese no es el botón correcto. |
