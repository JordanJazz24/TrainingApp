# Auditoría del proyecto — 2026-10-01 / 2026-10-02 ✅

> Movida desde `docs/roadmap.md` el 2026-10-06 sin cambios de contenido. El estado del
> proyecto vive en `docs/roadmap.md` → "📍 Estado actual". La auditoría siguiente
> (`2026-10-06-auditoria-final.md`) continúa la numeración: hallazgos desde **AUD-33** y
> decisiones desde **D-13**.

## Resultado

Fases A, B y C ✅. Las 5 entregas chicas (E1 bugs del flujo, E2 auth y secretos, E3
integridad del puntaje, E4 capas, E5 limpieza) se verificaron en el navegador y se
integraron a `TrainingApp` (fast-forward desde la rama `audit/fase-a-hallazgos`,
2026-10-02); el usuario también actualizó `main` con `TrainingApp` y le hizo push. El build
quedó en 0 warnings. **Los tests quedaron fuera** por decisión del usuario.

**Lo que sigue abierto:**
- ✅ **Spec 009 · motor del POD** (AUD-16, 25, 26), cerrada 2026-10-05: la lógica del POD
  en la BLL; el estado sigue en Session y se reconstruye si se pierde (D-10).
- ☑ **Fase 9, E2 (2026-10-22, enmienda):** se hizo lo barato de AUD-28 y los parches de AUD-29 (D-131); Bootstrap, SweetAlert2 y CodeDom 4.x quedaron fuera, y la cabecera `Server` espera la versión de IIS de TI (D-144). Original: **Fase 9 (o cuando el usuario lo pida explícitamente, D-7):** AUD-28 (endurecimiento
  del deploy) y AUD-29 (actualizar paquetes).
- **Enmienda (2026-10-23, Fase 10, spec 018):** el inventario de paquetes de AUD-29 suma:
  - **Chart.js 4.5.1:** local en `Scripts/chart-4.5.1.umd.min.js`, sin CDN.
  - **ClosedXML 0.105.1 (D-140)**, con sus dependencias por `packages.config`:
    DocumentFormat.OpenXml 3.1.1, ExcelNumberFormat, SixLabors.Fonts y `System.*`, con 4
    `bindingRedirect` en `Web.config`.

  Tenerlos en cuenta en la próxima revisión de paquetes. La descarga grande de sesiones no
  usa `System.IO.Packaging` (D-149).
- **Acción del usuario, en su momento (D-8):** rotar la contraseña de `sa` (AUD-04; sigue
  en el historial de git) y actualizar `connectionStrings.config`.
- AUD-27 quedó cerrado por decisión del usuario (D-6): la pregunta de Line Clearance se
  queda en el código.

## Lista única de hallazgos

**Cómo se hizo (Fase A, solo lectura):** `health-check`, `arch-check` + revisión de diseño
manual, `security-scan`, `/code-review high` sobre el código commiteado (BLL, DAL, Pages,
UI/Controls), `outdated` (contra la API de NuGet y su base de vulnerabilidades) y análisis
de código muerto (Roslyn + grep sobre markup, JS, CSS y SPs). Los tests quedaron fuera por
decisión del usuario. Es análisis estático: no reemplaza una prueba de penetración.

**Notas del health-check** (rúbrica estricta del kit): Build B (1 warning, CS0067) ·
Calidad C (2 `catch {}` reales) · Arquitectura C (2 violaciones de capa, 0 ciclos) ·
Código muerto C (~15 símbolos C#) · Superficie pública C · **Seguridad F** (la rúbrica
da F por cualquier secreto versionado; se debe a un solo hallazgo, AUD-04) ·
Documentación D · Tests: no evaluado. GPA 1.7; sin AUD-04 sube a ~2.1.

**Limpio, no tocar:** la DAL (todo pasa por SPs vía `Database.cs`, sin SQL inline ni
`DataSet`), el Domain (POCOs puros), PBKDF2 (100k iteraciones, sal de 16 bytes,
comparación en tiempo constante), la codificación de salida en casi todos los `Literal`,
ningún `[WebMethod]`, ningún ciclo entre tipos, los 20 SPs en uso, ningún paquete con CVE
conocido.

**Falsos positivos, no volver a reportar:** `TimeProvider`/`DateTime.UtcNow`, el
`HttpClient` estático, el `GetResult()` con `ConfigureAwait(false)`, el
`catch (Exception)` de `EmployeeRepository` (es el fallback `_lastKnownGood`), los 30
handlers que `find_dead_code` marca (están enlazados en el markup), `Operation` y los
`OperationId` (entidad futura), `SessionStatus.Abandoned` (se lee desde la BD).
**Corrección de la revisión de diseño:** sin `customErrors` el default es `RemoteOnly`
(un usuario remoto no ve el stack trace), y `Web.Release.config` ya quita `debug`.

**Severidad:** Alto = rompe el flujo, el puntaje o expone un secreto · Medio = riesgo
real en condiciones normales · Bajo = higiene o caso raro · Info = sin acción o por
diseño.

| ID | Sev. | Hallazgo | Dónde | Arreglo propuesto | Vía |
|---|---|---|---|---|---|
| AUD-01 | Alto | Volver a iniciar una guía que quedó `InProgress` con intentos falla con error 547 (FK): el SP hace `DELETE` de la sesión y los intentos la referencian sin cascada. El operador no puede volver a esa guía. | `03_spTrainingSession.sql:130`, `TrainingSessionService.cs:31` | El SP marca la sesión vieja como `Abandoned` en vez de borrarla (conserva el historial). **D-4** | E1 ✅ |
| AUD-02 | Alto | Después de activar la cuenta, `ChangePassword` redirige a `Pages/Training/Dashboard.aspx`, que no existe: todo operador nuevo termina en un 404. | `ChangePassword.aspx.cs:37` | Redirigir a `StartSession.aspx`. | E1 ✅ |
| AUD-03 | Alto | Un SFC Split forzado (4 fallos en Assessment) no deja ninguna fila del split, y en NonSerialized Log NC exige una: el paso queda imposible y se pierde otro 10%. | `GuidedTest.aspx.cs:528` (`RestorePodAfterForcedAdvance` no tiene caso `SFC_Split`) | Agregar el caso: un split por defecto, como ya se hace con Resource Setup y Start forzados. **D-1** | E1 ✅ |
| AUD-04 | Alto | `Web.config` está versionado (y en `origin`) con la cuenta `sa` y su contraseña en texto plano. | `Web.config:9` | Sacar la cadena a un archivo no versionado (`configSource` + `.gitignore` + plantilla de ejemplo), usar un login SQL de mínimo privilegio y **rotar la contraseña de `sa`** (queda en el historial de git). | E2 ✅ |
| AUD-05 | Alto | `SubmitStep` no valida que la sesión esté `InProgress`, que el paso sea el actual ni quién es el dueño. Con un `stepId` viejo, `UpdateCurrentStep(step.Order + 1)` haría retroceder la sesión. Hoy lo protege solo el code-behind. | `TrainingSessionService.cs:65` (y los otros métodos que llaman a `RecordAttempt`) | Guard en la BLL: estado, paso actual y operador. No cambia reglas. | E3 ✅ |
| AUD-06 | Alto | Registrar un intento y avanzar el paso son dos escrituras sin transacción (no hay ninguna en el proyecto). Si falla la segunda, el siguiente envío cuenta como intento 2 y el operador queda penalizado injustamente. | `TrainingSessionService.cs:173-221` | Un SP que haga el insert del intento y el avance/cierre dentro de `BEGIN TRAN`. | E3 ✅ |
| AUD-07 | Medio | Guided aplica la penalización por intento (80/60/0%), pero `CLAUDE.md` dice que Guided no penaliza. | `ScoreCalculator.cs:33` | Alinear el código o el `CLAUDE.md`. Toca una regla de puntaje. **D-2** | E3 ✅ |
| AUD-08 | Medio | No se exige el cambio de contraseña del primer acceso: después del login con la contraseña por defecto, el operador puede ir directo a `StartSession` y la cuenta sigue con `Sanjose1!`. | `StartSession.aspx.cs`, `GuidedTest.aspx.cs`, `SessionHelper` (nadie lee `IsFirstLogin` después del login) | Guardar `IsFirstLogin` en Session y redirigir a `ChangePassword` mientras sea `true`. | E2 ✅ |
| AUD-09 | Medio | `ChangePassword` no pide la contraseña actual y está disponible siempre: en una estación con la sesión abierta, cualquiera cambia la contraseña. La regla "no puede ser la por defecto" vive en el code-behind. | `ChangePassword.aspx.cs:23,38`, `AuthService.ActivateAccount` | `AuthService.ChangePassword(operatorId, actual, nueva)` con las reglas en la BLL. **D-3** | E2 ✅ |
| AUD-10 | Medio | Si la Session expiró pero el login sigue vigente, Save en `ChangePassword` redirige sin guardar ni avisar, y el operador entra a la app con la contraseña por defecto. | `ChangePassword.aspx.cs:29` | Usar `SessionHelper.EnsureOperatorSession()`, como las demás páginas. | E2 ✅ |
| AUD-11 | Medio | El `HfRows` que manda el cliente se mete sin escapar en un `<script>` (`rsRestoreRows(...)`) y se deserializa sin `try/catch`: un valor con `</script>` rompe la página (XSS propio) y un JSON mal formado da una página de error. | `ResourceSetupControl.ascx.cs:102,130` | Re-serializar lo ya parseado; JSON inválido = aviso de formato (sin intento). | E2 ✅ |
| AUD-12 | Medio | La Session dura 20 min (no hay `<sessionState>`) y el login 60 con renovación: tras 20 min sin actividad el POD se vacía a mitad de una evaluación. | `Web.config` | `<sessionState timeout="60" />` (arreglo rápido; el de fondo es AUD-26). | E3 ✅ |
| AUD-13 | Medio | El `StepValidator` se cachea 60 min por área con los flags y catálogos; la página los lee frescos. Tras correr un script de catálogo, la UI y la calificación no coinciden (ej. cambiar `RequiresStartChecklist` deja a Guided atascado en Start). | `TrainingSessionService.cs:237` | Quitar la caché (armarlo es barato) o invalidarla. | E1 ✅ |
| AUD-14 | Medio | `GuidedTest` usa 4 repositorios de la DAL directamente (ya estaba en la deuda). | `GuidedTest.aspx.cs:23-26` | Pasar por `TrainingGuideService` (`GetArea`, `GetNCCodes`, `GetDataCollectionParameters`). | E4 ✅ |
| AUD-15 | Medio | `SessionHelper` (en `Shared`) usa `OperatorRepository` y `System.Web`: las páginas llegan a la DAL sin pasar por la BLL y se forma un ciclo `Shared` ↔ DAL. | `SessionHelper.cs:30` | `AuthService.GetOperatorByCode1` y mover `SessionHelper` a la capa de presentación. | E4 ✅ |
| AUD-16 | Medio | `GuidedTest.aspx.cs` (1,407 líneas) es el motor real: modifica la orden simulada en el split y decide cuándo una selección de piezas es intento fallido (en NonSerialized, "Complete sin fila" está escrito en la página). | `GuidedTest.aspx.cs:379, 417-440, 655, 726, 867, 915-933` | Un servicio de la BLL dueño de la orden y del avance (ej. `PodService`), con comandos Start/Split/Complete. Incluye AUD-25 y AUD-26. | Spec 009 ✅ |
| AUD-17 | Bajo | No hay protección CSRF: ni `ViewStateUserKey` ni el token anti-XSRF del template. | `Site.Master.cs`, `Training.master.cs` | `ViewStateUserKey = Session.SessionID` en `Page_Init` de los dos masters. | E2 ✅ |
| AUD-18 | Bajo | `EscapeJs` no escapa `\`, `\r` ni `</script>`. Hoy solo recibe mensajes del servidor. | `GuidedTest.aspx.cs:1385` | `HttpUtility.JavaScriptStringEncode`. | E2 ✅ |
| AUD-19 | Bajo | Assembly muestra "Component X assembled" aunque la BLL haya rechazado el componente (id fuera del BOM). | `GuidedTest.aspx.cs:319` | Revisar `AssemblySubmitResult.ComponentAssembled`, que hoy nadie lee. | E1 ✅ |
| AUD-20 | Bajo | `catch {}` vacíos al leer el JSON de las respuestas. | `StepValidator.cs:120,151` | Capturar solo `JsonException`. | E1 ✅ |
| AUD-21 | Bajo | Código muerto C#: `BuildMoldeoLCQuestions`, `ScoreCalculator.CalculateStepScore` (ya borrado en E3), `StepValidator.ValidateDataCollection` (inalcanzable y más laxo que el de grupos), 6 propiedades de `StepViewModel` (`StepId`, `StepType`, `StepOrder`, `Hint`, `IsCorrect`, `AttemptCount`), `ComponentAssembledEventArgs.Barcode`, `ExtraData["AreaName"]`, `.mes-ico-retrieve`, los `Page_Load` vacíos de los masters y el evento de `CompleteControl` (CS0067). | Varios | Borrar. **Antes revisar:** `StepSubmitResult.CurrentScore` (también sin lectores) conviene usarlo (AUD-25), no borrarlo. | E5 ✅ |
| AUD-22 | Bajo | El proyecto **web** referencia `Microsoft.ApplicationInsights` (sin uso) y 6 paquetes de testing (`Microsoft.Testing.*`, `TestPlatform.ObjectModel`), que además generan un `Main` en `obj/`. | `packages.config`, `TrainingApp.csproj:3-5, 50-77` | Quitarlos con NuGet (tienen dependencias transitivas). | E5 ✅ |
| AUD-23 | Bajo | Restos del template de VS: `Site.Master` muestra "Application name" y enlaces a `~/About` y `~/Contact` (404); ~45 archivos estáticos sin uso (variantes de Bootstrap/jQuery, MsAjax, Menu, SmartNav); Modernizr y los bundles MsAjax sin uso; jQuery probablemente cargado dos veces en el POD; 2 líneas basura en `.gitignore`; ~16 `using` sin uso. | `UI/master/Site.Master`, `Content/`, `Scripts/`, `App_Start/BundleConfig.cs`, `.gitignore` | Limpiar. Conservar `WebForms.js`, `WebUIValidation.js`, `Focus.js` y el mapping `jquery` (los usan los validadores). | E5 ✅ |
| AUD-24 | Bajo | Los data-binding usan `<%# Eval(...) %>` sin codificar. Hoy los datos vienen de la BD o son números validados. | `GuidedTest.aspx:153-166, 342-350`, `DataCollectionControl.ascx`, `LogNCControl.ascx` | `<%#: ... %>` por consistencia. | E5 ✅ |
| AUD-25 | Bajo | Unas 15 conexiones a SQL por click en el POD: `GetContext` recarga sesión, guía y área; `UpdateScoreWidget` vuelve a consultar; `RecordAttempt` calcula el puntaje y lo descarta. | `GuidedTest.aspx.cs`, `TrainingSessionService.cs:195-224` | Usar `StepSubmitResult.CurrentScore` y cargar el contexto una vez por request. | Spec 009 ✅ |
| AUD-26 | Bajo | La orden simulada y el avance de DC/Assembly viven solo en la Session (ya estaba en la deuda, spec 008 E7). | `GuidedTest.aspx.cs`, `Constants.SessionKeys` | ~~Persistirlos en BD por sesión.~~ Siguen en Session; si faltan, la BLL los reconstruye (**D-10**). | Spec 009 ✅ |
| AUD-27 | Bajo | La pregunta de Line Clearance está en el code-behind, contra "datos de simulación en SQL" de `CLAUDE.md`. | `GuidedTest.aspx.cs:1030` | Sin acción: la pregunta es la misma en todas las operaciones (D-6). | Cerrado (D-6) |
| AUD-28 | Bajo | Endurecimiento para el deploy: cookies sin `requireSSL`, sin cabeceras de seguridad, `X-AspNet-Version` expuesto, el ID de Session no se renueva en el login, no hay `Application_Error` ni registro de errores o de logins fallidos, y el login no limita intentos. Sin `customErrors`, un valor con marcado en cualquier campo de texto libre (ej. Data Collection) muestra la pantalla de `HttpRequestValidationException` (Resource Setup ya lo bloquea en el cliente, E2). En IIS, el `idleTimeout` del app pool debe ser de al menos 60 min: si el proceso se detiene por inactividad, la Session InProc se pierde aunque su timeout sea 60 (E3). | `Web.config`, `Global.asax.cs`, `Login.aspx.cs` | Checklist de la Fase 9. | ☑ Fase 9, E2 (2026-10-22) |
| AUD-29 | Bajo | Paquetes atrasados, ninguno con CVE: parches de Newtonsoft (13.0.4), jQuery (3.7.1), `Microsoft.Web.Infrastructure` (2.0.1) y `System.*`; mayor de CodeDom (2.0.1 → 4.1.0). Bootstrap 5.3.8 y SweetAlert2 11.26 pueden cambiar la UI. | `packages.config`, `Scripts/`, `UI/master/Training.Master` | Uno por uno; Bootstrap y SweetAlert después de la 6.6, revisando la UI. | ☑ Fase 9, E2 (2026-10-22) |
| AUD-30 | Info | `origin/main` todavía contiene la app legacy (con `packages/`, `bin/` y `.vs/` versionados); `TrainingApp` va 44 commits adelante sin merge. Los worktrees salen de `main` por defecto y fallan por rutas largas o parten del código equivocado. | Repo | No se toca `main` (D-5); los worktrees deben salir de `TrainingApp`. El 2026-10-02 el usuario actualizó `main` con `TrainingApp`, así que `main` ya no tiene la app legacy. | Resuelto (D-5) |
| AUD-31 | Info | 4 XML docs de ~300 miembros públicos y no hay README. `CLAUDE.md` y `docs/` cubren ese papel. | — | Sin acción. | — |
| AUD-32 | Info | Por diseño (no tocar): contraseña por defecto + autoregistro por `code1` (quien conozca el `code1` de alguien que nunca entró puede activar su cuenta); mensajes distintos para `code1` inexistente y contraseña incorrecta (permite enumerar `code1`). | `AuthService`, `Login.aspx.cs` | Riesgo aceptado (`CLAUDE.md` → Auth Flow). | — |

## Decisiones del usuario

D-1 a D-8 cerradas el 2026-10-02; D-9 a D-12 salieron de la spec 009 (2026-10-05) y siguen
la misma numeración.
- **D-1 (AUD-03):** un SFC Split forzado deja una fila de split por defecto de **100
  unidades** (la orden simulada NonSerialized es de 250), igual que la orden por defecto
  de Resource Setup forzado.
- **D-2 (AUD-07):** **Guided no penaliza**, es práctica: `CLAUDE.md` es correcto y se
  corrige el código (acertar en cualquier intento = 100% del paso en Guided).
- **D-3 (AUD-09):** el cambio de contraseña **pide siempre la contraseña actual**; en el
  primer acceso la actual es la por defecto.
- **D-4 (AUD-01):** la sesión vieja se **marca `Abandoned`** (conserva los intentos).
- **D-5 (AUD-30):** **no se toca `main`**: `TrainingApp` es la rama de desarrollo y a
  `main` solo se suben los cambios finales. Los worktrees deben salir de `TrainingApp`
  (no de `origin/main`); conviene configurar `worktree.baseRef: head` en Claude Code.
- **D-6 (AUD-27):** la pregunta del paso Line Clearance **se queda en el código**: es la
  misma en todas las operaciones (excepción anotada en `CLAUDE.md` → Key Conventions).
- **D-7 (AUD-28, AUD-29):** el endurecimiento del deploy y la actualización de paquetes se
  ven **en la Fase 9**, o antes solo si el usuario lo pide explícitamente.
- **D-8 (AUD-04):** el usuario rota la contraseña de `sa` **en su momento**.
- **D-9 (spec 009, 2026-10-02):** **una sola sesión activa por operador**: iniciar cualquier
  guía o hacer logout abandona las sesiones `InProgress` del operador. No en el login ni
  al cerrar la ventana (el servidor no se entera).
- **D-10 (AUD-26):** el estado del POD **sigue en Session**; si falta, la BLL lo
  **reconstruye en silencio** con la orden por defecto en el paso actual, y Assembly/DC a
  medias se rehacen desde cero sin penalización. No se persiste en BD (sería
  sobreingeniería sin la opción de retomar sesiones, que no existe por decisión del usuario).
- **D-11 (spec 009):** los datos de simulación de `GuidedTest` (Shop Order por defecto, split
  de 100, lote de 250, Site, recurso, `LOT-yyyyMMdd`) pasan a **constantes de la BLL**, con
  los mismos valores.
- **D-12 (spec 009):** la spec 009 es un **refactor con cero cambios visibles**; los bugs que
  aparezcan se deciden aparte. La excepción es D-9.

## Entregas (Fase C)

| Entrega | Contenido | Vía | Revisión |
|---|---|---|---|
| E1 · Bugs del flujo ✅ | AUD-01, 02, 03, 13, 19, 20 | Plan directo (D-1, D-4); AUD-01 incluye un script SQL que corre el usuario | `/code-review` |
| E2 · Auth y secretos ✅ | AUD-04, 08, 09, 10, 11, 17, 18 | Plan directo (D-3); AUD-04 requiere que el usuario rote la contraseña | `/security-review` |
| E3 · Integridad del puntaje ✅ | AUD-05, 06, 07, 12 | Plan directo: toca el motor sin cambiar reglas, salvo AUD-07, que alinea el código con `CLAUDE.md` (D-2); AUD-06 incluye SP nuevo | `/code-review` |
| E4 · Capas ✅ | AUD-14, 15 | Mecánico, solo enrutamiento | `/code-review` |
| E5 · Limpieza ✅ | AUD-21, 22, 23, 24 | Mecánico | `/code-review` |
| Spec 009 · motor del POD ✅ | AUD-16, 25, 26 | `/spec` aprobada 2026-10-05; plan `docs/specs/009-plan.md` (6 entregas) | `/code-review` por entrega |

**Pendientes menores de E5, dejados a propósito:** las `ScriptReference` del template en `Site.Master` (MenuStandards, GridView, DetailsView, TreeView, WebParts y los bundles MsAjax) siguen cargándose aunque ninguna página las use, porque quitarlas tiene riesgo sobre los validadores y casi ningún beneficio. Reinstalar los paquetes de contenido (`bootstrap`, `jQuery`, `ScriptManager.MSAjax`) devolvería los archivos que E5 borró, así que habría que borrarlos de nuevo. Separar `IStepControl` (los controles que nunca envían dejan el evento con accessors vacíos) va con la spec del motor.

Ninguna entrega toca la fidelidad de UI de la 6.5 (respeta su "No incluye"). Proceso de
cada entrega: implementar → `/dotnet-claude-kit:verify` con MSBuild Rebuild y
`aspnet_compiler` (no `dotnet build`; sin tests, por la decisión del usuario) →
`/code-review` sobre el diff, o `/security-review` si toca auth o Session → checklist en
el navegador → el usuario confirma → commit.
