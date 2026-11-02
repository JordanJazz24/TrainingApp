# Inicio del operador · alternativa D "Modo primero" (aprobada 2026-11-02)

**Spec:** `../022-mockups-por-pantalla.md` · **Brief:** 9 de `../022-briefs.md` · **Plan:**
`../022-plan.md` → E8 · **Implementación:** `3817e50` (handoff local de Claude Design, D-251).

> **Reconstruida por Diseño UI/UX el 2026-11-02 desde el diff de `3817e50`.** El handoff no se
> guardó como lista de cambios antes de implementar (D-251 lo pide). Por eso faltan las rondas y
> el resumen de la crítica de Claude Design: aquí solo queda lo que cambió en el código.

## Alternativa elegida

**D · Modo primero.** La tarjeta del inicio pregunta primero "¿Cómo quieres entrenar?", con las
3 tarjetas de modo de siempre. Debajo va una sola fila con la Guía ("Guía · N guías") y el botón
Iniciar. El área ya no se repite en la página: está en la barra superior ("Tu área", D-286).

## Cambios frente al v1

- **Tokens:** ninguno.
- **Componentes (`admin.css`, bloque "Inicio del operador"):**
  - se borra `.start-fields`;
  - entran `.start-row` (fila flexible, el campo de 280 px como base) y `.start-go` (Iniciar de
    48 px de alto y 170 px de ancho mínimo, y a todo el ancho a 767 px o menos).
- **Estructura y controles (`StartSession.aspx`):**
  - sin cambio: `DdlGuide`, `RbGuided`, `RbFreePractice`, `RbAssessment`, `BtnStart`,
    `LblNoGuides`, `PhStart`, el aviso sin área (D-168) y el error del servidor;
  - se borra `LblArea`;
  - nuevo `LitGuideCount` (`Literal`, en la etiqueta de la Guía).
- **Comportamiento:** igual. Mismo postback de Iniciar, mismo orden de redirecciones del
  `Page_Load` y la misma `ShowArea` de la barra.
- **Textos:** la leyenda "Modo" pasa a "¿Cómo quieres entrenar?"; la etiqueta "Guía" pasa a
  "Guía · 1 guía" o "Guía · N guías".
- **Presentación en la BLL:** `OperatorStartOptions.GuideCountText` ("1 guía" / "N guías"), sin
  test y sin regla nueva.

## Desvíos de alcance

Ninguno. Solo hay un dato que deja de verse en la página: el área, que sigue en la barra (P-5,
D-316).
