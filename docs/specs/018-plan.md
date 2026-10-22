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

> **E1 de Desarrollo integrada a `TrainingApp` sin probar** (2026-10-22, rama `fase-10`; el
> usuario pidió integrar sin el "funciona": falta su checklist).
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
- [ ] **Checklist:**
  - el admin ve el menú; el operador no, y una URL de Operadores lo devuelve a su inicio
    (AC-2);
  - estados correctos con datos armados a mano;
  - "Sin iniciar";
  - 50 por página;
  - el aviso al borrar.

## E2 · Detalle del Operador

- [ ] SP: los intentos por sesión y paso de un operador (con el resultado, el motivo, la
  acción y las horas), para la duración por paso y los pasos a reforzar.
- [ ] **`DashboardService.GetOperator`:**
  - el estado por guía;
  - las sesiones, con la duración de la sesión, y "—" si está en curso;
  - **pasos a reforzar:** en sus **Assessment** de cada guía, los pasos que terminaron sin
    acierto o con 2 o más intentos fallidos. Se ordenan por veces sin acierto y después por
    intentos fallidos, hasta **5 por guía**, con su motivo más frecuente.
- [ ] **Duración de un paso** (DA-2), como función pura con tests: desde el último intento
  del paso anterior, o desde `StartedAt` en el primero.
- [ ] **Página `Operator.aspx?id=…`** (AC-14). Un id inexistente, de un administrador o
  manipulado → aviso y vuelta a Operadores, sin datos de otro usuario.
- [ ] **Checklist:** el detalle de un operador con sesiones de los tres modos, con horas de
  la planta y duraciones.

## E3 · Pasos

- [ ] SP: por paso, guía y modo, en el período, devuelve:
  - las sesiones que llegaron al paso;
  - las sesiones con algún fallo;
  - los intentos fallidos sin saltos;
  - las veces que terminó sin acierto;
  - los saltos;
  - el conteo por motivo y acción.

  Las duraciones se calculan en la BLL, como en la E2.
- [ ] **`DashboardService.GetSteps`:**
  - el **% de sesiones con fallo** y el orden;
  - la **mediana** de duración;
  - los saltos solo en Práctica libre (AC-17, AC-18).
- [ ] **Página `Steps.aspx`:** filtros de período, guía y modo (Assessment o Práctica
  libre).
- [ ] **Tests:** el % con y sin saltos, la mediana con un valor atípico y el orden.
- [ ] **Checklist** con los datos generados.

## E4 · Resumen e inicio del administrador

- [ ] **`DashboardService.GetSummary`** (AC-9 a AC-12):
  - operadores por estado y guía, con **todo el historial**;
  - del **período**:
    - la tasa de operadores aprobados;
    - la tasa de **aprobados al primer intento**;
    - el puntaje promedio;
    - las sesiones por semana (de lunes a domingo) y por modo;
    - el top 5 de pasos por % de sesiones con fallo en Assessment, que reutiliza la E3.
- [ ] **Página `Summary.aspx`**, con gráficas de Chart.js (estados, actividad y top 5) y el
  período de cada bloque.
- [ ] **`SessionHelper.AdminHomeUrl` → `Summary.aspx`** (AC-1).
- [ ] **Tests:** la tasa al primer intento (AC-10), "—" sin denominador (AC-11) y las semanas
  desde el lunes.
- [ ] **Checklist:** el login del admin cae en el Resumen; con la BD vacía, "Sin datos"; las
  guías no publicadas no aparecen (AC-12).

## E5 · Seguimiento

- [ ] **Reglas S-1, S-2 y S-3** en la BLL, como funciones puras con sus umbrales como
  constantes (D-141): por guía, sin período, y el operador sale al aprobar.
- [ ] **Página `FollowUp.aspx`**, con todas las reglas que cumple cada operador y el
  enlace a su detalle.
- [ ] **Tests:** AC-19 (S-2 por guía aunque practique otra), AC-20 (S-3 y la salida al
  aprobar) y AC-4 (S-1).
- [ ] **Checklist** con casos armados a mano.

## E6 · Exportar a Excel

- [ ] **Un `IHttpHandler` `Export.ashx?view=…&filters…`:**
  - es un **GET** con los mismos parámetros de la URL de la página. Solo lee datos, así que
    no necesita anti-CSRF;
  - verifica la sesión del usuario y `CanViewReports`; sin ellas, `RedirectHome` (AC-2);
  - valida los parámetros en el servidor: un valor inválido vuelve al valor de inicio;
  - pide los datos al `DashboardService` con los **mismos filtros** que la página (DA-5);
  - arma el `.xlsx` con ClosedXML: encabezados en español, fechas en la hora de la planta y
    nombre de archivo con la vista y la fecha.
- [ ] **Descargas** (AC-21, AC-22):
  - Operadores;
  - Pasos;
  - Seguimiento;
  - desde el **Resumen**, las sesiones, con las hojas **Sesiones** e **Intentos por paso**.
- [ ] Con 0 filas, solo los encabezados.
- [ ] **Checklist:** abrir cada archivo en Excel; probar la descarga como operador (AC-2).

## E7 · Rendimiento (AC-24)

- [ ] Correr el generador de la E1 en una BD descartable de LocalDB.
- [ ] Medir cada vista y cada descarga.
- [ ] Ajustar los índices o los SPs hasta que todas respondan en **menos de 3 s**.
- [ ] Anotar las mediciones en el plan.
- [ ] Borrar la BD de prueba al terminar (`docs/process.md` §6).

## E8 · Curva de aprendizaje y ficha (recortables)

- [ ] **Curva** (AC-15), en el detalle del Operador:
  - una gráfica de Chart.js con Práctica libre y Assessment en orden, sin Guided, con sus
    intentos fallidos y el puntaje de cada Assessment;
  - el dato de las sesiones de práctica antes del primer aprobado, contando Guided.
- [ ] **Ficha** (AC-16): **antes de implementarla, Desarrollo avisa** y el usuario decide
  DA-6 (impresión desde el navegador o un PDF en el servidor). Lleva el estado y los puntajes
  por guía, las sesiones y los pasos a reforzar, sin menú ni botones.
- [ ] **Tests:** el conteo de sesiones de práctica antes del primer aprobado.

## Docs al cerrar

- **Desarrollo:**
  - `docs/architecture.md`: el dashboard, `ReadinessPolicy` y `IDashboardRepository`;
  - `docs/domain.md`: la regla de Aprobado y las de seguimiento, en un solo lugar;
  - `CLAUDE.md`: los scripts pasan a ser `00`–`10`, y `CanViewReports` entra en "Auth";
  - `status.md` y `history.md`;
  - la auditoría `2026-10-01`: Chart.js y ClosedXML se suman al inventario de AUD-29.
- **Arquitectura y Planeación:** la Fase 10 ✅ en el roadmap y el estado de D-136 a D-142.
