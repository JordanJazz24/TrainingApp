# Pasos · alternativa 4a (encabezado H4)

**Spec:** `../022-mockups-por-pantalla.md` · **Rama de partida:** `cloud/022-pasos` (desde `TrainingApp`,
"no verificada") · **Brief:** 4 de `../022-briefs.md`.

Lista de cambios frente al v1, tal como la entregó Claude Design ("Comparación de diseños login
alternativos", `Pasos.dc.html`, versión consolidada). Ajustes de Tweaks con los que se implementó:
**Encabezado = Tinta**, **Período = Lista**, **Acento = Índigo** (los valores por defecto de la
exportación y los de la captura `pasos-claro.png`).

## Alternativa elegida
**4a · Mesa de enseñanza, con el encabezado H4 (banda partida).** Rompe con el v1 en estructura y en
lo visual (D-237): una columna por paso, el paso elegido en un panel propio y el ranking completo
abajo, con fondo cálido, acento índigo y una franja oscura para el título y los filtros.

## Cambios frente al v1
- **Tokens (solo en Pasos, `body.steps-4a`; el resto de la app no cambia):**

  | Token | Claro | Oscuro |
  |---|---|---|
  | `--mt-bg` | `#F5F3EE` | `#111114` |
  | `--mt-surface` | `#FFFFFF` (igual) | `#1A1A1F` |
  | `--mt-surface-2` | `#EEEBE4` | `#24242B` |
  | `--mt-border` | `#DDD8CE` | `#33333C` |
  | `--mt-field-border` | `#857F74` | `#6E6E7A` |
  | `--mt-text` | `#1B1A1F` | `#ECEAE4` |
  | `--mt-muted` | `#5A5650` | `#A8A49C` |
  | `--mt-accent` (y `-rgb`) | `#4B3BC4` | `#A99BFF` |
  | `--mt-accent-hover` | `#3E30A6` | `#C0B6FF` |
  | `--mt-accent-soft` | `#ECE9FB` | `#2A2550` |
  | `--mt-on-accent` | `#FFFFFF` (igual) | `#111114` |
  | `--mt-focus` | `#4B3BC4` | `#C0B6FF` |
  | **nuevo** `--mt-ink` (franja) | `#1B1A1F` | `#0A0A0C` |
  | **nuevo** `--mt-on-ink` | `#F5F3EE` | `#ECEAE4` |
  | **nuevo** `--mt-on-ink-2` | `#BDB8AE` | `#A8A49C` |
  | **nuevo** `--mt-on-ink-line` (divisor de la franja) | `rgba(255,255,255,.14)` | igual |
  | **nuevo** `--mt-bar-2` (columnas no elegidas, período anterior) | `#C4BDF0` | `#4A4290` |

  Los estados (verde, rojo, ámbar, gris) no cambian. La paleta también tiñe la barra superior de
  Pasos (pestaña activa e ícono en índigo). `body.steps-4a` se agregó a la regla que conecta los
  tokens con Bootstrap, para que formularios y botones tomen la paleta. Dentro de la franja,
  `--mt-focus` vale `--mt-on-ink` (el foco se ve sobre el fondo oscuro).
- **`Admin.Master` (toca una pieza compartida, sin efecto en las demás pantallas):** `<body>` pasa a
  `runat="server"` (`AdminBody`) y `AdminMaster` suma `PageCss` (clase extra en `<body>`) y `WideMain`
  (`.admin-main-bleed` con la barra superior visible), como `FullBleed`. Solo Pasos los usa.
- **`PeriodFilter.ascx`:** nueva propiedad `ShowsRange` (por defecto `true`); con `false` no pinta su
  línea de rango, porque Pasos la muestra en la franja. Resumen no cambia. La lista de período es la
  misma (Tweak "Lista").
- **Componentes nuevos, solo de Pasos:** `.steps-band` (`-inner`, `-head`, `-filters`, `-row`,
  `-actions`), `.steps-eyebrow`, `.steps-title`, `.steps-range`, `.steps-seg` (control segmentado),
  `.steps-btn-ghost`, `.steps-btn-ink`, `.steps-wrap`, `.steps-card` (+ `-head`), `.steps-kicker`,
  `.steps-chart`, `.steps-cols` / `.steps-col` / `.steps-labels` / `.steps-label`, `.steps-detail`
  (+ `-why`, `-numbers`, `-change`), `.steps-why*`, `.steps-stats`, `.steps-change-big`, `.steps-vs*`,
  `.steps-list` / `.steps-row` / `.steps-see` / `.steps-up`. Se quitaron `.steps-note`, `.steps-group`,
  `.steps-table`, `.steps-name`, `.steps-rate`, `.steps-rate-line` y `.steps-change` (solo los usaba
  la tabla del v1). Se reutilizan `.steps-reasons`, `.steps-more`, `.mt-change*`, `.mt-alert-warn` y
  `.mt-empty`.
- **Estructura:**
  - Franja de lado a lado bajo la barra: a la izquierda la etiqueta "Pasos · ranking de pasos
    difíciles", el título y el rango aplicado; a la derecha, tras un divisor, Período, Guía, Modo,
    Filtrar y Descargar. En tablet los filtros bajan debajo del título.
  - **Columnas por paso** (HTML/CSS, no Chart.js): cada columna es un botón con su % y debajo
    "#n", el nombre y el cambio. Con muchos pasos se desplazan dentro de la tarjeta.
  - **Paso elegido** (el #1 al cargar): los 2 motivos principales con su acción y su barra, "Ver
    todos (N más)"; % con fallo, "N de M sesiones", intentos fallidos, veces sin acierto, mediana y
    saltos (solo Práctica libre); el cambio con Mejora / Empeora, las barras Anterior y Este período,
    y la referencia y base.
  - **Ranking completo** abajo: #, paso, % con barra, motivo más frecuente y "Ver paso" / "Arriba".
- **Comportamiento:**
  - Elegir un paso (columna o "Ver paso") se resuelve en el cliente, sin postback; "Ver paso" además
    desplaza la vista al panel del paso. Sin JavaScript se ve el #1.
  - **Modo** pasa de `DropDownList` (`DdlMode`) a `RadioButtonList` segmentado (`RblMode`), con los
    mismos valores (`""` y `practice`); Filtrar arma la misma URL.
  - Filtros, período, comparación, Descargar y el aviso de período inválido no cambian.
- **Lo que deja de verse a la vez (marcado por el diseño):** el 2.º motivo, los conteos, la mediana,
  los saltos y la referencia del cambio se ven solo para el paso elegido; cada renglón del ranking
  completo muestra solo su primer motivo. Todo queda a un clic.
- **Textos:** título "Qué pasos fallan más en {guía o "todas las guías"}, en {modo}." (antes "…en
  {modo}, {período}.", el período queda en la línea de rango); etiqueta "Pasos · ranking de pasos
  difíciles"; "{guía} · % de sesiones con fallo"; "Toque una columna para ver el paso."; "#n ·
  elegido"; "#n del ranking · por qué falla"; "Sesiones con fallo"; "Contra el período anterior";
  "Mejora" / "Empeora" visibles; "Anterior" / "Este período"; "Con Todo no hay comparación.";
  "N vez/veces"; "Ranking completo"; "Ver paso"; "Arriba". El rango y "se compara con" salen de los
  mismos textos de la 023.
- **Métricas derivadas (sin datos nuevos):** la parte de cada motivo sobre todos los registrados en el
  paso ("6 veces · 54.5%") y el % con fallo del período anterior (`ReportChange.Previous`, ya
  calculado).
- **Diferencias con el mockup:** los % salen con el formato de la app ("58.3%", no "58 %"); la acción
  de cada motivo es su código registrado (como en el v1), no una frase; la altura de cada columna
  descuenta el espacio de su etiqueta para que el 100% no se salga.

## Para revisar en Desarrollo (no compilado)
- Compila con los controles nuevos y quitados de `Steps.aspx.designer.cs` (`RblMode`, `LitRange`,
  `LitPrevRange`, `PhRanking`, `RptColumns`, `RptLabels`, `RptDetails`, `RptList`; sin `DdlMode` ni
  `RptGuides`), `AdminBody` en `Admin.master.designer.cs` y `PhRange` en `PeriodFilter.ascx.designer.cs`.
  `StepGuideGroup` se eliminó de `StepRows.cs`.
- `<body runat="server">` en el master no rompe `RegisterStartupScript` (SweetAlert) ni el login y el
  cambio de contraseña (FullBleed).
- `RadioButtonList` con `RepeatLayout="Flow"` genera `input` + `label` hermanos (el CSS lo asume) y
  publica `""` para Assessment.
- Elegir columnas y "Ver paso" con mouse y teclado; `aria-pressed` y el foco visible en la franja.
- Una guía con muchos pasos (desplazamiento de las columnas), "Todas", Práctica libre (saltos), "Todo"
  (sin comparación), un paso sin datos en el anterior, Personalizado… e inválido.
- Contraste AA en claro y oscuro de la paleta 4a (índigo sobre `--mt-accent-soft` en la fila elegida).
- Capturas `pasos-claro.png`, `pasos-oscuro.png` y `pasos-tablet.png`: las toma Desarrollo de la versión
  final (D-240).
