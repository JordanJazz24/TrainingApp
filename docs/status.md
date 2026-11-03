# Estado — MESTrainingSimulator

> **Dónde quedamos.** Lo escribe Desarrollo (`docs/process.md` → "Tres sesiones de trabajo").
> Leer al iniciar cada sesión; actualizar al cerrar una entrega, una fase o una sesión de
> trabajo relevante. Corto: lo cerrado va a `docs/history.md`; las fases, su alcance ("No
> incluye") y los planes, a `docs/roadmap.md`. Separado del roadmap el 2026-10-21.

## 📍 Estado actual

**Última actualización:** 2026-11-03 · **Rama de desarrollo:** `TrainingApp`. Sin ramas ni
worktrees abiertos (el de `refactor-aud74` se quita al integrarla). Lo cerrado está en los planes
(`docs/specs/`), `docs/history.md` y `git log`.

**En curso**
- Nada abierto: la **Fase 13** (refactor de AUD-74 y D-15, `docs/specs/aud-74-plan.md`) está cerrada
  e integrada a `TrainingApp` con `--ff-only` (2026-11-03; `docs/history.md`). Tests: 558. El
  refactor no cambió comportamiento, markup ni firmas públicas.
- Las specs 022 y 025 están cerradas (2026-11-02; `docs/history.md`). Textos de ayuda validados por
  negocio (D-298, D-305); los de Usuarios, aceptados sin negocio (D-255).
- La cifra global «contados por guía» del Resumen se aceptó sin negocio (D-255). Lo siguiente es la
  E6 de `fase-9-plan.md` (2.ª publicación; la E3, la E4 y la E5 están cerradas; auditoría
  pre-deploy, AUD-71 a AUD-80; D-256): la hace el usuario y ya no espera a la Fase 13. `/rol
  desarrollo` al iniciar y `/cerrar-entrega` al cerrar.

**Plan de flujo eficiente (`docs/specs/proceso-eficiente-plan.md`)**
- Mías: E2 (este archivo en 40 líneas) y E3 (settings, hook de git, `.gitignore`).

**Pendiente del usuario**
- Fase 9, 2.ª publicación (E6 de `fase-9-plan.md`): HTTPS con redirección confirmado por TI
  (D-257), BD del servidor recreada limpia (D-130) y producción sin `sa`; ya no hay bloqueantes.
  Falta probar la guía de Subensamble 1 en el navegador y el mensaje de lanzamiento (D-132).
  Sin bloquear: rotar `sa` en la instancia local (AUD-71, D-8).
- Avisar a los trainers que cada operador elegirá su área en su próximo ingreso (D-170).
- BDs viejas: borrar `TrainingApp_Fase10` y `TrainingApp_Fase11` de LocalDB si ya no se usan.
- Elegir la guía piloto de Custom Resource Setup (D-194; hoy todas son Standard).
- Capturas de los 4 modales del Start de Moldeo.

## Deuda y riesgos conocidos

Solo lo abierto; lo resuelto queda en la auditoría o la spec que lo cerró. La auditoría
final (D-16) repasó esta lista.

- **`GenerateDashboardData.sql` no sirve solo para medir volumen:** genera 100 operadores con
  perfiles fijos. Para medir rendimiento, después correr `GenerateDashboardVolume.sql` (1 600
  operadores, ~247 000 intentos). `TrainingApp_DashboardTest` está hoy con ese volumen: borrarla
  si ya no se usa (`process.md` §6).
- **Entorno:** `aspnet_compiler` falla en este checkout porque `TrainingApp/obj/Release` y
  `TrainingApp/Properties/publish` (restos de la publicación del 22 de oct., ignorados por git) traen
  un `web.config` dentro de la app. Se compila una copia sin `obj`, `.vs` ni `publish` (`robocopy /XD`).
  No borrarlas: la publicación al servidor de la Fase 9 sigue pendiente del usuario.
- **Botón Atrás del navegador:** todavía puede reenviar un POST viejo (F5 sí está resuelto).
  Riesgo aceptado.
- **B-2 de Log NC:** solo se da con manipulación del cliente (`docs/specs/009-plan.md`, E4).
- **Cobertura de tests:** `ScoreCalculator` cubierto (Fase 5); `StepValidator` con cobertura
  parcial. `PodService` tiene tests del núcleo sin BD (Fase 13, E4, `PodServiceTests`): `LoadContext`,
  el registro del intento con el avance o el cierre, los tres modos, `Skip`, el avance forzado y
  `Complete`. Siguen sin test en `PodService` Split, Log NC, Assembly y Data Collection (D-259).
  Tampoco tiene test el orden validar-antes-de-abandonar en `StartSession` (spec 021); la regla sí
  (`TrainingAccessRule`).
- **`CompleteControl`** sin UI (Complete se resuelve desde `BtnComplete`).
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
| 2026-11-03 | `Ops/CreateAdmin` en `TrainingApp_DashboardTest` (LocalDB) | Fase 13, E1: cuenta de administrador para probar en el worktree con los datos del generador; esa BD sigue con su volumen (borrarla si ya no se usa) |

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

