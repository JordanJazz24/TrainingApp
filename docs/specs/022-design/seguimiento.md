# Seguimiento · alternativa B con el encabezado P1 (aprobada 2026-10-30)

**Spec:** `../022-mockups-por-pantalla.md` · **Brief:** 5 de `../022-briefs.md` ·
**Rama de partida:** `cloud/022-seguimiento` (desde `TrainingApp`, "no verificada").

Lista de cambios frente al v1, tal como se aprobó en el canvas "Seguimiento · spec 022" (sesión de
diseño en la nube, D-284). Hubo cuatro rondas:

1. La actual recreada y tres alternativas: A (lista agrupada), **B (cola de llamadas)** y C (matriz
   por persona). El usuario eligió **B**.
2. Dos variantes de B (B2, cola con titular; B3, cola y ficha del elegido). Se descartaron: el
   usuario se quedó con B tal cual.
3. Dos encabezados para B (H1, titular-frase; H2, siguiente llamada). Se descartaron.
4. Dos encabezados más: **P1, encabezado-índice** (el elegido), y P2, marcador.

## Alternativa elegida

**B · Cola de llamadas, con el encabezado P1 "encabezado-índice".** Rompe con la tabla del v1.

- **Encabezado:** una tarjeta blanca (`--mt-radius-xl`, sombra) con el título, la bajada y los
  filtros (Guía, Filtrar y Descargar) arriba. Debajo, una fila de **pestañas-índice** con la cifra
  en grande:
  - "**N** Llamar primero" (subrayada en `--mt-accent`);
  - "**N** Después";
  - a la derecha, los casos por regla (S-1, S-2 y S-3 con su número) y "N casos de N operadores".

  Las pestañas son anclas (`#fu-first`, `#fu-later`) que bajan a su sección, sin postback.
- **Llamar primero** (2 reglas o más): una tarjeta por caso con:
  - el nombre (enlace al detalle), "Code1 n", la guía con su título, "N de 3 reglas" y el estado;
  - las reglas como **frases completas con su etiqueta S-n**, las mismas del Excel;
  - "Última sesión de la guía" y el botón "Ver detalle".
- **Después** (1 regla): una fila compacta por caso, en dos líneas: nombre, Code1, guía, estado,
  la frase con su S-n, la última sesión y un chevron al detalle.
- **Columna lateral "En la cola":** el total de casos y, por regla, su número, una barra con su
  parte del total y su definición. Abajo, la nota de que es todo el historial.
- **Vacío:** "Nadie en la cola de <guía>" (o "Nadie en la cola" con Todas), sin pestañas ni cola.

### Resumen de la crítica (D-284)

- **B** quedó 2.ª en la primera ronda. Es la que mejor dice de qué hablar con cada uno y deja
  "Ver detalle" a la vista.
  - **Riesgos:** con muchos casos de 2 reglas o más la página se alarga (~200 px por tarjeta), los
    totales quedan abajo en tablet y una persona con 2 guías sale dos veces.
  - **Corregido en el mockup:** "Después" usaba chips y las tarjetas, frases; ahora las dos usan la
    etiqueta S-n con su frase.
- **P1** quedó 2.º en la cuarta ronda, detrás de P2. Es la más segura y la más barata, y el ojo va
  a "Llamar primero".
  - **Riesgos:** las pestañas parecen filtros, pero solo bajan a su sección. Los conteos por regla
    del encabezado repiten la columna lateral; el diseño aprobado la mantiene igual.

## Cambios frente al v1

### Tokens

Ninguno nuevo ni con otro valor, ni en claro ni en oscuro.

### Componentes (`admin.css`, bloque "Seguimiento")

- **Se borran:** `.followup-rules` (la lista de reglas suelta), `.followup-table` y
  `.followup-chips`. Las reglas ya no se pintan como `.mt-chip`; `.mt-chip` sigue igual para Guías.
- **Nuevos, solo de esta pantalla:**
  - encabezado: `.followup-head`, `-head-top`, `-tabs`, `-tab` (con `is-lead`), `-tab-n`, `-tab-t`,
    `-rule-counts` y `-rule-count`;
  - etiqueta S-n: `.followup-tag` (`is-alert` para S-1, en `--mt-bad`, siempre con su texto);
  - distribución: `.followup-layout`, `-col` y `-aside`;
  - secciones: `.followup-sec-head` y `.followup-code`;
  - tarjetas: `.followup-queue`, `-card`, `-card-top`, `-card-name`, `-card-guide`, `-card-side`,
    `-count`, `-why`, `-card-foot` y `-none`;
  - filas compactas: `.followup-later` y `.followup-l-who`, `-l-guide`, `-l-state`, `-l-why`,
    `-l-line`, `-l-last` y `-l-go`;
  - vacío: `.followup-empty-title`;
  - columna lateral: `.followup-total`, `-bars`, `-bar-top`, `-bar-d` y `-aside-note`.
- **Se reutilizan:** `.mt-panel`, `.mt-panel-body`, `.mt-badge-*`, `.mt-track` y `.mt-fill`,
  `.mt-empty`, `.mt-section-title`, `.mt-block-title` y `.btn`.
- **Responsive:**
  - la columna lateral baja cuando no cabe (flex con `flex-wrap`);
  - a 767 px o menos, las pestañas ocupan la mitad cada una, los conteos por regla bajan a su
    propia fila y las filas de "Después" se apilan.

### Estructura y controles (`FollowUp.aspx`)

- **Sin cambio:** `PnlFilters`, `DdlGuide`, `BtnFilter`, `LnkExport` y `LitCount`.
- **Se borran:** `RptRows` y `PhTable`.
- **Nuevos** (en el `.designer.cs`):
  - encabezado: `PhTabs`, `LitTabFirst`, `PhTabLater`, `LitTabLater`, `LitS1`, `LitS2` y `LitS3`;
  - Llamar primero: `PhQueue`, `LitFirstCount`, `PhFirst`, `RptFirst` y `PnlNoFirst` ("Nadie cumple
    2 o más reglas con este filtro");
  - Después: `PhLater`, `LitLaterCount` y `RptLater`;
  - vacío: `LitEmptyTitle`, dentro de `PnlEmpty`;
  - columna lateral: `LitTotal`, `LitTotalText`, `LitBarS1` a `LitBarS3` y `BarS1` a `BarS3` (el
    ancho de cada barra va en `Style["width"]`).
- `FollowUpListRow`:
  - **sin** `Chips`;
  - **con** `RuleCountText` ("3 de 3 reglas") y `Lines` (`FollowUpLine`: `Code`, `Text`, `Icon`,
    `Css` y `TagCss`), que reemplaza a `FollowUpChip`.
- `ReportText`:
  - **sin** `FollowUpChips`;
  - **nuevos** `FollowUpReasonLines` (S-n y frase por separado), `FollowUpRuleCount` y `Cases`;
  - `FollowUpLines` (Excel) ahora une `FollowUpReasonLines` con " · ", con **el mismo texto que
    antes**.

### Comportamiento

- No cambia: el filtro en la URL (`?guide=`), Filtrar, Descargar, el enlace al detalle y el orden
  (el de la BLL, primero quien cumple más reglas).
- Las pestañas del encabezado son anclas en el cliente.
- El reparto entre "Llamar primero" y "Después" lo decide `FollowUpSummary.IsCallFirst`.

### Textos nuevos

- Bajada: "A quién llamar primero y de qué hablarle, en guías que todavía no aprueba." Antes: "Quién
  necesita atención en una guía que todavía no aprueba, y por qué."
- Pestañas: "Llamar primero · Cumplen 2 o más reglas" y "Después · Cumplen 1 regla".
- Conteos:
  - "N casos de N operadores"; antes "N operadores en N casos (operador y guía)";
  - "N casos con 2 o más reglas" y "N casos con 1 regla".
- Tarjetas y filas: "N de 3 reglas", "Última sesión de la guía: …", "Ver detalle" y "Por qué" (lista
  de reglas, para lectores de pantalla).
- Frases de las reglas, sin cambio: son las del Excel, con su S-n en una etiqueta aparte.
- Columna lateral: "En la cola" y los nombres "Reprobados", "Sin practicar" y "Paso trabado", con la
  definición de cada regla (sale de las constantes de `FollowUpRules`).
- Vacío: "Nadie en la cola de <guía>", "Ningún operador requiere atención: nadie cumple S-1, S-2 ni
  S-3 con este filtro." y "Quien aprobó la guía ya no aparece." Antes: "Ningún operador requiere
  atención."

### Métricas derivadas (D-205), en la BLL y sin test, como D-235 y D-283

`BLL/Reports/FollowUpSummary.cs` (nuevo, en el `.csproj`) va en `FollowUpReport.Summary` y solo
cuenta las filas que `GetFollowUp` ya devuelve:

- casos con 2 reglas o más ("Llamar primero", `CallFirstMinRules = 2`) y con 1 regla ("Después");
- casos por regla: S-1, S-2 y S-3 (un caso puede contar en varias);
- operadores y casos (antes el conteo de operadores se hacía en el code-behind).

No cambia `FollowUpRules`, `ReadinessPolicy`, `StepRuns` ni el orden. El ancho de las barras (su %
del total) es presentación: va en el code-behind.

## Para revisar en Desarrollo (no compilado)

- Rebuild y `aspnet_compiler`:
  - `FollowUp.aspx` usa `<%: … %>` con las constantes de `FollowUpRules` y `FollowUpSummary` dentro
    de `title` de elementos HTML y dentro de `PnlNoFirst`;
  - el `.designer.cs` se escribió a mano.
- `ExcelReportsTests` tiene que seguir en verde: `FollowUpLines` se reescribió sobre
  `FollowUpReasonLines` con el mismo texto ("S-1 · Reprobó N Assessment sin aprobar ninguno"…).
- Casos que revisar:
  - solo casos de 1 regla: "Nadie cumple 2 o más reglas…", sin pestaña "Después" rota;
  - solo casos de 2 reglas o más: sin la sección ni la pestaña "Después";
  - vacío con Todas y con una guía;
  - S-3 con varios pasos: una frase por paso.
- Los anclas `#fu-first` y `#fu-later`: que bajen bien con la barra superior (`scroll-margin-top`).
- Claro, oscuro y 768 px: la columna lateral baja, las pestañas se reparten y no hay scroll
  horizontal de la página.
- Contraste de `.followup-tag.is-alert` y de la pestaña `is-lead` en oscuro.
- Capturas (D-240, claro y oscuro): las toma Desarrollo de la versión final.
