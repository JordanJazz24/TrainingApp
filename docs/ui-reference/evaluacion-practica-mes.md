# Evaluación práctica de uso de MES (QD001043644) contra el simulador

> **Qué es:** el contraste del documento oficial de evaluación práctica con lo que el
> simulador hace hoy. Es la tarea **T6** de la Fase 8.5 ("prueba en papel → Assessment").
> **Fuente:** `QD001043644 rev. 1, Evaluación Práctica Uso de MES` (plantilla QD001038964
> rev. 1, Philips IGTD QMS). Lo preparó Ernesto Fernández, con fecha del 2026-10-09; el
> usuario lo entregó el 2026-10-19. Evalúa la WI **QD001001524** (*Production Order Execution
> on ERP System for Disposables*), la misma que contrasta `wi-produccion-disposables.md`.
> **Estado del simulador:** después de la spec 014 (2026-10-19).
> **Es registro de consulta:** los hallazgos (EP-n) no cambian nada por sí solos. Lo que se
> decida implementar va a una spec o a un plan, y la decisión se anota en el roadmap.

## Cómo califica la evaluación real

- **Pasa / Falla, sin puntaje.** Para pasar, el operador ejecuta **cada paso** correctamente.
  Cualquier criterio omitido o mal ejecutado en todo el proceso es **FALLA**.
- Quien falla repite la evaluación después de las acciones correctivas de **QD001040447**
  (lineamientos de evaluación de destrezas, competencias y comprensión). Todo paso mal hecho
  se revisa con el supervisor, entrenamiento o el experto.
- De las cuatro categorías (pruebas mecánicas/eléctricas, inspección de producto,
  observación del proceso y otros), solo aplica **Otros → "Funciones fundamentales de
  MES"**. Las demás y la sección B (evidencia: parte, lote, cantidad) son N/A.
- **N/A por operación:** Assembly, Data Collection y equipos se marcan N/A si el BOM o la
  operación no los piden. Los bloques sin columna N/A son obligatorios siempre. Coincide con
  los flags del área del simulador (`RequiresAssembly`, `RequiresDataCollection`).
- **Causas de falla (sección C, "Otro"):** no logra ejecutar los pasos con la WI
  QD001001524, o los ejecuta sin seguir la WI tal como la describe.
- **Tipos de criterio:** unos son **de ejecución** ("el operario selecciona…") y otros **de
  comprensión** ("el operario reconoce / identifica…"). El simulador solo evalúa ejecución
  en el POD.

## Los 8 bloques, criterio por criterio

Columna "Simulador": ✅ cubierto y calificado · 🟡 parcial (se hace, pero no se califica o es
distinto) · ❌ no existe · 🧠 comprensión (no se ejecuta en el POD).

### 1. Acceso e inicio de sesión en MES (obligatorio)

| Criterio | Simulador |
|---|---|
| Ejecuta "MES Limpiar historial y cache", ingresa su Code1, abre Repositorio y selecciona Recursos MES | ❌ Es la **spec 016 (T10)** |
| En Recursos de MES ingresa su Code1 y escanea el recurso | ❌ Spec 016 |
| En MES ingresa su Code1 y su contraseña, y verifica que su Code1 aparezca en el sistema | 🟡 Login de SAP NetWeaver (sin calificar). Spec 016 lo incluye como paso |
| Reconoce que el acceso se hace al inicio del turno y al cambiar de estación, antes de procesar | 🧠 |

### 2. Cerrar sesión de MES (obligatorio)

| Criterio | Simulador |
|---|---|
| Identifica cuándo cerrar sesión: descansos, fin de turno, cambio de estación o actividad, entregar la estación a otro Guardián de Vida | 🧠 |
| En el POD selecciona Logout cuando deja de usar el sistema | ✅ Último paso (spec 015) |
| Reconoce que una sesión abierta registra transacciones con el usuario equivocado | 🧠 |
| Después de cerrar sesión ejecuta "MES Limpiar historial y cache" | ❌ No existe. Candidato para la spec 016 |

### 3. Resource Setup (obligatorio)

| Criterio | Simulador |
|---|---|
| Abre Resource Setup y selecciona Retrieve | ✅ Save se habilita solo después de Retrieve |
| Limpia la configuración anterior con **REMOVE ALL y Save**, y luego **Insert New** | 🟡 Los equipos precargados se califican (spec 015), pero vale cualquier forma de quitarlos (también Remove Selected), y no se pide el Save intermedio |
| Escanea la etiqueta de Shop Order y los Tool ID, eligiendo bien el Setup Type: **Material**, Shop Order o Tool Number | 🟡 El Setup Type solo tiene `Shop Order` y `Tool Number` (falta `Material`). "Exactamente 1 Shop Order" y "al menos 1 Tool Number" son avisos de formato, no se califican |
| Verifica que el Setup State sea Set Up antes de guardar y cerrar | ✅ Guardar en Open es intento fallido |
| Reconoce que no debe iniciar el Shop Order o SFC antes de completar Resource Setup | ✅ Por secuencia (en Assessment, Start antes es intento fallido) |
| Reconoce que Resource Setup se repite al sustituir equipo (mantenimiento preventivo, correctivo, calibración) | 🧠 |

### 4. Line Clearance (obligatorio)

| Criterio | Simulador |
|---|---|
| Completa las actividades de Line Clearance antes de confirmarlo en MES | 🧠 (actividad física) |
| Abre Data Collection Standalone desde Activities, Retrieve y Collect | ✅ |
| Responde **Yes** obligatoriamente | ✅ "No" es intento fallido |
| Verifica el mensaje de confirmación y cierra Data Collection Standalone | ✅ "1 DC Groups saved" (no se califica que lo cierre) |
| Reconoce que se registra de nuevo al cambiar de usuario o de Shop Order, si queda un SFC activo o al vencer el tiempo configurado | 🧠 Las mismas condiciones de la WI (ver EP-8) |

### 5. Iniciar una Shop Order o SFC (obligatorio)

| Criterio | Simulador |
|---|---|
| Escanea el Shop Order o SFC en el campo SFC, o usa Browse Work List | ✅ (spec 011 y Browse Work List) |
| Selecciona correctamente el Shop Order o los SFC que va a iniciar | 🟡 La cola solo trae piezas de la orden, así que "seleccionar todo" siempre es correcto (diferencia ya anotada en `general-flow.md` → Paso 3) |
| Verifica que el Shop Order coincida con el de Resource Setup | 🟡 La orden simulada sale del Shop Order de Resource Setup: no hay forma de que no coincida |
| Presiona Start después de Resource Setup y Line Clearance | ✅ Por secuencia |
| Reconoce el error de MES cuando el SFC **ya está iniciado en otro recurso** | ❌ No se simula (mensaje sin captura) |

### 6. Assembly (N/A si el BOM no pide consumo)

| Criterio | Simulador |
|---|---|
| Selecciona el SFC o Shop Order correcto en el POD Work List y abre Assemble Components | ✅ Por secuencia |
| Consume los componentes **antes** de empezar la instrucción del MPI | 🧠 |
| Escanea el código 2D de la etiqueta del componente en Barcode | 🟡 Barcode numérico libre; no se valida contra el número de parte |
| Verifica lo que muestra MES: número de parte, descripción, cantidad, **Inventory SFC, Batch Number o Material Document Number** según corresponda | 🟡 Siempre muestra Batch Number = Process Lot. Es **W13**, recortada de la spec 014 (enmienda 6) |
| Selecciona Add y repite con todos los componentes | ✅ |
| Reconoce los errores de componente no requerido, material sensible al tiempo expirado, batch inválido o cantidad insuficiente | ❌ No se simula ninguno |

### 7. Data Collection (N/A si la operación no la pide)

| Criterio | Simulador |
|---|---|
| Selecciona el Shop Order o los SFC correctos y abre DC Collect | ✅ |
| Identifica los DC requeridos de la operación y selecciona Collect | ✅ El paso termina con todos los obligatorios |
| Completa la información y Save | ✅ Valores calificados (rango, Sí, fecha) |
| Repite hasta completar todos los DC requeridos | ✅ |
| Interpreta el indicador de DC incompleta y el de DC completada | 🟡 Hay indicador, pero falta la captura del ✔ real (**W15**) |
| Usa Save Draft para registrar por secciones y Save cuando está completo | 🟡 Save Draft existe pero no guarda nada |
| Reconoce los errores por DC obligatoria faltante o valores inesperados, y las alertas por parámetros fuera de límites | 🟡 Un valor fuera de rango es intento fallido con mensaje genérico; no hay alerta de límites como la del MES |

### 8. Completar una Shop Order o SFC (obligatorio)

| Criterio | Simulador |
|---|---|
| Confirma que Assembly y Data Collection están completos antes de completar | ✅ Por secuencia |
| Sin Buyoff, selecciona el Shop Order o los SFC y presiona Complete | ✅ (Log Buyoff fuera de alcance) |
| Verifica que MES muestre el mensaje de los SFC completados | ✅ Banner (provisional desde la spec 015, D-22) |

## Hallazgos

- **EP-1 · La evaluación es Pasa/Falla y el simulador da puntaje.** Assessment aprueba con
  0.70 ponderado y penaliza reintentos, así que se puede "pasar" con un paso fallido. En la
  evaluación real, un solo criterio mal hecho es FALLA. *Decisión pendiente:* mostrar también
  un resultado Pasa/Falla al estilo de la evaluación (por ejemplo, Pasa solo si ningún paso
  quedó fallido), cambiar el criterio de aprobación o dejarlo como está.
- **EP-2 · Log NC y SFC Split no están en la evaluación.** Ningún bloque los evalúa, aunque la
  trainer considera Log NC crítico y la spec 014 lo reforzó (0.05 × 3). La evaluación no pide
  mover pesos: confirma que Resource Setup, LC, Start, Assembly, DC, Complete y Logout son
  lo obligatorio. *Decisión pendiente:* mantener los pesos actuales o reducir Log NC.
- **EP-3 · El bloque 1 confirma el alcance de la spec 016 (T10)** (limpiar caché,
  Repositorio → Recursos MES con Code1 y escaneo del recurso, login de MES y verificar el
  usuario). Agrega un criterio que la 016 no tenía: **limpiar historial y caché después del
  Logout** (bloque 2).
- **EP-4 · Resource Setup: dos brechas de ejecución.** (a) El procedimiento evaluado es
  **REMOVE ALL + Save → Insert New**; el simulador acepta también Remove Selected y no pide
  el Save intermedio. (b) Se evalúa elegir bien el **Setup Type**, con una opción
  **Material** que el simulador no tiene, y el tipo no se califica (solo es formato).
- **EP-5 · Errores que la evaluación pide reconocer y el simulador no muestra:** SFC ya
  iniciado en otro recurso (Start); componente no requerido, material sensible al tiempo
  expirado, batch inválido y cantidad insuficiente (Assembly); alertas de parámetros fuera
  de límites (DC). Ninguno tiene captura. Con el acceso al sandbox (T9) se pueden capturar.
- **EP-6 · W13 vuelve a tener respaldo.** La evaluación pide verificar Inventory SFC, Batch
  Number o Material Document Number según el material. Es una de las condiciones para
  retomar W13 que puso la enmienda 6 de la spec 014. Tendría valor solo si se califica (por
  ejemplo, con etiquetas simuladas y verificación del dato).
- **EP-7 · Criterios de comprensión** ("reconoce / identifica…"): cuándo iniciar y cerrar
  sesión, el riesgo de una sesión abierta, cuándo repetir Resource Setup y Line Clearance,
  consumir antes del MPI. El POD no los evalúa. *Opciones:* preguntas de comprensión al
  final de Assessment (otro tipo de paso), notas de la guía en Guided o dejarlos al
  instructor.
- **EP-8 · Line Clearance repetido.** La evaluación da las mismas condiciones que la WI:
  cambio de usuario o de Shop Order, un SFC activo o el tiempo configurado vencido. No
  explica de dónde sale el LC de cierre del simulador, así que **W7 sigue abierta**.
- **EP-9 · Coincidencias que confirman el diseño:** Retrieve antes de Save, Setup State en
  Set Up, LC con Yes obligatorio por Data Collection Standalone, Start después de Resource
  Setup y LC, todos los DC obligatorios, Complete sin Buyoff, Logout en el POD y N/A según
  el BOM y la operación.

## Decisiones del usuario (2026-10-19)

**Criterio general:** la evaluación en papel **no se replica**. Es un insumo para ver qué
cubre el simulador, que debe cumplir **como mínimo** lo que ella pide.

| Hallazgo | Decisión | Dónde queda |
|---|---|---|
| EP-1 · Pasa/Falla | Assessment se queda como está: puntaje ponderado, aprueba con 0.70 | — |
| EP-2 · Pesos | Sin cambios; Log NC se queda en 0.05 × 3 (0.10 en Moldeo). T6 no mueve pesos | — |
| EP-3 · Caché después del Logout | Entra en la **spec 016**: entrada (limpiar caché → Repositorio → Recursos MES → login) y salida (Logout → limpiar caché) | Fase 8.5, fila 7 |
| EP-4 / EP-5 · Resource Setup y errores del MES | **Spec 017 · fidelidad con la evaluación práctica**, después de la 012. Primero se capturan los mensajes reales en el ambiente de testing (T9); sin capturas no se inventa UI (`docs/process.md` §8) | Fase 8.5, fila 10 |
| EP-6 · W13 | Sigue diferida: verificar el dato lo evalúa el instructor | Deferred Decisions de la spec 014 |
| EP-7 · Comprensión | **Notas en la guía de Guided**, cada una en su paso, como las de la trainer (T7). Plan directo; Assessment no las evalúa | Fase 8.5, fila 11 |

