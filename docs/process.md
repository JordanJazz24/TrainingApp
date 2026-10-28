# Proceso de trabajo — MESTrainingSimulator

> Cómo se trabaja en este proyecto: acuerdos con el usuario (dev único) que funcionaron.
> Las reglas que aplican siempre están en `CLAUDE.md` → "Reglas de trabajo"; aquí está el
> procedimiento. Cuando el usuario corrija o confirme una forma de trabajar, se anota aquí.

## 1. Clasificar el pedido

El camino no depende del tipo de trabajo, sino de **cuánto cuesta equivocarse** y de
**cuántas decisiones quedan abiertas** (usuario, 2026-10-26). Las preguntas se hacen en orden
y **gana la primera que aplica**:

| # | Pedido | Camino |
|---|---|---|
| 1 | Funcionalidad o reglas de negocio nuevas: cambia el flujo, los pasos, el puntaje, los datos o los roles (fases, pasos nuevos, UX con reglas) | `/spec` → revisión y aprobación explícita del usuario ("apruebo", "procede con el plan") → `/plan` → aprobación del plan → implementación. Sin excepciones, aunque haya mucho contexto. Spec y plan, en la sesión de Arquitectura y Planeación; la implementación, en la de Desarrollo (ver "Tres sesiones de trabajo"). Es lo más caro de revertir: enseña mal o falsea la evaluación, y deja datos en la BD. |
| 2 | Pantallas que replican el SAP MES real (POD, Ingreso a MES) | Plan directo (o por entregas si son varias pantallas, como la Fase 6.5) con la captura real como criterio de aceptación (sección 8). La fidelidad es una regla disfrazada de UI: no se diseña, se replica. |
| 3 | Solo presentación (sin reglas ni datos nuevos), pero en varias pantallas o sin una intención de UX definida | **Brief de diseño**: una `/spec` ligera con las preguntas de UX de abajo, aprobada antes del plan. Sin él, cada pantalla queda distinta y se rehace al revisar. |
| 4 | Pocas decisiones acotadas (3-6 preguntas, una pantalla), incluidos textos de la guía y presentación de una sola pantalla | Una ronda de `AskUserQuestion` + plan directo; las decisiones se anotan en la sección de la tarea en el roadmap, con su fila en `docs/decisions.md`. |
| 5 | Bugs, robustez, rendimiento, deuda sin cambio de comportamiento | Plan corto en el chat e implementación directa, sin spec. La conducta correcta ya está definida. |

- **Escalar, nunca bajar en silencio:** si en un camino ligero aparece algo de la fila 1
  (una decisión de negocio, una regla nueva), se para y se propone pasar a spec.
- **Brief de diseño** (fila 3): se escribe con `/spec` en `docs/specs/`, pero pregunta por la
  intención de UX, no por el comportamiento: quién mira cada pantalla y qué decide con ella;
  qué tiene que ver primero; referencias visuales y restricciones (Bootstrap, marca, uso en
  planta); **primero el sistema de diseño** (tokens de color, tipografía y espaciado, y
  componentes compartidos en un CSS común) y después las pantallas; y criterios de aceptación
  verificables (capturas de antes y después, contraste accesible, anclas `data-guide` y
  postbacks intactos, los `.xlsx` abren bien en Excel).
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
  una con opciones y una **recomendada**, con la herramienta de preguntas (`AskUserQuestion`),
  no como texto para responder en bloque (usuario, 2026-10-14). Se anotan en el archivo de la
  auditoría, la spec o el plan (en un plan directo, en la sección de la tarea en el
  roadmap). La numeración D-n es continua entre todos ellos: la siguiente está en el
  encabezado de `docs/decisions.md`, y la fila se agrega en el mismo commit que la fuente.
- **Bugs que ya existían**, encontrados en un review o un refactor: no se arreglan en
  silencio. Se presentan como B-n con una propuesta y decide el usuario (D-12). Desde el
  2026-10-21 la numeración es global, como la de D-n (antes había un B-1 por contexto), y
  cada uno lleva su fila en `docs/decisions.md`.

### Tres sesiones de trabajo (usuario, 2026-10-14)

El usuario trabaja con tres sesiones de Claude en paralelo, cada una con un foco. Se llaman
así entre ellas al mandarse mensajes ("Reuniones y dudas", la de arquitectura y
planeación, "desarrollo").

| Sesión | Para qué se usa | Hace | No hace |
|---|---|---|---|
| **Reuniones y Dudas** | Documentar notas de reuniones, aclaraciones funcionales, decisiones de negocio, validaciones con usuarios expertos y descubrimientos del proceso | Registra lo que se habló y lo que decidió el usuario; lo manda a Arquitectura y Planeación para que lo lleve a specs y roadmap; puede documentar pantallas en `docs/ui-reference/` | Specs, planes ni código |
| **Arquitectura y Planeación** | Analizar alternativas, diseñar soluciones, definir arquitectura, modelar datos, evaluar trade-offs y planificar funcionalidades o fases | `/spec`, enmiendas de spec y **plan por entregas** (`docs/specs/NNN-plan.md`), con la aprobación del usuario; orden de tareas y secciones de fases y planes del roadmap; decisiones de diseño | Código |
| **Desarrollo** | Solo la implementación: construir lo ya definido y aprobado, sin desviar la conversación hacia negocio o diseño | Ejecuta el plan aprobado tal cual, entrega por entrega (sección 2): implementar, verificar, `/code-review`, checklist, commit con el "funciona" del usuario; al cerrar, `docs/status.md`, `docs/history.md`, `CLAUDE.md`, `docs/domain.md` y `docs/architecture.md` | Specs ni planes; discusiones de negocio o diseño (las deriva) |

- El traspaso es el repo: lo que sale de Reuniones y Dudas, en el roadmap y
  `docs/ui-reference/`; la spec y el plan aprobados, en `docs/specs/`. Además, las sesiones
  se avisan por mensaje (hallazgo de reunión, spec o plan aprobado, entrega cerrada, algo
  que no cierra).
- Si al ejecutar algo de la spec o del plan no cierra, Desarrollo avisa a Arquitectura y
  Planeación, que propone la enmienda al usuario y la registra **antes** de que se toque
  código. Las decisiones menores que no cambian ni la spec ni el plan las toma Desarrollo con
  el usuario y las informa.
- **Commits sin pisarse (usuario, 2026-10-21):** las sesiones trabajan en la carpeta real
  (sección 7) y cada una commitea **solo sus rutas**: `git commit -m "…" -- <rutas>`, nunca
  `git add -A`, `git add .` ni `git commit -a`, que se llevarían el trabajo sin commitear de
  otra sesión. Si dos sesiones tienen que tocar el mismo archivo, la segunda espera el commit
  de la primera (se avisan por mensaje). Reemplaza el worktree de docs obligatorio de
  Arquitectura y Planeación: dejaba ramas sueltas y contenido sin integrar (al 2026-10-21,
  6 worktrees de docs y el hallazgo W23 olvidado en su rama desde el 2026-10-15).
- **Un dueño por archivo de estado (usuario, 2026-10-21):** Arquitectura y Planeación
  escribe `docs/roadmap.md` (fases, alcance, planes, revisiones con negocio y orden de
  tareas); Desarrollo, `docs/status.md` (estado, deuda, BD del usuario y auditorías) y
  `docs/history.md`. Antes eran secciones de un solo roadmap y las dos sesiones editaban el
  mismo archivo. Cuando Desarrollo cierra una fase, le avisa a Arquitectura y Planeación para
  que la marque ✅ en la tabla de fases.

#### Variante temporal: specs y planes en la nube (usuario, 2026-10-27)

**Temporal:** cuando el usuario lo pida, se vuelve al flujo normal (specs y planes en la
sesión local de Arquitectura y Planeación) y esta subsección se quita.

- **Remotes:** `azure` es el repo oficial (Azure DevOps, `SimulatorMES`); `origin` es GitHub
  (`JordanJazz24/TrainingApp`, privado, con la Claude GitHub App), el puente que ve la nube.
  La nube solo ve lo que está pusheado a `origin`.
- **La nube hace de Arquitectura y Planeación** para las specs y los planes que el usuario le
  asigne: `/spec` y plan por entregas, con las mismas reglas (sección 1). Trabaja en la rama
  `cloud/NNN-slug` y hace push **solo a esa rama**, nunca a `TrainingApp` ni a `main`. No
  compila ni prueba (VM Linux: sin .NET Framework, `aspnet_compiler`, vstest ni SQL Server):
  a lo sumo propone cambios de BD o BLL con tests que corre el usuario.
- **Integrar en local** (carpeta real, rama `TrainingApp`):
  `git fetch origin` → `git merge origin/cloud/NNN-slug` → `git push azure TrainingApp` y
  `git push origin TrainingApp`, para que la nube vea lo último.
- **Avisos:** una sesión en la nube recibe mensajes pero no puede contestar ni avisar; el
  usuario hace de mensajero. Cuando la spec o el plan entran en `TrainingApp`, el usuario le
  pide a Desarrollo la E1, con su ciclo normal (sección 2).
- **La sesión local de Arquitectura y Planeación sigue siendo dueña de `docs/roadmap.md`**:
  revisa el alcance de lo que llega de la nube contra la fase en curso, registra la fase, el
  plan y el orden, y no duplica la spec.
- **Enmiendas:** en la nube (el usuario le pega el problema) o en la sesión local, si la de la
  nube ya terminó.
- **Sin diff antes de cada commit** (usuario, 2026-10-27): la nube no le muestra el diff de los
  docs al usuario antes de commitear; la sesión local revisa la rama al integrarla.
- **Numeración:** cada frente usa su rango de D-n y B-n (`docs/decisions.md` → encabezado). El
  2026-10-27 la nube y la sesión local tomaron D-202 a la vez.
- **Handoff de Claude Design (spec 022, D-226; usuario, 2026-10-28):** el usuario aprueba un
  mockup en Claude Design y lo manda, con el "Prompt de handoff" de `022-briefs.md`, a una
  sesión de la nube que lo implementa en `cloud/022-<pantalla>`. El commit va marcado "no
  verificada" y la rama trae `docs/specs/022-design/<pantalla>.md`. Esa sesión no numera
  decisiones ni toca docs de estado. La nube de planeación de la 022 (`Diseño UI/UX`) registra
  las D-n y enmienda la spec y el plan. **Desarrollo** trae la rama a su entrega como punto de
  partida, con el ciclo normal (sección 2): Rebuild, `aspnet_compiler`, `/code-review`, el
  "funciona" del usuario y el commit en `TrainingApp`. Nunca se integra tal cual. La sesión
  local de Arquitectura y Planeación integra solo las ramas de docs (`cloud/NNN-slug`). Las
  capturas de `022-design/` las toma Desarrollo, solo de la versión final aprobada (claro,
  oscuro y tablet); si su contexto no alcanza, pregunta antes al usuario (D-240).

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
   la línea `Co-Authored-By` vigente. `docs/status.md` (y la auditoría o el plan, si aplica)
   se actualiza **en el mismo commit**.

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
- en el motor del POD (cada operador con su propia sesión): carga, concurrencia entre muchos
  usuarios y rendimiento a escala. **No aplica al dashboard** (Fase 10) ni a nada que lea el
  historial de todos los operadores: ese volumen crece con cada intento, así que ahí el
  rendimiento sí se revisa (usuario, 2026-10-21).

Siguen en alcance siempre: las invariantes del servidor (progreso, puntaje, paso actual,
dueño de la sesión, intento y avance en la misma transacción) y lo que cruza usuarios. En
auth, Session, roles y gestión de usuarios no se descarta nada por este criterio.

## 4. Auditorías

1. **Fase A, solo lectura:** health-check, arch-check, security-scan, `/code-review` por
   carpetas, outdated, código muerto. No se corrige mientras se audita.
2. **Fase B:** lista única en su propio archivo `docs/audits/AAAA-MM-DD-*.md` con severidad,
   vía y entrega. `docs/status.md` → "Auditorías" solo la referencia.
3. **Fase C:** entregas chicas con el ciclo de la sección 2. Commit con `docs/status.md` y el
   archivo de la auditoría actualizados.

## 5. Tests

Los tests de la BLL con datos en memoria cubren reglas puras (`TrainingApp.Test/`).
- **Las reglas nuevas o cambiadas de la BLL llevan sus tests en la misma spec o plan**
  (usuario, 2026-10-21; así se hizo de la spec 013 a la 017: 206 tests al 2026-10-21).
- La cobertura de lo que ya existe va en fases propias (la Fase 5 blindó `ScoreCalculator`;
  `PodService` contra LocalDB sigue en la deuda): no proponerla fuera de su fase salvo que el
  usuario lo pida.

## 6. Base de datos

- La conexión MCP a SQL Server es de **solo lectura**: los scripts de
  `Database/StoredProcedures/` los ejecuta el usuario, en orden numérico (`00`–`09`; índice
  en el encabezado de `00_Schema.sql`). Todos se pueden volver a correr sobre su BD.
  `Database/Ops/RunAllScripts.ps1` los corre todos en orden y se detiene en el primero que
  falla; con `-Recreate` borra y crea la BD antes (pide confirmación).
- Antes de entregarlos, probarlos en una BD descartable de LocalDB
  (`(localdb)\MSSQLLocalDB`, con `sqlcmd -I -f 65001`): instalación limpia, re-ejecución
  sobre una BD con sesiones e intentos, y comparación de datos contra la versión anterior.
  Al terminar, borrar esa BD.
- **Esquema:** no hay scripts de migración. Un cambio se escribe directo en `00_Schema.sql`
  y el `ALTER` para la BD existente se entrega aparte, indicando qué otros scripts volver a
  correr.
- **BD de pruebas (usuario, 2026-10-19):** mientras la BD del usuario sea de pruebas, se
  puede borrar y volver a crear con `00`–`09`. Un cambio de esquema o de guía no necesita
  `ALTER` aparte ni cuidar las sesiones en curso: basta indicar que se recrea la BD. Cuando
  haya una BD con historial que conservar (el servidor real, D-19), vuelve la regla del
  `ALTER`.
- **BD del servidor del MVP (D-130):** también es de pruebas hasta la versión final de
  noviembre, que instala una BD limpia. El usuario corre en ella los scripts nuevos o la
  recrea con `RunAllScripts.ps1 -Recreate` + `CreateAdmin.sql` (`docs/deploy.md` → 2 y 7, desde
  el 2026-10-22 lo hace él y no TI); no hace falta `ALTER`.
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
- Después de que el usuario corre un script, anotarlo en `docs/status.md` → "BD de desarrollo del
  usuario".

## 7. Ramas y worktrees

- `TrainingApp` es la rama de desarrollo y la carpeta real
  (`…\MES Training Simulator`) siempre está en ella: ahí apuntan VS, IIS Express y la
  terminal del usuario. A `main` sube los cambios finales el usuario (D-5): no tocarla. El
  usuario hace push cuando quiere. **Antes de commitear en la carpeta real, revisar la rama**
  (`git branch --show-current`): el 2026-10-27 quedó en `main` y un commit de docs entró ahí.
- **Se trabaja en una rama y su worktree, y se integra a `TrainingApp` cuando el usuario lo
  aprueba** (usuario, 2026-10-22: "primero trabajamos en la branch o worktree y luego de
  aprobado se integra"). Así la carpeta real queda libre para mostrar o probar otra cosa. Un
  commit por entrega y solo con las rutas propias (sección 1 → "Commits sin pisarse"). La
  rama y el worktree siguen vivos mientras dure su fase.
- Cómo armar el worktree:
  - crearlo desde `TrainingApp`: `git worktree add .claude/worktrees/<n> -b <rama> TrainingApp`;
  - copiar `TrainingApp/connectionStrings.config` (está en `.gitignore`) y, para compilar, una
    junction a `TrainingApp/packages`; si la entrega agrega o actualiza paquetes, una copia
    propia en vez de la junction (así no se meten versiones nuevas en la carpeta real), y para
    probar, una BD propia de LocalDB;
  - integrar, desde la carpeta real: `git merge --ff-only <rama>`;
  - al terminar la fase, desde la carpeta real: `git merge --ff-only <rama>`, `git worktree remove`,
    `git branch -d <rama>` y borrar la rama remota si se subió. Nunca dejar el proyecto activo
    en un worktree ni ramas sueltas (pasó el 2026-10-02 y al usuario no le gustó).
  - **Un job en segundo plano no puede integrar su rama:** la herramienta no le deja usar git
    fuera de su worktree (2026-10-21). Deja sus commits sobre `TrainingApp` y le pasa al
    usuario los comandos para la carpeta real. Una rama que entró por cherry-pick se borra con
    `-D`, después de comprobar con `git cherry` o `git range-diff` que su contenido ya está.
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
| Cerrar una entrega o una sesión de trabajo relevante | `docs/status.md` → "📍 Estado actual" (corto: lo cerrado se resume ahí en una línea) y la casilla del plan |
| Cerrar una fase, una spec o un plan directo | Su sección en `docs/history.md`: resultado, commits y fecha (Desarrollo); la tabla de fases del roadmap (Arquitectura y Planeación) |
| Fijar una regla de negocio del dominio (flujo, intentos, pesos, flags, catálogos) | `docs/domain.md`, en **un solo lugar**; los demás documentos la referencian |
| Fijar una regla general (capas, seguridad, convención, comando) | `CLAUDE.md`, en **un solo lugar**. Mantenerlo corto: se carga en cada turno; lo que solo hace falta para cierta tarea va a un doc con su puntero en el mapa |
| Cerrar una decisión de diseño o cambiar la estructura del código | `docs/architecture.md` |
| Aprender algo de una pantalla del MES | `docs/ui-reference/` |
| Acordar una forma de trabajar | Este archivo |
| Correr un script de BD (el usuario) | `docs/status.md` → "BD de desarrollo del usuario" |

- No copiar reglas ni tablas (pesos, flags, fórmulas) en otros documentos: enlazar
  `CLAUDE.md → sección` o `docs/domain.md → sección`. Las copias se desalinean (pasó con los
  pesos en `general-flow.md`).
- Specs y auditorías son registro histórico: no se reescriben; se enmiendan con fecha.
- Cuando el usuario cita un plan propuesto en un `/btw` (la respuesta no queda guardada),
  reconstruirlo, anotarlo marcado como reconstrucción y pedirle que lo corrija.
