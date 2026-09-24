# Spec 007: Cierre del flujo en el POD — Log NC y Complete

**Status:** Approved (2026-09-24) — enmendada 2026-09-24: paginación funcional de NC Selection (8 por página) y lista visible detrás de NC Data Entry
**Date:** 2026-09-23
**Fase:** 6 (cierre)
**Relacionada con:** Spec 004 (UserControls), Spec 005 (orden simulada), Spec 006 (controles estáticos)

---

## Problem

Los dos últimos pasos de la guía no cumplen el flujo real del operador (reporte de
bugs del 2026-09-22, recorrido de R&C/Moldeo):

- **Log NC (BUG 4, 5, 6):**
  - NC Data Entry pide *Component* en Moldeo, que no lo usa.
  - El scrap no tiene efecto: la fila sigue en el POD y no hay confirmación.
  - La NC Selection List no tiene la estructura del Data Collection List.
  - *Return to POD* deja la pantalla en blanco.
  - *Add* y *Done* se comportan como si fueran válidos.
- **Complete (BUG 7):** no exige una fila seleccionada y el resumen final no se
  muestra como overlay.
- **Selección de filas (parte del BUG 3):** la regla de "máximo 1 fila" en
  NonSerialized no está escrita en ninguna spec.

Como entrenamiento, el operador tiene que aprender la secuencia real: seleccionar la
fila correcta, usar el botón correcto y ver el efecto del scrap en la orden. En
Assessment, equivocarse en eso tiene que costar puntaje.

---

## Scope

### In

1. **NC Selection List:** reemplaza el POD Work List, igual que el Data Collection
   List. Tiene la misma estructura:
   - Header "NC Selection" con `[?]` y `[✗]`.
   - Grilla de 2 columnas con `[Log NC]` por código del catálogo del Area.
   - Footer de paginación **funcional** (enmienda 2026-09-24): **8 códigos por página**,
     First / Prev / Next / Last se deshabilitan en los extremos, y se muestra
     "Rows x-y of N". Paginación en cliente (estado intermedio de UI, sin postback). La
     página actual se conserva al abrir NC Data Entry y volver con la `X`.

   `[✗]` vuelve al POD **sin registrar intento**.

   Al abrir NC Data Entry, la NC Selection List **permanece visible en segundo plano**
   detrás del modal (enmienda 2026-09-24).

2. **NC Data Entry (modal):**

   | Elemento | Comportamiento |
   |---|---|
   | SFC | Solo lectura (SFC de la fila seleccionada) |
   | NC Code | Solo lectura, precargado con el código elegido |
   | Component | Visible y requerido **solo si `Area.RequiresNCComponent = 1`**; con 0 el campo y su validador no se renderizan |
   | Comments | Opcional |
   | **Add-Done** | Único camino correcto: envía el código NC → la BLL lo valida contra el catálogo |
   | **Add** / **Done** | **Intento fallido:** envían un Answer centinela que no existe en el catálogo NC, así que `ValidateLogNC` lo rechaza sin cambiar la BLL. Se muestra un único SweetAlert **"Not the correct button"**, en lugar del hint genérico, y el modal sigue abierto con los datos (E3 de la spec 006) |
   | **Return to POD** | Vuelve al POD con sus filas y la selección intactas, **sin registrar intento** |
   | `X` del header | Cierra el modal y vuelve a la **NC Selection List**, **sin registrar intento** |

   Los datos de NC (Component, Comments) no se persisten: el simulador evalúa el
   código y el botón usado.

3. **Gate de fila al abrir Log NC** (validado en servidor; **cuenta como intento fallido**):
   - NonSerialized: tiene que haber una fila seleccionada **y debe ser una fila creada
     por el SFC Split**. La fila original del lote no se puede scrapear.
   - Serialized: tiene que haber una fila seleccionada.
   - Si falla: se envía el Answer centinela a la BLL (intento fallido), **no se abre**
     la NC Selection List y se muestra un alert con el motivo:
     "Selecciona una fila creada por el split para scrapear" (NonSerialized) /
     "Selecciona una fila para scrapear" (Serialized).
   - Como el intento tiene que registrarse, el gate **no puede bloquear en el
     cliente**: el click siempre llega al servidor.

4. **Efecto del scrap.** Cuando LogNC termina en `Correct` o en `FailedForced`
   **con una fila válida seleccionada**:
   - La fila se **elimina** de la orden simulada.
   - El POD se vuelve a pintar y la selección se limpia.
   - Aparece un banner verde durante 5 s: **"All of {SFC} scrapped"**.
   - En NonSerialized, txtSFC no cambia: es el SFC del lote.
   - Se habilita Complete.

5. **Complete:**
   - `[Complete]` exige una fila seleccionada (cualquiera de las que quedan). Se valida
     en servidor y **cuenta como intento fallido**: sin fila, se envía un Answer
     centinela y se muestra **"Select 1 row to complete"**.
   - **Cambio mínimo en la BLL:** `StepValidator` para `Complete` acepta solo el Answer
     `"complete"`; cualquier otro es incorrecto. Se actualiza la documentación de
     formatos en `StepValidator`. El scoring no cambia.
   - Con fila seleccionada: se envía `"complete"` → la sesión se completa.

6. **Overlay de resumen:** el panel de completion actual (score, modo, área, producto
   y aprobado/reprobado en Assessment) pasa a mostrarse como **overlay a pantalla
   completa** por encima del POD. Mismos datos; el score vacío ya se corrigió en
   `51636a7`.

7. **Regla de selección:** en todas las áreas se puede seleccionar **como máximo 1
   fila** del POD. Es la regla definitiva para NonSerialized. La multi-selección de
   Serialized se define en su propia spec (ver Deferred Decisions).

### Out

- **Start de Serialized:** piezas con SFC propio por fila (ej. `0000001020-430`),
  txtSFC cargado al seleccionar una fila y multi-selección. Va en su propia spec,
  **antes de la Fase 7**.
- Robustez ante F5 (bugs 8 a 12): spec 008.
- Bug de Assembly y `ExpectedComponentCount` en BD: diferidos (spec 006, Deferred Decisions).
- Detalle por paso en el resumen final.
- Persistir los datos de NC (Component, Comments).
- Cambios de pesos, orden de pasos o `ScoreCalculator`.

---

## Domain Model

### Base de datos

- `Areas.RequiresNCComponent BIT NOT NULL DEFAULT 1`. Script nuevo, idempotente.
  - Moldeo = **0**.
  - Redefinity y Subensamble (ejemplo) = 1.
  - LoFlo = 1 **provisorio**, se confirma en la Fase 7.
- `spAreaGetById` y `spAreaGetByProduct` devuelven la columna.

### Domain / BLL

- `Area.RequiresNCComponent` (bool), mapeado por `AreaRepository`.
- `StepValidator`, único cambio de lógica: `Complete` → `submittedAnswer == "complete"`.

### Modelo de UI (orden simulada en Session, spec 005)

- `SimulatedOrderRow.IsFromSplit` (bool): `true` en las filas creadas por
  `HandleSplitRequested`. La fila original del lote queda en `false`.

### Formatos de Answer

| StepType | Correcto | Incorrecto |
|---|---|---|
| LogNC | Código NC del catálogo del Area (Add-Done) | Centinela que no existe en el catálogo (Add, Done, gate de fila) |
| Complete | `"complete"` | Centinela (sin fila seleccionada) |

El valor exacto del centinela se define en el plan, como una constante única de la
capa de presentación.

---

## API Contract

No aplica: no hay endpoints HTTP. La interacción sigue siendo por postback de Web Forms.

| Postback | Qué hace el servidor |
|---|---|
| `[Log NC]` de la botonera | Gate de fila → si falla: intento fallido + alert; si pasa: abre NC Selection List |
| `[Log NC]` de un código | Abre NC Data Entry con el código |
| Add / Done | Submit centinela → `Incorrect` → alert "Not the correct button", modal abierto |
| Add-Done | Submit del código → `Correct` → scrap (Scope In #4) |
| `[✗]` de NC Selection / Return to POD | `OnReturnToPod` → cierra el control, sin intento |
| `X` de NC Data Entry | Vuelve a la NC Selection List dentro del control, sin intento |
| `[Complete]` | Gate de fila → centinela o `"complete"` |

---

## Authorization

Sin cambios: FormsAuth + `LoadAndValidateSession`. Se mantiene el guard de sender de
la spec 006 (E12) para `OnStepSubmit` y `OnReturnToPod`.

---

## Edge Cases & Failure Modes

| # | Caso | Comportamiento requerido |
|---|---|---|
| E1 | Add o Done con Component vacío (flag = 1) | Intento fallido igual; el botón equivocado pesa más que la validación |
| E2 | Add-Done con Component vacío (flag = 1) | Lo bloquea el validador; no registra intento (comportamiento actual) |
| E3 | Respuesta incorrecta dentro de NC Data Entry | El control queda abierto con los datos (E3 de la spec 006) |
| E4 | Gate de fila falla 4 veces en Assessment | Force-advance a Complete; **no se elimina ninguna fila** ni se muestra banner, porque no hay fila válida |
| E5 | Add/Done 4 veces en Assessment con fila válida | Force-advance; la fila **sí** se elimina y se muestra el banner (Scope In #4) |
| E6 | Después del scrap | La selección queda limpia; para Complete hay que seleccionar una de las filas que quedan |
| E7 | Complete sin fila 4 veces en Assessment | Force-advance → sesión completada → overlay de resumen |
| E8 | Serialized (Redefinity, datos de prueba) | El gate de LogNC exige solo una fila seleccionada (no hay split) |
| E9 | Click en controles de un paso ya cerrado | Descartado por el guard de sender (spec 006) |
| E10 | F5 en cualquier punto | Fuera de alcance (spec 008) |

---

## Non-Functional Requirements

- Sin consultas extra por request: `RequiresNCComponent` viaja en el `Area` que ya se carga.
- `ALTER TABLE` sobre `Areas`, una tabla de pocas filas; el script es idempotente.

---

## Integrations

Ninguna.

---

## Acceptance Criteria

Verificación manual en navegador (Guided y Assessment) + build + precompilación del markup.

- **AC-1 (estructura):** Dado el paso LogNC con una fila válida, cuando el operador
  abre Log NC, entonces la NC Selection List reemplaza el POD con header
  "NC Selection" `[?][✗]`, grilla de 2 columnas y footer de paginación como el Data
  Collection List. La lista muestra 8 códigos por página y se puede avanzar y
  retroceder (30 códigos en Moldeo = 4 páginas); al abrir un código, la lista sigue
  visible detrás de NC Data Entry.
- **AC-2 (volver):** Dado NC Selection abierta, `[✗]` vuelve al POD; dado NC Data Entry
  abierto, Return to POD vuelve al POD y la `X` del modal vuelve a la NC Selection List.
  Al volver al POD, las filas y la fila seleccionada siguen intactas. Ninguno de los
  tres registra intento.
- **AC-3 (Add / Done):** Dado NC Data Entry abierto, cuando el operador aprieta Add o
  Done, entonces ve solo el alert "Not the correct button", el modal sigue abierto con
  los datos y se registra un intento fallido (en Assessment el puntaje de LogNC baja).
- **AC-4 (Component por área):** Dado Moldeo, NC Data Entry no muestra Component;
  dada Redefinity, lo muestra como requerido.
- **AC-5 (gate de LogNC):** Dado Moldeo, cuando el operador abre Log NC sin fila o con
  la fila original del lote, entonces ve el alert correspondiente, no se abre la NC
  Selection List y se registra un intento fallido.
- **AC-6 (scrap):** Dada una fila creada por el split seleccionada, cuando el operador
  completa Add-Done, entonces la fila desaparece del POD, se ve durante 5 s el banner
  "All of {SFC} scrapped", txtSFC no cambia y se habilita Complete.
- **AC-7 (force-advance):** Dado Assessment, al 4º Add/Done con fila válida la fila se
  elimina y aparece el banner (E5); al 4º fallo del gate no se elimina nada (E4).
- **AC-8 (Complete):** Cuando el operador aprieta Complete sin fila, ve "Select 1 row
  to complete" y se registra un intento fallido; con una fila seleccionada, la sesión
  se completa y el resumen aparece como overlay a pantalla completa con score, modo,
  área, producto y (en Assessment) el resultado.
- **AC-9 (selección):** En cualquier área, seleccionar una fila deselecciona la
  anterior; nunca hay más de una fila seleccionada.
- **AC-10 (build):** El proyecto compila, el markup precompila con `aspnet_compiler`
  y el script SQL es re-ejecutable.

---

## Deferred Decisions

| Decisión | Fallback elegido | Disparador para retomarla |
|---|---|---|
| Start de Serialized: una fila por pieza con SFC propio, txtSFC por fila, multi-selección | Una sola fila por orden y selección simple en todas las áreas | Spec propia, **antes de la Fase 7** (LoFlo es Serialized) |
| `RequiresNCComponent` de LoFlo | 1 (valor por defecto) | Fase 7, al cargar el catálogo real de LoFlo |
| Detalle por paso en el resumen final | Solo los datos actuales del panel | Pedido del instructor / Fase 8 |
| Bug de Assembly y `ExpectedComponentCount` | Diferido (ver spec 006) | Primera guía real con Assembly |

---

## Open Questions

_(ninguna)_
