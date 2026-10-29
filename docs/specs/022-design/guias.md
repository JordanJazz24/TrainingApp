# Guías · alternativa 2b

**Spec:** `../022-mockups-por-pantalla.md` · **Rama de partida:** `cloud/022-guias` (desde `TrainingApp`,
"no verificada") · **Brief:** 3 de `../022-briefs.md`.

Lista de cambios frente al v1, tal como la entregó Claude Design ("Comparación de diseños login
alternativos", `Guias.dc.html`, ronda 2, opción 2b).

## Alternativa elegida
**2b · Conteo por tipo en el encabezado.** Es la misma tabla del v1, con el reparto por tipo a la vista y las guías Custom primero.

## Cambios frente al v1
- **Tokens:** ninguno nuevo ni con otro valor, ni en claro ni en oscuro.
- **Componentes:**
  - Nuevos: `.guides-head`, `.guides-counts` y `.guides-note`.
  - Se reutilizan `.mt-chip` y `.mt-panel-body` sin cambios.
- **Estructura:**
  - El encabezado del panel lleva el título y dos chips ("N Estándar", "N Custom").
  - Nota al pie del panel: "Las guías con Custom van primero."
  - Los chips y la nota se ocultan si no hay guías.
- **Comportamiento:**
  - La tabla se ordena con las Custom primero; dentro de cada tipo queda el orden de la BLL.
  - Después de guardar, la fila puede cambiar de lugar y los conteos se actualizan.
  - El selector, Guardar, la confirmación y los avisos no cambian.
- **Textos nuevos:** "Estándar" y "Custom" en los chips, y la nota al pie.
- **Métrica derivada:** guías por tipo, que se calculan contando las publicadas por Resource Setup. No requiere datos nuevos.

## Para revisar en Desarrollo (no compilado)
- `Guides.aspx.cs` compila con los controles nuevos: `PhCounts`, `LitCountStandard`, `LitCountCustom` y `PhOrderNote`
  (agregados también a `Guides.aspx.designer.cs`).
- El orden y los conteos son correctos con 0, 1 y varias guías de cada tipo.
- Después de guardar se entiende qué fila cambió, aunque se haya movido.
- Capturas `guias-claro.png`, `guias-oscuro.png` y `guias-tablet.png`: las toma Desarrollo de la versión final (D-240).
  *2026-10-29: no se tomaron (la app no estaba corriendo al cerrar la E3; al usuario le dan igual
  las capturas, como en la E2b).*
- *Ajuste del usuario (2026-10-29):* la leyenda de la página va debajo del título "Guías" (título
  y leyenda en un `<div>` dentro de `.admin-page-head`, como Seguimiento y Operadores).
