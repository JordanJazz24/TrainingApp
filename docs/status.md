# Estado — MESTrainingSimulator

> **Dónde quedamos.** Lo escribe Desarrollo (`docs/process.md` → "Tres sesiones de trabajo").
> Leer al iniciar cada sesión; actualizar al cerrar una entrega, una fase o una sesión de
> trabajo relevante. Corto: lo cerrado va a `docs/history.md`; las fases, su alcance ("No
> incluye") y los planes, a `docs/roadmap.md`. Separado del roadmap el 2026-10-21.

## 📍 Estado actual

**Última actualización:** 2026-10-22 · **Branch:** `TrainingApp` · **Último commit de
código:** Fase 9, E1: perfil de publicación del MVP (`git log --oneline -10` para ver el estado real)

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
- **Fase 9 · Deploy del MVP, en curso** (plan aprobado el 2026-10-22:
  `docs/specs/fase-9-plan.md`, D-129 a D-134). Fase 8.5 ✅.
  - **E1 ✅ de Desarrollo** (2026-10-22): perfil `TrainingApp/Properties/PublishProfiles/MVP.pubxml`
    (Release, a carpeta `publish\MVP`, precompilado y no actualizable, sin `.pdb`), carpeta
    publicada revisada (sin `connectionStrings.config` ni `.cs`; `web.config` sin `debug`, con
    `customErrors` y `configSource`), `docs/deploy.md` para TI y D-130 en `docs/process.md` §6.
    Regresión local en Assessment (LoFlo y Moldeo completas) confirmada por el usuario.
  - **Pendiente del usuario:** publicar con el perfil y pasarle a TI la carpeta, `Database/` y
    `docs/deploy.md`; la prueba de humo en el servidor cuando TI lo tenga listo (cierra D-19);
    el mensaje de lanzamiento (Redefinity de demostración, D-132; retroalimentación fuera de la
    app, D-134).
  - **Siguiente:** E2, después del MVP (AUD-28 lo barato y los parches de AUD-29, D-131).
- **Documentación ✅ (2026-10-21, sin spec):** optimización de los docs (ver
  `docs/history.md`).
- **Para mostrar a negocio:**
  [hoja de requerimientos](https://claude.ai/code/artifact/7e0ae556-9a55-4d78-8ff7-a20b2419e352),
  una foto del 2026-10-09 (la fuente de verdad sigue siendo el repo); se actualiza solo
  cuando el usuario lo pida.
- **Pendientes del usuario (diferidos):** revisar la auditoría de fidelidad AUD-45 a AUD-70,
  que sigue en la rama `docs/fidelidad-ui` hasta esa revisión (D-115; AUD-63/64/69 ya
  aprobadas por la 017); las capturas de los 4 modales del Start de Moldeo (inventario #8);
  rotar la contraseña de `sa` y actualizar `connectionStrings.config` (D-8), y probar T1 en
  el servidor IIS real (D-19; checklist en `docs/history.md` → "T1").
- **Descartado:** que la guía sugiera los códigos NC más comunes (spec 010, R6).

## Deuda y riesgos conocidos

Solo lo abierto; lo resuelto queda en la auditoría o la spec que lo cerró. La auditoría
final (D-16) repasó esta lista.

- **Botón Atrás del navegador:** todavía puede reenviar un POST viejo (F5 sí está resuelto).
  Riesgo aceptado.
- **B-2 de Log NC:** solo se da con manipulación del cliente (`docs/specs/009-plan.md`, E4).
- **Cobertura de tests:** `ScoreCalculator` cubierto (Fase 5); `StepValidator` con cobertura
  parcial. `PodService` crea sus repositorios internamente, así que lo que hace alrededor
  del puntaje (registrar el intento, avance forzado, guardar `FinalScore`) no se puede probar
  sin BD; se probó en el navegador (spec 009, auditoría final). Cubrirlo sería otra fase
  (interfaces de repositorio o integración contra LocalDB).
- **`CompleteControl`** sin UI (Complete se resuelve desde `BtnComplete`).
- **Detalle por paso en el resumen final** (intentos y puntaje por paso): diferido
  (spec 007) hasta que lo pida un instructor o la Fase 8.
- **AUD-28/29** (endurecimiento del deploy y paquetes): en la E2 de la Fase 9 (D-7, D-131). El inventario
  de AUD-29 suma JsBarcode 3.11.6 (`Scripts/`, spec 011).
- **Contraseña por defecto versionada (D-18):** quien tenga acceso al repo la conoce y, con un
  Code1, puede activar la cuenta de un compañero que aún no entró ("reclamo de cuenta
  ajena"); un reset por admin vuelve a abrirlo (spec 013). Riesgo aceptado también en
  producción (D-20): solo detección, con trainer presente y último acceso visible. Se revisa
  si Assessment pasa a usarse como evidencia de la CAPA.
- **AUD-40 y AUD-42** (`PodState` mutable fuera de `PodService`; orden no reproducible tras
  reconstruir en Serialized): diferidos (D-16). Con la spec 014, al reconstruir también
  cambian los hallazgos (se regeneran sobre las piezas nuevas).
- **Spec 016, provisional o genérico hasta tener capturas:** el aviso de Hora a Hora después
  de Iniciar (D-72) y el marco de Edge (D-71; sin la URL de las pestañas de Hora a Hora y
  Recursos MES, solo el host, ni la del logout de MES).
- **Spec 017, provisional:** el texto de Start sin Resource Setup (M-1, D-111), sin captura
  del MES y sin número, hasta capturarlo en testing. Al integrar la auditoría de fidelidad
  (`docs/fidelidad-ui`, D-115): marcar AUD-63, AUD-64 y AUD-69 ☑ vía la 017 y anotar la
  corrección de `SfcNotInOrder` (un SFC ajeno es `22507`, no `22501`; D-106).

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
| 2026-10-01 / 02 | `docs/audits/2026-10-01-auditoria.md` | Proyecto completo: AUD-01…32, D-1…D-12, entregas E1-E5 + spec 009 | ✅ (AUD-28/29 en la Fase 9) |
| 2026-10-06 | `docs/audits/2026-10-06-auditoria-final.md` | Spec 009 + regresión completa en el navegador, antes de la 6.6: AUD-33…44, D-13…D-16 | ✅ (AUD-40/42 diferidos) |

