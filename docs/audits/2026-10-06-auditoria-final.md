# Auditoría final antes de la 6.6 — 2026-10-06 🔍

> Pedida por el usuario el 2026-10-05 para asegurar que no se escapó nada grave antes de
> los overlays. Plan propuesto por Claude en un `/btw` y **aprobado por el usuario el
> 2026-10-06**. Continúa la numeración de `2026-10-01-auditoria.md`: hallazgos desde
> **AUD-33**, decisiones desde **D-13**. Los criterios de severidad, los falsos positivos y
> lo marcado como "limpio, no tocar" de esa auditoría siguen vigentes.

## Alcance

La auditoría anterior fue completa y reciente; desde entonces el cambio grande fue la spec
009 (`b2db442..df08c44`). Por eso es una auditoría enfocada en ese cambio más una regresión
completa en el navegador, con el mismo esquema A → B → C, más liviana.

**Costo:** un `/code-review high` sobre todo el cambio en vez de seis, medium en las
entregas; lo demás (arch-check, código muerto) es barato.

## Plan

- **Fase A · Revisión, solo lectura (no se corrige nada mientras se audita):**
  1. **Un solo `/code-review high`** sobre todo el cambio de la spec 009 (no uno por entrega
     ni por carpeta). Foco: las reglas de `PodService` (secuencia, guardar el estado después
     del intento, reconstrucción); las capas (que la página no decida nada); la redirección
     de P-1.
  2. `/dotnet-claude-kit:arch-check` (dependencias entre capas; es barato).
  3. Código muerto después del refactor, con Roslyn: helpers de `GuidedTest` sin uso, claves
     de `StepViewModel` sin lectores, `IStepControl`/`CompleteControl`.
  4. `/security-review` acotado a lo que tocó la Session y el login: logout con D-9, la
     redirección de P-1 y `SessionPodStateStore`.
  5. Docs contra código: que `CLAUDE.md` (árbol y convenciones), el roadmap y el glosario
     describan lo que existe hoy.
  6. Repaso de la deuda abierta (abajo), para decidir qué se arregla ya y qué se difiere.
- **Fase B · Lista única:** hallazgos nuevos AUD-33 en adelante, con severidad, vía (plan
  directo, decisión o diferido) y entrega. Las decisiones se preguntan juntas, numeradas desde
  **D-13**, cada una con una propuesta.
- **Fase C · Entregas chicas, solo si hay hallazgos Alto o Medio.** Mismo ciclo: arreglo →
  MSBuild + `aspnet_compiler` → `/code-review medium` → checklist → prueba del usuario →
  commit.
- **Fase D · Pasada final en el navegador** (criterio de cierre antes de la 6.6): un solo
  checklist de punta a punta, un recorrido por combinación: Moldeo en Guided y en Assessment
  (con errores, clicks fuera de secuencia y avances forzados); Redefinity en Guided y en
  Assessment; pérdida de la Session; sesión única y logout. Así la 6.6 arranca sobre una base
  probada entera, no por entregas sueltas.

## Deuda abierta a repasar (Fase A, punto 6)

- El Start de NonSerialized acepta cualquier SFC escrito (`docs/specs/009-plan.md`, E2).
- B-2 de Log NC: solo se da con manipulación del cliente (`docs/specs/009-plan.md`, E4).
- Guardar por request el BOM y los grupos DC.
- El botón Atrás del navegador todavía puede reenviar un POST viejo (riesgo aceptado hasta
  ahora; F5 sí está resuelto).
- AUD-28/29: siguen para la Fase 9 (D-7).

**Decisión a tomar en esta auditoría:** los tests siguen fuera por decisión del usuario.
Ahora las reglas del POD viven en `PodService`, pero no se pueden probar sin BD porque el
servicio crea sus repositorios internamente. Si algún día se retoma la Fase 5, conviene
decidir ahora si se prepara eso o se deja como está.

## Fase A · Resultados

_Pendiente._

## Lista única de hallazgos (AUD-33 en adelante)

_Pendiente (Fase B)._

## Decisiones del usuario (D-13 en adelante)

_Pendiente (Fase B)._

## Entregas (Fase C)

_Pendiente._

## Pasada final en el navegador (Fase D)

_Pendiente._
