# Proceso de trabajo — MESTrainingSimulator

> Cómo se trabaja en este proyecto: acuerdos con el usuario (dev único) que funcionaron.
> Las reglas que aplican siempre están en `CLAUDE.md` → "Reglas de trabajo"; aquí está el
> procedimiento. Cuando el usuario corrija o confirme una forma de trabajar, se anota aquí.

## 1. Clasificar el pedido

| Pedido | Camino |
|---|---|
| Funcionalidad o reglas de negocio nuevas (fases, pasos nuevos, UX con reglas) | `/spec` → revisión y aprobación explícita del usuario ("apruebo", "procede con el plan") → `/plan` → aprobación del plan → implementación. Sin excepciones, aunque haya mucho contexto. Spec y plan, en la sesión de diseño; la implementación, en la de desarrollo (ver "Dos sesiones"). |
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

### Dos sesiones: diseño y desarrollo (usuario, 2026-10-14)

| Sesión | Hace | No hace |
|---|---|---|
| **Diseño** (arquitectura y planificación) | `/spec`, enmiendas de spec y **plan por entregas** (`docs/specs/NNN-plan.md`), con la aprobación del usuario; secciones de fases y planes del roadmap; decisiones de diseño | Código |
| **Desarrollo** | Ejecuta el plan aprobado tal cual, entrega por entrega (sección 2): implementar, verificar, `/code-review`, checklist, commit con el "funciona" del usuario; al cerrar, "Estado actual", "Historial", la fila de la BD del usuario, `CLAUDE.md`, `docs/domain.md` y `docs/architecture.md` | Specs ni planes |

- El traspaso es el repo: la spec y el plan aprobados en `docs/specs/`. Además, las sesiones
  se avisan por mensaje (spec o plan aprobado, entrega cerrada, algo que no cierra).
- Si al ejecutar algo de la spec o del plan no cierra, desarrollo avisa a diseño, que
  propone la enmienda al usuario y la registra **antes** de que se toque código. Las
  decisiones menores que no cambian ni la spec ni el plan las toma desarrollo con el usuario
  y las informa.
- Cada sesión commitea los docs que le tocan; diseño trabaja en un worktree de docs y hace
  `merge --ff-only` en la carpeta real (sección 7) sin tocar el trabajo sin commitear de
  desarrollo.

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

**Riesgo proporcional (usuario, 2026-10-14).** El foco del review incluye el modelo de
amenaza: un simulador interno de entrenamiento, con operadores autenticados y sin
incentivo para atacarlo. Pedirle al revisor que descarte, y al verificar hallazgos
descartar en una línea:
- escenarios que necesitan HTML o JS manipulado, dos pestañas, doble click o postbacks
  repetidos, cuando la BLL ya protege la invariante y el efecto queda en la UI del propio
  usuario;
- carga, concurrencia entre muchos usuarios y rendimiento a escala.

Siguen en alcance siempre: las invariantes del servidor (progreso, puntaje, paso actual,
dueño de la sesión, intento y avance en la misma transacción) y lo que cruza usuarios. En
auth, Session, roles y gestión de usuarios no se descarta nada por este criterio.

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
  `Database/StoredProcedures/` los ejecuta el usuario, en orden numérico (`00`–`09`; índice
  en el encabezado de `00_Schema.sql`). Todos se pueden volver a correr sobre su BD.
- Antes de entregarlos, probarlos en una BD descartable de LocalDB
  (`(localdb)\MSSQLLocalDB`, con `sqlcmd -I -f 65001`): instalación limpia, re-ejecución
  sobre una BD con sesiones e intentos, y comparación de datos contra la versión anterior.
  Al terminar, borrar esa BD.
- **Esquema:** no hay scripts de migración. Un cambio se escribe directo en `00_Schema.sql`
  y el `ALTER` para la BD existente se entrega aparte, indicando qué otros scripts volver a
  correr.
- **SPs:** `CREATE OR ALTER`. Un SP que se elimina se borra de su script y se entrega un
  `DROP` aparte (los scripts no arrastran borrados viejos).
- **Datos:** cada script corre en una transacción (`SET XACT_ABORT ON`): si falla, no deja
  nada. El script es la fuente de verdad de su catálogo: cada tabla se declara una vez como
  tabla de valores, se actualiza lo que existe y se inserta lo que falta en el orden del
  script (así los Ids no cambian). Nunca borra filas: las sesiones y los intentos las
  referencian. Excepción (spec 015, D-23): los parámetros de DC sobrantes de un grupo
  reemplazado, porque nada referencia grupos ni parámetros de DC. Textos siempre con `N'…'`
  (sin la `N`, "Ω" se guardaba como "O").
- **Guías:** se cargan con `spTrainingGuideSync` (en `02`): crea la guía si falta, actualiza
  los pesos y aborta si no suman 1.00 o si la estructura de una guía existente cambió (eso
  se resuelve a mano).
- **Área nueva** (ej. LoFlo): un script de catálogo y uno de guía con la plantilla de `06` y
  `07`. Un área de ejemplo de `05` que pase a tener catálogo real se deja de tocar en `05`.
- Después de que el usuario corre un script, anotarlo en el roadmap → "BD de desarrollo del
  usuario".

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
- Fuente: capturas que aporta el usuario en `docs/ui-reference/Screens SAP MES REAL/` (y lo
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
| Fijar una regla de negocio del dominio (flujo, intentos, pesos, flags, catálogos) | `docs/domain.md`, en **un solo lugar**; los demás documentos la referencian |
| Fijar una regla general (capas, seguridad, convención, comando) | `CLAUDE.md`, en **un solo lugar**. Mantenerlo corto: se carga en cada turno; lo que solo hace falta para cierta tarea va a un doc con su puntero en el mapa |
| Cerrar una decisión de diseño o cambiar la estructura del código | `docs/architecture.md` |
| Aprender algo de una pantalla del MES | `docs/ui-reference/` |
| Acordar una forma de trabajar | Este archivo |
| Correr un script de BD (el usuario) | Roadmap → "BD de desarrollo del usuario" |

- No copiar reglas ni tablas (pesos, flags, fórmulas) en otros documentos: enlazar
  `CLAUDE.md → sección` o `docs/domain.md → sección`. Las copias se desalinean (pasó con los
  pesos en `general-flow.md`).
- Specs y auditorías son registro histórico: no se reescriben; se enmiendan con fecha.
- Cuando el usuario cita un plan propuesto en un `/btw` (la respuesta no queda guardada),
  reconstruirlo, anotarlo marcado como reconstrucción y pedirle que lo corrija.
