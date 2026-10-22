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
