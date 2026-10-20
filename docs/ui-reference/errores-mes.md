# Errores del MES real por paso de la guía

> **Para qué:** insumo de la **spec 017** (fila 10 de la Fase 8.5; EP-4 y EP-5). Reúne, paso
> por paso, los errores que el MES real muestra al operador: el **texto exacto** cuando se
> conoce, dónde se vio y qué hace hoy el simulador en ese caso. Sin captura o texto real no
> se inventa UI (`docs/process.md` §8): lo que falta está en "Pendiente de capturar".
> **Es registro de consulta:** no cambia nada por sí solo; lo que se decida implementar va a
> la spec o al plan.
> **Hecho el 2026-10-20** en dos pasadas: contraste con las WI (sesión de contraste) y
> capturas en el ambiente de testing el mismo día (otra sesión de Claude con Chrome, con
> permiso del usuario; ver "Escrituras hechas en testing").

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
| Componente no requerido | 🟡 Probable `Component <c>/A is an invalid alternate for the component. (Message 13252)`; se probó con el material del propio SFC, no con uno de otro BOM | 🧪📸 |
| SFC iniciado en otro recurso | ❔ El MES responde antes con los avisos de Line Clearance (`22501`); el mensaje propio no se alcanzó | 🧪📸 |
| Material sensible al tiempo expirado | ❔ La operación probada no tiene componentes sensibles al tiempo; solo salió `15357` al abrir TSM Check In/Out | 🧪📸 |
| DC fuera de límites | ❔ DC Collect pide firma electrónica (contraseña) antes de los parámetros; no se pudo. Los sitios no coinciden (alerta o error) | 🧪📸 · 📝 |

Más los 4 de Resource Setup (EP-4): ninguno tiene texto; la ventana es una emergente que la
extensión del navegador no controla. En cambio, el testing dio **más mensajes que los
pedidos** (Line Clearance, Start, Assemble, DC y Split): están en cada paso.

---

## 0. Ingreso a MES

Sin errores del MES documentados. Las WI no describen fallos del login de MES. El
`ERROR: The process "…" not found` del script de limpieza es normal (`pre-sap-flow.md`). Los
mensajes de login que traen las WI ("Debe llenar el campo de usuario", "Usuario o Contraseña
Inválida") son de la herramienta de Shelf Life, no de este paso.

## 1. Resource Setup

La WI de Costa Rica y la de Colorado solo dicen "el sistema mostrará un error". Es una app de
SAP MII en una **ventana emergente aparte**: ni la auditoría del 19 ni la sesión con Chrome
del 20 pudieron medirla o controlarla.

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Start antes de completar Resource Setup | ❔ No se probó: no hubo un recurso sin setup. Sin Line Clearance desde el último setup sale `22501` (ver Line Clearance) | 📝 CR y CO | Aviso `ResourceSetupRequired` ("Debes completar Resource Setup antes de iniciar."), sin intento; en Assessment el click fuera de secuencia es intento fallido |
| Herramienta ya configurada en **otro recurso** | ❔ | 📝 CR y CO | No se simula: acepta valores libres (decisión vigente) |
| **Menos** herramientas que las requeridas (CO: también más) | ❔ | 📝 CR y CO | No se simula; solo se exige ≥ 1 Tool Number (W3) |
| Herramienta de **otro grupo** (un micrómetro en vez de una regla) o equivocada | ❔ | 📝 CR y CO | No se simula; haría falta un catálogo de herramientas por área (W5) |
| Herramienta **expirada** o no disponible (estado Unscheduled Down o Scheduled Down, según CO) | ❔ | 📝 CR y CO | No se simula |

Éxito (no es error): "Se mostrará un mensaje que indica que se aplicó el cambio"; la
captura `record changed correcto.png` ya está en el repo.

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

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Start con el campo SFC **vacío** | ✅ `22506` · `SFC is missing` (banner) | 🧪 (19/10) | NonSerialized: `SfcRequired`, SweetAlert "Campo requerido / Debes ingresar o escanear un SFC antes de iniciar.", sin intento. Serialized: `StartWholeOrder` ("Inicia la orden completa") |
| Start con un SFC que **no existe** | ✅ `22507` · `Invalid SFC` (banner) y, en el panel Component List, `13005` · `SFC <sfc> does not exist`. La medición del 19 vio además `15049` · `No valid SFCs meet the specified criteria`. Con Enter en el campo SFC el banner dice `13006` · `SFC <sfc> does not exist` (sin captura limpia) | 🧪📸 `start - sfc inexistente 22507 13005.png` | NonSerialized: `SfcNotInOrder`, "SFC no encontrado / El número no corresponde a la orden de este recurso." (el código lo marca **provisional hasta tener el mensaje real**, spec 011). Serialized: `StartWholeOrder` |
| SFC de una Shop Order **distinta** (existe, pero es de otra orden) | ✅ **`22501`**, no 22507: `The SFC(s) you are trying to process are not from shop order <so>, please perform Line Clearance` | 🧪📸 | Mismo `SfcNotInOrder` (en Serialized, `StartWholeOrder`) |
| SFC **ya activo en esta misma operación** | ✅ `13961` · `SFC <sfc> is not in queue at operation <op>. It is in work at <op>. (Message 13961)` | 🧪📸 `start - sfc ya activo en la operacion 13961.png` | No se simula |
| SFC **ya iniciado en otro recurso** | ❔ Se intentó (42812 activo en un recurso, Start en otro): los chequeos de LC salen antes y el mensaje propio no se alcanzó | 📝 CR y CO · 🧪 | No se simula |
| Start sin Line Clearance (cualquier causa) | `22501`, ver Line Clearance | 🧪📸 | No existe |

## 4. Assembly

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Barcode con formato inválido | ✅ `17150` · `2D barcode is not compliant with ISO 15434 format` (franja de la ventana; se despliegan Component Details y Assembly Data vacíos con [Add]) | 🧪 (19/10) | SweetAlert "Barcode inválido" cuando no es numérico, sin intento (AUD-69) |
| Componente **fuera de la Component List** | 🟡 `13252` · `Component 300014142061/A is an invalid alternate for the component. (Message 13252)` (franja de la ventana, sin Component Details). **Cuidado:** se escribió en Find component el material del propio SFC, no un componente de otro BOM; es el candidato más cercano a "componente no requerido", sin confirmar | 🧪📸 `assembly - componente fuera de la lista 13252.png` | No se simula; el barcode no se valida (solo secuencia) |
| Material **inexistente** | ✅ `12001` · `data is missing (Message 12001)` (misma franja; se probó `999999999999`) | 🧪📸 `assembly - material inexistente 12001.png` | No se simula |
| **Batch inválido** o inexistente en el almacén | ✅ `22801` · `Batch Number is Invalid or the batch doesn't exist in Storage location AL01 (Message 22801)`. CO dice `CS01`: el almacén cambia por sitio. Quién resuelve: Materiales/Almacén; el operador **avisa al encargado de materiales** del área para revisar el estado en SAP | 📷 `assembly - batch invalido 22801.png` | No se simula |
| **Cantidad insuficiente** en ERP | ✅ `22801` · `Not enough Quantity Available in Unrestricted Stock (Message 22801)`. Mismo número que el anterior, otro texto. Mismo responsable | 📷 `assembly - cantidad insuficiente 22801.png` | No se simula |
| Dato de ensamble que no corresponde al componente (WI) | ✅ `13251` · `Assembly data 0000018790 is invalid for data field INVENTORY_ID and component 600000005112,A (Message 13251)`. La WI pone esta figura bajo la nota de "componente no requerido", pero el componente sí se encontró y lo inválido es el Inventory ID | 📷 `assembly - inventory id invalido 13251.png` | No se simula |
| Material **sensible al tiempo expirado** | ❔ La operación probada (`O_OMNI_FIRST_DISTAL`) no tiene componentes sensibles al tiempo. Al abrir TSM Check In/Out (Activities) sobre un SFC sin ellos sale `15357` · `SFC <sfc> does not contain any time-sensitive components (Message 15357)` | 📝 CR y CO ("Error 3") · 🧪📸 `tsm - sfc sin componentes sensibles al tiempo 15357.png` | No se simula |
| Assemble sin LC (cualquier causa) | `22501`, ver Line Clearance | 🧪 | No existe |
| Time-Based Assembly (ranuras): `SAP MII transaction failed with message: Slot Configuration not found for Component -ItemBO:CR9A,300000257791, A (Message 10059)` y `…Not Enough QTY on the Slots for ITEM: … Available Slot Qty: 0 Required Slot Qty: 15 (Message 10059)` | ✅ solo texto de ejemplo | 📝 CR | Fuera de alcance: ningún área simulada usa Time-Based (W4); la WI manda a "Cargar/Reponer" |

Otros datos útiles de la WI: el MES **no permite sobreconsumir ni desechar componentes**
(se desecha la parte según 801061001 y se avisa al responsable de producción); y hacer
Check-out de un material con fecha de expiración detiene su reloj, lo que permitiría usar uno
expirado, y **no es aceptable**. De paso: tras Add, el MES muestra "Component has been
added" (`assembly - componente agregado exito.png`).

## 5. Data Collection

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Save sin responder un parámetro **obligatorio** | ✅ `10015` · `Required value "<param>" missing; enter a value in the <param> field` (franja de Data Collection Entry, probado con `DC_LC_DONE` en el DC Standalone) | 🧪📸 `dc standalone - valor obligatorio vacio 10015.png` | Un campo vacío es intento fallido (excepción del formato) con mensaje genérico |
| **Firma electrónica** al abrir un grupo de DC Collect | No es error: `Data Collection Authentication` (Site, User ID prellenado, Password, [OK] [Cancel]) **antes** de mostrar los parámetros. Es de esta workstation (`_ESIG`) | 🧪📸 `dc collect - firma electronica al abrir el grupo.png` | Verificar si el simulador modela esta ventana |
| DC **no completada con los valores esperados** | ❔ No se pudo: la firma pide contraseña | 📝 CR y CO | Valor fuera de rango = intento fallido, mensaje genérico |
| Valor **fuera de los límites** del MPI | ❔ No se pudo. **Los sitios no coinciden:** CR dice que el sistema "activará una **alerta**"; CO dice que "an **error** will occur" | 📝 CR y CO | Intento fallido, mensaje genérico; no hay alerta de límites |
| DC obligatoria **pendiente** al Complete | ❔ No se pudo (la WI no dice si sale al guardar o al Complete) | 📝 CR y CO | El paso de DC no termina hasta guardar todos los grupos obligatorios |
| DC Collect sin LC (cualquier causa) | `22501`, ver Line Clearance | 🧪 | No existe |

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

Sin mensajes de error del MES en las WI ni en testing: solo confirmaciones ("All of SFC Number
scrapped", la de remover componente, la de agregar, la de la operación devuelta). Los fallos
de este paso son de **decisión** (otra pieza, otro código, otro método): el MES los deja
pasar y el simulador los califica contra el hallazgo. La WI de Colorado agrega un requisito
del scrap rápido: el SFC debe estar completamente ensamblado en MES (no dice qué mensaje
sale si no lo está).

## 8. Complete y Logout

Complete: ningún mensaje de error propio en las WI. Colorado menciona uno que no es de la
guía: ajustar la cantidad de la orden antes de completar las unidades construidas da error
al completar. Logout: sin errores (página "You have logged off").

---

## Pendiente de capturar

Lo que sigue ❔, con lo que cada caso necesita. Guardar la captura en
`Screens SAP MES REAL/Errores/<paso> - <caso> <número>.png`.

| Caso | Qué falta |
|---|---|
| Resource Setup: herramienta en dos recursos, de menos, de otro grupo, expirada o caída; Start sin setup | La ventana es una emergente fuera del grupo de pestañas de la extensión: captura **manual** del usuario o control de escritorio. Además, un recurso sin setup |
| SFC ya iniciado en **otro** recurso (mensaje propio) | Pasar antes los chequeos de LC: dejar el LC del recurso destino en SI para la misma Shop Order y sin SFCs activos de otras órdenes (`Place back in queue` del 43000, ver abajo) |
| Material sensible al tiempo **expirado** | Una operación con componentes TSM y una etiqueta vencida; `O_OMNI_FIRST_DISTAL` no las tiene |
| DC: valor fuera de límites, valor inesperado, grupo obligatorio pendiente al Complete | La **contraseña** de la firma electrónica, que la escribe el usuario (o captura manual) |
| Textos sin captura limpia | `13006` (Enter en el campo SFC con un SFC inexistente) y `Too much time has passed since last Line Clearance` (22501) |
| LC por **cambio de usuario** | No probado |

## Escrituras hechas en testing (2026-10-20)

Reportadas por la sesión que hizo las capturas, con el permiso del usuario:

- `R_OMNI_FIRST_DISTAL_2`: Line Clearance `DC_LC_DONE = SI` (11:38).
- `R_OMNI_FIRST_DISTAL_1`: `DC_LC_DONE = NO` (11:46) y después `SI`. Su LC quedó asociado a la
  Shop Order `303627317`, y en ese recurso **sigue activo el SFC `300014142061-43000`** (Shop
  Order `303627318`).

Es estado que quedó en un ambiente compartido: conviene que el usuario decida si se hace
`Place back in queue` del 43000 y se restablece el LC.

## Observaciones para la spec 017

1. **Dos clases de fallo.** Los hay con mensaje del MES (formato de SFC, batch, cantidad, DC
   obligatoria, Line Clearance) y los hay de **procedimiento, sin mensaje** (código NC o
   cantidad de split equivocados, herramienta que existe pero no es la de la estación). El
   simulador califica ambos. La 017 solo debería sumar la primera clase; la segunda conserva
   el mensaje genérico de Assessment, que no revela la respuesta.
2. **Line Clearance: el MES lo revisa en otro lugar y de otra forma.** Lo revisan **Start,
   Assemble y DC Collect**, no el propio paso del LC: responder No se guarda sin error y el
   bloqueo sale en Start. Y el chequeo del LC va **antes** que los del SFC, así que el
   operador ve primero los `22501`. El simulador hoy exige el LC por secuencia y marca el No
   como intento fallido en su paso: decidir si la 017 lo mueve a Start.
3. **Un `22501` no es un solo mensaje:** tiene al menos 5 textos según la causa (ver Line
   Clearance). Usar el **texto** como clave, no el número. Pasa igual con `22801` (batch y
   cantidad).
4. **El SFC de otra orden da `22501`, no `22507`.** El `22507` (con `13005`) es solo de un SFC
   que **no existe**. La fila AUD-63 de la auditoría de fidelidad propone `SfcNotInOrder →
   22507/13005`: según estas capturas, eso corresponde al SFC inexistente, y el de otra orden
   necesita el `22501`. Avisar a quien lleve esa auditoría.
5. **"Componente no requerido" es `13252` solo de forma probable.** Probarlo con un
   componente de otro BOM lo confirmaría; mientras, no conviene darlo por cerrado.
6. **El almacén del batch cambia por sitio** (`AL01` en Costa Rica, `CS01` en Colorado); el
   testing es CR9A, así que corresponde `AL01`.
7. **El responsable.** En los dos errores 22801 la WI manda **avisar al encargado de
   materiales**: es contenido de comprensión (EP-7) que el mensaje podría reforzar.
8. **El mecanismo de mostrar errores** (banner y franja rojos en inglés, el OK que cierra
   Split) es la AUD-63/64/69 de la auditoría de fidelidad, pendiente de aprobación. La 017
   depende de esa variante visual: definir si la construye ella o la auditoría.
9. **Firma electrónica de DC.** En esta workstation DC Collect pide contraseña antes de los
   parámetros. Verificar si el simulador la modela; si no, es una pantalla que falta.
10. **Time-Based Assembly** (10059) queda fuera mientras ningún área lo use.
