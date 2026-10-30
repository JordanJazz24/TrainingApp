# Operadores · alternativa D con el encabezado K1 (aprobada 2026-10-30)

**Spec:** `../022-mockups-por-pantalla.md` · **Brief:** 6 de `../022-briefs.md` (enmendado con la
spec 024) · **Rama de partida:** `cloud/022-operadores` (desde `TrainingApp`, "no verificada").

Lista de cambios frente al v1 (el código de la 024), tal como se aprobó en el canvas
"Operadores · spec 022" (sesión de diseño en la nube, D-284). Hubo cuatro rondas:

1. La actual recreada y A (tabla con el pulso), B (por estado, con qué hacer) y C (mapa por área
   y fichas). A B "a medias".
2. D (pestañas por estado), E (filtros con conteos) y F (tablero de progreso). Elegida **D**.
3. Tres encabezados para D: título con el área, banda con pestañas y contador del grupo.
   Descartados.
4. Dos encabezados más: **K1 (barra proporcional)**, elegido, y K2 (mapa del recorrido). El
   usuario pidió además meter **todo el encabezado en una tarjeta**, como el de Seguimiento.

## Alternativa elegida

**D · Pestañas por estado, con el encabezado K1 "barra proporcional" en una tarjeta.**

- **Encabezado (una tarjeta, `--mt-radius-xl` y sombra):**
  - **Arriba:** a la izquierda, el antetítulo "Estado en la guía de su área · <área>" (Todas las
    áreas, Producto · Área o Sin área) y el título "Operadores"; a la derecha, Buscar, Área,
    Filtrar y Descargar.
  - **En medio:** "Elige un tramo para ver ese grupo · el ancho es cuántos hay", el enlace "Ver
    todos N" y la **barra por estado**: un tramo por estado (Reprobado, Sin iniciar, En
    entrenamiento, Aprobado) y otro para Sin área. Cada tramo lleva su número grande, su ícono,
    su nombre y "N% de la vista".
  - **Al pie:** la línea de **qué hacer** con el grupo elegido (en `--mt-surface-2`) y, a la
    derecha, "N sin actividad en 14 días o más".
- **Cómo funciona la barra:**
  - Cada tramo es un enlace con los filtros en la URL, como antes: `?state=`, y para Sin área
    `?area=none`.
  - El **ancho** de cada tramo es cuántos hay, con un mínimo del 7% del total para que se lea. Un
    estado sin nadie no sale, salvo que esté elegido.
  - El tramo elegido sube, lleva un borde en su color y `aria-current`.
- **Tabla:** la del v1, con dos cambios:
  - bajo la fecha de "Última actividad", "hace N días", en ámbar y con reloj desde 14 días;
  - una columna nueva con el botón "Ver detalle" (enlace al detalle del Operador, que es la
    siguiente pantalla).

  El orden (por nombre), la paginación de 50, el vacío y "Quitar filtros" no cambian.

### Resumen de la crítica (D-284)

- **D** quedó 1.ª en la segunda ronda: se queda con lo que gustó de B (el estado manda y dice qué
  hacer) sin su riesgo, porque no cambia el orden ni parte la lista.
  - **Riesgos:** con "Todos" la línea de qué hacer es genérica; los textos de "qué hacer" son guía
    nueva que negocio debe validar.
- **K1** quedó 1.º en la cuarta ronda: muestra el reparto y sirve para navegar en una sola pieza,
  y se lee sin texto (tamaño y color, con número, ícono y nombre).
  - **Riesgos:** un estado con 0 no aparece; los tramos chicos se aprietan (por eso el mínimo); en
    tablet (767 px o menos) la barra pasa a dos tramos por fila y se pierde la proporción.

## Cambios frente al v1

### Tokens

Ninguno nuevo ni con otro valor, ni en claro ni en oscuro. La barra usa los `*-soft` de cada
estado y `--mt-field-border` (borde punteado de Sin área).

### Componentes (`admin.css`, bloque "Operadores: encabezado con la barra por estado")

- **Nuevos, solo de esta pantalla:**
  - encabezado: `.operators-head`, `-head-top` y `-eyebrow`;
  - barra: `.operators-meter-k`, `.operators-all` (con `is-selected`), `.operators-meter` y
    `.operators-seg`, con sus modificadores `-seg-bad`, `-seg-idle`, `-seg-warn`, `-seg-ok`,
    `-seg-none` e `is-selected`, y sus partes `-seg-n`, `-seg-l` y `-seg-p`;
  - pie: `.operators-callout`, `.operators-tone-*` e `.operators-inactive-count`;
  - tabla: `.operators-ago` (con `is-inactive`) y `.operators-go`.
- **Modificado:** `.operators-table td:last-child { min-width: 0; }`, para la columna de "Ver
  detalle".
- `.admin-page-head` ya no se usa en esta página.

### Estructura y controles (`Operators.aspx`)

- **Sin cambio:** `PnlFilters` (ahora dentro de la tarjeta), `TxtSearch`, el `<select>` nativo
  `operatorsArea` con `RptAreaGroups`, `BtnFilter`, `LnkExport`, `RptOperators`, `PhTable`,
  `PnlEmpty`, `LnkClear`, `PhPager`, `LnkPrev`, `LitPageInfo` y `LnkNext`.
- **Se borra:** `DdlState`. El Estado se elige en la barra; cuando se pulsa Filtrar, el estado
  aplicado viaja en el nuevo `HfState`.
- **Nuevos** (en el `.designer.cs`):
  - antetítulo: `LitAreaLabel`;
  - estado aplicado: `HfState` (`HiddenField`);
  - "Ver todos": `LnkAll` y `LitAllCount`;
  - barra: `RptMeter`, con `OperatorMeterSegment`;
  - línea de qué hacer: `IcoHint` (`<i runat="server">`), `LitHintTitle` y `LitHintText`;
  - aviso de inactividad: `PhInactive` y `LitInactive`.
- `OperatorListRow` suma `AgoText`, `Inactive` y `HasAgo`. Clases nuevas en el mismo archivo:
  `OperatorMeterSegment` y `OperatorHint`.
- `ReportText` suma `DaysAgo(int?)` y `StateHint(ReadinessState?, bool)`, con los textos de qué
  hacer.

### Comportamiento

- **Filtros:** los mismos en la URL (`q`, `state`, `area`, `page`).
  - Elegir un estado conserva el área, salvo Sin área: un operador sin área no tiene estado
    (D-267), así que vuelve a Todas.
  - El tramo Sin área fija `area=none` y quita el estado.
  - "Ver todos" quita el estado y conserva el área.
- **Filtrar:** usa el estado aplicado (`HfState`); si el Área elegida es Sin área, lo quita.
- La BLL sigue validando todo (P-4); el Excel no cambia.
- No hay postbacks nuevos: la barra es de enlaces.

### Textos nuevos

- Antetítulo: "Estado en la guía de su área · …"; se quita la bajada "El estado de cada operador
  en la guía de su área.".
- Barra: "Elige un tramo para ver ese grupo · el ancho es cuántos hay", "Ver todos" y "N% de la
  vista".
- Qué hacer:
  - Reprobado: "repasar con él los pasos que falla y volver a evaluar.";
  - Sin iniciar: "tiene área y ninguna sesión en su guía; arrancar con Guided.";
  - En entrenamiento: "practica sin aprobar todavía; evaluar cuando esté listo.";
  - Aprobado: "ya aprobó la guía de su área.";
  - Sin área: "todavía no elige su área; no cuenta como Sin iniciar.";
  - Todos: "Elige un estado para ver qué hacer con ese grupo.".
- Otros: "N sin actividad en 14 días o más", "hoy / hace 1 día / hace N días" y "Ver detalle".

### Métricas derivadas (D-205), en la BLL y sin test, como D-235 y D-283

`BLL/Reports/OperatorSummary.cs` (nuevo, en el `.csproj`) va en `OperatorPage.Summary` y solo
cuenta a los operadores que `FindOperators` ya cargó:

- **Por estado:** `Passed`, `Failed`, `InTraining` y `NotStarted`, con la búsqueda y el área y sin
  el filtro de estado; con Sin área, en todas las áreas. Usa `OperatorArea.Matches`, la misma
  regla del filtro.
- **Totales:** `All` (los mismos operadores, también Sin área y sin guía publicada) y
  `WithoutArea` (con la búsqueda).
- **Inactividad:** `Inactive` cuenta en la lista filtrada, todas las páginas, a quienes tienen guía
  en su área, no son Sin iniciar y llevan 14 días o más desde `LastActivity`. Usa
  `InactiveDays = FollowUpRules.InactiveDays`.
- **Por fila:** `OperatorRow` suma `DaysSinceActivity` e `Inactive`, que pone
  `OperatorSummary.MarkActivity` (con el reloj inyectable `_utcNow` del servicio).

`FindOperators` ahora arma primero a **todos los que coinciden con la búsqueda** y después aplica
el área y el estado. Las filas, el orden, el total y la paginación son los mismos que antes.

El % de cada tramo y su ancho son presentación: van en el code-behind, como el ancho de las barras
de Seguimiento.

## Para revisar en Desarrollo (no compilado)

- Rebuild y `aspnet_compiler`, porque el `.designer.cs` se escribió a mano. En particular:
  - `IcoHint` es `HtmlGenericControl`;
  - `RptMeter` pone `aria-current` y `style` con `<%# %>` / `<%#: %>` dentro de un `<a>` literal;
  - en la tabla hay un `PlaceHolder` anidado para el ícono de inactividad.
- `DashboardServiceTests` y `DashboardPerformanceTests` tienen que seguir en verde: cambió el
  armado de `FindOperators`; se mantienen las filas, el orden, el total y la paginación.
- Casos que revisar:
  - Todas, un área, Sin área;
  - cada estado;
  - Sin área + Filtrar con un estado aplicado (lo quita);
  - búsqueda sin resultados (vacío y "Quitar filtros");
  - un estado con 0 (no sale su tramo);
  - área sin guía publicada;
  - página 2 y 3 (el resumen cuenta todas las páginas).
- Claro, oscuro y 768 px: la barra pasa a dos tramos por fila y no hay scroll horizontal de la
  página.
- Contraste de los números de cada tramo en su `*-soft`, y del texto ámbar sobre `--mt-surface-2`.
- Que los textos de "qué hacer" los valide negocio.
- Capturas (D-240, claro y oscuro): las toma Desarrollo de la versión final.
