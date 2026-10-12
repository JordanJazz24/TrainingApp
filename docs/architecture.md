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
│   │   │                                     cambio de contraseña del primer acceso y logout
│   │   ├── Auth/
│   │   │   ├── Login.aspx + .cs
│   │   │   └── ChangePassword.aspx + .cs
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
│   │   │   └── AuthService.cs
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
│   │                                         con precarga y refresco), User, Product, Area,
│   │                                         TrainingGuide, TrainingSession, StepAttempt,
│   │                                         NCCode, DataCollectionGroup,
│   │                                         DataCollectionParameter, AssemblyComponent
│   ├── Domain/                            ← POCOs puros, sin dependencias
│   │   ├── Employee.cs, User.cs, Product.cs
│   │   ├── Area.cs                        ← SerializationType + flags (Requires*)
│   │   ├── TrainingGuide.cs, TrainingStep.cs (StepWeight)
│   │   ├── TrainingSession.cs (SessionMode), TrainingSessionStepAttempt.cs
│   │   ├── Operation.cs                   ← POCO mínimo, entidad FUTURA inactiva
│   │   ├── NCCode.cs, DataCollectionGroup.cs, DataCollectionParameter.cs
│   │   └── AssemblyComponent.cs           ← BOM (Qty decimal)
│   ├── Shared/
│   │   ├── Constants.cs                   ← DEFAULT_PASSWORD, SessionKeys, API de empleados
│   │   └── PasswordHelper.cs              ← hashing y comparación (PBKDF2)
│   └── Web.config
├── TrainingApp.Test/                      ← NUnit (.NET FW 4.7.2), tests de reglas BLL
├── Database/
│   └── StoredProcedures/                  ← 00 esquema · 01-04 SPs · 05 datos dev · 06-07 Moldeo · 08-09 LoFlo
└── docs/                                  ← ver el mapa de documentos en CLAUDE.md
```

## Sesión del usuario (`Pages/SessionHelper.cs`)

La Session (InProc) se pierde al cerrar el navegador, por timeout (60 min) o al reciclar el
App Pool, pero la cookie de Forms Authentication sobrevive: sin re-hidratar,
`Session[UserId]` sería null con el usuario autenticado.

- `EnsureUserSession()`: si la Session está vacía y el usuario autenticado, la
  re-hidrata con `AuthService.GetUserByCode1(User.Identity.Name)`. Se re-hidrata desde
  BD (una consulta) y no con `FormsAuthenticationTicket.UserData` (4 KB, no extensible).
- `SetUserSession(User)`: **única** escritura de `UserId`, `Code1`, `Name` e
  `IsFirstLogin`; la usan el login y la re-hidratación. Una sola escritura evita que una
  clave nueva quede solo en el login (como pasó con `IsFirstLogin`).
- `MustChangePassword()`: true mientras `IsFirstLogin` (AUD-08).
- `Logout()`: abandona las sesiones `InProgress` del usuario (D-9) y cierra la sesión.

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
- La guía no deja avanzar con una decisión incorrecta (R10): el control y `StepValidator` no
  cambian; el servidor le manda las respuestas esperadas solo en Guided.
- Los mensajes del MES que se ocultan solos pasan por `podAutoHide` (`Training.Master`) y
  esperan a la guía solo si ella los muestra (R9).
