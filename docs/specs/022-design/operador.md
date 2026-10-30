# Operador · alternativa H "Tres preguntas con historial por guía" (aprobada 2026-10-30)

**Spec:** `../022-mockups-por-pantalla.md` · **Brief:** 7 de `../022-briefs.md` (enmendado con la
spec 024) · **Rama de partida:** `cloud/022-operador` (desde `TrainingApp`, "no verificada").

Lista de cambios frente al v1 (el código de la 024), tal como se aprobó en el canvas
"Operador · spec 022" (sesión de diseño en la nube, D-284). Hubo tres rondas:

1. La actual recreada y A (veredicto con qué hacer), B (plan de refuerzo) y C (expediente y
   recorrido). Ninguna convenció: se borraron.
2. D (una guía a la vez), E (la ficha es la pantalla) y F (tres preguntas). Gustó **F**.
3. Dos variantes de F que además mejoran la curva y la tabla de Sesiones (pedido del usuario): G
   (evolución en una gráfica con pestañas y sesiones filtrables) y **H (historial por guía)**,
   elegida.

## Alternativa elegida

**H · Tres preguntas con historial por guía.**

- **Encabezado:** "Operadores" (volver), el nombre, "Code1 n" y la etiqueta "Área · Producto ·
  Área" (en el acento si tiene área); a la derecha, "Imprimir ficha".
- **Tres preguntas**, tres tarjetas lado a lado (la del medio más ancha):
  - **¿Está listo en su área?** Por cada guía de su área (también Sin iniciar): la insignia de
    estado, el nombre y el título de la guía, mejor y último puntaje, "Última actividad … · hace
    N días. N puntos menos que su mejor." y, al pie, la línea de **qué hacer** de Operadores
    (`ReportText.StateHint`). Sin área: la insignia "Sin área" y la línea de Sin área; sin guía
    publicada: "Sin guía publicada".
  - **¿Qué le falla?** Los pasos a reforzar agrupados por guía con su etiqueta "Su área" / "Otra
    área": cada paso con sus intentos fallidos en grande, el motivo y las veces sin acierto. Los
    de otras áreas arrancan ocultos y los muestra "Ver también otras áreas (N pasos)" en el
    cliente; si no hay de su área, salen todos.
  - **¿Qué ha hecho?** "N sesiones", "Última: fecha · hace N días", una barra con el reparto por
    modo (Guided, Práctica libre, Assessment) con su cuenta, y "También entrenó: …".
- **Historial por guía:** un `<details>` por guía con sesiones, su área primero y abierta; las
  otras plegadas (abiertas si no tiene área, P-8).
  - **El resumen** hace de tarjeta de la guía: Producto · Área y título, estado, mejor, último,
    sesiones, la etiqueta Su área / Otra área, la última actividad y "hace N días". Reemplaza las
    tarjetas de "Otras áreas".
  - **La curva** (Chart.js) en dos paneles alineados por sesión: arriba el puntaje de cada
    Assessment (línea con su valor, ✓ y punto verde si aprobó) con la línea punteada del mínimo
    para aprobar (70%, `ScoreCalculator.PassingScore`); abajo los intentos fallidos de cada
    sesión de Práctica libre y Assessment (barras con su número). El eje dice "#n", el día y el
    modo.
  - **La lectura**, a la derecha: la práctica antes del primer aprobado, "Puntajes de
    Assessment: a → b → c" y tres cifras (Assessment, aprobados, intentos fallidos).
  - **Sus sesiones**, en una tabla sin la columna Área: día y hora en dos líneas, el modo con su
    color, el estado con ícono, el puntaje con un medidor (marca en 70%) y "Aprobó / Sin
    aprobar", la etiqueta "Mejor" en su mejor Assessment, la duración y los intentos fallidos con
    una barrita.

### Resumen de la crítica (D-284)

- **F** quedó 1.ª en la segunda ronda: responde el estado y los pasos a reforzar en el primer
  pantallazo y se lee proyectada.
  - **Riesgos:** en tablet las tarjetas pasan a 2 + 1; la tarjeta de pasos crece con muchos
    pasos; el reparto por modo mezcla las áreas.
- **H** quedó 2.ª en la tercera ronda (detrás de G) y es la elegida: cada guía junta su curva y
  sus sesiones, y dos paneles se leen mejor que dos ejes.
  - **Riesgos:** se aparta del brief ("Sesiones, tabla completa"): en pantalla las sesiones se
    parten por guía sin la columna Área (la ficha impresa conserva la tabla única con Área); la
    página crece con cada área; más costo que G.
  - **Corregido en el mockup:** el bloque de cifras tenía el valor antes que su etiqueta en el
    HTML.

## Cambios frente al v1

### Tokens

Ninguno nuevo ni con otro valor, ni en claro ni en oscuro. Los modos usan `--mt-m1` a `--mt-m3`
(por `.summary-dot` / `.summary-mode-*` del Resumen) y el mínimo para aprobar, `--mt-ok`.

### Componentes (`admin.css`, bloque "Operador")

- **Se borran:** `.operator-top`, `.operator-guides`, `.operator-guide`, `.operator-guide-name`,
  `.operator-guide-data`, `.operator-reinforce`, `.operator-reinforce-guide`, `.operator-section`,
  `.operator-section-head`, `.operator-curves`, `.operator-curve` y `.operator-other` (con su
  `@media`). En `@media print` se quita `.operator-top`.
- **Nuevos, solo de esta pantalla:**
  - encabezado: `.operator-head`, `.operator-head-sub` y la etiqueta `.operator-tag` (con
    `is-mine`);
  - preguntas: `.operator-qs`, `.operator-q` (y `-wide`), `-q-title`, `-q-guide`, `-q-name`,
    `-q-pair`, `-q-note`, `-q-do`, `-q-group` (y `-q-other`, `-q-group-head`), `-q-step` (`-name`,
    `-why`, `-fails`), `-q-total`, `-q-also`, `.operator-mix` y `.operator-mix-counts`;
  - historial: `.operator-history-head`, `.operator-hist` (`-sum`, `-chev`, `-name`, `-figs`,
    `-act`, `-body`, `-chart`, `-read`, `-practice`, `-facts`, `-table`);
  - sesiones: `.operator-day`, `-time`, `-mode`, `-status`, `-score` (`-score-value`),
    `.operator-meter` (la marca de 70% es un `::after`), `.operator-verdict` y `.operator-fails`.
- **Se conservan:** `.mt-badge-lg` y `.operator-reinforce-none`.
- **Responsive:** bajo 1200 px las preguntas pasan a 2 + 1 (pasos abajo) y el resumen del bloque
  baja sus cifras a una segunda línea; bajo 992 px la curva y la lectura se apilan; a 767 px o
  menos, una pregunta por fila y sin la columna de actividad del resumen. Las tablas se desplazan
  dentro de su bloque.
- `admin-charts.js` suma `mtCharts.valueLabels(t)`, un plugin que escribe sobre cada punto o barra
  el texto de `dataset.mtLabels` (con un halo del color de la superficie).

### Estructura y controles (`Operator.aspx`)

- **Sin cambio:** `LnkBack`, `LitPrinted`, `LitName`, `LitCode1`, `LitArea`, `RptGuides`,
  `RptReinforcement` (ahora con los pasos como filas, no tabla), `PnlNoReinforcement`,
  `LitAlsoTrained`, `RptSessions` (ahora solo en la ficha impresa) y `LblNoSessions`. Mismo
  parámetro `?id=`, mismo regreso a Operadores con `?missing=1`, sin postbacks.
- **Se borran:** `LblAreaNote`, `PhOtherGuides`, `DetOtherGuides`, `RptOtherGuides`, `PhCurves`,
  `RptCurves` y `PhSessions`.
- **Nuevos** (en el `.designer.cs`):
  - Sin área / sin guía publicada: `PhAreaNote`, `LitAreaNote`, `PhAreaHint`, `LitAreaHintTitle`
    y `LitAreaHintText`;
  - otras áreas en los pasos: `PhOtherSteps` (el botón es HTML, `id="operatorOtherSteps"`);
  - ¿Qué ha hecho?: `LitSessions`, `PhLastSession`, `LitLastSession`, `PhMix` y `PhAlsoTrained`;
  - historial: `PhHistory` y `RptHistory` (con un repeater anidado de sesiones);
  - ficha: `PhPrintSessions`.
- `OperatorDetailRows.cs`: `OperatorGuideCard` suma `AgoText`, `GapText` y la línea de qué hacer;
  `OperatorReinforcementGroup` suma `Tag`, `TagCss` y `Collapsed`; `OperatorSessionListRow` suma
  día, hora, color del modo, ícono del estado, medidor, aprobó, mejor y la barrita; nuevo
  `OperatorHistoryBlock`; se borra `OperatorCurveCard`.
- `ReportText` suma `Day`, `Time`, `ScoreGap`, `Sessions`, `ModeCss` y `StatusIcon`.

### Comportamiento

- **Cliente, sin postback:** "Ver también otras áreas" muestra u oculta los grupos de otras áreas;
  cada bloque del historial se abre y cierra con `<details>`. La curva de un bloque plegado se
  dibuja la primera vez que se abre (Chart.js no mide un canvas oculto) y se redibuja al cambiar
  el tema, como las demás (`mtCharts`).
- **Curva:** pasa de barras de intentos fallidos con el puntaje al pasar el mouse a una gráfica
  mixta con dos ejes apilados (`stack` / `stackWeight` de Chart.js 4): puntaje arriba (0 a 100%)
  y fallidos abajo, con los rótulos visibles. El JSON de cada curva suma `scores` (puntaje en %),
  `passed` (`ScoreCalculator.IsPassed`) y `passing` (`ScoreCalculator.PassingScore`); las etiquetas
  pasan a `["#n", "dd/MM", modo]`.
- La BLL sigue validando todo; el estado lo decide `ReadinessPolicy` y quién está en cada área
  `OperatorArea`, sin cambios.

### Textos nuevos

- Preguntas: "¿Está listo en su área?" / "Estado de la guía de su área"; "¿Qué le falla?" / "Pasos
  de sus Assessment sin acierto o con 2 o más intentos fallidos, hasta 5 por guía"; "¿Qué ha
  hecho?" / "Todas sus sesiones, de todas las áreas".
- "Mejor puntaje", "Último puntaje", "Última actividad … · hace N días.", "N puntos menos que su
  mejor" / "Igual que su mejor", "Su área" / "Otra área", "Ver también otras áreas (N pasos)" /
  "Ocultar las otras áreas", "N sesiones", "Última: …".
- Historial: "Historial por guía", "Cada guía reúne su curva y sus sesiones. Su área primero; las
  otras áreas (entrenamiento cruzado) vienen plegadas.", "Guided (solo en la tabla)", "Estado",
  "Mejor", "Último", "Sesiones", "Puntaje", "Intentos fallidos", "Puntajes de Assessment: a → b",
  "Assessment", "aprobados", "intentos fallidos", "Todavía no tiene sesiones de Práctica libre ni
  Assessment: Guided no se grafica.", "Mejor", "Aprobó" / "Sin aprobar".
- La línea de qué hacer es la de Operadores (`StateHint`), **pendiente de que la valide negocio**
  (D-298); en el detalle se lee de una persona ("repasar con él…"): si negocio la cambia, cambia
  en las dos pantallas.

### Métricas derivadas (D-205), en la BLL y sin test, como D-235, D-283 y D-297

`BLL/Reports/OperatorActivity.cs` (nuevo, en el `.csproj`) va en `OperatorDetail.Activity` y solo
cuenta las sesiones que `GetOperator` ya cargó:

- **Por modo:** `Sessions`, `Guided`, `FreePractice` y `Assessment` (todas las áreas), y la última
  sesión con `DaysSinceLastSession`.
- **Por guía** (`GuideHistory`, en el orden de su área y las otras): `Sessions`, `Assessments` y
  `PassedAssessments` (`ReadinessPolicy.CountsAsAssessment` e `IsPassed`), `FailedAttempts`,
  `BestSessionId` (el mejor puntaje; el más viejo si empatan), `LastMinusBest` y
  `DaysSinceActivity`.
- `OperatorSessionRow` suma `Passed` (`ReadinessPolicy.IsPassed`).
- Los días, con el reloj inyectable `_utcNow` del servicio, igual que
  `OperatorSummary.MarkActivity`. **No es una regla nueva:** `ReadinessPolicy`, `OperatorArea`,
  `FollowUpRules` y el mínimo para aprobar no cambian.

El ancho del medidor y de la barrita, y la etiqueta "Su área", son presentación (code-behind).

### Ficha impresa

Siempre en claro, sin barra, botones ni curva (D-157). Salen las tres preguntas una debajo de otra
(con la línea de qué hacer y el reparto por modo), **todos** los pasos a reforzar (también los de
otras áreas, aunque en pantalla estén ocultos), "También entrenó: …" (D-270) y la **tabla única de
Sesiones** de todas las áreas con su columna Área, como antes. El historial por guía no se
imprime.

## Para revisar en Desarrollo (no compilado)

- Rebuild y `aspnet_compiler`, porque el `.designer.cs` se escribió a mano. En particular:
  - los `<%# %>` dentro de etiquetas HTML literales (`hidden` del grupo, `open` y `data-curve` del
    `<details>`, `style` del medidor y de la barrita);
  - los `<%= %>` / `<%: %>` dentro de `PhOtherSteps` y `PhMix`;
  - el repeater anidado de sesiones dentro de `RptHistory`.
- `DashboardServiceTests` y `DashboardPerformanceTests` tienen que seguir en verde: `GetOperator`
  arma las filas antes y suma `Activity`; las filas, el orden, los pasos y las curvas son los
  mismos.
- **Curva:** que el puntaje quede en el panel de arriba (Chart.js apila los ejes en el orden en
  que se declaran: `score` y después `fails`; si sale al revés, invertir el orden), que los
  rótulos no se corten arriba (`layout.padding.top` y `grace`) y que el tooltip diga el día y el
  modo. Probar una guía solo con Práctica libre (sin puntajes) y una solo con Guided (sin puntos).
- Casos: un operador con entrenamiento cruzado; uno Sin área (todos los bloques abiertos, todos
  los pasos a la vista, sin botón); uno de un área sin guía publicada; uno Sin iniciar en su área;
  uno sin sesiones (sin historial ni tabla impresa, con "Todavía no tiene sesiones…"); un
  Assessment abandonado o en curso (sin medidor, "—").
- Claro, oscuro y 768 px; contraste del verde de "Aprobó" y del rojo de "Sin aprobar" en las dos;
  sin scroll horizontal de la página.
- La impresión: las tres tarjetas apiladas, los grupos ocultos impresos, la tabla única y nada del
  historial.
- Que negocio valide la línea de qué hacer (D-298).
- Capturas (D-240, claro y oscuro): las toma Desarrollo de la versión final.
