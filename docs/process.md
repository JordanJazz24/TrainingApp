# Proceso de trabajo — MESTrainingSimulator

> Cómo se trabaja en este proyecto: acuerdos con el usuario (dev único) que funcionaron.
> Las reglas que aplican siempre están en `CLAUDE.md` → "Reglas de trabajo"; aquí está el
> procedimiento. Cuando el usuario corrija o confirme una forma de trabajar, se anota aquí.

## 1. Clasificar el pedido

| Pedido | Camino |
|---|---|
| Funcionalidad o reglas de negocio nuevas (fases, pasos nuevos, UX con reglas) | `/spec` → revisión y aprobación explícita del usuario ("apruebo", "procede con el plan") → `/plan` → aprobación del plan → implementación. Sin excepciones, aunque haya mucho contexto. |
| Pocas decisiones acotadas (3-6 preguntas, una pantalla) | Una ronda de `AskUserQuestion` + plan directo; las decisiones se anotan en el roadmap. |
| Bugs, robustez, rendimiento | Plan corto en el chat e implementación directa, sin spec. Ofrecer spec solo si aparecen decisiones de negocio. |

- Ante la duda, preguntarse si una spec es sobreingeniería para un simulador interno, y
  decirle al usuario en una línea qué camino se propone y por qué. Él suele pedir "considera
  si realmente necesita spec".
- En una spec, las decisiones que propuso Claude (no las respondió el usuario) se listan
  aparte para que las confirme antes de aprobar.
- **Plan grande** → `docs/specs/NNN-plan.md` (o `fase-N-plan.md`) con casillas, dividido en
  **entregas** chicas en el orden del flujo.
- **Enmiendas:** si durante la implementación aparece un supuesto equivocado en una spec
  aprobada, o el usuario corrige algo al probar, parar, proponer la enmienda y registrarla
  con fecha en la spec ("enmendada AAAA-MM-DD") **antes** de tocar código. Nunca divergir en
  silencio.
- **Decisiones que bloquean** una entrega: se preguntan juntas, numeradas (D-1, D-2…), cada
  una con una propuesta. El usuario responde en bloque. Se anotan en el archivo de la
  auditoría o en la spec. La numeración D-n es continua entre auditorías.
- **Bugs que ya existían**, encontrados en un review o un refactor: no se arreglan en
  silencio. Se presentan como B-1, B-2… con una propuesta y decide el usuario (D-12).

## 2. Cada entrega

1. Implementar.
2. Verificar con los comandos de `CLAUDE.md` → "Correr local, compilar y probar": rebuild,
   `aspnet_compiler` si se tocó markup y tests si cambió la BLL o el Domain. Para una
   verificación completa: `/dotnet-claude-kit:verify` con esos mismos comandos.
3. `/code-review` calibrado (sección 3).
4. Verificar cada hallazgo contra el código antes de aplicarlo. Aplicar lo válido y explicar
   en una línea por qué lo demás no aplica (en la spec 010, 4 de 6 hallazgos `low` eran
   falsos positivos).
5. Checklist por criterio de aceptación para el navegador. Antes de escribirlo, verificar
   qué va a hacer la plataforma: un checklist que asume un comportamiento sin verificarlo
   (ej. "la fila vuelve con el texto literal" cuando ASP.NET bloqueaba el `<`) le hace perder
   una vuelta al usuario.
6. El usuario prueba y confirma ("funciona") → commit `feat|fix|docs|test: … (spec NNN)` con
   la línea `Co-Authored-By` vigente. El roadmap (y la auditoría o el plan, si aplica) se
   actualiza **en el mismo commit**.

**Variantes que usa el usuario:**
- "corrige eso, haz commit y sigamos": hacerlo y avisar qué quedó sin re-verificar, para
  sumarlo al próximo checklist.
- "haz commit y continuemos" sin "funciona": commitear, marcar la entrega como **no
  probada** y pasar su checklist al de la siguiente.
- "funciona haz commit y sigue con EN": commitear y empezar la entrega N.
- Si algo no coincide con lo que dice (ej. pidió "push de todo" y `origin/TrainingApp`
  seguía atrás), avisarle con datos.

## 3. `/code-review` calibrado

Siempre con **nivel, archivos y foco**; nunca sin argumentos (sin nivel reusa el último y
revisa todo el diff: una vez fueron ~16 min y ~220k tokens). Decirle al usuario en una línea
qué nivel, qué archivos y por qué.

| Cambio | Review |
|---|---|
| Solo markup, anclas o textos, o un ajuste de pocas líneas pedido al probar | Ninguno (MSBuild + `aspnet_compiler` + checklist lo cubren) |
| Lógica chica sobre una base ya revisada | `low --max-findings 5`, solo los archivos con lógica, foco explícito |
| Lógica nueva o infraestructura (motor, BLL) | `medium` con foco explícito (qué reglas e invariantes revisar) |
| Seguridad, auth, Session o un cambio grande | `high` o `/security-review` |

## 4. Auditorías

1. **Fase A, solo lectura:** health-check, arch-check, security-scan, `/code-review` por
   carpetas, outdated, código muerto. No se corrige mientras se audita.
2. **Fase B:** lista única en su propio archivo `docs/audits/AAAA-MM-DD-*.md` con severidad,
   vía y entrega. El roadmap solo la referencia.
3. **Fase C:** entregas chicas con el ciclo de la sección 2. Commit con el roadmap y el
   archivo de la auditoría actualizados.

## 5. Tests

Los tests de la BLL con datos en memoria cubren reglas puras (`TrainingApp.Test/`). La Fase 5
blindó `ScoreCalculator`. Fuera de una fase de tests, no proponerlos salvo que el usuario lo
pida: él decidió dejarlos para fases propias.

## 6. Base de datos

- La conexión MCP a SQL Server es de **solo lectura**: los scripts de
  `Database/StoredProcedures/` los ejecuta el usuario, en orden numérico (`00`–`07`).
- No hay scripts de migración. Un cambio de esquema se escribe directo en `00_Schema.sql`, y
  el `ALTER` para la BD existente se le entrega al usuario aparte, indicando qué scripts de
  SPs volver a correr. Después se anota en el roadmap → "BD de desarrollo del usuario".

## 7. Ramas y worktrees

- `TrainingApp` es la rama de desarrollo y la carpeta real
  (`…\MES Training Simulator`) siempre está en ella: ahí apuntan VS, IIS Express y la
  terminal del usuario. A `main` sube los cambios finales el usuario (D-5): no tocarla. El
  usuario hace push cuando quiere.
- Por defecto se trabaja en la carpeta real, con un commit por entrega verificada.
- **Worktrees solo cuando hacen falta** (ej. trabajo aislado en segundo plano):
  - crearlo desde `TrainingApp`: `git worktree add .claude/worktrees/<n> -b <rama> TrainingApp`;
  - copiar `TrainingApp/connectionStrings.config` (está en `.gitignore`) y, para compilar, una
    junction a `packages`;
  - al terminar, desde la carpeta real: `git merge --ff-only <rama>`, `git worktree remove`,
    `git branch -d <rama>` y borrar la rama remota si se subió. Nunca dejar el proyecto activo
    en un worktree ni ramas sueltas (pasó el 2026-10-02 y al usuario no le gustó).
  - En Windows `git worktree remove` puede fallar por rutas largas (`bin\roslyn`): borrar con
    `rmdir /s /q "\\?\<ruta>"` y después `git worktree prune`.

## 8. Pantallas: fidelidad con el SAP MES real

- El simulador replica el SAP MES real: el operador aprende a moverse en cada pantalla y qué
  dato va en cada campo; una UI inventada entrena otra cosa.
- Fuente: capturas que aporta el usuario en `C:\Users\320313439\Pictures\TrainingApp\` (y lo
  que consulta con operadores de planta), más lo ya documentado en `docs/ui-reference/`.
- Antes de maquetar una pantalla, revisar su captura y su sección en `docs/ui-reference/`. No
  inventar estructura, textos ni de dónde sale cada valor (ej. Batch Number = Process Lot de
  la orden, no el barcode; `[250 SELECTED]`, no `[SELECTED 250]`).
- Si una captura contradice algo ya acordado, señalarlo y proponer seguir la captura. Lo que
  se aprenda de cada pantalla se documenta en `docs/ui-reference/`.

## 9. Documentación: qué se actualiza y dónde

| Al… | Actualizar |
|---|---|
| Cerrar una entrega o una sesión de trabajo relevante | `docs/roadmap.md` → "📍 Estado actual" (corto) y la casilla del plan |
| Cerrar una fase | Su sección del roadmap: resultado, commits y fecha |
| Fijar una regla nueva (negocio, convención) | `CLAUDE.md`, en **un solo lugar**; los demás documentos la referencian |
| Cerrar una decisión de diseño o cambiar la estructura del código | `docs/architecture.md` |
| Aprender algo de una pantalla del MES | `docs/ui-reference/` |
| Acordar una forma de trabajar | Este archivo |
| Correr un script de BD (el usuario) | Roadmap → "BD de desarrollo del usuario" |

- No copiar reglas ni tablas (pesos, flags, fórmulas) en otros documentos: enlazar
  `CLAUDE.md → sección`. Las copias se desalinean (pasó con los pesos en `general-flow.md`).
- Specs y auditorías son registro histórico: no se reescriben; se enmiendan con fecha.
- Cuando el usuario cita un plan propuesto en un `/btw` (la respuesta no queda guardada),
  reconstruirlo, anotarlo marcado como reconstrucción y pedirle que lo corrija.
