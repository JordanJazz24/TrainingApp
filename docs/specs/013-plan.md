# Plan de implementación — Spec 013

**Spec:** `docs/specs/013-cuentas-roles-actividad.md` (Approved 2026-10-13)
**Plan:** aprobado por el usuario (2026-10-13). E1-E3 en la rama `spec-013` (worktree), integrada en `TrainingApp` el 2026-10-14 (rebase + `--ff-only`); E4 sobre `TrainingApp` · **Cerrado ✅ (2026-10-14)**
**Arquitectura:** N-Tier. Reglas de acceso, bloqueo y gestión en la BLL (`BLL/Auth/`,
`BLL/Admin/`); los SPs solo guardan (salvo "nunca sin admins", que la spec pone en el SP por
la concurrencia). Páginas nuevas en `Pages/Admin/` con `Admin.Master`.

Cuatro entregas en el orden del flujo (entrar → bloqueo → administrar → registro). Cada una
deja la app funcionando. Ciclo de cada entrega (`docs/process.md` → "Cada entrega"):
implementar → MSBuild Rebuild + `aspnet_compiler` → tests (cambia la BLL en las cuatro) →
SQL probado en LocalDB (instalación limpia y re-ejecución) → review calibrado → checklist →
el usuario prueba → commit `feat: … (spec 013, entrega N)` con el roadmap.

## Base de datos (una sola vez, en E1)

- `00_Schema.sql`: `Users` suma `Role TINYINT NOT NULL DEFAULT 1`, `LastLoginAt`,
  `FailedLoginCount INT NOT NULL DEFAULT 0`, `LockedUntil`; `TrainingSessionStepAttempts`
  suma `Reason TINYINT NULL` y `Action NVARCHAR(50) NULL`.
- **ALTER aparte** para la BD del usuario (no se versiona, `docs/process.md` → "Base de
  datos"): agrega las seis columnas con `IF COL_LENGTH(...) IS NULL`. Todas las cuentas
  existentes quedan como Operator.
- SPs nuevos o ampliados en `01_spUser.sql` (E1-E3) y `04_spStepAttempt.sql` (E4); después de
  cada entrega, el usuario vuelve a correr el script que cambió.
- `Database/Ops/CreateAdmin.sql` (E1): con `@Code1` y `@Name`, crea el usuario como
  Administrator con la contraseña por defecto e `IsFirstLogin = 1`, o, si ya existe, solo le
  da el rol. `ResetUserPassword.sql` (T1) también desbloquea (E2).

## Decisiones de implementación (propuestas: confirmar al aprobar el plan)

| # | Decisión | Propuesta |
|---|---|---|
| P-1 | Contador al bloquear | Al quinto fallo se bloquea y el contador vuelve a 0: al vencer el bloqueo hay otros 5 intentos (la spec: "vuelve a 0 al desbloquear") |
| P-2 | Reset de contraseña y bloqueo | El reset también desbloquea (contador a 0): un usuario bloqueado que olvidó la contraseña puede entrar enseguida |
| P-3 | Code1 sin `User` en el login de admin | "Sin acceso administrativo" y no crea la cuenta: el autorregistro es solo por el login de SAP |
| P-4 | Dónde se ve el último acceso | Operador: una línea arriba en `StartSession`; admin: en el encabezado de `Admin.Master`. Hora local del servidor. Si la Session se re-hidrata, no se muestra (no se sabe el anterior) |
| P-5 | Rol en cada request | `UserPrincipal` (`GenericPrincipal` + `Role`) armado en `Application_PostAuthenticateRequest` con una caché de la BLL (`RoleCache`, 2 min). `UserAdminService.SetRole` la invalida para ese Code1: en este proceso el cambio es inmediato |
| P-6 | Página de inicio por capacidad | `CanManageUsers` → `Pages/Admin/Users.aspx`; si no, `StartSession`. Un operador en `Pages/Admin/` cae al login de SAP (Forms), que con sesión lo manda a su inicio: sin bucle (AC-4) |
| P-7 | Testabilidad de `UserAdminService` (AC-5) | Interfaz `IUserRepository` en el DAL (la implementa `UserRepository`) y un constructor que la recibe; el de siempre crea el repositorio real. Tests de AC-5 con un repositorio falso y de `AccessPolicy` |
| P-8 | Lista de usuarios | Búsqueda por Code1 o nombre (contiene), orden por nombre, 20 por página, `COUNT(*) OVER()` para el total |
| P-9 | Catálogo de `Reason` y `Action` | Tabla de abajo (la spec deja el catálogo final para la Fase 10) |

### Motivo (`AttemptReason`) y acción (`AttemptActions`) de cada intento

`Reason` = `None` en todo intento correcto. Las acciones son constantes de la BLL; la página
solo traduce un botón de relleno a su constante.

| Dónde | Acción | Motivo si falla |
|---|---|---|
| Botón fuera de secuencia (Assessment) | la del botón: `POD.ResourceSetup`, `POD.Activities.DCStandalone`, `POD.Start`, `POD.Assemble`, `POD.DCCollect`, `POD.SFCSplit`, `POD.LogNC`, `POD.Complete` y los de relleno (`POD.WorkInstruction`, `POD.Reports`, `POD.PlaceBackInQueue`, `POD.ToolList`, `POD.Activities.AsBuiltConfig`, `.LogComment`, `.SfcPlaceHold`, `.SfcReleaseHold`, `.RemoveFutureHold`, `.ChangeEquipmentStatus`, `.TimeSensitiveMaterial`) | `OutOfSequence` |
| Resource Setup → Save | `ResourceSetup.Save` | `WrongDecision` (Setup State) |
| Line Clearance → Save | `LC.Save` | `WrongDecision` |
| Start directo / checklist de Moldeo | `POD.Start` / `Start.Checklist.Save` | — / `WrongDecision` |
| Assembly → Add (último componente) | `Assembly.Add` | — |
| Data Collection → Save | `DC.Save` | `EmptyRequiredField` si falta un valor; si no, `WrongDecision` |
| SFC Split → Close | `SFCSplit.Close` | `WrongDecision` |
| Log NC → Add-Done / Add o Done | `LogNC.AddDone` / `LogNC.WrongButton` | `WrongDecision` / `InvalidAction` |
| Log NC o Complete sin la fila que exige el paso | `POD.LogNC` / `POD.Complete` | `MissingSelection` |
| Complete | `POD.Complete` | — |

## Entregas

### E1 — Roles, dos logins y acceso por capacidad  ✅ (2026-10-13, probada por el usuario)
- [x] Esquema + ALTER (todas las columnas de la spec); `CreateAdmin.sql`.
- [x] Domain: `UserRole`, `User.Role` (y las columnas de E2 en el POCO). `spUser*` leen y
      escriben el rol.
- [x] BLL: `AccessPolicy` (`CanTrain`, `CanManageUsers`); `AuthService.Login(code1, password,
      portal)`: cada login acepta solo su capacidad (`WrongPortal`, sin fallo); `RoleCache`.
- [x] `Global.asax`: `UserPrincipal` con el rol. `Web.config`: `Pages/Admin` solo
      Administrator; su Login y ChangePassword sin sesión.
- [x] `Admin.Master` (hereda `AntiXsrfMasterPage`, diseño neutro con Bootstrap),
      `Pages/Admin/Login.aspx`, `ChangePassword.aspx` y `Users.aspx` (por ahora solo el título).
- [x] Login de SAP: solo operadores, aviso con enlace al de admin; sin sesión y con
      `ReturnUrl` en `Pages/Admin/` → login de admin (AC-2c). `StartSession` y `GuidedTest`
      mandan al admin a su inicio; `SessionHelper.Logout` vuelve al login que corresponde.
- **AC:** 1, 2, 2b, 2c, 2d, 3, 4, 6.
- **Review:** `high`, `BLL/Auth`, `Global.asax.cs`, `Web.config`, `SessionHelper.cs` y las
  páginas de login; foco: nadie entra por el portal equivocado, un operador nunca llega a
  `Pages/Admin`, sin bucles de redirección, el rol del request viene del servidor.
- **Resultado:** review `high`, 10 hallazgos; se aplicaron 4. (1) El login de admin revisa
  la contraseña antes de decir "Sin acceso administrativo" a un Code1 sin cuenta (no revela
  qué Code1 tienen cuenta). (2) `<location path="Pages/Training">` solo para Operator, el
  mismo doble control que `Pages/Admin`. (3) Un `ReturnUrl` inválido (`/../x`) ya no tumba
  el login de SAP. (4) `CreateAdmin.sql` abandona el entrenamiento en curso. No aplicados:
  la contraseña por defecto del admin creado por script (D-20), un 500 con la BD caída y
  duplicación de páginas. **Bug al probar:** FriendlyUrls redirige a la URL sin `.aspx` y
  la autorización se evalúa sobre esa ruta. El login de admin caía en la regla de
  `Pages/Admin` y en un bucle con el login de SAP. `Web.config` abre las dos formas.

### E2 — Bloqueo y último acceso  ✅ (2026-10-13, probada por el usuario)
- [x] SPs `spUserLoginFailed` (suma atómica; bloquea con el umbral y la duración que pasa la
      BLL) y `spUserLoginSucceeded` (contador a 0, desbloquea, `LastLoginAt`).
- [x] `AuthService.Login`: bloqueo antes de la contraseña; quinto fallo → `Locked` con la
      hora de fin; correcto → devuelve el `LastLoginAt` anterior y lo actualiza. El primer
      acceso sin `User` no cuenta (AC-18).
- [x] Mensajes en los dos logins ("Cuenta bloqueada; intente de nuevo a las HH:mm") y el
      último acceso (P-4). `ResetUserPassword.sql` desbloquea.
- **AC:** 14, 15, 16, 17, 18.
- **Review:** `medium`, `AuthService.cs` y `01_spUser.sql`; foco: el orden bloqueo →
  contraseña, el contador no se pierde con dos fallos simultáneos, un bloqueo no revela si la
  contraseña era correcta.
- **Resultado:** review `medium`, 10 hallazgos; se aplicaron 7. Lo principal: logins
  simultáneos. `spUserLoginFailed` no suma con la cuenta ya bloqueada y
  `spUserLoginSucceeded` no quita un bloqueo vigente (devuelve 0 y la BLL responde
  `Locked`). Además: el alta guarda `LastLoginAt`, el tiempo se toma después de la API, la
  hora de planta es explícita (`Central America Standard Time`) y hay un solo mensaje de
  bloqueo (`SessionHelper.LockedMessage`). No aplicados: el bloqueo como enumeración y
  bloqueo ajeno (propio de la regla de la spec) y los tests de `LoginLockPolicy`
  (`docs/process.md` §5).

### E3 — Gestión de usuarios  ✅ (2026-10-14, probada por el usuario)
- [x] `IUserRepository`; `UserAdminService` (`BLL/Admin/`): `Search`, `Create`,
      `ResetPassword`, `Unlock`, `SetRole`. Cada método verifica la capacidad del admin
      autenticado (si no, excepción). Nadie cambia su propio rol; "nunca sin admins" en el SP
      dentro de la transacción (la BLL traduce el error a un aviso). Un operador que pasa a
      admin pierde su entrenamiento en curso (D-9).
- [x] SPs: búsqueda paginada, alta con rol, reset, desbloqueo y cambio de rol.
- [x] `Users.aspx`: búsqueda, tabla paginada (Code1, nombre, rol, estado, creada, último
      acceso), alta (Code1 + rol), y por fila reset, desbloquear y dar o quitar admin, con
      confirmación.
- [x] Tests: AC-5 (cada método con un usuario sin la capacidad lanza excepción) y
      `AccessPolicy`.
- **AC:** 5, 7, 8, 9, 10, 11, 12, 13.
- **Review:** `high`, `BLL/Admin`, `UserRepository.cs`, `01_spUser.sql` y `Users.aspx(.cs)`;
  foco: ningún método confía en la página, "nunca sin admins" con dos admins a la vez,
  búsqueda sin SQL dinámico.
- **Enmienda de la spec (2026-10-13, usuario, al probar; registrada en `9cfe566`):** sale
  dar o quitar el rol de admin (`SetRole` y `spUserSetRole`) y entra **Borrar usuario**.
  `spUserDelete` borra intentos, sesiones y usuario en una transacción. Con historial, la
  confirmación es una alerta con la cantidad de sesiones (`SessionCount` en
  `spUserSearch`). Nadie se borra a sí mismo, y el SP lo vuelve a verificar bajo un lock de
  aplicación.
- **Resultado:** review `high`, 10 hallazgos; se aplicaron 7. (1) Quien pide un cambio
  sigue siendo admin, verificado en el SP bajo el lock. (2) La acción es idempotente
  (`history.replaceState` y el rol pedido en el botón; luego salió con la enmienda).
  (3) El abandono del entrenamiento va en la misma transacción (luego salió con la
  enmienda). (4) El alta no falla si el empleado se autorregistra a la vez (`TryInsert`).
  (5) Patrón de búsqueda sin truncar y texto de 100 caracteres como máximo. (6) Un admin
  degradado en otro proceso vuelve a su inicio en vez de ver un error. (7) Rol fuera del
  enum: excepción. No aplicados: contraseña por defecto de admins nuevos (D-20) y
  consultas repetidas. Review `low` del borrado: sin cambios. Tests: 88 (21 de la spec 013).

### E4 — Registro de actividad  ✅ (2026-10-14, probada por el usuario)
- [x] Domain: `AttemptReason`; `TrainingSessionStepAttempt.Reason` y `.Action`.
- [x] BLL: `AttemptActions` (catálogo de arriba); `RecordAttempt` recibe motivo y acción;
      `CheckSequence` recibe la acción del botón; `ClickNonStepAction(ctx, action)`;
      `StepValidator` dice si a un DC le falta un valor.
- [x] `spStepAttemptRecord` con `@Reason` y `@Action` (misma transacción);
      `spStepAttemptGetBySession` los devuelve (null en los viejos).
- [x] `GuidedTest`: cada botón de relleno pasa su acción.
- [x] Cierre de la spec: `CLAUDE.md` → "Auth Flow" (roles, creación por admin, bloqueo,
      último acceso) y la lista de MasterPages; `docs/architecture.md` (estructura,
      principal con rol); roadmap.
- **AC:** 19, 20, 21 (`ScoreCalculatorTests` en verde). Se verifican con una consulta a
  `TrainingSessionStepAttempts` después de una sesión en el navegador.
- **Review:** `medium`, `PodService.cs`, `AttemptActions.cs`, `StepAttemptRepository.cs`,
  `04_spStepAttempt.sql`; foco: todo intento lleva motivo y acción, la secuencia S2 y el
  puntaje no cambian.
- **Resultado:** review `medium`, 9 hallazgos; se aplicaron 4. La acción del registro es
  obligatoria y de 50 caracteres como máximo. Los botones de relleno están en un solo mapa
  (`GuidedTest.NonStepActions`). `ForSubmit` no inventa acciones. `PodCommandResult.AttemptReason`
  pasó a llamarse `FeedbackReason`. No aplicados: el orden del despliegue (los scripts van
  antes), DC con un vacío y un fuera de rango a la vez (queda `EmptyRequiredField`), doble
  lectura del JSON de DC y tests del catálogo (`docs/process.md` §5). Verificado en la BD
  del usuario: un intento correcto de Guided con `ResourceSetup.Save` y uno fuera de
  secuencia de Assessment con `OutOfSequence` y `POD.Start` (AC-20). AC-21 (intentos viejos
  en NULL) verificado en LocalDB.

## Fuera de este plan

Lo de "Out" de la spec y D-20 (reclamo de cuenta ajena: solo detección).
