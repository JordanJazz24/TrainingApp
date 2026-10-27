# Estado — MESTrainingSimulator

> **Dónde quedamos.** Lo escribe Desarrollo (`docs/process.md` → "Tres sesiones de trabajo").
> Leer al iniciar cada sesión; actualizar al cerrar una entrega, una fase o una sesión de
> trabajo relevante. Corto: lo cerrado va a `docs/history.md`; las fases, su alcance ("No
> incluye") y los planes, a `docs/roadmap.md`. Separado del roadmap el 2026-10-21.

## 📍 Estado actual

**Última actualización:** 2026-10-27 · **Branch:** `TrainingApp`; la Fase 11 y R3 están
cerradas e integradas (`33afc98`). La spec 021 se trabaja en la rama `spec-021`, en el worktree
`.claude/worktrees/fase-11` (la carpeta se reutilizó; la rama `fase-11` ya no está en ningún
worktree) · **Último commit de código:** R3 E5 (`3336221`), en `TrainingApp`
(`git log --oneline -10` para ver el estado real)

- **Fase 8.5 · Entrega de noviembre** (2.ª semana de noviembre; plan y orden en
  `docs/roadmap.md` → "Fase 8.5"). Cerradas: T1 (preparado; la prueba en el servidor real
  quedó diferida, D-19), 011, 013, T7, 015, 014, 016, T6, notas EP-7, 012, 017 y las **filas 12 y 13**
  (detalle en `docs/history.md`). La 016 ya se puede subir a `main` (D-81); lo sube el
  usuario.
- **Spec 017 ✅** (2026-10-21, fila 10, ver "Historial"): mensajes de error del MES en
  inglés y con número, en los tres modos y sin cambiar la calificación; Setup Type Material;
  explicaciones en español (nota de la guía en Guided, botón "¿Qué significa?" en Práctica
  libre). Probada por el usuario. Se trabajó en la rama `spec-017`, ya integrada en
  `TrainingApp`.
- **Fila 12 ✅** (2026-10-22, W23, ver "Historial"): las piezas Serialized pasan a
  `{12NC}-{serial}`, con un 12NC simulado por área y seriales consecutivos de 1 a 6 dígitos;
  el As-Built de Log NC muestra `{12NC}/A`. Sin cambios de BD ni de puntaje. Probada por el
  usuario.
- **Fila 13 ✅** (2026-10-22, ver "Historial"): se quitó `Areas.RequiresNCComponent` (sin uso
  desde la enmienda 4 de la spec 014), sin cambio de comportamiento; BD del usuario recreada
  con `00`–`09` (D-124). Nuevo `Database/Ops/RunAllScripts.ps1` para correr los scripts en
  orden. Probada por el usuario.
- **Fase 9 · Deploy del MVP ✅** (2026-10-22, ver "Historial"; plan
  `docs/specs/fase-9-plan.md`, D-129 a D-135): E1 (perfil `MVP.pubxml` y `docs/deploy.md`) y
  E2 (AUD-28 lo barato y los parches de AUD-29), probadas por el usuario.
  - **Pendiente del usuario** (reparto nuevo, 2026-10-22: él prepara la BD y entrega la
    carpeta con su `connectionStrings.config`; `docs/deploy.md` → 0): la BD de producción ya
    tiene `00`–`10`, **falta el primer administrador** (`CreateAdmin.sql`; revisado el
    2026-10-22: 0 admins); publicar, copiar `connectionStrings.config` a `publish\MVP` y
    entregarle a TI la carpeta y `docs/deploy.md`; la prueba de humo en el servidor (cierra D-19); el mensaje de lanzamiento
    (Redefinity de demostración, D-132; retroalimentación fuera de la app, D-134); preguntarle a
    TI la versión de IIS (cabecera `Server`, D-144) y si hay HTTPS (`requireSSL`).
  - **El reparto nuevo es D-145** (reemplaza D-133; `fase-9-plan.md` → enmienda 3). La app en
    producción usa el login `developer3`, que tiene `CONTROL` sobre la BD; `docs/deploy.md` →
    2.3 recomienda uno con solo `GRANT EXECUTE` (lo decide el usuario; no bloquea el MVP, D-130).
- **Fase 10 · Dashboard de Training (spec 018) ✅** (2026-10-22 a 2026-10-23, ver
  "Historial"; plan `docs/specs/018-plan.md`, D-136 a D-150): **E1 a E8**, probadas por el
  usuario e integradas en `TrainingApp`:
  - Resumen (el inicio del admin), Operadores, Operador (con la curva y la ficha imprimible),
    Pasos y Seguimiento;
  - las descargas a Excel;
  - el rendimiento (AC-24).

  Los "Docs al cerrar" están hechos. Arquitectura y Planeación marca la fase en el roadmap. La
  rama y el worktree `fase-10` siguen hasta que el usuario los cierre (`docs/process.md` §7).
  - **Pendiente del usuario:** volver a correr `10_spDashboard.sql` en su BD local y en la de
    producción (trae los SPs y los filtros de las E2, E3, E5 y E7).
  - **Para probar el dashboard con volumen:**
    1. crear la BD `TrainingApp_DashboardTest` en LocalDB (`00`–`10`) y correr
       `Database/Ops/GenerateDashboardData.sql` (1600 operadores; admin = Code1 del usuario);
    2. medir con `DashboardPerformanceTests` (test explícito; ids del admin y de un operador en
       `MES_PERF_ADMIN` y `MES_PERF_OPERATOR`).

    Se borró al cerrar la E7.
- **Lo que sigue después de la Fase 10** (orden del usuario, 2026-10-23; `docs/roadmap.md` →
  "Después de la Fase 10"):
  1. **R1 y R2 ✅** (2026-10-26, ver "Historial"): Hora a Hora fuera del Ingreso a MES y la guía
     sin acciones disciplinarias (D-151, D-152).
  2. **Fase 11 · UI/UX del dashboard y diseño de los `.xlsx` ✅** (2026-10-26 a 2026-10-27,
     ver "Historial"; brief `docs/specs/019-ui-ux-dashboard.md` y plan `docs/specs/019-plan.md`,
     D-153 a D-164): **E1 a E6**, probadas por el usuario e integradas en `TrainingApp`:
     - Bootstrap 5.3.8 y el sistema de diseño con tema claro/oscuro en `Admin.Master` y
       `Site.Master` (el POD no cambia);
     - las cinco vistas del dashboard, login, cambio de contraseña, Usuarios e inicio del
       operador;
     - los cuatro `.xlsx`.

     AC-7 medido el 2026-10-27: todas las vistas por debajo de 3 s con la BD de volumen (cifras
     en `019-plan.md` → "Docs al cerrar"); la BD de prueba se borró. Los "Docs al cerrar" están
     hechos; la fase está marcada ✅ en el roadmap (`7cd4691`).
     - **Pendiente del usuario** (`docs/process.md` §7): el cierre ya está integrado en
       `TrainingApp` (`33afc98`, 2026-10-27). Falta `git branch -d fase-11` (su carpeta ahora
       es el worktree de `spec-021`) y, si ya no se usa, borrar la BD `TrainingApp_Fase11` de
       LocalDB. Con `fase-10`: `git worktree prune` (su carpeta ya no existe) y
       `TrainingApp_Fase10`.
  3. **R3 · Custom Resource Setup ✅** (2026-10-26 a 2026-10-27, ver "Historial"; spec y plan
     `docs/specs/020-*.md`, D-180 a D-201): **E1 a E5**, probadas por el usuario, directo en
     `TrainingApp` (sesión `Custom-RS`):
     - la columna `ResourceSetupType` por guía y por sesión;
     - el modal Custom Resource Setup en el POD, con su guía de Guided;
     - la página Guías del admin para elegir Estándar o Custom (`CanManageGuides`).

     Todas las guías siguen en Standard hasta que se elija el piloto (D-194). Tests: 393.
     - **Pendiente del usuario:** correr `00`, `02` y `03` en la BD de producción antes de
       publicar este código; el `03` y el código van juntos (sin él, iniciar una sesión
       falla). La BD local `TrainingApp` ya los tiene. Para el piloto,
       `Database/Ops/SetGuideResourceSetupType.sql` o la página Guías.
  4. **Spec 021 · Área asignada por operador** (en curso desde 2026-10-27; spec y plan
     `docs/specs/021-*.md`, D-165 a D-179; cierra B-20 y B-3), rama `spec-021`:
     - **E1 · Datos y DAL:** `Users.AreaId` (FK a `Areas`), los SPs del usuario y de Operadores
       con el área, `spUserChooseArea`, `spUserSetArea` y `spAreaGetEligible`; Domain y DAL.
       Sin cambios visibles. Probada en LocalDB (limpia y sobre una BD anterior con usuarios,
       sesiones e intentos; dos corridas). Tests: 393.
     - **E2 · BLL:** `TrainingAccessRule` (quién inicia qué guía; corrige B-20),
       `StartSession` valida antes de abandonar la sesión en curso, `OperatorAreaService`
       (elegir la propia), `UserAdminService.SetArea`, `TrainingGuideService.GetEligibleAreas` y
       `GetStartOptions`, `AreaLabel` ("Producto · Área"); B-4 (modo no definido, enmienda 1).
       Tests: 414. Hasta la E3, un operador
       sin área no puede iniciar ninguna guía (página de error, esperado).
     - Sigue la E3 (UI del operador: "Elige tu área" e inicio).
     - **Pendiente del usuario:** correr `00`, `01`, `02`, `05` y `10` en su BD local antes
       de probar la E3.
- **Guía de Subensamble 1 (2026-10-26, pedido del usuario para el MVP de los trainers):**
  `11_WiresSubensamble1Catalog.sql` y `12_WiresSubensamble1Guide.sql`. Subensamble 1 pasa a
  Serialized con Assembly y DC (la forma de LoFlo; catálogo SIM) y sale de `05`. Probados en
  LocalDB (limpia, re-ejecución y actualización desde el `05` anterior: el área conserva su
  Id). **Pendiente del usuario:** correr `05`, `11` y `12` en su BD local y en la de
  producción, y probar la guía en el navegador.
- **Documentación ✅ (2026-10-21, sin spec):** optimización de los docs (ver
  `docs/history.md`).
- **Para mostrar a negocio:**
  [hoja de requerimientos](https://claude.ai/code/artifact/7e0ae556-9a55-4d78-8ff7-a20b2419e352),
  una foto del 2026-10-09 (la fuente de verdad sigue siendo el repo); se actualiza solo
  cuando el usuario lo pida.
- **Pendientes del usuario (diferidos):** revisar la auditoría de fidelidad AUD-45 a AUD-70
  (`docs/audits/2026-10-19-fidelidad-ui.md`, integrada en `TrainingApp` el 2026-10-22;
  AUD-63/64/69 ya ☑ vía la 017) y marcar sus casillas; las capturas de los 4 modales del Start de Moldeo (inventario #8);
  rotar la contraseña de `sa` de su BD local y actualizar su `connectionStrings.config` (D-8;
  en el servidor no aplica: la app usa el login de producción, no `sa`), y probar T1 en el servidor IIS real
  (D-19; checklist en `docs/history.md` → "T1").
- **Descartado:** que la guía sugiera los códigos NC más comunes (spec 010, R6).

## Deuda y riesgos conocidos

Solo lo abierto; lo resuelto queda en la auditoría o la spec que lo cerró. La auditoría
final (D-16) repasó esta lista.

- **B-20:** `PodService.StartSession` no revisa que la guía esté publicada (ya existía antes de
  R3; anotado por el usuario al cerrar R3, `docs/decisions.md`).
- **Botón Atrás del navegador:** todavía puede reenviar un POST viejo (F5 sí está resuelto).
  Riesgo aceptado.
- **B-2 de Log NC:** solo se da con manipulación del cliente (`docs/specs/009-plan.md`, E4).
- **Cobertura de tests:** `ScoreCalculator` cubierto (Fase 5); `StepValidator` con cobertura
  parcial. `PodService` crea sus repositorios internamente, así que lo que hace alrededor
  del puntaje (registrar el intento, avance forzado, guardar `FinalScore`) no se puede probar
  sin BD; se probó en el navegador (spec 009, auditoría final). Cubrirlo sería otra fase
  (interfaces de repositorio o integración contra LocalDB). Tampoco tiene test el orden
  validar-antes-de-abandonar en `StartSession` (spec 021); la regla sí (`TrainingAccessRule`).
- **`CompleteControl`** sin UI (Complete se resuelve desde `BtnComplete`).
- **Detalle por paso en el resumen final** (intentos y puntaje por paso): diferido
  (spec 007) hasta que lo pida un instructor o la Fase 8.
- **Lo que quedó fuera de AUD-28/29** (☑ en la Fase 9, E2): la cabecera `Server` hasta que TI
  confirme IIS 10 o superior (D-144), `requireSSL` hasta que confirme HTTPS, CSP,
  SweetAlert2 11.26 y CodeDom 4.x (D-131). Bootstrap 5.3.8 entró en la Fase 11, E1 (D-155). El inventario de paquetes suma JsBarcode
  3.11.6 (spec 011), ClosedXML 0.105.1 y sus dependencias, y Chart.js 4.5.1 (spec 018).
- **Contraseña por defecto versionada (D-18):** quien tenga acceso al repo la conoce y, con un
  Code1, puede activar la cuenta de un compañero que aún no entró ("reclamo de cuenta
  ajena"); un reset por admin vuelve a abrirlo (spec 013). Riesgo aceptado también en
  producción (D-20): solo detección, con trainer presente y último acceso visible. Se revisa
  si Assessment pasa a usarse como evidencia de la CAPA.
- **AUD-40 y AUD-42** (`PodState` mutable fuera de `PodService`; orden no reproducible tras
  reconstruir en Serialized): diferidos (D-16). Con la spec 014, al reconstruir también
  cambian los hallazgos (se regeneran sobre las piezas nuevas).
- **Spec 016, genérico hasta tener capturas:** el marco de Edge (D-71; sin la URL de la
  pestaña de Recursos MES, solo el host, ni la del logout de MES). El aviso provisional de
  Hora a Hora (D-72) se quitó con R1 (D-151).
- **Spec 017, provisional:** el texto de Start sin Resource Setup (M-1, D-111), sin captura
  del MES y sin número, hasta capturarlo en testing.

## BD de desarrollo del usuario

Al día con los scripts `00`–`09`. Una instalación limpia se arma con ellos en orden
(verificado en LocalDB el 2026-09-25: idéntica a la BD de desarrollo). Últimos scripts que
corrió el usuario:

| Fecha | Qué corrió | Por qué |
|---|---|---|
| 2026-09-28 | ALTER de la spec 008 | Esquema Serialized (grupos DC, BOM, `RequiresStartChecklist`) |
| 2026-10-01 | ALTER + `02`, `05`, `06` | Columna `DataCollectionParameters.Description` (Fase 6.5, E8) |
| 2026-10-02 | `03`, después `03` y `04` | Auditoría E1 y E3: `spTrainingSessionAbandonActiveByOperator`; `spStepAttemptRecord` reemplaza a los SPs de intento, avance y cierre |
| 2026-10-05 | `03` | Spec 009 E6: `spTrainingSessionAbandonActiveByOperator` sin `@TrainingGuideId` |
| 2026-10-07 | `01`–`07` | Limpieza de scripts (2026-10-07): `CREATE OR ALTER`, catálogos como tablas de valores, transacciones, `spTrainingGuideSync` |
| 2026-10-07 | `08`, `09` | Fase 7: catálogo y guía de LoFlo |
| 2026-10-12 | ALTER + `01_spUser`, `03` | D-17: `Operators` → `Users`, `OperatorId` → `UserId`, SPs `spUser*` y `spTrainingSessionAbandonActiveByUser` |
| 2026-10-14 | ALTER de la spec 013 + `01_spUser`, `04`, `Ops/CreateAdmin` | Spec 013: `Users.Role`, `LastLoginAt`, `FailedLoginCount`, `LockedUntil`; `TrainingSessionStepAttempts.Reason` y `Action`; SPs de login, bloqueo y gestión de usuarios; primer admin |
| 2026-10-14 | `06`, `07` | T7: el producto `R&C` → `S&RC` y el título de su guía, renombrados en sitio |
| 2026-10-14 | `05`, `06`, `08` | Spec 015 E2: grupo `DCGTM_TRAINEE/A` (renombra en sitio el tercer grupo opcional de Redefinity y LoFlo; en Moldeo se agrega) |
| 2026-10-14 | `02`, `05`, `07`, `09` | Spec 015 E3: paso Logout (`StepType` 8) al final de las 3 guías, Complete 0.05 y Logout 0.05 |
| 2026-10-16 | ALTER de la spec 014 E1 + `02`, `05`, `06`, `08` | Spec 014 E1: `NCCodes.Method` y `RequiresComments`; códigos `SIM_FR`/`SIM_FX` en LoFlo y Redefinity |
| 2026-10-19 | `02`, `05`, `09` | Spec 014 E2: `StepType` 9 (reparación rápida) y D-56; guías de LoFlo y Redefinity en versión nueva con el paso y los pesos de D-57 |
| 2026-10-19 | ALTER de la spec 014 E3 + `02`, `05`, `08`, `09` | Spec 014 E3: `Operations.Sequence` y `Description` con 2 operaciones `SIM` por área, `spOperationGetByArea`, `StepType` 10 (versión nueva de las guías, DC 0.25); enmienda 5: `F_FR_GENERAL`/`F_FX_GENERAL` en LoFlo y Redefinity y se borran los `SIM_FR`/`SIM_FX` |
| 2026-10-19 | `00`–`09` (BD recreada) | Spec 016 E2: `StepType` 11 (Ingreso a MES) primero en las tres guías, DC −0.05 |
| 2026-10-22 | `00`–`09` (BD recreada con `RunAllScripts.ps1 -Recreate`) + `Ops/CreateAdmin` | Fila 13: sin la columna `Areas.RequiresNCComponent` (D-124) |

`05_SeedData.sql` es **solo dev**: Redefinity (Serialized con Assembly, 2 DC Groups
obligatorios + el grupo Trainer opcional, BOM `SIM-100001`/`SIM-100002`) y Subensamble 1.
Limpieza del 2026-09-25: las migraciones (`08`–`11` y los ALTER de `06`) se integraron al
esquema y se borraron. Cómo se entregan los cambios de esquema: `docs/process.md` → "Base de
datos".

## Auditorías

| Fecha | Archivo | Alcance | Estado |
|---|---|---|---|
| 2026-10-01 / 02 | `docs/audits/2026-10-01-auditoria.md` | Proyecto completo: AUD-01…32, D-1…D-12, entregas E1-E5 + spec 009 | ✅ (AUD-28/29 ☑ en la Fase 9, E2) |
| 2026-10-06 | `docs/audits/2026-10-06-auditoria-final.md` | Spec 009 + regresión completa en el navegador, antes de la 6.6: AUD-33…44, D-13…D-16 | ✅ (AUD-40/42 diferidos) |
| 2026-10-19 | `docs/audits/2026-10-19-fidelidad-ui.md` | Fidelidad de UI/UX contra el MES de testing: AUD-45…70 | Abierta: espera la revisión del usuario (AUD-63/64/69 ☑ vía la 017) |

