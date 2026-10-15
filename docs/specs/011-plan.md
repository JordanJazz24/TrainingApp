# Plan de implementación — Spec 011

**Spec:** `docs/specs/011-escanear-shop-order.md` (Approved 2026-10-12)
**Plan:** aprobado por el usuario (2026-10-12) · **Cerrado ✅ (2026-10-13)**
**Arquitectura:** N-Tier. La carga por escaneo es **presentación** (cliente, como Browse Work
List); el único cambio de la BLL es la validación del SFC de NonSerialized en
`PodService.ClickStart` (enmienda E-2). Sin cambios de esquema, Domain, DAL ni SPs.

Tres entregas, en el orden del flujo. Cada una deja la app funcionando. Ciclo de cada
entrega (`docs/process.md` → "Cada entrega"): implementar → MSBuild Rebuild +
`aspnet_compiler` (se toca markup en las tres) → tests si cambia la BLL (E1) → review
calibrado → checklist → el usuario prueba en el navegador → commit
`feat: … (spec 011, entrega N)` en `TrainingApp` con el roadmap.

## Enmiendas a la spec (decididas por el usuario, 2026-10-12)

Registradas en la spec (bloque "Enmienda 2026-10-12", commit `1865c03`), con AC-18 a AC-21.

| # | Enmienda | Decisión |
|---|---|---|
| E-1 | Start con texto en `TxtSFC` que no se cargó (sin Enter) | Solo cuando Search es el paso actual: Start primero lo resuelve como si fuera Enter. Válido → carga y sigue con el Start; no es de la orden → aviso del punto 5 en el cliente, sin postback ni intento. Un Start fuera de turno en Assessment sigue llegando al servidor (S2) |
| E-2 | NonSerialized: `ClickStart` aceptaba cualquier SFC no vacío (el campo era readonly) | La BLL compara el SFC recibido con el SFC del lote y la Shop Order; si no coincide, `PodNotice.SfcNotInOrder` (sin intento), con el mismo texto que el aviso del cliente. Misma firma |
| E-3 | Ubicación de la etiqueta | Tarjeta flotante fija en la esquina inferior derecha, encima del POD; el POD conserva su ancho |

## Decisiones de implementación (no cambian la spec)

- **Datos para resolver en el cliente:** las filas de Browse Work List ya traen `data-sfc`,
  `data-row-id` y `data-shop-order`. Antes de Resource Setup no hay filas: todo número da el
  aviso del punto 5.
- **Una sola función de carga** (`podLoadScan(text)`) para Enter, el lector, el clic en las
  barras y el Start de E-1. Llena lo mismo que el OK de Browse (`HfBrowseRowIds`,
  `HfSelectedSFC`, `TxtSFC`, header `In Queue`) y además marca en Browse las filas cargadas
  (así Browse muestra lo vigente; gana la última acción).
- **`TxtSFC`:** editable mientras Search no está resuelto (también antes, en Assessment: AC-13);
  `readonly` después, como hoy. El atributo `readonly` sigue siendo HTML (no la propiedad
  `ReadOnly`, que descarta el valor posteado). Enter en `TxtSFC` nunca envía el formulario
  (implícitamente haría clic en el primer botón submit, Logout). Placeholder vacío (AC-16).
  NonSerialized con la Shop Order escaneada: `TxtSFC` muestra el SFC del lote (AC-5).
- **Etiqueta:** la pinta el servidor cuando hay orden (`PodState.Order`), con
  `ShopOrderInput` (incluye la orden por defecto del avance forzado). Después del Start sigue
  visible pero inerte (AC-4). JsBarcode (solo Code 128) en `Scripts/`, versión fija, MIT.
- **Guía:** extensión chica de `pod-guide.js`: un subpaso puede declarar elementos **libres**
  además del resaltado (los sube por encima del oscurecido como Logout y los deja usar). El
  subpaso de escaneo resalta la etiqueta y libera `TxtSFC`; avanza cuando `TxtSFC` queda con
  la orden completa cargada (marca `data-loaded="order"`, selector `done`).

## Entregas

### E1 — Carga desde `TxtSFC` (sin etiqueta)  ✅ (2026-10-13, probada por el usuario)
- [x] `GuidedTest.aspx`: placeholder vacío; script `podLoadScan` + Enter en `TxtSFC`; aviso
      del cliente (texto provisional en español, T9); marca de filas en Browse;
      `confirmBrowseSelection` y `podLoadScan` comparten el llenado (sin duplicar).
- [x] Start con texto sin cargar (E-1): chequeo de cliente en `BtnStart` solo cuando Search
      es el paso actual (patrón `SetClientRowCheck`).
- [x] `GuidedTest.aspx.cs`: `readonly` solo con Search resuelto; texto nuevo de
      `PodNotice.SelectAllPieces` (orden completa: escanee la Shop Order o elija todas en
      Browse); texto de `SfcNotInOrder`.
- [x] BLL (E-2): `SimulatedOrder.IsLotReference(text)` (SFC del lote o Shop Order, con
      Trim) y `ClickStart` NonSerialized → `PodNotice.SfcNotInOrder`.
- **AC:** 5, 7, 8, 9, 10, 12, 13, 14, 16 (placeholder), 17, 18, 19, 20. AC-6 y AC-11
  escribiendo la Shop Order.
- **Review:** `medium`, `PodService.cs`, `SimulatedOrder.cs` y el script de `GuidedTest.aspx`;
  foco: el servidor nunca inicia piezas ni un lote que no son de la orden, carga sin intento,
  secuencia S2 intacta, Browse sin regresión.
- **Resultado:** el review `medium` encontró 10 hallazgos; se aplicaron 6 (aviso propio del
  Start `PodNotice.StartWholeOrder`, segunda enmienda `b6846b9`; `<` y `>` se quitan al
  escribir; el escaneo siguiente reemplaza lo cargado; campo vacío descarga la orden; la Shop
  Order solo toma sus filas; texto neutro de `SfcNotInOrder`). No aplicados: revalidar el SFC
  en el checklist de Moldeo (no usa el SFC ni inicia un lote ajeno), tests de
  `IsLotReference` (`docs/process.md` §5), `data-loaded` (lo usa E3). **B-1** (ya existía):
  Browse después del Start reescribe `TxtSFC` y la selección del POD; decide el usuario.

### E2 — Etiqueta simulada  ✅ (2026-10-13, probada por el usuario)
- [x] `Scripts/JsBarcode.code128.min.js` (versión fija) y su `<script>` en `GuidedTest.aspx`.
- [x] Tarjeta flotante (markup + `pod.css`): Shop Order en texto + Code 128 con tamaño legible
      por un lector de imagen; por debajo de los modales; visible si hay orden.
- [x] Clic en las barras → escribe la Shop Order en `TxtSFC` y llama `podLoadScan`; inerte con
      Search resuelto.
- **AC:** 1, 2, 3, 4, 6 (tocar las barras), 11, 21.
- **AC-21 (no tapar controles):** la tarjeta va por debajo de los modales y de la guía
  (z-index menor que Bootstrap 1055 y que la guía 1059) y se dimensiona para la franja libre
  de abajo a la derecha (lado derecho del pie, vacío). Antes del Start esa zona no tiene
  controles (el POD Work List está vacío); después, Assembly, DC y Log NC ocupan la tabla. Si
  al probar tapa algo, se propone una enmienda (por ejemplo, ocultarla con el Start, que hoy
  la spec deja visible e inerte) antes de cambiar el código.
- **Review:** ninguno (markup, CSS y una línea de JS que reusa E1).
- **Resultado:** tarjeta de 64 px de alto como máximo sobre el lado vacío del pie (80 px), por debajo de los
  modales y de la guía. JsBarcode 3.11.6 (MIT), idéntico en cdnjs y npm, en el `.csproj`.
  Con la orden por defecto, el código mide 101 módulos (214 px). En el mismo commit, **B-1**
  (aprobado por el usuario): después del Start, el OK de Browse solo cierra el modal. **Sin
  probar**, entra al checklist de E3.

### E3 — Guía de Guided  ✅ (2026-10-13, probada por el usuario)
- [x] Anclas `data-guide`: `pod-label`, `pod-label-barcode`, `pod-sfc`.
- [x] `pod-guide.js`: elementos libres por subpaso.
- [x] `GuideScriptBuilder.Start`: escaneo (etiqueta, `TxtSFC` libre, avanza al cargar la
      orden completa) → burbuja informativa en el ícono de Browse ("también sirve, sobre todo
      con varias órdenes") → Start → checklist de Moldeo sin cambios. Se quitan los subpasos
      de Browse del camino principal.
- **AC:** 15, 16.
- **Review:** `low --max-findings 5`, `pod-guide.js` y `GuideScriptBuilder.cs`; foco: el
  bloqueo no deja escapar clics fuera del guion y Assessment no recibe nada.
- **Resultado:** `pod-guide.js` admite elementos libres por subpaso (`free`: se suben sobre
  el oscurecido mientras el subpaso está en pantalla; el foco cae en ellos si lo resaltado no
  tiene campo). Review `low` sin hallazgos. En el mismo commit, la tercera enmienda
  (`2e96361`): la etiqueta desaparece con el Start. B-1 probado.

## Fuera de este plan

Lo de "Out" de la spec, y registrar el método de carga (spec 013).
