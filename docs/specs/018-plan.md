# Plan de la spec 018 — Dashboard de Training (Fase 10)

**Status:** **aprobado por el usuario (2026-10-22)** · spec `018-dashboard-training.md`,
aprobada el 2026-10-22 (D-136 a D-142).

Cada entrega se cierra como dice `docs/process.md` §2:
1. verificar (rebuild, `aspnet_compiler` si se toca markup, tests si cambia la BLL);
2. `/code-review` calibrado;
3. checklist en el navegador;
4. "funciona" del usuario;
5. commit con `docs/status.md`.

Las entregas siguen el flujo de valor: primero lo que responde "¿está listo?" y después lo
demás. La E8 es lo que se recorta si falta tiempo (spec → "Recorte").

**Antes de la E1 va la E2 de la Fase 9** (`fase-9-plan.md`: AUD-28 barato, parches de AUD-29
y el comentario de `Web.config`). Así lo decidió el usuario el 2026-10-22, dentro de D-135:
- toca el login y la Session, que es por donde entra el admin al dashboard, así que hecha
  primero no obliga a volver a probar el dashboard;
- el MVP se republica endurecido antes.

**Decisiones del plan que tomó Arquitectura y Planeación, confirmadas por el usuario el
2026-10-22:**
- el orden de las entregas, con Operadores primero;
- la E2 de la Fase 9 antes de la E1;
- "pasos a reforzar" = el top 5 de sus Assessment;
- un script nuevo, `10_spDashboard.sql`;
- los filtros en la URL;
- el generador de datos en la E1, con resguardo;
- las descargas por GET con `Export.ashx`;
- la curva y la ficha al final;
- los nombres de archivo en inglés, como los existentes (`Users.aspx`).

> **Enmienda 1 · 2026-10-22 (usuario, con el desvío que avisó Desarrollo al hacer la E1;
> D-143):** la lista de operadores **no se pagina en el SP**.
> - `spDashboardOperators` devuelve todos los que cumplen la búsqueda, y la BLL calcula el
>   estado, filtra por estado y guía y pagina de a 50. Sigue siendo "paginada en el servidor".
> - **Por qué:** el estado lo decide `ReadinessPolicy` en la BLL (D-139). Si el SP paginara,
>   el filtro de estado solo vería los 50 de la página y el total saldría mal. Paginar en SQL
>   obligaría a repetir la regla de Aprobado en SQL.
> - **Medido:** 150-250 ms con 1600 operadores.
> - **Plan B, también aceptado:** si la E7 no da los 3 s, el SP agrega por operador, guía,
>   modo y estado, sin reglas.
>
> **Enmienda 2 · 2026-10-22 (usuario; B-3):** la E2 suma el arreglo de B-3, un bug que ya
> existía. Cuando la BLL niega el acceso, `Users.aspx` (spec 013) manda al login, en vez de
> mandar al inicio del rol (`SessionHelper.RedirectHome`), como piden `CLAUDE.md` → Auth y
> AC-2. `Operators.aspx` ya lo hace bien.
>
> **Enmienda 3 · 2026-10-23 (usuario, al cerrar la E2; D-146):** el SP de Pasos (E3) **no
> agrega**: devuelve los intentos.
> - **Qué cambia:** el SP devuelve los intentos del período, la guía y el modo, con su sesión,
>   paso, resultado, motivo, acción y hora, y el `StartedAt` y el estado de la sesión. Es la
>   versión por período, guía y modo de `spDashboardOperatorAttempts`, con los mismos filtros
>   de rol y de guía publicada. `DashboardService.GetSteps` arma todo con `StepRuns`: sesiones
>   con fallo, intentos fallidos, veces sin acierto, saltos, motivos y la mediana.
> - **Por qué:** "terminó sin acierto" depende del modo (avance forzado en el 4.º fallo, el
>   último paso de una sesión abandonada, el salto con fallos; `ModePolicy`) y ya vive en
>   `StepRuns` desde la E2. Agregarlo en el SP repetiría la regla en SQL (D-139). Además, la
>   mediana necesita la duración de cada paso (DA-2), que sale de los intentos uno por uno.
>   Así la misma regla sirve para el detalle, Pasos, el top 5 del Resumen (E4), S-3 (E5) y el
>   Excel (E6).
> - **Plan B, también aceptado:** si en la E7 Pasos con "Todo" no responde en menos de 3 s, el
>   SP baja a una fila por sesión y paso (fallos, saltos, si hubo acierto, la hora del último
>   intento y los conteos por motivo y acción), sin reglas, y `StepRuns` parte de esas filas.

> **Enmienda 4 · 2026-10-23 (usuario, con un hallazgo del `/code-review` de la E7; D-149):**
> la descarga de sesiones del Resumen se arma con **`System.IO.Compression.ZipArchive`** y las
> hojas en XML directo (`XmlWriter`), **sin `System.IO.Packaging`** (el SDK de OpenXML).
> - **Por qué:** en .NET Framework, el `ZipPackage` de `System.IO.Packaging` no escribe en
>   streaming. Pasados ~10 MB por parte, la guarda en IsolatedStorage del usuario, y en un App
>   Pool de IIS sin perfil cargado eso puede fallar ("Unable to determine application identity
>   of the caller"). En IIS Express no se ve. La hoja Intentos por paso con "Todo" pasa de
>   10 MB, y además el archivo no quedaba de verdad en streaming, que era el motivo de D-148.
> - **Cómo se cubre que sea código propio:** un test valida el archivo contra el esquema de
>   SpreadsheetML (`OpenXmlValidator` del SDK, solo en `TrainingApp.Test`), y otros lo leen con
>   ClosedXML.
> - **Medido** (1600 operadores, un año): con 30 días, 1,3 s; con "Todo", 4,7 s. Siguen la
>   excepción de AC-24 con "Todo" y su aviso (D-148).
> - Las otras descargas (Operadores, Pasos, Seguimiento) siguen con ClosedXML: son archivos
>   chicos, lejos de los 10 MB.
> - Descartado: volver al SDK y probar en el servidor real, con `LoadUserProfile=true` en el App
>   Pool (a cargo de TI) si fallaba.

## Diseño común (lo usan todas las entregas)

- **Capas** (`CLAUDE.md`), de abajo hacia arriba:
  - **Domain:** POCOs de lectura: filas de sesión con sus conteos y filas de paso.
  - **DAL:** `IDashboardRepository` y `DashboardRepository` llaman SPs nuevos de solo
    lectura que **agregan sin reglas** (D-139).
  - **BLL:** `BLL/Reports/` con `ReadinessPolicy`, las reglas de seguimiento y
    `DashboardService`.
  - **Pages:** `Pages/Admin/`.
- **`DashboardService`:**
  - tiene un constructor con `IDashboardRepository` inyectable, como `UserAdminService`, y
    así las reglas se prueban con datos en memoria, sin BD;
  - cada método recibe el `userId` del admin y verifica `CanViewReports`, que es una
    excepción si falta.
- **`ReadinessPolicy`**, estática como `AccessPolicy`, es el único lugar con la regla de
  Aprobado, Reprobado y En entrenamiento (D-136). Las tasas, el seguimiento, el Excel y la
  ficha le preguntan.
- **SPs:** en un script nuevo, `Database/StoredProcedures/10_spDashboard.sql`, con
  `CREATE OR ALTER`. Los índices van en `00_Schema.sql`. La BD se recrea con
  `RunAllScripts.ps1 -Recreate`, sin `ALTER` (D-130).
- **Filtros:** cada página guarda los suyos (período, guía, modo, página) en la URL
  (`?from=…&guide=…`), así un enlace, F5 o Atrás conservan la vista. Se validan en el
  servidor: un valor inválido vuelve al valor de inicio. Nada se escribe en `Session`.
- **Horas:** `SessionHelper.ToLocal`, formato `dd/MM/yyyy HH:mm`.
- **Nombres de paso:** `StepNames.Display`.
- **Sin datos:** cada bloque muestra "Sin datos para este período", y una tasa sin
  denominador muestra "—".

## E1 · Cimientos, Operadores y verificación de las librerías

> **E1 de Desarrollo ✅** (2026-10-22, `a4ffefb`; se integró a `TrainingApp` antes de probarse
> y el usuario la probó después: "funciona").
> - **Desvío aceptado por el usuario (2026-10-22, D-143, lo registra Arquitectura y
>   Planeación como enmienda 1):** el SP de
>   operadores devuelve todos los que cumplen la búsqueda, **sin paginar**, y la BLL filtra por
>   estado y guía y pagina de a 50. El estado lo decide `ReadinessPolicy` (D-139); si el SP
>   paginara, el filtro de estado solo vería los 50 de la página. Con 1600 operadores la página
>   responde en unos 200 ms. Si la E7 no da los 3 s, el plan B es que el SP agregue por
>   operador, guía, modo y estado, que sigue sin reglas.
> - **ClosedXML ✅ (D-140):** 0.105.1 con sus 11 dependencias por `packages.config` (web y
>   tests; 4 `bindingRedirect` de `System.*` en `Web.config`). Un test genera un `.xlsx` con dos
>   hojas, acentos, fecha y %, y Excel lo abre sin avisos. El primer `AdjustToContents` tarda
>   unos 20 s (SixLabors.Fonts escanea las fuentes del sistema): se resuelve en la E6.
> - **Chart.js 4.5.1** (la 4.6.1 salió hoy): `Scripts/chart-4.5.1.umd.min.js`. `Admin.Master`
>   tiene un `ScriptContent` al final para que cada vista con gráficas lo cargue; el login no lo
>   descarga.
> - El menú muestra solo las vistas que ya existen: en la E1, Operadores y Usuarios.
> - `spDashboardSessions` cuenta los intentos fallidos solo con `@IncludeFailedAttempts = 1`:
>   contarlos para todos los operadores tarda ~1,5 s y el estado no los necesita.
>   `IX_StepAttempts_Session` suma `INCLUDE (IsCorrect, Reason)`, y `00` lo recrea en una BD
>   que lo tenga sin él.
> - Los filtros de Operadores: con solo el estado, el operador lo tiene en alguna guía (Sin
>   iniciar = ninguna sesión); con solo la guía, la entrenó; con los dos, su estado en esa guía.
> - **Generador:** con los valores por defecto, 1600 operadores (~10 % sin iniciar), ~22 000
>   sesiones y ~285 000 intentos en unos 40 s. A lo más una sesión en curso por operador (D-9),
>   y `CompletedAt` en las abandonadas, como la app.
> - Del `/code-review medium` (8 hallazgos) se aplicaron 7 (vuelta al inicio y no al login si
>   la BLL niega el acceso, recrear el índice sin `INCLUDE`, `@Pattern` que no se trunca, la
>   página usa los filtros que aplicó el servicio, y en el generador: `CompletedAt`, una sesión
>   en curso y el overflow de `ABS(CHECKSUM())`). Se descartó cargar todas las sesiones en cada
>   búsqueda: responde en ~200 ms y la E7 es la que mide. `Users.aspx` tiene el mismo patrón de
>   mandar al login cuando la BLL niega el acceso (spec 013): no se tocó.

- [x] **`AccessPolicy.CanViewReports`**, solo para Administrator, con su test.
- [x] **Menú en `Admin.Master`:** Resumen · Operadores · Pasos · Seguimiento · Usuarios.
  - Solo se ve con un usuario con `CanViewReports`: el mismo master lo usan el login y el
    cambio de contraseña.
  - Las vistas que todavía no existen no se muestran.
- [x] **Índices** en `00_Schema.sql`: sesiones por `(UserId, StartedAt)` y por
  `(TrainingGuideId, Mode, StartedAt)`, con `INCLUDE` de estado y puntaje.
- [x] **SPs** (`10_spDashboard.sql`):
  - las sesiones por operador y guía publicada, con el modo, el estado, el puntaje, las
    fechas y el conteo de intentos fallidos;
  - la lista paginada de operadores, con búsqueda por nombre o Code1, que incluya a los que
    no tienen sesiones.
- [x] **`ReadinessPolicy`** y **`DashboardService.SearchOperators`**:
  - el estado por guía, el general **Sin iniciar**, el mejor y el último puntaje y la
    última actividad;
  - filtro por estado y guía y 50 por página.
- [x] **Página `Operators.aspx`** (AC-7, AC-13), con su enlace al detalle. La página de
  detalle se arma en la E2.
- [x] **Aviso al borrar** (D-142, AC-23): `Users.aspx.cs` suma al texto actual, que ya dice
  cuántas sesiones se pierden, que "los indicadores del dashboard cambian".
- [x] **Generador de datos de prueba**, `Database/Ops/GenerateDashboardData.sql`:
  - solo para una BD **descartable** de LocalDB: se niega a correr si la BD no se llama
    `TrainingApp_DashboardTest`;
  - se usa desde la E1 para ver cada vista con datos realistas, y en la E7 para medir;
  - genera unos **1600 operadores y 12 meses** de sesiones de las tres guías, con un reparto
    realista: aprobados, reprobados, abandonos y fallos concentrados en algunos pasos;
  - parámetros de cantidad arriba del script.
- [x] **Verificar ClosedXML** (D-140): instalarlo con `packages.config` en .NET FW 4.7.2,
  compilar, generar un `.xlsx` de prueba con un test y abrirlo en Excel.
  - **Si falla: parar y avisar** a Arquitectura y Planeación, que lo pasa al usuario con las
    opciones. No se elige un plan B solo.
- [x] **Agregar Chart.js** (versión fija, el build UMD) en `Scripts/` y comprobar que carga
  en `Admin.Master`.
- [x] **Tests:**
  - `ReadinessPolicy` (AC-3 a AC-6 y AC-8): aprobado con 0.70, reprobado, aprobó y después
    reprobó, abandonado o en curso, solo Guided y sin sesiones;
  - la búsqueda y la paginación con un repositorio en memoria.
- [x] **Checklist:**
  - el admin ve el menú; el operador no, y una URL de Operadores lo devuelve a su inicio
    (AC-2);
  - estados correctos con datos armados a mano;
  - "Sin iniciar";
  - 50 por página;
  - el aviso al borrar.

## E2 · Detalle del Operador

> **E2 de Desarrollo ✅** (2026-10-22; probada por el usuario el 2026-10-23: "funciona").
> Detalle que no cambia el plan:
> - `StepRuns` (BLL) arma cada paso de una sesión desde sus intentos: duración (DA-2),
>   intentos fallidos (sin contar el salto), y si terminó sin acierto. Lo van a usar también
>   Pasos y las descargas. "Sin acierto" = el paso quedó atrás sin un intento correcto: en
>   Assessment, el avance forzado, también en el último paso de una sesión abandonada
>   (`ModePolicy.ForcesAdvanceAt`, el 4.º fallo); en Práctica libre, el salto **con** fallos.
> - Los intentos fallidos de cada sesión del detalle salen de sus intentos (`StepRuns`), con la
>   misma regla que los pasos a reforzar.
> - El puntaje de una sesión se muestra si está completada y su modo lo muestra
>   (`ModePolicy.ShowsScore`, D-90): "—" en Práctica libre y en las no completadas.
> - Un id inválido vuelve a `Operators.aspx?missing=1`, que muestra el aviso.
> - **B-3:** `SessionHelper.RedirectHomeAfterRoleChange` invalida `RoleCache`, vuelve a leer el
>   rol y manda al inicio del rol nuevo. `RedirectHome` solo no alcanzaba: el principal del
>   request todavía tiene el rol viejo, así que mandaba a `Users.aspx`, que `Web.config` ya le
>   niega. Lo usan `Users`, `Operators` y `Operator`.
> - **Generador:** ahora cada paso dura de 60 a 180 s más 40 s por fallo (antes todas las
>   sesiones duraban lo mismo), el avance forzado lleva el 4.º fallo como en la app, y ninguna
>   sesión queda en el futuro.
> - Del `/code-review medium` (8 hallazgos) se aplicaron 6 (B-3 con el rol nuevo, salto sin
>   fallos, avance forzado en una sesión abandonada, los intentos agrupados por sesión, las
>   sesiones de una guía publicada a mitad de la lectura, el filtro de rol en el SP de intentos,
>   y los tiempos del generador). Se descartaron 2: el mapeo de pasos que se parece al de
>   `TrainingGuideRepository` (este SP tiene otra forma) y los nombres de modo que también están
>   en `GuidedTest` (unificarlos es otro refactor). El índice para leer los intentos de un
>   operador sin key lookups queda para la E7.

- [x] SP: los intentos por sesión y paso de un operador (con el resultado, el motivo, la
  acción y las horas), para la duración por paso y los pasos a reforzar.
- [x] **`DashboardService.GetOperator`:**
  - el estado por guía;
  - las sesiones, con la duración de la sesión, y "—" si está en curso;
  - **pasos a reforzar:** en sus **Assessment** de cada guía, los pasos que terminaron sin
    acierto o con 2 o más intentos fallidos. Se ordenan por veces sin acierto y después por
    intentos fallidos, hasta **5 por guía**, con su motivo más frecuente.
- [x] **Duración de un paso** (DA-2), como función pura con tests: desde el último intento
  del paso anterior, o desde `StartedAt` en el primero.
- [x] **Página `Operator.aspx?id=…`** (AC-14). Un id inexistente, de un administrador o
  manipulado → aviso y vuelta a Operadores, sin datos de otro usuario.
- [x] **B-3** (enmienda 2): en `Users.aspx.cs`, cuando la BLL niega el acceso,
  `RedirectHome` en vez del login.
- [x] **Checklist:**
  - el detalle de un operador con sesiones de los tres modos, con horas de la planta y
    duraciones;
  - B-3: un admin al que le quitan el rol a mitad de sesión vuelve a su inicio, no al login.

## E3 · Pasos

> **Enmienda 3:** el SP devuelve los intentos y los conteos de abajo los calcula
> `DashboardService.GetSteps` con `StepRuns` (D-146).

> **E3 de Desarrollo ✅** (2026-10-23; probada por el usuario: "funciona"). Detalle:
> - `spDashboardStepAttempts` (modo, desde, guía) devuelve los intentos con su sesión, sin
>   `ORDER BY`: la BLL agrupa. `StepStatistics.Rank` agrega los `StepRun` por paso.
> - **"Sesiones que llegaron al paso"** = las que tienen algún intento en él, el salto
>   incluido. Por eso un salto sin fallos suma al denominador del %, pero no al numerador.
> - **Mediana:** solo de los pasos resueltos sin salto. Un salto de 3 s o el paso a medias de
>   una sesión en curso o abandonada la bajarían. Si no hay ninguno, "—".
> - **Cambio pedido por el usuario al probar (2026-10-23; lo registra Arquitectura y
>   Planeación):** con la guía en "Todas", Pasos muestra **una sola tabla resumen**. Junta el
>   mismo paso de cada guía en un renglón, agrupado por el nombre de `StepNames`, así que
>   "Line Clearance (apertura)" y "(cierre)" y los tres Log NC siguen separados. El %, los
>   conteos y la mediana salen de todas sus sesiones juntas, no de promediar guías. Con una
>   guía elegida, su ranking como antes (`StepStatistics.RankByName`).
> - La columna de saltos y su nota salen con `ModePolicy.AllowsSkip`, no con el nombre del modo.
> - **Motivos y acciones:** "Motivo · acción (veces)", con el código de la acción tal como se
>   registra (`AttemptActions`, p. ej. `LC.Save`).
> - El período (`ReportPeriod`: últimos 30 días o todo) vive en su propio archivo. Lo van a
>   usar el Resumen, el Seguimiento y las descargas.
> - Medido con `sqlcmd` en `TrainingApp_DashboardTest`, con "Todo": ~1,5 s y ~100 000 intentos
>   por modo.
> - Del `/code-review medium` (8 hallazgos) se aplicaron 5:
>   - la mediana sin saltos ni pasos a medias;
>   - el SP sin `ORDER BY`;
>   - `FailureReasons` se calcula a partir de `Failures`;
>   - la validación de la guía en un solo método;
>   - `ReportPeriod` en su propio archivo.
> - Quedan para la E7: el índice que cubre los intentos por sesión y leer las columnas por
>   ordinal. Se descartó el filtro repetido de `StepRuns.ForSession`: es defensivo y barato.

- [x] SP: por paso, guía y modo, en el período, devuelve:
  - las sesiones que llegaron al paso;
  - las sesiones con algún fallo;
  - los intentos fallidos sin saltos;
  - las veces que terminó sin acierto;
  - los saltos;
  - el conteo por motivo y acción.

  Las duraciones se calculan en la BLL, como en la E2.
- [x] **`DashboardService.GetSteps`:**
  - el **% de sesiones con fallo** y el orden;
  - la **mediana** de duración;
  - los saltos solo en Práctica libre (AC-17, AC-18).
- [x] **Página `Steps.aspx`:** filtros de período, guía y modo (Assessment o Práctica
  libre).
- [x] **Tests:** el % con y sin saltos, la mediana con un valor atípico y el orden.
- [x] **Checklist** con los datos generados.

## E4 · Resumen e inicio del administrador

> **E4 de Desarrollo ✅** (2026-10-23; probada por el usuario: "funciona"; Arquitectura y
> Planeación aceptó las tres interpretaciones). Detalle que no cambia el plan:
> - **El filtro de guía del Resumen** aplica a todos los bloques. Con una guía, los estados,
>   la tasa, la actividad y el top 5 son solo de ella.
> - **Reglas puras en `SummaryRules`:**
>   - "Primer Assessment" = el primer Assessment completado **por fecha de inicio**, la misma
>     fecha con la que filtra el período.
>   - Las semanas son de la planta (`Shared/PlantTime`, que ahora también usa
>     `SessionHelper.ToLocal`) y las semanas sin sesiones van en 0.
> - **El top 5** sale del mismo cálculo que Pasos (`RankSteps`, con las mismas guías y el
>   mismo período) y **solo trae pasos con algún fallo**: un paso con 0 % no es "un paso con
>   más fallos". Por eso puede traer menos de 5.
> - **`HomeUrl`** elige el Resumen con `CanViewReports`, la misma capacidad que revisa la página.
> - **Gráficas:**
>   - colores de la paleta de referencia de la skill dataviz: estados con los colores de
>     estado, como sus badges; modos con los tres primeros colores categóricos;
>   - cada gráfica lleva su tabla o sus totales al lado.
> - **Del `/code-review medium` (10 hallazgos) se aplicaron 6:**
>   - `HomeUrl` por capacidad;
>   - el top 5 con la misma guía y el mismo período, sin volver a leer;
>   - el top 5 sin pasos en 0 %;
>   - el primer Assessment por inicio;
>   - el año en las semanas con "Todo";
>   - los mensajes sin datos de Estados y de la tasa.
> - **Queda para la E7:** leer las sesiones de una sola guía en el SP.
> - **Descartados:**
>   - recorrer las sesiones una vez por guía (son 3);
>   - la primera semana parcial con 30 días (la define el período);
>   - el menú de un rol Trainer, que no existe.

- [x] **`DashboardService.GetSummary`** (AC-9 a AC-12):
  - operadores por estado y guía, con **todo el historial**;
  - del **período**:
    - la tasa de operadores aprobados;
    - la tasa de **aprobados al primer intento**;
    - el puntaje promedio;
    - las sesiones por semana (de lunes a domingo) y por modo;
    - el top 5 de pasos por % de sesiones con fallo en Assessment, que reutiliza la E3. Con
      "Todas", son los primeros 5 de la agrupación por tipo de paso de `GetSteps`
      (`RankByName`), sin el nombre de la guía; con una guía elegida, los de esa guía
      (spec → enmienda 1, D-147).
- [x] **Página `Summary.aspx`**, con gráficas de Chart.js (estados, actividad y top 5) y el
  período de cada bloque.
- [x] **`SessionHelper.AdminHomeUrl` → `Summary.aspx`** (AC-1).
- [x] **Tests:** la tasa al primer intento (AC-10), "—" sin denominador (AC-11) y las semanas
  desde el lunes.
- [x] **Checklist:** el login del admin cae en el Resumen; con la BD vacía, "Sin datos"; las
  guías no publicadas no aparecen (AC-12); el top 5 con "Todas" coincide con Pasos al seguir
  el enlace (D-147).

## E5 · Seguimiento

> **E5 de Desarrollo ✅** (2026-10-23; probada por el usuario: "funciona"). Detalle que no
> cambia el plan:
> - **`FollowUpRules`** (S-1, S-2, S-3), con los umbrales como constantes (2, 14 días, 2).
>   La página los muestra desde ahí.
> - **S-2:** cuenta los días completos desde el `StartedAt` de la última sesión de la guía
>   (`GuideReadiness.LastActivity`). Con 14 o más, aparece.
> - **S-3:** agrupa por `StepId` y cuenta los `StepRun` con `EndedWithoutSuccess`.
> - **Origen de los datos:** los pasos salen de todos los Assessment
>   (`spDashboardStepAttempts` sin período), solo de los operadores que no aprobaron esa guía.
>   El SP ahora devuelve también el `UserId` de la sesión.
> - **La página:**
>   - una fila por operador y guía, con el estado, la última sesión de la guía, una línea
>     por regla (S-3, una por paso) y el enlace al detalle;
>   - filtro de guía en la URL;
>   - abajo, cuántos operadores y casos hay.
> - **Del `/code-review medium` (9 hallazgos) se aplicaron 6:**
>   - el `UserId` en el SP, sin cruzar dos lecturas;
>   - los pasos solo de quien no aprobó;
>   - el estado se calcula una vez y S-2 sale de él;
>   - S-3 por `StepId` solo;
>   - los pasos por guía con un lookup;
>   - cada regla con su texto.
> - **Descartados:**
>   - validar dentro de la regla la guía y el modo de los pasos (el contrato está documentado
>     y el servicio los filtra);
>   - unificar `StuckStep` con `ReinforcementStep` (son vistas distintas);
>   - el orden de las revisiones de `Page_Load` (es el de todas las páginas del dashboard).
> - **Queda para la E7:** el rendimiento de leer todos los Assessment.
>   - **Primero:** el índice que cubre los intentos por sesión.
>   - **Si no alcanza:** el plan B de la enmienda 3, una fila por sesión y paso.
>   - **No va:** filtrar en el SP a quien ya aprobó, porque repetiría en SQL la regla de
>     `ReadinessPolicy` (D-136, D-139). Mover una regla a SQL lo decide el usuario.

- [x] **Reglas S-1, S-2 y S-3** en la BLL, como funciones puras con sus umbrales como
  constantes (D-141): por guía, sin período, y el operador sale al aprobar.
- [x] **Página `FollowUp.aspx`**, con todas las reglas que cumple cada operador y el
  enlace a su detalle.
- [x] **Tests:** AC-19 (S-2 por guía aunque practique otra), AC-20 (S-3 y la salida al
  aprobar) y AC-4 (S-1).
- [x] **Checklist** con casos armados a mano.

## E6 · Exportar a Excel

> **E6 de Desarrollo ✅** (2026-10-23; probada por el usuario: "funciona"). Al aprobarla, el
> usuario pidió dejar anotado que **al cerrar la Fase 10 sigue una fase de mejora de UI/UX**
> del dashboard, sus módulos y el Excel: la funcionalidad es correcta pero el diseño es muy
> básico (`docs/status.md` → "Deuda y riesgos"). Detalle que no cambia el plan:
> - **`Pages/Admin/Export.ashx`:** queda bajo la regla de `Web.config` de `Pages/Admin`. La
>   Session es de solo lectura, así una descarga larga no bloquea las otras pestañas.
> - **Los filtros de la URL se leen en un solo lugar, `ReportFilters`:** lo usan las cuatro
>   páginas y el handler, así una descarga no puede filtrar distinto que su pantalla (DA-5).
> - **Los datos salen del mismo método del servicio que cada página:**
>   - `GetSteps` y `GetFollowUp`, sin cambios;
>   - `ExportOperators`: la misma búsqueda que `SearchOperators`, sin paginar;
>   - `GetSessionExport`, nuevo: período y guía, todos los modos, incluidas las sesiones sin
>     intentos.
> - **El armado está en `ExcelReports` (presentación):**
>   - los textos de pantalla (`ReportText`); la acción va como código;
>   - fechas en la hora de la planta;
>   - duraciones como tiempo de Excel;
>   - encabezado fijo con autofiltro;
>   - sin `AdjustToContents`, porque medir el texto carga las fuentes y tardaba ~20 s: cada
>     columna lleva su ancho.
> - **Nombres de archivo:** `Operadores_`, `Pasos_`, `Seguimiento_` y `Sesiones_`, cada uno
>   con la fecha de la planta (`yyyy-MM-dd`).
> - **Resultado de cada paso (hoja Intentos por paso):** `StepRun.Outcome` (acierto, sin
>   acierto, saltado o sin resolver).
> - **El puntaje visible** lo decide la BLL (`VisibleScore`, D-90) para el detalle y el Excel.
> - **Corrección en `StepRuns`:** un salto resuelve el paso (D-83) aunque sea el último con
>   intentos de una sesión en curso o abandonada. Antes salía "sin resolver" y, si tenía
>   fallos, no contaba como "sin acierto" (spec: "saltado con fallos en Práctica libre"). Como
>   solo se salta en Práctica libre, en la práctica solo cambia Pasos en ese modo: S-3 y los
>   pasos a reforzar miran solo Assessment. Lo revisó Arquitectura y Planeación.
> - **Medición aproximada (AC-24, con un test explícito):** sesiones con "Todo", ~22 000
>   sesiones y ~264 000 filas de pasos (10 MB).
>   - ClosedXML: 3,7 s para armar y 8,6 s para guardar, ~12 s.
>   - Con la lectura de datos: ~16 s.
>   - Con 30 días, ~1/12 del volumen: ~1 s.
>   - **D-148 (enmienda 2 de la spec):** esta descarga con "Todo" queda fuera de AC-24. El
>     Resumen avisa junto al botón que puede tardar, y la E7 la prueba en streaming con
>     `OpenXmlWriter`.
> - **Del `/code-review medium` (10 hallazgos) se aplicaron 8:**
>   - la Session de solo lectura;
>   - el salto como resuelto;
>   - el puntaje visible en la BLL;
>   - S-3 con el texto de pantalla;
>   - escribir el archivo sin copiarlo;
>   - sin código redundante en la descarga de sesiones;
>   - la rama explícita sin paginar;
>   - sin alias de constantes.
> - **Queda para la E7:** las tres lecturas del SP (una por modo) y leer todas las sesiones.
> - **Descartado:** armar la URL de descarga desde los filtros aplicados. El handler vuelve a
>   validar igual que la página.

- [x] **Un `IHttpHandler` `Export.ashx?view=…&filters…`:**
  - es un **GET** con los mismos parámetros de la URL de la página. Solo lee datos, así que
    no necesita anti-CSRF;
  - verifica la sesión del usuario y `CanViewReports`; sin ellas, `RedirectHome` (AC-2);
  - valida los parámetros en el servidor: un valor inválido vuelve al valor de inicio;
  - pide los datos al `DashboardService` con los **mismos filtros** que la página (DA-5);
  - arma el `.xlsx` con ClosedXML: encabezados en español, fechas en la hora de la planta y
    nombre de archivo con la vista y la fecha.
- [x] **Descargas** (AC-21, AC-22):
  - Operadores;
  - Pasos, con la misma vista que la pantalla: con "Todas", la tabla agrupada por tipo de
    paso (spec → enmienda 1, D-147);
  - Seguimiento;
  - desde el **Resumen**, las sesiones, con las hojas **Sesiones** e **Intentos por paso**.
- [x] Con 0 filas, solo los encabezados.
- [x] **Checklist:** abrir cada archivo en Excel; probar la descarga como operador (AC-2).

## E7 · Rendimiento (AC-24)

> **E7 de Desarrollo ✅** (2026-10-23; probada por el usuario: "funciona"; BD de prueba
> `TrainingApp_DashboardTest` borrada con su aprobación).
> - **Cómo se midió:** `DashboardPerformanceTests` es un test explícito: no corre con el resto
>   y necesita la BD del generador. Usa `TrainingApp_DashboardTest` (1600 operadores, 22 868
>   sesiones, 292 495 intentos, 12 meses). Los tiempos son de la BLL más el DAL (sin el
>   render), en caliente, con ±20 % de ruido entre corridas.
> - **Vistas (todas por debajo de 3 s, sin cambios):**
>   - Resumen: 0,4 s con 30 días y 0,7–1,2 s con Todo.
>   - Pasos: 0,2–0,3 s con 30 días y 0,6–1,0 s con Todo.
>   - Operadores: 0,1 s.
>   - Operador: 0,02 s.
>   - Seguimiento: 0,7–1,2 s.
> - **Descargas chicas (Operadores, Pasos con Todo, Seguimiento):** 0,2–1,0 s. La primera
>   descarga del proceso tarda 2,3–2,8 s mientras se inicializa ClosedXML.
> - **Descarga de sesiones:**
>
>   | | Antes | Después |
>   |---|---|---|
>   | 30 días | 3,75 s · 930 MB asignados | **1,3 s** · 167 MB |
>   | Todo | 16,7 s · 3,4 GB asignados · pico +230 MB | **4,7 s** · 615 MB · pico +104 MB |
>
>   - Con Todo, ~2,2 s son de leer los datos.
>   - Sigue la excepción de AC-24 con su aviso (D-148, enmendada por D-149).
>   - Con 30 días, el usuario ya había aceptado el borde (2,9–3,0 s) antes del cambio a
>     ZipArchive.
> - **Ajustes, en el orden del plan:**
>   1. **Índice:** ampliar `IX_StepAttempts_Session` con `StepId`, `AttemptNumber`,
>      `Timestamp` y `Action` no mejoró nada, porque con Todo SQL lee la tabla igual. Se
>      descartó: solo encarecería cada intento del POD.
>   2. **Plan B de la enmienda 3:** no aplica. Una fila por sesión y paso son ~264 000 contra
>      292 000 intentos, y el tiempo estaba en escribir el archivo.
>   3. **Menos lecturas:** `spDashboardStepAttempts` con `@Mode` NULL (todos los modos) y
>      `spDashboardSessions` con `@From` y `@GuideId`. Son filtros sin reglas: ninguna regla
>      pasó a SQL.
>   4. **Streaming:**
>      - `OpenXmlWriter` tardaba ~10 s en escribir, porque crea varios objetos por celda.
>      - XML directo con `XmlWriter` lo bajó a ~5,6 s.
>      - Después, el `/code-review` mostró que `System.IO.Packaging` (el SDK en .NET
>        Framework) no escribe en streaming y pasa las partes de más de ~10 MB a
>        IsolatedStorage, que puede fallar en IIS.
>      - Se cambió a `ZipArchive` (enmienda 4, D-149), con un test que valida el archivo contra
>        el esquema de SpreadsheetML (`OpenXmlValidator`) y tests que lo leen con ClosedXML.
>      - El archivo va a un temporal del disco con nombre único y `DeleteOnClose`, no a memoria.
> - **Del `/code-review medium` (10 hallazgos) se aplicaron 8:**
>   - `ZipArchive` en vez de `System.IO.Packaging`;
>   - quitar los caracteres que XML no admite;
>   - Calibri 11 y el estilo Normal, como ClosedXML;
>   - el formato en la columna;
>   - no terminar de armar un archivo que falló;
>   - el temporal con nombre único;
>   - los formatos en un solo lugar (`XlsxStreamWriter.FormatCode`);
>   - los valores NaN como celda vacía.
> - **Descartados:**
>   - quitar el filtro de guías publicadas de `GetSessionExport` (protege si se publica una
>     guía entre dos lecturas);
>   - una proyección más chica del SP (la medición no la justifica).

- [x] Correr el generador de la E1 en una BD descartable de LocalDB.
- [x] Medir cada vista y cada descarga.
- [x] Ajustar los índices o los SPs hasta que todas respondan en **menos de 3 s**, en este
  orden: índices (el que cubre los intentos por sesión), después el plan B de la enmienda 3
  (una fila por sesión y paso, sin reglas). **Ninguna regla pasa a SQL** (D-139): si no
  alcanza, Desarrollo avisa y decide el usuario.
- [x] **Descarga de sesiones del Resumen con "Todo"** (spec → enmienda 2, D-148):
  - escribirla en streaming con `OpenXmlWriter`, con las mismas hojas y columnas;
  - reducir las lecturas: hoy son tres de `spDashboardStepAttempts` (una por modo) y todas las
    sesiones en `GetSessionExport`;
  - medir el tiempo **y la memoria** del proceso, antes y después. Si no llega a 3 s, se
    mantiene la excepción y el aviso.
- [x] Anotar las mediciones en el plan.
- [x] Borrar la BD de prueba al terminar (`docs/process.md` §6).

## E8 · Curva de aprendizaje y ficha (recortables)

- [ ] **Curva** (AC-15), en el detalle del Operador:
  - una gráfica de Chart.js con Práctica libre y Assessment en orden, sin Guided, con sus
    intentos fallidos y el puntaje de cada Assessment;
  - el dato de las sesiones de práctica antes del primer aprobado, contando Guided.
- [ ] **Ficha** (AC-16): **antes de implementarla, Desarrollo avisa** y el usuario decide
  DA-6 (impresión desde el navegador o un PDF en el servidor). Lleva el estado y los puntajes
  por guía, las sesiones y los pasos a reforzar, sin menú ni botones.
  - **DA-6 decidida (usuario, 2026-10-23; D-150): impresión desde el navegador.** Estilos de
    impresión (`@media print`) que ocultan el menú, los filtros y los botones; el PDF sale del
    "Guardar como PDF" del navegador. Sin librerías nuevas. El diseño se pule en la Fase 11.
- [ ] **Tests:** el conteo de sesiones de práctica antes del primer aprobado.

## Docs al cerrar

- **Desarrollo:**
  - `docs/architecture.md`: el dashboard, `ReadinessPolicy` y `IDashboardRepository`;
  - `docs/domain.md`: la regla de Aprobado y las de seguimiento, en un solo lugar;
  - `CLAUDE.md`: los scripts pasan a ser `00`–`10`, y `CanViewReports` entra en "Auth";
  - `status.md` y `history.md`;
  - la auditoría `2026-10-01`: Chart.js y ClosedXML se suman al inventario de AUD-29.
- **Arquitectura y Planeación:** la Fase 10 ✅ en el roadmap y el estado de D-136 a D-142.
