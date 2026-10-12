# Spec 011: Escanear la Shop Order en Search/Start (T2, RF-17)

**Status:** Approved (2026-10-12)
**Date:** 2026-10-12 · **Approved:** 2026-10-12 (usuario, con las propuestas de diseño incluidas)

## Problem

Hoy el simulador entrena un solo camino para cargar la orden en el POD: abrir Browse Work
List, elegir las piezas (en Serialized, todas: spec 008) y presionar Start. `TxtSFC` es
readonly (spec 005). En planta lo habitual es otra cosa: el operador **escanea la etiqueta
de la orden en el campo SFC del POD**, la orden se carga y presiona **Start** a mano. Browse
Work List se usa sobre todo cuando se trabajan varias órdenes a la vez (revisión con SAP MES
y con Ernesto, 2026-10-09: roadmap → "Fase 8"). El simulador entrena como principal el
camino secundario.

## Scope

### In

1. **Escaneo como camino principal** de Search/Start, en Serialized y NonSerialized
   (incluido Moldeo, cuyo checklist de 4 modales tras Start no cambia). Browse Work List
   **sigue siendo válido**, sin penalización.
2. **Qué se escanea** (usuario): en `TxtSFC` sirve la etiqueta de la **Shop Order** o la de
   un **SFC** de la orden, sin importar el tipo de serialización.
   - Shop Order → carga la orden completa: todas las piezas en Serialized, el lote en
     NonSerialized.
   - SFC del lote (NonSerialized) → carga el lote, igual que la Shop Order.
   - SFC de una pieza (Serialized) → carga **solo esa pieza** (ver punto 6).
3. **Etiqueta simulada** (usuario): un panel con aspecto de **etiqueta impresa**, junto al
   POD y fuera del marco del MES, con la Shop Order en texto y en **código de barras
   Code 128**. Aparece cuando Resource Setup deja la orden asociada, en Guided y en
   Assessment, y muestra la Shop Order de la orden simulada (si Resource Setup avanzó
   forzado, la orden por defecto `303406950`). Volver a guardar Resource Setup la actualiza.
   Solo la etiqueta de la Shop Order: los SFC de pieza se escriben o se eligen en Browse.
4. **Tres formas de cargar** (usuario): **escribir** el número en `TxtSFC` y dar Enter;
   **escanear** la etiqueta con un lector real (manda el número y el Enter); o **tocar las
   barras** de la etiqueta, que escribe la Shop Order en `TxtSFC` y la carga como si se
   hubiera escaneado. En los tres casos Start sigue siendo **manual** y se califica por
   secuencia, como hoy.
5. **Número que no es de la orden** (ni su Shop Order ni uno de sus SFC; incluye texto no
   numérico y el caso sin orden porque falta Resource Setup): aviso, no carga nada y **no
   cuenta como intento**. Cargar es navegación, como Browse Work List; lo que se califica es
   el Start (`CLAUDE.md` → "Qué cuenta como intento").
6. **Serialized, una sola pieza cargada** (usuario): escanearla la carga, pero al presionar
   Start aparece un aviso de formato, **sin intento**: en el MES se puede iniciar una pieza,
   pero esta guía trabaja la orden completa, así que sugiere escanear la Shop Order. La
   regla de la spec 008 (Start exige todas las piezas) no cambia.
7. **Gana la última acción** (usuario): cada escaneo, o cada confirmación de Browse Work
   List, reemplaza lo cargado antes. Nada se acumula.
8. **Solo en Search/Start** (usuario): después del Start, `TxtSFC` vuelve a lo de hoy
   (muestra el SFC o `[N SELECTED]` y no se edita) y la etiqueta deja de cargar al tocarla.
9. **Guía de Guided** (usuario): el guion enseña el escaneo (señala la etiqueta: "escanee o
   toque las barras"; escribir el número también vale) y, después, Start. Una burbuja en el
   ícono de Browse avisa que también sirve, sobre todo con varias órdenes. Assessment no
   muestra nada nuevo: no recibe guía (spec 010).
10. **Placeholder de `TxtSFC`:** pasa de "Use Browse Work List..." a un texto neutro sin
    instrucciones (propuesta: vacío, como el MES real). En Assessment no hay texto que diga
    qué hacer.

### Out

- Piezas de otras órdenes en la cola de Browse Work List (RF-16, backlog).
- Autoarranque al escanear (Start automático).
- Escanear después del Start para seleccionar piezas de Assembly, DC, Log NC o Complete.
- Registrar con qué método se cargó la orden: lo decide la spec 013 (registro de actividad).
- Etiquetas por SFC (piezas o lote) en el panel.
- Cualquier variabilidad nueva del escenario (Ernesto, 2026-10-09).

## Domain Model

Sin cambios de esquema ni de `Domain/`.

- **Resolver lo escaneado** es una regla del simulador: se compara el texto (sin espacios
  al inicio ni al final; el resto exacto) con la Shop Order y con los SFC de las filas de la
  `SimulatedOrder`. Resultado: orden completa, una pieza o nada.
- **Dónde se resuelve:** la carga sigue el mecanismo de Browse Work List. Se resuelve **en el
  cliente**, contra los datos de la orden que el POD ya pinta (filas de Browse con su SFC y
  su Shop Order). Llena los mismos campos ocultos (`HfBrowseRowIds`, `HfSelectedSFC`),
  `TxtSFC` (el SFC o `[N SELECTED]`) y el header (Shop Order `In Queue`), **sin postback**
  (`CLAUDE.md` → "Un postback por paso"). El servidor no cambia su forma de validar: al
  presionar Start, `PodService.ClickStart` revisa las filas recibidas como hoy. Una
  manipulación del cliente termina en el aviso de selección, nunca en un intento ni en
  piezas ajenas.
- `PodState` no guarda lo cargado antes del Start (hoy tampoco guarda la selección de
  Browse).

## API Contract

Web Forms, sin endpoints HTTP nuevos.

- `PodService.ClickStart(ctx, browseRowIds, scannedSfc)`: misma firma. Cambia un aviso:
  en Serialized, con selección incompleta, el aviso (`PodNotice.SelectAllPieces`) pasa a
  decir que la guía trabaja la orden completa y sugiere escanear la Shop Order o elegir
  todas las piezas en Browse Work List. Un solo aviso para los dos caminos (propuesta).
- Aviso nuevo, **solo en el cliente**, para el número que no es de la orden (punto 5).
  Texto provisional en español hasta tener el mensaje real del MES (T9).
- Etiqueta: un bloque nuevo en `GuidedTest.aspx` con anclas `data-guide` (etiqueta y
  barras) y su guion en `GuideScriptBuilder` (`CLAUDE.md` → "POD y controles").

## Authorization

Sin cambios: el POD opera solo sobre la sesión propia y en curso (`PodService.LoadContext`).

## Edge Cases & Failure Modes

- **Escaneo antes de Resource Setup:** no hay etiqueta ni orden; lo escrito da el aviso del
  punto 5. Presionar Start sigue dando el aviso actual (Resource Setup pendiente).
- **Assessment, escaneo fuera de turno** (por ejemplo, con el Line Clearance de apertura
  pendiente): cargar es navegación y no cuenta. Presionar Start antes de tiempo sigue siendo
  un intento fallido por secuencia (S2), como hoy.
- **Lector que no manda Enter:** el número queda escrito en `TxtSFC` y el operador da Enter.
- **Lector láser sin lectura de pantalla:** muchos lectores láser no leen pantallas; los de
  imagen sí. Si el lector no lee, quedan escribir el número o tocar las barras.
- **SFC Split posterior** (NonSerialized): crea SFC nuevos después del Start; no aplica,
  porque el escaneo termina con el Start.
- **Re-escaneo o Browse después de cargar:** gana la última acción (punto 7).
- **Avance forzado del Start** (Assessment, 4.º intento): sin cambios; inicia todas las
  piezas (spec 008, E9).

## Non-Functional Requirements

- **Código de barras:** Code 128 dibujado en el cliente con **JsBarcode**, copiado al repo
  (`Scripts/`) con versión fija y **sin CDN**, porque la red de planta puede bloquearlo.
  Entra al inventario de paquetes de AUD-29. Tamaño suficiente para que lo lea un lector de
  imagen desde un monitor.
- Sin postbacks nuevos: escanear y cargar es inmediato.

## Integrations

Ninguna. JsBarcode es un archivo local (licencia MIT).

## Acceptance Criteria

**Etiqueta**
- **AC-1:** Given Resource Setup guardado con Shop Order X, When se pinta el POD, Then se ve
  la etiqueta con X en texto y en Code 128, en Guided y en Assessment.
- **AC-2:** Given Resource Setup pendiente, Then no se ve la etiqueta.
- **AC-3:** Given Resource Setup avanzado por forzado (Assessment), Then la etiqueta muestra
  `303406950`.
- **AC-4:** Given el Start ya ejecutado, When se toca la etiqueta, Then no carga nada.

**Carga**
- **AC-5:** Given una orden NonSerialized, When escribe la Shop Order o el SFC del lote en
  `TxtSFC` y da Enter, Then el lote queda cargado (`TxtSFC` con el SFC y el header con la
  Shop Order `In Queue`), sin postback.
- **AC-6:** Given una orden Serialized con N piezas, When escanea o escribe la Shop Order, o
  toca las barras, Then quedan cargadas las N (`TxtSFC` = `[N SELECTED]`).
- **AC-7:** Given una orden Serialized, When escribe el SFC de una pieza y da Enter, Then
  queda cargada solo esa pieza (`TxtSFC` con su SFC).
- **AC-8:** Given un texto que no es la Shop Order ni un SFC de la orden, When da Enter,
  Then aparece el aviso, no cambia lo cargado y no se registra intento.
- **AC-9:** Given algo cargado, When escanea otra cosa válida o confirma Browse Work List,
  Then queda cargado solo lo último.
- **AC-10:** Given el Start ya ejecutado, Then `TxtSFC` no se edita y muestra el SFC o
  `[N SELECTED]` como hoy.

**Start**
- **AC-11:** Given la orden completa cargada por escaneo, When presiona Start en el paso
  Search, Then el Start se comporta igual que con Browse Work List (Serialized: todas las
  piezas `Active`; Moldeo: abre el checklist).
- **AC-12:** Given una sola pieza cargada (Serialized), When presiona Start, Then aparece el
  aviso de orden completa (sugiere escanear la Shop Order) y no se registra intento.
- **AC-13:** Given Assessment con un paso anterior pendiente, When escanea la orden, Then no
  se registra intento; y When presiona Start, Then es intento fallido por secuencia (como hoy).
- **AC-14:** Given una selección de Browse manipulada en el cliente, When presiona Start,
  Then el servidor responde con el aviso de selección y no registra intento.

**Guía y Assessment**
- **AC-15:** Given Guided en el paso Search, Then la guía señala la etiqueta (escanear o
  tocar las barras), después Start, y avisa en el ícono de Browse que también sirve.
- **AC-16:** Given Assessment, Then no hay texto ni placeholder que indique cómo cargar la
  orden.
- **AC-17:** Given Browse Work List como camino, Then todo funciona como antes de esta spec
  (regresión de las specs 005 y 008).

## Deferred Decisions

| Decisión | Respaldo elegido | Se revisa cuando |
|---|---|---|
| Texto real del mensaje del MES para un número que no es de la orden | Texto provisional en español | T9: acceso al ambiente de testing |
| Si se registra el método de carga (escaneo, escritura, clic, Browse) | No se registra | Spec 013 (registro de actividad) |

## Open Questions

_Ninguna._
