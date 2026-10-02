# Roadmap — MESTrainingSimulator

> Historial completo de fases, extraído de `CLAUDE.md`. El resumen operativo (fase actual + siguiente) vive en `CLAUDE.md` → sección Roadmap.

Cada fase tiene un alcance cerrado. No mezclar tareas de otras fases — la
causa principal de deuda técnica temprana es que features de fases futuras
se filtran en fases anteriores.

---

## 📍 Estado actual — dónde quedamos

> **Leer esta sección al iniciar cada sesión.** Actualizarla al cerrar una fase o
> una sesión de trabajo relevante.

**Última actualización:** 2026-10-01 · **Branch:** `TrainingApp` · **Último commit de código:** entrega 8 de la Fase 6.5 (`git log --oneline -5` para ver el estado real)

- **Fase 6 cerrada ✅** (2026-09-24). El flujo completo de R&C/Moldeo (NonSerialized)
  funciona de punta a punta en Guided y Assessment (penalización, force-advance y
  aprobado/reprobado), verificado en navegador por el usuario.
- **Assessment real (S1 + S4, sin spec):** al probar Assessment se vio que el 50% del
  puntaje nunca podía fallar porque los controles rechazaban las decisiones antes de
  enviarlas. Se separó formato de decisión (ver `CLAUDE.md` → Key Conventions): LC "No",
  Setup State "Open" y los "No" de Start ahora llegan a `StepValidator` y cuentan como
  intento. En Assessment el mensaje de error no revela la respuesta (muestra los intentos
  restantes). El 70% de aprobación pasó a `ScoreCalculator.PassingScore`. Los banners de
  guardado (Record change, DC group saved) solo aparecen si el servidor aceptó el paso.
  Verificado en navegador por el usuario (2026-09-25).
- **S2 — secuencia en Assessment (sin spec, decisiones cerradas con el usuario 2026-09-25):**
  - En Assessment todos los botones del POD están habilitados, incluidos los que no aplican
    al área (Assemble en Moldeo, SFC Split en Serialized). Guided sin cambios.
  - Un click en un botón que no es el del paso actual es un intento fallido **del paso
    actual** (`TrainingSessionService.RegisterOutOfSequence`), con la misma penalización y
    force-advance. El control del botón equivocado no se abre.
  - Mismo mensaje genérico que una respuesta incorrecta (no revela que fue de secuencia).
  - La secuencia se evalúa antes que los avisos de formato del botón (SFC vacío, fila sin
    seleccionar).
  - Activities → LC entra en la regla; Browse Work List, selección de filas, Abandon y
    Logout son navegación y nunca cuentan.
  - Las acciones de MES sin lógica (Work Instructions, Reports, Place Back in Queue, Tool
    List y las 7 opciones de relleno de Activities) también se habilitan en Assessment y
    su click es siempre un intento fallido del paso actual.
  - Resource Setup forzado → siempre la orden por defecto (Shop Order genérica `303406950`),
    ignorando lo capturado en intentos fallidos (spec 008, E8).
    Start forzado → el POD se carga como si Start se hubiera ejecutado.
  Verificado en navegador por el usuario (2026-09-25).
- **Spec 008 cerrada ✅** (2026-09-28, 5 entregas + cierre, plan en
  `docs/specs/008-plan.md`): el motor ya soporta áreas Serialized.
  - Resource Setup genera 3-5 piezas con SFC propio; Browse Work List y POD con
    multi-selección; `TxtSFC` con el SFC o `[N SELECTED]`; Start directo cuando el área
    no tiene checklist (`Areas.RequiresStartChecklist`, hoy solo Moldeo = 1).
  - Assembly con el BOM de la operación (`AssemblyComponents`, simulado): Component List,
    modal replicado del MES real, Batch Number = Process Lot. Se eliminó
    `ExpectedComponentCount`.
  - Data Collection por pieza con grupos obligatorios (`DataCollectionGroups.IsRequired`),
    indicador `SFC: … (n/N)` y avance conservado. **Moldeo ahora exige** su grupo de
    fechas de expiración para completar DC.
  - Log NC y Complete multi-fila. En Serialized la selección parcial es aviso sin
    penalización (el motor trabaja todas las piezas juntas; ver Deferred Decisions de la
    spec: recorrido pieza por pieza).
  - Verificado en navegador por el usuario: Redefinity de punta a punta y regresión de
    Moldeo en Guided y Assessment.
- **Fase 6.5 — Fidelidad de UI/UX con el SAP MES real 🔄 (en curso, formalizada
  2026-09-29).** Nació como mini-fase de ajustes de UI tras la spec 008. Objetivo:
  dejar cerrada la interfaz antes de los overlays guiados (Fase 6.6), que se anclan a
  ella. Detalle, alcance y criterio de cierre abajo, en "Fase 6.5".
  - **Entregas 1-9 ✅** (2026-09-29 a 2026-10-01, verificadas por el usuario; plan con
    casillas y decisiones en `docs/specs/fase-6.5-plan.md`): columna SFC y Select All;
    Collect deshabilitado por pieza (enmienda de E3, spec 008); Shop Order / Status (SFC);
    layout y colores del MES real (header y POD Search en una franja clara, Code1 en el
    header); header estándar de los 12 modales; Resource Setup; botones grises del MES
    (`.mes-btn` + `.mes-ico-*`); Line Clearance con submodal apilado; listas (Component,
    DC, NC Selection); DC Entry y NC Data Entry, con la columna nueva
    `DataCollectionParameters.Description` (Moldeo con los textos del legacy) y
    `SFC (1/N)` + "Apply to all SFCs" en NC Data Entry; SFC Split como la captura real.
  - **Regla cambiada dentro de la fase, por decisión del usuario:** el SFC Split crea un
    **SFC nuevo** de la misma Shop Order (`BLL/Training/SplitSfcGenerator`; enmienda
    2026-10-01 de la spec 005).
  - **Inventario de pantallas** (criterio de cierre): `docs/ui-reference/inventario-pantallas.md`
    — ✅ 14 · 🟡 0 · ⚪ 2. **Criterio de cierre cumplido (2026-10-01):** todas las
    pantallas con captura están fieles (POD y Browse con diferencias menores aceptadas por
    el usuario). Sin captura, sin bloquear: los 4 modales del Start de Moldeo y el
    resultado de Complete.
  - **Ahora:** confirmar con el usuario el cierre de la 6.5 y pasar a la spec de la 6.6.
- **Después — Fase 6.6 · Overlays guiados** (requiere `/spec`), sobre la interfaz ya
  cerrada. Ver "Fase 6.6" abajo.
- **Fase 7 (LoFlo)** solo espera el bloqueante #3: los flags reales de LoFlo, a
  confirmar con negocio. Con eso, la Fase 7 vuelve a ser "solo datos"; no compite con la
  6.5 ni con la 6.6.
  **Qué pedirle a negocio** para escribir el script de LoFlo:
  1. Flags: `RequiresAssembly` (BD = false, CLAUDE.md = true), `RequiresDataCollection`,
     `RequiresNCComponent`.
  2. DC Groups de la operación que se entrena, **cuáles son obligatorios**, y sus
     parámetros (tipo, unidad, rangos).
  3. Códigos NC de LoFlo.
  4. BOM (números de parte y cantidades): puede seguir **simulado** (decisión del usuario).
- **Auditoría del proyecto (2026-10-01): Fase A ✅ · Fase B ✅ · Fase C 🔄.**
  Los hallazgos están en una sola lista priorizada, con severidad, vía y entrega, más
  abajo en "Auditoría 2026-10-01". Decisiones D-1 a D-5 cerradas con el usuario
  (2026-10-02). **E1 ✅** (bugs del flujo) y **E2 ✅** (auth y secretos), 2026-10-02,
  verificadas por el usuario. **Siguiente:** E3 (integridad del puntaje). Las entregas
  se acumulan en la rama `audit/fase-a-hallazgos` y se integran a `TrainingApp` al final
  (decisión del usuario). Cada entrega sigue el proceso
  de la Fase C de la auditoría. **Los tests quedan fuera de la
  auditoría y de sus correcciones** por decisión del usuario.
- **Mientras llega la respuesta de negocio** (opciones, sin orden decidido): entregas de
  la auditoría (Fase C); Fase 5 (tests de `ScoreCalculator`/`StepValidator`, el
  usuario la quiere al final). Preguntar al usuario antes de elegir.

### Cómo retomar en una sesión nueva
1. Leer esta sección y `git log --oneline -10` (branch `TrainingApp`; no hay push pendiente
   de pedir: el usuario hace push cuando quiere).
2. La BD de desarrollo del usuario ya tiene el esquema de la spec 008 (ALTER entregado y
   corrido el 2026-09-28) y la columna `DataCollectionParameters.Description` (Fase 6.5,
   entrega 8: ALTER + `02`, `05` y `06` corridos el 2026-10-01). Una instalación limpia
   se arma con los scripts `00`–`07` en orden. Auditoría E1 (2026-10-02): el usuario corrió
   `03` (`spTrainingSessionDeleteActiveByOperator` → `spTrainingSessionAbandonActiveByOperator`).
   Auditoría E2: la cadena de conexión vive en `TrainingApp/connectionStrings.config` (no se
   versiona); en un checkout nuevo se copia de `connectionStrings.config.example`.
3. **Proceso acordado:**
   - Funcionalidad o reglas de negocio nuevas → `/spec` → aprobación explícita → `/plan`
     (guardado en `docs/specs/NNN-plan.md`, en entregas chicas) → implementación.
   - Bugs, robustez y cambios con pocas decisiones acotadas → plan directo en el chat,
     con una ronda de preguntas si hace falta (así se hicieron el arreglo de F5, S1 y S2).
   - Cada entrega: rebuild + `aspnet_compiler` + tests (`CLAUDE.md` → "Build y
     validación") → checklist → el usuario prueba en el navegador → commit.
4. Pantallas: replicar el SAP MES real. Las capturas de referencia que aporta el usuario
   están en `C:\Users\320313439\Pictures\TrainingApp\` y lo ya documentado en
   `docs/ui-reference/` (`general-flow.md` + un archivo por área). Estado de cada pantalla
   frente a su captura: `docs/ui-reference/inventario-pantallas.md` (Fase 6.5).

### Bloqueantes antes de la Fase 7 (LoFlo)

| # | Tema | Por qué bloquea | Estado |
|---|---|---|---|
| 1 | **Start de Serialized**: una fila por pieza con SFC propio, txtSFC por selección, multi-selección en el POD | LoFlo es Serialized | ✅ Resuelto (spec 008, entrega 2) |
| 2 | **Assembly no puede completarse** (`ExpectedComponentCount` nulo, `ExpectedCount = 3` hardcodeado) | LoFlo tiene Assembly según CLAUDE.md | ✅ Resuelto (spec 008, entrega 3): Assembly rediseñado con el BOM; `ExpectedComponentCount` eliminado |
| 3 | **Flags reales de LoFlo a confirmar con negocio**: `RequiresAssembly` (BD = false, CLAUDE.md = true), `RequiresNCComponent` (1 provisorio, spec 007), `RequiresDataCollection` ("variable" según CLAUDE.md) | Sin esto el catálogo de la Fase 7 no se puede escribir | **Pendiente de negocio** |
| 4 | **Huecos de UI de Assembly**: la X del Component List y Return to POD vacíos | Se ejercitan por primera vez con LoFlo | ✅ Resuelto (spec 008, entrega 3) |

### Deuda y riesgos conocidos (no bloquean la Fase 7)

- **Checklist de la spec 006 cerrado ✅ (2026-09-29):** el punto 6 (volver a guardar
  Resource Setup → AC-12) se dio por resuelto por análisis de código: el Save repetido
  en el modal ya confirmado lo descartan `ResourceSetupControl.BtnSave_Click`
  (`if (!Visible) return;`, no sobrescribe la orden) y el guard de sender de
  `HandleControlSubmit`; no hay intento ni cambio de puntaje y el modal se cierra. El
  5 (force-advance en DC) y el 7 (Assembly) quedaron cubiertos por la spec 008.
- **`GuidedTest` llama a repositorios de la DAL directamente** (`_areaRepo`,
  `_productRepo`, `_ncRepo`, `_dcParamRepo`), contra la regla de dependencias de
  CLAUDE.md. El código nuevo de la spec 008 pasa por `TrainingGuideService`; migrar lo
  existente es una tarea aparte. → **AUD-14** (entrega E4 de la auditoría).
- **Botón Atrás del navegador** todavía puede reenviar un POST viejo. F5 sí está
  resuelto (`history.replaceState`). Riesgo aceptado.
- **Fase 5 en pausa:** `ScoreCalculator` y `StepValidator` sin cobertura NUnit. La spec
  006 dejó los tests fuera de los criterios de aceptación.
- **`CompleteControl`** está declarado e implementa `IStepControl`, pero no tiene UI:
  Complete se resuelve directo desde `BtnComplete`.
- **Detalle por paso en el resumen final** (intentos y puntaje por paso): diferido
  (spec 007), hasta que lo pida un instructor o la Fase 8.
- **Pérdida de la Session a mitad de sesión** (spec 008, E7): la orden simulada y el
  avance de DC/Assembly viven solo en `HttpSessionState`; si se pierden, el operador
  tiene que reiniciar. No se implementa ahora; caso especial a analizar cuando
  evolucione la arquitectura de flujos (opción: persistirlos en BD por sesión).
  → La auditoría lo divide en **AUD-12** (arreglo rápido: la Session dura 20 min y el
  login 60) y **AUD-26** (persistir en BD, con la spec del motor).

### Auditoría 2026-10-01 — lista única de hallazgos

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
| AUD-05 | Alto | `SubmitStep` no valida que la sesión esté `InProgress`, que el paso sea el actual ni quién es el dueño. Con un `stepId` viejo, `UpdateCurrentStep(step.Order + 1)` haría retroceder la sesión. Hoy lo protege solo el code-behind. | `TrainingSessionService.cs:65` (y los otros métodos que llaman a `RecordAttempt`) | Guard en la BLL: estado, paso actual y operador. No cambia reglas. | E3 |
| AUD-06 | Alto | Registrar un intento y avanzar el paso son dos escrituras sin transacción (no hay ninguna en el proyecto). Si falla la segunda, el siguiente envío cuenta como intento 2 y el operador queda penalizado injustamente. | `TrainingSessionService.cs:173-221` | Un SP que haga el insert del intento y el avance/cierre dentro de `BEGIN TRAN`. | E3 |
| AUD-07 | Medio | Guided aplica la penalización por intento (80/60/0%), pero `CLAUDE.md` dice que Guided no penaliza. | `ScoreCalculator.cs:33` | Alinear el código o el `CLAUDE.md`. Toca una regla de puntaje. **D-2** | E3 |
| AUD-08 | Medio | No se exige el cambio de contraseña del primer acceso: después del login con la contraseña por defecto, el operador puede ir directo a `StartSession` y la cuenta sigue con `Sanjose1!`. | `StartSession.aspx.cs`, `GuidedTest.aspx.cs`, `SessionHelper` (nadie lee `IsFirstLogin` después del login) | Guardar `IsFirstLogin` en Session y redirigir a `ChangePassword` mientras sea `true`. | E2 ✅ |
| AUD-09 | Medio | `ChangePassword` no pide la contraseña actual y está disponible siempre: en una estación con la sesión abierta, cualquiera cambia la contraseña. La regla "no puede ser la por defecto" vive en el code-behind. | `ChangePassword.aspx.cs:23,38`, `AuthService.ActivateAccount` | `AuthService.ChangePassword(operatorId, actual, nueva)` con las reglas en la BLL. **D-3** | E2 ✅ |
| AUD-10 | Medio | Si la Session expiró pero el login sigue vigente, Save en `ChangePassword` redirige sin guardar ni avisar, y el operador entra a la app con la contraseña por defecto. | `ChangePassword.aspx.cs:29` | Usar `SessionHelper.EnsureOperatorSession()`, como las demás páginas. | E2 ✅ |
| AUD-11 | Medio | El `HfRows` que manda el cliente se mete sin escapar en un `<script>` (`rsRestoreRows(...)`) y se deserializa sin `try/catch`: un valor con `</script>` rompe la página (XSS propio) y un JSON mal formado da una página de error. | `ResourceSetupControl.ascx.cs:102,130` | Re-serializar lo ya parseado; JSON inválido = aviso de formato (sin intento). | E2 ✅ |
| AUD-12 | Medio | La Session dura 20 min (no hay `<sessionState>`) y el login 60 con renovación: tras 20 min sin actividad el POD se vacía a mitad de una evaluación. | `Web.config` | `<sessionState timeout="60" />` (arreglo rápido; el de fondo es AUD-26). | E3 |
| AUD-13 | Medio | El `StepValidator` se cachea 60 min por área con los flags y catálogos; la página los lee frescos. Tras correr un script de catálogo, la UI y la calificación no coinciden (ej. cambiar `RequiresStartChecklist` deja a Guided atascado en Start). | `TrainingSessionService.cs:237` | Quitar la caché (armarlo es barato) o invalidarla. | E1 ✅ |
| AUD-14 | Medio | `GuidedTest` usa 4 repositorios de la DAL directamente (ya estaba en la deuda). | `GuidedTest.aspx.cs:23-26` | Pasar por `TrainingGuideService` (`GetArea`, `GetNCCodes`, `GetDataCollectionParameters`). | E4 |
| AUD-15 | Medio | `SessionHelper` (en `Shared`) usa `OperatorRepository` y `System.Web`: las páginas llegan a la DAL sin pasar por la BLL y se forma un ciclo `Shared` ↔ DAL. | `SessionHelper.cs:30` | `AuthService.GetOperatorByCode1` y mover `SessionHelper` a la capa de presentación. | E4 |
| AUD-16 | Medio | `GuidedTest.aspx.cs` (1,407 líneas) es el motor real: modifica la orden simulada en el split y decide cuándo una selección de piezas es intento fallido (en NonSerialized, "Complete sin fila" está escrito en la página). | `GuidedTest.aspx.cs:379, 417-440, 655, 726, 867, 915-933` | Un servicio de la BLL dueño de la orden y del avance (ej. `PodService`), con comandos Start/Split/Complete. Incluye AUD-25 y AUD-26. | Spec, entre la 6.5 y la 6.6 (antes de los overlays) |
| AUD-17 | Bajo | No hay protección CSRF: ni `ViewStateUserKey` ni el token anti-XSRF del template. | `Site.Master.cs`, `Training.master.cs` | `ViewStateUserKey = Session.SessionID` en `Page_Init` de los dos masters. | E2 ✅ |
| AUD-18 | Bajo | `EscapeJs` no escapa `\`, `\r` ni `</script>`. Hoy solo recibe mensajes del servidor. | `GuidedTest.aspx.cs:1385` | `HttpUtility.JavaScriptStringEncode`. | E2 ✅ |
| AUD-19 | Bajo | Assembly muestra "Component X assembled" aunque la BLL haya rechazado el componente (id fuera del BOM). | `GuidedTest.aspx.cs:319` | Revisar `AssemblySubmitResult.ComponentAssembled`, que hoy nadie lee. | E1 ✅ |
| AUD-20 | Bajo | `catch {}` vacíos al leer el JSON de las respuestas. | `StepValidator.cs:120,151` | Capturar solo `JsonException`. | E1 ✅ |
| AUD-21 | Bajo | Código muerto C#: `BuildMoldeoLCQuestions`, `ScoreCalculator.CalculateStepScore`, `StepValidator.ValidateDataCollection` (inalcanzable y más laxo que el de grupos), 6 propiedades de `StepViewModel` (`StepId`, `StepType`, `StepOrder`, `Hint`, `IsCorrect`, `AttemptCount`), `ComponentAssembledEventArgs.Barcode`, `ExtraData["AreaName"]`, `.mes-ico-retrieve`, los `Page_Load` vacíos de los masters y el evento de `CompleteControl` (CS0067). | Varios | Borrar. **Antes revisar:** `StepSubmitResult.CurrentScore` (también sin lectores) conviene usarlo (AUD-25), no borrarlo. | E5 |
| AUD-22 | Bajo | El proyecto **web** referencia `Microsoft.ApplicationInsights` (sin uso) y 6 paquetes de testing (`Microsoft.Testing.*`, `TestPlatform.ObjectModel`), que además generan un `Main` en `obj/`. | `packages.config`, `TrainingApp.csproj:3-5, 50-77` | Quitarlos con NuGet (tienen dependencias transitivas). | E5 |
| AUD-23 | Bajo | Restos del template de VS: `Site.Master` muestra "Application name" y enlaces a `~/About` y `~/Contact` (404); ~45 archivos estáticos sin uso (variantes de Bootstrap/jQuery, MsAjax, Menu, SmartNav); Modernizr y los bundles MsAjax sin uso; jQuery probablemente cargado dos veces en el POD; 2 líneas basura en `.gitignore`; ~16 `using` sin uso. | `UI/master/Site.Master`, `Content/`, `Scripts/`, `App_Start/BundleConfig.cs`, `.gitignore` | Limpiar. Conservar `WebForms.js`, `WebUIValidation.js`, `Focus.js` y el mapping `jquery` (los usan los validadores). | E5 |
| AUD-24 | Bajo | Los data-binding usan `<%# Eval(...) %>` sin codificar. Hoy los datos vienen de la BD o son números validados. | `GuidedTest.aspx:153-166, 342-350`, `DataCollectionControl.ascx`, `LogNCControl.ascx` | `<%#: ... %>` por consistencia. | E5 |
| AUD-25 | Bajo | Unas 15 conexiones a SQL por click en el POD: `GetContext` recarga sesión, guía y área; `UpdateScoreWidget` vuelve a consultar; `RecordAttempt` calcula el puntaje y lo descarta. | `GuidedTest.aspx.cs`, `TrainingSessionService.cs:195-224` | Usar `StepSubmitResult.CurrentScore` y cargar el contexto una vez por request. | Con AUD-16 |
| AUD-26 | Bajo | La orden simulada y el avance de DC/Assembly viven solo en la Session (ya estaba en la deuda, spec 008 E7). | `GuidedTest.aspx.cs`, `Constants.SessionKeys` | Persistirlos en BD por sesión. | Con AUD-16 |
| AUD-27 | Bajo | La pregunta de Line Clearance está en el code-behind, contra "datos de simulación en SQL" de `CLAUDE.md`. | `GuidedTest.aspx.cs:1030` | Moverla al catálogo por área. | Fase 7 (LoFlo necesitará la suya) |
| AUD-28 | Bajo | Endurecimiento para el deploy: cookies sin `requireSSL`, sin cabeceras de seguridad, `X-AspNet-Version` expuesto, el ID de Session no se renueva en el login, no hay `Application_Error` ni registro de errores o de logins fallidos, y el login no limita intentos. Sin `customErrors`, un valor con marcado en cualquier campo de texto libre (ej. Data Collection) muestra la pantalla de `HttpRequestValidationException` (Resource Setup ya lo bloquea en el cliente, E2). | `Web.config`, `Global.asax.cs`, `Login.aspx.cs` | Checklist de la Fase 9. | Fase 9 |
| AUD-29 | Bajo | Paquetes atrasados, ninguno con CVE: parches de Newtonsoft (13.0.4), jQuery (3.7.1), `Microsoft.Web.Infrastructure` (2.0.1) y `System.*`; mayor de CodeDom (2.0.1 → 4.1.0). Bootstrap 5.3.8 y SweetAlert2 11.26 pueden cambiar la UI. | `packages.config`, `Scripts/`, `UI/master/Training.Master` | Uno por uno; Bootstrap y SweetAlert después de la 6.6, revisando la UI. | Fase 9 |
| AUD-30 | Info | `origin/main` todavía contiene la app legacy (con `packages/`, `bin/` y `.vs/` versionados); `TrainingApp` va 44 commits adelante sin merge. Los worktrees salen de `main` por defecto y fallan por rutas largas o parten del código equivocado. | Repo | No se toca `main` (D-5); los worktrees deben salir de `TrainingApp`. | Resuelto (D-5) |
| AUD-31 | Info | 4 XML docs de ~300 miembros públicos y no hay README. `CLAUDE.md` y `docs/` cubren ese papel. | — | Sin acción. | — |
| AUD-32 | Info | Por diseño (no tocar): contraseña por defecto + autoregistro por `code1` (quien conozca el `code1` de alguien que nunca entró puede activar su cuenta); mensajes distintos para `code1` inexistente y contraseña incorrecta (permite enumerar `code1`). | `AuthService`, `Login.aspx.cs` | Riesgo aceptado (`CLAUDE.md` → Auth Flow). | — |

**Decisiones del usuario (cerradas 2026-10-02):**
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

**Entregas propuestas (Fase C), en orden:**

| Entrega | Contenido | Vía | Revisión |
|---|---|---|---|
| E1 · Bugs del flujo ✅ | AUD-01, 02, 03, 13, 19, 20 | Plan directo (D-1, D-4); AUD-01 incluye un script SQL que corre el usuario | `/code-review` |
| E2 · Auth y secretos ✅ | AUD-04, 08, 09, 10, 11, 17, 18 | Plan directo (D-3); AUD-04 requiere que el usuario rote la contraseña | `/security-review` |
| E3 · Integridad del puntaje | AUD-05, 06, 07, 12 | Plan directo: toca el motor sin cambiar reglas, salvo AUD-07, que alinea el código con `CLAUDE.md` (D-2); AUD-06 incluye SP nuevo | `/code-review` |
| E4 · Capas | AUD-14, 15 | Mecánico, solo enrutamiento | `/code-review` |
| E5 · Limpieza | AUD-21, 22, 23, 24 | Mecánico | `/code-review` |
| Spec del motor | AUD-16, 25, 26 | `/spec`, entre la 6.5 y la 6.6 | — |

Ninguna entrega toca la fidelidad de UI de la 6.5 (respeta su "No incluye"). Proceso de
cada entrega: implementar → `/dotnet-claude-kit:verify` con MSBuild Rebuild y
`aspnet_compiler` (no `dotnet build`; sin tests, por la decisión del usuario) →
`/code-review` sobre el diff, o `/security-review` si toca auth o Session → checklist en
el navegador → el usuario confirma → commit.

### Scripts de BD (`Database/StoredProcedures/`)

- **Limpieza 2026-09-25:** las migraciones (`08`–`11` y los ALTER de `06`) se
  integraron al esquema original y se borraron. Quedan 8 scripts definitivos, que se
  ejecutan en orden numérico (`00` tablas → `01`–`04` SPs → `05` datos de ejemplo →
  `06`–`07` catálogo y guía reales de R&C/Moldeo). Instalación limpia y re-ejecución
  verificadas en LocalDB; el resultado es idéntico a la BD de desarrollo.
- `05_SeedData.sql` — **solo dev**: áreas de ejemplo (Redefinity, LoFlo provisoria,
  Subensamble 1) y la guía publicada de Redefinity (Serialized, con Assembly), con 2 DC
  Groups obligatorios + 1 opcional y un BOM simulado (`SIM-100001`, `SIM-100002`) para
  probar el camino Serialized de punta a punta (spec 008).
- Los cambios de esquema futuros se escriben directo en `00_Schema.sql`; el `ALTER`
  para la BD existente se le entrega al usuario aparte y no se guarda como script.

### Fase 0 — Setup ✅ (completada)
**Objetivo:** Proyecto Web Forms creado, N-Tier scaffolded, build verde.
**No incluye:** lógica de negocio, UI funcional, datos reales.

---

### Fase 1 — Auth ✅ (completada)
**Objetivo:** Login.aspx + ChangePassword.aspx funcionales, flujo completo
de primer acceso (API de empleados → Operator local → PBKDF2 → FormsAuth).
**No incluye:** Dashboard, GuidedTest, catálogo de áreas, ningún paso de entrenamiento.

---

### Fase 2 — Dominio y catálogo de R&C ✅ (completada)
**Objetivo:** Auditoría del legacy de R&C + script SQL con el catálogo real:
`Products` (R&C), `Areas` (Moldeo, NonSerialized, RequiresAssembly=false,
RequiresDataCollection=true), `NCCodes` (30 códigos), `DataCollectionParameters`
(15 parámetros: ID molde + 10 params MSS + verificación MSS + fecha kerosene +
2 first-shot). Catálogo en `06_RCMoldeoCatalog.sql`.
ExpectedComponentCount: N/A (RequiresAssembly=false).
`DataCollectionParameters` schema evolucionado: columnas `DataType`, `DisplayOrder`
añadidas; `MinValue`/`MaxValue` son ahora nullable (rangos pendientes de MSS D000281359).
**No incluye:** UI, motor de validación de pasos, TrainingGuideService,
ninguna pantalla de entrenamiento.

---

### Fase 3 — Motor de TrainingGuide ✅ (completada)
**Objetivo:** `TrainingGuideRepository` (dual result set puebla `Steps`),
`TrainingGuideService` con `BuildValidatorForGuide`, `StepValidator` completo
con constructor injection de `StepValidationContext` (Area + NCCodes + DCParams).
Validación implementada para todos los StepTypes: DataCollection (JSON por
param Id, tipado por DataType con InvariantCulture), LogNC (lookup en catálogo),
Assembly (count vs ExpectedComponentCount), LineClearance ("true"), pasos de
secuencia siempre true.
Nuevos repositorios: `AreaRepository`, `NCCodeRepository`,
`DataCollectionParameterRepository`. Nuevos SPs: `spAreaGetById`,
`spNCCodeGetByArea`, `spDataCollectionParameterGetByArea`.
Guía real de R&C/Moldeo publicada en `07_RCMoldeoGuide.sql` (8 pasos, pesos
suman 1.00; Search hereda peso 0.15 de Assembly ausente; 3 LC instancias con
pesos 0.04/0.03/0.03).
LC Moldeo modelada como paso LineClearance en el guide seed (step 4, weight 0.03);
la validación es idéntica al LC estándar — el contenido hardcodeado de sus 3
preguntas es responsabilidad de la UI (Fase 6).
`DataCollectionParameter` POCO evolucionado: añadidos `DataType`, `DisplayOrder`,
`MinValue`/`MaxValue` como `decimal?`.
Tests NUnit diferidos a Fase 5 (cobertura de ScoreCalculator y StepValidator).
**No incluye:** TrainingSession con gate-keeping real, persistencia de intentos
optimizada, UI, ScoreCalculator tests.

---

### Fase 4 — TrainingSession y modos ✅ (completada)
**Objetivo:** `TrainingSessionService` extendido con gate-keeping real Guided vs.
Assessment, `MemoryCache` de validator keyed por `AreaId` (TTL 60 min), método
`Abandon`, score en tiempo real (`CurrentScore` en `StepSubmitResult`).
`TrainingSessionRepository` extendido: `Abandon` + `DeleteActiveByOperator`.
Nuevos SPs: `spTrainingSessionAbandon`, `spTrainingSessionDeleteActiveByOperator`,
`spProductGetAll`, `spAreaGetByProduct`.
`ProductRepository` nuevo; `AreaRepository` extendido con `GetByProduct`.
`TrainingGuideService` extendido con `GetAllProducts` y `GetAreasByProduct`.
`StartSession.aspx` — selección Product → Area → Guide → Mode, inicia sesión.
`GuidedTest.aspx` — input genérico por paso, feedback + hint hardcodeado por
`StepType`, score en vivo, abandon con confirm JS, panel de completion.
Login redirige a `StartSession.aspx` tras autenticación exitosa.
Spec de referencia: `docs/specs/003-training-session-y-modos.md` (Approved 2026-09-04).
**No incluye:** UserControls por tipo de paso, ScoreCalculator tests, LoFlo.

---

### Fase 5 — ScoreCalculator: cobertura de tests ⏸️ (en pausa)
**Objetivo:** `ScoreCalculator` ya está implementado. Esta fase lo blinda
con tests NUnit exhaustivos: todos los casos de penalización, force-advance,
pasos opcionales ausentes, sesiones completadas vs. abandonadas.
Es el componente más sensible del sistema — debe tener cobertura total
antes de que usuarios reales generen puntajes.
**No incluye:** nuevas features, cambios al algoritmo.

---

### Fase 6 — UserControls por tipo de paso ✅ (completada 2026-09-24)
**Objetivo:** UserControls `.ascx` reutilizables para cada `StepType`:
`ResourceSetup`, `LineClearance`, `SearchOrder`, `Assembly`,
`DataCollection`, `NCScrap`, `Complete`. Cada control recibe el estado
del servidor y emite un submit — sin lógica de negocio interna.
**No incluye:** catálogo de LoFlo, nuevas áreas.

**Specs:**
- `004-fase6-dashboard-usercontrols.md` (Approved 2026-09-09): POD, botonera y contrato de UserControls.
- `005-pod-orden-dinamica.md` (Approved 2026-09-24, de forma retroactiva): orden simulada en Session creada
  por Resource Setup; SFC Split como paso propio (StepType 5); DC agrupado por DC Group
  (columnas `DCGroup`/`GroupDescription`/`CollectDataAt`, hoy en `00_Schema.sql`).
- `006-controles-de-paso-estaticos.md` (Approved 2026-09-22).
- `007-cierre-flujo-lognc-complete.md` (Approved 2026-09-24).

**Decisiones de arquitectura cerradas — no reabrir sin spec:**
- **Controles de paso estáticos (spec 006).** Los 8 controles están declarados en
  `GuidedTest.aspx` y se alterna `Visible`. **Prohibido volver a `LoadControl`
  dinámico**: generó cuatro bugs de ViewState en una sola sesión. Confirmado con
  negocio: los admins no crean tipos de paso desde la UI y un `StepType` no tiene UIs
  estructuralmente distintas por Area. Un `StepType` nuevo = `.ascx` + una línea de
  markup + un `case` en `GetStepControl`.
- El paso abierto vive en el ViewState de la página (`OpenStepType`).
  `ApplyStepControlVisibility` es el único punto que toca `Visible`.
- `IStepControl.Initialize()` es el **único punto de reinicio** del control. Todo
  control tiene que tolerar `ViewModel == null` y no registrar scripts cuando está
  invisible.
- **Guard de sender:** `HandleControlSubmit` y `HandleReturnToPod` descartan eventos
  de controles que no son el del paso activo, porque ASP.NET dispara eventos también
  en controles invisibles.
- **Submit incorrecto:** el control queda abierto en el modal donde se respondió y
  solo se muestra el SweetAlert. Es la única excepción aprobada a la paridad de UX
  (spec 006 E3).
- **Acción incorrecta = intento fallido** (spec 007): botón equivocado o gate de fila
  sin cumplir → `StepAnswers.Invalid` a la BLL. `StepValidator.Complete` acepta solo
  `"complete"`.
- Salir de un control sin responder (✗ / Return to POD) usa el evento
  `OnReturnToPod` y **no** registra intento.
- **F5 = GET** (`history.replaceState`). En la carga por GET, el POD y txtSFC se
  reconstruyen desde la orden simulada. F5 no restaura la selección ni el control
  abierto.
- Estado intermedio de UI en el cliente, sin postback: paginación de NC Selection,
  Save Draft de DC, navegación entre los 4 modales de Search.

**Commits de cierre:** `3567e07` (migración a controles estáticos), `51636a7` (DC
return to POD, txtSFC, resaltado de fila, score final), `f54ff50` (spec 007),
`3a38b86` (F5).

---

### Fase 6.5 — Fidelidad de UI/UX con el SAP MES real 🔄 (en curso, formalizada 2026-09-29)
**Objetivo:** que cada pantalla y modal del POD replique su captura del SAP MES real
(estructura, textos, colores y comportamiento visual), y dejar la interfaz cerrada antes
de los overlays guiados de la Fase 6.6, que se anclan a ella.
**No incluye:** cambios de reglas de negocio, del motor de pasos o del puntaje; pasos
nuevos; los overlays guiados; el catálogo de LoFlo; las páginas del simulador que no son
del MES (Login, Cambio de contraseña, StartSession). Si una corrección toca una regla (ej.
Status "In Queue" o el SFC nuevo del Split), se señala y se decide aparte.
**Criterio de cierre:** `docs/ui-reference/inventario-pantallas.md` con todas las
pantallas que tienen captura en ✅ (o con sus diferencias cerradas como *aceptadas*). Las
pantallas sin captura quedan anotadas como pendientes y no bloquean.
**Proceso:** sin spec — las capturas del MES real son la fuente. Cada tanda de
correcciones del usuario = una entrega (ronda de preguntas si hace falta + plan directo en
`docs/specs/fase-6.5-plan.md`) → build + precompilación (+ tests si cambia la BLL) →
checklist → prueba del usuario → commit → actualizar el inventario.
**Commits:** `48b67c1` (entrega 1), `fbcd132` (2), `8fde133` (3), `b832e77` (4),
`f432462` (formalización + inventario), `c38fe94` (5), `70e5e8e` (6), `03d767b` (7),
`4ca602c` (8) y la entrega 9 (SFC Split + SFC nuevo en el split).

---

### Fase 6.6 — Overlays guiados 📝 (planeada, requiere `/spec`)
**Objetivo:** en modo Guided, overlays que guían al operador paso a paso sobre la interfaz
ya cerrada en la 6.5 (qué botón, campo o modal usar en cada paso).
**A definir en la spec:** si aplican solo en Guided (en Assessment no pueden revelar el
camino); cómo se anclan a los elementos (anclas estables en botones y campos); que se
definan por tipo de paso y no por área, para que LoFlo los reciba sin trabajo extra.
Entrada: el inventario de pantallas de la 6.5.
**No incluye:** cambios de UI de las pantallas del MES (eso es la 6.5), reglas de negocio
nuevas.

---

### Fase 7 — Catálogo de LoFlo (segunda área) ⛔ (bloqueada solo por el bloqueante #3: flags de negocio)
**Objetivo:** Script SQL con el catálogo real de LoFlo (Sensores).
Solo carga de datos — cero código nuevo. Valida que el motor de Fases 3-4
es verdaderamente genérico y que `RequiresAssembly`/`RequiresDataCollection`
funcionan con la configuración real de LoFlo.
**El script tiene que cargar** (spec 008): flags del área (incluido
`RequiresStartChecklist = 0`), NC Codes, `DataCollectionGroups` con qué grupos son
obligatorios, sus `DataCollectionParameters`, el BOM en `AssemblyComponents` (simulado
por ahora, ≥1 componente si `RequiresAssembly = 1`) y la guía con sus pasos.
**No incluye:** cambios al motor, nuevas pantallas.

---

### Fase 8 — Validación interna
**Objetivo:** Sesión con trainer/operador real sobre R&C y LoFlo.
Correcciones de UX, bugs de flujo, ajustes de catálogo.
**No incluye:** nuevas áreas, Dashboard admin, reportes.

---

### Fase 9 — Deploy del MVP
**Objetivo:** Deploy en servidor interno con IIS. Configuración de
connection string de producción, App Pool, permisos, prueba de humo.
**No incluye:** CI/CD automatizado, monitoreo avanzado.

