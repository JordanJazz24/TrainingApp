# Historial — MESTrainingSimulator

> **Lo que se cerró, en orden.** Lo escribe Desarrollo al cerrar una fase, una spec o un plan
> directo, y solo se agrega (`docs/process.md` → "Documentación"). No hace falta leerlo al
> iniciar una sesión: el estado está en `docs/status.md`. Separado del roadmap el
> 2026-10-21: las referencias `roadmap → "Historial"` de specs y auditorías apuntan aquí.
> Cada sección describe lo que se hizo **en su momento**: si contradice a `CLAUDE.md` o
> `docs/domain.md`, mandan ellos.

## Fase 0 — Setup ✅
**Objetivo:** proyecto Web Forms creado, N-Tier scaffolded, build verde.
**No incluye:** lógica de negocio, UI funcional, datos reales.

## Fase 1 — Auth ✅
**Objetivo:** `Login.aspx` + `ChangePassword.aspx` con el flujo completo de primer acceso
(API de empleados → `Operator` local → PBKDF2 → FormsAuth).
**No incluye:** dashboard, GuidedTest, catálogo de áreas, pasos de entrenamiento.

## Fase 2 — Dominio y catálogo de R&C ✅
**Objetivo:** auditoría del legacy de R&C + catálogo real en `06_RCMoldeoCatalog.sql`:
`Products` (R&C), `Areas` (Moldeo), `NCCodes` (30 códigos), `DataCollectionParameters` (15:
ID molde + 10 parámetros MSS + verificación MSS + fecha kerosene + 2 first-shot). El esquema
de `DataCollectionParameters` sumó `DataType` y `DisplayOrder`; `MinValue`/`MaxValue` quedaron
nullable (rangos pendientes del MSS D000281359).
**No incluye:** UI, motor de validación, `TrainingGuideService`, pantallas.
**Deuda de proceso:** se saltó la aprobación de la spec 002 antes de implementar.

## Fase 3 — Motor de TrainingGuide ✅
**Objetivo:** `TrainingGuideRepository` (un SP con dos result sets puebla `Steps`),
`TrainingGuideService.BuildValidatorForGuide` y `StepValidator` con `StepValidationContext`
inyectado (Area + NCCodes + DCParams), con validación para todos los `StepType`. Repositorios
`Area`, `NCCode`, `DataCollectionParameter` y sus SPs. Guía real de Moldeo en
`07_RCMoldeoGuide.sql`.
*Superado después:* la LC Moldeo era un paso propio de la guía (con pesos 0.04/0.03/0.03 en
3 instancias de LC) y Assembly se validaba contra `ExpectedComponentCount`. Hoy el checklist
va dentro de Search, Assembly usa el BOM (spec 008) y los pesos están en `docs/domain.md`.
**No incluye:** gate-keeping real, UI, tests de `ScoreCalculator`.

## Fase 4 — TrainingSession y modos ✅
**Objetivo:** gate-keeping real Guided vs. Assessment en `TrainingSessionService`, `Abandon`,
puntaje en tiempo real. `StartSession.aspx` (Product → Area → Guía → Modo) y un
`GuidedTest.aspx` genérico por paso. Spec `003-training-session-y-modos.md` (aprobada
2026-09-04).
**No incluye:** UserControls por tipo de paso, tests de `ScoreCalculator`, LoFlo.

## Fase 5 — ScoreCalculator: cobertura de tests ✅ (2026-10-07)
**Objetivo:** blindar `ScoreCalculator` (el componente más sensible) con tests NUnit
exhaustivos antes de que usuarios reales generen puntajes: penalización, avance forzado,
pasos opcionales ausentes, sesiones completadas vs. abandonadas. Decisión del usuario: antes
de la 7 y la 8.
**No incluye:** nuevas features, cambios al algoritmo.
**Resultado:** 3 entregas, sin spec (`docs/specs/fase-5-plan.md`).
`TrainingApp.Test/ScoreCalculatorTests.cs` cubre todas las ramas de `ScoreCalculator`, las
guías reales de Moldeo y Redefinity, el límite de aprobación y un barrido de las 65 536
combinaciones de cada guía. El código cumplía todas las reglas: no hubo cambios al
algoritmo. De paso, los pesos de `CLAUDE.md` se alinearon con la BD (manda la BD).

## Fase 6 — UserControls por tipo de paso ✅ (2026-09-24)
**Objetivo:** un UserControl `.ascx` por `StepType` que recibe el estado del servidor y emite
un submit, sin lógica de negocio.
**No incluye:** catálogo de LoFlo, nuevas áreas.
**Specs:** `004-fase6-dashboard-usercontrols.md` (POD, botonera y contrato de controles) ·
`005-pod-orden-dinamica.md` (orden simulada creada por Resource Setup, SFC Split como paso
propio, DC agrupado por DC Group; aprobada de forma retroactiva) ·
`006-controles-de-paso-estaticos.md` · `007-cierre-flujo-lognc-complete.md`.
**Decisiones de arquitectura** (controles estáticos, `Initialize`, guard de sender, F5 =
GET…): ahora en `docs/architecture.md` → "`GuidedTest` y controles de paso".
**Commits de cierre:** `3567e07` (controles estáticos), `51636a7` (DC, txtSFC, resaltado,
puntaje final), `f54ff50` (spec 007), `3a38b86` (F5).
**Cierre:** el flujo completo de R&C/Moldeo (NonSerialized) funciona de punta a punta en
Guided y Assessment, verificado por el usuario.

## Ajustes tras la Fase 6 y spec 008 ✅ (2026-09-25 a 2026-09-28)
- **Assessment real (S1 + S4, sin spec):** el 50% del puntaje nunca podía fallar porque los
  controles rechazaban las decisiones antes de enviarlas. Se separó formato de decisión (hoy
  `docs/domain.md` → "Qué cuenta como intento"); el 70% pasó a `ScoreCalculator.PassingScore`; los
  banners de guardado solo aparecen si el servidor aceptó el paso.
- **S2, secuencia en Assessment (sin spec):** reglas cerradas con el usuario el 2026-09-25,
  hoy en `docs/domain.md` → "Qué cuenta como intento".
- **Spec 008 (5 entregas, `008-plan.md`):** el motor soporta áreas Serialized: 3-5 piezas
  con SFC propio, Browse Work List y POD con multi-selección, `[N SELECTED]`, Start directo
  sin checklist (`Areas.RequiresStartChecklist`); Assembly con el BOM (se eliminó
  `ExpectedComponentCount`); DC por pieza con grupos obligatorios (Moldeo exige su grupo de
  fechas de expiración); Log NC y Complete multi-fila. Pendiente diferido en la spec: recorrido
  pieza por pieza. Verificado: Redefinity de punta a punta y regresión de Moldeo.
- **Checklist de la spec 006 cerrado (2026-09-29):** el punto 6 (volver a guardar Resource
  Setup) se resolvió por análisis de código (`BtnSave_Click` con `if (!Visible) return;` + el
  guard de sender); el 5 y el 7 los cubrió la spec 008.

## Fase 6.5 — Fidelidad de UI/UX con el SAP MES real ✅ (2026-09-29 a 2026-10-02)
**Objetivo:** que cada pantalla y modal del POD replique su captura del SAP MES real, y dejar
la interfaz cerrada antes de los overlays de la 6.6.
**No incluye:** reglas de negocio, motor, puntaje, pasos nuevos, overlays, LoFlo, páginas que
no son del MES (Login, Cambio de contraseña, StartSession).
**Proceso:** sin spec; cada tanda de correcciones del usuario = una entrega, con plan en
`docs/specs/fase-6.5-plan.md`.
**Resultado:** inventario `docs/ui-reference/inventario-pantallas.md` con todas las pantallas
con captura en ✅. Entregas 1-9: columna SFC y Select All; Collect deshabilitado por pieza;
Shop Order / Status; layout y colores del MES; header estándar de los 12 modales; Resource
Setup; botones grises (`.mes-btn` + `.mes-ico-*`); Line Clearance con submodal apilado;
listas; DC Entry y NC Data Entry (columna nueva `DataCollectionParameters.Description`);
SFC Split.
**Regla cambiada por decisión del usuario:** el SFC Split crea un **SFC nuevo** de la misma
Shop Order (`SplitSfcGenerator`; enmienda 2026-10-01 de la spec 005).
**Commits:** `48b67c1`, `fbcd132`, `8fde133`, `b832e77`, `f432462` (inventario), `c38fe94`,
`70e5e8e`, `03d767b`, `4ca602c`, `f9aa1c7`, `c8d99d4`, `2f550eb`.

## Auditoría, spec 009 y auditoría final ✅ (2026-10-01 a 2026-10-06)
- **Auditoría del proyecto (2026-10-01/02):** 32 hallazgos (AUD-01…32), decisiones D-1…D-8
  y 5 entregas (E1 bugs del flujo · E2 auth y secretos · E3 integridad del puntaje · E4
  capas · E5 limpieza). Detalle: `docs/audits/2026-10-01-auditoria.md`.
- **Spec 009 · Motor del POD en la BLL (2026-10-05, 6 entregas `d5331ab`…`df08c44`):**
  `PodService` decide el flujo y la página solo pinta; contexto una vez por request (Line
  Clearance: de ~21 a 5 conexiones); el estado del POD se reconstruye si se pierde (D-10);
  una sesión activa por operador (D-9). Cerró AUD-16, 25 y 26. Detalle:
  `docs/specs/009-motor-pod.md` y `009-plan.md`.
- **Auditoría final (2026-10-06):** revisión de la spec 009 + regresión completa en el
  navegador: AUD-33…44 (1 Medio: el logout), D-13…D-16 y 4 entregas (E1 logout · E2
  invariantes de la BLL · E3 la página lee el flujo del contexto · E4 limpieza). Detalle:
  `docs/audits/2026-10-06-auditoria-final.md`.

## Fase 6.6 — Overlays guiados ✅ (2026-10-07)
**Objetivo:** en Guided, overlays que guían al operador paso a paso sobre la interfaz cerrada
en la 6.5.
**No incluye:** cambios de UI de las pantallas del MES, reglas de negocio nuevas.
**Resultado (spec 010, 5 entregas + E2b, `4e24ce5`…`39482d3`):** guía clic por clic en Guided
sobre los 9 pasos, siempre activa y bloqueante (Logout y Abandon libres), con respuestas y
valores de ejemplo, que solo avanza con la respuesta correcta (R10), con mensajes del MES que
esperan a la guía (R9), falla segura y globo final. Nada en Assessment. Mecanismo:
`docs/architecture.md` → "Guía visual de Guided". De paso, en Serialized el POD vuelve sin
selección tras Start, Assembly y DC, como el MES real. La investigación previa (legacy y app)
quedó como anexo en `docs/specs/010-overlays-guiados.md`.

## Reorganización de la documentación (2026-10-07, sin spec)
Cada regla en un solo lugar. `CLAUDE.md` quedó con solo reglas vigentes y un mapa de
documentos (de 37,5 KB a 22,9 KB); el proceso pasó a `docs/process.md` y los mecanismos a
`docs/architecture.md`; el roadmap quedó como estado + historial; el glosario, solo
definiciones. Se quitaron los pesos copiados en `docs/ui-reference/` (estaban desalineados
de la BD). La memoria pasó de 10 a 4 entradas (perfil, reporte semanal, `/btw` y dónde
anotar cada cosa).

## Limpieza de scripts SQL (2026-10-07, sin spec)
Mismos 8 archivos y numeración (decisión del usuario). Los SPs pasan a `CREATE OR ALTER` sin
los `DROP` de SPs ya eliminados. Cada catálogo se declara una vez como tabla de valores y se
actualiza o inserta en orden (los Ids no cambian), todo dentro de una transacción. Las guías
se cargan con `spTrainingGuideSync` (nuevo, en `02`), que actualiza los pesos y aborta si no
suman 1.00. Se corrigió un bug: los literales sin `N` guardaban "Ω" como "O". Se probó en
LocalDB: la instalación limpia y la re-ejecución sobre una BD con sesiones dan los mismos
datos e Ids que los scripts anteriores. Reglas: `docs/process.md` → "Base de datos".

## Arreglo de rendimiento del arranque ✅ (2026-10-07, sin spec)
`optimizeCompilations` en `Web.config` (Login tras un rebuild: de ~4 s a ~1,7 s); la lista de
empleados se precarga al arrancar y se refresca en segundo plano (`docs/architecture.md` →
"API de empleados"); `/bundles` y `/Content` accesibles sin login (Login y ChangePassword
recibían el HTML del Login en vez de sus JS). B-1: una lista vacía de la API no reemplaza la
última. Pendiente para la Fase 9: el arranque en frío en IIS.

## Login SAP NetWeaver ✅ (2026-10-08, sin spec)
Login y Cambio de contraseña replican el login SAP NetWeaver real (captura `Login SAP
NETWEAVER.png`) en un `Auth.Master` propio que hereda `AntiXsrfMasterPage`; `StartSession`
sigue con `Site.Master`. La foto y el logo de SAP están recortados de la captura; sin
Bootstrap. Decisiones del usuario: los errores del servidor van en SweetAlert, como el
legacy, y Cambio de contraseña tiene Cancel, que cierra la sesión. Las validaciones no
cambiaron. Cambio de contraseña no tiene captura: usa los textos del cambio de contraseña de
NetWeaver (`docs/ui-reference/inventario-pantallas.md`). Probado por el usuario en el
navegador.

## Ultrareview ✅ (2026-10-08, sin spec)
Por pedido del usuario, `main` se movió (fast-forward) a `25fa64f`, el cierre de la auditoría
final: así `/ultrareview` sin argumentos revisa solo lo no auditado (6.6, rendimiento, Fase 5,
scripts SQL, Fase 7). Un solo hallazgo, *nit*: en Serialized, la guía de Log NC empezaba con
un subpaso "quitar la selección" que siempre se saltaba (el POD ya llega sin selección), y el
contador arrancaba en "Subpaso 2". Se quitó el subpaso.

## Refactor `Operator` → `User` ✅ (2026-10-09 a 2026-10-12, sin spec)
**D-17 (usuario):** la cuenta local se llama `User` (tabla `Users`, `TrainingSessions.UserId`).
"Operator" es un término del MES (POD = *Production Operator Dashboard*; operador = persona
en la línea) y, con el módulo administrativo, una tabla `Operators` con administradores sería
contradictoria; "Operator" queda como futuro valor de rol. Se hizo antes del módulo admin y
del deploy (después habría más referencias y datos reales que migrar). Refactor puro, sin
cambio de comportamiento: Domain, DAL (SPs `spUser*`, `spTrainingSessionAbandonActiveByUser`,
`@UserId`, `r["UserId"]`), BLL, páginas, `SessionKeys.UserId`, scripts `00`, `01_spUser`
(antes `01_spOperator`) y `03`; los textos de UI no cambiaron. Para la BD existente, un ALTER
aparte (`sp_rename`, conserva los datos) + `01` y `03`. Verificación: build, tests,
`aspnet_compiler`, barrido de texto, contrato DAL ↔ SPs en LocalDB (cada SP, parámetro y
columna que nombra el DAL existe), BD migrada idéntica a una instalación limpia y prueba en
el navegador por el usuario.

## T1 · Preparación de la versión de prueba ✅ (2026-10-12, Fase 8.5, plan directo)
Configuración para publicar la versión de prueba en el IIS interno (Ernesto y técnicos de
MES). **Decisiones del usuario (2026-10-12):** `DEFAULT_PASSWORD` **conserva su valor** (no
cambia, a diferencia del plan) y la BD de prueba **incluye `05`** (Redefinity y Subensamble 1
de ejemplo). Con el valor de siempre, el reclamo de cuenta ajena (D-18) es más fácil: la
spec 013 debe cerrarlo antes de que entren operadores reales.
- `Web.Release.config` agrega `customErrors mode="RemoteOnly"` (parte mínima de AUD-28);
  `debug` y `optimizeCompilations` ya se quitaban. Transformación verificada con
  `XmlTransform`.
- `Database/Ops/ResetUserPassword.sql` (temporal hasta el reset por admin de la spec 013):
  con el `@Code1` de un tester, vuelve la cuenta a la contraseña por defecto con
  `IsFirstLogin = 1`, sin borrar sesiones ni intentos. El hash va precalculado con
  `PasswordHelper.Hash` (T-SQL no reproduce el PBKDF2 de la app); si cambia
  `DEFAULT_PASSWORD`, se regenera con el comando del encabezado.
- Probado en LocalDB: reset con Code1 existente e inexistente, e instalación limpia `00`–`09`
  (3 guías publicadas, pesos en 1.00).
- **Pasos de publicación:** BD con `00`–`09` en orden → publicar desde VS en **Release** con
  el `connectionStrings.config` del servidor → App Pool con `idleTimeout` ≥ 60 min (o 0;
  AUD-28: si el proceso se detiene, la Session InProc se pierde).
- **D-19 (usuario, 2026-10-12):** se commitea como *preparación* y se da por aprobado **sin
  probarlo en el servidor real**; esa prueba queda diferida. Checklist pendiente: el
  `web.config` publicado no tiene `debug="true"` y sí `customErrors`; desde otra PC, ya
  autenticado, `…/Pages/Training/GuidedTest.aspx?sessionId=<b>` muestra el "Runtime Error"
  genérico sin detalle; reset de un tester → entra con la contraseña por defecto y lo manda a
  cambiarla; aparecen las guías de Redefinity, Moldeo y LoFlo.

## Spec 011 · Escanear la Shop Order ✅ (2026-10-13, Fase 8.5, T2)
Plan `docs/specs/011-plan.md`, tres entregas probadas por el usuario: **E1** (`5976d22`)
carga desde `TxtSFC` (Shop Order o SFC de la orden + Enter, en el cliente como Browse Work
List; Start resuelve lo escrito sin Enter; la BLL valida el SFC de NonSerialized con
`PodNotice.SfcNotInOrder` y el Start de Serialized avisa con `StartWholeOrder`); **E2**
(`1db8c23`) etiqueta simulada con Code 128 (JsBarcode 3.11.6 local) sobre el lado vacío del
pie, y **B-1** (ya existía: después del Start el OK de Browse solo cierra); **E3** guía de
Guided (resalta la etiqueta con `TxtSFC` libre, burbuja de Browse, Start) y la etiqueta
desaparece con el Start. Tres enmiendas de la spec, todas decididas por el usuario
(`1865c03`, `b6846b9`, `2e96361`). Sin cambios de esquema ni SPs.

## Spec 013 · Cuentas, roles y registro de actividad ✅ (2026-10-14, Fase 8.5)
Plan `docs/specs/013-plan.md`, cuatro entregas probadas por el usuario. E1-E3 se hicieron en
la rama `spec-013` (worktree) mientras el usuario validaba con un técnico en la carpeta real;
después se hizo rebase sobre `TrainingApp` y merge `--ff-only` (sin conflictos). **E1**
(`e4ef6ae`): roles excluyentes Operator / Administrator (`AccessPolicy`), login de admin
aparte (`Pages/Admin`, `Admin.Master`), rol en cada request (`UserPrincipal` + `RoleCache`),
`<location>` por rol y `CreateAdmin.sql`. Bug al probar: FriendlyUrls quita el `.aspx` y el
login de admin entraba en un bucle con el de SAP. **E2** (`5ea64e4`): bloqueo de 15 min tras
5 fallos (atómico frente a logins simultáneos) y último acceso visible, en hora de planta.
**E3** (`85bf5a5`): gestión de usuarios (lista paginada, crear, reset, desbloquear y
**borrar** con su historial). La enmienda del usuario al probar sacó el cambio de rol desde
la UI (`9cfe566`). **E4:** cada intento guarda motivo (`AttemptReason`) y acción
(`AttemptActions`) en la misma transacción. Reviews: `high` en E1 y E3, `medium` en E2 y E4.
Tests: 88 (21 nuevos). `CLAUDE.md` → "Auth Flow" y `docs/architecture.md` actualizados.

## T7 · Mensajes y alertas de la guía ✅ (2026-10-14, Fase 8.5, plan directo)
Retroalimentación de la trainer, con textos de la sesión de planeación y la revisión del
usuario. Las notas van **dentro del globo** de la guía, después de la instrucción y solo en
el subpaso donde aplican (`GuideSubstep.NoteHtml`; en rojo con `NoteIsAlert`). La primera
versión, con una burbuja roja aparte en todo el paso, la descartó el usuario por ruido visual.
**Alertas:** Resource Setup subpaso 3 (Remove All y escanear los equipos de la estación, por
trazabilidad), Start subpaso 1 (solo piezas de su orden y estación) y LC de cierre subpaso 1
(no aplica en algunas operaciones). **Notas:** LC de apertura (cuándo se hace), Select All de
Assembly y DC en Serialized (el simulador trabaja todas las piezas; el MES real, cualquier
selección), DC Collect y Log NC. **Bug "RCI":** el producto R&C se llama **S&RC** (confirmado
por el usuario); `06` y `07` lo renombran en sitio (`spTrainingGuideSync` busca la guía por
título). Probado por el usuario en el navegador.

## Spec 015 · Ajustes de la trainer ✅ (2026-10-14, Fase 8.5)
Tres prácticas de planta de la retroalimentación de la trainer de Wires, en tres entregas
probadas por el usuario. **E1 — Resource Setup con equipos precargados:** el control abre con
una Shop Order y 2 herramientas "del turno anterior" (las genera la BLL en cada apertura;
enmienda: ya en la tabla, no al hacer Retrieve). Guardar con alguna es decisión equivocada;
con alguna en la tabla el control no aplica el formato (D-26) y, sin Shop Order propia, la
orden no se crea. En Guided, la captura no acepta un precargado (D-25). **E2 — Grupo de DC
Trainer:** `DCGTM_TRAINEE/A` opcional en Moldeo, LoFlo y Redefinity (en `05` y `08` reemplaza
en sitio al tercer grupo opcional, D-23), con el nombre de la persona en entrenamiento
(`PersonName`: letras y espacios, 2-60, vacío válido; inválido = formato) y la nota de la
guía (D-24). **E3 — Logout como último paso:** `StepType.Logout` (0.05, sale de Complete).
Simulado: cierra la sesión de entrenamiento con el resumen sin salir de la app; antes del
último paso es un intento fallido en Assessment y está deshabilitado en Guided; con la sesión
cerrada sale de la app (D-21). Complete deja el POD vacío con un banner **provisional** (D-22).
Reglas en `docs/domain.md`.

## Spec 014 · Tres métodos de Log NC ✅ (2026-10-19, Fase 8.5, T4)
Plan `docs/specs/014-plan.md`, tres entregas probadas por el usuario; la cuarta se recortó.
Pedido del ingeniero de SAP MES, de Ernesto y de los técnicos, y el error más grave según la
trainer (scrapear la pieza equivocada). **E1** (`b685f1d`, `40c1433`): un **hallazgo** por
paso de Log NC (la pieza defectuosa, el defecto y la disposición) con su nota en Guided y
Assessment; desecho rápido calificado por pieza y código exacto en las tres áreas; 4-5 piezas
por orden Serialized; `NCCodes.Method` y `RequiresComments`; enmiendas 2 y 3 (en Moldeo el
hallazgo dicta la cantidad del split y se califica; subpaso Info de la nota). **E2**
(`a41d138`): reparación rápida en LoFlo y Redefinity (Browse de lo ensamblado, Remove
component con Scrap, Add Component con ★ Batch Number, banner provisional de D-55) en un solo
postback; `StepType` 9 y la versión automática de las guías (D-56); enmienda 4 (el desecho no
lleva Component, `RequiresNCComponent` queda sin uso). **E3** (`0654e9d`): reparación
extendida (Comments con el MPI simulado, "NC logged", Select Disposition Function, banner con
las operaciones `SIM` de D-58; Local Rework provisional); `StepType` 10 y pesos finales
(Log NC 0.05 × 3, DC 0.25); enmienda 5 (las reparaciones usan los códigos reales
`F_FR_GENERAL` y `F_FX_GENERAL` y el defecto de la nota sale de un código de desecho, sin
repetirse en la orden). **E4 recortada** (enmienda 6, `a436c73`): la Assembly Data por tipo
de material (W13) no se califica, así que no entrenaba el error real; queda diferida y se
retoma con otra spec si T6, la trainer o T9 muestran errores con Inventory SFC o Material
Document. Moldeo solo entrena el desecho (D-53). Un solo control para los tres métodos (D-60)
con la respuesta en JSON (D-59). Tests nuevos: `StepValidatorLogNCTests` y
`NcFindingGeneratorTests`. Reglas en
`docs/domain.md`; mecanismos en `docs/architecture.md`.

## Spec 016 · Preparación antes de SAP ✅ (2026-10-19, Fase 8.5, T10)
Plan `docs/specs/016-plan.md`, cuatro entregas probadas por el usuario. **E1** (`50aca9d`):
un solo login y un solo cambio de contraseña, neutros, para Operator y Administrator
(enmienda 2 de la 013); se borraron el login y el cambio de contraseña de admin y
`Auth.Master`. Su review encontró la fijación de sesión, que se cerró sin spec (`32fee6e`:
la Session se descarta si no es de la cuenta de la cookie; Logout renueva la cookie de
sesión). **E2** (`4be4c3f`): paso Ingreso a MES (`StepType` 11, primero en las tres guías,
0.05; DC −0.05) en pantalla completa: escritorio, script con el Code1, Edge con el
Repositorio, Hora a Hora (aviso provisional), Recursos MES con la etiqueta de la estación y el
login de NetWeaver; se califican la secuencia y el Code1 propio, por pantalla (D-68), con
`MesLoginRule`. **E3** (`7a19f5a`): Logout en el último paso abre "You have logged off";
cerrar Edge lleva al escritorio y el script con el Code1 cierra la sesión (el resumen sale
al cerrar la consola), con `LogoutCleanupRule`. **E4** (`e1227dd`): guía de Guided del
Ingreso y de la limpieza (Hora a Hora antes que Recursos MES, el Code1 del operador en el
texto, el usuario en el header después de Log On); dos correcciones del usuario al probar.
Reviews: `high` en E1 y en la fijación de sesión, `medium` en E2 y E3, `low` en E4. Tests:
151 (reglas del Ingreso y de la limpieza, y las copias de las guías comparadas con sus
scripts). Reglas en `docs/domain.md`; mecanismos en `docs/architecture.md`; flujo en
`docs/ui-reference/pre-sap-flow.md`.

## Notas EP-7 · Comprensión en la guía de Guided ✅ (2026-10-20, Fase 8.5, plan directo)
Fila 11 de la Fase 8.5, salida de T6 (`9a5433c`). Tres notas normales, solo texto en
`GuideScriptBuilder`, con los textos del plan aprobado: Resource Setup `open` (se repite al
sustituir un equipo), Line Clearance de apertura `activities` (reemplaza la nota de T7: siempre
después de Resource Setup y cuándo se registra de nuevo) y Assembly `open` (consumir antes de
la instrucción del MPI). El LC de cierre conserva su alerta. Sin cambios de BD, puntaje ni
Assessment. Probado por el usuario en el navegador.

## Spec 012 · Práctica libre ✅ (2026-10-20, Fase 8.5, T3)
Plan `docs/specs/012-plan.md`, cuatro entregas probadas por el usuario, con las enmiendas 1
y 2 de la spec y la enmienda 1 del plan. Pedido de Ernesto (revisión del 2026-10-09).
**E1** (`1b45779`): `ModePolicy` en `BLL/Scoring/` concentra lo que decidía cada modo
(guía, botones, secuencia, penalización, avance forzado, mensaje de fallo, resumen), sin
cambio de comportamiento. **E2** (`f2653a6`): modo **Práctica libre** (`Mode` = 2): sin guía
ni puntaje, todos los botones habilitados, "Ese no es el paso actual." sin intento para el
botón de otro paso (Log On sin Hora a Hora es intento fallido, hallazgo del review), sin
penalización ni avance forzado. **E3** (`af0bd2a`): Ver pista (con el paso actual), Saltar
paso e Ir a… (`Reason = Skipped`, reusa el avance forzado; tras el salto, `Response.Redirect`
como un F5, D-84 enmendada), barra sobre los modales (D-91) con el estilo "Fiori Industrial"
(decisión del usuario al probar, también en Abandon). **E4** (`56e8fcf`): resumen con los intentos por
paso ("Saltado (N fallos)") y pistas por tramo en el Ingreso a MES y en Logout (enmienda 2).
Sin scripts de BD (solo comentarios en `00`). Reviews: `high` en E1 y E3, `medium` en E2 y
E4. Tests: 184 (`ModePolicyTests`, `StepNamesTests`, `StepHintsTests`). Reglas en
`docs/domain.md`; mecanismos en `docs/architecture.md`.

## Spec 017 · Mensajes de error del MES ✅ (2026-10-21, Fase 8.5, fila 10)
Plan `docs/specs/017-plan.md`, cuatro entregas probadas por el usuario, con las enmiendas 1 a
6 del plan. Salida de T6 (EP-4, EP-5) con las capturas del ambiente de testing (T9). Primera
spec trabajada en su propia rama (`spec-017`), integrada a `TrainingApp` por fast-forward en
cada entrega. Regla de toda la spec (D-92): **ningún cambio de intento ni de puntaje**.
**E1** (`b3dfff8`, `0d0b0da`): catálogo `MesMessage` en la BLL (textos literales, número y
superficie) y `MesError` en los resultados. Banner rojo del POD (`#podMesError`, sin
auto-hide, lo llena `podShowMesErrors` desde el servidor o el cliente), franja de Assemble
Components y mensaje de SFC Split que reemplaza el formulario con un OK que cierra la ventana
(AUD-63, AUD-64, AUD-69). Start sin SFC o con uno ajeno (`22506`, `22507`), sin fila
(`12906`, también en Serialized sin ninguna, corrección del usuario), barcode no numérico
(`17150`) y Qty del Split (`13875`, `13213`, `13885`). **E2** (`9f0ea3a`, `7b27967`): el botón
de otro paso muestra el mensaje del MES si el MES lo rechazaría (`MesSequenceRule` y
`MesSequenceState.Build`, puras y con tests; C-1 a C-3): Resource Setup provisional (D-111),
`22501` del LC de apertura ("status is NO" sacado de los intentos, enmienda 3), sin SFC y
`15246` por pieza seleccionada. Correcciones del usuario: el SFC cuenta como iniciado recién
tras el Start, y sin selección el MES dice "SFC is missing" (enmienda 4). **E3** (`7ee24ed`):
la franja de Data Collection Entry con el primer error (C-6: `10015`, `12205`, `15232`,
`15233`); M-17 fuera (D-120, enmienda 5). **E4** (`a39b6c1`, `f77147e`, `b692d45`): Setup
Type **Material** en Resource Setup (D-94); explicaciones en español (`MesErrorNotes`, D-114):
la nota de la guía en Guided, también para los chequeos del cliente (`PodGuide.note`,
enmienda 6), y en Práctica libre un botón "¿Qué significa?" junto al mensaje que abre un
aviso de información (decisión del usuario al probar, en lugar de Ver pista). Assessment no
recibe explicaciones. Sin scripts de BD. Reviews: `medium` en E1, E2 y E4, `low` en E3.
Tests: 281 (`MesMessageTests`, `MesSequenceRuleTests`, `MesSequenceStateTests`,
`StepValidatorDataCollectionTests`, `MesErrorNotesTests`, `GuideMesErrorNoteTests`). Reglas
en `docs/domain.md` → "Mensajes del MES"; mecanismos en `docs/architecture.md`; pantallas en
`general-flow.md`; EP-4/EP-5 enmendados en `evaluacion-practica-mes.md`.

## Optimización de la documentación ✅ (2026-10-21, sin spec)
Plan aprobado por el usuario en el chat, en 7 entregas sobre la rama `docs/optimizacion`
(`81f3407`…`d9337ae`). **E0:** se integró W23, que estaba olvidado en su rama desde el
2026-10-15, y se borraron 4 worktrees de docs ya integrados; la auditoría de fidelidad sigue
en su rama por D-115. **E1:** `docs/process.md`: cada sesión commitea solo sus rutas en la
carpeta real y desaparece el worktree de docs obligatorio (se contradecía con la sección 7);
un job en segundo plano no integra su rama; la exclusión de escala del review aplica solo al
motor; las reglas nuevas de la BLL llevan tests. **E2:** el roadmap se dividió por dueño:
`docs/status.md` y `docs/history.md` (Desarrollo) y `docs/roadmap.md` (Arquitectura y
Planeación). **E3:** `docs/decisions.md`, índice de D-1…D-119 y de los B-n, que pasan a
numeración global. **E4:** CLAUDE.md de 16,9 a 13,3 KB (Auth Flow a `docs/architecture.md`)
y dos contradicciones del glosario. **E5:** `general-flow.md` apunta a `docs/domain.md` para la
calificación; Logout con la limpieza de la spec 016. **E6:** la memoria, al día. Iniciar una
sesión pasó de leer 81 KB (CLAUDE.md y el roadmap entero) a 21 KB (CLAUDE.md y
`docs/status.md`). La medición con preguntas de prueba se omitió (usuario). Pendiente para
el cierre de la spec 017: `general-flow.md` todavía no refleja sus mensajes del MES (hecho en
el cierre de la 017).

## Fila 12 · W23, SFC serializado `{12NC}-{serial}` ✅ (2026-10-22, Fase 8.5, plan directo)
Plan aprobado por el usuario el 2026-10-21 (`docs/roadmap.md` → "Plan de la fila 12"), con
D-121 a D-123 y D-125 a D-128. Las piezas de una orden Serialized pasan de
`{ShopOrder}-{sufijo}` a `{12NC}-{serial}`, como en el POD real de Wires:
`PodDefaults.MaterialNumber(Area)` arma un 12NC simulado (`"3000"` + el `Id` del área con 8
dígitos) y `SerialSfcGenerator` sortea cuántos dígitos tiene el inicio (de 1 a 6, decisión del
usuario al implementar: el plan decía 5) y numera las 4-5 piezas consecutivas, con la última
≤ `999999`. `SimulatedOrder` guarda `MaterialNumber`; el As-Built de Log NC muestra
`{12NC}/A` y `SimBom` sigue con la Shop Order. NonSerialized no cambia. Se confirmó que nada
partía el SFC por `-` ni lo comparaba con la Shop Order. Sin scripts de BD ni cambios de
puntaje. Review `low` sin hallazgos. Tests: 283 (`SerialSfcGeneratorTests` reescrito,
`PodDefaultsMaterialNumberTests`). W23 resuelto en `wi-produccion-disposables.md`; glosario
y `general-flow.md` al día. Probada por el usuario en el navegador.

## Fila 13 · Quitar `RequiresNCComponent` ✅ (2026-10-22, Fase 8.5, plan directo)
Plan aprobado por el usuario el 2026-10-21 (`docs/roadmap.md` → "Plan de la fila 13", D-124).
La columna `Areas.RequiresNCComponent` no se leía desde la enmienda 4 de la spec 014
(Component lo decide el método del código NC). Se quitó de `00_Schema.sql` (con su default),
de los `spAreaGet*` de `02`, de los catálogos `05`, `06` y `08`, de `Area` y de
`AreaRepository`. Sin cambio de comportamiento ni de puntaje. Verificado en LocalDB: los
scripts nuevos corren limpios dos veces y `Areas` queda con los mismos datos menos la
columna. La BD del usuario se recreó con `00`–`09`, sin `ALTER` aparte. A pedido del
usuario, `Database/Ops/RunAllScripts.ps1` corre los scripts en orden (`-Recreate` borra y
crea la BD antes); documentado en `docs/process.md` §6. Sin review (solo se borraron líneas).
Tests: 283. Probada por el usuario en el navegador.

## Fase 9 · Deploy del MVP ✅ (2026-10-22, plan directo)
Plan aprobado por el usuario el 2026-10-22 (`docs/specs/fase-9-plan.md`, D-129 a D-135).
- **E1 (`584cdf2`):** perfil de publicación `MVP.pubxml` (Release, precompilado, sin `.pdb`) y
  `docs/deploy.md` para TI. Regresión en Assessment (LoFlo y Moldeo) confirmada por el usuario.
- **E2 (`a4ffefb`, en la rama `fase-10`):**
  - AUD-28 lo barato: ID de Session nuevo en el login (el "Último acceso" viaja en el ticket de
    Forms); `Shared/AppLog` con errores, logins fallidos y bloqueos en `App_Data/Logs` (sin
    contraseñas, retención de 60 días); cabeceras nosniff, SAMEORIGIN y same-origin; sin
    `X-AspNet-Version` ni `X-Powered-By`; cookies HttpOnly; `requireSSL` comentado en
    `Web.Release.config` hasta que TI confirme HTTPS.
  - AUD-29, solo parches: Newtonsoft 13.0.4, jQuery 3.7.1 y Microsoft.Web.Infrastructure 2.0.1;
    en los tests, DiagnosticSource 6.0.2 y Reflection.Metadata 8.0.1.
  - El comentario de `Web.config` apunta a `docs/deploy.md`, que suma el permiso de escritura
    en `App_Data`.
  - Review `medium`: 9 hallazgos, 5 aplicados. Tests: 283.
  - Se integró a `TrainingApp` antes de probarse, a pedido del usuario. Probada después por el
    usuario en el navegador.
- **Queda abierto:** la cabecera `Server`, hasta que TI confirme la versión de IIS (D-144);
  publicar, la prueba de humo en el servidor (D-19) y el mensaje de lanzamiento (usuario).

## Fase 10 · Dashboard de Training (spec 018) ✅ (2026-10-22 a 2026-10-23)
Spec y plan aprobados por el usuario el 2026-10-22 (`docs/specs/018-dashboard-training.md`,
`docs/specs/018-plan.md`; D-136 a D-150). Rama y worktree `fase-10`; cada entrega se integró
a `TrainingApp` con el "funciona" del usuario. Mecanismo: `docs/architecture.md` →
"Dashboard de Training"; reglas: `docs/domain.md` → "Dashboard de Training".
- **E1 (`a4ffefb`):**
  - `CanViewReports`, el menú del dashboard, `ReadinessPolicy` y la lista de Operadores;
  - el aviso al borrar un usuario, los índices y `10_spDashboard.sql`;
  - el generador `GenerateDashboardData.sql`;
  - ClosedXML verificado (D-140) y Chart.js 4.5.1 local.
  - Paginar en la BLL y no en el SP fue un desvío aceptado (D-143).
- **E2 (`33a6a9d`):** el detalle del Operador (estado, sesiones, pasos a reforzar) con
  `StepRuns`, y B-3 (vuelta al inicio del rol nuevo).
- **E3 (`e542fce`):** Pasos.
  - El SP devuelve los intentos y la BLL calcula (D-146).
  - Con "Todas", un resumen agrupado por tipo de paso: lo pidió el usuario al probar (D-147).
- **E4 (`d519151`):** el Resumen como inicio del admin: estados, tasas, actividad por semana de
  la planta (`Shared/PlantTime`) y el top 5 (el mismo cálculo que Pasos).
- **E5 (`bd82796`):** Seguimiento (S-1, S-2, S-3; D-141).
- **E6 (`c5b0741`):** descargas a `.xlsx` con los mismos filtros que cada vista
  (`Export.ashx`, `ReportFilters`).
  - Un salto siempre resuelve el paso (D-83).
  - El usuario pidió una fase de mejora de UI/UX al cerrar la 10: es la Fase 11 del roadmap.
- **E7 (`e71f7d2`):** rendimiento (AC-24).
  - Vistas y descargas en menos de 3 s con 1600 operadores y un año de datos.
  - Descarga de sesiones: de 3,75 a 1,3 s con 30 días y de 16,7 a 4,7 s con Todo (excepción con
    aviso, D-148); en streaming con `ZipArchive` (D-149).
  - El índice ampliado se descartó y el plan B no aplicó.
  - BD de prueba borrada con la aprobación del usuario.
- **E8:** la curva de aprendizaje en el detalle (AC-15) y la ficha imprimible desde el navegador
  (AC-16, D-150). Review `low` sin hallazgos. Tests: 356. Con ella se hicieron los "Docs al
  cerrar": `architecture.md`, `domain.md`, `CLAUDE.md`, esta sección y la enmienda de AUD-29.
- **Queda abierto:**
  - la Fase 11 (UI/UX del dashboard y diseño de los `.xlsx`);
  - volver a correr `10_spDashboard.sql` en la BD local y en la de producción (usuario).

## R1 y R2 · Ajustes de la revisión con Ernesto ✅ (2026-10-26, plan directo y spec 016 → enmienda 1)
Plan `docs/specs/r1-r2-plan.md` y enmienda 1 de la spec 016, aprobados por el usuario el
2026-10-26 (D-151, D-152). Rama `r1-r2`; una entrega (E1), probada por el usuario el 2026-10-26.
- **R1 (D-151):** Hora a Hora sale del Ingreso a MES, que queda script → Repositorio →
  Recursos MES → login.
  - Se borraron la pantalla de Hora a Hora 3.0, su pestaña, el aviso de D-72, `HoraHoraDone`,
    la acción `HoraHoraStart`, su pista, los `.mes-hh-*` y `horahora-photo.jpg`.
  - La tarjeta (3.0) se ve y no hace nada, como la (1.0).
  - Una `HoraHoraStart` de una página vieja la descarta el control, sin intento.
  - La guía va del Repositorio a Recursos MES; también salió el subpaso `backToRepo`.
- **Corrección durante la E1 (usuario, con el aviso de Desarrollo):** Log On sin Recursos MES
  sigue inválido, sin intento, porque el login no se abre sin él. No es "fallido y vuelve al
  Repositorio".
- **R2 (D-152):** las alertas de Remove All (Resource Setup) y del subpaso 1 de Log NC terminan
  sin la frase disciplinaria.
- Sin cambios de BD, pesos ni versiones de guía. Los intentos guardados con
  `MES.HoraHora.Iniciar` se siguen viendo en el dashboard.

## Fase 11 · UI/UX del dashboard y diseño de los `.xlsx` (brief 019) ✅ (2026-10-26 a 2026-10-27)
Brief y plan aprobados por el usuario el 2026-10-26 (`docs/specs/019-ui-ux-dashboard.md`,
`docs/specs/019-plan.md`; D-153 a D-164; mockup v1 aprobado antes del plan, D-159). Rama y
worktree `fase-11`; cada entrega, probada por el usuario en su navegador; integrada en
`TrainingApp` por fast-forward el 2026-10-27 (`a22eec1`, con las E1 a E3 de R3 debajo).
Mecanismo: `docs/architecture.md` → "Sistema de diseño y tema". Sin cambios de BLL, BD ni
puntaje, salvo el orden de Seguimiento (E5).
- **E1 (`a474c7d`):** Bootstrap 5.2.3 → 5.3.8 (D-155), sin cambios de diseño y con regresión
  completa.
- **E2 (`840a208`):**
  - el sistema de diseño en `admin.css`: los tokens del mockup en claro y en oscuro, y el borde
    propio de los campos (D-162);
  - Bootstrap Icons 1.13.2 local y el tema con `theme.js` en `Admin.Master` y `Site.Master`;
    el POD no lo recibe;
  - login, cambio de contraseña, Usuarios y el inicio del operador (D-163).
- **E3 (`121947f`):** el Resumen como el mockup ("¿Quién está listo?", Actividad y "Dónde se
  falla más") con `admin-charts.js`. Mismas cifras de `GetSummary`; sin "N sin iniciar" ni el
  número de Seguimiento (D-164). Se borró `Site.css`, sin uso.
- **E4 (`3786397`):** Operadores y Operador. La insignia con ícono por guía sale de
  `ReportText.StateCss`/`StateIcon`. La ficha se imprime en claro (D-157).
- **E5 (`6f7b0a2`):** Pasos (la barra de %, los 2 motivos más frecuentes y "Ver todos" en el
  cliente) y Seguimiento (las reglas como chips con texto). Primero quien cumple más reglas,
  con el orden en `DashboardService.GetFollowUp` y su test.
- **E6 (`6bdbfea`):** los cuatro `.xlsx` con la fila de título (vista, filtros y fecha), el
  encabezado con el acento, fijo y con autofiltro, y los estados con color (D-158); mismas
  columnas y datos. La descarga de sesiones sigue en streaming y tarda lo mismo (3.9 → 3.7 s con
  "Todo").
- **Limpieza:** el alias de `GuideSummary` en el Resumen, que se necesitó al integrar R3
  (`11982c1`), se quitó en `5e172f0` cuando R3 renombró la suya a `Domain.PublishedGuide`.
  Tests: 390.
- **Docs al cerrar (2026-10-27):** `architecture.md`, `CLAUDE.md` → "Sesión y seguridad", la
  enmienda de AUD-29 en la auditoría `2026-10-01` y esta sección.
- **AC-7 (2026-10-27):** con la BD de volumen (1600 operadores y un año de datos), todas las
  vistas por debajo de 3 s y en línea con la Fase 10. La más lenta es el Resumen: 0,40 s con 30
  días y 0,98 s con Todo. Sin cambios de SPs ni índices; la BD de
  prueba se borró. Cifras: `019-plan.md` → "Docs al cerrar".

## R3 · Custom Resource Setup (spec 020) ✅ (2026-10-26 a 2026-10-27)
Spec y plan aprobados por el usuario el 2026-10-26 (`docs/specs/020-custom-resource-setup.md`,
`docs/specs/020-plan.md`, `42dafdf`; D-180 a D-201). Sesión `Custom-RS`, directo en
`TrainingApp`, en paralelo con la Fase 11; esta sección la escribe Desarrollo con sus
resúmenes (D-197).
- **E1 (`144d845`, datos y DAL):**
  - la columna `ResourceSetupType` (Standard o Custom) en `TrainingGuides` y
    `TrainingSessions`;
  - los SPs del admin para listar las guías publicadas y cambiarles el tipo, y la versión
    nueva de una guía hereda el tipo (D-198);
  - `SetGuideResourceSetupType.sql` para el piloto. Todas las guías arrancan en Standard
    (D-194).
- **E2 (`a0c7c77`, BLL):**
  - los datos de Custom salen de `BLL/Training/CustomResourceSetup.cs`;
  - la sesión copia el tipo de la guía al empezar (D-188);
  - las reglas del Save (D-193) y sus excepciones sin intento (D-191, D-196);
  - `AccessPolicy.CanManageGuides` y `GuideAdminService`. 35 tests nuevos.
- **E3 (`3a18960`, `82d000d`, UI):** el modal Custom Resource Setup en `GuidedTest`, con su
  guion de Guided y su pista. La enmienda 1, al probar, dejó siempre 2 Tool Groups (D-200) y el
  Tool Number en cada fila (D-201). `Domain.GuideSummary` pasó a `Domain.PublishedGuide`.
- **E4 (`5d12e80`, Admin):**
  - la página Guías (`Pages/Admin/Guides.aspx`): guías publicadas (D-199), un selector
    Estándar/Custom por fila y la confirmación al guardar (D-195), que vale para las sesiones
    nuevas;
  - solo con `CanManageGuides`, verificado también en la BLL (B-3 al perder la capacidad);
  - "Guías" en el menú de `Admin.Master`.
  - Probada por el usuario. Tests: 390.
- **E5 (`3336221`, revisión final y docs de cierre):**
  - el servidor exige que el Save traiga exactamente los Tool Groups de la sesión, todos con su
    Tool Number (D-201). Antes, una respuesta editada a mano con una fila de menos se calificaba
    como correcta;
  - la guía de Guided arma un subpaso por cada Tool Group de la sesión, resalta su fila y
    termina con el OK del Success (D-190);
  - un Save fallido de Resource Setup crea la orden con Setup State "Open" (spec 005, D-193) y
    ahora la muestra en el Browse Work List en esa misma respuesta, también en el estándar;
  - el Code1 del modal sale de `ctx.Code1`.
  - Docs: `domain.md` (Resource Setup con sus dos pantallas), `architecture.md` (el árbol y "Dos
    pantallas para Resource Setup"), `CLAUDE.md` (`CanManageGuides` y `<%# %>` en atributos de
    controles de servidor, aprobado por el usuario), `inventario-pantallas.md` (#31), el
    roadmap, la spec 020 en Implemented y todas las casillas del plan.
  - Probada por el usuario. Tests: 393.
- **Queda abierto:**
  - B-20: `StartSession` no revisa que la guía esté publicada;
  - correr `00`, `02` y `03` en la BD de producción antes de publicar (usuario).

## Spec 021 · Área asignada por operador ✅ (2026-10-27)
**Objetivo:** cada operador entrena en una sola área y solo inicia las guías publicadas de esa
área (spec `docs/specs/021-area-operador.md`, plan `021-plan.md`, D-165 a D-179 y D-208). Rama
`spec-021`, en el worktree `.claude/worktrees/fase-11`.
- **E1 (`425e944`, datos y DAL):** `Users.AreaId` con FK a `Areas` (en el `CREATE` y con su
  `ALTER` idempotente), el área en `spUserGetBy*`, `spUserSearch` y `spDashboardOperators`;
  `spUserChooseArea`, `spUserSetArea` y `spAreaGetEligible`. Se quitó "NO USAR EN PRODUCCIÓN"
  de `05` (D-178). Probada en LocalDB (limpia y sobre una BD anterior, dos corridas).
- **E2 (`d6af08d`, BLL):** `TrainingAccessRule` y `StartSession` validando antes de abandonar
  la sesión en curso (AC-8; corrige **B-20** y **B-4**, enmienda 1 del plan),
  `OperatorAreaService.ChooseOwnArea`, `UserAdminService.SetArea`, `AreaLabel`. Tests: 414.
- **E3 (`a483731`, operador):** "Elige tu área" (`ChooseArea.aspx`, selector agrupado por
  producto y la confirmación), `SessionHelper.MustChooseArea` y `RefreshUserSession`; el inicio
  con el área como texto y solo sus guías.
- **E4 (`03cd8df`, Usuarios):** la columna Área con un lápiz que abre el selector en la fila.
  Al probarla, el usuario quitó "Quitar área", también de la BLL (enmienda 1 de la spec,
  D-208). **B-3** ya estaba corregido por la 018 (E2).
- **E5 (dashboard y cierre):** el área en Operadores (lista y Excel) y en el encabezado y la
  ficha del Operador; filtros y números sin cambios (D-175). Docs: `domain.md` ("Área del
  operador"), `architecture.md` (paso 6 del login), `CLAUDE.md` (`MustChooseArea` en
  "Sesión y seguridad"), `decisions.md` (B-20 corregido) y la spec en Implemented. Tests: 415.
- Probada por el usuario entrega por entrega.
- **Queda abierto:** correr `00`, `01`, `02`, `05` y `10` en la BD de producción antes de
  publicar y avisar a los trainers (D-170).

## Spec 024 · Dashboard por área ✅ (2026-10-29)
**Objetivo:** mirar Operadores y Operador por el área de cada operador (spec
`docs/specs/024-dashboard-por-area.md`, plan `024-plan.md`, D-260 a D-274; enmienda D-175).
Hecha de punta a punta en la nube (variante temporal de `process.md`), revisada en local por
Planeación (enmienda 1: D-272 a D-274) y traída por Desarrollo en la rama `spec-024`.
- **E1 (`091f62c`, datos y BLL):** `AreaId` en `spDashboardGuides` y `spDashboardOperators`;
  `BLL/Reports/OperatorArea.cs` (sus guías y las de otras áreas, estado en su área, Sin iniciar
  nueva, D-263); `DashboardService` filtra por área y estado antes de paginar.
- **E2 (`edf946a`, Operadores):** filtro Área agrupado por producto (D-262), columnas Área,
  Estado en su área y Última actividad; `?guide=` viejo e `?area=` inválido valen Todas; el
  Excel con las columnas nuevas.
- **E3 (`f5b1d74`, Operador):** "Su área" y "Otras áreas (entrenamiento cruzado)" en un
  `<details>`; Sesiones con columna Área (D-272); la ficha con "También entrenó" y Reforzar y
  Sesiones completos (D-270, D-273).
- **E4 (`bc473e9`, cierre en la nube):** `domain.md`, `architecture.md` y `decisions.md` (D-175
  enmendada). El hook y `tools/` de la nube no entraron (D-274).
- **Verificación local (Desarrollo):** merge de `TrainingApp`, Rebuild, `aspnet_compiler`, 461
  tests en verde y `10_spDashboard.sql` en la BD del usuario. Probada por el usuario.
- **Queda abierto:** correr `10_spDashboard.sql` en la BD de producción antes de publicar.

## Spec 022 · Un mockup por pantalla (en curso, 2026-10-28)
**Objetivo:** rediseñar cada pantalla de `Admin.Master` según su mockup aprobado (spec
`docs/specs/022-mockups-por-pantalla.md`, plan `022-plan.md`). Rama `spec-022`, en el worktree
`.claude/worktrees/fase-11`, en el orden de D-219.
- **E1 (`642cacd`, sistema de diseño):** los tokens de D-221 y `.mt-status-line` (D-222) en
  `admin.css`, sin cambio visible.
- **E2a (Login):** el panel dividido 3b (D-220 a D-225) desde `cloud/022-login-3b`;
  `BtnLogin` pasa a `LinkButton`, los errores van a `.mt-status-line` con `aria-live` y
  `Admin.Master` gana el modo `FullBleed` (D-223). `/code-review low` sin hallazgos. Capturas en
  `022-design/login-*.png` (D-240). Probada por el usuario.
- **E2b (cambio de contraseña):** la 2a (D-228 a D-232) desde `cloud/022-cambio-contrasena`;
  pantalla completa siempre (D-230), los 3 pasos del primer acceso, errores en
  `.mt-status-line` y los botones como `LinkButton`. `/code-review low` sin hallazgos. Capturas
  del handoff, sin reemplazar (decisión del usuario). Probada por el usuario.
- **E3 (Guías):** la 2b (D-233 a D-236) desde `cloud/022-guias`; el orden Custom primero y el
  conteo por tipo pasan del code-behind del handoff a `GuideAdminService.GetPublishedGuides`
  (`PublishedGuideList`, D-235, sin test); el aviso de guardado nombra la guía (D-236); la
  leyenda, debajo del título (ajuste del usuario). `/code-review low` sin hallazgos. Sin
  capturas. Probada por el usuario.

## Spec 023 · Filtro de período en Pasos y Resumen ✅ (2026-10-28)
**Objetivo:** en Pasos y Resumen, elegir el período (cinco atajos, Todo y Personalizado) y ver
cada número clave contra el período anterior, con su base (spec `docs/specs/023-filtro-periodo.md`,
plan `023-plan.md`, D-209 a D-218; enmienda el punto 8 de la 018). Rama `spec-022`, en el
worktree `.claude/worktrees/fase-11`, después de la 022 E1 (D-219).
- **E1 (`ca9980f`, datos y BLL):** `@To` exclusivo (`DATETIME2`) en `spDashboardSessions` y
  `spDashboardStepAttempts`; `ReportPeriod` con los 7 períodos (Este mes al entrar y como valor
  por defecto), `ReportRange` y `ReportPeriods.Resolve` en hora de la planta, `ReportChange`;
  `DashboardService` con el cambio de la tasa de aprobación, las sesiones, el Top 5 y Pasos (una
  sola lectura de intentos) y `SummaryRules.Activity` por día, semana o mes. Nota del usuario en
  el plan: un Inicio anterior a 2000 o un período fuera del enum también son inválidos.
- **E2 (`55cfd7e`, Resumen):** `UI/Controls/Admin/PeriodFilter.ascx`, `ReportFilters` con
  `?period=…&from=…&to=…`, el aviso de período inválido, `ChangeView` (flecha, valor,
  referencia y base; mejora o empeora también en texto), "Sesiones del período", la gráfica
  con su granularidad y el período en el enlace a Pasos y en el Excel de sesiones.
- **E3 (`388dd3a`, Pasos):** el mismo control, el cambio por paso y el Excel de Pasos con el
  rango y las columnas "% con fallo (anterior)" y "Cambio (pts)", vacías con Todo.
- **E4 (rendimiento y cierre):** P-13 cumplido sin tocar SPs ni índices (Resumen 0,66 s y Pasos
  0,55 s con Todo; Excel de Pasos 0,73 s; cifras en el plan); AC-9 sin cambios. Docs:
  `domain.md` ("Período" de las vistas), `architecture.md` ("Período y comparación") y la spec
  en Implemented. Tests: 459.
- Probada por el usuario entrega por entrega. Un timeout de SQL pasajero en la BD local del
  usuario durante la E2 no se repitió; la medición con volumen descartó el código.
- **Queda abierto:** correr `10_spDashboard.sql` en la BD de producción antes de publicar.
