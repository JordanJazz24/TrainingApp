# Plan 024: Dashboard por área (Operadores y Operador)

**Spec:** `024-dashboard-por-area.md` (aprobada 2026-10-28) · **Decisiones:** D-260 a D-271 ·
**Sesión:** `TrainingApp-Desarrollo` en la nube, de punta a punta (`docs/process.md` → "Variante
temporal" → "Spec 024"), rama `cloud/024-dashboard-por-area`.

## Cómo se trabaja este plan

- **En la nube:** cada entrega se implementa en `cloud/024-dashboard-por-area`, con un commit
  por entrega marcado **"no verificada"** (la VM no compila ni prueba), su `/code-review` con
  nivel y foco (`process.md` → 3) y push solo a esa rama.
- **En local, después:** Arquitectura y Planeación revisa la rama contra la spec y este plan.
  Desarrollo la trae entrega por entrega con su ciclo (`process.md` → 2): Rebuild,
  `aspnet_compiler` si se tocó markup, tests, checklist y el "funciona" del usuario antes del
  commit en `TrainingApp`. Cada entrega dice qué verifica Desarrollo.
- Sin ORM ni SQL inline; la regla en la BLL; el code-behind solo UI. `ReadinessPolicy` no cambia.
  `Admin.Master` no se toca. Un estilo nuevo en `admin.css` va en un bloque propio
  "Operadores / Operador".
- No se tocan `docs/roadmap.md` ni `docs/status.md`. Los docs de cierre (`domain.md`,
  `architecture.md`) van en la E4.
- Si algo no cierra: parar y proponer la enmienda (`process.md` → 1). Decisiones nuevas desde
  D-272.

> **Compilación en la nube (2026-10-28):** con Mono y Roslyn (`tools/build-check.py --tests`, hook
> de inicio de la nube), la rama compila (TrainingApp y TrainingApp.Test) y pasan los 461 tests (4
> `Explicit` omitidos). Se corrigió un test de la E1 que fallaba (`Row_HasStateInHisArea` comprobaba
> un texto que el operador falso no tiene). El markup (`aspnet_compiler`), la BD y el navegador
> siguen sin verificar: los verifica Desarrollo.

## E1 · Datos y BLL, con tests — hecha en la nube, no verificada

- [x] `10_spDashboard.sql`: `spDashboardGuides` devuelve también `g.AreaId`; `spDashboardOperators`,
      `u.AreaId`. Sin parámetros nuevos (P-3).
- [x] Domain: `DashboardGuide.AreaId` (`int`) y `DashboardOperator.AreaId` (`int?`).
      `DashboardRepository` las mapea.
- [x] `BLL/Reports/OperatorArea.cs` (regla pura, P-10): con las guías publicadas, el `AreaId`
      del operador y sus sesiones, separa **sus guías** (las publicadas de su área, también Sin
      iniciar) y **otras** (las de otras áreas con sesiones), y da su estado en el área:
      - `NoArea` (sin área), `NoPublishedGuide` (su área no tiene guía publicada, P-5) o el
        estado de sus guías (`GuideReadiness` de `ReadinessPolicy`, una por guía, P-6);
      - **Sin iniciar** = tiene área, su área tiene guía y no hay sesiones en ellas (D-263);
      - `Matches(state)`: Estado = X si alguna guía de su área está en X; `NoArea` y
        `NoPublishedGuide` solo salen con Estado = Todos (D-267, P-5).
- [x] `DashboardService`:
  - `OperatorFilter`: se quita `GuideId` y se agrega el filtro de área (Todas, un `AreaId` o Sin
    área). Un área que no tiene guía publicada vale Todas (P-4).
  - `OperatorRow`: `AreaGuides` (las de su área, en lugar de `Guides`), `AreaStatus` y
    `LastActivity` (de todas sus sesiones). `NotStarted` usa la regla nueva.
  - `OperatorPage`: se quita `Guides` y se agregan las áreas del filtro (las que tienen guía
    publicada, agrupadas por producto, D-262).
  - `FindOperators` filtra por área y estado con `OperatorArea` antes de paginar (P-3).
  - `GetOperator`: `OperatorDetail` con `AreaGuides` (y su `Readiness`) y `OtherGuides` (y la
    suya); Reforzar y curvas, su área primero (D-269); cada sesión con el texto de su área.
- [x] Tests NUnit en memoria, **solo lo nuevo** (P-11): filtro Área (Todas, un área, Sin área,
      inválido o sin guía publicada = Todas), Sin iniciar nueva (área sin sesiones; cruzado sin su
      área = Sin iniciar; sin área y sin guía publicada fuera de Sin iniciar), Aprobado solo en otra
      área no sale como Aprobado. Los tests existentes de `DashboardServiceTests` que usaban
      `GuideId` o `Guides` se ajustan, no se borran.
- [x] `/code-review medium` sobre `OperatorArea.cs` y `DashboardService.cs`. Foco: la regla de
      Sin iniciar, el filtro antes de paginar, `CanViewReports` en cada método, que los números de
      Resumen, Pasos y Seguimiento no cambien (AC-10), y rendimiento (lee todo el historial,
      `process.md` → 3).

**Review (nube):** `/code-review medium`: aplicados el filtro de sesiones por las guías leídas,
la comparación por `AreaId` en `OperatorArea` y una sola búsqueda del área por sesión. Descartados
los tests del detalle y del Excel (P-11: se prueban en el navegador) y unir estado y guías en un
solo parámetro (estilo). `ExcelReportsTests` se ajusta en la E2, con `ExcelReports.cs`.

**Verifica Desarrollo:** Rebuild; scripts `10` sobre su BD; tests (todo en verde, también
`ReadinessPolicyTests`, `SummaryRulesTests` y `ScoreCalculatorTests`). Sin cambios visibles
todavía: las páginas se ajustan a las firmas nuevas en la E2 y la E3, así que la E1 compila
junto con ellas (si Desarrollo la trae sola, ajusta lo mínimo en `Operators.aspx.cs`,
`Operator.aspx.cs` y `ExcelReports.cs` para compilar).

## E2 · Operadores y su Excel — hecha en la nube, no verificada

- [x] `ReportFilters.Operators`: lee `?area=<Id>` o `?area=none`; ignora `?guide=` (P-4).
      `ExportUrl` sigue igual.
- [x] `Operators.aspx`: se quitan las columnas por guía y el filtro Guía (D-261, D-264). Filtros:
      Buscar, **Área** (con `<optgroup>` por producto como "Elige tu área" de la 021, D-262;
      "Todas" y "Sin área" fuera de los grupos) y Estado. Columnas: Operador, Área, **Estado en su
      área** (una insignia por guía de su área con "Mejor · Último", o "Sin área" / "Sin guía
      publicada" atenuados, P-5) y Última actividad. "Ningún operador coincide" y la paginación,
      como hoy.
- [x] `Operators.aspx.cs`, `OperatorListRow.cs`, `ReportText.cs`: solo binding y textos.
- [x] `ExcelReports.Operators` (P-7): columnas Código, Nombre, Área, Estado en su área, Mejor,
      Último y Última actividad; filtros del título: Búsqueda, Área y Estado. Con varias guías en
      un área (futuro), una fila por operador con los estados unidos por "; ".
- [x] `/code-review low --max-findings 5` sobre `ReportFilters.cs` y `Operators.aspx.cs`. Foco:
      `area` inválido, el `<optgroup>` sin EventValidation (la BLL valida), codificación (`<%#: %>`
      en HTML; `<%# %>` en atributos de controles de servidor).

**Review (nube):** `/code-review low`, sin hallazgos. Estilo nuevo en `admin.css`, en el bloque
"Operadores / Operador por área (spec 024)". `ExcelReportsTests` ajustado a las columnas nuevas.

**Verifica Desarrollo:** Rebuild; `aspnet_compiler`; tests (`ExcelReportsTests` se ajusta a las
columnas nuevas). Checklist: AC-1 a AC-6 en claro y oscuro (Área = una, Sin área, Estado = Sin
iniciar con un operador de entrenamiento cruzado, un `?area=abc` y un `?guide=` viejo, F5 y
Atrás, el Excel abierto en Excel).

## E3 · Operador y su ficha — hecha en la nube, no verificada

- [x] `Operator.aspx`: "Estado por guía" pasa a **"Su área"** (sus guías, también Sin iniciar;
      "Sin área" o "Sin guía publicada" si aplica) y **"Otras áreas (entrenamiento cruzado)"** en
      un `<details>` nativo, cerrado, solo si hay; abierto si el operador no tiene área (D-269,
      P-8, P-9). Reforzar y curvas, en el orden de la BLL. Sesiones con columna **Área**.
- [x] Ficha impresa: "Otras áreas" no se imprime (`d-print-none`) y va la línea "También
      entrenó: Producto · Área — Estado; …" (D-270, P-9); nada si no hay.
- [x] `Operator.aspx.cs`, `OperatorDetailRows.cs`: solo binding y textos.
- [x] `admin.css`: solo si hace falta (el `<details>`), en el bloque "Operadores / Operador".
- [x] Sin `/code-review` si es solo binding y markup (`process.md` → 3); `low` si se toca lógica.

**Ajuste (nube):** en Sesiones, la columna "Guía" pasa a ser **"Área"** ("Producto · Área", con
el título de la guía al pasar el mouse) en vez de agregar otra: hoy la guía es la de un área
(D-268) y dos columnas dirían lo mismo. El desplegable es un `<details runat="server">`
(`HtmlGenericControl`), abierto desde el servidor si no tiene área. Sin `/code-review` (binding y
markup); entra en el de la E4.

**Verifica Desarrollo:** Rebuild; `aspnet_compiler`. Checklist: AC-7 a AC-9 (un operador con
entrenamiento cruzado, uno sin él y uno sin área; la ficha impresa en claro) y AC-11 (un
operador que abre Operadores u Operador vuelve a su inicio).

## E4 · Cierre — hecha en la nube, no verificada

- [x] `docs/domain.md` → "Dashboard de Training": la nueva Sin iniciar (D-263), Operadores por
      área y el detalle (su área / otras). → "Área del operador": el dashboard ya filtra por área
      (deja de decir "solo la muestra", D-175 enmendada por D-261 a D-270).
- [x] `docs/architecture.md` → "Dashboard de Training": `OperatorArea` y el filtro en la BLL.
- [x] `docs/decisions.md`: D-175 pasa a "enmendada (spec 024)".
- [x] Spec: "Status" a Implemented (no verificada) con los commits; casillas de este plan.
- [x] `/code-review low` de todo el diff de la rama, foco en consistencia con la spec.
- [x] Aviso al usuario (para Planeación): rama lista para la revisión local; Diseño UI/UX enmienda
      los briefs 6 y 7 (spec → "Qué cambia para la 022").

**Review (nube):** `/code-review low` de toda la rama, sin hallazgos (la compilación queda para el
Rebuild de Desarrollo).

**Verifica Desarrollo:** al traer la rama, que los docs coinciden con lo implementado; después
escribe `docs/status.md` y `docs/history.md`, y Planeación el roadmap.
