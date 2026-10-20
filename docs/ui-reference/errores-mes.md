# Errores del MES real por paso de la guía

> **Para qué:** insumo de la **spec 017** (fila 10 de la Fase 8.5; EP-4 y EP-5). Reúne, paso
> por paso, los errores que el MES real muestra al operador: el **texto exacto** cuando se
> conoce, dónde se vio y qué hace hoy el simulador en ese caso. Sin captura o texto real no
> se inventa UI (`docs/process.md` §8): lo que falta está en "Pendiente de capturar".
> **Es registro de consulta:** no cambia nada por sí solo; lo que se decida implementar va a
> la spec o al plan.
> **Hecho el 2026-10-20** en tres pasadas: contraste con las WI (sesión de contraste) y dos
> tandas de capturas en el ambiente de testing el mismo día (otra sesión de Claude con
> Chrome, con permiso del usuario; ver "Escrituras hechas en testing").

## Fuentes y cómo leer la evidencia

| Marca | Fuente |
|---|---|
| 🧪 **Testing** | Mensaje medido en el ambiente de testing (sitio CR9A). El 2026-10-19: `estilos-y-mensajes-mes.md` (rama `docs/fidelidad-ui`, aún sin integrar a `TrainingApp`). El 2026-10-20: capturas propias, marcadas 📸 |
| 📸 | Captura en `Screens SAP MES REAL/Errores/` (nombre `<paso> - <caso> <número>.png`) |
| 📷 **WI-CR** | Figura de la WI de Costa Rica QD001001524 (`.docx` en español), también en `Errores/` |
| 📝 **WI-CR / WI-CO** | Solo la nota de texto de la WI de Costa Rica o de la de Colorado Springs D001372587 rev. E (en inglés): dice **que** hay un error, no **cuál** es su texto |
| ❔ | Se sabe la situación pero no hay texto ni captura |

Los mensajes del MES van **siempre en inglés y con número**, nunca como popup: banner rojo
con "!" en el POD o franja roja dentro de la ventana (medidas y comportamiento:
`estilos-y-mensajes-mes.md`). El simulador hoy usa SweetAlert en español (AUD-63).

## Resumen: los 6 errores de la fila 10 del roadmap

| Error (EP-5) | ¿Texto real? | Evidencia |
|---|---|---|
| Batch inválido | ✅ `Batch Number is Invalid or the batch doesn't exist in Storage location AL01 (Message 22801)` | 📷 |
| Cantidad insuficiente | ✅ `Not enough Quantity Available in Unrestricted Stock (Message 22801)` | 📷 |
| Componente no requerido | ✅ `Component 600000005112/A is an invalid alternate for the component. (Message 13252)`, con un componente de **otro BOM** | 🧪📸 |
| DC fuera de límites | ✅ **Es error, no alerta:** `Value 99999 is above the upper limit (Message 15233)` y `Value -99999 is below the lower limit (Message 15232)`; bloquea el Save. Resuelve la diferencia entre CR ("alerta") y CO ("error") | 🧪📸 |
| SFC iniciado en otro recurso | ❔ El MES responde antes con los avisos de Line Clearance (`22501`); el mensaje propio no se alcanzó y el usuario de testing no puede liberar el SFC activo (`16034`) | 🧪📸 |
| Material sensible al tiempo expirado | ❔ La operación probada no tiene componentes sensibles al tiempo; solo salió `15357` al abrir TSM Check In/Out | 🧪📸 |

Más los de Resource Setup (EP-4): se capturaron los del **Custom** Resource Setup (faltan
herramientas, otro grupo, inexistente); siguen sin texto "otro recurso" y "expirada", y no se
sabe qué dice el Standard, que es el que modela el simulador. Además, el testing dio **muchos
mensajes que nadie pidió** (Line Clearance, Start, Assemble, DC, Complete, Log NC, Split y Tool
List): están en cada paso.

---

## 0. Ingreso a MES

Sin errores del MES documentados. Las WI no describen fallos del login de MES. El
`ERROR: The process "…" not found` del script de limpieza es normal (`pre-sap-flow.md`). Los
mensajes de login que traen las WI ("Debe llenar el campo de usuario", "Usuario o Contraseña
Inválida") son de la herramienta de Shelf Life, no de este paso.

## 1. Resource Setup

**Hay dos pantallas** (CO 6.5; cuál se usa lo fija la configuración del POD): el **Standard**,
con filas `Setup Type` / `Setup Type Value` (Insert New, Shop Order y Tool Number a mano, sin
saber qué grupos pide la operación), y el **Custom**, que precarga los grupos de herramientas
de la operación y se llena así: Setup State → Shop Order → *Associate New ShopOrder* → un Tool
Number por grupo → Save → Yes. **El simulador modela el Standard** (y, según el usuario,
2026-10-20, no usa por ahora el Custom, aunque los mensajes de error deberían ser los mismos).
**El recurso de testing (`R_OMNI_FIRST_DISTAL_1`) usa el Custom**, así que todo lo capturado
abajo es del Custom: son **ventanas modales dentro de la app** (título "Error" o
"Information"), **sin número de mensaje**, en una emergente fuera del POD.

Mensajes del Custom Resource Setup (🧪📸):

| Situación | Mensaje | Captura |
|---|---|---|
| Retrieve sin herramientas asociadas | Information · `No tool number association exists in resource set up. Please click: Associate New Shoporder button to verify once.` | `resource setup - retrieve sin herramientas asociadas.png` |
| Associate New ShopOrder | Information · `Shoporder is associated in resource setup. Please associate tool numbers as well`; carga los 8 grupos de la operación (`TG_FLUID_DISPENSER`, `TG_HEATER_BLOCK`, `TG_HOLE_GAUGE`, `TG_RULER`, `TG_SOLDERING_STATION`, `TG_TIMER`, `TG_UV_LIGHT`, `TG_WELLER_HOT_AIR`) | `resource setup - shop order asociada pide herramientas.png` |
| Save con **menos herramientas** que los grupos | Error · `Please enter all configured tool numbers for tool groups.` Sale igual con Setup State "Not Set Up" | `resource setup - faltan herramientas.png` |
| Herramienta de **otro grupo** | Error · `Tool number: 10080229 does not belong to tool group: TG_HOLE_GAUGE`. Sale **al salir del campo**, antes del Save | `resource setup - herramienta de otro grupo.png` |
| Herramienta **inexistente** | Error · `Tool number: 99999999 is invalid.` (al salir del campo) | `resource setup - herramienta inexistente.png` |
| Grupo **sin herramientas habilitadas** (buscador de `TG_HEATER_BLOCK`) | `No Configured Tools found with Enabled Status . Please configure tool in Tool Maintenance`. Es lo más cercano a "herramienta no disponible" | `resource setup - grupo sin herramientas habilitadas.png` |

Setup State ofrece: Set Up, Breaking Down, Open, Not Set Up y Setting Up. En esa operación
`TG_HEATER_BLOCK` no tiene herramientas habilitadas: **no se puede guardar un setup completo**,
así que los chequeos propios del Save (p. ej. herramienta en otro recurso) no se alcanzaron.

Lo que las WI dicen y siguen sin dar texto (❔), para los dos tipos de pantalla:

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Start antes de completar Resource Setup | ❔ No se probó: no hubo un recurso sin setup. Sin Line Clearance desde el último setup sale `22501` (ver Line Clearance) | 📝 CR y CO | Aviso `ResourceSetupRequired` ("Debes completar Resource Setup antes de iniciar."), sin intento; en Assessment el click fuera de secuencia es intento fallido |
| Herramienta ya configurada en **otro recurso** | ❔ Las WI solo dicen que sale un error (una herramienta se configura en un recurso a la vez) | 📝 CR y CO | No se simula: acepta valores libres (decisión vigente) |
| **Menos** herramientas que las requeridas (CO: también más) | ✅ en el Custom (arriba); en el Standard ❔ | 🧪📸 · 📝 | No se simula; solo se exige ≥ 1 Tool Number (W3) |
| Herramienta de **otro grupo** (un micrómetro en vez de una regla) | ✅ en el Custom (arriba); el Standard no conoce los grupos, ❔ | 🧪📸 · 📝 | No se simula; haría falta un catálogo de herramientas por área (W5) |
| Herramienta **expirada** o no disponible | ❔ Las WI dicen que sale un error y que una herramienta Scheduled o Unscheduled Down no sirve para producción. No existe un estado "Expired" (Apéndice B de CO: Enabled, Productive, Standby, Engineering, Disabled, Scheduled Down, Unscheduled Down, Non-scheduled, Holds). El Custom solo ofrece herramientas en estado Enabled | 📝 CR y CO | No se simula |

Éxito (no es error): "Se mostrará un mensaje que indica que se aplicó el cambio"; la
captura `record changed correcto.png` ya está en el repo.

### Tool List (botón del POD)

Es el Log Tool estándar de SAP ME por SFC (una herramienta por grupo, Qty Logged 1). Solo
muestra herramientas en estado Enabled (CO 6.7.2), así que **no sirve para encontrar una
expirada o caída**. Errores en la franja de la ventana `Log Tool Entry`, 🧪📸:

| Situación | Mensaje | Captura |
|---|---|---|
| Herramienta de otro grupo | `Tool "10080229" does not belong to tool group "TG_HOLE_GAUGE" (Message 16625)` | `tool list - herramienta de otro grupo 16625.png` |
| Herramienta inexistente | `Tool Number "99999999" does not exist. (Message 12103)` | `tool list - herramienta inexistente 12103.png` |

El mismo error tiene **texto y número distintos** en Resource Setup (sin número) y en Tool
List (`16625` / `12103`). El estado de una herramienta se ve y se cambia en *Activity Manager →
Tool Number Maintenance* (Retrieve; el menú Status; Save escribe) o en *Activities → Change
Equipment Status Tool*, que según la WI de Costa Rica exige ser técnico de ingeniería,
ingeniero de manufactura o supervisor.

## 2. Line Clearance (apertura y cierre)

El MES revisa el Line Clearance en **Start, Assemble y DC Collect**, y todas las causas
comparten el número **`22501`** con **textos distintos**. Banner del POD, 🧪:

| Causa | Texto | Dónde se vio |
|---|---|---|
| Sin LC desde el último Resource Setup | `Line Clearance has not been done since the last Resource Setup, please perform Line Clearance` | Start 📸 `start - sin line clearance tras resource setup 22501.png`; Assemble y DC Collect (19/10) |
| Respuesta del LC = **NO** | `Line Clearance status is NO, please perform Line Clearance` | Start 📸 `start - line clearance en NO 22501.png` |
| El LC es de una Shop Order pero hay SFCs **activos de otras órdenes** en el recurso | `Line Clearance is setup for Shop Order <so> but there are still SFCs active for other shop orders, either place these SFCs back in queue or change the Line Clearance and process these SFCs first.` | Start 📸 `start - sfcs activos de otra orden en el recurso 22501.png` |
| SFC de una Shop Order **distinta** a la del LC | `The SFC(s) you are trying to process are not from shop order <so>, please perform Line Clearance` | Start 📸 `start - sfc de otra orden 22501.png` |
| **Tiempo vencido** desde el último LC | `Too much time has passed since last Line Clearance, please perform Line Clearance` | Assemble (sin captura en disco) |

Esto da mensaje a casi todas las condiciones de repetición del LC que dice la WI (SFC
activo, cambio de Shop Order, tiempo vencido). El **cambio de usuario** no se probó. Sigue
sin explicar el LC "de cierre" del simulador (W7).

**Responder No no da error en el LC:** el MES guarda la respuesta ("⚠ 1 DC Groups saved") y
el bloqueo aparece **después, en Start** (`status is NO`). 📸 `dc standalone - line
clearance NO se guarda sin aviso.png`.

**Hoy en el simulador:** el LC va por secuencia y no se revisa en Start, Assemble ni DC
Collect; responder No es intento fallido en el propio paso del LC.

## 3. Search / Start

**"SFC is missing" tiene dos números según el botón,** con el mismo texto (banner del POD):

| Botón | Número | Evidencia |
|---|---|---|
| Start, Complete | `22506` | 🧪 (Start, 19/10) · 📸 `complete - sin sfc 22506.png` |
| Assemble, DC Collect, Log NC, SFC Split | `12906` | 📸 `assembly - sin sfc 12906.png`, `dc collect - sin sfc 12906.png`, `log nc - sin sfc 12906.png` · 🧪 (Split, 19/10) |

**Con un SFC que sigue en cola (no iniciado)** en el campo, Assemble y Complete lo **vacían**
y dan el mismo "SFC is missing" (`12906` y `22506`). 📸 `assembly - sfc en cola sin iniciar
12906.png`, `complete - sfc en cola sin iniciar 22506.png`.

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Start con el campo SFC **vacío** | ✅ `22506` · `SFC is missing` (banner) | 🧪 (19/10) | NonSerialized: `SfcRequired`, SweetAlert "Campo requerido / Debes ingresar o escanear un SFC antes de iniciar.", sin intento. Serialized: `StartWholeOrder` ("Inicia la orden completa") |
| Start con un SFC que **no existe** | ✅ `22507` · `Invalid SFC` (banner). Con un panel abierto (NC Selection), el panel muestra `Messages (2):` con `15049` · `No valid SFCs meet the specified criteria` y `13005` · `SFC <sfc> does not exist`; con el panel de Component List, solo `13005` | 🧪📸 `start - sfc inexistente 22507 13005.png`, `start - sfc inexistente panel messages 22507 15049 13005.png` | NonSerialized: `SfcNotInOrder`, "SFC no encontrado / El número no corresponde a la orden de este recurso." (el código lo marca **provisional hasta tener el mensaje real**, spec 011). Serialized: `StartWholeOrder` |
| **Enter** en el campo SFC con un SFC inexistente | ✅ `13005` · `SFC <sfc> does not exist` en el panel, sin banner. Una medición anterior mostró un banner `13006`; no se repitió, tratarlo como no confirmado | 📸 `sfc - enter con sfc inexistente 13005.png` | Igual que arriba |
| SFC de una Shop Order **distinta** (existe, pero es de otra orden) | ✅ **`22501`**, no 22507: `The SFC(s) you are trying to process are not from shop order <so>, please perform Line Clearance` | 🧪📸 | Mismo `SfcNotInOrder` (en Serialized, `StartWholeOrder`) |
| SFC **ya activo en esta misma operación** | ✅ `13961` · `SFC <sfc> is not in queue at operation <op>. It is in work at <op>. (Message 13961)` | 🧪📸 `start - sfc ya activo en la operacion 13961.png` | No se simula |
| SFC **ya iniciado en otro recurso** | ❔ Se intentó (42812 activo en un recurso, Start en otro): los chequeos de LC salen antes y el mensaje propio no se alcanzó | 📝 CR y CO · 🧪 | No se simula |
| Start sin Line Clearance (cualquier causa) | `22501`, ver Line Clearance | 🧪📸 | No existe |

## 4. Assembly

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Barcode con formato inválido | ✅ `17150` · `2D barcode is not compliant with ISO 15434 format` (franja de la ventana; se despliegan Component Details y Assembly Data vacíos con [Add]) | 🧪 (19/10) | SweetAlert "Barcode inválido" cuando no es numérico, sin intento (AUD-69) |
| Componente **fuera de la Component List** (**componente no requerido**) | ✅ `13252` · `Component 600000005112/A is an invalid alternate for the component. (Message 13252)` (franja de la ventana, sin Component Details). 600000005112 es un material de otro BOM (el de la WI). Con el material del propio SFC dio el mismo `13252` | 🧪📸 `assembly - componente de otro bom 13252.png`, `assembly - componente fuera de la lista 13252.png` | No se simula; el barcode no se valida (solo secuencia) |
| Material **inexistente** | ✅ `12001` · `data is missing (Message 12001)` (misma franja; se probó `999999999999`) | 🧪📸 `assembly - material inexistente 12001.png` | No se simula |
| **Batch inválido** o inexistente en el almacén | ✅ `22801` · `Batch Number is Invalid or the batch doesn't exist in Storage location AL01 (Message 22801)`. CO dice `CS01`: el almacén cambia por sitio. Quién resuelve: Materiales/Almacén; el operador **avisa al encargado de materiales** del área para revisar el estado en SAP | 📷 `assembly - batch invalido 22801.png` | No se simula |
| **Cantidad insuficiente** en ERP | ✅ `22801` · `Not enough Quantity Available in Unrestricted Stock (Message 22801)`. Mismo número que el anterior, otro texto. Mismo responsable | 📷 `assembly - cantidad insuficiente 22801.png` | No se simula |
| Dato de ensamble que no corresponde al componente (WI) | ✅ `13251` · `Assembly data 0000018790 is invalid for data field INVENTORY_ID and component 600000005112,A (Message 13251)`. La WI pone esta figura bajo la nota de "componente no requerido", pero el componente sí se encontró y lo inválido es el Inventory ID | 📷 `assembly - inventory id invalido 13251.png` | No se simula |
| Material **sensible al tiempo expirado** | ❔ La operación probada (`O_OMNI_FIRST_DISTAL`) no tiene componentes sensibles al tiempo. Al abrir TSM Check In/Out (Activities) sobre un SFC sin ellos sale `15357` · `SFC <sfc> does not contain any time-sensitive components (Message 15357)` | 📝 CR y CO ("Error 3") · 🧪📸 `tsm - sfc sin componentes sensibles al tiempo 15357.png` | No se simula |
| Assemble sin SFC o con un SFC en cola | `12906`, ver Search / Start | 🧪📸 | No existe |
| Assemble sin LC (cualquier causa) | `22501`, ver Line Clearance | 🧪 | No existe |
| Return to POD con datos escritos | No es error: ventana del MES `Unsaved Data` · `Do you want to return to POD without saving record?` [OK] [Cancel] | 🧪 (19/10) | — |
| Time-Based Assembly (ranuras): `SAP MII transaction failed with message: Slot Configuration not found for Component -ItemBO:CR9A,300000257791, A (Message 10059)` y `…Not Enough QTY on the Slots for ITEM: … Available Slot Qty: 0 Required Slot Qty: 15 (Message 10059)` | ✅ solo texto de ejemplo | 📝 CR | Fuera de alcance: ningún área simulada usa Time-Based (W4); la WI manda a "Cargar/Reponer" |

Otros datos útiles de la WI: el MES **no permite sobreconsumir ni desechar componentes**
(se desecha la parte según 801061001 y se avisa al responsable de producción); y hacer
Check-out de un material con fecha de expiración detiene su reloj, lo que permitiría usar uno
expirado, y **no es aceptable**. De paso: tras Add, el MES muestra "Component has been
added" (`assembly - componente agregado exito.png`).

## 5. Data Collection

Todos los errores de DC salen como **error rojo en la franja dentro de la ventana Data
Collection Entry**, no como alerta. Probado en DC Collect por SFC (grupos
`DCGTM_FIRST_SHOT_QA_INSPECTION*` y `DCGTM_WELDING_MACHINE`) y en el DC Standalone del LC.

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Save con un parámetro **obligatorio vacío** | ✅ `10015` · `Required value "<param>" missing; enter a value in the <param> field`. En un grupo con varios vacíos **reporta solo uno**: el primer Data Field List obligatorio (`DC_RESULTADO_DE_INSP_FUNCIONAL`), no los Text vacíos que están antes | 🧪📸 `dc collect - grupo vacio obligatorio 10015.png`, `dc standalone - valor obligatorio vacio 10015.png` | Un campo vacío es intento fallido (excepción del formato) con mensaje genérico |
| Valor **que no está en la lista** (Data Field List) | ✅ `19973` · `Value "XYZ" does not exist for DC_RESULTADO_DE_INSP_FUNCIONAL; select a value from the list (Message 19973)` | 🧪📸 `dc collect - valor fuera de la lista 19973.png` | Intento fallido, mensaje genérico |
| Numérico con **texto** | ✅ `12205` · `Field DC_WELDING_MACHINE must contain a number (Message 12205)` | 🧪📸 `dc collect - numerico con texto 12205.png` | Intento fallido con mensaje genérico (el caso de texto en un campo numérico no se verificó) |
| Valor **sobre el límite superior** | ✅ `15233` · `Value 99999 is above the upper limit (Message 15233)` | 🧪📸 `dc collect - valor sobre el limite superior 15233.png` | Intento fallido, mensaje genérico; no hay mensaje de límites |
| Valor **bajo el límite inferior** | ✅ `15232` · `Value -99999 is below the lower limit (Message 15232)` | 🧪📸 `dc collect - valor bajo el limite inferior 15232.png` | Ídem |
| **Save Draft** con un valor fuera de rango | No es error: se acepta **sin validar**, con un check verde `Collected data was saved as a draft.` | 🧪📸 `dc collect - save draft acepta fuera de rango.png` | Save Draft existe pero no guarda nada (W16) |
| Return to POD con datos sin guardar | No es error: ventana del MES `Unsaved Data` · `Do you want to return to POD without saving record?` [OK] [Cancel] | 🧪📸 `dc collect - return to pod con datos sin guardar.png` | — |
| DC Collect sin SFC | `12906`, ver Search / Start | 🧪📸 | No existe |
| DC Collect sin LC (cualquier causa) | `22501`, ver Line Clearance | 🧪 | No existe |
| Grupos obligatorios pendientes | Sale al **Complete**: `15246`, ver Complete | 🧪📸 | El paso de DC no termina hasta guardar todos los grupos obligatorios |

La pantalla de captura **no muestra los límites**, solo `Data Type: Numeric`: el operador
los conoce por el MPI. En el DC Standalone, el valor "inesperado" de un Boolean (NO) se guarda
sin alerta y el bloqueo sale después, en Start (ver Line Clearance).

## 6. SFC Split (solo guías NonSerialized)

Todos los mensajes **reemplazan el formulario** con un [OK], y el OK **cierra la ventana
entera** (hay que reabrir SFC Split; AUD-64). Los 4 son 🧪 (19/10):

| Situación | Mensaje | Hoy en el simulador |
|---|---|---|
| Split sin SFC seleccionado | `12906` · `SFC is missing` (banner del POD) | `SelectRowToSplit`: SweetAlert en español |
| Qty vacía | `13875` · `Define Qty to Split` | Lo bloquea el control ("No se pudo dividir") |
| Qty mayor a la disponible | `13885` · `Requested qty to split <n> is greater than the available SFC qty <m>` | `InvalidSplit` |
| Qty 0 | `13213` · `Quantity must be greater than 0` | `InvalidSplit` |

Una cantidad **válida pero equivocada** (la de Moldeo, spec 014 enmienda 3) **no produce
error del MES**: es un error de procedimiento y el simulador lo califica con el mensaje
genérico.

## 7. Log NC

Los errores salen en la **franja de la ventana NC Data Entry** y no registran ningún NC. 📸:

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| **Done** con el NC Code vacío | ✅ `18117` · `No NC codes logged (Message 18117)` | 🧪📸 `log nc - done sin nc code 18117.png` | Done es botón equivocado: intento fallido sin este mensaje (spec 007) |
| **Add** con un NC Code inexistente | ✅ `12901` · `Input value "NC_CODE" is invalid ("XYZ") (Message 12901)` | 🧪📸 `log nc - nc code inexistente 12901.png` | No verificado cómo trata el control un NC Code escrito a mano |
| Botón Log NC **sin SFC** | ✅ `12906` · `SFC is missing` (banner del POD) | 🧪📸 `log nc - sin sfc 12906.png` | Sin fila seleccionada es intento fallido |

Los fallos de **decisión** (otra pieza, otro código válido, otro método) no tienen mensaje:
el MES los deja pasar y el simulador los califica contra el hallazgo. Éxitos conocidos: "All
of SFC Number scrapped", y las confirmaciones de remover o agregar componente y de la
operación devuelta. La WI de Colorado agrega un requisito del scrap rápido: el SFC debe estar
completamente ensamblado en MES (no dice qué mensaje sale si no lo está).

## 8. Complete y Logout

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Complete con **grupos de DC obligatorios sin recolectar** | ✅ `15246` · `There are missing required values for data collection parameter(s) <PARAM> (<DC_GROUP>) , <PARAM> (<DC_GROUP>) , … for SFC "<sfc>" (Message 15246)`. Banner **larguísimo**: lista todos los parámetros faltantes de todos los grupos (la sesión midió 7.729 caracteres con 19 grupos; la captura solo muestra el inicio). Patrón: `<PARAM> (<DC_GROUP>)` separados por ` , ` | 🧪📸 `complete - dc obligatorio sin recolectar 15246.png` | Complete antes del paso de DC es un botón fuera de secuencia: intento fallido en Assessment, ignorado en Guided |
| Complete sin SFC o con un SFC en cola | `22506` · `SFC is missing`; con uno en cola, el POD **vacía el campo** | 🧪📸 `complete - sin sfc 22506.png`, `complete - sfc en cola sin iniciar 22506.png` | `NoRowToComplete`: "Seleccione todas las filas para completar" |

Colorado menciona un error que no es de la guía: ajustar la cantidad de la orden antes de
completar las unidades construidas da error al completar. Logout: sin errores (página "You
have logged off").

---

## Pendiente de capturar

Lo que sigue ❔. Guardar la captura en `Screens SAP MES REAL/Errores/<paso> - <caso> <número>.png`.

| Caso | Qué falta |
|---|---|
| Resource Setup: herramienta en **otro recurso**, **expirada** o caída; Start sin setup | Guardar un setup completo: `TG_HEATER_BLOCK` no tiene herramientas habilitadas en `O_OMNI_FIRST_DISTAL`, así que hace falta otra operación/recurso o habilitar una herramienta (*Tool Number Maintenance*, que escribe y puede exigir permiso). Un recurso sin setup para el Start |
| Resource Setup **Standard** (el que modela el simulador) | Un recurso del testing configurado con el Standard, o confirmar con el usuario que los textos del Custom valen |
| SFC ya iniciado en **otro** recurso (mensaje propio) | Pasar antes los chequeos de LC. El usuario de testing **no puede** hacer `Place back in queue` (`Permissions or settings are required to run activity PR520 (Message 16034)`, 📸 `place back in queue - sin permiso PR520 16034.png`): hace falta otro usuario con ese permiso, o dos recursos con LC compatibles |
| Material sensible al tiempo **expirado** | Una operación con componentes TSM y una etiqueta vencida; `O_OMNI_FIRST_DISTAL` no las tiene |
| `Too much time has passed since last Line Clearance` | Solo hay texto, falta la captura |
| LC por **cambio de usuario** | No probado |

## Escrituras hechas en testing (2026-10-20)

Reportadas por la sesión que hizo las capturas, con el permiso del usuario:

- `R_OMNI_FIRST_DISTAL_2`: Line Clearance `DC_LC_DONE = SI` (11:38).
- `R_OMNI_FIRST_DISTAL_1`: `DC_LC_DONE = NO` (11:46) y después `SI`. Su LC quedó asociado a la
  Shop Order `303627317`, y en ese recurso **sigue activo el SFC `300014142061-43000`** (Shop
  Order `303627318`). El intento de `Place back in queue` falló por permisos (`16034`).
- Segunda tanda: un **Save Draft** en `DCGTM_WELDING_MACHINE` con `-99999` para el SFC
  `42812` (`R_OMNI_FIRST_DISTAL_2`). Nada más se guardó: todos los Save fallaron por
  validación y no se registró ningún NC.

- Resource Setup (Custom, `R_OMNI_FIRST_DISTAL_1`): *Associate New ShopOrder* asoció la Shop
  Order `303627317` (ya era la misma). Todos los Save fallaron por validación: no se guardó
  ninguna herramienta. En Tool List no se registró ninguna herramienta.

Es estado que quedó en un ambiente compartido: conviene que el usuario decida cómo
restablecer el LC y el SFC 43000 (con un usuario que tenga el permiso PR520).

## Observaciones para la spec 017

1. **Dos clases de fallo.** Los hay con mensaje del MES (formato de SFC, batch, cantidad, DC,
   Line Clearance, NC Code) y los hay de **procedimiento, sin mensaje** (código NC o
   cantidad de split equivocados, herramienta que existe pero no es la de la estación). El
   simulador califica ambos. La 017 solo debería sumar la primera clase; la segunda conserva
   el mensaje genérico de Assessment, que no revela la respuesta.
2. **El clic fuera de secuencia sí tiene mensaje real.** Assemble o Complete con un SFC que
   no se inició dan `SFC is missing`; Start sin Line Clearance da `22501`; Complete con DC
   pendiente da `15246`. Hoy el simulador responde a todo eso con el aviso genérico (o lo
   ignora en Guided). La 017 puede mostrar el mensaje real en cada caso sin cambiar la
   calificación.
3. **Line Clearance: el MES lo revisa en otro lugar y de otra forma.** Lo revisan **Start,
   Assemble y DC Collect**, no el propio paso del LC: responder No se guarda sin error y el
   bloqueo sale en Start. Y el chequeo del LC va **antes** que los del SFC, así que el
   operador ve primero los `22501`. El simulador hoy exige el LC por secuencia y marca el No
   como intento fallido en su paso: decidir si la 017 lo mueve a Start.
4. **Un `22501` no es un solo mensaje:** tiene al menos 5 textos según la causa. Usar el
   **texto** como clave, no el número. Pasa igual con `22801` (batch y cantidad) y con `SFC
   is missing` (`22506` o `12906` según el botón).
5. **El SFC de otra orden da `22501`, no `22507`.** El `22507` (con `13005`) es solo de un SFC
   que **no existe**. La fila AUD-63 de la auditoría de fidelidad propone `SfcNotInOrder →
   22507/13005`: según estas capturas, eso corresponde al SFC inexistente, y el de otra orden
   necesita el `22501`. Avisar a quien lleve esa auditoría.
6. **El DC fuera de límites es error que bloquea el Save** (`15232`/`15233`), no alerta;
   Save Draft no valida nada. La pantalla no muestra los límites. El MES reporta **un solo**
   parámetro vacío por Save (`10015`), mientras el Complete lista **todos** los que faltan
   (`15246`).
7. **El almacén del batch cambia por sitio** (`AL01` en Costa Rica, `CS01` en Colorado); el
   testing es CR9A, así que corresponde `AL01`.
8. **El responsable.** En los dos errores 22801 la WI manda **avisar al encargado de
   materiales**: es contenido de comprensión (EP-7) que el mensaje podría reforzar.
9. **El mecanismo de mostrar errores** (banner y franja rojos en inglés, el OK que cierra
   Split) es la AUD-63/64/69 de la auditoría de fidelidad, pendiente de aprobación. La 017
   depende de esa variante visual: definir si la construye ella o la auditoría.
10. **Time-Based Assembly** (10059) queda fuera mientras ningún área lo use.
11. **Resource Setup: los textos capturados son del Custom, el simulador modela el Standard.**
    El Custom precarga los grupos de herramientas de la operación y valida "otro grupo" e
    "inexistente" al salir del campo; el Standard recibe filas libres y no conoce los grupos.
    Si la 017 usa estos mensajes, hay que decidir si el simulador gana grupos de herramientas
    por área (la idea de W5) o si basta con el aviso "faltan herramientas". Los mensajes del
    Custom no llevan número; los de Tool List sí (`16625`, `12103`).
