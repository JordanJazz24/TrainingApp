# Plan 021: Área asignada por operador

**Spec:** `021-area-operador.md` (aprobada 2026-10-27) · **Decisiones:** D-165 a D-179 · cierra
B-20 y B-3 · **Planeado en:** la nube (`cloud/021-area-operador`) · **Implementa:** Desarrollo,
en local.

**Enmienda 1 (2026-10-27):** B-4 entra en la E2.
**Enmienda 2 (2026-10-27):** la E4 sin Quitar área (en la página ni en la BLL) y Cambiar área en la columna Área (spec 021 → enmienda 1, D-208).

Cada entrega sigue `docs/process.md` → "2. Cada entrega": implementar, verificar con los
comandos de `CLAUDE.md` → "Correr local, compilar y probar", `/code-review` con nivel y foco, el
"funciona" del usuario en el navegador **antes** del commit y commit solo con sus rutas
(`git commit -- <rutas>`), con `docs/status.md` y la casilla de este plan en el mismo commit.

## Reglas de este plan

- Rama y worktree según `docs/process.md` → 7; se integra a `TrainingApp` con la aprobación del
  usuario.
- Sin ORM ni SQL inline; la lógica en la BLL; el code-behind solo UI (visibilidad, binding y
  redirecciones).
- BD de pruebas (`process.md` → 6): el esquema va en `00_Schema.sql` con su
  `IF COL_LENGTH … ALTER`; basta volver a correr los scripts o recrear la BD.
- No se toca el motor del POD (`GuidedTest`, controles, puntaje), salvo la redirección del
  `Page_Load` de `GuidedTest` (E3).
- Si algo de la spec no cierra al implementar: parar y pedir la enmienda (process.md → 1).
- Ninguna decisión nueva en este plan. Si aparece una, se pide un rango libre (el de la spec
  terminó en D-179).

## E1 · Datos y DAL

- [x] `00_Schema.sql`: `Users.AreaId INT NULL` con FK a `Areas` (en el `CREATE` y un
      `IF COL_LENGTH('dbo.Users','AreaId') IS NULL ALTER TABLE … ADD` para la BD existente),
      con comentario: solo Operator, `NULL` = sin área (D-165, D-179/P-1).
- [x] `01_spUser.sql`:
  - `spUserGetByCode1`, `spUserGetById` y `spUserSearch` devuelven `AreaId` y el texto del
    área (`ProductName`, `AreaName`, con `LEFT JOIN`).
  - SP nuevo `spUserChooseArea (@UserId, @AreaId)`: actualiza **solo** `WHERE Id = @UserId AND
    AreaId IS NULL AND Role = 1` y devuelve las filas afectadas (P-4).
  - SP nuevo `spUserSetArea (@UserId, @AreaId NULL)`: cambia o quita el área de un Operator;
    devuelve las filas afectadas (P-5).
- [x] `02_spTrainingGuide.sql`: SP nuevo `spAreaGetEligible`: áreas con al menos una guía
      publicada, con `ProductId`, `ProductName`, `AreaId` y `AreaName`, ordenadas por producto y
      área (D-176, P-6).
- [x] `10_spDashboard.sql`: `spDashboardOperators` devuelve `ProductName` y `AreaName` del
      operador (`LEFT JOIN`; sin tocar filtros ni cálculos, D-175).
- [x] `05_SeedData.sql`: quitar "NO USAR EN PRODUCCIÓN" del encabezado (D-178).
- [x] Domain: `User.AreaId` (`int?`), `ProductName` y `AreaName`; los mismos en `UserSummary` y
      `DashboardOperator`; POCO nuevo `EligibleArea` (`ProductId`, `ProductName`, `AreaId`,
      `AreaName`).
- [x] DAL: `UserRepository` mapea las columnas y agrega `ChooseArea(userId, areaId)` (`bool`) y
      `SetArea(userId, areaId?)` (`bool`), también en `IUserRepository`; `AreaRepository.GetEligible()`;
      `DashboardRepository` mapea el área.
- [x] Verificación (Desarrollo): Rebuild; scripts `00`–`12` en una BD descartable de LocalDB
      (instalación limpia y re-ejecución sobre una con usuarios, sesiones e intentos):
      `spAreaGetEligible` devuelve Moldeo, LoFlo, Subensamble 1 y Redefinity;
      `spUserChooseArea` dos veces seguidas → la segunda afecta 0 filas; sobre un admin, 0.
- [x] `/code-review low --max-findings 5` sobre `00`, `01`, `02`, `10` y el DAL. Foco: `ALTER`
      idempotente, `WHERE AreaId IS NULL`, `LEFT JOIN` (un operador sin área sigue apareciendo).

**Hecho cuando:** compila, los scripts corren dos veces sin error y los usuarios existentes
quedan con `AreaId = NULL`. Sin cambios visibles en la app.

## E2 · BLL, validación de `StartSession` y tests

- [x] `BLL/Training/TrainingAccessRule.cs` (pura, sin repositorios; P-8):
  - `EnsureCanStart(User user, TrainingGuide guide)`: excepción si la guía es `null` (B-20), no
    está publicada (B-20), el usuario no es Operator, no tiene área o la guía es de otra área
    (D-173).
  - `IsEligible(int areaId, IEnumerable<EligibleArea> eligible)`.
- [x] `PodService.StartSession(userId, guideId, mode)`: carga el usuario y la guía y llama a
      `TrainingAccessRule.EnsureCanStart` **antes** de `TrainingSessionService.Start` (que
      abandona las sesiones en curso, D-9): un inicio rechazado no toca la sesión en curso (AC-8).
      Excepción, no intento (P-7).
- [x] `TrainingGuideService`: `GetEligibleAreas()` y `GetStartOptions(userId)` (el texto
      "Producto · Área" del operador y las guías publicadas de su área; vacío si no tiene).
- [x] `BLL/Training/OperatorAreaService.cs` (con `IUserRepository` inyectable, como
      `UserAdminService`): `ChooseOwnArea(userId, areaId)`: el usuario es Operator, no tiene área
      y el área es elegible; si `ChooseArea` afecta 0 filas, excepción (D-171, P-4).
- [x] `UserAdminService.SetArea(adminId, userId, int? areaId)`: verifica que quien llama es
      admin (`UnauthorizedAccessException`, como el resto), que la cuenta existe y es Operator y
      que el área es elegible; `null` la quita (D-172, P-2, P-5). Para leer las elegibles en los
      tests, la misma costura que `findEmployee` (un `Func` o una interfaz pequeña) (enmienda 2: `null` → excepción; ya no la quita).
- [x] Tests NUnit en memoria:
  - `TrainingAccessRuleTests`: guía inexistente, no publicada, otra área, operador sin área,
    administrador y el caso válido;
  - `OperatorAreaServiceTests`: elige bien, ya tenía área, área no elegible, administrador, carrera
    (el repositorio devuelve 0 filas);
  - `UserAdminServiceTests`: `SetArea` con quien no es admin, cuenta admin, cuenta inexistente, área
    no elegible, null (excepción) y cambiar.
- [x] Verificación (Desarrollo): Rebuild de `TrainingApp` y `TrainingApp.Test`; `vstest.console`
      con todo en verde (`ScoreCalculatorTests` incluido).
- [x] `/code-review medium` sobre los archivos de la BLL. Foco: validar antes de abandonar, que
      ninguna regla confíe en la página, admin verificado en la BLL y lo que cruza usuarios
      (`SetArea` sobre otra cuenta).
- [x] B-4 (enmienda 1, usuario 2026-10-27): `StartSession` rechaza con excepción un modo de sesión no definido antes de `Start`; antes abandonaba la sesión en curso y guardaba una sesión que fallaba al cargar (POST manipulado). Con su test.

**Hecho cuando:** los tests pasan; desde la app (Rebuild + F5), un operador sin área ya no puede
iniciar ninguna guía (la página de error, esperado hasta la E3); con un área puesta a mano en la
BD, solo inicia las guías de esa área. B-20 queda corregido.

## E3 · UI del operador: "Elige tu área" e inicio

- [x] `Shared/Constants.cs`: `SessionKeys.HasArea`. `SessionHelper`: `SetUserSession` la escribe
      (única escritura, P-3); `MustChooseArea()`; `ChooseAreaUrl`; `RefreshUserSession()` (relee el
      usuario y llama a `SetUserSession`).
- [x] `Pages/Training/ChooseArea.aspx` (`Site.Master`, P-10): `EnsureUserSession` →
      `CanTrain` → `MustChangePassword`; con área, al inicio (AC-5).
  - Un **solo selector agrupado por producto** (D-169): un `<select>` con `<optgroup>` por
    producto, desde `GetEligibleAreas()`. `DropDownList` no tiene `optgroup`: la página lo
    arma con un `Repeater` y lee el valor del form; la BLL valida que el área sea elegible, así
    que no depende de EventValidation.
  - Continuar → confirmación en el cliente ("¿Tu área es *Producto · Área*? Después solo tu
    trainer podrá cambiarla.", Sí / No) con la pieza de confirmación del sistema de diseño. Si
    `Site.Master` no carga la de `Admin.Master`, se lleva a un JS común. Sí → un postback →
    `ChooseOwnArea` → `RefreshUserSession` → inicio (D-177, AC-3).
- [x] `StartSession.aspx`: tras `MustChangePassword`, `MustChooseArea` → `ChooseArea` (D-169).
      Con área: "*Producto · Área*" como texto y solo el selector de guía (y los modos); se quitan
      los selectores de Producto y Área (D-167). Sin área (el admin la quitó durante la Session):
      el aviso de D-168, sin selector ni Iniciar. Sin guías publicadas en su área: el mensaje
      actual (P-6).
- [x] `GuidedTest.aspx.cs`: la misma redirección tras `MustChangePassword` (D-169). La sesión en
      curso de otra área sigue (D-174, AC-7).
- [x] Verificación (Desarrollo): Rebuild; `aspnet_compiler` (markup tocado); tests.
- [x] `/code-review low --max-findings 5` sobre `SessionHelper` y los code-behind. Foco: orden
      de las redirecciones, Session escrita solo en `SetUserSession`, nada de lógica en la página.
- [x] Checklist del navegador: AC-1 a AC-5, AC-7 (operador nuevo, operador existente sin área,
      No en la confirmación, abrir `ChooseArea` con área, sesión en curso de otra área), en claro
      y oscuro.

**Hecho cuando:** el usuario prueba el flujo completo de un operador nuevo y de uno existente y
dice "funciona".

## E4 · UI del admin: Usuarios y B-3

- [x] `Users.aspx`: columna **Área** ("Producto · Área", "Sin área", "—" para un admin; P-9).
- [x] Por fila de Operator, en la columna Área: un botón de lápiz junto al texto que abre ahí el selector agrupado, con Guardar (`adminConfirm`) y Cancelar, sin postback hasta Guardar (D-172 enmendada por D-208, P-11). Sin Quitar área. Acciones queda con Resetear, Desbloquear y Borrar. Las filas de admin no muestran el lápiz (AC-10).
- [x] B-3: un `UnauthorizedAccessException` de la BLL (todas las acciones de la página,
      Cambiar y Quitar área incluidas) manda a `SessionHelper.RedirectHome`, no al login
      (AC-6b). Si B-3 ya se cerró por la E2 de la 018, se marca sin efecto. **Sin efecto:** ya
      lo hacía el `OnError` de `Users.aspx.cs` (`RedirectHomeAfterRoleChange`, `33a6a9d`), que
      cubre también Cambiar y Quitar área.
- [x] Verificación (Desarrollo): Rebuild; `aspnet_compiler`; tests.
- [x] `/code-review low --max-findings 5` sobre `Users.aspx.cs`. Foco: ids de fila, B-3, que la
      acción no salga para un admin.
- [x] Checklist del navegador: AC-6, AC-6b y AC-10 (cambiar el área y ver el efecto en su inicio; la fila de un admin sin lápiz), en claro y oscuro.

**Hecho cuando:** el usuario cambia y quita el área de un operador y ve el efecto en su inicio.

## E5 · Dashboard y cierre

- [x] Operadores: columna **Área** en la lista y en su Excel (`ExcelReports.Operators`), con el
      formato de la Fase 11 (D-175, P-9).
- [x] Operador: el área en el encabezado y en la ficha imprimible.
- [x] Verificación (Desarrollo): Rebuild; `aspnet_compiler`; tests (`ExcelReportsTests` y
      `DashboardServiceTests` siguen en verde; si comparan columnas, se actualizan).
- [x] Sin `/code-review` si solo es binding y markup (`process.md` → 3); si se toca
      `DashboardService`, `low` con foco en que filtros y números no cambian.
- [x] Checklist del navegador: AC-9 y AC-11 (lista, Excel abierto en Excel, detalle y ficha
      impresa; operador sin área = "Sin área").
- [x] Docs de cierre (Desarrollo, P-12): `docs/domain.md` (sección "Área del operador" y sin "NO
      USAR EN PRODUCCIÓN" en Redefinity), `docs/architecture.md` → "Roles, login y gestión de
      usuarios" (el paso "Elige tu área" y `MustChooseArea`), `CLAUDE.md` → "Sesión y
      seguridad" (el orden `EnsureUserSession` → `MustChangePassword` → `MustChooseArea` en
      Training), `docs/decisions.md` (B-20 y B-3 corregidos), `docs/status.md` y
      `docs/history.md`. Avisar a Arquitectura y Planeación para el roadmap.

**Hecho cuando:** el usuario confirma el dashboard y la spec pasa a Implemented.
