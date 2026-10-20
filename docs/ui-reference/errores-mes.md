# Errores del MES real por paso de la guía

> **Para qué:** insumo de la **spec 017** (fila 10 de la Fase 8.5; EP-4 y EP-5). Reúne, paso
> por paso, los errores que el MES real muestra al operador: el **texto exacto** cuando se
> conoce, dónde se vio y qué hace hoy el simulador en ese caso. Sin captura o texto real no
> se inventa UI (`docs/process.md` §8): lo que falta está en "Pendiente de capturar".
> **Es registro de consulta:** no cambia nada por sí solo; lo que se decida implementar va a
> la spec o al plan. Hecho el 2026-10-20 (sesión de contraste con las WI).

## Fuentes y cómo leer la evidencia

| Marca | Fuente |
|---|---|
| 🧪 **Testing** | Mensaje medido en el ambiente de testing el 2026-10-19 (sitio CR9A). Texto y número exactos: `estilos-y-mensajes-mes.md` (rama `docs/fidelidad-ui`, aún sin integrar a `TrainingApp`) |
| 📷 **WI-CR** | Figura de la WI de Costa Rica QD001001524 (`.docx` en español). Las 4 figuras usadas están en `Screens SAP MES REAL/Errores/` |
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
| Componente no requerido | 🟡 La WI lo nombra y pone debajo una figura con otro mensaje (13251, ver Assembly) | 📷 dudosa |
| SFC iniciado en otro recurso | ❔ solo la situación | 📝 |
| Material sensible al tiempo expirado | ❔ solo la situación | 📝 |
| DC fuera de límites | ❔ solo la situación; los sitios no coinciden (alerta o error) | 📝 |

Más los 4 de Resource Setup (EP-4): ninguno tiene texto. De los 11 mensajes que midió la
auditoría de fidelidad en testing, ninguno es de la fila 10: son de Start, Split, Assemble
y DC.

---

## 0. Ingreso a MES

Sin errores del MES documentados. Las WI no describen fallos del login de MES. El
`ERROR: The process "…" not found` del script de limpieza es normal (`pre-sap-flow.md`). Los
mensajes de login que traen las WI ("Debe llenar el campo de usuario", "Usuario o Contraseña
Inválida") son de la herramienta de Shelf Life, no de este paso.

## 1. Resource Setup

La WI de Costa Rica y la de Colorado solo dicen "el sistema mostrará un error". Es una app de
SAP MII en ventana emergente aparte que la auditoría de fidelidad **no midió** (la omitió el
usuario).

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Start de una Shop Order o SFC antes de completar Resource Setup | ❔ | 📝 CR y CO | Aviso `ResourceSetupRequired` ("Debes completar Resource Setup antes de iniciar."), sin intento; en Assessment el click fuera de secuencia es intento fallido |
| Herramienta ya configurada en **otro recurso** | ❔ | 📝 CR y CO | No se simula: acepta valores libres (decisión vigente) |
| **Menos** herramientas que las requeridas (CO: también más) | ❔ | 📝 CR y CO | No se simula; solo se exige ≥ 1 Tool Number (W3) |
| Herramienta de **otro grupo** (un micrómetro en vez de una regla) o equivocada | ❔ | 📝 CR y CO | No se simula; haría falta un catálogo de herramientas por área (W5) |
| Herramienta **expirada** o no disponible (estado Unscheduled Down o Scheduled Down, según CO) | ❔ | 📝 CR y CO | No se simula |

Éxito (no es error): "Se mostrará un mensaje que indica que se aplicó el cambio"; la
captura `record changed correcto.png` ya está en el repo.

## 2. Line Clearance (apertura y cierre)

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| **Assemble o DC Collect** sin Line Clearance desde el último Resource Setup | ✅ `22501` · `Line Clearance has not been done since the last Resource Setup, please perform Line Clearance` (banner del POD) | 🧪 | No existe: el simulador no revisa el LC en Assemble ni en DC; el LC va por secuencia |
| LC pendiente tras un cambio de Resource Setup (CO: "or an error will occur") | Probablemente el mismo 22501; la WI no da texto | 📝 CO | Ídem |
| Repetir el LC (cambio de usuario o de Shop Order, SFC activo, tiempo vencido) | ❔ | 📝 CR | No se simula (W7, EP-8 siguen abiertas) |

Responder **No** al LC no es un error del MES que se conozca: es un error de procedimiento
(la WI exige Sí). El simulador lo cuenta como intento fallido.

## 3. Search / Start

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Start con el campo SFC **vacío** | ✅ `22506` · `SFC is missing` (banner) | 🧪 | NonSerialized: `SfcRequired`, SweetAlert "Campo requerido / Debes ingresar o escanear un SFC antes de iniciar.", sin intento. Serialized: `StartWholeOrder` ("Inicia la orden completa") |
| Start con un SFC que **no existe** | ✅ `22507` · `Invalid SFC` (banner) más, en `Messages (2):` del panel, `15049` · `No valid SFCs meet the specified criteria` y `13005` · `SFC <sfc> does not exist` | 🧪 | NonSerialized: `SfcNotInOrder`, "SFC no encontrado / El número no corresponde a la orden de este recurso." (el código lo marca **provisional hasta tener el mensaje real**, spec 011). Serialized: `StartWholeOrder` |
| Start de un SFC o Shop Order **distinta a la del Resource Setup** (existe, pero es de otra orden) | ❔ ("el sistema dará un error") | 📝 CR y CO | Mismo aviso que arriba (`SfcNotInOrder`; en Serialized, `StartWholeOrder`) |
| SFC **ya iniciado en otro recurso** | ❔ | 📝 CR y CO (CO: "in Standard Flow route") | No se simula |
| Start antes de Resource Setup | ❔ (ver Resource Setup) | 📝 | Ver Resource Setup |

## 4. Assembly

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Barcode con formato inválido | ✅ `17150` · `2D barcode is not compliant with ISO 15434 format` (franja de la ventana; se despliegan Component Details y Assembly Data vacíos con [Add]) | 🧪 | SweetAlert "Barcode inválido" cuando no es numérico, sin intento (AUD-69) |
| **Batch inválido** o inexistente en el almacén | ✅ `22801` · `Batch Number is Invalid or the batch doesn't exist in Storage location AL01 (Message 22801)`. CO dice `CS01`: el almacén cambia por sitio. Quién resuelve: Materiales/Almacén; el operador **avisa al encargado de materiales** del área para revisar el estado en SAP | 📷 `assembly - batch invalido 22801.png` | No se simula; el barcode no se valida (solo secuencia) |
| **Cantidad insuficiente** en ERP | ✅ `22801` · `Not enough Quantity Available in Unrestricted Stock (Message 22801)`. Mismo número que el anterior, otro texto. Mismo responsable | 📷 `assembly - cantidad insuficiente 22801.png` | No se simula |
| Escanear un componente que **no se requiere** en la operación | ❔ La nota de la WI de Costa Rica va seguida de una figura con otro mensaje: `13251` · `Assembly data 0000018790 is invalid for data field INVENTORY_ID and component 600000005112,A (Message 13251)`; el componente sí se encontró y lo inválido es el Inventory ID. Parece el error de un dato de ensamble que no corresponde al componente, no el de "componente no requerido" | 📷 dudosa `assembly - inventory id invalido 13251.png` | No se simula |
| Material **sensible al tiempo expirado** | ❔ | 📝 CR y CO ("Error 3") | No se simula |
| Assemble sin LC desde el último Resource Setup | `22501` (ver Line Clearance) | 🧪 | No existe |
| Time-Based Assembly (ranuras): `SAP MII transaction failed with message: Slot Configuration not found for Component -ItemBO:CR9A,300000257791, A (Message 10059)` y `…Not Enough QTY on the Slots for ITEM: … Available Slot Qty: 0 Required Slot Qty: 15 (Message 10059)` | ✅ solo texto de ejemplo | 📝 CR | Fuera de alcance: ningún área simulada usa Time-Based (W4); la WI manda a "Cargar/Reponer" |

Otros datos útiles de la WI: el MES **no permite sobreconsumir ni desechar componentes**
(se desecha la parte según 801061001 y se avisa al responsable de producción); y hacer
Check-out de un material con fecha de expiración detiene su reloj, lo que permitiría usar uno
expirado, y **no es aceptable**. De paso: tras Add, el MES muestra "Component has been
added" (`assembly - componente agregado exito.png`).

## 5. Data Collection

| Situación | Mensaje | Evidencia | Hoy en el simulador |
|---|---|---|---|
| Save sin responder un parámetro **obligatorio** | ✅ `10015` · `Required value "<param>" missing; enter a value in the <param> field` (franja de Data Collection Entry) | 🧪 | Un campo vacío es intento fallido (excepción del formato) con mensaje genérico |
| DC **no completada con los valores esperados** | ❔ | 📝 CR y CO | Valor fuera de rango = intento fallido, mensaje genérico |
| Valor **fuera de los límites** del MPI | ❔ **Los sitios no coinciden:** CR dice que el sistema "activará una **alerta**"; CO dice que "an **error** will occur" | 📝 CR y CO | Intento fallido, mensaje genérico; no hay alerta de límites |
| DC obligatoria **pendiente** | ❔ (la WI no dice si sale al guardar o al Complete) | 📝 CR y CO | El paso de DC no termina hasta guardar todos los grupos obligatorios |
| DC Collect sin LC desde el último Resource Setup | `22501` (ver Line Clearance) | 🧪 | No existe |

## 6. SFC Split (solo guías NonSerialized)

Todos los mensajes **reemplazan el formulario** con un [OK], y el OK **cierra la ventana
entera** (hay que reabrir SFC Split; AUD-64). Los 4 son 🧪:

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

## Pendiente de capturar (T9, ambiente de testing)

Todo lo ❔ de arriba. Cada caso necesita **el texto exacto y su número** (franja o banner);
guardar la captura en `Screens SAP MES REAL/Errores/<paso> - <caso> <número>.png`.

| Caso | Cómo provocarlo | Escribe datos en testing |
|---|---|---|
| Resource Setup: herramienta en dos recursos, de menos, de otro grupo, expirada o caída | Guardar el Resource Setup con cada combinación | Sí (cambia el setup del recurso) |
| Start sin Resource Setup, de otra orden, de un SFC ya iniciado en otro recurso | Start en un recurso sin setup; con un SFC ajeno; con el mismo SFC en dos recursos | Sí (Start inicia el SFC) |
| Assemble: componente no requerido y material sensible al tiempo expirado | Escanear la etiqueta de un componente fuera del BOM; una de un material vencido | No si el MES rechaza (si acepta, consume) |
| DC: valor fuera de límites, valor inesperado, grupo obligatorio pendiente al Complete | Capturar un valor fuera de rango; dejar un grupo sin guardar y presionar Complete | Sí al guardar |
| LC: ¿Start también se bloquea sin LC? | Start tras cambiar el Resource Setup, sin LC | Sí |

La auditoría de fidelidad solo guardó un Line Clearance en testing, autorizado por el
usuario: estos casos también necesitan su autorización antes de escribir.

## Observaciones para la spec 017

1. **Dos clases de fallo.** Los hay con mensaje del MES (formato de SFC, batch, cantidad, DC
   obligatoria) y los hay de **procedimiento, sin mensaje** (No en el LC, código NC o
   cantidad de split equivocados, herramienta que existe pero no es la de la estación). El
   simulador califica ambos. La 017 solo debería sumar la primera clase; la segunda conserva
   el mensaje genérico de Assessment, que no revela la respuesta.
2. **Dónde se revisa el LC.** El único bloqueo observado en testing es en **Assemble y DC
   Collect** (22501), no en Start. El procedimiento (Resource Setup → LC → Start) no cambia,
   pero el simulador hoy exige el LC por secuencia y no lo revisa donde el MES lo revisa.
   No se probó si Start también lo bloquea.
3. **`22507` es de un SFC que no existe.** `SfcNotInOrder` cubre además el SFC que **sí existe
   pero es de otra orden**, cuyo texto no se conoce. No conviene asignarle el 22507 a ese
   caso sin capturarlo.
4. **El número no identifica el mensaje:** `22801` sirve a dos textos distintos (batch y
   cantidad). Usar el texto como clave, no el número.
5. **El almacén del batch cambia por sitio** (`AL01` en Costa Rica, `CS01` en Colorado); el
   testing es CR9A, así que corresponde `AL01`.
6. **El responsable.** En los dos errores 22801 la WI manda **avisar al encargado de
   materiales**: es contenido de comprensión (EP-7) que el mensaje podría reforzar.
7. **El mecanismo de mostrar errores** (banner y franja rojos en inglés, el OK que cierra
   Split) es la AUD-63/64/69 de la auditoría de fidelidad, pendiente de aprobación. La 017
   depende de esa variante visual: definir si la construye ella o la auditoría.
8. **Time-Based Assembly** (10059) queda fuera mientras ningún área lo use.
