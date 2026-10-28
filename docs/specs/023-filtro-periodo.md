# Spec 023: Filtro de período en Pasos y Resumen

**Status:** Approved (2026-10-28, usuario: "apruebo") · plan `023-plan.md` (4 entregas, aprobado el 2026-10-28)
**Date:** 2026-10-27 · **Origen:** usuario, 2026-10-27 ("en Pasos y Resumen agregar filtro de
fecha inicio y fin, o proponer un diseño que permita filtrar con mejor UX") · **Sesión:**
Arquitectura y Planeación local (decisión del usuario)
**Decisiones:** D-209 a D-217, del usuario (tres rondas, más una cuarta el 2026-10-28 que
redujo los atajos de D-210); las propuestas de Claude P-1 a P-16, abajo, todas confirmadas por
el usuario (2026-10-27 y 2026-10-28), van juntas como D-218. **Enmienda** el punto 8 de la spec 018 ("período, con los
últimos 30 días al entrar y la opción Todo").

## Problem

Pasos y Resumen solo ofrecen "Últimos 30 días" o "Todo" (spec 018, punto 8). Quienes los miran
(gerencia y trainers) necesitan:
- **revisar un grupo o una capacitación** en fechas concretas;
- **reportes periódicos** (esta semana, este mes, el mes anterior);
- **comparar antes y después** de un cambio (una guía o una WI nueva);
- **seguimiento diario** (qué pasó hoy).

Con dos opciones fijas no se puede ninguna de las cuatro, y para comparar hay que hacer cuentas
a mano.

## Scope

### In

**A. El control de período** (D-210, D-215, D-216)
1. En Pasos y Resumen, el selector "Período" pasa a tener **siete opciones**: cinco atajos,
   Todo y **Personalizado…** (lista exacta en P-3). Cerrado ocupa lo mismo que hoy. Al elegir Personalizado aparecen **Inicio** y **Fin** con el
   selector de fecha nativo del navegador (`<input type="date">`) y el botón Aplicar.
2. Debajo del filtro, el rango aplicado en palabras: "Del 1 al 27 de oct. de 2026".
3. Al entrar, **Este mes** (del día 1 a hoy), en las dos vistas.

**B. Comparación con el período anterior** (D-211, D-212)
4. Cada número elegido muestra su cambio contra el **período anterior de igual largo**; con un
   período en curso, contra **los mismos días** del anterior (este mes al 27 vs. del 1 al 27 del
   mes pasado). Reglas por atajo en P-4.
5. Números con cambio:
   - Resumen: la **tasa de aprobación** por guía (en puntos), las **sesiones** del período
     (en %) y el **% con fallo** de cada paso del Top 5 (en puntos);
   - Pasos: el **% de sesiones con fallo** de cada paso (en puntos).
6. Con "Todo" no hay comparación.
6b. Junto al cambio va la **base de los dos períodos** (evaluados o sesiones), para que un
   cambio grande con pocos datos se lea como tal (P-16).

**C. La gráfica de actividad del Resumen** (D-214)
7. Granularidad automática según el largo del período: por **día**, por **semana** (lunes a
   domingo) o por **mes** (umbrales en P-9). El título dice cuál usa.

**D. Excel** (D-213)
8. El Excel de **Pasos** suma, por paso, el % con fallo del período anterior y el cambio en
   puntos. Los dos Excel (Pasos y sesiones del Resumen) dicen el rango en su título.

**E. Orden con la spec 022** (D-217)
9. La 023 se implementa **antes** del mockup de Pasos de la 022, con el sistema de diseño
   actual (`admin.css`). El mockup de Pasos recibe el filtro y la comparación como parte de la
   pantalla y puede reacomodarlos, sin cambiar su comportamiento.

### Out (no incluye)

- El período en Operadores, Operador (detalle, curva y ficha) y Seguimiento (D-209).
- Que el estado "listo" use el período: sigue con todo el historial (DA-1); Seguimiento sigue
  sin período (DA-3, D-141).
- Comparar dos rangos elegidos libremente, lado a lado.
- Cambio en la tasa de primer intento y en el puntaje promedio (no se eligieron).
- Una librería de calendario.
- Guardar el último período elegido entre visitas (la URL basta para compartirlo).

## Decisiones del usuario (2026-10-27)

| ID | Decisión |
|---|---|
| D-209 | Filtro de período **solo en Pasos y Resumen**, para cuatro usos: revisar un grupo o capacitación, reportes periódicos, antes y después de un cambio y seguimiento diario |
| D-210 | Un solo control: **atajos + Personalizado…** (Inicio y Fin al elegirlo) y el rango aplicado en palabras debajo. **Enmendada 2026-10-28:** con bocetos a la vista, el usuario eligió la lista **reducida** de 7 opciones (P-3) para que no se vea densa; "solo Inicio y Fin" se descartó porque obliga a armar a mano los reportes semanales y mensuales |
| D-211 | **Comparación con el período anterior** de igual largo; con un período en curso, contra los **mismos días** del anterior |
| D-212 | Muestran el cambio: tasa de aprobación, sesiones y Top 5 en el Resumen; % con fallo por paso en Pasos |
| D-213 | El Excel de Pasos lleva las **columnas de cambio** |
| D-214 | Gráfica de actividad con **granularidad automática** (día, semana o mes) |
| D-215 | Al entrar: **Este mes** |
| D-216 | Fechas con el **selector nativo** del navegador, sin librería |
| D-217 | La 023 va **antes** del mockup de Pasos de la 022, con el sistema de diseño actual |

## Propuestas de Claude (confirmadas por el usuario, D-218)

| # | Propuesta |
|---|---|
| P-1 | **Fecha que filtra:** el inicio de la sesión, como hoy (018, punto 8). Una sesión que empezó el 31 y terminó el 1 cuenta en el día 31 |
| P-2 | **Hora de la planta:** todos los atajos cortan a medianoche de la planta (`PlantTime`, Central America), también "Últimos 30 días" (hoy y los 29 días anteriores, completos; deja de ser la ventana móvil de 30×24 h de la spec 018); el día de Fin entra completo. La BLL convierte el rango local a UTC |
| P-3 | **Atajos (enmendada 2026-10-28):** Hoy · Esta semana (lunes a hoy) · Este mes (día 1 a hoy) · Mes anterior · Últimos 30 días (incluye hoy) · Todo · Personalizado…. Salen Ayer, Semana anterior y Últimos 7 y 90 días (antes confirmados): se arman con Personalizado. Sin trimestre |
| P-4 | **Período anterior:** Hoy → ayer; Esta semana → los mismos días de la semana anterior; Este mes → los mismos días del mes anterior (si es más corto, hasta su último día: 1–31 oct. vs. 1–30 sep.); Mes anterior → el mes previo; Últimos 30 días → los 30 días previos; Personalizado → los mismos días justo antes; Todo → sin comparación |
| P-5 | **Cómo se ve el cambio:** flecha ▲/▼ con el valor ("▲ 6 pts", "▼ 12 %") y la referencia ("vs. 1–27 sep."), nunca solo color (D-156). El color dice si es mejora: en aprobación y sesiones, subir es mejora; en % con fallo, bajar es mejora. Sin datos en el anterior: "sin datos para comparar" |
| P-6 | **Validación en la BLL** (no confía en la página): Personalizado exige Inicio y Fin, Fin ≥ Inicio y Fin ≤ hoy. Un período inválido en la URL cae en **Este mes** con un aviso. El selector nativo pone `max` = hoy |
| P-7 | **URL:** `?period=today|week|month|prevmonth|30d|all|custom&from=AAAA-MM-DD&to=AAAA-MM-DD`; sin parámetro = Este mes; `period=all` sigue funcionando. El enlace del Top 5 a Pasos y las descargas llevan el período (D-147) |
| P-8 | **Datos:** `spDashboardSessions` y `spDashboardStepAttempts` suman `@To` (exclusivo, UTC, `NULL` = sin límite). Una sola lectura cubre el período y el anterior, y la BLL los separa en memoria. Sin tablas ni índices nuevos, salvo que la medición (P-13) lo pida. (Precisión 2026-10-28: el Resumen ya lee todas las sesiones y filtra en memoria; el `@To` lo usan Pasos, el Top 5 y la descarga de sesiones) |
| P-9 | **Granularidad:** por día hasta 31 días; por semana hasta 366 días; por mes, más largo. "Todo" usa el largo real (desde la primera sesión) |
| P-10 | **Excel:** el de Pasos suma "% con fallo (anterior)" y "Cambio (pts)"; el de sesiones del Resumen no lleva comparación (una fila por sesión), solo el rango en el título |
| P-11 | **"Sesiones" en el Resumen:** un valor nuevo en el encabezado de la tarjeta de actividad, sobre las barras, "Sesiones del período" (todos los modos, de la guía filtrada), con su cambio en %. Es derivado: no pide datos nuevos |
| P-12 | **Un solo control reutilizable** (un UserControl de Admin o una pieza común de la página) para Pasos y Resumen, que la 022 puede reestilizar |
| P-13 | **Rendimiento:** con la BD de volumen (Fase 11, AC-7), Resumen y Pasos cargan en menos de 3 s con Este mes, con Todo y con un Personalizado de 12 meses (que lee 24 con el anterior); el Excel de Pasos, en menos de 20 s con Todo |
| P-14 | **Textos:** el nombre del atajo en el selector y el rango exacto debajo; en Excel, el título con el rango ("Pasos · Assessment · Del 1 al 27 de oct. de 2026") |
| P-15 | **Plan:** E1 datos y BLL del rango y la comparación, con tests; E2 Resumen (control, comparación, sesiones y gráfica); E3 Pasos y su Excel; E4 rendimiento y cierre |
| P-16 | **Base de la comparación (2026-10-28):** cada cambio lleva la base de los dos períodos ("▲ 100 pts · 1 vs. 2 evaluados"; en sesiones y % con fallo, las sesiones). Sin umbral mínimo: un cambio con pocos datos se ve, pero con su base |

## Acceptance criteria

- **AC-1** Given Pasos o Resumen sin parámetros, Then el período es **Este mes** y debajo dice
  el rango ("Del 1 al 27 de oct. de 2026").
- **AC-2** Given el selector, Then ofrece los atajos de P-3 y Personalizado. When se elige
  Personalizado, Then aparecen Inicio y Fin; When se aplica un rango válido, Then los números
  y la gráfica son de ese rango, en hora de la planta, con el día de Fin completo.
- **AC-3** Given un rango inválido (Fin antes de Inicio, Fin en el futuro, una sola fecha) en
  el formulario o en la URL, Then no se aplica: se ve un aviso y el período queda en Este mes.
- **AC-4** Given un período distinto de Todo, Then la tasa de aprobación por guía, las sesiones
  del período, el Top 5 y el % con fallo de Pasos muestran su cambio vs. el período anterior de
  P-4, con flecha, valor, referencia y la base de los dos períodos (P-16). Given Todo, Then no
  hay comparación.
- **AC-5** Given el período anterior sin datos, Then dice "sin datos para comparar", sin flecha.
- **AC-6** Given períodos de 1, 7, 31, 90 y 400 días, Then la gráfica va por día, día, día,
  semana y mes, y su título lo dice.
- **AC-7** Given el Top 5 del Resumen, When se sigue el enlace a Pasos, Then Pasos abre con el
  mismo período (también Personalizado) y la misma guía, y sus números coinciden (D-147).
- **AC-8** Given el Excel de Pasos, Then su título dice el rango y cada paso trae el % del
  período anterior y el cambio en puntos (vacíos con Todo). El Excel de sesiones dice el rango
  y sus filas son solo las de ese rango.
- **AC-9** Given Operadores, Operador y Seguimiento, Then no cambian; el estado "listo" sigue
  con todo el historial.
- **AC-10** Given la BD de volumen, Then se cumple P-13.
- **AC-11** Los tests de la BLL cubren el cálculo de los rangos (cada atajo, el anterior, los
  bordes de mes y de semana y la hora de la planta) y la validación; los tests existentes
  siguen en verde.

## Riesgos y coordinación

- **Spec 022:** su mockup de Pasos espera esta spec (D-217). Se le avisa a la sesión de la nube
  al aprobarla.
- **Rendimiento:** "Este mes" al entrar lee menos que los 30 días de hoy; un Personalizado
  largo con su anterior lee casi como Todo. Si P-13 no se cumple, se avisa a Planeación antes
  de tocar SPs o índices.
- **Zona horaria:** las sesiones se guardan en UTC; un error en la conversión mueve sesiones de
  un día a otro. Lo cubren los tests de AC-11.
