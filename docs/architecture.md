# Arquitectura — mecanismos y decisiones de diseño

> Las **reglas** (capas, invariantes, convenciones) están en `CLAUDE.md`, y las de negocio en
> `docs/domain.md`. Este archivo explica **cómo** están implementadas y registra las
> decisiones de diseño cerradas: no reabrirlas sin spec. Actualizarlo cuando cambie la estructura del código o se cierre una decisión.

## Estructura del código

```
MES Training Simulator/                    ← raíz del repositorio
├── TrainingApp/                           ← proyecto Web Forms (.NET FW 4.7.2)
│   ├── Pages/
│   │   ├── SessionHelper.cs               ← re-hidrata Session (vía AuthService), aviso de
│   │   │                                     cambio de contraseña, capacidades del request,
│   │   │                                     inicio y login por rol, último acceso y logout
│   │   ├── UserPrincipal.cs               ← principal con el rol (spec 013), lo arma Global.asax
│   │   ├── Auth/                          ← login y cambio de contraseña únicos, neutros, de
│   │   │   │                                 los dos roles (spec 016, D-62; Admin.Master)
│   │   │   ├── Login.aspx + .cs
│   │   │   └── ChangePassword.aspx + .cs
│   │   ├── Admin/                         ← módulo administrativo (spec 013, Admin.Master):
│   │   │                                     Users (gestión de usuarios) y UserListRow
│   │   │                                     (fila de la lista)
│   │   └── Training/
│   │       ├── StartSession.aspx + .cs    ← elegir Product → Area → Guía → modo
│   │       ├── GuidedTest.aspx + .cs      ← el POD: host de los controles de paso
│   │       ├── SessionPodStateStore.cs    ← IPodStateStore sobre la Session (spec 009)
│   │       ├── StepHints.cs               ← pista de cada paso (Guided tras un fallo y Ver
│   │       │                                 pista); por tramo en el Ingreso y en Logout (spec 012)
│   │       ├── Guide/                     ← guía visual de Guided (spec 010): GuideScriptBuilder
│   │       │                                 (guion por tipo de paso y textos), GuideSubstep,
│   │       │                                 GuideMoment (qué dejó la página en pantalla)
│   │       └── Models/                    ← modelos de UI: StepViewModel (+ EventArgs),
│   │                                         IStepControl, PodWorkListRow
│   ├── UI/
│   │   ├── Controls/Training/             ← un UserControl por tipo de paso (spec 006):
│   │   │                                     ResourceSetup, LineClearance, Search (4 modales
│   │   │                                     Moldeo), Assembly, DataCollection, SFCSplit,
│   │   │                                     LogNC (los tres métodos, D-60), Complete y
│   │   │                                     MesLogin (Ingreso a MES y limpieza, spec 016)
│   │   └── master/                        ← MasterPages (heredan AntiXsrfMasterPage) + pod.css
│   │                                         + íconos + pod-guide.js/.css (motor de la guía,
│   │                                         solo en Guided) + presap.css e imgs/presap
│   │                                         (pantallas del Ingreso a MES, spec 016)
│   ├── BLL/                               ← lógica de negocio, sin referencias a UI ni DAL directo
│   │   ├── Auth/
│   │   │   ├── AuthService.cs             ← login único, bloqueo, último acceso, cambio de
│   │   │   │                                 contraseña
│   │   │   ├── AccessPolicy.cs            ← rol → capacidades (CanTrain, CanManageUsers) (spec 013)
│   │   │   ├── RoleCache.cs               ← rol e Id de la cuenta por Code1, 2 min (Global.asax)
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
│   │   │   ├── PodContext.cs              ← sesión, guía, área, intentos, puntaje, estado,
│   │   │   │                                 Code1 del usuario y catálogos del área (perezosos,
│   │   │   │                                 una vez por request)
│   │   │   ├── PodCommandResult.cs        ← resultado de un comando: PodNotice, MesErrors,
│   │   │   │                                 intento, scrap…
│   │   │   ├── MesMessage.cs              ← catálogo de mensajes del MES, MesError y
│   │   │   │                                 superficies (spec 017)
│   │   │   ├── MesSequenceRule.cs         ← mensaje del MES de un botón fuera de turno y
│   │   │   │                                 MesSequenceState.Build (spec 017)
│   │   │   ├── StepSubmitResult.cs        ← resultado de un intento (outcome, intentos restantes)
│   │   │   ├── PodState.cs                ← estado del POD (orden + avance + precargados +
│   │   │   │                                 hallazgos + lo ensamblado + Ingreso a MES y
│   │   │   │                                 limpieza) e IPodStateStore
│   │   │   ├── MesLogin.cs                ← Ingreso a MES y limpieza (spec 016): pantallas,
│   │   │   │                                 acciones, MesLoginProgress, LogoutProgress y sus
│   │   │   │                                 reglas (MesLoginRule, LogoutCleanupRule)
│   │   │   ├── PodDefaults.cs             ← datos de simulación (Shop Order por defecto, etc.)
│   │   │   ├── SimulatedOrder.cs (+Row)   ← orden simulada y sus operaciones (split, scrap…)
│   │   │   ├── StepAnswers.cs             ← answers fijos (complete, start, acción inválida)
│   │   │   ├── AttemptActions.cs          ← catálogo de acciones del registro de actividad
│   │   │   ├── PieceSelectionRule.cs      ← qué filas exige cada paso (Serialized)
│   │   │   ├── StepTypes.cs               ← familia Log NC: IsLogNC y el método de cada tipo (spec 014)
│   │   │   ├── StepNames.cs               ← nombre de un paso con su variante (spec 012)
│   │   │   ├── StepAttemptSummary.cs      ← resumen de Práctica libre: intentos por paso (spec 012)
│   │   │   ├── NcFinding.cs               ← hallazgo de un paso de Log NC, NcDisposition y
│   │   │   │                                 NcFindingGenerator (spec 014)
│   │   │   ├── LogNCAnswer.cs             ← contrato JSON de la respuesta de Log NC (D-59)
│   │   │   ├── AssembledComponent.cs      ← lo ensamblado por componente, con su Assembly Data
│   │   │   ├── SerialSfcGenerator.cs      ← 4-5 piezas por orden Serialized
│   │   │   ├── PreloadedEquipment.cs      ← equipos precargados de Resource Setup y su generador (spec 015)
│   │   │   ├── SplitSfcGenerator.cs       ← SFC nuevo de cada SFC Split (Fase 6.5)
│   │   │   ├── AssemblyProgress.cs        ← avance de Assembly (en PodState)
│   │   │   └── DataCollectionProgress.cs  ← avance de DC por pieza (en PodState)
│   │   └── Scoring/
│   │       ├── ModePolicy.cs              ← modo → capacidades (guía, botones, secuencia,
│   │       │                                 penalización, avance forzado, pistas, salto,
│   │       │                                 resumen) (spec 012)
│   │       ├── ScoreCalculator.cs         ← puntaje, penalización, umbral de aprobación
│   │       ├── StepValidator.cs           ← reglas de validación por tipo de paso
│   │       ├── StepValidationContext.cs   ← área, catálogos, hallazgo del paso y filas
│   │       │                                 seleccionadas que usa el validador
│   │       ├── DataCollectionCompletionRule.cs ← grupos DC obligatorios
│   │       ├── PreloadedEquipmentRule.cs  ← ¿un valor es un equipo precargado? (spec 015)
│   │       └── PersonNameRule.cs          ← nombre del grupo Trainer (spec 015)
│   ├── DAL/
│   │   ├── Infrastructure/
│   │   │   ├── Database.cs                ← wrapper ADO.NET (SqlConnection + SqlCommand)
│   │   │   └── DatabaseFactory.cs         ← lee connection string de Web.config
│   │   └── Repositories/                  ← uno por tabla: Employee (API, lista en memoria
│   │                                         con precarga y refresco), User (+ IUserRepository
│   │                                         para los tests de la gestión), Product, Area,
│   │                                         TrainingGuide, TrainingSession, StepAttempt,
│   │                                         NCCode, DataCollectionGroup,
│   │                                         DataCollectionParameter, AssemblyComponent,
│   │                                         Operation
│   ├── Domain/                            ← POCOs puros, sin dependencias
│   │   ├── Employee.cs, Product.cs
│   │   ├── User.cs (UserRole), UserSummary.cs ← cuenta local; UserSummary suma sus sesiones
│   │   ├── Area.cs                        ← SerializationType + flags (Requires*)
│   │   ├── TrainingGuide.cs, TrainingStep.cs (StepWeight)
│   │   ├── TrainingSession.cs (SessionMode)
│   │   ├── TrainingSessionStepAttempt.cs  ← intento + AttemptReason y acción (spec 013)
│   │   ├── Operation.cs                   ← hoy solo las operaciones SIM del banner de la
│   │   │                                     extendida (D-58); nivel de las guías a futuro
│   │   ├── NCCode.cs (NCMethod), DataCollectionGroup.cs, DataCollectionParameter.cs
│   │   └── AssemblyComponent.cs           ← BOM (Qty decimal)
│   ├── Shared/
│   │   ├── Constants.cs                   ← DEFAULT_PASSWORD, SessionKeys, API de empleados
│   │   └── PasswordHelper.cs              ← hashing y comparación (PBKDF2)
│   └── Web.config
├── TrainingApp.Test/                      ← NUnit (.NET FW 4.7.2), tests de reglas BLL
│                                             (ScoreCalculator, StepValidator, Log NC y
│                                             hallazgos, Ingreso a MES y limpieza, guías
│                                             contra sus scripts, gestión de usuarios)
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
- **Fijación de sesión (2026-10-19, plan directo):** nunca se usa la Session de otra cuenta.
  - `EnsureUserSession` exige que `Session[Code1]` coincida con `User.Identity.Name`
    (`Trim`, sin distinguir mayúsculas) y `Session[UserId]` con el `UserPrincipal.UserId`
    (`RoleCache` guarda el Id con el rol, de la misma consulta). Si no, `Session.Clear()`
    (también el `PodState`) y sigue como una Session perdida. Cubre también una cuenta
    borrada y vuelta a registrar con el mismo Code1: el borrado invalida `RoleCache` en
    este proceso; en otro, se ve al vencer el caché (a lo sumo 2 min).
  - En un login, `SetUserSession` descarta la Session si tenía los datos de otra cuenta. El
    mismo usuario que vuelve a entrar (el ticket de Forms puede vencer antes que la Session)
    conserva su `PodState`.
  - `Logout()` expira la cookie `ASP.NET_SessionId` (`Abandon` conserva el id): el próximo
    login en ese navegador recibe uno nuevo. En el login no se renueva el id: en Web Forms
    exige un redirect extra y los dos puntos de arriba ya lo cubren.
  - Una sesión de entrenamiento en curso cuya Session se descartó se recupera con la
    reconstrucción del POD (D-10).
- `SetUserSession(User, LoginResult)`: **única** escritura de `UserId`, `Code1`, `Name`,
  `IsFirstLogin` y `LastAccess`; la usan el login y la re-hidratación. Una sola escritura
  evita que una clave nueva quede solo en el login (como pasó con `IsFirstLogin`).
  `LastAccess` (el último acceso anterior, spec 013) solo lo conoce el login: tras
  re-hidratar no se muestra.
- Capacidades del request (spec 013): `CurrentRole()` sale del `UserPrincipal`;
  `CanTrain()` / `CanManageUsers()` preguntan a `AccessPolicy`; `RedirectHome()` manda a
  quien no tiene la capacidad de una página a su inicio (sin rol: logout). `HomeUrl(role)`
  lo usa el login, porque en ese request el principal todavía es anónimo. `Logout()` vuelve
  al login único.
- `MustChangePassword()`: true mientras `IsFirstLogin` (AUD-08).
- `Logout()`: abandona las sesiones `InProgress` del usuario (D-9) y cierra la sesión.

## Roles, login y gestión de usuarios (spec 013; login único, spec 016)

Las invariantes (cuenta `User`, roles excluyentes, `AccessPolicy`) están en `CLAUDE.md` →
"Auth"; aquí, el flujo y cómo está implementado (movido desde `CLAUDE.md` el 2026-10-21: las
referencias `CLAUDE.md → "Auth Flow"` de specs, auditorías e historial apuntan aquí).

**Flujo del login** (`AuthService`, `Pages/Auth/Login.aspx`, el `loginUrl` de Forms; el
diseño de SAP NetWeaver es del login de MES simulado, dentro de la sesión de entrenamiento):
1. `AuthService` busca el `code1` en la lista de empleados de la API (`EmployeeRepository`,
   ver "API de empleados"). Si no existe → "Código de empleado no registrado".
   **Autorregistro** (siempre como Operator): cualquier empleado de la lista activa su
   cuenta. **Alta por un admin**: Gestión de usuarios crea la cuenta con su rol y la
   contraseña por defecto. El primer admin, por `Database/Ops/CreateAdmin.sql`.
2. Sin `User` local (primer acceso): la contraseña debe ser `Constants.DEFAULT_PASSWORD` → se
   crea el `User` con `IsFirstLogin = true` → cambio de contraseña. Con `User`: primero el
   **bloqueo** y después `PasswordHash` (PBKDF2).
3. **Bloqueo** (`LoginLockPolicy`): 5 fallos seguidos bloquean la cuenta 15 minutos; se
   desbloquea sola al vencer, antes por un admin o con un reset. El contador vuelve a 0 al
   bloquear o con un login correcto. El primer acceso sin `User` no cuenta fallos.
4. `AuthService.ChangePassword` (`Pages/Auth/ChangePassword.aspx`, la misma pantalla para los
   dos roles) pide **siempre** la contraseña actual (en el primer acceso o tras un reset, la
   por defecto; D-3) y la nueva no puede ser la por defecto → se guarda hasheada e
   `IsFirstLogin = false`.
5. Login exitoso → `FormsAuthentication.SetAuthCookie(code1)` +
   `SessionHelper.SetUserSession` → el inicio del rol (`SessionHelper.HomeUrl`). Mientras
   `IsFirstLogin`, las páginas del rol redirigen al cambio de contraseña (AUD-08). El usuario
   ve su **último acceso anterior** ("Primer acceso" si no hay): si no fue él, avisa al
   trainer (única detección del reclamo de cuenta ajena, D-20).

- **Rol en cada request:** `Global.Application_PostAuthenticateRequest` arma un
  `UserPrincipal` con el rol de `RoleCache` (2 min; un borrado o `CreateAdmin.sql` se ve al
  vencer, o al instante si lo hizo este proceso). No usa la Session: la autorización por
  `<location>` corre antes de que exista. `Web.config`: `Pages/Admin` solo Administrator,
  `Pages/Training` solo Operator y `Pages/Auth` (login y cambio de contraseña) abierta.
- **Un solo login** (spec 016, D-62; antes había uno de SAP para operadores y otro de admin,
  con `WrongPortal`): es el `loginUrl`. Un request sin sesión a cualquier carpeta cae ahí;
  el login no sigue el `ReturnUrl`, manda a cada rol a su inicio. Con sesión, el login
  manda a su inicio (`RedirectHome`): un operador en `Pages/Admin` vuelve a entrenar sin
  bucle.
- **Bloqueo y logins simultáneos:** `spUserLoginFailed` no suma con la cuenta ya bloqueada y
  `spUserLoginSucceeded` no quita un bloqueo vigente (devuelve 0 y la BLL responde
  `Locked`). Las fechas se guardan en UTC y se muestran en la hora de planta
  (`Central America Standard Time`, `SessionHelper.ToLocal`).
- **Gestión de usuarios** (`Pages/Admin/Users.aspx`): lista paginada con búsqueda, crear,
  resetear contraseña (vuelve a la por defecto y desbloquea), desbloquear y borrar (borra la
  cuenta y su historial; nadie se borra a sí mismo). El rol de una cuenta existente no se
  cambia desde la UI (enmienda de la spec 013). `UserAdminService` recibe el Id del admin autenticado y lo
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
  `PodCommandResult` (`PodNotice`, `MesErrors`, intento) a UI y elige los textos de los
  avisos propios del simulador (los del MES vienen armados de la BLL, ver "Mensajes del MES").
- **Estado (`PodState`)**: orden simulada + avance de Assembly y DC + equipos precargados
  de Resource Setup (spec 015: los genera `OpenResourceSetup` en cada apertura, viajan con el
  control en un campo oculto sin otro postback y se descartan al resolver el paso) +
  hallazgos de Log NC (`NcFindings`) + lo ensamblado (`AssembledComponents`), en Session bajo una
  clave por sesión (`SessionPodStateStore`). Si falta (Session reciclada), `LoadContext` lo
  reconstruye con la orden por defecto (D-10); también si hay orden pero, ya pasado el
  Start de una guía con Log NC, faltan los hallazgos. La reconstrucción aplica cada paso
  resuelto como si hubiera avanzado forzado: Start regenera los hallazgos sobre las piezas
  nuevas, Assembly llena lo ensamblado con la Assembly Data por defecto y cada paso de Log NC
  resuelto aplica la disposición de su hallazgo. Diferidos: AUD-40 (`PodState` mutable desde
  fuera de `PodService`, solo por convención) y AUD-42 (tras reconstruir en Serialized pueden
  cambiar las piezas).
- **Una sesión activa por operador (D-9):** `PodService.StartSession` y
  `SessionHelper.Logout` abandonan las `InProgress`. Iniciar una guía también descarta el
  estado del POD de las sesiones anteriores (AUD-41).
- **Logout simulado (spec 015):** el ícono del header es el último paso
  (`PodService.ClickLogout`): con la sesión en curso pasa por `CheckSequence` y, en su paso,
  empieza la limpieza al salir (spec 016, ver "Ingreso a MES y limpieza"), que cierra la
  sesión de entrenamiento con el resumen, sin tocar la sesión de la app. Sin
  sesión en curso (resumen final, pestaña vieja), `GuidedTest.LoadContext` llama
  `SessionHelper.Logout()` (D-21, AUD-33). Logout no tiene control de paso
  (`GetStepControl` devuelve null). Complete quita las filas completadas de la orden (D-22),
  también al forzarse y al reconstruir el estado.

## Mensajes del MES (spec 017)

Reglas: `docs/domain.md` → "Qué cuenta como intento" → "Mensajes del MES".

- **Catálogo en la BLL** (`BLL/Training/MesMessage.cs`): `MesMessageId` (el M-n de la spec;
  solo lo que el simulador puede mostrar), número, plantilla en inglés con marcadores
  (`{param}`, `{value}`, `{sfc}`, `{list}`, `{n}`, `{m}`) y superficie. `Format` arma el texto
  con "(Message n)" al final; `MesMessage.Error(id, args)` devuelve el `MesError` (id, texto,
  superficie). La BLL elige el mensaje; la página nunca lo decide.
- **Dónde viaja:** `PodCommandResult.MesErrors` (lista: solo M-7 trae varios, uno por pieza),
  `AssemblySubmitResult.MesError`, `DataCollectionSubmitResult.MesError` y, para el Split,
  `SFCSplitRequestedEventArgs.MesError`. El formato del Split (Qty como decimal) y del barcode
  de Assembly lo decide `PodService`, no el control.
- **Fuera de turno:** `PodService.OutOfSequence` arma el estado con `MesSequenceState.Build`
  (función pura: pasos sin resolver, SFC iniciado y seleccionado, "No" del LC de apertura
  sacado de los intentos para que sobreviva a la reconstrucción D-10, DC pendiente por pieza
  solo para Complete) y `MesSequenceRule.For` devuelve el mensaje. Con `RecordAttempt` el
  intento se registra igual que antes; con `Notice` el mensaje reemplaza "Ese no es el paso
  actual."; con `Ignore` (Guided) no se calcula.
- **Superficies (página y controles):**
  - **B, banner del POD:** `#podMesError` en `GuidedTest.aspx`, una fila por mensaje, lo
    llena `podShowMesErrors` (`Training.Master`, en el head para los scripts de arranque)
    desde el servidor (`ShowMesErrors`) o desde un chequeo del cliente (DC/Split sin fila,
    escaneo de Start ajeno). Sin auto-hide: un postback o elegir una fila lo quitan. Oculta el
    banner de éxito. Ancla `pod-mes-error`.
  - **F, franja de ventana:** Assemble Components (`#asmMesError`, la llena el JS del control:
    escaneo en el cliente o `ShowMesError` tras el Add), Data Collection Entry
    (`PnlDcMesError`). Anclas `asm-mes-error` y `dc-mes-error`.
  - **S, SFC Split:** `PnlSplitMesError` reemplaza el formulario; su OK cierra el modal en el
    cliente. Ancla `split-mes-error`. Los `Panel.Visible` de F y S se reponen en el
    `Page_Load` del control (persisten en ViewState).
  - Los chequeos del cliente toman el texto del catálogo renderizado por el servidor
    (`*MesErrorJs`, con `JavaScriptStringEncode`), sin copias en el JS.
- **Explicaciones en español:** `Pages/Training/Guide/MesErrorNotes` (presentación, textos de
  D-114). La página anota el primer mensaje del servidor del request (`NoteMesError`); en
  Guided, `GuideMoment.WithMesError` y `GuideScriptBuilder.AddMesErrorNote` insertan una nota
  Info sobre su ancla justo antes de la entrada del paso (la del Split espera el clic en su
  OK); en Práctica libre, Ver pista la muestra como último fallo, junto al texto del intento.
  Los mensajes de los chequeos del cliente usan `podNoteMesError` (`Training.Master`) con
  `window.podMesNotes`, que la página llena solo con lo que el modo recibe: en Guided la nota
  entra con `PodGuide.note` (antes del subpaso en curso); en Práctica libre, en
  `podPractice.failure`. Assessment no recibe ni notas ni la barra de práctica.

## Modos de sesión y Práctica libre (spec 012)

Reglas: `docs/domain.md` → "Modos de sesión" y "Qué cuenta como intento".

- **`ModePolicy`** (`BLL/Scoring/`): una instancia inmutable por modo, `ModePolicy.For(mode)`
  (un modo desconocido es excepción), expuesta como `PodContext.Policy`. Capacidades:
  `ShowsGuide`, `EnablesAllButtons`, `OutOfSequence` (`Ignore`/`RecordAttempt`/`Notice`),
  `NoLogicButtons` (`Disabled`/`RecordAttempt`/`Inert`), `PenalizesRetries`, `ForcesAdvance`,
  `FailureFeedback`, `HintOnDemand`, `AllowsSkip`, `ShowsScore` y `Summary`, más
  `PenaltyFactor` y `ForcesAdvanceAt`. `ScoreCalculator` conserva sus firmas con
  `SessionMode` y delega en ella; `ModeLabel` (nombre y badge) es lo único de la página que
  mira el `SessionMode`.
- **Aviso de otro paso (D-88):** con `Notice`, `PodService.OutOfSequence` y `MesFailure`
  devuelven `PodCommandResult.NotCurrentStepNotice` sin intento y la página muestra "Ese no
  es el paso actual.". En el Ingreso y la limpieza solo el Repositorio fuera de turno es
  "otro paso" (`MesAction.OpenRepository`); Log On sin Hora a Hora llega también como
  `OutOfSequence` pero es intento fallido.
- **Saltar e Ir a… (D-83, D-84 enmendada):** `PodService.Skip(ctx, targetOrder)` recorre
  desde el paso actual hasta el anterior al destino: `RecordAttempt(..., Skipped, resolve:
  true)` (sigue el camino del avance forzado, cada paso en su transacción de
  `spStepAttemptRecord`) y `RestoreAfterForcedAdvance`. El destino viaja en `HfSkipTarget`.
  La página (`BtnSkip_ServerClick`) muestra el resumen si el salto cerró la sesión; si no,
  también cuando la BLL lo ignoró (doble clic), hace `Response.Redirect` a la misma URL: el
  POD se vuelve a armar desde `PodState` como en un F5 (sin selección ni control abierto),
  en lugar de repintar paso por paso con `RepaintAfterForcedAdvance`.
- **Barra de práctica (D-89, D-91):** Ver pista, Saltar paso e Ir a… a la derecha de Abandon,
  en el pie del POD y en `mes-simbar`, dentro de una cápsula (`.practice-group`) con el
  estilo "Fiori Industrial" del simulador (enmienda 2; Abandon lo lleva en los tres modos).
  `ApplyPracticeBar` (en `OnPreRender`) decide la visibilidad por la política y deja en
  `window.podPractice` (con `JavaScriptSerializer`) el paso actual, su pista, el texto del
  último fallo de ese request y los pasos siguientes; los diálogos son SweetAlert con
  `titleText`/`textContent`. Para que la pista esté siempre disponible, la cápsula tiene
  `z-index` 1059, por encima de los modales del POD (1055, apilados hasta 1058) y por
  debajo de SweetAlert (1060); un `focusin` en captura en `document` evita que el focus
  trap de Bootstrap le quite el foco a SweetAlert (solo con `window.podPractice`).
- **Pistas (D-87, enmienda 2):** `StepHints.For(ctx, stepType)` en `Pages/Training`; en el
  Ingreso a MES y en Logout elige el texto por tramo con el mismo avance que usan los
  `ResumeName` de `GuideScriptBuilder` (`MesLoginProgress` y `State.Logout`). Lo usan Ver pista
  y la pista de Guided tras un fallo.
- **Resumen (D-86):** `PodService.AttemptSummary(ctx)` → `StepAttemptSummary.Build`, con los
  intentos que ya trae el contexto del cierre (sin consulta nueva); `ShowCompletionPanel` lo
  muestra cuando `Summary = AttemptsPerStep`, sin puntaje ni resultado.

## Log NC: tres métodos (spec 014)

Reglas de negocio y calificación: `docs/domain.md` → "Flujo estándar" (paso 8) y "Qué cuenta
como intento".

- **Familia de tipos de paso:** `StepType.LogNC` (desecho rápido), `LogNCFastRepair` (9) y
  `LogNCExtendedRepair` (10). `StepTypes.IsLogNC` y `StepTypes.MethodOf` son el único lugar
  que los agrupa: `GuidedTest`, `CheckSequence` (el botón [Log NC] vale en cualquiera de los
  tres) y la reconstrucción preguntan por la familia, nunca por un tipo suelto.
- **Hallazgos (`NcFinding`, `[Serializable]`):** `NcFindingGenerator` genera uno por paso de
  Log NC de la guía al dar Start (`StartPieces`) y al reconstruir. Serialized: piezas
  distintas al azar; NonSerialized: N unidades del lote (la cantidad del split). El código
  sale del método del paso; en las reparaciones el defecto de la nota sale de un código de
  desecho del área, sin repetirse en la orden mientras alcancen. Viven en
  `PodState.NcFindings`; `PodContext` expone el del paso actual (nota, guía y validador).
- **Lo ensamblado (`AssembledComponent`):** se registra una vez, al resolver Assembly, en el
  orden del BOM, con su Assembly Data (hoy Batch Number = Process Lot). Sobrevive al paso
  (`PodState.Assembly` sí pasa a `null`): lo leen el Browse de Component y Remove component.
  Un Assembly forzado o reconstruido lo completa con los valores por defecto.
- **Un solo control (D-60):** `LogNCControl` sirve a los tres métodos: NC Selection y NC Data
  Entry son las mismas pantallas, y los modales de la reparación rápida (Browse, Confirm,
  Remove component, Add Component) y de la extendida (NC logged, Select Disposition
  Function) son paneles del mismo control, manejados en el cliente. Todo el flujo termina en
  **un solo postback** (Add, la disposición o la decisión equivocada que lo cierra).
- **Respuesta JSON (D-59):** el control arma `{"code","component","removeAction",
  "addComponent","disposition"}` (cada campo según el método) y lo envía en
  `StepSubmitEventArgs.Answer`; las filas seleccionadas llegan aparte. `LogNCAnswer` es el
  único lugar que lee el contrato (validador, registro de actividad y control).
  `StepValidator` lo compara con el hallazgo del paso (`StepValidationContext.NcFinding` y
  `SelectedRows`).
- **Disposición:** al acertar o al forzar, `PodService` aplica la del hallazgo en la misma
  transacción del intento: desecho y Return to any previous sacan la pieza; la reparación
  rápida y Local Rework la dejan activa. Las operaciones del banner de la extendida salen de
  `PodContext.Operations` (`SIM`, D-58); `PodDefaults.SimOperation` es el respaldo de un
  área sin operaciones.
- **Banners:** reparación rápida (`GuidedTest.ShowRepaired`, D-55) y la extendida
  (`GuidedTest.ShowReworked`, D-54); textos en `general-flow.md` → Paso 8.

## Ingreso a MES y limpieza al salir (spec 016)

Reglas de negocio y calificación: `docs/domain.md` → "Flujo estándar" (pasos 0 y 10) y "Qué
cuenta como intento".

- **Pantalla completa (P-1):** mientras el paso actual es `MesLogin`, o Logout después del
  clic en el ícono (`PodContext.IsLogoutCleanup`), `GuidedTest` renderiza el POD oculto
  (`d-none`) y muestra `PnlMesScreen`: la barra del simulador (área, modo y Abandon, P-11) y
  `MesLoginControl`. No es un `IStepControl` ni pasa por `OpenStepType`: lo decide
  `ApplyMesLoginScreen` en `OnPreRender`, después de los handlers.
- **Un control para las dos (D-76):** `MesLoginControl` tiene el escritorio, la consola del
  script, Edge con pestañas, el Repositorio, Hora a Hora, Recursos MES con la etiqueta de la
  estación, el login de NetWeaver y "You have logged off" (`MesLoginView.Mode`: login o
  logout). Pantallas y pestañas se manejan en el cliente; la página le pasa en `View` la
  pantalla de arranque y el avance. Las pestañas abiertas viajan con cada postback en un
  campo oculto (F5 las pierde, D-78).
- **Un postback por pantalla calificable (D-68, D-77):** excepción a "un postback por paso",
  como el Split. Cada acción (`MesAction`) va a `PodService.SubmitMesAction`, que delega en
  `MesLoginRule` o `LogoutCleanupRule`: reglas puras de la BLL, con tests, que devuelven
  aceptado, ignorado (doble envío, script ya hecho), fallido (con la pantalla a la que se
  vuelve) o inválido. Un envío inválido (Session perdida, otra pestaña, manipulación) no
  registra nada y vuelve a la pantalla del avance guardado (`ResumeScreen`). Los fallos de
  secuencia en Guided se ignoran sin intento (`MesFailure`).
- **Avance en `PodState` (D-78):** `MesLoginProgress` (script, Edge abierto, Hora a Hora,
  Recursos MES) y `LogoutProgress` (Edge cerrado). F5 retoma desde ahí. Se descartan al
  resolver el paso. Si la Session se pierde, el Ingreso vuelve a empezar desde el escritorio
  y la limpieza desde el ícono Logout.
- **Code1 (D-79):** `PodContext.Code1` sale de la BD por el `UserId` (perezoso), nunca del
  cliente; se compara sin distinguir mayúsculas ni espacios alrededor. La contraseña del
  login de MES no viaja: solo una marca de que tenía algo (D-73).
- **Fin de la limpieza:** si el script cierra la sesión, la respuesta trae la consola con su
  salida y el resumen oculto; al cerrar la consola, el script del control muestra el POD y el
  resumen, sin otro postback (P-12).

## Versión de las guías (D-56, spec 014)

`spTrainingGuideSync` identifica una guía por `AreaId` + `Title` (la publicada más nueva).
Si la guía cambia de forma (otro número de pasos o un paso con otro `StepType`),
**despublica** la vieja y crea una nueva,
publicada, con el mismo título. Las sesiones guardan `TrainingGuideId` y los intentos
`StepId`, así que las sesiones en curso y el historial siguen con la vieja:
`spTrainingGuideGetByArea` lista solo las publicadas y `spTrainingGuideGetById` no filtra.
Reemplaza el `THROW 50014` de antes. Cambiar solo los pesos actualiza la guía en sitio.

## `GuidedTest` y controles de paso (Fase 6, specs 004-007)

Decisiones cerradas: no reabrir sin spec.

- **Controles estáticos (spec 006).** Los 8 controles de paso (más `MesLoginControl`, ver la
  excepción al final de este punto) están declarados en `GuidedTest.aspx`
  y se alterna `Visible`. Prohibido volver a `LoadControl` dinámico: generó cuatro bugs de
  ViewState en una sola sesión. Confirmado con negocio: los admins no crean tipos de paso
  desde la UI y un `StepType` no tiene UIs distintas por Area. Un `StepType` nuevo = `.ascx`
  + una línea de markup + un `case` en `GetStepControl`. Excepciones: los tres tipos de Log
  NC comparten `LogNCControl` (D-60); `MesLogin` y la limpieza de Logout usan
  `MesLoginControl`, en pantalla completa y fuera de `GetStepControl` (spec 016).
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
- **Notas y alertas de la trainer** (T7): `GuideSubstep.NoteHtml` va dentro del mismo globo,
  después de la instrucción, y solo en el subpaso donde aplica; con `NoteIsAlert` se pinta en
  rojo. Nunca una burbuja aparte (decisión del usuario, 2026-10-14: ruido visual). Son texto
  del guion, como las instrucciones.
- La guía no deja avanzar con una decisión incorrecta (R10): el control y `StepValidator` no
  cambian; el servidor le manda las respuestas esperadas solo en Guided. En un subpaso de
  escaneo con respuesta esperada, Enter solo pasa con ese valor (spec 016: el Code1 en la
  consola del script).
- **Pantalla completa del Ingreso (spec 016):** tiene su propio z-index, como un modal: el
  recuadro iluminado incluye los elementos libres que están dentro (la etiqueta de la
  estación). La guía retoma según el avance guardado (`StepScript.ResumeName`) y, tras un
  fallo, en la pantalla que falló (`GuideMoment.Incorrect` con la `MesScreen`). Los íconos
  del escritorio (doble clic) avanzan cuando aparece la consola o Edge.
- Los mensajes del MES que se ocultan solos pasan por `podAutoHide` (`Training.Master`) y
  esperan a la guía solo si ella los muestra (R9).
