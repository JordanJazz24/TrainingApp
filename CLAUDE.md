# MESTrainingSimulator

Plataforma interna de entrenamiento y evaluación para operadores de manufactura médica:
flujos guiados inspirados en SAP MES, con progreso, puntaje y desempeño evaluados en el
servidor. Puede crecer a dashboards, métricas por operador, preguntas con más fallos y
reportes. Parte de los datos está hardcodeada porque representa pantallas y valores del día
a día del operador; eso no justifica una mala arquitectura: capas separadas, mantenible y
extensible.

Este archivo tiene solo **reglas vigentes**. El historial (qué se cerró y cuándo) va en
`docs/history.md`.

## Mapa de documentos

| Qué | Dónde | Cuándo leerlo |
|---|---|---|
| Reglas de trabajo, arquitectura, convenciones y comandos | este archivo | siempre |
| Reglas de negocio del dominio: jerarquía, ejes de variación, flujo estándar, modos, qué cuenta como intento, pesos, catálogos | `docs/domain.md` | antes de tocar `Domain/`, `BLL/Scoring`, `BLL/Training`, controles de paso o scripts de catálogo y guía |
| **Dónde quedamos**: fase actual y siguiente, pendientes, deuda, estado de la BD del usuario y auditorías | `docs/status.md` | al iniciar cada sesión |
| Fases, su alcance ("No incluye"), planes, revisiones con negocio y orden de tareas | `docs/roadmap.md` | al planificar o ante una duda de alcance |
| Qué se cerró, cuándo y cómo | `docs/history.md` | al buscar el origen de algo ya cerrado |
| Proceso de trabajo: spec o plan directo, entregas, verificación, `/code-review` calibrado, auditorías, commits, ramas y worktrees, scripts de BD, fidelidad de pantallas | `docs/process.md` | antes de proponer o empezar trabajo |
| Mecanismos y decisiones de diseño: árbol del código, motor del POD, controles de paso, guía de Guided, Session y caché de empleados | `docs/architecture.md` | antes de tocar `GuidedTest`, controles, `BLL/Training` o auth |
| Términos MES (SFC, LC, Shop Order, POD…) | `docs/glossary.md` | ante un término desconocido |
| Cómo se ve y se opera cada pantalla del MES real | `docs/ui-reference/` (`general-flow.md`, un archivo por área, `inventario-pantallas.md`) | antes de tocar UI |
| Specs y planes por entregas | `docs/specs/` | al trabajar en esa spec |
| Auditorías (hallazgos AUD-n) | `docs/audits/` (una por archivo) | al citar un hallazgo |
| Índice de decisiones D-n y bugs B-n: estado y dónde está cada una | `docs/decisions.md` | al citar, numerar o reabrir una decisión |

**Cada regla vive en un solo lugar.** Los demás documentos la referencian
(`CLAUDE.md → sección` o `docs/domain.md → sección`) en vez de copiarla. Las specs y las
auditorías son registro histórico: no se reescriben, se enmiendan con fecha. Qué se
actualiza en cada cierre: `docs/process.md` → "Documentación".

## Reglas de trabajo (siempre)

- Respetar el alcance de la fase en curso: su "No incluye" en el roadmap. Las features de
  fases futuras que se filtran a fases anteriores son la causa principal de la deuda técnica.
- Funcionalidad o reglas de negocio nuevas: `/spec` aprobada antes de planificar. Los bugs
  van con un plan directo (`docs/process.md`).
- El usuario prueba en el navegador y confirma ("funciona") **antes de cada commit**. Nunca
  afirmar que la UI funciona sin esa confirmación.
- Rama de desarrollo `TrainingApp`. No tocar `main`: ahí sube los cambios el usuario (D-5).

## Legacy

- `/MES Simulator Legacy` es la aplicación anterior y
  `/MES Simulator Legacy/CLAUDE_Legacy_Reference.md` es solo referencia funcional y de
  dominio: sirve para entender flujos, reglas del tutorial guiado, pantallas, pasos y datos
  migrables. No se actualiza ni es la arquitectura destino.
- No copiar sus patrones: `[WebMethod]` para lógica de negocio, `DataSet` como modelo de
  dominio, `sessionStorage` como estado global ni lógica de negocio en code-behind.
- Si una sección del tutorial se reutiliza tal cual, documentarlo explícitamente. Si cambia
  de parámetros o de reglas, modelarla de nuevo en el dominio.

## Arquitectura objetivo y seguridad

- El servidor es la **única fuente de verdad** para progreso, puntajes, estado de sesión,
  validaciones del flujo y resultados. Las páginas ASPX no pueden saltarse pasos ni modificar
  el estado de negocio directamente.
- La lógica de negocio vive en la BLL, nunca en code-behind ni en stored procedures.
  Dependencias solo hacia abajo: Presentación → BLL → DAL → Domain.
- Proteger autenticación, autorización, integridad del progreso y validaciones del lado
  servidor, en proporción al contexto: un simulador interno de entrenamiento.
- Contraseñas con **PBKDF2** (`Rfc2898DeriveBytes`, en `Shared/PasswordHelper.cs`), nunca en
  texto plano. Definitivo: reemplaza cualquier mención previa de SHA256+salt o BCrypt.

## Reglas de Negocio del Dominio

Cerradas con negocio: jerarquía Product → Area, ejes de variación, flujo estándar de una
guía, modos de sesión, qué cuenta como intento, pesos de los pasos y catálogos en BD. Viven
en `docs/domain.md`, la fuente de verdad para `Domain/`, `BLL/Scoring/`, `BLL/Training/` y
los stored procedures de catálogo. Leerlo antes de tocar cualquiera de ellos, un control de
paso o un script de guía; no re-derivar las reglas del legacy ni asumir variantes sin
confirmar.

## Arquitectura: N-Tier

```
TrainingApp/                  Web Forms (.NET FW 4.7.2)
  Pages/                      code-behind: Auth/ (login único), Admin/ (módulo administrativo),
                              Training/ (POD, guía de Guided, modelos de UI)
  UI/Controls/Training/       un UserControl por tipo de paso
  UI/master/                  MasterPages (heredan AntiXsrfMasterPage), pod.css, pod-guide.js
  BLL/  Auth/ Admin/ Training/ Scoring/
  DAL/  Infrastructure/ Repositories/   (un repositorio por tabla)
  Domain/                     POCOs puros
  Shared/                     Constants.cs, PasswordHelper.cs
TrainingApp.Test/             NUnit 4: reglas de la BLL con datos en memoria
Database/StoredProcedures/    00 esquema · 01-04 SPs · 05 datos dev · 06-07 Moldeo · 08-09 LoFlo
Database/Ops/                 scripts manuales: primer admin, reset de contraseña
```

Árbol completo con la responsabilidad de cada archivo: `docs/architecture.md`.

**Regla de dependencias:** Pages → BLL → DAL → Domain. Ninguna capa referencia hacia arriba;
el code-behind nunca llama al DAL.

**Domain:** solo POCOs, sin lógica, sin atributos de ORM, sin `System.Web` ni ADO.NET. La
lógica que cruza features (scoring, validación de pasos) va en `BLL/Scoring/`.
- `TrainingGuide.Steps` sí es una colección de navegación (una guía sin pasos está
  incompleta): el repositorio que carga una guía **debe** poblarla (JOIN o segunda consulta).
  Nunca asumir que una colección está cargada si no se pidió.
- Las relaciones situacionales (ej. `TrainingSession` + sus `TrainingSessionStepAttempt`)
  **no** se exponen como colección: la BLL pide cada repositorio y compone el resultado.

## Auth Flow

La cuenta local es `User` (tabla `Users`), nunca `Operator`: "operador" es la persona en la
línea del MES y uno de los dos roles (D-17).

**Roles (spec 013), excluyentes:** `Users.Role` = **Operator** (entrena) o **Administrator**
(administra usuarios y **no** entrena). Qué puede cada rol vive en un solo lugar,
`AccessPolicy` (`CanTrain`, `CanManageUsers`): las páginas y los servicios preguntan por la
capacidad, nunca por el nombre del rol. El rol de cada request lo arma `Global.asax`
(`Application_PostAuthenticateRequest` → `UserPrincipal`, con `RoleCache` de 2 min); con él
`Web.config` protege `Pages/Admin` (solo Administrator) y `Pages/Training` (solo Operator), y
las páginas lo revisan otra vez. Un usuario sin la capacidad de una página va a su inicio
(`SessionHelper.RedirectHome`), nunca al login.

1. **Un solo login, neutro** (spec 016, D-62): `Pages/Auth/Login.aspx` (el `loginUrl` de
   Forms) y una sola pantalla de cambio de contraseña (`Pages/Auth/ChangePassword.aspx`),
   con `Admin.Master`, para los dos roles. Cada rol va a su inicio
   (`SessionHelper.HomeUrl`). El diseño de SAP NetWeaver es del login de MES simulado,
   dentro de la sesión de entrenamiento.
2. `AuthService` busca el `code1` en la lista de empleados de la API (`EmployeeRepository`,
   en memoria; caché y fallos: `docs/architecture.md`). Si no existe → "Código de empleado
   no registrado". **Autorregistro** (siempre como Operator): cualquier
   empleado de la lista activa su cuenta. **Alta por un admin**: Gestión de usuarios crea la
   cuenta con su rol y la contraseña por defecto. El primer admin, por
   `Database/Ops/CreateAdmin.sql`.
3. Sin `User` local (primer acceso): la contraseña debe ser `Constants.DEFAULT_PASSWORD`
   → se crea el `User` con `IsFirstLogin = true` → cambio de contraseña. Con `User`: primero
   el **bloqueo** y después `PasswordHash` (PBKDF2).
4. **Bloqueo** (`LoginLockPolicy`): 5 fallos seguidos bloquean la cuenta 15 minutos; se
   desbloquea sola al vencer, antes por un admin o con un reset. El contador vuelve a 0 al
   bloquear o con un login correcto. El primer acceso sin `User` no cuenta fallos.
5. `AuthService.ChangePassword` pide **siempre** la contraseña actual (en el primer acceso o
   tras un reset, la por defecto) y la nueva no puede ser la por defecto → se guarda hasheada
   e `IsFirstLogin = false`, en la pantalla única de los dos roles.
6. Login exitoso → `FormsAuthentication.SetAuthCookie(code1)` +
   `SessionHelper.SetUserSession`. Mientras `IsFirstLogin`, las páginas del rol redirigen al
   cambio de contraseña (AUD-08). El usuario ve su **último acceso anterior** ("Primer
   acceso" si no hay): si no fue él, avisa al trainer (única detección del reclamo de cuenta
   ajena, D-20).
7. **Gestión de usuarios** (`Pages/Admin/Users.aspx`, `UserAdminService`): lista paginada con
   búsqueda, crear, resetear contraseña (vuelve a la por defecto y desbloquea), desbloquear y
   borrar (borra la cuenta y su historial; nadie se borra a sí mismo). El rol de una cuenta
   existente no se cambia desde la UI (enmienda de la spec 013). Cada método verifica en la
   BLL que quien llama es admin.

## Convenciones clave

**Datos**
- **Sin ORM**: toda consulta pasa por stored procedures vía ADO.NET (`SqlCommand` +
  `SqlDataReader`); cero SQL inline en C#. Es el estándar de la empresa y la app se audita:
  no proponer EF, Dapper ni otro ORM.
- El DAL devuelve POCOs de `Domain/`, nunca `DataSet` ni `DataTable`.
- Connection string `"Default"` fuera del repo: `TrainingApp/connectionStrings.config`
  (`configSource` desde `Web.config`, en `.gitignore`; en un checkout nuevo se copia de
  `connectionStrings.config.example`). Nunca hardcodeada ni versionada (AUD-04).

**Capas**
- Code-behind solo UI: visibilidad, binding y redirecciones. Sin lógica de negocio ni DAL.
- La BLL valida las reglas de negocio y **protege sus invariantes** sin confiar en que la
  página ya validó: `PodService.LoadContext` recibe el `userId` autenticado y solo
  devuelve contexto para una sesión propia y en curso; el paso actual lo define el servidor;
  `TrainingSessionService.Abandon` valida el dueño. Violar un invariante es una excepción, no
  un intento. Intento y avance/cierre se guardan juntos en una transacción
  (`spStepAttemptRecord`) (AUD-05/06, spec 009).
- **Qué hace cada modo de sesión vive en un solo lugar, `ModePolicy`** (`BLL/Scoring/`,
  spec 012), como `AccessPolicy` con los roles: la página, `PodService` y `ScoreCalculator`
  preguntan por la capacidad (`ctx.Policy.ShowsGuide`, `OutOfSequence`, `PenaltyFactor`…),
  nunca `mode == …`. Un modo nuevo es una instancia más de `ModePolicy`, no ramas nuevas.
- **Estado del POD solo en la BLL**: la orden simulada y el avance de Assembly/DC (`PodState`)
  solo los modifica `PodService`; la página y los controles leen el `PodContext` del request.
  Una sola sesión activa por operador (D-9). Mecanismo: `docs/architecture.md`.
- Sin `[WebMethod]` para lógica de negocio: si hace falta AJAX, un `IHttpHandler` (.ashx) o
  un endpoint mínimo.

**Sesión y seguridad**
- `Session` se escribe solo desde el servidor tras autenticarse, nunca desde JavaScript.
  Toda página con usuario autenticado llama `SessionHelper.EnsureUserSession()` al inicio de
  `Page_Load` (false → Login) y después `MustChangePassword()` (true → ChangePassword). Las
  claves nuevas del usuario se agregan solo en `SessionHelper.SetUserSession`.
- `Constants.DEFAULT_PASSWORD` y `Constants.EmployeeApiUrl` viven en `Shared/Constants.cs`:
  no en `Web.config` ni hardcodeadas en páginas.
- Toda página usa `Site.Master`, `Training.Master` o `Admin.Master` (diseño neutro: login,
  cambio de contraseña y módulo administrativo); las tres heredan `AntiXsrfMasterPage` (anti-CSRF). Texto del servidor hacia un
  `<script>`: `HttpUtility.JavaScriptStringEncode` o `JavaScriptSerializer`; texto al HTML
  con `<%#: %>`. ASP.NET rechaza postbacks con marcado (`<x`): los controles lo bloquean
  como formato.

**POD y controles** (detalle y decisiones cerradas: `docs/architecture.md`)
- Controles de paso **estáticos** declarados en `GuidedTest.aspx`, alternando `Visible`;
  nunca `LoadControl` dinámico (spec 006).
- **Un postback por paso**: los estados intermedios (wizard de modales, paginación, Save
  Draft) se resuelven en el cliente. Excepción: una pantalla que se califica al enviarla va
  en su propio postback (el Split de Moldeo, spec 014; cada pantalla del Ingreso a MES y de
  la limpieza al salir, spec 016, D-68).
- **Guía visual de Guided** (spec 010): solo presentación, no decide el flujo ni guarda
  estado; Assessment no recibe ni el motor ni el guion. Toda pantalla o control nuevo del POD
  necesita sus anclas `data-guide` (nunca ids) y su guion en `GuideScriptBuilder`.

**Código**
- Nullable: `<Nullable>enable</Nullable>` en el csproj cuando sea posible, o comentarios de
  nullable si no. No usar referencias nulas sin validar.

## Tech Stack

- .NET Framework 4.7.2 · ASP.NET Web Forms (ASPX + code-behind) · ADO.NET + stored
  procedures de SQL Server · Visual Studio 2019/2022 + IIS Express. Es el estándar que exige
  la empresa (la app se audita): no proponer migrar a .NET moderno.
- Forms Authentication · `HttpSessionState` (server-side, timeout 60 min) · PBKDF2.
- Empleados: API `Constants.EmployeeApiUrl` · Tests: NUnit 4 (.NET FW 4.7.2).

## Correr local, compilar y probar

- **Local:** abrir `TrainingApp.slnx` → crear `TrainingApp/connectionStrings.config` desde el
  `.example` (SQL Server local) → ejecutar los scripts de `Database/StoredProcedures/` en
  orden numérico (`00`–`09`) → F5 con IIS Express.
- **Tests:** unit tests de la BLL (servicios, scoring) con datos en memoria, sin BD. La
  integración del DAL, contra una BD de test dedicada (LocalDB); Testcontainers descartado.
  Las ASPX no se prueban.
- Nunca `dotnet build` ni `dotnet test`: usar estos comandos.

1. **Rebuild completo** (el incremental puede no recompilar; los warnings MSB3061 de
   `bin\roslyn` bloqueado por IIS Express/VS son inofensivos):
   `& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" "TrainingApp\TrainingApp.csproj" /t:Rebuild /p:Configuration=Debug /v:minimal /nologo "/clp:ErrorsOnly;Summary" /nowarn:MSB3061`
2. **Precompilar el markup** siempre que se toque `.aspx`/`.ascx`/`.master`: MSBuild no los
   compila en un Web Application Project y un error de markup aparecería recién en runtime.
   Carpeta de salida de ruta **corta** (las largas fallan por el límite de Windows):
   `& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe" -v / -p "<ruta>\TrainingApp" (Join-Path $env:TEMP "mespc")`
   `Web.config` tiene `optimizeCompilations="true"`: si tras cambiar la firma de algo que usa
   el markup aparece un error raro en runtime, borrar `%TEMP%\Temporary ASP.NET Files\vs`.
3. **Tests**, cuando cambie la BLL o el Domain (`ScoreCalculatorTests.cs` cubre todo
   `ScoreCalculator` desde la Fase 5: si se toca el puntaje, tiene que seguir en verde):
   compilar `TrainingApp.Test` y correr
   `& "C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" "TrainingApp.Test\bin\Debug\TrainingApp.Test.dll"`

## Skills

`/scaffold` (nueva feature slice) · `/code-review` (siempre con nivel, archivos y foco:
`docs/process.md` → "/code-review calibrado").
