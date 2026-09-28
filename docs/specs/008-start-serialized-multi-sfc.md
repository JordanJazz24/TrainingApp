# Spec 008: Start de Serialized y procesamiento de múltiples SFC

**Status:** Approved (2026-09-28)
**Date:** 2026-09-25

> **Enmienda 2026-09-28 (usuario, al aprobar el plan):** en Moldeo el único DC Group
> obligatorio es el de las fechas de expiración (`DCGTM_FECHA_EXPIRACION_NIR/A`, que
> incluye la fecha del kerosene); el resto de sus grupos son opcionales. Browse Work List
> no muestra columna Status. Afecta los puntos 8, 13, Out y AC-10/AC-18.

## Problem

LoFlo (Fase 7) es un área **Serialized**, pero hoy el motor trata todas las áreas como
un lote: Resource Setup crea una `SimulatedOrder` con **un solo SFC y una sola fila**, el
POD permite seleccionar como máximo 1 fila y Start abre la secuencia de 4 modales, que es
exclusiva de Moldeo. En el SAP MES real, una Shop Order serializada tiene varias piezas,
cada una con su propio SFC, y el operador las procesa juntas o por separado.

Además, dos reglas actuales no reflejan el MES real:
- **Data Collection** se da por completo al guardar **un** grupo cualquiera, pero en la
  realidad cada pieza tiene grupos **obligatorios** (y otros opcionales).
- **Assembly** está modelado con un `ExpectedComponentCount` (N escaneos genéricos), pero
  en la realidad el operador escanea el material de cada componente de la operación y
  lo agrega. El conteo esperado no existe.

Sin resolver esto no se puede cargar LoFlo (bloqueantes #1, #2 y #4 de la Fase 7).

## Scope

### In

1. **Generación de SFCs (Resource Setup, solo Serialized).** Al guardar la Shop Order se
   generan automáticamente entre **3 y 5 SFCs** aleatorios, uno por pieza, y quedan en la
   orden simulada con Status **`New`**. Todavía no aparecen en el POD.
   - Formato: `{ShopOrder}-{sufijo}`. El sufijo es numérico y aleatorio, de **1 a 6
     dígitos** (ej. `000123-7`, `000123-430`, `000123-048213`). Los sufijos de una misma
     orden no se repiten.
   - Volver a guardar Resource Setup reemplaza la orden y genera SFCs nuevos (igual que hoy).
2. **Start en Serialized.** Siempre se usa el botón Start (en el MES real a veces cargan
   solos; en el simulador no).
   - El operador abre **Browse Work List**, donde cada pieza es una fila, y selecciona
     las que va a trabajar. TxtSFC se actualiza (punto 5).
   - Al presionar Start, las piezas seleccionadas pasan a **`Active`** y se cargan como
     filas en el POD. **No** se abre la secuencia de 4 modales.
   - Browse Work List **no** muestra columna Status: el Status de cada pieza se ve en el
     POD.
   - El paso Start en Serialized se califica **solo por secuencia** (S2).
3. **Start con o sin checklist (catálogo).** Nuevo flag por área en `Areas`:
   **`RequiresStartChecklist`**. Moldeo = 1 (secuencia de 4 modales, sin cambios); el
   resto = 0 (Start directo). No hay nombres de área en el código.
4. **Multi-selección (solo Serialized).** En Browse Work List y en el POD, **cada click
   alterna** la selección de esa fila (resaltada). Sin columna nueva. En NonSerialized
   sigue siendo como máximo 1 fila (spec 007).
5. **TxtSFC:**
   - 1 fila seleccionada → el SFC exacto de esa fila.
   - Varias filas → texto exacto **`[{N} SELECTED]`** (ej. `[3 SELECTED]`), igual que la
     pantalla real (`[250 SELECTED]` en la captura de EEP).
6. **Qué filas exige cada paso (Serialized):**

   | Paso | Filas | Si no se cumple |
   |---|---|---|
   | Start | todas las piezas de la orden (Browse Work List) | aviso de formato, sin intento |
   | Assembly | todas las filas del POD | aviso de formato, sin intento |
   | DC Collect | todas las filas del POD | aviso de formato, sin intento |
   | Log NC | ≥1 fila, y debe quedar ≥1 fila sin scrapear | ninguna fila: intento fallido (spec 007). Todas: aviso de formato, sin intento |
   | Complete | todas las filas que quedan en el POD | ninguna fila: intento fallido (spec 007). Algunas: aviso de formato, sin intento |

   - Texto del aviso (Start, Assembly, DC, Complete): *"En el simulador se trabajan todas
     las piezas juntas: selecciona todas las filas."*
   - Texto del aviso de Log NC con todas las filas: *"Debe quedar al menos una pieza para
     Complete."*
   - **Por qué sin penalización:** en el SAP MES real trabajar un subconjunto **no es un
     error** (lo común es trabajarlas todas, pero se puede ir pieza por pieza hasta
     Complete). El motor avanza un paso a la vez para toda la sesión y no puede retomar
     piezas que quedaron atrás, así que el simulador entrena el flujo común sin castigar
     el otro (ver Deferred Decisions).
   - La secuencia (S2) se sigue evaluando primero: un click fuera de secuencia cuenta
     como intento fallido aunque falten filas.
7. **Grupos DC obligatorios (catálogo).** Nueva tabla **`DataCollectionGroups`** (AreaId,
   Code, Description, CollectDataAt, IsRequired, DisplayOrder). Los parámetros pasan a
   referenciar su grupo; se normalizan Description y CollectDataAt, que hoy se repiten en
   cada parámetro.
   - La Data Collection List **no** indica qué grupos son obligatorios, en ningún modo:
     saberlo es parte de lo que se entrena.
8. **Regla de completitud de DC (todas las áreas).**
   - Una pieza queda completa cuando guardó **todos los grupos obligatorios** del área.
     Si el área **no tiene grupos obligatorios**, basta con **un grupo cualquiera** por
     pieza (es el comportamiento actual).
   - Los grupos opcionales se pueden guardar y se validan igual, pero no cuentan para
     completar la pieza.
   - Con varias piezas, se procesan **una por una** en el orden del POD. Debajo del
     header "Data Collection Entry" se muestra `SFC: 000123-001 (1/3)`. Al completar una
     pieza, el sistema pasa solo a la siguiente y actualiza el indicador.
   - El paso DC se completa cuando se completa la última pieza.
   - Moldeo tiene **un solo grupo obligatorio**: `DCGTM_FECHA_EXPIRACION_NIR/A` (fechas
     de expiración, incluida la del kerosene). El paso DC de Moldeo se completa al
     guardar ese grupo; guardar otro grupo ya no alcanza (cambio respecto de hoy).
9. **Errores en DC.** Un Save con valores incorrectos cuenta como intento fallido del paso
   DC (penalización normal), pero los grupos y piezas ya completados **se conservan** y
   el operador sigue desde donde estaba.
10. **Avance conservado.** Si el operador vuelve al POD a mitad de DC o Assembly, el
    avance (piezas, grupos, componentes) queda guardado en el servidor y al reabrir el
    paso sigue donde estaba.
11. **Assembly rediseñado.** Se elimina `ExpectedComponentCount` (de CLAUDE.md y del
    código; nunca llegó a la BD).
    - **Qué es la Component List** (captura real de EEP + confirmación de un operador de
      Sensores): la **lista de materiales (BOM) de la operación**, es decir, los números
      de parte que forman el producto que se trabaja. Varía según la operación.
    - Nueva tabla de catálogo **`AssemblyComponents`** por área, con `OperationId`
      nullable (mismo patrón que `DataCollectionParameters`): hoy cada área representa la
      operación de su guía; cuando exista Operation, el catálogo se mueve a ese nivel sin
      cambiar el esquema.
    - **Cada fila** muestra `{PartNumber}/{Revision}` (ej. `453601027151/A`), la Qty por
      unidad debajo, la Description y el **Rem Qty** (arranca igual a Qty), con los
      botones Assemble y Disassemble. Paginación `Rows 1-N of N`, ayuda (?) y X.
    - **La Qty es decimal**: puede ser `1` o una fracción como `0.0001` (materiales que
      se consumen por peso o longitud). Se muestra sin ceros de relleno (`1`, `0.0001`),
      igual que el Rem Qty.
    - **Por ahora el BOM se simula**: los números de parte, descripciones y cantidades
      son datos de ejemplo cargados en el catálogo, no el BOM real de la operación.
    - **Assemble** abre el modal para **ese** componente. El operador escanea **un**
      barcode numérico y presiona Add: **un escaneo cubre toda la Qty** y el Rem Qty pasa
      a 0. Con Rem Qty = 0, el botón Assemble de esa fila queda deshabilitado.
    - **Disassemble**: visible y **deshabilitado** siempre (el simulador no entrena el
      desensamble).
    - **Estructura del modal** (réplica de las capturas reales, corrección del usuario
      2026-09-28): franja superior con **Mode** como texto ("Choose") y **Action** como
      información no accionable (Assembly marcado, Removal gris), más los inputs Find
      component y Barcode. **Component Details**, **Assembly Data** y el botón **Add**
      aparecen recién al escanear (Enter o salir del campo); antes solo está Return to POD.
    - **Component Details**: inputs de solo lectura con datos del catálogo (Component =
      PartNumber, Version = Revision, Quantity) y Description como texto. **Batch
      Number** (★): el lote al que pertenecen las piezas, es decir el **Process Lot** de la
      orden (columna Process Lot del POD), no el barcode (corrección del usuario 2026-09-28).
    - La casilla **"Apply to all SFCs" marcada y deshabilitada** aparece cuando el
      escaneo cubre más de una pieza (en Serialized, siempre): cada escaneo se aplica a
      todas las piezas seleccionadas a la vez.
    - El paso se completa cuando **todos los componentes** del catálogo quedan con
      Rem Qty = 0.
    - Barcode vacío o no numérico: aviso de formato, sin intento. El barcode **no** se
      valida contra el número de parte.
    - Se califica **solo por secuencia** (S2).
    - Se completan la X de la Component List y **Return to POD** (mismo patrón
      `OnReturnToPod` que DC y Log NC).
12. **Log NC y Complete multi-fila.** Ambos actúan sobre todas las filas seleccionadas de
    una sola vez: Log NC scrapea las seleccionadas con el mismo código NC (y Component si
    `RequiresNCComponent`); Complete cierra las que quedan.
13. **Datos de prueba (dev).**
    - Moldeo: sus 6 grupos migran a `DataCollectionGroups`; solo
      `DCGTM_FECHA_EXPIRACION_NIR/A` es **obligatorio**, los otros 5 son opcionales.
      `RequiresStartChecklist = 1`. Sin componentes (RequiresAssembly = 0).
    - Redefinity (Catéteres; flags y guía de ejemplo, Serialized): **2 grupos
      obligatorios + 1 opcional** y un BOM simulado de **2 componentes** (uno con Qty
      `1` y otro con Qty decimal `0.0001`), con números de parte claramente ficticios,
      para probar las reglas en el navegador. `RequiresStartChecklist = 0`.
14. **CLAUDE.md** se actualiza: Assembly sin conteo esperado, catálogo de componentes y
    grupos DC obligatorios, flag `RequiresStartChecklist`.

### Out

- **Moldeo (NonSerialized) no cambia** en su comportamiento visible, incluida la
  secuencia de 4 modales de Start, **salvo** la regla de DC: ahora exige su grupo
  obligatorio (enmienda 2026-09-28).
- Recorrido pieza por pieza hasta Complete (ver Deferred Decisions).
- Catálogo real de LoFlo (grupos, componentes, flags): es la Fase 7.
- Validar el barcode de Assembly contra el número de parte.
- Disassemble funcional.
- Activar el nivel Operation (los componentes varían por operación; hoy se modelan por
  área con `OperationId` nullable).
- Indicador de grupos obligatorios en la UI.
- Selección parcial con penalización.

## Domain Model

- **`DataCollectionGroup`** (nuevo): `Id`, `AreaId`, `Code`, `Description`,
  `CollectDataAt`, `IsRequired`, `DisplayOrder`. Uno a muchos con
  `DataCollectionParameter`.
- **`DataCollectionParameter`**: reemplaza `DCGroup`/`GroupDescription`/`CollectDataAt`
  por `DataCollectionGroupId`.
- **`AssemblyComponent`** (nuevo, BOM de la operación): `Id`, `AreaId`, `OperationId`
  (nullable, futuro), `PartNumber`, `Revision`, `Description`, `Qty` (**decimal**, al
  menos 4 decimales, ej. `0.0001`), `DisplayOrder`.
- **`Area`**: agrega `RequiresStartChecklist` (bit).
- **Entrega de esquema** (proceso de CLAUDE.md): las tablas y columnas nuevas van directo
  a `00_Schema.sql`, y los datos a los scripts de catálogo (`05`–`07`). Para la BD de
  desarrollo existente se entrega aparte un script de `ALTER`/carga, con el orden de
  re-ejecución de los scripts de SPs.
- **Orden simulada (Session, sin BD):** cada fila pasa a tener su propio `SFCCode` y
  `Status` (`New` | `Active`). En NonSerialized, las filas siguen compartiendo el SFC del
  lote.
- **Avance de DC y Assembly (Session, sin BD):** por sesión, qué grupos guardó cada pieza
  y qué componentes se ensamblaron. Se descarta al completar o abandonar la sesión.
- **Reglas en la BLL:** la completitud de una pieza, del paso DC y del paso Assembly la
  decide la BLL a partir del avance y del catálogo. La página solo guarda y entrega el
  avance.
- **Puntaje:** el intento correcto del paso DC o Assembly se registra recién cuando el
  paso se completa. Los intentos fallidos de DC se registran en cada Save incorrecto.

## API Contract

No aplica: Web Forms, sin endpoints nuevos. Cambios de contrato interno:
- `StepValidator`: Answer de Search en Serialized (Start directo); Assembly deja de
  recibir `{"scannedCount":N}`.
- BLL: operaciones para guardar un grupo DC y un componente de Assembly que devuelven si
  la pieza o el paso quedaron completos.

## Authorization

Sin cambios: solo el operador autenticado dueño de la `TrainingSession`.

## Edge Cases & Failure Modes

| # | Caso | Comportamiento |
|---|---|---|
| E1 | Área con `RequiresAssembly = 1` y sin componentes en el catálogo | Error de configuración: el paso Assembly no se puede completar. El script de catálogo debe cargar ≥1 componente (Fase 7) |
| E2 | Área con `RequiresDataCollection = 1` y sin grupos | Igual que hoy (error de configuración) |
| E3 | Guardar dos veces el mismo grupo en la misma pieza | Se acepta; el segundo Save sobrescribe. No cuenta dos veces |
| E4 | Save incorrecto de un grupo opcional | Cuenta como intento fallido (es una decisión del operador) |
| E5 | 4.º fallo en DC a mitad de las piezas (Assessment) | Force-advance del paso completo, como hoy; el avance parcial se descarta |
| E6 | F5 a mitad de DC o Assembly | El avance en Session sobrevive. Igual que hoy, la selección de filas y el control abierto no se restauran |
| E7 | Session perdida (reciclo del App Pool, timeout) | **No se implementa ahora.** Se pierden la orden simulada y el avance, igual que hoy con la orden. Queda documentado como caso especial a analizar (ver Deferred Decisions) |
| E8 | Resource Setup forzado (por secuencia o por Setup State incorrecto) | Siempre se usa la orden por defecto con la Shop Order genérica, ignorando lo que el operador capturó en un intento fallido (corrección del usuario 2026-09-28). En Serialized también genera 3-5 SFCs |
| E9 | Start forzado (S2) en Serialized | Se cargan **todas** las piezas como `Active` en el POD |
| E10 | Selección de filas en NonSerialized | Sin cambios: máximo 1 fila |
| E11 | Click fuera de secuencia con filas faltantes | Cuenta como intento fallido de secuencia (S2 primero) |

## Non-Functional Requirements

Sin requisitos nuevos. Volumen trivial (3-5 piezas por orden, un operador por sesión).

## Integrations

Ninguna.

## Acceptance Criteria

- **AC-1:** Dado un área Serialized, cuando el operador guarda Resource Setup con Shop
  Order `000123`, entonces la orden tiene entre 3 y 5 SFCs `000123-{sufijo}` con sufijos
  numéricos únicos de 1 a 6 dígitos, todos con Status `New`, y el POD sigue vacío.
- **AC-2:** Dado que Browse Work List muestra las piezas, cuando el operador selecciona
  una sola, entonces TxtSFC muestra su SFC; cuando selecciona 3, TxtSFC muestra
  `[3 SELECTED]`.
- **AC-3:** Dado un área con `RequiresStartChecklist = 0` y todas las piezas
  seleccionadas, cuando presiona Start, entonces no se abre ningún modal, el paso se
  completa, las piezas pasan a `Active` y aparecen en el POD.
- **AC-4:** Dado que no están seleccionadas todas las piezas (Start, Assembly o DC
  Collect) y el botón es el del paso actual, cuando hace click, entonces ve el aviso
  "En el simulador se trabajan todas las piezas juntas…", no se abre el paso y no se
  registra intento.
- **AC-5:** Dado Moldeo (`RequiresStartChecklist = 1`), cuando presiona Start, entonces
  se abre la secuencia de 4 modales como hoy.
- **AC-6:** Dado un área con 2 grupos obligatorios y 1 opcional y 3 piezas
  seleccionadas, cuando abre DC, entonces ve `SFC: {sfc1} (1/3)`; al guardar
  correctamente los 2 obligatorios, el indicador pasa a `(2/3)`; al completar la tercera
  pieza, el paso DC se completa.
- **AC-7:** Dado el caso de AC-6, cuando guarda solo el grupo opcional, entonces la pieza
  no se completa.
- **AC-8:** Dado que está en la pieza 2/3 de DC, cuando guarda un grupo con un valor
  incorrecto, entonces se registra un intento fallido del paso DC y sigue en la pieza
  2/3 con los grupos ya guardados intactos.
- **AC-9:** Dado que está en la pieza 2/3 de DC, cuando vuelve al POD y reabre DC,
  entonces sigue en la pieza 2/3.
- **AC-10:** Dado Moldeo (único grupo obligatorio: `DCGTM_FECHA_EXPIRACION_NIR/A`),
  cuando guarda correctamente otro grupo, entonces el paso DC **no** se completa y vuelve
  a la Data Collection List; cuando guarda correctamente el grupo de fechas de
  expiración, entonces el paso DC se completa. (Un área sin grupos obligatorios se
  completa con un grupo cualquiera; se prueba con los tests de la regla.)
- **AC-11:** Dado un área con 2 componentes en el catálogo y todas las filas
  seleccionadas, cuando abre Assembly, entonces la Component List muestra cada
  componente como `{PartNumber}/{Revision}` con su Qty, Description y Rem Qty = Qty
  (una Qty decimal se ve tal cual, ej. `0.0001`), con Assemble habilitado y
  Disassemble deshabilitado.
- **AC-12:** Dado AC-11, cuando presiona Assemble en un componente, entonces el modal
  muestra los Component Details de **ese** componente (del catálogo) y "Apply to all
  SFCs" marcada y deshabilitada; al escanear un barcode numérico y presionar Add, su
  Rem Qty pasa a 0 y su Assemble queda deshabilitado. Al ensamblar el segundo
  componente, el paso Assembly se completa.
- **AC-13:** Dado el modal Assemble, cuando presiona Add con un barcode vacío o no
  numérico, entonces ve un aviso de formato y no se registra intento.
- **AC-14:** Dado que está en Assembly, cuando presiona Return to POD o la X de la
  Component List, entonces vuelve al POD sin registrar intento y conserva los
  componentes ya ensamblados.
- **AC-15:** Dado Log NC con 2 de 3 filas seleccionadas, cuando completa Add-Done,
  entonces las 2 filas se scrapean y quedan fuera del POD.
- **AC-16:** Dado Log NC con todas las filas seleccionadas, cuando presiona Log NC,
  entonces ve "Debe quedar al menos una pieza para Complete." y no se registra intento.
- **AC-17:** Dado Complete con todas las filas restantes seleccionadas, cuando presiona
  Complete, entonces el paso se completa. Con solo algunas: aviso de formato sin intento.
- **AC-18:** Dado Moldeo, cuando se recorre el flujo completo en Guided y Assessment,
  entonces el comportamiento es idéntico al actual, salvo la regla de DC de AC-10.

## Deferred Decisions

| Decisión | Fallback elegido | Disparador para retomarla |
|---|---|---|
| Recorrido pieza por pieza (una pieza hasta Complete, luego la siguiente), válido en el MES real | Todas las piezas juntas en cada paso; selección parcial = aviso sin penalización | Pedido de negocio o instructor; requiere cambiar el motor lineal de pasos |
| Catálogo real de LoFlo (grupos obligatorios, flags) | Datos de dev en Redefinity | Fase 7 |
| BOM real de cada operación (números de parte, cantidades) | BOM **simulado** en el catálogo, también para LoFlo en la Fase 7 | Cuando negocio pida fidelidad del BOM o se obtenga el BOM real de la operación |
| E7 — Pérdida de la Session a mitad de sesión (orden simulada + avance de DC/Assembly solo viven en `HttpSessionState`) | Se pierden; el operador tiene que abandonar y reiniciar (riesgo aceptado, igual que hoy) | Evolución de la arquitectura de flujos, Fase 8 (validación interna) o reportes de operadores. Opción a evaluar: persistir la orden y el avance en BD por `TrainingSessionId` |

## Open Questions

_Ninguna._
