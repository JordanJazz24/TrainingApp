# Plan — Fase 6.5: Fidelidad de UI/UX con el SAP MES real

Nació como "mini-fase de ajustes de UI post-spec 008" (entregas 1-4) y se formalizó como
fase el 2026-09-29. Objetivo, alcance y criterio de cierre: `docs/roadmap.md` → Fase 6.5.
Estado de cada pantalla: `docs/ui-reference/inventario-pantallas.md`.

**Sin spec:** las capturas del MES real (`Pictures\TrainingApp\`) son la fuente. Cada tanda
de correcciones del usuario = una entrega, con una ronda de preguntas si hace falta y plan
directo. Cada entrega: rebuild + `aspnet_compiler` + tests si cambia la BLL → checklist →
el usuario prueba en el navegador → commit → actualizar el inventario. Si una corrección
toca una regla de negocio, se señala y se decide aparte (no entra en la fase).

## Entrega 1 ✅ (2026-09-29, `48b67c1`)
- [x] Columna "CP" → "SFC"; columna selectora con Select All en POD y Browse Work List.
- [x] DC Collection: Collect deshabilitado en el grupo ya guardado por la pieza actual.
- [x] Assemble Components: "Apply to all SFCs" en el pie, a la izquierda de Add.

## Decisiones del usuario (2026-09-29, pedido "Ajustes varios")
- **Score** compacto en el header, a la izquierda de los íconos; **Abandon** a la izquierda
  del pie gris (fila de Resource Setup / Tool List).
- **Home y Help** del header: solo visuales, sin acción; nunca cuentan como intento
  (navegación, igual que Logout).
- **Colores del MES real:** header gris claro con texto oscuro, encabezado de tabla gris
  claro, sección gris continua. Deja de usarse el azul oscuro.
- Supuestos propuestos (a validar en el MES real, según el usuario): Status (SFC)
  `In Queue` antes del Start también en NonSerialized; los valores no se limpian al
  cambiar la selección de filas en el POD.

## Entrega 2 ✅ — Shop Order / Status (SFC) del POD Search (verificada por el usuario 2026-09-29)
- [x] Browse Work List → OK: `Shop Order: {orden}` y `Status (SFC): In Queue` (en el
      cliente, sin postback). Sin orden cargada, ambos quedan vacíos (como el real).
- [x] Start completado (directo, con checklist o forzado): `Status (SFC): Active`.
- [x] El servidor los recalcula en cada request (`BindSearchHeader` al final de
      `Page_Load`, y `LoadStartedOrderOnPod` al completar el Start) desde la orden
      simulada, las piezas de Browse (`HfBrowseRowIds`) y si Search ya se completó: así
      sobreviven a postbacks y F5.
- [x] Etiqueta "Status (SFC):". La ubicación a la derecha de la fila pasa a la entrega 3
      (hoy el widget de Score tapa esa zona).

## Entrega 3 ✅ — Layout del POD (verificada por el usuario 2026-09-29)
Capturas: "pantall central de referencia", "mensaje de banner correcto" y "pod cargado
con la orden selcionada". Colores medidos sobre las capturas (variables en `pod.css`).
- [x] Estructura en bloques (header → banner → sección gris → POD Work List + panel
      derecho → pie), sin `position: fixed` apilados: nada se solapa ni tapa modales.
      Causa del NC Data Entry tapado: se dibujaba dentro del panel fijo con z-index 1025.
- [x] Header gris claro: título + Score (con badge del modo) + Home, Logout y Help.
- [x] Banner entre el header y la búsqueda: fondo blanco, ícono verde, se oculta solo.
      Sin el "✓" en el texto; DC como el real: "All data for DC group {code} has been
      collected".
- [x] Búsqueda (operación y recurso azules con `*` rojo, SFC con ícono de browse dentro
      del campo, Shop Order y Status (SFC) a la derecha) y acciones (botones grandes
      centrados, "DC Collect") en una sola sección gris con el panel derecho y el pie.
- [x] Panel derecho: botones con ícono arriba y texto abajo; textos como el real ("Work
      Instruction", "Reports...", "Activities..." con triángulo, "Place back in queue").
- [x] Panel "POD Work List" con (?) y línea azul; encabezado de tabla gris claro; fila
      seleccionada celeste con la celda selectora azul; Status solo con el ícono (como
      las capturas). Browse Work List con la misma tabla. Component List, Data
      Collection List y NC Selection con el mismo encabezado de panel.
- [x] Activities en el orden real (Data Collection Standalone al final, sin divisor).
- [x] Abandon a la izquierda del pie.
- Los headers de los modales siguen oscuros hasta la entrega 4.

## Entrega 4 ✅ — Headers estándar de modales (verificada por el usuario 2026-09-29)
- [x] Barra de título `[Título] [↗] [X]` en los 12 modales: gris claro, ↗ maximiza /
      restaura (`mesToggleMaximize` en `Training.Master`), X con la misma acción de
      salida que ya tenía cada modal (en los 4 del Start de Moldeo, `searchCancel()`; en
      NC Data Entry, volver a NC Selection).
- [x] Encabezado interno según la captura de cada modal: Resource Setup (banda oscura +
      ?), SFC Split (banda "SFC Split"), Assemble, LC (submodal) y los 4 del Start de
      Moldeo (subencabezado + fila DC Group/Resource), DC Entry (`SFC: … (n/N)` + fila DC
      Group) y NC Data Entry (`SFC: …`). Data Collection Standalone y Browse: solo barra.
- [x] Corrección tomada de la captura: el submodal de LC decía `DCGTM_LC/CA`; ahora
      `DCGTM_LC/A`, igual que la fila de la lista.
- El cuerpo de cada modal no cambia en esta entrega.

## Tanda "Correcciones UI - Modales" (2026-09-29) → entregas 5-8

**Hallazgos en las capturas nuevas** (`Line Clearence/modal data collection standalone`,
`Line Clearence/modal line clearence- sub modal de la pregunta`):
- Patrón de ventana del MES: barra superior gris `#F2F2F2` + encabezado blanco con título
  en negrita, (?) y **línea celeste `#007DC5`** (la "línea divisoria celeste" pedida).
- Barra superior **solo con ⤢ ✕, sin título**: Data Collection Standalone, DC Entry y NC
  Data Entry (corrección del usuario). Resource Setup, Assemble, submodal de LC y Split
  conservan su título.
- Submodal de LC: el padre queda oscurecido ~20% y el submodal encima.

**Decisiones del usuario:**
- El ⧉ de cada fila de Resource Setup **copia el valor al portapapeles** (reemplaza al
  basurero; borrar sigue siendo REMOVE SELECTED / REMOVE ALL).
- DC Entry: **agregar la columna `Description`** a `DataCollectionParameters` (nullable;
  la línea "Description:" solo aparece si hay texto). El usuario aporta los textos.

**Supuesto propuesto (solo UI):** el Clear de Data Collection Standalone limpia el
formulario (oculta la lista hasta Retrieve), como el real; hoy cierra el modal.

## Entrega 5 ✅ — Resource Setup (verificada por el usuario 2026-09-29)
- [x] Botón de contorno celeste del MES (`.mes-btn-outline`: Retrieve, Save, Clear) y
      campo con ⧉ adentro (`.mes-field`, reutilizable en LC y NC).
- [x] Resource Setup: campos centrados con etiquetas a la derecha (`* Site`, `* Resource`
      con ⧉ + [Retrieve], `Setup State`); acciones juntas a la izquierda sobre la franja
      celeste; tabla `Setup Type` / `Setup Type Value` con ⧉ que copia al portapapeles (sin
      basurero; borrar = REMOVE SELECTED / REMOVE ALL); pie centrado.
- [x] Corrección del usuario (encabezados que se movían al primer INSERT NEW): tabla de
      dos columnas con ancho fijo desde el primer render (`table-layout: fixed`,
      `colgroup`, Setup Type 34%). Franja de acciones y encabezados visibles desde que abre
      el modal (como la captura vacía); las acciones siguen deshabilitadas hasta Retrieve,
      así el flujo no cambia. Sin línea vertical en todo el alto (el usuario aclaró que era
      solo una observación); queda el divisor del encabezado.
- [x] "Record changed" como la captura `Resource Setup/record changed correcto`: ícono de
      check verde + texto sobre el gris, debajo de la banda oscura (antes: alerta verde
      "✓ Record Change").

## Entrega 6 ✅ — Botones grises del MES + Line Clearance + línea celeste (verificada por el usuario 2026-09-29)
- [x] Estilo común de botones grises del MES con ícono (`.mes-btn` + `.mes-ico-*`: ícono
      por clase CSS como fondo, así sirve también en `asp:Button`); variante con ícono
      arriba (`.mes-btn-stack`). Pie de modal `.mes-footer`; parámetro de DC `.mes-param`
      (se reutiliza en la entrega 8).
- [x] Data Collection Standalone: barra solo ⤢ ✕; encabezado "Data Collection
      Standalone" con (?) y ✕ y línea celeste; campos con ⧉, [Clear] y [Retrieve] a la
      derecha; fila con la pregunta en cursiva, fecha del último registro y [Collect]
      grande; Clear limpia el formulario; mensaje "⚠ 1 DC Groups saved" como el real.
- [x] Submodal Data Collection Entry apilado: el hijo sube de z-index y su velo (20 %)
      oscurece suave al padre (pila propia en `Training.Master`, porque tras un postback
      padre e hijo se abren seguidos). Cuerpo con `.mes-param` y pie con [Save] [Cancel].
- [x] Línea celeste bajo el encabezado en Browse Work List y en los modales de LC;
      Browse con [OK] [Cancel] grises con ícono.

## Entrega 7 — Listas: Component List, Data Collection List, NC Selection
- [ ] Misma distribución que las capturas: datos centrados, botones grandes con ícono,
      paginación centrada con "Rows 1-N of N" debajo; NC Selection con el mismo patrón.

## Entrega 8 — Formularios: DC Entry y NC Data Entry
- [ ] Barra solo ⤢ ✕; encabezado con título y (?); fila SFC; fila DC Group (DC Entry).
- [ ] DC Entry: parámetros en recuadros (`* Pregunta:`, `Description:`, campo,
      `Data Type:` y [Comments]); columna `Description` nueva en BD (ALTER entregado aparte).
- [ ] NC Data Entry: etiquetas a la derecha, Component con ⧉, Comments como área de texto.
- [ ] Botones del pie grises con ícono en ambos.
