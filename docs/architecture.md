# Arquitectura — mecanismos y decisiones de diseño

> Las **reglas** (capas, invariantes, convenciones) están en `CLAUDE.md`. Este archivo explica
> **cómo** están implementadas y registra las decisiones de diseño cerradas: no reabrirlas sin
> spec. Actualizarlo cuando cambie la estructura del código o se cierre una decisión.

## Estructura del código

```
MES Training Simulator/                    ← raíz del repositorio
├── TrainingApp/                           ← proyecto Web Forms (.NET FW 4.7.2)
│   ├── Pages/
│   │   ├── SessionHelper.cs               ← re-hidrata Session (vía AuthService), aviso de
│   │   │                                     cambio de contraseña, capacidades del request,
│   │   │                                     inicio y login por rol, último acceso y logout
│   │   ├── UserPrincipal.cs               ← principal con el rol (spec 013), lo arma Global.asax
│   │   ├── Auth/                          ← login y cambio de contraseña de SAP (operadores)
│   │   │   ├── Login.aspx + .cs
│   │   │   └── ChangePassword.aspx + .cs
│   │   ├── Admin/                         ← módulo administrativo (spec 013, Admin.Master):
│   │   │                                     Login, ChangePassword, Users (gestión de
│   │   │                                     usuarios) y UserListRow (fila de la lista)
│   │   └── Training/
│   │       ├── StartSession.aspx + .cs    ← elegir Product → Area → Guía → modo
│   │       ├── GuidedTest.aspx + .cs      ← el POD: host de los controles de paso
│   │       ├── SessionPodStateStore.cs    ← IPodStateStore sobre la Session (spec 009)
│   │       ├── Guide/                     ← guía visual de Guided (spec 010): GuideScriptBuilder
│   │       │                                 (guion por tipo de paso y textos), GuideSubstep,
│   │       │                                 GuideMoment (qué dejó la página en pantalla)
│   │       └── Models/                    ← modelos de UI: StepViewModel (+ EventArgs),
│   │                                         IStepControl, PodWorkListRow
│   ├── UI/
│   │   ├── Controls/Training/             ← un UserControl por tipo de paso (spec 006):
│   │   │                                     ResourceSetup, LineClearance, Search (4 modales
│   │   │                                     Moldeo), Assembly, DataCollection, SFCSplit,
│   │   │                                     LogNC, Complete
│   │   └── master/                        ← MasterPages (heredan AntiXsrfMasterPage) + pod.css
│   │                                         + íconos + pod-guide.js/.css (motor de la guía,
│   │                                         solo en Guided)
│   ├── BLL/                               ← lógica de negocio, sin referencias a UI ni DAL directo
│   │   ├── Auth/
│   │   │   ├── AuthService.cs             ← login por portal, bloqueo, último acceso, cambio
│   │   │   │                                 de contraseña
│   │   │   ├── AccessPolicy.cs            ← rol → capacidades (CanTrain, CanManageUsers) y
│   │   │   │                                 qué login acepta cada rol (spec 013)
│   │   │   ├── RoleCache.cs               ← rol por Code1, 2 min (Global.asax)
│   │   │   ├── LoginLockPolicy.cs         ← 5 fallos → 15 min de bloqueo
│   │   │   └── LoginResult.cs, ChangePasswordStatus.cs
│   │   ├── Admin/
│   │   │   └── UserAdminService.cs        ← gestión de usuarios: cada método verifica que
│   │   │                                     quien llama es admin (spec 013)
│   │   ├── Training/
│   │   │   ├── TrainingGuideService.cs    ← guías y catálogos (grupos DC, BOM, NC, Products)
│   │   │   ├── TrainingSessionService.cs  ← iniciar y abandonar sesiones
│   │   │   ├── PodService.cs              ← motor del POD (spec 009): LoadContext (una vez
│   │   │   │                                 por request) y comandos que registran intentos
│   │   │   │                                 (intento + avance atómico con spStepAttemptRecord);
│   │   │   │                                 arma el StepValidator con los catálogos del contexto
│   │   │   ├── PodContext.cs              ← sesión, guía, área, intentos, puntaje, estado y
│   │   │   │                                 catálogos del área (perezosos, una vez por request)
│   │   │   ├── PodCommandResult.cs        ← resultado de un comando: PodNotice, intento, scrap…
│   │   │   ├── StepSubmitResult.cs        ← resultado de un intento (outcome, intentos restantes)
│   │   │   ├── PodState.cs                ← estado del POD (orden + avance) e IPodStateStore
│   │   │   ├── PodDefaults.cs             ← datos de simulación (Shop Order por defecto, etc.)
│   │   │   ├── SimulatedOrder.cs (+Row)   ← orden simulada y sus operaciones (split, scrap…)
│   │   │   ├── StepAnswers.cs             ← answers fijos (complete, start, acción inválida)
│   │   │   ├── AttemptActions.cs          ← catálogo de acciones del registro de actividad
│   │   │   ├── PieceSelectionRule.cs      ← qué filas exige cada paso (Serialized)
│   │   │   ├── SerialSfcGenerator.cs      ← 3-5 piezas por orden Serialized
│   │   │   ├── SplitSfcGenerator.cs       ← SFC nuevo de cada SFC Split (Fase 6.5)
│   │   │   ├── AssemblyProgress.cs        ← avance de Assembly (en PodState)
│   │   │   └── DataCollectionProgress.cs  ← avance de DC por pieza (en PodState)
│   │   └── Scoring/
│   │       ├── ScoreCalculator.cs         ← puntaje, penalización, umbral de aprobación
│   │       ├── StepValidator.cs           ← reglas de validación por tipo de paso
│   │       ├── StepValidationContext.cs   ← área y catálogos que usa el validador
│   │       └── DataCollectionCompletionRule.cs ← grupos DC obligatorios
│   ├── DAL/
│   │   ├── Infrastructure/
│   │   │   ├── Database.cs                ← wrapper ADO.NET (SqlConnection + SqlCommand)
│   │   │   └── DatabaseFactory.cs         ← lee connection string de Web.config
│   │   └── Repositories/                  ← uno por tabla: Employee (API, lista en memoria
│   │                                         con precarga y refresco), User (+ IUserRepository
│   │                                         para los tests de la gestión), Product, Area,
│   │                                         TrainingGuide, TrainingSession, StepAttempt,
│   │                                         NCCode, DataCollectionGroup,
│   │                                         DataCollectionParameter, AssemblyComponent
│   ├── Domain/                            ← POCOs puros, sin dependencias
│   │   ├── Employee.cs, Product.cs
│   │   ├── User.cs (UserRole), UserSummary.cs ← cuenta local; UserSummary suma sus sesiones
│   │   ├── Area.cs                        ← SerializationType + flags (Requires*)
│   │   ├── TrainingGuide.cs, TrainingStep.cs (StepWeight)
│   │   ├── TrainingSession.cs (SessionMode)
│   │   ├── TrainingSessionStepAttempt.cs  ← intento + AttemptReason y acción (spec 013)
│   │   ├── Operation.cs                   ← POCO mínimo, entidad FUTURA inactiva
│   │   ├── NCCode.cs, DataCollectionGroup.cs, DataCollectionParameter.cs
│   │   └── AssemblyComponent.cs           ← BOM (Qty decimal)
│   ├── Shared/
│   │   ├── Constants.cs                   ← DEFAULT_PASSWORD, SessionKeys, API de empleados
│   │   └── PasswordHelper.cs              ← hashing y comparación (PBKDF2)
│   └── Web.config
├── TrainingApp.Test/                      ← NUnit (.NET FW 4.7.2), tests de reglas BLL
├── Database/
│   ├── StoredProcedures/                  ← 00 esquema · 01-04 SPs · 05 datos dev · 06-07 Moldeo · 08-09 LoFlo
│   └── Ops/                               ← scripts manuales: CreateAdmin, ResetUserPassword
└── docs/                                  ← ver el mapa de documentos en CLAUDE.md
```

## Sesión del usuario (`Pages/SessionHelper.cs`)

La Session (InProc) se pierde al cerrar el navegador, por timeout (60 min) o al reciclar el
App Pool, pero la cookie de Forms Authentication sobrevive: sin re-hidratar,
`Session[UserId]` sería null con el usuario autenticado.

- `EnsureUserSession()`: si la Session está vacía y el usuario autenticado, la
  re-hidrata con `AuthService.GetUserByCode1(User.Identity.Name)`. Se re-hidrata desde
  BD (una consulta) y no con `FormsAuthenticationTicket.UserData` (4 KB, no extensible).
- `SetUserSession(User, LoginResult)`: **única** escritura de `UserId`, `Code1`, `Name`,
  `IsFirstLogin` y `LastAccess`; la usan el login y la re-hidratación. Una sola escritura
  evita que una clave nueva quede solo en el login (como pasó con `IsFirstLogin`).
  `LastAccess` (el último acceso anterior, spec 013) solo lo conoce el login: tras
  re-hidratar no se muestra.
- Capacidades del request (spec 013): `CurrentRole()` sale del `UserPrincipal`;
  `CanTrain()` / `CanManageUsers()` preguntan a `AccessPolicy`; `RedirectHome()` manda a
  quien no tiene la capacidad de una página a su inicio (sin rol: logout). `Logout()` vuelve
  al login de admin o al de SAP según el rol.
- `MustChangePassword()`: true mientras `IsFirstLogin` (AUD-08).
- `Logout()`: abandona las sesiones `InProgress` del usuario (D-9) y cierra la sesión.

## Roles, logins y gestión de usuarios (spec 013)

- **Rol en cada request:** `Global.Application_PostAuthenticateRequest` arma un
  `UserPrincipal` con el rol de `RoleCache` (2 min; un borrado o `CreateAdmin.sql` se ve al
  vencer, o al instante si lo hizo este proceso). No usa la Session: la autorización por
  `<location>` corre antes de que exista. `Web.config`: `Pages/Admin` solo Administrator,
  `Pages/Training` solo Operator, el login y el cambio de contraseña de admin abiertos **con
  y sin `.aspx`** (FriendlyUrls redirige a la URL sin extensión y la autorización se evalúa
  sobre esa ruta; sin las dos, el login de admin entraba en un bucle con el de SAP).
- **Un solo `loginUrl`** (el de SAP): un request sin sesión a `Pages/Admin/` cae ahí con su
  `ReturnUrl` y el login de SAP lo reenvía al de admin. Con sesión, cualquier login manda a
  su inicio (`RedirectHome`): un operador en `Pages/Admin` vuelve a entrenar sin bucle.
- **Bloqueo y logins simultáneos:** `spUserLoginFailed` no suma con la cuenta ya bloqueada y
  `spUserLoginSucceeded` no quita un bloqueo vigente (devuelve 0 y la BLL responde
  `Locked`). Las fechas se guardan en UTC y se muestran en la hora de planta
  (`Central America Standard Time`, `SessionHelper.ToLocal`).
- **Gestión de usuarios:** `UserAdminService` recibe el Id del admin autenticado y lo
  verifica antes de tocar nada (`UnauthorizedAccessException` si no es admin; la página
  manda al inicio). `spUserDelete` borra intentos, sesiones y usuario en una transacción,
  bajo un lock de aplicación, y vuelve a verificar que quien borra sigue siendo admin y no se
  borra a sí mismo (dos admins borrándose a la vez no dejan cero admins).

## Registro de actividad (spec 013)

- Cada intento guarda `Reason` (`AttemptReason`: `None` si es correcto, `OutOfSequence`,
  `WrongDecision`, `InvalidAction`, `MissingSelection`, `EmptyRequiredField`) y `Action`
  (`AttemptActions`), en `spStepAttemptRecord`, en la misma transacción que el avance.
- `PodService.RecordAttempt` los recibe de cada comando: `CheckSequence` con el botón
  presionado, `SubmitStep` con la acción del envío (`AttemptActions.ForSubmit`), Assembly y
  DC con la suya, `FailedAttempt` con `MissingSelection`. Los botones de relleno pasan su
  acción desde la página (`GuidedTest.NonStepAction`).
- Sin UI: lo consume el dashboard de la Fase 10. Los intentos anteriores tienen `NULL`
  ("sin dato").

## API de empleados (`DAL/Repositories/EmployeeRepository`)

- Lista de `Constants.EmployeeApiUrl` en memoria del proceso (`_current`).
  `Global.Application_Start` la precarga en segundo plano (`AuthService.WarmUpEmployees`).
- Cuando vence (`Constants.EmployeeCacheMinutes`, 30 min), el siguiente login sigue usando la
  lista y dispara un refresco en segundo plano (uno a la vez). Ningún login espera a la API.
- Si la API falla o devuelve una lista vacía (B-1), se sigue sirviendo la última lista y se
  reintenta a los `EmployeeApiRetryMinutes`. Cada descarga espera como máximo
  `EmployeeApiTimeoutSeconds` (10 s). La primera descarga es una sola aunque lleguen varios
  logins a la vez. Solo falla si nunca hubo una descarga exitosa.
- **Limitación aceptada:** si el App Pool se recicla con la API caída, `_current` se pierde y
  el login falla hasta que la API responda. Aceptable para un simulador interno; si hiciera
  falta, guardar la lista en disco o en un caché distribuido.

## Motor del POD (spec 009)

- **`PodService.LoadContext(sessionId, userId)`** se llama una vez por request y devuelve el
  `PodContext`: sesión, guía, área, intentos, puntaje, estado del POD y catálogos del área
  (perezosos, una vez por request). La página lee de ahí el paso actual, `IsResolved`,
  `IsPassed` y los catálogos; nunca de `TrainingGuideService` ni de `ScoreCalculator`.
- **Comandos de `PodService`**: cada acción del operador es un comando que usa ese contexto
  (el paso actual lo define el servidor). Decide el flujo: orden simulada, selección como
  intento o como aviso, secuencia (`CheckSequence`), avance forzado y scrap.
  `spStepAttemptRecord` vuelve a validar en su transacción estado, paso y número de intento:
  si la sesión cambió, falla sin guardar nada.
- **Código nuevo del POD** = un comando en `PodService`; la página solo traduce el
  `PodCommandResult` (`PodNotice`, intento) a UI y elige los textos de los avisos.
- **Estado (`PodState`)**: orden simulada + avance de Assembly y DC, en Session bajo una
  clave por sesión (`SessionPodStateStore`). Si falta (Session reciclada), `LoadContext` lo
  reconstruye con la orden por defecto (D-10). Diferidos: AUD-40 (`PodState` mutable desde
  fuera de `PodService`, solo por convención) y AUD-42 (tras reconstruir en Serialized pueden
  cambiar las piezas).
- **Una sesión activa por operador (D-9):** `PodService.StartSession` y
  `SessionHelper.Logout` abandonan las `InProgress`. Iniciar una guía también descarta el
  estado del POD de las sesiones anteriores (AUD-41).

## `GuidedTest` y controles de paso (Fase 6, specs 004-007)

Decisiones cerradas: no reabrir sin spec.

- **Controles estáticos (spec 006).** Los 8 controles están declarados en `GuidedTest.aspx`
  y se alterna `Visible`. Prohibido volver a `LoadControl` dinámico: generó cuatro bugs de
  ViewState en una sola sesión. Confirmado con negocio: los admins no crean tipos de paso
  desde la UI y un `StepType` no tiene UIs distintas por Area. Un `StepType` nuevo = `.ascx`
  + una línea de markup + un `case` en `GetStepControl`.
- El paso abierto vive en el ViewState de la página (`OpenStepType`).
  `ApplyStepControlVisibility` es el único punto que toca `Visible`.
- `IStepControl.Initialize()` es el **único punto de reinicio** del control. Todo control
  tolera `ViewModel == null` y no registra scripts cuando está invisible.
- **Guard de sender:** `HandleControlSubmit` y `HandleReturnToPod` descartan eventos de
  controles que no son el del paso activo (ASP.NET dispara eventos también en controles
  invisibles).
- **Submit incorrecto:** el control queda abierto en el modal donde se respondió y solo se
  muestra el SweetAlert. Única excepción aprobada a la paridad de UX (spec 006 E3).
- Salir sin responder (✗ / Return to POD) usa `OnReturnToPod` y no registra intento.
- **F5 = GET** (`history.replaceState`): el POD y `TxtSFC` se reconstruyen desde la orden
  simulada; F5 no restaura la selección ni el control abierto. El botón Atrás todavía puede
  reenviar un POST viejo (riesgo aceptado).
- **Un postback por paso completado**, nunca uno por modal intermedio: un wizard que hace
  postback por modal corrompe el ViewState (`No se pudo cargar viewstate…`). Navegación del
  wizard y validación de formato en JS; los datos viajan en los inputs o hidden fields; un
  solo postback final que valida en el servidor y llama `OnStepSubmit`. Así funcionan
  Resource Setup, Line Clearance, los 4 modales de Search, la paginación de NC Selection y el
  Save Draft de DC.
- `CompleteControl` existe e implementa `IStepControl` pero no tiene UI: Complete se resuelve
  desde `BtnComplete`.

## Guía visual de Guided (spec 010)

- Solo presentación: no decide nada del flujo ni guarda estado.
- En cada respuesta, solo en Guided, `GuidedTest.OnPreRender` arma el guion con
  `GuideScriptBuilder` (subpasos y textos, en el código) a partir del `PodContext` y lo pasa
  con `JavaScriptSerializer` a `PodGuide.start` (`UI/master/pod-guide.js`). Assessment no
  recibe ni el motor ni el guion (AC-2).
- Los handlers que cambian lo que queda en pantalla anotan el `GuideMoment` (abrió,
  incorrecto, avance, resuelto), que fija la entrada del guion.
- Los elementos se anclan con `data-guide`, nunca con ids. Un control o pantalla nueva del
  POD necesita sus anclas y su guion.
- **Elementos libres de un subpaso** (`GuideSubstep.Free`, spec 011): además del resaltado,
  se pueden usar mientras el subpaso está en pantalla (suben sobre el oscurecido como Logout).
  Si lo resaltado no tiene campo, el foco va al primero libre. Lo usa el escaneo de Start:
  resalta la etiqueta de la orden y deja libre `TxtSFC`.
- La guía no deja avanzar con una decisión incorrecta (R10): el control y `StepValidator` no
  cambian; el servidor le manda las respuestas esperadas solo en Guided.
- Los mensajes del MES que se ocultan solos pasan por `podAutoHide` (`Training.Master`) y
  esperan a la guía solo si ella los muestra (R9).
