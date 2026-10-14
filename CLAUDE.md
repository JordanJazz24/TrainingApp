# MESTrainingSimulator

Plataforma interna de entrenamiento y evaluación para operadores de manufactura médica:
flujos guiados inspirados en SAP MES, con progreso, puntaje y desempeño evaluados en el
servidor. Puede crecer a dashboards, métricas por operador, preguntas con más fallos y
reportes. Parte de los datos está hardcodeada porque representa pantallas y valores del día
a día del operador; eso no justifica una mala arquitectura: capas separadas, mantenible y
extensible.

Este archivo tiene solo **reglas vigentes**. El historial (qué se cerró y cuándo) va en el
roadmap.

## Mapa de documentos

| Qué | Dónde | Cuándo leerlo |
|---|---|---|
| Reglas de dominio, arquitectura, convenciones y comandos | este archivo | siempre |
| **Dónde quedamos**: fase actual y siguiente, pendientes, deuda, estado de la BD del usuario e historial de fases | `docs/roadmap.md` → "📍 Estado actual" | al iniciar cada sesión |
| Proceso de trabajo: spec o plan directo, entregas, verificación, `/code-review` calibrado, auditorías, commits, ramas y worktrees, scripts de BD, fidelidad de pantallas | `docs/process.md` | antes de proponer o empezar trabajo |
| Mecanismos y decisiones de diseño: árbol del código, motor del POD, controles de paso, guía de Guided, Session y caché de empleados | `docs/architecture.md` | antes de tocar `GuidedTest`, controles, `BLL/Training` o auth |
| Términos MES (SFC, LC, Shop Order, POD…) | `docs/glossary.md` | ante un término desconocido |
| Cómo se ve y se opera cada pantalla del MES real | `docs/ui-reference/` (`general-flow.md`, un archivo por área, `inventario-pantallas.md`) | antes de tocar UI |
| Specs y planes por entregas | `docs/specs/` | al trabajar en esa spec |
| Auditorías (hallazgos AUD-n, decisiones D-n) | `docs/audits/` (una por archivo) | al citar un hallazgo o una decisión |

**Cada regla vive en un solo lugar.** Los demás documentos la referencian
(`CLAUDE.md → sección`) en vez de copiarla. Las specs y las auditorías son registro
histórico: no se reescriben, se enmiendan con fecha. Qué se actualiza en cada cierre:
`docs/process.md` → "Documentación".

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

Cerradas con negocio. Son la fuente de verdad para `Domain/`, `BLL/Scoring/` y los stored
procedures de catálogo. No re-derivarlas del legacy ni asumir variantes sin confirmar.

### Jerarquía

```
Product (línea de manufactura)
  └─ Area (sublínea de ensamblaje)
       └─ [FUTURO] Operation: hoy las guías son generales por Area. Cada Area tendrá de 3 a
          10 Operations; el esquema ya lo prevé (FK nullable) aunque no se use.
```

**Products y Areas oficiales** (confirmado con negocio 2026-09-25):

| Product | Areas |
|---|---|
| **Wires** | Subensamble 1, Subensamble 2, Composite Core, Línea Final, Testing, Backend |
| **Catéteres** | Redefinity, EEP, Ice, PV 35, PV 18, Otw |
| **Sensores** | LoFlo, Capnotrack, Mainstream, Source, Reusables, Detector |
| **R&C** | Moldeo |

Un área se carga en BD cuando se migra su catálogo (sus flags son `NOT NULL` y solo se
conocen al validarla con negocio). Áreas en BD y sus flags:

| Área | Origen | Flags |
|---|---|---|
| **Moldeo** (R&C) | catálogo real (`06_RCMoldeoCatalog.sql`, guía en `07`) | NonSerialized · Assembly no · DC sí · checklist de Start sí |
| **LoFlo** (Sensores) | `08_SensoresLoFloCatalog.sql`, guía en `09` (Fase 7) | Confirmado 2026-10-07 para **todas las áreas de Sensores**: Serialized · Assembly sí · DC sí · Component en NC sí · checklist de Start no. DC Groups, parámetros, códigos NC y BOM: simulados por ahora |
| **Redefinity** (Catéteres) | **datos de ejemplo** en `05` ("NO USAR EN PRODUCCIÓN"), para probar el camino Serialized en dev | Serialized · Assembly sí · DC sí (no son decisiones de negocio) |
| **Subensamble 1** (Wires) | ejemplo en `05` | no son decisiones de negocio |

**Particularidades de Moldeo:**
- El ID del molde (`DCGTM_ID_MOLDE/A`) es un **DataCollectionParameter**, no Assembly
  (`RequiresAssembly = false`, confirmado con negocio).
- Las 3 preguntas de LC Moldeo (`DCGTM_LC_MOLDEO/A`, dentro del Start) **no** son
  DataCollectionParameters: son parte del checklist de Start (4 modales en `SearchControl`)
  y las califica `StepValidator` en el paso Search.

### Ejes de variación (independientes: ninguno se deriva de otro)

- **SerializationType** — la diferencia aparece **solo en Search/Start y en Cierre/Scrap**.
  Resource Setup es idéntico en ambos tipos.
  - `Serialized`: la Shop Order genera varios SFC individuales con identidad y trazabilidad
    propias (ej. 989609001101-01, -02, -03). Se escanean uno a uno o se eligen varios en
    Browse Work List. Scrap: selección individual.
  - `NonSerialized`: un único SFC con cantidad (ej. SFC 50012345, Qty 500); no existen SFCs
    tipo 50012345-001. MES controla la cantidad total. Scrap: split de la cantidad del lote.
- **RequiresAssembly** y **RequiresDataCollection** (`bool`, hoy por Area; se moverán a
  Operation cuando exista): independientes de SerializationType. Una operación serializada
  puede no requerir DC (confirmado con negocio). Nunca asumir "serializado ⇒ Assembly".
- El login a SAP NetWeaver **no** se evalúa: es solo carga de escenario.

### Flujo estándar de una TrainingGuide

Los 3 primeros pasos de Apertura y los de Cierre son iguales en todas las Areas; solo cambia
el catálogo (códigos NC, parámetros de DC), nunca la secuencia de tipos de paso.

```
Apertura
  1. Resource Setup   Asocia el recurso (estación) a la Shop Order y las herramientas
                      (ej. R_SCN_MASK_1, Shop Order 303406950, Tools 10033058 y 10087734).
                      No define qué piezas se procesan (eso es Search/Start). Captura libre,
                      sin validar contra catálogo. Se califica por SECUENCIA y por
                      Setup State = "Set Up" (guardar en "Open" es intento fallido); los
                      recursos capturados no se califican.
  2. Line Clearance   Pregunta sí/no.
  3. Search/Start     Se califica por SECUENCIA. Aquí aparece la diferencia por
                      SerializationType:
                      · Serialized: 3-5 piezas por orden en el simulador; Start, Assembly,
                        DC y Complete trabajan todas las piezas juntas (selección parcial =
                        aviso sin penalización); TxtSFC muestra "[N SELECTED]".
                      · NonSerialized: se selecciona el único SFC una vez; el POD lo carga
                        con su cantidad.
                      · Con checklist de Start (RequiresStartChecklist, hoy solo Moldeo):
                        Start abre 4 modales y se califican sus respuestas.
  4. Assembly         Si RequiresAssembly. Component List = BOM de la operación
                      (AssemblyComponents, simulado). Un escaneo numérico por componente
                      cubre toda su Qty y se aplica a todas las piezas ("Apply to all
                      SFCs"). Termina con todo el BOM ensamblado. No valida el barcode
                      contra el número de parte: se califica solo por secuencia.
  5. Data Collection  Si RequiresDataCollection. Parámetros de catálogo por Area, por pieza.
Cierre
  6. Line Clearance   Igual que el de apertura.
  7. SFC Split        Solo en guías NonSerialized: divide filas del lote antes de seguir.
  8. Log NC/Scrap     Catálogo de códigos por Area.
  9. Complete         Cierra el proceso de manufactura.
```

### Modos de sesión (`TrainingSession.Mode`)

- **Guided** — práctica: reintentos ilimitados, sin penalización ni bloqueo. La guía visual
  no deja enviar la decisión incorrecta, así que casi siempre queda un intento por paso: es
  lo esperado (decisión del usuario, 2026-10-07). Los pasos que más fallan se miden con
  Assessment.
- **Assessment** — evaluación con puntaje mínimo de aprobación (`ScoreCalculator.PassingScore`
  = 0.70). Cada paso se completa antes de desbloquear el siguiente, con penalización por
  reintento:

  ```
  PenaltyFactor(intento 1)  = 1.00
  PenaltyFactor(intento 2)  = 0.80
  PenaltyFactor(intento 3)  = 0.60
  PenaltyFactor(intento 4+) = 0.00 → el paso queda fallido y el sistema FUERZA el avance
                                     al siguiente (la evaluación nunca queda bloqueada)

  StepScore = StepWeight × PenaltyFactor(intento en que acertó)
  ```

  Aplica igual a pasos con contenido (Data Collection, NC) y a pasos de secuencia (Resource
  Setup, Search): en estos, cada intento fuera de secuencia es un intento fallido.

### Qué cuenta como intento

- **Formato vs. decisión.** Un error de *formato* (campo requerido vacío, Shop Order o
  barcode no numérico, filas faltantes, marcado `<x`) lo bloquea el control y **no** cuenta
  como intento. Toda *decisión* del operador (Sí/No, Setup State, código NC, valores de DC,
  elegir la fila en Log NC y Complete) se envía siempre al servidor y la califica
  `StepValidator`. Nunca rechazar una decisión en el control ni revelar la respuesta correcta
  en Assessment (el mensaje solo muestra los intentos restantes). Excepción: en Data
  Collection un campo vacío sí es intento fallido.
- **Acción incorrecta** (spec 007): botón equivocado dentro de un paso (ej. Add o Done en
  vez de Add-Done) o gate de fila sin cumplir → `StepAnswers.Invalid` a la BLL, intento
  fallido. Complete acepta solo `"complete"`.
- **Secuencia en Assessment (S2):** todos los botones del POD están habilitados, incluidos
  los que no aplican al área y las acciones del MES sin lógica (Work Instructions, Reports,
  Place Back in Queue, Tool List, las opciones de relleno de Activities). Un click en un
  botón que no es el del paso actual es un intento fallido **del paso actual**, con el mismo
  mensaje genérico (no revela que fue de secuencia), y no abre su control. La secuencia se
  evalúa antes que los avisos de formato. Activities → LC entra en la regla.
- **Nunca cuentan:** navegación (Browse Work List, selección de filas, Abandon, Logout) y
  salir de un control sin responder (✗ / Return to POD).
- **Avance forzado:** Resource Setup forzado carga la orden por defecto (Shop Order
  `303406950`), ignorando lo capturado; Start forzado deja el POD como si Start se hubiera
  ejecutado.

### Ponderación de pasos (`StepWeight`) — la fuente de verdad es la BD

Cada paso tiene su peso en `TrainingSteps.Weight` (0.00-1.00) y `ScoreCalculator` usa ese
valor. Los pesos de una guía **deben sumar 1.00**: el cálculo no normaliza (una guía que
suma 0.90 nunca llega a 100%). Pesos actuales (verificados contra `05`, `07` y `09`, 2026-10-07):

```
Paso                        Moldeo (07)   LoFlo (09)   Redefinity (05, ejemplo)
Resource Setup                 0.20         0.20          0.20
Line Clearance (apertura)      0.05         0.05          0.05
Search / Start                 0.15         0.05          0.05
Assembly                        —           0.15          0.15
Data Collection                0.30         0.30          0.30
Line Clearance (cierre)        0.05         0.05          0.05
SFC Split (NonSerialized)      0.05          —             —
Log NC/Scrap                   0.10         0.10          0.10
Complete                       0.10         0.10          0.10
Total                          1.00         1.00          1.00
```

En Moldeo, Search absorbe el peso de Assembly (que no tiene) menos el de SFC Split (que sí
tiene). LoFlo tiene la forma de Redefinity y sus mismos pesos. Una guía nueva define sus
pesos en su script SQL y debe sumar 1.00 (`spTrainingGuideSync` lo valida al cargarla).

### Catálogos en BD (nunca hardcodeados en código)

- `NCCodes`: códigos de scrap/NC por Area.
- `DataCollectionGroups`: por Area (a futuro, por Operation), con `IsRequired`. Una pieza
  completa Data Collection al guardar todos los grupos obligatorios; si el área no tiene
  ninguno, basta uno cualquiera. Moldeo: solo `DCGTM_FECHA_EXPIRACION_NIR/A` es obligatorio.
- `DataCollectionParameters`: cada uno pertenece a un grupo (`DataCollectionGroupId`).
- `AssemblyComponents`: BOM de la operación (hoy por Area, `OperationId` nullable),
  `PartNumber`/`Revision`, `Description`, `Qty` decimal. Simulado por ahora. No hay conteo
  esperado de escaneos (`ExpectedComponentCount` se eliminó).
- Flags de `Areas`: `SerializationType`, `RequiresAssembly`, `RequiresDataCollection`,
  `RequiresNCComponent` y `RequiresStartChecklist`.
- Datos de simulación (preguntas LC, parámetros de DC, códigos NC) van en scripts de
  `Database/StoredProcedures/`, nunca en arrays de la BLL ni en `Web.config`. Excepción
  aceptada (D-6): la pregunta de Line Clearance ("¿La limpieza de linea se ejecuto?") es la
  misma en todas las operaciones y queda en el código.

## Arquitectura: N-Tier

```
TrainingApp/                  Web Forms (.NET FW 4.7.2)
  Pages/                      code-behind: Auth/ (login de SAP), Admin/ (módulo administrativo),
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

1. **Dos logins, mismo `AuthService.Login`:** el de SAP NetWeaver (`Pages/Auth/Login.aspx`,
   parte de la simulación) acepta solo operadores; el administrativo
   (`Pages/Admin/Login.aspx`), solo admins. El rol equivocado con la contraseña correcta
   recibe el camino correcto y no suma un fallo. Forms tiene un solo `loginUrl` (el de SAP):
   un request sin sesión a `Pages/Admin/` sigue al login de admin.
2. `AuthService` busca el `code1` en la lista de empleados de la API (`EmployeeRepository`,
   en memoria; caché y fallos: `docs/architecture.md`). Si no existe → "Código de empleado
   no registrado". **Autorregistro** (solo por el login de SAP, como Operator): cualquier
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
   e `IsFirstLogin = false`. El operador lo hace en la pantalla de SAP; el admin, en la
   administrativa.
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
- Toda página usa `Site.Master`, `Training.Master`, `Auth.Master` (Login y Cambio de
  contraseña de SAP) o `Admin.Master` (módulo administrativo, diseño neutro); las cuatro
  heredan `AntiXsrfMasterPage` (anti-CSRF). Texto del servidor hacia un
  `<script>`: `HttpUtility.JavaScriptStringEncode` o `JavaScriptSerializer`; texto al HTML
  con `<%#: %>`. ASP.NET rechaza postbacks con marcado (`<x`): los controles lo bloquean
  como formato.

**POD y controles** (detalle y decisiones cerradas: `docs/architecture.md`)
- Controles de paso **estáticos** declarados en `GuidedTest.aspx`, alternando `Visible`;
  nunca `LoadControl` dinámico (spec 006).
- **Un postback por paso**: los estados intermedios (wizard de modales, paginación, Save
  Draft) se resuelven en el cliente.
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
