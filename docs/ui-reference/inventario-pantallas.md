# Inventario de pantallas — Fase 6.5 (fidelidad de UI/UX con el SAP MES real)

Lista cerrada de las pantallas y modales del POD que el operador ve, cada una con sus
capturas del MES real y su estado. Es el **criterio de cierre de la Fase 6.5** y la
**entrada de la spec de overlays guiados** (Fase 6.6): los overlays se anclan a estas
pantallas.

- Capturas: `docs/ui-reference/Screens SAP MES REAL/` (rutas relativas a esa carpeta; hasta
  el 2026-10-08 estaban en `C:\Users\320313439\Pictures\TrainingApp\`).
- Detalle de cada pantalla: `general-flow.md` y `moldeo-flow.md`.
- Plan y entregas de la fase: `docs/specs/fase-6.5-plan.md`.

**Estados**
- ✅ **Fiel** — replica su captura; no quedan diferencias pendientes.
- 🟡 **Con diferencias** — hay captura y quedan diferencias listadas abajo.
- ⚪ **Sin captura** — no hay captura del MES real; no se inventa: se pide la captura.

**Cierre de la fase:** todas las pantallas con captura en ✅. Las ⚪ quedan anotadas como
pendientes (no bloquean). Una diferencia se puede cerrar también como **aceptada** si es
una regla del simulador; se anota en la columna de notas.

**Cómo mantenerlo:** al cerrar cada entrega, actualizar el estado y las diferencias de
las pantallas tocadas. Si aparece una captura nueva, agregarla y revisar su pantalla.

_Última revisión: 2026-09-29, después de la entrega 4 (`b832e77`). Comparación hecha
contra las 25 capturas de la carpeta. Estados actualizados el 2026-10-19 (cierre de la
spec 014, #17-#23)._

## Pantallas del MES

| # | Pantalla | Paso | Capturas | Estado |
|---|---|---|---|---|
| 1 | POD (header, POD Search, acciones, POD Work List, panel derecho, pie) | todos | `pantall central de referencia`, `Search - Start order/pod cargado con la orden selcionada`, `Search - Start order/campo de search`, `Split - Log NC/seleccion de la orden o sfc y click en LOG NC`, `Assemble/se seleciona el lote o todas las piezas del POD` | ✅ |
| 2 | Banner de mensajes | — | `mensaje de banner correcto`, `Split - Log NC/mensaje confirmacion scrap` | ✅ |
| 3 | Menú Activities | 2 y 6 (LC) | `Line Clearence/boton activies, dropdown de opciones` | ✅ |
| 4 | Browse Work List | 3 | `Search - Start order/browse work list` | ✅ |
| 5 | Resource Setup | 1 | `Resource Setup/resource setu modal`, `Resource Setup/resource setup modal con datos`, `Resource Setup/record changed correcto` | ✅ |
| 6 | Data Collection Standalone (Line Clearance) | 2 y 6 | `Line Clearence/modal data collection standalone`, `…modal line clearence`, `…mas nitido`, `…dc group save` | ✅ |
| 7 | Data Collection Entry de Line Clearance (submodal) | 2 y 6 | `Line Clearence/modal line clearence- sub modal de la pregunta` | ✅ |
| 8 | Start de Moldeo: 4 modales Data Collection Entry | 3 (Moldeo) | — | ⚪ |
| 9 | Component List | 4 | `Assemble/component list` | ✅ |
| 10 | Assemble Components | 4 | `Assemble/asemble components modal - estado anterior al escaneo`, `…posterior al escaneo` | ✅ |
| 11 | Data Collection List | 5 | `DC COLLECT/tabla de data collection list` | ✅ |
| 12 | Data Collection Entry (DC Collect) | 5 | `DC COLLECT/modal data collection entry 1`, `…2` | ✅ |
| 13 | SFC Split Plug-In | 7 | `Split - Log NC/modal sfc split`, `Split - Log NC/split creado` | ✅ |
| 14 | NC Selection | 8 | `Split - Log NC/mensaje confirmacion scrap` (muestra la lista) | ✅ |
| 15 | NC Data Entry | 8 | `Split - Log NC/MODAL NC DATA ENTRY` | ✅ |
| 16 | Resultado de Complete | 9 | — | No aplica: en el MES real Complete no tiene pantalla, solo termina la orden (usuario, 2026-10-07) |
| 17 | Browse de componentes (⧉ de Component en NC Data Entry) | 8 | `Split - Log NC/browse component` | ✅ spec 014 |
| 18 | Confirm "Do you want to remove component from {SFC}?" | 8 (reparación rápida) | `Split - Log NC/mensaje eliminar componente` | ✅ spec 014 |
| 19 | Remove component (As-Built Configuration) | 8 (reparación rápida) | `Split - Log NC/remove compoenet modal ` | ✅ spec 014 |
| 20 | Confirm "Do you want to add component to {SFC}?" | 8 (reparación rápida) | `Split - Log NC/confirmacion agregar compoenente ` | ✅ spec 014 |
| 21 | Add Component | 8 (reparación rápida) | `Split - Log NC/modal add componenet `, `Split - Log NC/recorte de agregado del nuevo compoenente ` | ✅ spec 014 (*aceptada:* Add Assembly Details solo con ★ Batch Number; la variante por material del recorte quedó diferida, enmienda 6) |
| 22 | NC Data Entry de la extendida (★ Comments, "NC logged") | 8 (extendida) | `Split - Log NC/modal nc entry extended` | ✅ spec 014 |
| 23 | Select Disposition Function | 8 (extendida) | `Split - Log NC/sub modal seleccion operaicon a ser devuelta ` | ✅ spec 014 |

**Resumen:** ✅ 21 · 🟡 0 · ⚪ 1 (#8) · no aplica 1 (#16, 2026-10-07). Las #17-#23 (capturas
del 2026-10-15) pasaron a ✅ con la spec 014 (2026-10-19), probadas por el usuario. La captura `Split - Log
NC/modal sfc split` que citan la #13 y `general-flow.md` no está en la carpeta. **Criterio de cierre cumplido (2026-10-01, tras la entrega 9):** todas las pantallas con captura están fieles; la #8 queda pendiente y no bloquea.

## Diferencias pendientes por pantalla

**Transversal (afecta a 4, 5, 6, 7, 9, 11, 12, 14 y 15):** los botones dentro de los
modales y listas del MES son grises, con ícono y texto (Save, Clear, Retrieve, Collect,
Assemble, Log NC, OK/Cancel, Add-Done…). _Estilo común creado en la entrega 6
(`.mes-btn` + `.mes-ico-*` en `pod.css`) y aplicado en Browse y Line Clearance; falta en
las listas (entrega 7) y en DC/NC Entry (entrega 8)._

1. **POD** — ✅ (el usuario lo dio por fiel, 2026-10-01). La cuadrícula de filas vacías de
   la tabla se replicó después, a pedido del usuario (fondo del panel con el mismo paso de
   32px que las filas). *Aceptadas:* la × del campo SFC no se replica; botones deshabilitados en
   Guided (regla del simulador); orden del panel derecho según la captura de referencia (el
   MES lo cambia según la configuración del POD).
2. **Banner** — ✅. Textos con captura: grupo de DC y scrap. Los de Start, Assembly, Split
   y "SFC … completed. Next: …" no tienen captura.
4. **Browse Work List** — ✅ (el usuario lo dio por fiel, 2026-10-01): línea celeste bajo el
   encabezado y [✓ OK] [✗ Cancel] al estilo MES (entrega 6). *Aceptada:* la flecha de
   orden de la columna SFC no se replica.
5. **Resource Setup** — ✅ (entrega 5): campos centrados, franja celeste de acciones a la
   izquierda, tabla de dos columnas de ancho fijo con ⧉ que copia al portapapeles, pie
   centrado y "Record changed" como el real. Nota: `Resource Setup/…pop up de record
   change` es una captura del **legacy**; la real es `record changed correcto`.
6. **Data Collection Standalone**
   - `Filter Operation` y `* Collect Data For` como campos (dropdown "Resource" + recurso
     con ⧉); [Clear] y [Retrieve] grises con ícono, a la derecha.
   - Fila: `DC_LC_DONE` con la pregunta en cursiva debajo ("Realizó la Limpieza de
     Linea?"); `DC Group/Vers.: DCGTM_LC/A` con la fecha del último registro; [Collect]
     gris con ícono.
   - Mensaje de guardado real: "⚠ 1 DC Groups saved" (hoy "◆ dc group save").
   - _Entrega 6 ✅: todo lo anterior, con barra solo ⤢ ✕,
     encabezado con línea celeste y Clear que limpia el formulario._
7. **Data Collection Entry de LC** — Barra, subencabezado y fila de contexto ✅ (entrega 4).
   Cuerpo: `Data Type: Boolean` y [0 files attached] a la derecha del campo; botones del
   pie al estilo MES. _Entrega 6 ✅: cuerpo y pie como el real,
   y apilado sobre el principal con velo suave._
9. **Component List**
   - Fila: número de parte y Qty a la izquierda; `Description:` / `Rem Qty:` como
     etiqueta: valor al centro; [Assemble] y [Disassemble] grandes, grises, con ícono.
   - Paginación centrada con "Rows 1-N of N" debajo; (?) y × del encabezado como íconos.
   - _Entrega 7 ✅: todo lo anterior._
11. **Data Collection List** — Código del grupo grande; descripción al centro con
    "Collect Data At:" debajo; [Collect] gris con ícono; paginación centrada.
    _Entrega 7 ✅: todo lo anterior._
12. **Data Collection Entry (DC Collect)** — Barra, subencabezado y fila ✅ (entrega 4).
    Cuerpo: cada parámetro en un recuadro — `* Etiqueta:` en negrita, `Description: …`,
    campo, `Data Type: …` al centro y [Comments] a la derecha; pie con Save, Save Draft,
    Clear y Return to POD grises con ícono. _Entrega 8 ✅: todo
    lo anterior, con la jerarquía de encabezados del real (barra solo ⤢ ✕ → "Data
    Collection Entry" (?) → SFC → DC Group), estilo de la captura nueva `modal data
    collection entry` y la columna `Description` (Moldeo con los textos del legacy)._
13. **SFC Split** — Barra y banda ✅ (entrega 4).
    - Cuerpo: etiquetas a la derecha (`SFC to Split`, `Available Qty`, `* Qty to Split` con
      × dentro del campo); [Split] [Close] centrados.
    - Confirmación real: "New SFC {sfc} created with quantity {n}" (hoy "New split row
      created with quantity n"). ⚠ El MES crea un **SFC nuevo**; el simulador reusa el SFC
      del lote (spec 007). Cambiar solo el texto es UI; cambiar el SFC de la fila nueva es
      una regla y se decide aparte.
    - _Entrega 9 ✅: captura nueva `modal sfc split` — banda
      oscura "SFC Split" (como Resource Setup), mensaje "☑ New SFC … created with quantity
      …" arriba, formulario centrado con etiquetas en negrita, campo amarillo claro con ×
      de cliente y [Split] [Close] de contorno celeste; sin mensajes de validación bajo el
      campo. Regla resuelta por el usuario (enmienda 2026-10-01 de la spec 005): el split
      crea un **SFC nuevo** de la misma Shop Order y el mensaje lo muestra._
14. **NC Selection** — Celdas sin recuadro: código en negrita, descripción en cursiva,
    [Log NC] gris con ícono. (La captura recortada "NC Selection table" ya no está en la
    carpeta.) _Entrega 7 ✅: todo lo anterior, con la paginación
    centrada del patrón común._
15. **NC Data Entry** — Barra y subencabezado ✅ (entrega 4).
    - Subencabezado real con contador: `SFC: … (1/1)`.
    - Cuerpo: etiquetas a la derecha (`* NC Code`, `Component` con ⧉, `Comments` como área
      de texto). En la captura, Component no lleva asterisco.
    - Pie: Add, Add-Done, Return to POD y Done grises con ícono.
    - _Entrega 8 ✅: barra solo ⤢ ✕ → "NC Data Entry" (?) → SFC,
      formulario y pie como el real; Component sin asterisco (sigue obligatorio);
      `SFC (1/N)` y "Apply to all SFCs" con varias piezas, como Assemble._

## Capturas que faltan (pedirlas en planta)

- Los 4 modales del Start de Moldeo (#8).
- ~~Lo que muestra el MES al ejecutar Complete (#16)~~: no tiene pantalla (2026-10-07).
- ~~Los banners de Start, Assembly y Split~~: no hacen falta por ahora (2026-10-07).
- NC Selection completa (la recortada se perdió).
- Mensajes del MES sin captura, detectados en la revisión con SAP MES (2026-10-09): el error
  al iniciar una pieza de otra orden, el de material equivocado en Assembly y el banner de
  avance entre piezas en Data Collection. Fuente posible: el ambiente de calidad de SAP MES
  que ofreció el ingeniero (acceso por coordinar).
- Detectadas al contrastar la WI QD001001524 (2026-10-12, `wi-produccion-disposables.md`):
  la Data Collection List con un grupo ya guardado (✔ en [Collect], W15), las ventanas de
  Log Buyoff y de firma electrónica al completar (W17). Assembly Data (W13) quedó resuelto
  con las imágenes de la propia WI (2026-10-15).

## Elementos propios del simulador (fuera de la comparación)

No existen en el MES real. No se comparan contra capturas, pero los overlays guiados los
tienen que considerar: **Score** (header), **Abandon Session** (pie), **Home** y **Help**
(solo visuales), la **pantalla final de la sesión** y los **SweetAlerts** de formato e
intento.

**Fuera del alcance de la Fase 6.5:** las páginas del simulador que no son del MES
(Login, Cambio de contraseña, StartSession). Desde el 2026-10-08, Login y Cambio de
contraseña replican el login SAP NetWeaver real (captura `Login SAP NETWEAVER.png`) en
`Auth.Master`; la foto y el logo de SAP están recortados de esa captura. Cambio de contraseña
no tiene captura: usa el mismo marco con los textos del cambio de contraseña de NetWeaver
(Old/New/Confirm Password, Change) más Cancel, que cierra la sesión.
