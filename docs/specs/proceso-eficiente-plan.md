# Plan directo — Flujo de trabajo eficiente (2026-10-30)

**Pedido del usuario (2026-10-30):** optimizar el trabajo entre sesiones para que funcione bien
y sin gastar tokens de más ("flujos óptimos, senior, profesionales"). Adopta las 7 propuestas
de Arquitectura y Planeación y, antes de planear, se revisaron `/usage` y `/insights`.
Camino: plan directo (`process.md` → 1, fila 4: forma de trabajar, sin reglas de negocio).

## Datos de partida

- **`/insights`** (2026-10-06 a 10-30): 81 sesiones, 287 commits, 397 `SendMessage`, 175
  preguntas; 43 de 46 objetivos logrados. Fricciones: relevos entre sesiones (una sesión
  creada sin pedirla, mensajes al destinatario equivocado, cambio de dirección), afirmaciones
  sin verificar, y git en Windows (carpeta de precompilado con `git add -A`, commit a `main`,
  ediciones por script que rompen CRLF).
- **`/usage`** (últimos 7 días): "Empezar de cero al cambiar de tarea (`/clear`)" aplica al
  **78 %** del uso; "subagentes en un modelo más barato", al 68 %; "cerrar sesiones que no se
  usan (algunas de 8 h o más)", al 16 %. `/code-review` es el 6 %. Ventana de 5 h al 42 %
  tras 1 h 40 de esta sesión; semana al 6 %: el límite que aprieta es la ventana de 5 h, que
  comparten todas las sesiones abiertas.
- **Sesión de Planeación del 2026-10-30:** $8,16 en Opus 5.5: lectura de caché 24 %,
  escritura de caché 30 %, salida 26 %, entrada 20 %; contexto promedio ~200k tokens por
  turno. En Sonnet 5.5 todo eso cuesta la mitad salvo la lectura de caché: ~38 % menos.
- **Repo** (desde el 2026-10-16): 194 de 269 commits son solo de docs; el cierre de la E5
  quedó escrito en 6 archivos; `roadmap.md` tiene 687 líneas (~450 de fases cerradas) y
  "Estado actual" de `status.md`, unas 190.

## Decisiones (usuario, 2026-10-30)

| D | Decisión |
|---|---|
| D-243 | **Sin sesión de documentación aparte.** Quien decide, registra: Planeación escribe specs, planes, enmiendas y D-n; Desarrollo, los cierres de su entrega. Una sesión más solo agrega contexto en frío y relevos. |
| D-244 | **Estado por tiempo, un dueño por archivo y sin relevos.** Pasado: git, las casillas del plan y `history.md` solo al cerrar una spec, fase o plan directo. Presente: `status.md` → "Estado actual", **40 líneas como máximo**, se reescribe (Desarrollo). Futuro: `roadmap.md` → "Siguiente", **15 líneas como máximo** (Planeación: orden y bloqueos). Desaparece "Para retomar"; la numeración D-n/B-n vive solo en `decisions.md`; Desarrollo avisa a Planeación solo si el plan no cierra o al cerrar una spec o fase, no por entrega. |
| D-245 | **Lo cerrado sale del roadmap** a `docs/archivo/roadmap-hasta-2026-10-30.md` (fases 7 a 11, specs 023 y 024 y las revisiones con negocio). El roadmap queda con la tabla de fases, lo abierto y "Siguiente": menos de 200 líneas. |
| D-246 | **Modelo por trabajo.** Planeación: Opus 5.5, `medium`; `high` en specs, planes y enmiendas. Desarrollo: Sonnet 5.5, `medium`; `high` en entregas con BLL o SQL o ante un bug difícil (piloto en la E6 de la 022). Review: **Opus** (verificador fuerte; el review es el 6 % del uso). Subagentes por defecto: Sonnet. Reuniones y Dudas: Sonnet 5.5, `medium`. Los docs mecánicos de Desarrollo salen en Sonnet por ser de Desarrollo. |
| D-247 | **Coordinación por mecanismos, no por texto.** Un nombre único y fijo por sesión (los mensajes van por nombre, que sobrevive a un cambio de dirección); hooks de proyecto para lo que se puede bloquear (git); una skill `/rol <planeacion\|desarrollo\|reuniones>` que carga el rol al iniciar y apunta a `process.md` (única fuente). `CLAUDE.md` no crece. |
| D-248 | **Higiene de sesión.** `/clear` al cambiar de tarea (el estado vive en el repo, no en la conversación); cerrar las sesiones que terminaron su trabajo; `/usage` (semana) los viernes, junto con el reporte. |
| D-249 | **La variante de la nube termina al cerrar la 022** (E8): se quita `process.md` → "Variante temporal" y las specs y planes vuelven a la sesión local. |

**Propuestas de Claude, confirmadas por el usuario el 2026-10-30 (las cuatro como se propusieron):**
- P-1. `/cerrar-entrega` es una skill normal, **sin subagente**: Desarrollo ya corre en Sonnet,
  así que el subagente no ahorraría nada y sumaría una pieza.
- P-2. Del "On the horizon" de `/insights` (agente integrador, entregas en paralelo en
  worktrees, autoverificación en Chrome con capturas) **no se adopta nada por ahora**: suman
  sesiones y tokens, y el "funciona" del usuario ya es el control. Se revisa al terminar la 022.
- P-3. Las reglas de exactitud que propone `/insights` ("citar una regla solo tras buscarla",
  "IDs de controles que no choquen con funciones JS", "no codificar dos veces") no van a
  `CLAUDE.md`: la primera ya es el estándar de trabajo y las otras dos van al checklist de
  `/cerrar-entrega`.
- P-4. El MCP de SQL Server que sugiere `/insights` ya está configurado (`mcp__sqlserver__*`):
  sin cambios.

## Entregas

### E1 · Docs de proceso y roadmap (Arquitectura y Planeación, en `TrainingApp`)

- [x] `process.md` → "Tres sesiones de trabajo": modelos y esfuerzo por sesión (D-246),
      coordinación por nombre y sin avisos por entrega (D-244, D-247), higiene (D-248), la
      fecha de cierre de la variante de la nube (D-249).
- [x] `process.md` → "9. Documentación": la tabla según D-244 (qué va al pasado, al presente
      y al futuro; `history.md` solo al cerrar spec, fase o plan directo).
- [x] `roadmap.md`: lo cerrado a `docs/archivo/roadmap-hasta-2026-10-30.md` (D-245), "Para
      retomar" se reemplaza por "Siguiente" (máx. 15 líneas), la numeración sale.
- [x] Skills `.claude/skills/rol/SKILL.md` y `.claude/skills/cerrar-entrega/SKILL.md`
      (markdown): `/rol` lee `process.md` → "Tres sesiones" y el archivo de estado de su rol;
      `/cerrar-entrega` sigue `process.md` → 2 y 9 (casilla y hash en el plan, `status.md`,
      commit solo de sus rutas) y suma el checklist de P-3. Ninguna copia reglas: apuntan.
- [x] `decisions.md`: D-243 a D-249.
- [x] Commit solo de esas rutas.

**Hecho cuando:** el usuario aprueba el diff. ✅ Cerrada el 2026-10-30: el roadmap pasó de 687 a 147
líneas (583 al archivo, con los abiertos listados); D-n de `decisions.md` que apuntaban a secciones
movidas, actualizadas (11).

### E2 · `status.md` en 40 líneas (Desarrollo, donde esté su `status.md` más reciente)

- [ ] "Estado actual" reescrito en 40 líneas como máximo: en curso, lo siguiente de
      Desarrollo y lo que le falta al usuario. Lo cerrado ya está en el plan, `history.md` y
      git: no se copia. "Deuda", "BD de desarrollo del usuario" y "Auditorías" quedan como
      están.
- [ ] Commit solo de `docs/status.md`.

### E3 · Configuración y hooks (Desarrollo, en `TrainingApp`)

- [ ] `.claude/settings.json` (versionado): `CLAUDE_CODE_SUBAGENT_MODEL=sonnet` (D-246).
- [ ] Review en Opus: confirmar cómo elige modelo `/code-review` para sus subagentes. Si
      respeta un agente con `model: opus`, crear `.claude/agents/revisor.md`; si no, la regla
      es `/model opus` antes del review (cuesta una reconstrucción de caché por entrega).
      Informar a Planeación cuál quedó.
- [ ] Averiguar por qué `/code-review` volvió vacío en la E6 de la 022 (y en la E1b "no informó
      qué leyó"): un review que no reporta qué revisó no cuenta como review.
- [ ] Hook `PreToolUse` (Bash y PowerShell) que rechaza `git add -A`, `git add .`,
      `git commit -a` y cualquier commit con `main` como rama actual (D-247). Probar que
      bloquea esos cuatro casos y deja pasar `git commit -m "…" -- <rutas>`.
- [ ] `.gitignore`: la carpeta suelta `_users_*_git/` que aparece en los worktrees.
- [ ] Rebuild no hace falta (no toca código de la app). Commit solo de esas rutas.

**Hecho cuando:** el usuario ve el hook rechazar un `git add -A` de prueba.

### Lo que hace el usuario

- Dar un **nombre único** a cada sesión viva (`/rename`): "Planeación", "Desarrollo",
  "Reuniones"; cerrar o renombrar la "Desarrollo" de Remote Control que está offline.
- En cada sesión, `/model` y `/effort` según D-246; Desarrollo cambia a Sonnet en la E6.
- `/clear` en Planeación al terminar este plan.

## Orden

E1 → E2 y E3 (independientes entre sí). No toca código de la app ni la 022: puede ir entre
entregas de la 022, nunca a la mitad de una.
