# Resumen · alternativa I "Sala" (aprobada 2026-11-02)

> **Enmienda 2026-11-02 (D-320 a D-327):** lo de abajo es el handoff tal cual se guardó antes de
> implementar. Cambió al probar: el libro de Actividad tiene tope de 560 px y desplazamiento
> (D-325); los filtros van siempre bajo el título y el aviso de «Todo» y el rango a la izquierda
> (D-327); `ChangeView.Full` dice «= 0 pts · Sin cambio» (D-326); la barra de pestañas va oculta
> hasta que el script la muestra (D-321). El desvío de cliente lo aprobó el usuario (D-321).

**Spec:** `../022-mockups-por-pantalla.md` · **Brief:** 10 de `../022-briefs.md` (D-252) · **Plan:** `../022-plan.md` → E8b · **Punto de partida:** `TrainingApp` (sin rama de handoff; el paquete es `design_handoff_resumen_sala/`, con el prototipo `Resumen Sala.dc.html` como referencia; incluye la pantalla actual recreada para comparar).

Lista de cambios frente al v1 (el código actual de `Summary.aspx`), tal como se aprobó en el prototipo "Resumen · brief 10" (sesión de diseño, D-284). Tres rondas:

1. La actual recreada y A (tablero alineado), B (parte del período) y C (tres preguntas). Ninguna convenció; descartadas.
2. D (por estado), E (antes y ahora) y F (pizarra). Ninguna convenció; descartadas. El usuario pidió más creatividad y rediseñar también "Dónde se falla más" y "Actividad".
3. G (plano de planta), H (informe en prosa) e **I (sala)**. Elegida **I**, sin ajustes.

## Alternativa elegida

**I · Sala (una pregunta a la vez).** Pensada para proyectar. Los filtros y el encabezado no cambian; debajo, una barra de cuatro pestañas y un escenario (`.summary-stage`) que muestra un paso a la vez:

1. **Listos y mejor o peor.** A la izquierda, un anillo con "46 de 90 aprobados" (estados de todo el historial, sumados). A la derecha, la aprobación del período en 56 px con su veredicto en pastilla y, debajo, una fila por guía (nombre, tasa en 32 px y pastilla de veredicto). Al pie, `LnkFollowUp`.
2. **Por guía.** Una tarjeta por guía (`RptGuides`): barra de estados, conteos con ícono, Aprobados, Al primer intento y Puntaje promedio con su cambio.
3. **Dónde se falla.** El paso n.º 1 en un bloque `--mt-accent-soft` con anillo (% con fallo), nombre en 36 px, detalle y cambio; los pasos 2 a 5 en filas con número, barra, % y cambio. Al pie, `LnkSteps`.
4. **Actividad.** Total de sesiones del período con su cambio y un **libro de barras horizontales** (Chart.js): una fila por día, semana o mes, apilada por modo, con el total al final de cada barra.

Navegación: las pestañas, dos botones (Anterior, Siguiente) y las flechas ← / → del teclado. Entra siempre en el paso 1 (también tras Filtrar).

### Resumen de la crítica (D-284)

- **Fortalezas:** la mejor de las tres para proyectar y para ordenar la conversación; el paso 1 contesta las dos preguntas del brief ("¿quién está listo?" y "¿va mejor o peor?") con el v1 pedía arriba y sin scroll (AC-3); el cambio nunca va solo por color (flecha, valor, palabra y pastilla con ícono).
- **Riesgos:** oculta tres de cuatro bloques (más clics para consultar rápido y no se imprime); Chart.js en una pestaña oculta mide 0 hasta que se muestra; pide un comportamiento nuevo de cliente (ver "Desvíos de alcance").
- **Qué se corrigió:** el paso 1 trae las dos preguntas del brief; sin JavaScript se ven los cuatro bloques apilados; el libro se redibuja al abrir su pestaña; los totales globales dicen que cuentan operadores por guía.
- **Orden de la ronda 3:** 1.º G (plano de planta), 2.º H (informe en prosa), 3.º I (sala). El usuario eligió I.

## Cambios frente al v1

### Tokens

**Ninguno nuevo ni con otro valor**, en claro y oscuro; no afecta a ninguna pantalla ya hecha. Se usan (claro / oscuro): `--mt-surface`, `--mt-surface-2`, `--mt-border`, `--mt-text`, `--mt-muted`, `--mt-accent` `#1F55A5` / `#7EA7EB`, `--mt-accent-soft` `#E3ECF8` / `#1E2F49`, `--mt-on-accent`, `--mt-ok/-soft`, `--mt-bad/-soft`, `--mt-warn/-soft`, `--mt-warn-bar`, `--mt-idle/-soft`, `--mt-m1`, `--mt-m2`, `--mt-m3`, `--mt-shadow`, `--mt-radius-xl` 14 px, `--mt-control-h` 44 px (las pestañas y los botones usan 48 px: `min-height: 48px` local).

### Componentes (`admin.css`, bloque "Resumen: Sala")

- **Nuevos, solo de esta pantalla:**
  - navegación: `.summary-tabs` (`role="tablist"`), `.summary-tab` (con `[aria-selected="true"]`: fondo `--mt-accent`, texto `--mt-on-accent`), `.summary-tab-n` (círculo de 32 px con el número), `.summary-nav` y `.summary-nav-btn` (48 × 48 px);
  - escenario: `.summary-stage` (panel `--mt-radius-xl`, sombra, `min-height: 560px`, relleno 36 px; 24 px bajo 992 px), `.summary-slide` (con `[hidden]`), `.summary-eyebrow` (12 px, 700, mayúsculas, `--mt-muted`);
  - anillo: `.summary-ring` y `.summary-ring-hole`, con `conic-gradient` y variables CSS que pone el servidor en `style` (`--ring-ok`, `--ring-bad` para el global; `--ring-fill` para el paso #1). 260/190 px el global y 200/146 px el del paso #1; 200/146 px en tablet;
  - paso 1: `.summary-glance` (fila de guía: nombre 20 px, tasa 32 px, pastilla), `.summary-rate-big` (56 px);
  - paso 2: `.summary-gcards` (grid `auto-fit, minmax(320px, 1fr)`), `.summary-gcard`;
  - paso 3: `.summary-top1` (bloque `--mt-accent-soft`), `.summary-top-rest`;
  - paso 4: `.summary-ledger` (contenedor del canvas; alto = filas × 32 px + 40, lo pone el servidor en `style`).
- **Modificados:** `.summary-stack` (alto 16 px en las tarjetas), `.summary-counts` (cada conteo con ícono), `.summary-figure` (28 px en las tarjetas), `.summary-top` (filas con número de 24 px, nombre de 20 px y % de 24 px), `.summary-chart` (alto por `style`).
- **Reutilizados sin cambio:** `.mt-badge`, `.mt-badge-ok|bad|idle` (las pastillas de veredicto; con `font-size: .9375rem` local), `.mt-change*`, `.mt-track` / `.mt-fill`, `.mt-alert-warn`, `.summary-alert-link`, `.summary-more`, `.summary-note`, `.summary-legend*`, `.summary-dot`, `.summary-seg-*`, `.summary-mode-*`.
- **Se borran** (revisar con `grep` que ninguna otra pantalla los use): `.summary-ready`, `.summary-ready-head`, `.summary-ready-title`, `.summary-guide`, `.summary-guide-name`, `.summary-guide-title`, `.summary-guide-states`, `.summary-figures`, `.summary-grid`, `.summary-block`, `.summary-block-head`, `.summary-block-title`, `.summary-sessions`, `.summary-top-line`.
- `.mt-hero` ya no se usa aquí.

### Estructura y controles (`Summary.aspx`)

**No cambian** (mismo id, mismo handler): `PnlFilters` (`DefaultButton="BtnFilter"`) con `Period`, `DdlGuide`, `BtnFilter` (`BtnFilter_Click`) y `LnkExport`; `LblInvalidPeriod`, `LblExportSlow`; `LitReadyPeriod`, `LitActivityPeriod`, `LitTopPeriod`; `PhStates` y `LblNoStates`; `RptGuides`; `LnkFollowUp`; `PhSessions` (con `SessionsText` y `SessionsChange`); `PhActivity`, `LitActivityTotals`, `LblNoActivity`; `PhTop`, `RptTop`, `LblNoTop`; `LnkSteps`; el `<canvas>` de actividad (ahora `id="chartActivity"` dentro de `.summary-ledger`).

**Se mueven:** el encabezado (título, bajada y `PnlFilters`) queda arriba; los antiguos bloques "¿Quién está listo?", Actividad y "Dónde se falla más" pasan a ser los pasos 1/2, 4 y 3 del escenario. `PhStates` envuelve el contenido de los pasos 1 y 2 (el vacío se muestra una vez, en el paso 1).

**Se borran:** el `<section class="mt-hero summary-ready">`, el `<div class="summary-grid">` y las dos `<section class="mt-panel …summary-block">` como contenedores (su contenido se reubica).

**Nuevos:**
- barra de pestañas: `<div class="summary-tabs" role="tablist">` con cuatro `<button type="button" role="tab" id="sumTab1…4" aria-controls="sumSlide1…4">` y los botones `sumPrev`, `sumNext` (`type="button"`, sin postback);
- `<section class="summary-stage" aria-live="polite">` con cuatro `<div class="summary-slide" id="sumSlide1…4" role="tabpanel" aria-labelledby="sumTab1…4">`;
- paso 1: `RptGlance` (`Repeater` de solo presentación sobre las mismas filas que `RptGuides`: nombre, tasa y veredicto), y las propiedades de página `GlobalPassedText`, `GlobalPassedDetail`, `GlobalRateText`, `GlobalRateDetail`, `GlobalChange` (`ChangeView`), `GlobalVerdict*` y `GlobalRingOk` / `GlobalRingBad` (anchos del anillo), enlazadas con un `PhGlobal.DataBind()` como `PhSessions`;
- `RptTop` conserva su id, pero su `ItemTemplate` lleva dos `PlaceHolder`: el bloque del n.º 1 (`Visible='<%# Container.ItemIndex == 0 %>'`) y la fila de los demás (`Visible='<%# Container.ItemIndex > 0 %>'`);
- `SummaryGuideRow` suma `VerdictCss`, `VerdictIcon` y `VerdictText`; `SummaryTopRow` suma `RingFill`; `ChangeView` suma `VerdictLabel` ("Mejora", "Empeora" o vacío) y `Full` (texto + " · " + etiqueta; solo el texto si no hay veredicto), sin tocar sus propiedades actuales (Pasos las usa);
- `LblNoActivity`, `LblNoTop` y `LblNoStates` mantienen sus textos.

Hay que actualizar `Summary.aspx.designer.cs` (`RptGlance`, `PhGlobal`).

### Comportamiento

- **Postbacks:** los mismos que hoy (solo `BtnFilter`, que redirige con los filtros en la URL). Cambiar de paso es **cliente, sin postback**.
- **Cliente** (en el `ScriptContent` de la página, junto al de la gráfica, ~40 líneas, sin librerías):
  - al cargar, oculta (`hidden`) los pasos 2 a 4 y marca el 1; **sin JavaScript se ven los cuatro apilados**;
  - clic en una pestaña, en Anterior/Siguiente o ← / → (solo si el foco no está en un campo, un `<select>` o un `<input>`): cambia `aria-selected`, `tabindex` (rotatorio) y `hidden`; la pestaña elegida recibe el foco con el teclado;
  - al mostrar el paso 4, llama `entry.chart.resize()` de la gráfica (Chart.js mide 0 en un contenedor oculto);
  - Filtrar siempre vuelve al paso 1 (la URL no lleva el paso).
- **Gráficas:** siguen en Chart.js con `mtCharts` y se redibujan al cambiar el tema (`mt-theme`). El libro de actividad es `type: 'bar'` con `indexAxis: 'y'`, `scales: mtCharts.axes(t, { stacked: true, horizontal: true })` y los tres datasets de `mtCharts.bars` (`m1`, `m2`, `m3`); el total de cada fila lo escribe un plugin local (como el de hoy, a la derecha de la barra, `ctx.textAlign = 'left'`). **`admin-charts.js` no cambia**: Pasos y Operador no se tocan. Los anillos son CSS (`conic-gradient`), no Chart.js.
- **Período y comparación (023):** intactos. Con Todo no hay comparación: el veredicto dice "Sin comparación" y el global y las sesiones no muestran cambio. Un período inválido muestra `LblInvalidPeriod` y vale Este mes.
- **Veredicto por guía:** "Mejora" (`IsImprovement == true`, `mt-badge-ok`, `bi-graph-up-arrow`), "Empeora" (`false`, `mt-badge-bad`, `bi-graph-down-arrow`), "Sin cambio" (`HasComparison` y dirección igual, `mt-badge-idle`, `bi-dash-lg`), "Sin datos" (sin `HasComparison`, `mt-badge-idle`, `bi-question-circle`) y "Sin comparación" (cambio nulo, con Todo; `mt-badge-idle`, `bi-slash-circle`). Nunca solo color: ícono y palabra.

### Textos nuevos

- Pestañas: "Listos y mejor o peor", "Por guía", "Dónde se falla", "Actividad". Botones: "Anterior" y "Siguiente" (`aria-label`).
- Paso 1: eyebrow "1 · ¿Quién está listo? ¿Va mejor o peor?"; en el anillo "46 de 90" y "aprobados"; debajo "operadores aprobados, contados por guía · 51% del historial"; "Aprobación, {período} · {N de M con Assessment}" y la referencia del cambio; "Sin comparación" y "Con Todo no hay período anterior" cuando no hay.
- Paso 2: "2 · Por guía" y "El detalle de cada guía".
- Paso 3: "3 · Assessment, {período}", "¿Dónde se falla más?", "Paso n.º 1" y "con fallo" (en el anillo).
- Paso 4: "4 · {período}", "Actividad" y "sesiones".
- Veredictos: Mejora, Empeora, Sin cambio, Sin datos, Sin comparación.
- Los textos de la nota de Aprobados / Al primer intento, de los vacíos y de los períodos no cambian.

### Métricas derivadas (D-205), en la BLL y sin test (como D-235, D-283, D-293, D-297, D-304 y D-318)

- `BLL/Reports/SummaryTotals.cs` (nuevo, en el `.csproj`), en `SummaryReport.Totals`; solo suma lo que `GetSummary` ya carga, sin SP nuevo:
  - **Aprobados totales:** ΣPassed y Σ(Passed + Failed + InTraining) de las guías mostradas.
  - **Aprobación global del período:** ΣPassedOperators / ΣAssessedOperators; "—" sin evaluados.
  - **Cambio global:** `ReportChange.Points(actual, ΣAssessed, anterior, ΣPreviousBase, true)`, con el anterior = Σ(Previous × PreviousBase) / ΣPreviousBase de las guías con comparación; `null` con Todo.
  - Los operadores se cuentan **por guía**: quien entrena en dos guías suma dos veces (por eso el texto "contados por guía"). **Negocio debe validar esta cifra global.**
- **Veredicto por guía:** de `PassRateChange` (ya calculado); solo presentación.
- Los porcentajes del anillo y de las barras (anchos) son presentación y salen con `ReportText.BarWidth`, como hoy.
- Nada cambia las definiciones de Aprobados, Al primer intento, Puntaje promedio, Actividad ni el top 5 (AC-5).

## Para revisar en Desarrollo (no compilado)

- Rebuild y `aspnet_compiler`: se toca `.aspx`; el `.designer.cs` se edita a mano. `<%# %>` en atributos de controles de servidor y `<%#: %>` en HTML literal (`CLAUDE.md`). El `style` con variables CSS (`--ring-ok: 51%`) va con `<%#: %>`.
- Los tests existentes de `DashboardService` siguen en verde; no cambia nada de la BLL salvo `SummaryTotals`.
- **Casos de prueba (los seis):**
  - varias guías con estados mezclados (una Mejora, una Empeora, una Sin cambio);
  - una guía sin Assessment en el período ("—", "Sin Assessment en el período", "Sin datos");
  - un período sin actividad (vacíos del paso 4 y del paso 3; estados del paso 1 con todo el historial);
  - **Todo** (sin comparación, aviso de descarga lenta, actividad por mes);
  - **Personalizado** (Inicio y Fin, rango en palabras, actividad por día);
  - un período inválido (el aviso y Este mes).
  - Además: sin ninguna sesión (`LblNoStates`) y con una guía elegida en `DdlGuide`.
- **Enlaces:** "Ver seguimiento" (con la guía, sin período), "Ver todos los pasos" (con el período, Personalizado y guía) y el menú a Operadores; "Descargar sesiones" con los filtros.
- **Los números son los mismos que hoy:** comparar con el v1 cada cifra de cada guía (estados, Aprobados, Al primer intento, Puntaje), las sesiones y su cambio, la actividad y el top 5 con su cambio.
- **Claro, oscuro y 768 px:** sin scroll horizontal de la página; las pestañas se parten en filas; el anillo baja a 200/146 px; la gráfica se redibuja al cambiar el tema y al mostrar el paso 4.
- **Teclado y lectores:** ← / → cambian de paso solo fuera de campos; Tab llega a la pestaña elegida; `aria-selected`, `aria-controls`, `aria-live="polite"` en el escenario; los anillos con `role="img"` y su `aria-label`.
- **Contraste AA (y 3:1 en componentes):** `--mt-on-accent` sobre `--mt-accent` en la pestaña elegida; `--mt-ok` / `--mt-bad` / `--mt-idle` sobre sus `*-soft` en las pastillas; texto y números sobre `--mt-accent-soft` en el bloque del paso #1; `--mt-muted` sobre `--mt-surface`; el borde del anillo (relleno `--mt-ok` / `--mt-bad` / `--mt-warn-bar` sobre `--mt-surface-2`).
- Que el texto de "contados por guía" y la cifra global los valide negocio.

## Desvíos de alcance

- **Datos nuevos (SQL o tablas):** ninguno.
- **Una regla de negocio nueva:** ninguna (el veredicto y los totales son presentación; no tocan `ReadinessPolicy`, `StepRuns` ni `FollowUpRules`).
- **Otro comportamiento (otros controles, postbacks o flujo de errores):** **sí, de cliente.** Pestañas y teclado ← / → para cambiar de paso, sin postback, y Filtrar siempre vuelve al paso 1. Los controles de servidor, los ids, los postbacks y el flujo de errores no cambian; se añaden solo `RptGlance` y `PhGlobal` (presentación). **Desarrollo se detiene aquí y lo confirma con el usuario antes de implementar.**
- **Tocar los masters:** ninguno.
- **Tokens que cambien pantallas ya cerradas:** ninguno.
- **Algo que el brief deja en "Queda fuera":** nada (no vuelve "N sin iniciar" ni el número de Seguimiento; sin filtro por área, sin comparar operadores, sin detalle de un operador o de un paso, sin el fin de sesión del POD).
