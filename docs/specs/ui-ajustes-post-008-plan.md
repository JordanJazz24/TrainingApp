# Plan — Mini-fase de ajustes de UI post-spec 008

**Sin spec:** pedidos acotados del usuario con capturas del MES real como fuente
(`Pictures\TrainingApp\`). Una ronda de preguntas por pedido y plan directo (ver
`docs/roadmap.md` → mini-fase). Cada entrega: rebuild + `aspnet_compiler` + tests →
checklist → el usuario prueba en el navegador → commit.

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

## Entrega 4 — Headers estándar de modales
- [ ] Barra de título `[Título] [↗] [X]`: gris claro, ↗ maximiza/restaura, X cierra
      (con la misma acción que hoy tiene cada modal).
- [ ] Encabezado interno según la captura de cada modal: Resource Setup (banda oscura +
      ?), SFC Split (banda "SFC Split"), Assemble, LC (principal y submodal), DC Entry y
      NC Data Entry (subencabezado con ? y línea azul).
- [ ] Browse Work List y los 4 modales del Start de Moldeo heredan la barra estándar.
