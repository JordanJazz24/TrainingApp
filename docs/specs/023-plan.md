# Plan 023: Filtro de período en Pasos y Resumen

**Spec:** `023-filtro-periodo.md` (aprobada 2026-10-28) · **Decisiones:** D-209 a D-218 ·
**Planeado en:** Arquitectura y Planeación local · **Implementa:** Desarrollo, en local.
**Status:** por aprobar (escrito el 2026-10-28) · **Enmienda:** el punto 8 de la spec 018
(período "Últimos 30 días / Todo").

Cada entrega sigue `docs/process.md` → "2. Cada entrega": implementar, verificar con los
comandos de `CLAUDE.md` → "Correr local, compilar y probar", `/code-review` con nivel y foco, el
"funciona" del usuario en el navegador **antes** del commit y commit solo con sus rutas
(`git commit -- <rutas>`), con `docs/status.md` y la casilla de este plan en el mismo commit.

## Reglas de este plan

- Rama y worktree según `docs/process.md` → 7; se integra a `TrainingApp` con la aprobación del
  usuario.
- Sin ORM ni SQL inline; la lógica (rangos, comparación, granularidad, validación) en la BLL; el
  code-behind solo UI (visibilidad, binding y redirecciones).
- **Solo Pasos y Resumen** (D-209). Operadores, Operador, Seguimiento y el estado "listo" no
  cambian (AC-9): el estado sigue con todo el historial (DA-1).
- Sin tablas ni índices nuevos (P-8). Si la medición de la E4 no cumple P-13, se para y se avisa
  a Planeación antes de tocar SPs o índices.
- **Coordinación con la 022:** el mockup de Pasos (022, E4) espera esta spec (D-217). Esta spec
  usa el sistema de diseño actual (`admin.css`); si la E1 de la 022 está en curso, no se tocan
  `admin.css` a la vez (la segunda espera el commit de la primera).
- Si algo de la spec no cierra al implementar: parar y pedir la enmienda (process.md → 1).
- Ninguna decisión nueva en este plan. Si aparece una, la siguiente libre del rango local es
  **D-219**; si hace falta más, se pide un rango (`docs/decisions.md` → encabezado).

## E1 · Datos y BLL del rango y la comparación, con tests

- [ ] `10_spDashboard.sql`: `spDashboardSessions` y `spDashboardStepAttempts` suman
      `@To DATETIME2 = NULL` (exclusivo, UTC; `NULL` = sin límite):
      `AND (@To IS NULL OR s.StartedAt < @To)`. Sin cambiar columnas ni el resto de los filtros
      (P-8).
- [ ] DAL: `DashboardRepository.GetSessions(…, fromUtc, toUtc, guideId)` y
      `GetStepAttempts(mode, fromUtc, toUtc, guideId)` pasan `@To`; las llamadas actuales
      siguen igual con `toUtc = null`.
- [ ] `BLL/Reports/ReportPeriod.cs`: el enum pasa a **Today, ThisWeek, ThisMonth, PrevMonth,
      Last30Days, All, Custom** (P-3), con **ThisMonth** como el de inicio (D-215).
- [ ] `ReportRange` (BLL, inmutable): período, `FromUtc`/`ToUtc` (`null` con Todo), el anterior
      (`PrevFromUtc`/`PrevToUtc`, `null` con Todo), las fechas locales de los dos (para los
      textos) y el largo en días. `ReportPeriods.Resolve(period, DateTime? from, DateTime? to,
      DateTime nowUtc)`, **pura**:
  - corta a medianoche de la planta con `PlantTime` y convierte a UTC; el día de Fin entra
    completo (`ToUtc` = medianoche del día siguiente) (P-2);
  - el anterior de cada atajo según P-4: Hoy → ayer; Esta semana (lunes a hoy) → los mismos
    días de la semana anterior; Este mes → los mismos días del mes anterior, hasta su último
    día si es más corto; Mes anterior → el mes previo; Últimos 30 días (hoy y los 29 previos) →
    los 30 días previos; Personalizado → los mismos días justo antes; Todo → sin anterior;
  - Personalizado inválido (falta una fecha, Fin < Inicio, Fin > hoy de la planta): devuelve
    **Este mes** y marca `Invalid` para el aviso (P-6).
  - `PlantTime` suma lo que haga falta (`FromLocal` o el "hoy" de la planta) sin cambiar
    `ToLocal`.
- [ ] `ReportChange` (BLL): valor actual, anterior, cambio (puntos o %), dirección, si es
      mejora (subir en aprobación y sesiones; bajar en % con fallo) y la base de los dos
      períodos (evaluados o sesiones; P-5, P-16). Sin datos en el anterior: "sin datos para
      comparar".
- [ ] `DashboardService`:
  - `SummaryFilter`/`StepFilter` llevan `Period`, `From` y `To`; `ValidPeriod` se reemplaza
    por `ReportPeriods.Resolve` y el filtro aplicado devuelve el rango y el aviso;
  - **Resumen:** la tasa de aprobación por guía y las sesiones del período se calculan para
    el rango y el anterior con las sesiones que ya carga (en memoria); el Top 5, con **una sola
    lectura** de `GetStepAttempts` desde `PrevFromUtc` hasta `ToUtc`, que la BLL separa por
    período (P-8). "Sesiones del período": todos los modos, de la guía filtrada (P-11);
  - **Pasos:** el ranking del período y el % con fallo del anterior por paso, con la misma
    lectura única; la agrupación de "Todas" (enmienda 1 de la 018, D-147) igual en los dos;
  - **Gráfica:** `SummaryRules` agrupa por día, semana (lunes a domingo, `WeekStart`) o mes
    según el largo: día hasta 31 días, semana hasta 366, mes si es más largo; con Todo, el
    largo real desde la primera sesión (P-9, D-214);
  - **descarga de sesiones:** filtra por `FromUtc` y `ToUtc`.
- [ ] Tests NUnit en memoria (AC-11):
  - `ReportPeriodsTests` (nuevo): cada atajo y su anterior con un "ahora" fijo; bordes de mes
    (31 de oct. → 1–30 sep.; 31 de mar. → 1–28/29 feb.; enero → diciembre del año anterior) y de
    semana (lunes y domingo); medianoche de la planta (una sesión a las 23:30 locales cae en
    ese día aunque en UTC sea el siguiente); Personalizado válido e inválido (cada caso de P-6);
    Todo sin anterior;
  - `SummaryRulesTests`: la granularidad en 1, 7, 31, 90 y 400 días (AC-6);
  - `DashboardServiceTests`: el cambio de la tasa de aprobación, sesiones, Top 5 y Pasos;
    "sin datos para comparar"; la base de los dos períodos; con Todo, sin comparación; un
    período inválido cae en Este mes con aviso;
  - los existentes en verde (`ScoreCalculatorTests` incluido); los que asumían 30 días al
    entrar se ajustan a Este mes.
- [ ] Verificación (Desarrollo): Rebuild de `TrainingApp` y `TrainingApp.Test`; `10` en una BD
      de LocalDB (dos corridas); `vstest.console` en verde. Las páginas compilan con lo mínimo
      (el período de la URL todavía de la forma vieja si hace falta) hasta la E2.
- [ ] `/code-review medium` sobre `10_spDashboard.sql`, el DAL y `BLL/Reports`. Foco: la
      conversión local ↔ UTC y el `@To` exclusivo (que ninguna sesión caiga en dos períodos ni
      en ninguno), la validación en la BLL sin confiar en la página, y que Operadores,
      Operador y Seguimiento no cambien.

**Hecho cuando:** los tests pasan y el SP corre dos veces sin error. Sin cambios visibles
todavía, salvo que el período de inicio ya es Este mes.

## E2 · Resumen: control, comparación, sesiones y gráfica

- [ ] `UI/Controls/Admin/PeriodFilter.ascx` (nuevo, P-12): la lista **Período** con las 7
      opciones (Hoy · Esta semana · Este mes · Mes anterior · Últimos 30 días · Todo ·
      Personalizado…, D-210 enmendada); al elegir Personalizado… aparecen **Inicio** y **Fin**
      (`<input type="date">`, `max` = hoy de la planta; D-216) sin postback; debajo, el rango
      aplicado en palabras ("Del 1 al 28 de oct. de 2026") y la referencia del anterior (P-14).
      Expone el período elegido y las fechas; no calcula rangos (eso es la BLL). Se declara
      estático en las dos páginas (sin `LoadControl`).
- [ ] `ReportFilters`: lee y escribe `?period=today|week|month|prevmonth|30d|all|custom&from=AAAA-MM-DD&to=AAAA-MM-DD`;
      sin parámetro = Este mes; `period=all` sigue funcionando (P-7). Un valor desconocido
      vale Este mes; la BLL vuelve a validar.
- [ ] `Summary.aspx`: el control en la barra de filtros (con Guía y Filtrar); el aviso de
      período inválido (P-6); el cambio en la tasa de aprobación por guía (en puntos), en el
      nuevo **"Sesiones del período"** del encabezado de la tarjeta de actividad (en %; P-11) y
      en el % con fallo de cada paso del Top 5 (en puntos), cada uno con flecha ▲/▼, valor,
      referencia ("vs. 1–28 sep.") y la base ("1 vs. 2 evaluados"), nunca solo color
      (P-5, P-16, D-156); con Todo, sin comparación.
- [ ] La gráfica de actividad con la granularidad de la BLL; el título dice "por día", "por
      semana" o "por mes" (AC-6). `ChartJson` sigue saliendo de `JavaScriptSerializer`.
- [ ] El enlace del Top 5 a Pasos y la descarga de sesiones llevan el período, también
      Personalizado (D-147, AC-7). La nota "con Todo la descarga puede tardar" (D-148) sigue.
- [ ] `ReportText.Period` y los textos de los bloques ("Tasas y puntaje: …") dicen el nombre del
      atajo o el rango (P-14).
- [ ] Estilos mínimos del cambio (flecha, mejora/empeora, base) en `admin.css` con los tokens
      `--mt-*`, en claro y oscuro (coordinar con la E1 de la 022).
- [ ] Verificación (Desarrollo): Rebuild; `aspnet_compiler` (markup tocado); tests.
- [ ] `/code-review low --max-findings 5` sobre `PeriodFilter`, `ReportFilters` y
      `Summary.aspx.cs`. Foco: nada de cálculo en la página o el control, el texto al HTML
      codificado (`<%#: %>`), la URL leída en un solo lugar.
- [ ] Checklist del navegador: AC-1 a AC-7 en el Resumen (cada atajo; Personalizado válido e
      inválido, también editando la URL; Todo sin comparación; un anterior vacío; el enlace del
      Top 5), en claro y oscuro y en tablet; el Excel de sesiones abre en Excel con el rango en
      el título y solo las filas del rango.

**Hecho cuando:** el usuario prueba el Resumen con varios períodos y dice "funciona".

## E3 · Pasos y su Excel

- [ ] `Steps.aspx`: el mismo `PeriodFilter` en lugar de `DdlPeriod` (con Guía, Modo, Filtrar y
      Descargar); el aviso de período inválido; por paso, el cambio del % de sesiones con fallo
      (en puntos) con flecha, valor, referencia y la base (sesiones) (P-5, P-16); con Todo, sin
      comparación. Con la guía en "Todas", la agrupación por tipo de paso (D-147) en los dos
      períodos.
- [ ] Abrir Pasos desde el Top 5 del Resumen: mismo período, misma guía, mismos números
      (AC-7).
- [ ] `ExcelReports.Steps`: el título con el rango ("Pasos · Assessment · Del 1 al 27 de oct. de
      2026") y dos columnas por paso, **"% con fallo (anterior)"** y **"Cambio (pts)"**, vacías
      con Todo (P-10, D-213); el formato de la Fase 11. `ExcelReports.Sessions`: solo el rango en
      el título (sin comparación).
- [ ] Tests: `ExcelReportsTests` con las columnas nuevas, vacías con Todo, y el título con el
      rango.
- [ ] Verificación (Desarrollo): Rebuild; `aspnet_compiler`; tests.
- [ ] `/code-review low --max-findings 5` sobre `Steps.aspx.cs` y `ExcelReports`. Foco: que la
      pantalla y el Excel traigan lo mismo con los mismos filtros (DA-5), columnas vacías con
      Todo.
- [ ] Checklist del navegador: AC-2 a AC-5, AC-7 y AC-8 en Pasos (cada atajo, Personalizado,
      Assessment y Práctica libre, "Todas" y una guía), en claro y oscuro; el Excel abierto en
      Excel.

**Hecho cuando:** el usuario prueba Pasos y su Excel y dice "funciona".

## E4 · Rendimiento y cierre

- [ ] Medir con la BD de volumen (`Database/Ops/GenerateDashboardData.sql` en
      `TrainingApp_DashboardTest`, como en la Fase 11, AC-7) y `DashboardPerformanceTests`:
      Resumen y Pasos en menos de 3 s con Este mes, con Todo y con un Personalizado de 12 meses
      (que lee 24 con el anterior); el Excel de Pasos en menos de 20 s con Todo (P-13, AC-10).
      Cifras en este plan. Si algo no cumple: parar y avisar a Planeación antes de tocar SPs o
      índices.
- [ ] AC-9: Operadores, Operador (detalle, curva, ficha) y Seguimiento sin cambios.
- [ ] Docs de cierre (Desarrollo): `docs/architecture.md` si describe el período del dashboard;
      `docs/status.md`, `docs/history.md`; la spec 023 a Implemented. Avisar a Arquitectura y
      Planeación (roadmap) y, por el usuario, a la nube: el mockup de Pasos de la 022 ya puede
      empezar (D-217).
- [ ] Borrar la BD de volumen al terminar.

**Hecho cuando:** las cifras cumplen P-13, el usuario confirma y la spec pasa a Implemented.
