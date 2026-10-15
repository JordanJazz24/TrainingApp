# WI QD001001524 — Ejecución de órdenes de producción en MES (Disposables)

Contraste entre el manual oficial de uso de SAP MES y el simulador. Fuente: **QD001001524
rev. 27**, "WI Production Order Execution on ERP System for Disposables" (versión en
español), revisada el 2026-10-12. Aplica a las plantas de Costa Rica y Rancho Cordova, solo
para productos descartables.

**Para qué sirve:** dar contexto del proceso real, no una lista de cambios. La WI describe
**todas** las funciones posibles; cada operación usa solo las que pide su configuración
("La configuración de cada operación puede ser diferente según los requisitos de
operación"). Que el simulador no tenga algo no es un defecto: los puntos marcados como
**a considerar** son candidatos que se deciden con negocio y van con su `/spec`
(`CLAUDE.md` → "Reglas de trabajo").

Cómo se ve y se opera cada pantalla del simulador: `general-flow.md`. Reglas de puntaje:
`docs/domain.md`. Aquí no se copian.

**Clasificación** (la misma de la revisión con SAP MES, roadmap → "Fase 8"):
- **Confirma** — la WI coincide con el simulador.
- **A considerar** — diferencia que podría entrar al simulador; se decide con negocio.
- **Por verificar** — la WI y lo documentado no coinciden o no está claro; confirmarlo en
  el ambiente de testing (T9) o con Ernesto.
- **Contexto** — dato del proceso real que ayuda a entenderlo; no pide cambio.

---

## Resumen

| # | Paso | Hallazgo | Clase |
|---|---|---|---|
| W1 | Secuencia | Resource Setup → Line Clearance → Start, como el simulador. El MES real bloquea el Start sin Resource Setup | Confirma |
| W2 | Resource Setup | Retrieve → Remove + Save → Insert New → filas → Setup State "Set up" → Save → mensaje → X | Confirma |
| W3 | Resource Setup | "Algunas operaciones no requieren la configuración de herramientas"; el simulador exige ≥1 Tool Number | Por verificar |
| W4 | Resource Setup | Setup Type tiene tres valores: Material, Shop Order y Tool Number (Material solo para Time-Based Assembly) | Contexto |
| W5 | Resource Setup | Errores reales: herramienta en dos recursos, menos herramientas de las requeridas, herramienta de otro grupo o expirada | A considerar |
| W6 | Line Clearance | Activities → Data Collection Standalone → Retrieve → Collect → Sí/No → mensaje → cerrar | Confirma |
| W7 | Line Clearance | La WI no describe un LC "de cierre": el LC se repite cuando cambia el usuario, pasa cierto tiempo, queda un SFC activo o cambia la Shop Order | Por verificar |
| W8 | Search/Start | Escanear la Shop Order o el SFC es el camino principal; Browse Work List, si no se puede escanear | Confirma T2 |
| W9 | Search/Start | El MES rechaza iniciar una orden distinta a la del Resource Setup, o un SFC ya iniciado en otro recurso | A considerar |
| W10 | Search/Start | Primera operación de una ruta con fecha de expiración: iniciar 1 SFC y después el resto | Contexto |
| W11 | Assembly | Seleccionar SFC → Assemble → escanear el 2D → Add → repetir; con el último, la lista se cierra sola | Confirma |
| W12 | Assembly | Escanear un componente que no es de la operación da error; también material perecedero expirado | A considerar |
| W13 | Assembly | Assembly Data depende de cómo se controla el **material**: Batch Number, Inventory SFC + Batch Number o Material Document Number. El simulador siempre muestra un Batch Number con el Process Lot de la orden | A considerar |
| W14 | Data Collection | "Apply to All SFCs" para recolectar en varias piezas a la vez | Confirma backlog |
| W15 | Data Collection | El grupo completo se marca con un ✔ en [Collect]; el simulador lo deshabilita | Por verificar |
| W16 | Data Collection | Save Draft sí guarda un borrador; el simulador no guarda nada | Contexto |
| W17 | Complete | Tres variantes: directo, con **Log Buyoff** (≤50 SFC por transacción) o con **firma electrónica** (mover toda la orden). El simulador solo tiene el directo | Decidido: simplificación aceptada |
| W18 | SFC Split | Split para aislar la cantidad y luego scrap del SFC nuevo; recomendado después de Assembly y DC | Confirma |
| W19 | Log NC | Fast scrap: Log NC → código `F_FS…` → Add-Done → "All of SFC Number scrapped" | Confirma |
| W20 | Log NC | Prefijos reales: `F_FS` scrap, `F_FR` reparación rápida, `F_FX` reparación extendida, `F` fallas de DC. Moldeo incluye `F_FR_GENERAL` y el simulador lo trata como scrap | A considerar |
| W21 | Log NC | Pasos de la reparación rápida y la extendida, útiles para T4 | Contexto |
| W22 | Sesión | Logout obligatorio en descansos, cambio de estación o de turno | Contexto |

---

## Por paso

### Antes del POD: login y selección

- Login: script "MES Limpiar historial y cache" con el Code1 → Repositorio → Recursos MES →
  Code1 y escaneo del código de barras del **recurso** → usuario y contraseña (o PIN + tarjeta
  de identidad). Se repite al inicio del turno y al cambiar de estación.
- **Logout (sección nueva de la rev. 27):** obligatorio en descansos, al terminar el turno, al
  cambiar de estación o de producto y siempre que otra persona pueda usar la sesión. Si no,
  las transacciones quedan con el usuario equivocado (trazabilidad del eDHR). **Contexto
  (W22):** el simulador no evalúa el login ni el logout (`docs/domain.md` → "Ejes de variación");
  es un buen mensaje para la guía o la capacitación (T7), no un paso.
- La WI escanea la Shop Order o el SFC en el campo SFC **antes** del Resource Setup, con la
  nota "NO INICIE EL SFC en este momento". Cargar la orden no es iniciarla. En el simulador,
  abrir Browse Work List y seleccionar filas es navegación y no cuenta, así que es coherente.

### 1. Resource Setup

**Procedimiento de la WI:** [Resource Setup] → Retrieve → **Remove y Save** → Insert New →
por cada fila, escanear la Shop Order o la herramienta y elegir el Setup Type → verificar
**Setup State = "Set up"** → Save → mensaje de cambio aplicado → cerrar con X.

- **W1, Confirma.** "Esta configuración es el primer paso después de registrarse en el POD"
  y "si el operario pretende iniciar la orden antes de la configuración del recurso, el
  sistema mostrará un mensaje de error". Es la base real de la regla de secuencia.
- **W2, Confirma.** Setup State "Set up" como requisito, el orden libre de las filas ("No
  existe un orden específico para las filas") y el cierre con X coinciden con
  `general-flow.md` → "Paso 1". Diferencia menor: la WI guarda después de remover y antes de
  insertar (dos Save); el simulador hace un solo Save al final. No cambia lo que se aprende.
- **W3, Por verificar.** "Algunas operaciones no requieren la configuración de
  herramientas", y la función completa "podría no ser usada en una operación". El simulador
  exige al menos 1 Tool Number en todas las áreas. Preguntar si en las áreas cargadas
  (Moldeo, LoFlo) siempre hay herramienta; si no, sería un flag por área.
- **W4, Contexto.** Setup Type tiene un tercer valor, **Material**, que se usa solo con
  Time-Based Assembly (ensamble automático al Start, con ranuras). En pre-esterilización,
  esterilización, post-esterilización y QFI no se pone la fila de Shop Order: solo
  herramientas. Ninguna de esas operaciones se simula hoy.
- **W5, A considerar.** El MES real rechaza la herramienta que ya está configurada en otro
  recurso, menos herramientas de las requeridas, una herramienta de otro grupo (ej. un
  micrómetro en lugar de una regla) y una herramienta expirada. El simulador acepta valores
  libres (decisión vigente). Sería la base de una variante "herramienta incorrecta", que
  necesitaría catálogo de herramientas por área. Sin textos de mensaje en la WI.
- Herramientas con sufijo en MES (`JMD00571CP-1`, `-2`): son la misma herramienta del ERP;
  el sufijo solo va en el eDHR. Contexto para los datos simulados.
- Cuándo se repite el Resource Setup: mantenimiento preventivo o correctivo y calibración,
  cuando se reemplaza un equipo. Y cada Resource Setup obliga a un Line Clearance nuevo.

### 2 y 6. Line Clearance

**Procedimiento de la WI:** Activities → "Data collection Standalone" → Retrieve → Collect →
responder Sí/No ("Confirme la ejecución de la limpieza de línea según el procedimiento") →
verificar el mensaje de confirmación → cerrar la ventana.

- **W6, Confirma.** Coincide con `general-flow.md` → "Paso 2", incluido que la respuesta es
  obligatoria y que debe ser **Sí** si se hizo la limpieza según el MPI 800956-001. Requiere
  el Resource Setup antes: "esta funcionalidad verifica si la orden de producción en la
  configuración del recurso ha cambiado".
- **W7, Por verificar.** La WI no describe un Line Clearance al cerrar. Dice que el MES pide
  documentarlo de nuevo cuando **cambia el usuario, pasa un tiempo desde el último, queda un
  SFC activo o cambia la Shop Order**. El simulador tiene un LC de cierre fijo, que confirmó
  el ingeniero de SAP MES (roadmap → "Revisión con SAP MES"). Probablemente sale del MPI de
  Line Clearance o de la configuración de cada operación, no de esta WI. No cambia nada;
  sirve para explicar por qué existe y para preguntarle a Ernesto si la prueba en papel lo
  evalúa (T6).
- **Contexto.** Con varias personas sobre un SFC y una sola que inicia, las demás también
  registran su LC. Y la firma del LC equivale a la aprobación al completar: por eso un
  supervisor puede completar la operación de otro.

### 3. Search / Start

**Procedimiento de la WI:** escanear la Shop Order o la etiqueta del SFC en el campo SFC; si
no se puede, Browse Work List → seleccionar (se pueden varios) → OK → [Start]. También se
puede escanear la etiqueta Traveler del SFC directamente.

- **W8, Confirma T2.** El camino principal es escanear, Browse Work List es la alternativa.
  Respalda la spec 011.
- **W9, A considerar.** Dos errores reales: iniciar una Shop Order distinta a la del
  Resource Setup ("el sistema dará un error") e iniciar un SFC que ya se inició en otro
  recurso. En el simulador no pueden pasar: la orden simulada nace del Shop Order del
  Resource Setup (spec 005). Si la spec 011 deja escribir la orden a mano, el primer error
  aparece solo: escanear una orden que no es la del Resource Setup. Anotarlo en esa spec.
  La WI no da el texto del mensaje.
- **W10, Contexto.** En la primera operación de una ruta con número de parte con fecha de
  expiración, se inicia **1 SFC primero y después el resto** (ej. 1 y luego 249). Y no se
  inician unidades en la primera operación entre 11:30 p. m. y 12:30 a. m. ni durante un
  mantenimiento del MES (se pierden datos de expiración). Ninguna área simulada es primera
  operación con expiración que se sepa; no aplica hoy.

### 4. Assembly

**Procedimiento de la WI:** seleccionar el SFC o la orden → [Assemble] → en Barcode escanear
el 2D del componente → la ventana se llena sola (número de parte, descripción, cantidad, SFC
de inventario, número de batch o documento de material) → [Add] → mensaje → repetir con cada
componente. Con el último, la lista se cierra sola.

- **W11, Confirma.** Coincide con `general-flow.md` → "Paso 4", incluido el cierre
  automático tras el último componente. El consumo en MES va **antes** de ejecutar la
  instrucción del MPI.
- **W12, A considerar.** El MES rechaza un componente que no es de la operación y un material
  perecedero expirado. El simulador no valida el barcode (decisión vigente, se califica por
  secuencia). Es el mismo pendiente "validación del material contra el BOM" que quedó sin
  respuesta con Diego, y el mensaje sigue sin captura (la WI no da su texto).
  Mensajes reales que **sí** da la WI, por si se simulan fallas de inventario:
  - `Batch number is invalid or the batch doesn't exist in storage location AL01 (message 22801)`
  - `Not enough Quantity Available in Unrestricted Stock(Message 22801)`

  En ambos casos el operador avisa al encargado de materiales; no se resuelve en el POD.
- **W13, A considerar** (resuelto con las imágenes de la WI, 2026-10-15). Assembly Data
  muestra **una de tres configuraciones**, según cómo se controla el material del
  componente en SAP:

  | Control del material | Campos de Assembly Data | Fuente |
  |---|---|---|
  | Por batch | `★ Batch Number` | Captura real `Assemble/…posterior al escaneo`: `0000168957` para un adhesivo |
  | Por inventario (recibido con Floor Stock Receipt) | `★ Inventory SFC` (se escanea) + `★ Batch Number` (en gris, lo llena MES desde el Inventory SFC) | WI, Assembly y el "Ejemplo" de la reparación rápida: `CR9AP1700000801` / `0301366528` |
  | Por documento de material | `★ Material Document Number` | WI, Assembly |

  El dato es **del material**, no de la orden. El simulador muestra siempre un solo
  ★ Batch Number con el Process Lot de la orden (`general-flow.md` → "Paso 4" y
  `glossary.md`). Para ser fiel: un tipo de dato de ensamble por componente en
  `AssemblyComponents` (simulado) y que Assemble Components muestre sus campos. No cambia el
  puntaje (Assembly se califica por secuencia), pero es regla de catálogo: va con spec. La
  reparación rápida (W21) reutiliza el mismo modal, así que hereda los campos correctos.
- **Contexto.** La cantidad solo se puede **bajar**. El MES no sirve para sobreconsumir: el
  componente extra se desecha por otro procedimiento (801061001).
- Time-Based Assembly (ensamble automático al Start con ranuras, botón Cargar/Reponer, y sus
  errores `Slot Configuration not found` y `Not Enough QTY on the Slots`): solo en estaciones
  que lo tienen. Fuera de alcance.

### 5. Data Collection

**Procedimiento de la WI:** seleccionar la orden o los SFC → [DC Collect] → Data Collection
List → [Collect] de cada grupo → llenar → Save → repetir con cada grupo.

- **W14, Confirma el backlog.** "Se puede ejecutar para múltiples SFC al mismo tiempo si la
  opción Apply to All SFCs está marcada." Es el ítem "DC por pieza o para todas" de la
  revisión con SAP MES; sigue en backlog.
- **W15, Por verificar.** El estado del grupo se ve en el ícono de [Collect]: gris con flecha
  a la izquierda = sin completar; gris con ✔ = completo. El simulador **deshabilita** el
  [Collect] del grupo ya guardado. En la captura `DC COLLECT/tabla de data collection list`
  los tres grupos están sin completar (ícono de flecha); falta una captura con un grupo
  guardado. Si el real cambia el ícono, es un ajuste de UI.
- **W16, Contexto.** Save Draft sí guarda un borrador, para registrar por secciones a lo largo
  del proceso; el simulador no guarda nada con Save Draft (`general-flow.md` → "Paso 5"). Un
  valor fuera de límites dispara una alerta y un grupo obligatorio pendiente da error, igual
  que el intento fallido del simulador.
- **Contexto.** "El entrenador debe agregar el nombre completo de la persona que se está
  entrenando" en el comentario: en piso, el entrenamiento queda registrado en el DC. La
  captura de la Data Collection List (Detector, Sensores) tiene un grupo
  `DCGTM_TRAINEE/A` "Entrenamiento de personal": en algunas operaciones es un DC Group
  propio. Dato útil para la conversación con Ernesto sobre cómo se registra hoy el
  entrenamiento.

### 7. SFC Split

**Procedimiento de la WI:** seleccionar la orden → [Split] → Qty to Split → [Split] → se crea
un SFC nuevo → [Close] → seleccionar el SFC nuevo y hacer el scrap (fast scrap).

- **W18, Confirma.** Coincide con el simulador: split que crea un SFC nuevo de la misma orden
  (enmienda 2026-10-01 de la spec 005) y luego Log NC sobre él. La WI recomienda dividir
  **después de Assembly y DC**, que es el orden de la guía. Su otro uso es que varios
  operadores completen partes de la misma orden; no se simula.

### 8. Log NC

**Fast scrap según la WI:** seleccionar el SFC o la orden → [Log NC] → en NC Selection,
[Log NC] del código de desecho rápido → en NC Data Entry, [Add-Done] → mensaje "All of SFC
Number scrapped" → cerrar NC Selection.

- **W19, Confirma.** Coincide con `general-flow.md` → "Paso 8", incluido Add-Done y el
  mensaje. Énfasis de la WI: **verificar que solo estén seleccionadas las unidades a
  desechar** antes de elegir el código. Si Comments es obligatorio, se escribe el SFC (se
  puede escanear) y la cantidad.
- **W20, A considerar.** Los prefijos dicen qué hace el código:

  | Prefijo | Método | Qué hace |
  |---|---|---|
  | `F_FS` | Desecho rápido (fast scrap) | Desecha la unidad en un paso |
  | `F_FR` | Reparación rápida (fast repair) | Quita un componente y lo devuelve (Return) o lo desecha (Scrap); se puede agregar otro. Retrabajo en la misma operación |
  | `F_FX` | Reparación rápida extendida | Regresa la unidad a la misma operación o a una anterior; el componente es opcional |
  | `F` | Falla de DC (DC-DC ESign Exceptions) | Retrabajo en la misma operación |

  El catálogo de Moldeo (`06`) tiene `F_FR_GENERAL` mezclado con los `F_FS_…`, y el
  simulador lo trata como scrap: en el real ese código abre el flujo de reparación. Si se
  quiere fidelidad sin esperar a T4, se puede quitar del catálogo de scrap. Los códigos de
  LoFlo son simulados (`SIM_FS_…`) y siguen la convención `FS`.
- **W21, Contexto para T4** (los tres métodos de Diego). Aunque faltan las pantallas, la WI
  ya trae los pasos:
  - **Reparación rápida (`F_FR`):** Log NC → código `F_FR` → en Component, buscar el
    componente a reemplazar → OK → Add Done → confirmar la remoción (Yes) → **Return** (vuelve
    a inventario, solo si el MPI lo permite) o **Scrap** (sobreconsumo) → "¿agregar
    componente?" Yes/No → si Yes, datos del componente nuevo → Add. Si la orden necesita
    split, va **antes** de quitar componentes (Anexo A).
  - **Reparación extendida (`F_FX`):** Log NC → código `F_FX` → componente opcional → Add
    Done → elegir la operación a la que vuelve la unidad → confirmación. `F_FX_GENERAL` lleva
    en Comments el MPI y su revisión, o el número de NCR.

### 9. Complete

**Procedimiento de la WI:** depende de la configuración de la operación.

- **Sin Log Buyoff:** seleccionar → [Complete] → el sistema completa. Es lo que hace hoy el
  simulador.
- **Con Log Buyoff** (operaciones que transaccionan **50 SFC o menos** a la vez):
  [Complete] → ventana de Log Buyoff → usuario y contraseña (o PIN + tarjeta) → seleccionar la
  línea de buyoff → comentario opcional → [Accept] → mensaje de que la orden pasó a la
  siguiente operación.
- **Con firma electrónica** (cuando se mueve toda la orden, producto terminado): [Complete] →
  "Data Collection Authentication" → usuario y contraseña → confirmar que todas las
  actividades terminaron → Save.
- Con equipo automatizado no hay buyoff.

- **W17, A considerar.** `general-flow.md` → "Paso 9" dice que "el flujo real SAP es
  directo" y que el buyoff del legacy no corresponde. La WI muestra que el buyoff **es
  habitual**, y lo define como "forma electrónica de aprobar la finalización de una actividad
  de fabricación". Con 3-5 piezas por orden, las guías Serialized del simulador caerían en
  el rango del buyoff. Preguntar a Ernesto o en el ambiente de testing qué variante tienen
  Moldeo y LoFlo. Si entra, va con spec: es una decisión del operador en Complete (hoy solo
  acepta `"complete"`).
  **Decidido (validación con técnicos, 2026-10-12):** simplificación aceptada, el simulador
  entrena solo el Complete directo (`general-flow.md` → "Paso 9").

---

## Funciones de la WI fuera del simulador

Ninguna está en el flujo de entrenamiento. Varias ya aparecen en el POD como botones de
relleno (en Assessment cuentan como intento fallido de secuencia: `docs/domain.md` → "Qué cuenta
como intento"). Sirven para que la guía o un trainer explique para qué son.

| Función de la WI | Dónde está en el POD simulado | Quién la usa |
|---|---|---|
| SFC Place Hold / Release Hold / Future Hold (retener la orden por un NCR) | Activities | Calidad |
| Change Equipment Status Tool + Tool List → Log Tool (cambiar una herramienta ya asociada) | Activities, pie [Tool List] | Técnico, ingeniero, supervisor o líder |
| Time Sensitive Material Check In/Out (materiales con expiración) | Activities | Operador; nunca Check-Out de un material ya registrado |
| Floor Stock Receipt (recibo de inventario de piso, imprime etiqueta) | — | Operador |
| Estado del recurso (Engineering, Schedule Down, Unschedule Down, Standby) | — | Operador |
| Work Instruction (abre la última versión del MPI) | Panel derecho | Operador |
| Reports (As-Built Summary, BO Device History Record, Shop Order Report, SFC Report) | Panel derecho | Todos |
| Place back in queue (SFC activo vuelve a la cola de la misma operación) | Panel derecho | Operador |
| Process Lot: crear el lote en pre-esterilización e iniciar esterilización, post y QFI con él | — | Operador de esterilización |
| DC File Upload (adjuntar documentos a un DC Group) | — | Operador |
| Unscrap (dentro de 24 h, con conciliación y el formulario D000351707) | — | MES/ERP, con supervisor y Calidad |
| Herramienta de Shelf Life (PASA/FALLA antes de completar en la primera operación) | — | Operador |
| Liberar la orden en SAP (CO02, ruta sin "NCB", recibo en AL01) | — | Líder o supervisor, antes del MES |

## Términos nuevos

Definiciones en `glossary.md`: Buyoff, firma electrónica, eDHR/eLHR, fast scrap/repair,
Hold, Time-Based Assembly, MPI.
