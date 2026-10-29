# Spec 024: Dashboard por área (Operadores y Operador)

**Status:** Implemented en la nube, **no verificada** (2026-10-28: E1 `091f62c`, E2 `edf946a`, E3 `f5b1d74`, E4 `bc473e9`) · Approved 2026-10-28 · **enmendada 2026-10-29** (enmienda 1: D-272 a D-274) · plan `024-plan.md`
**Date:** 2026-10-28 · **Origen:** Arquitectura y Planeación (local), después de cerrar la 021 ·
**Sesión:** `TrainingApp-Desarrollo` en la nube, de punta a punta (`docs/process.md` →
"Variante temporal" → "Spec 024"), rama `cloud/024-dashboard-por-area`
**Decisiones:** D-260 a D-270, del usuario. Las propuestas de Claude (P-1 a P-12), confirmadas
por el usuario el 2026-10-28 (P-11 con menos tests), se registran juntas como **D-271**. Rango de
este frente: D-260 a D-279 y B-40 a B-49.

## Problem

La 021 le dio a cada operador **un área** (`Users.AreaId`) y solo lo deja iniciar las guías
de esa área. El dashboard lo dejó solo como texto (D-175): Operadores sigue con **una columna
por cada guía publicada** (`Operators.aspx`), así que cada operador sale "Sin iniciar" en las
guías de áreas que no le tocan. Para el trainer de un área eso es ruido e información falsa:
no puede ver de un vistazo quién de **su** área falta, reprobó o está en entrenamiento.

El detalle del Operador mezcla también todas las guías publicadas, sin separar su área del
entrenamiento cruzado (la 021 lo hace cambiando el área y regresándola, D-165).

## Scope

### In

**A. Operadores: la lista por área** (D-260, D-261, D-264, D-265, D-266, D-268)
1. **Quién la usa:** el trainer o el gerente de un área, para decidir a quién entrenar o
   evaluar de **su** área (D-260).
2. Se quitan las **columnas por guía**. Queda una sola columna, **Estado en su área**: la
   insignia de la guía publicada de su área con sus puntajes de Assessment (mejor y último),
   como hoy en cada celda (D-261). Hoy cada operador tiene **una guía que entrenar**: la de su
   área (D-268).
3. Las guías de **otras áreas** no aparecen en la lista: ni como columna, ni como indicador
   (D-266). Un operador nunca sale "Sin iniciar" en una guía que no es de su área.
4. **Filtros:** Buscar, **Área** (nuevo) y Estado. Se quita el filtro **Guía** (D-264): la lista
   ya no se mira por guía; cada operador se mira en su área (D-265).
5. **Área** arranca en **Todas** (D-262). Las opciones: Todas, cada área con guía publicada
   (D-176 de la 021), **agrupadas por producto** como en "Elige tu área", y **Sin área**. No
   hay opción de producto completo (D-262). Vive en la URL como los demás filtros.
6. **Estado** se evalúa en la guía de su área (D-268). **Sin iniciar** = el operador tiene
   área y ninguna sesión en las guías de su área (D-263).
7. **Sin área** (todavía no la eligió): la columna dice "Sin área", sin insignia, y **no**
   cuenta como Sin iniciar (D-267). Solo sale con Estado = Todos.
8. El **Excel** de Operadores, igual que la pantalla (P-7).

**B. Operador: su área primero** (D-269, D-270)
9. **Su área:** todas las guías publicadas de su área, también las Sin iniciar, con estado,
   puntajes y última actividad.
10. **Otras áreas (entrenamiento cruzado):** una sección **colapsada**, solo con las guías de
    otras áreas en las que tiene sesiones; no aparece si no hay (D-269).
11. **Pasos a reforzar** y **Curva de aprendizaje:** primero su área, después las otras.
12. **Sesiones:** la tabla completa (todas las áreas), con una columna **Área**.
13. **Ficha imprimible:** su área completa y las otras áreas en **una línea** ("También
    entrenó: …") (D-270).

**C. La regla del dominio** (D-263)
14. "Sin iniciar" cambia en `docs/domain.md` → "Dashboard de Training": de "ninguna sesión en
    ninguna guía publicada" a "**ninguna sesión en las guías publicadas de su área**". El estado
    por guía (`ReadinessPolicy`) no cambia.

### Out (no incluye)

- **Resumen, Pasos y Seguimiento**: sin cambios. El Resumen ya no cuenta "Sin iniciar" (D-164)
  y sus números son por guía, con todos los que la entrenaron (P-2).
- Período en Operadores y Operador (siguen con todo el historial, D-209).
- Historial de cambios de área (P-1).
- Guías distintas dentro de un área (llegarían con las **guías por operación**, futuro; D-268).
  El código no se rompe si llegan (P-6), pero su diseño se decide entonces.
- Una guía **compartida** por varias áreas en la BD: las áreas de un producto que usan "la misma
  guía" (ej. las de Catéteres) se cargan con **una copia por área**, como hoy (`docs/process.md`
  → 6, "Área nueva"; D-268).
- Filtrar por producto completo (D-262).
- Alcance por área para los administradores o el rol Trainer.
- Cambios en cómo se entrena, se califica o se calcula el estado de una guía.

## Decisiones del usuario (2026-10-28)

| ID | Decisión |
|---|---|
| D-260 | Operadores es la vista del **trainer de un área**: quién de su área falta, reprobó o está en entrenamiento |
| D-261 | Se quitan las columnas por guía: **una columna "Estado en su área"** |
| D-262 | El filtro **Área** arranca en **Todas**; sus áreas, **agrupadas por producto**; sin opción de producto completo (2.ª aclaración) |
| D-263 | **Sin iniciar** = ninguna sesión en las guías publicadas **de su área** (cambia `domain.md`) |
| D-264 | Se **quita el filtro Guía** de Operadores |
| D-265 | La lista muestra a cada operador **solo en su área**; nadie aparece por guías de otras áreas |
| D-266 | La lista **no muestra nada** de las guías de otras áreas |
| D-267 | Un operador **sin área** sale como "Sin área", sin insignia, **fuera de Sin iniciar**; el filtro Área tiene la opción "Sin área" |
| D-268 | Hoy un operador tiene **una guía que entrenar: la de su área** (la misma en contenido para las áreas de un producto, cargada como **una copia por área**). Guías distintas por área u operación llegarían con las guías por operación (futuro), fuera de alcance (aclaración del usuario, 2026-10-28) |
| D-269 | Operador: "Su área" con todas sus guías y **"Otras áreas" colapsada**, solo con guías con sesiones; Reforzar y curvas, su área primero; Sesiones completas con el área |
| D-270 | Ficha: **su área completa** y las otras áreas **en una línea** |

## Propuestas de Claude, confirmadas por el usuario (2026-10-28)

El usuario las confirmó una por una el 2026-10-28; solo cambió P-11 (menos tests). Se registran
juntas como **D-271**.

| # | Propuesta |
|---|---|
| P-1 | **"Su área" es la actual**, sin historial de cambios (el usuario no eligió). Si a alguien lo cambian de área, su fila y su detalle pasan a la nueva (Sin iniciar si no la entrenó) y lo de la anterior queda en "Otras áreas". Sin tabla nueva; coincide con D-174 y el Out de la 021 |
| P-2 | **Resumen, Pasos y Seguimiento quedan fuera.** El Resumen no cuenta "Sin iniciar" desde D-164: cuenta, por guía, a quien tiene sesiones en ella. No hay contradicción con Operadores. La duda 2 de Planeación queda sin efecto |
| P-3 | **El filtro Área va en la BLL**, no en el SP: `DashboardService.FindOperators` ya pagina en la BLL, después de calcular el estado (ver el comentario de `spDashboardOperators`). Los SPs solo agregan columnas: `spDashboardOperators` devuelve `AreaId` y `spDashboardGuides` devuelve `AreaId`. Sin parámetros nuevos ni cambios de índices |
| P-4 | **URL:** `?area=<Id>` o `?area=none` (Sin área); sin `area` = Todas. Un `area` que no es elegible o no es un número se ignora (Todas), como hoy un `guide` inválido. Un enlace viejo con `?guide=` se ignora |
| P-5 | **Celda "Estado en su área":** la insignia y los puntajes de la guía de su área, como la celda de hoy. Sin área: "Sin área" (texto atenuado). Con área pero sin guía publicada (el área se quedó sin guía, P-6 de la 021): "Sin guía publicada" (atenuado), y no cuenta como Sin iniciar |
| P-6 | **Si un área llega a tener varias guías** (guías por operación, futuro, D-268): la celda muestra una insignia por guía de su área y Estado = X es "alguna guía de su área en X"; Sin iniciar, "ninguna sesión en ninguna". Así el código no se rompe; el diseño se revisa cuando pase |
| P-7 | **Excel de Operadores:** columnas Operador, Código, Área, Estado en su área, Mejor, Último y Última actividad, con los filtros Buscar, Área y Estado; sin columnas por guía. Mantiene el formato de la Fase 11 |
| P-8 | **Operador sin área:** "Su área" dice "Sin área" y sus guías con sesiones van en "Otras áreas", que en ese caso se muestra **abierta** (es todo lo que tiene). La ficha, igual: "Sin área" y la línea de otras |
| P-9 | **"Otras áreas" colapsada:** un `<details>` nativo (sin JS ni estado guardado); en la ficha impresa no se imprime (va la línea de D-270). La línea: "También entrenó: Producto · Área — Estado" por cada guía, separadas por punto y coma |
| P-10 | **Código:** la división "su área / otras" se calcula en la BLL (`DashboardService`, una regla pura con tests), no en la página. `ReadinessPolicy` no cambia. `OperatorRow.NotStarted` pasa a "ninguna sesión en las guías de su área" |
| P-11 | **Tests, solo lo nuevo** (usuario: "menos tests"; NUnit, en memoria): el filtro Área (Todas, un área, Sin área, inválido) y Estado con la nueva Sin iniciar (área sin sesiones, cruzado sin su área, sin área y sin guía publicada fuera). El detalle del Operador y el Excel se prueban en el navegador. Los tests existentes que se rompan por las columnas nuevas se ajustan, no se borran |
| P-12 | **Briefs de la 022:** Diseño UI/UX enmienda con fecha los briefs 6 y 7 de `022-briefs.md` antes de la E6 (ver "Qué cambia para la 022") |

## Enmienda 1 (2026-10-29, revisión local de Arquitectura y Planeación)

La revisión de la rama contra esta spec y el plan no encontró desvíos de la regla ni bugs.
Lo que la nube decidió sin preguntar, o que la spec no decía, lo confirmó el usuario:

| ID | Decisión |
|---|---|
| D-272 | **Sesiones** del Operador: la columna "Guía" pasa a ser **"Área"** ("Producto · Área", el título de la guía al pasar el mouse) en vez de agregar otra; hoy cada área tiene una guía (D-268). Acepta el ajuste de la nube en la E3 (punto 12) |
| D-273 | **Ficha impresa:** solo las tarjetas de "Otras áreas" van en la línea "También entrenó" (D-270); **Pasos a reforzar** (su área primero) y **Sesiones** se imprimen completos, con las otras áreas |
| D-274 | El hook de inicio de la nube y `tools/build-check.py` / `tools/restore-packages.py` (`b95b27c`) **no entran** a `TrainingApp`: la rama local `spec-024` parte de `73a2829`, sin ese commit |

## Acceptance criteria

- **AC-1** Given Operadores, Then no hay columnas por guía: cada fila tiene Operador, Área,
  Estado en su área y Última actividad. Nadie sale "Sin iniciar" por una guía de otra área.
- **AC-2** Given el filtro Área, Then sus opciones salen agrupadas por producto. Given Área = LoFlo, Then solo salen los operadores de LoFlo, con el estado de la guía
  de LoFlo. Given Área = Sin área, Then solo los que no la eligieron.
- **AC-3** Given Estado = Sin iniciar, Then salen los operadores con área y sin sesiones en la
  guía de su área (incluido quien solo entrenó otra área), y **no** los sin área.
- **AC-4** Given Estado = Aprobado (o Reprobado, o En entrenamiento), Then salen los que tienen
  ese estado en la guía de su área; uno aprobado solo en otra área no sale.
- **AC-5** Given filtros en la URL, When F5, Atrás o un enlace, Then la vista se conserva. Un
  `area` inválido vale Todas; un `?guide=` viejo se ignora.
- **AC-6** Given el Excel con esos filtros, Then tiene las mismas filas y las columnas de P-7.
- **AC-7** Given el detalle de un operador con entrenamiento cruzado, Then "Su área" muestra la
  guía de su área (aunque esté Sin iniciar) y "Otras áreas" aparece colapsada con la guía
  cruzada; Reforzar y curvas, su área primero; Sesiones con todas y su Área.
- **AC-8** Given un operador sin entrenamiento cruzado, Then no aparece "Otras áreas". Given uno
  sin área, Then "Su área" dice "Sin área" y "Otras áreas" está abierta con lo que entrenó.
- **AC-9** Given la ficha impresa, Then lleva su área completa y la línea "También entrenó: …"
  (o nada si no hay), en claro, sin menú ni botones.
- **AC-10** Resumen, Pasos y Seguimiento muestran los mismos números que antes; los tests de
  `ReadinessPolicy`, `SummaryRules` y `ScoreCalculator` siguen en verde.
- **AC-11** Cada método del dashboard sigue verificando `CanViewReports` en la BLL; un operador
  que abre Operadores u Operador vuelve a su inicio.

## Qué cambia para la 022 (briefs 6 y 7)

Diseño UI/UX los enmienda con fecha antes de mandar la E6 a Claude Design:
- **Brief 6 (Operadores):** sin columnas dinámicas por guía; una columna "Estado en su área"
  (una insignia, hoy una guía por área); filtros Buscar, **Área** (Todas, cada área, Sin área) y
  Estado (el de Área, agrupado por producto); **sin** filtro Guía; "Sin área" sin insignia; "filtrar por área" deja de estar en
  "Queda fuera". Capturas: Área = una, Estado = Sin iniciar.
- **Brief 7 (Operador):** "Estado por guía" pasa a **"Su área"** (sus guías, también Sin iniciar)
  más **"Otras áreas (entrenamiento cruzado)"** colapsada; Reforzar y curvas con su área primero;
  Sesiones con columna Área; la ficha con su área y una línea "También entrenó". Capturas: un
  operador con entrenamiento cruzado y uno sin área.

## Riesgos y coordinación

- **Orden con la 022:** esta spec va **antes** de la E6 de la 022 (Operadores y Operador). La E6
  rediseña sobre la estructura que deja esta spec.
- **`admin.css`:** si hace falta un estilo nuevo, va en un bloque propio "Operadores / Operador"
  para no chocar con los bloques de la 022 (`.auth-*`, `.pw-*`, `.mt-steps`). `Admin.Master` no
  se toca.
- **`10_spDashboard.sql`** lo tocó la 023 (`@To`); esta spec solo agrega columnas a
  `spDashboardGuides` y `spDashboardOperators`.
- **Verificación:** la nube no compila ni prueba; el código va marcado "no verificada" y lo
  verifica Desarrollo local con su ciclo (`docs/process.md` → "Spec 024").
