# Roadmap — MESTrainingSimulator

> **Estado e historial.** Las reglas vigentes están en `CLAUDE.md`, el proceso de trabajo en
> `docs/process.md` y los mecanismos en `docs/architecture.md`. Specs y auditorías tienen sus
> propios archivos (`docs/specs/`, `docs/audits/`); aquí solo se referencian. Las secciones
> de cada fase describen lo que se hizo **en su momento**: si contradicen a `CLAUDE.md`,
> manda `CLAUDE.md`.

---

## 📍 Estado actual — dónde quedamos

> Leer al iniciar cada sesión. Actualizar al cerrar una entrega, una fase o una sesión de
> trabajo relevante. Mantenerla corta: el detalle va en la sección de la fase, la spec o la
> auditoría.

**Última actualización:** 2026-10-19 · **Branch:** `TrainingApp` · **Último commit de
código:** spec 016, entrega 4 (`git log --oneline -10` para ver el estado real)

- **Ahora — Fase 8.5 · Entrega de noviembre** (plan aprobado el 2026-10-12 y reordenado el
  2026-10-13 tras la trainer: 013 → T7 → 015 → 014 → 016 → 012; ver su sección). La Fase 8 cerró el
  2026-10-12 con la validación con técnicos. **T1 preparado** (ver "Historial"); la prueba en
  el servidor real queda diferida (D-19). **Spec 011 (T2) ✅** (2026-10-13, ver
  "Historial"): escanear la Shop Order, etiqueta simulada y guía de Guided, probada por el
  usuario. **Spec 013 ✅** (2026-10-14, ver "Historial"): roles, login de admin, bloqueo,
  último acceso, gestión de usuarios con borrado y registro de actividad. El usuario decidió
  terminarla antes que T7. **T7 ✅** (2026-10-14, ver "Historial"): notas y alertas de la
  trainer en la guía de Guided y el producto R&C → **S&RC**. **Spec 015 ✅** (2026-10-14, ver
  "Historial"): equipos precargados en Resource Setup, grupo de DC Trainer y Logout como
  último paso, probada por el usuario. **Spec 014 ✅** (2026-10-19, tres métodos de Log NC,
  ver "Historial"): E1-E3 probadas por el usuario; la E4 (Assembly Data por tipo de
  material, W13) se recortó (enmienda 6); el usuario la subió a `main`. **T6 ✅**
  (2026-10-19): llegó el documento de la evaluación práctica QD001043644; contraste en
  `docs/ui-reference/evaluacion-practica-mes.md` y decisiones en las filas 9-11 de la
  Fase 8.5. No cambia pesos. **Spec 016 ✅** (2026-10-19, T10, ver
  "Historial"): login único y neutro, paso Ingreso a MES (script, Repositorio, Hora a Hora,
  Recursos MES y login de MES) y Logout con la limpieza, con su guía de Guided; además, sin
  spec, la fijación de sesión. Ya se puede subir a `main` (D-81); lo sube el usuario.
  **Siguiente:** las **notas EP-7** (plan directo), después la **012** y la **017**. Orden
  vigente: **notas EP-7 (plan directo) → 012 → 017**.
  Para mostrar a negocio:
  [hoja de requerimientos](https://claude.ai/code/artifact/7e0ae556-9a55-4d78-8ff7-a20b2419e352),
  una foto del 2026-10-09 (la fuente de verdad sigue siendo el repo); se actualiza solo
  cuando el usuario lo pida.
- **Decidido por negocio (2026-10-19):** **T5**: guías por área, como están. **T9**: el
  usuario ya tiene acceso al ambiente de testing de SAP MES.
- **Documentación (2026-10-12 a 2026-10-15):** contraste con la WI oficial de MES
  QD001001524, integrado en `TrainingApp` (`docs/ui-reference/wi-produccion-disposables.md`,
  W1-W22). Insumo de la spec 014: pasos de los tres scraps (W20, W21) y Assembly Data por
  tipo de material (W13). Preguntas abiertas en "Fase 8" → "Contraste con la WI".
- **Lo último cerrado (2026-10-19):** la **spec 016** (preparación antes de SAP) y, antes, la
  **spec 014** (tres métodos de Log NC). Antes
  (2026-10-14), la **spec 015**, **T7** (mensajes y alertas de la guía) y la
  **spec 013** (cuentas, roles y registro de actividad). Antes (2026-10-12), **T1 · preparación de la versión de prueba**
  (`Web.Release.config` con `customErrors`, `Database/Ops/ResetUserPassword.sql`). Antes, el
  refactor **`Operator` → `User`** en todas las capas (D-17, ver "Historial"): paso previo
  al módulo administrativo.
- **Diferido (D-19):** probar T1 en el servidor IIS real (checklist en "Historial" → T1).
- **Antes (2026-10-08):** Login y Cambio de contraseña con el diseño del login SAP
  NetWeaver real (ver "Historial"). Las capturas del MES real pasaron al repo:
  `docs/ui-reference/Screens SAP MES REAL/`. Y el **ultrareview** de lo no auditado (ver
  "Historial"): `main` = último estado revisado.
- **Antes (2026-10-07):** la **Fase 7 · catálogo de LoFlo** (`08`/`09`; NC, DC y
  BOM simulados con marca `SIM`), probada por el usuario en el navegador. Antes, la
  **Fase 5 · tests de `ScoreCalculator`**; después,
  sin fase, la **reorganización de la documentación** (cada regla en un solo lugar: mapa en
  `CLAUDE.md`, nuevos `docs/process.md` y `docs/architecture.md`; la memoria solo guarda lo
  que no va al repo) y la **limpieza de los scripts SQL** (corridos y probados por el
  usuario). Antes, la 6.6 (overlays guiados) y un arreglo de rendimiento (ver "Historial").
- **Pendientes del usuario (diferidos):** las capturas de los 4 modales del Start de Moldeo
  (inventario #8), rotar la contraseña de `sa` y actualizar `connectionStrings.config`
  (D-8) y probar T1 en el servidor IIS real (D-19). Se descartó que la guía sugiera los
  códigos NC más comunes (spec 010). El detalle por paso en el resumen final se verá en su
  momento.

### Deuda y riesgos conocidos

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
- **AUD-28/29** (endurecimiento del deploy y paquetes): en la Fase 9 (D-7). El inventario
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
- **`RequiresNCComponent` sin uso** desde la enmienda 4 de la spec 014 (Component lo decide
  el método del código): columna `Areas.RequiresNCComponent` en `00`, `05`, `06` y `08`,
  `spAreaGet*`, `AreaRepository` y `Area.RequiresNCComponent`. Quitarla no entró en la
  spec 014; va con su propio plan.

### BD de desarrollo del usuario

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

`05_SeedData.sql` es **solo dev**: Redefinity (Serialized con Assembly, 2 DC Groups
obligatorios + el grupo Trainer opcional, BOM `SIM-100001`/`SIM-100002`) y Subensamble 1.
Limpieza del 2026-09-25: las migraciones (`08`–`11` y los ALTER de `06`) se integraron al
esquema y se borraron. Cómo se entregan los cambios de esquema: `docs/process.md` → "Base de
datos".

### Auditorías

| Fecha | Archivo | Alcance | Estado |
|---|---|---|---|
| 2026-10-01 / 02 | `docs/audits/2026-10-01-auditoria.md` | Proyecto completo: AUD-01…32, D-1…D-12, entregas E1-E5 + spec 009 | ✅ (AUD-28/29 en la Fase 9) |
| 2026-10-06 | `docs/audits/2026-10-06-auditoria-final.md` | Spec 009 + regresión completa en el navegador, antes de la 6.6: AUD-33…44, D-13…D-16 | ✅ (AUD-40/42 diferidos) |

---

## Fases

| Fase | Estado | Cierre | Spec / plan |
|---|---|---|---|
| 0 · Setup | ✅ | — | — |
| 1 · Auth | ✅ | — | — |
| 2 · Dominio y catálogo de R&C | ✅ | — | `002-rc-moldeo-catalog.md` |
| 3 · Motor de TrainingGuide | ✅ | — | — |
| 4 · TrainingSession y modos | ✅ | — | `003-training-session-y-modos.md` |
| 5 · ScoreCalculator: cobertura de tests | ✅ | 2026-10-07 | `fase-5-plan.md` |
| 6 · UserControls por tipo de paso | ✅ | 2026-09-24 | `004`, `005`, `006`, `007` |
| — · Ajustes tras la 6 (S1/S2) y spec 008 (Serialized) | ✅ | 2026-09-28 | `008-*.md` |
| 6.5 · Fidelidad de UI/UX con el SAP MES real | ✅ | 2026-10-02 | `fase-6.5-plan.md` |
| — · Auditoría + spec 009 (motor del POD) + auditoría final | ✅ | 2026-10-06 | `009-*.md`, `docs/audits/` |
| 6.6 · Overlays guiados | ✅ | 2026-10-07 | `010-*.md` |
| 7 · Catálogo de LoFlo | ✅ | 2026-10-07 | plan directo |
| 8 · Validación interna | ✅ | 2026-10-12 | revisiones y validación con técnicos |
| **8.5 · Entrega de noviembre** | ▶️ en curso | 2.ª semana de nov. | specs 011-014 |
| 9 · Deploy del MVP | pendiente (T1 adelanta una parte) | | |
| 10 · Dashboard e historial por operador (T8) | pendiente | | |

### Fase 7 — Catálogo de LoFlo (segunda área) ✅
**Objetivo:** script SQL con el catálogo de LoFlo (Sensores). Solo carga de datos, cero
código nuevo: valida que el motor es genérico y que `RequiresAssembly` y
`RequiresDataCollection` funcionan con otra configuración.
**El script tiene que cargar** (spec 008): los flags del área (`CLAUDE.md` → "Jerarquía",
incluido `RequiresStartChecklist = 0`), NC Codes, `DataCollectionGroups` con sus obligatorios,
sus `DataCollectionParameters`, el BOM en `AssemblyComponents` (≥1 componente) y la guía con
sus pasos y pesos.
**No incluye:** cambios al motor, nuevas pantallas.
**Desbloqueo:** los bloqueantes #1 (Start de Serialized), #2 (Assembly sin completar) y #4
(huecos de UI de Assembly) los resolvió la spec 008; el #3 (flags reales) lo resolvió negocio
el 2026-10-07: todas las áreas de Sensores son Serialized con Assembly, DC y Component en NC,
sin checklist de Start; DC Groups, parámetros, códigos NC y BOM se simulan por ahora.
**Resultado (2026-10-07, plan directo):** `08_SensoresLoFloCatalog.sql` (flags reales; 6
códigos NC, 3 DC Groups —2 obligatorios— con 6 parámetros y un BOM de 3 componentes, todo
simulado y marcado `SIM` para retirarlo a mano cuando llegue el catálogo real) y
`09_SensoresLoFloGuide.sql` (pesos de Redefinity). `05` dejó de tocar LoFlo. El motor resultó
genérico: los únicos cambios de código fueron dos bugs de `DataCollectionControl`, ambos
anteriores a LoFlo: los campos numéricos rechazaban decimales (`step="any"`) y, tras cerrar un
grupo con la ✕ y abrir otro, el Save podía caer con una excepción de validación de eventos
(los campos dinámicos usaban IDs automáticos que se corrían; ahora tienen un ID fijo por
parámetro). Probado en LocalDB y por el usuario en el navegador.

### Fase 8 — Validación interna ✅ (2026-10-12)
**Objetivo:** sesión con trainer/operador real sobre R&C y LoFlo. Correcciones de UX, bugs de
flujo, ajustes de catálogo.
**No incluye:** nuevas áreas, dashboard admin, reportes.
**Resultado:** revisiones con el ingeniero de SAP MES y con Ernesto (2026-10-09) y
validación con técnicos de MES (2026-10-12), que aprobaron el flujo general y la fidelidad de
la interfaz. Lo que pide funcionalidad nueva pasó a la Fase 8.5.

#### Revisión con SAP MES (2026-10-09)
30 minutos con el ingeniero de SAP MES (Diego) sobre Redefinity en Guided, desde el paso 3.
Lo que se aprendió de cada pantalla está en `docs/ui-reference/general-flow.md` (pasos 3, 5
y 8). Clasificación: **S** = funcionalidad nueva, necesita spec y sale de esta fase
(`CLAUDE.md` → "Reglas de trabajo"); **B** = backlog.

| Hallazgo | Clase | Siguiente paso |
|---|---|---|
| Start manual después de seleccionar; solo carga solo con escáner configurado | Confirmado | — |
| Line Clearance de cierre igual al de apertura | Confirmado | — |
| Flujo general (Start, Assembly, DC, LC, Complete) correcto; en piso hay muchas variantes | Confirmado | Decidir con negocio qué variantes entran |
| La cola trae piezas de otras órdenes y el procedimiento es iniciar la orden. El simulador entrena "seleccionar todo" (Start exige todas, spec 008) | S | Piezas de otra orden en Browse Work List y elegir bien se califica (hoy seleccionar filas es navegación y nunca cuenta) |
| Lo habitual es escanear la orden y dar Start | S | Escanear la Shop Order en `TxtSFC` (hoy readonly, spec 005). Misma spec que la anterior |
| Log NC tiene tres métodos en uso (fast scrap, scrap/return component, extensión de la operación), según la operación | S | Decidir con negocio cuáles se entrenan |
| DC por pieza o para todas, según la operación | B | Mantener por pieza; "aplicar a todas" como mejora |
| Mensajes sin captura (pieza de otra orden, material equivocado, avance de DC) | Por verificar | Coordinar el ambiente de calidad que ofreció el ingeniero |

Sin respuesta: Operations por área (solo un indicio: varía la configuración, no se habló de
la secuencia), validación del material contra el BOM, catálogos reales de Sensores, Status
`In Queue` en NonSerialized, uso de Disassemble/Hold y los dos pendientes de captura. El
ingeniero recomendó definir alcance y requisitos con Ernesto antes de agregar variantes.

#### Revisión con Ernesto, MES (2026-10-09)
68 minutos: contexto de negocio y demo de Guided y Assessment.
- **Contexto:** hoy no hay entrenamiento formal de MES (enseña el compañero de al lado); unas
  1600 personas usan el sistema. Ernesto libera en 1-2 semanas una **prueba práctica en
  papel** (parte de una CAPA): unos 10 puntos que suman 100, evaluada en producción. Pidió
  que sea el **mínimo** que evalúe el simulador; los pesos y penalizaciones los define el
  simulador. A futuro, la prueba en papel pasaría al simulador: eso sube la importancia de la
  integridad de Assessment (identidad del operador, historial auditable).
- **Entrega planeada:** segunda semana de noviembre de 2026 (Fase 8.5).
- **Tareas que salieron** (el plan y su orden, en la Fase 8.5):

| # | Tarea | Estado |
|---|---|---|
| T1 | Versión de prueba publicada para Ernesto y técnicos de MES | Fase 8.5 |
| T2 | Escanear la Shop Order como camino principal de Search/Start (RF-17). Decidido en la reunión: escaneo principal + aviso de que Browse Work List también vale; sin agregar variabilidad | Fase 8.5, spec 011 |
| T3 | **Modo práctica**, tercer modo: Assessment sin evaluación, sin la guía visual y con pistas ante la acción incorrecta | Fase 8.5, spec 012 |
| T4 | Log NC: practicar los tres tipos de scrap (fast scrap, scrap/return component, extensión de la operación) | Fase 8.5, spec 014 (UI espera pantallas) |
| T5 | Guías por **área** (lo que pidió el equipo de Paola) o **dos escenarios** generales, serializado y por lote (recomendación de Ernesto), con los pasos opcionales siempre incluidos | ✅ **Decidido (usuario, 2026-10-19): guías por área**, como están hoy. Sin escenarios generales |
| T6 | Mapear los criterios de la prueba en papel a pasos del simulador (puede mover pesos; deben sumar 1.00) | Documento recibido el 2026-10-19 (QD001043644); contraste en `docs/ui-reference/evaluacion-practica-mes.md` (EP-1…EP-9) |
| T7 | Revisar los mensajes de la guía de Guided con los trainers (menos texto; burbuja de "lo que hay que recordar" o botón "?") | Contenido de `GuideScriptBuilder`, en paralelo |
| T8 | Dashboard: historial por operador (intentos, fechas, duración por paso, registro de actividad) para ver dónde fallan antes de producción. Sin tiempo mínimo de práctica | Fase 10 |
| T9 | Acceso al ambiente de **testing** de SAP MES (no el sandbox): mensajes reales, pantallas de los tres scraps y Start de Moldeo | ✅ El usuario ya tiene acceso (2026-10-19). Los banners de la reparación rápida y de Local Rework se confirmaron correctos (V-3, V-5) |

Backlog: RF-16 (piezas de otras órdenes en Browse Work List; Diego a favor, Ernesto en contra
de agregar variabilidad). Data Collection "aplicar a todas las piezas" se descartó el
2026-10-12 (ver abajo).

#### Validación con técnicos de MES (2026-10-12) y contraste con la WI
Validación sin transcripción; contraste con la WI de producción QD001001524 rev. 27
(sesión "contraste-manual-sap-mes", rama `docs/wi-produccion-contraste`, archivo
`docs/ui-reference/wi-produccion-disposables.md`). Aclarado por el usuario.
- **Aprobado:** flujo general, fidelidad de la interfaz y orden actual de los pasos. La orden
  **no** se escanea antes de Resource Setup, aunque la WI 6.4 lo sugiera (descartado por el
  usuario; coincide con la spec 011).
- **Data Collection pieza por pieza:** se mantiene; "aplicar a todas las piezas" queda
  descartado.
- **Complete:** solo el flujo directo. Las variantes de la WI (Log Buyoff, firma electrónica,
  supervisor que completa por otro) quedan como **simplificación aceptada**
  (`docs/ui-reference/general-flow.md` → Paso 9).
- **Tool List:** descartado por ahora. Lo pidieron antes del LC de apertura, pero la WI solo
  lo usa para cambiar una herramienta de un SFC ya iniciado, y lo hace un técnico, ingeniero
  o supervisor, no el operador; las herramientas iniciales ya se asocian en Resource Setup.
- **Tres tipos de scrap (T4):** los piden otra vez; ya son tres fuentes (Diego, Ernesto y los
  técnicos), así que sube de prioridad. La WI documenta el flujo de cada uno: desecho rápido
  (`F_FS…`, lo que el simulador ya hace), reparación rápida (`F_FR…`, quitar y devolver o
  desechar un componente y agregar el nuevo) y reparación rápida extendida (`F_FX…`, volver a
  otra operación; con `F_FX_GENERAL`, el MPI y su revisión o el NCR en Comments). Faltan las
  pantallas: imágenes del .docx de la WI o T9.
- **T10 · Preparación antes de SAP (nueva, WI 6.4, pasos 1 a 3):** limpiar historial y caché
  con el Code1, abrir "Recursos MES" con el Code1 y el código del recurso, y login de MES
  (Code1 + contraseña, o PIN + tarjeta), verificando que el Code1 sea el correcto. Se repite
  al inicio de cada turno y al cambiar de estación. Choca con `CLAUDE.md` ("el login a SAP
  NetWeaver no se evalúa"): necesita spec. Abierto: si se evalúa en Assessment (con peso, y
  los pesos siguen sumando 1.00) o solo se enseña en Guided; si es común a todas las áreas;
  si incluye el logout obligatorio de la WI 6.5 (descansos, fin de turno, cambio de estación
  u operador). Sin capturas: no inventar UI (`docs/process.md` §8). Spec 016 (ver la
  retroalimentación de la trainer).

#### Retroalimentación de la trainer (Carla, Wires línea final, 2026-10-13)
Fuente: sesión "Reuniones y dudas". Validación final con ella en 1-2 semanas.
- **Confirmado:** flujo e interfaz ("un espejo de producción"); escanear la Shop Order como
  la spec 011 (en Wires también escanean pieza por pieza o eligen de la lista);
  `[N SELECTED]` igual al real; las dos formas de salir de Assembly existen (se deja igual);
  DC pieza por pieza y LC de cierre (Wires no hace LC de cierre: nota en la guía).
- **Los 3 errores más graves:** (1) scrapear las unidades equivocadas: el más grave, el MES no
  lo detecta y les pasa a veteranos → spec 014 (la guía indica la pieza defectuosa y, en
  Assessment, scrapear otra es intento fallido; usuario, 2026-10-13); (2) iniciar piezas que
  no son de la orden o la estación → RF-16 sigue diferido (usuario); lo cubren en parte la
  alerta de Search/Start (T7) y la spec 011; (3) no hacer Remove All de los equipos en
  Resource Setup → spec 015. Pasos más críticos para ella: Resource Setup y scrap (el peso de
  Log NC, hoy 0.10, se revisa con T6).
- **Preparación antes de SAP (T10):** viene de una CAPA y en planta se audita que entren por
  el Repositorio: script de limpieza de caché → Repositorio con dos registros obligatorios en
  cualquier orden, "Recursos" (escanear el recurso) y "Ahora Ahora" (Code1, registro de
  unidades por hora) → login de MES. El usuario: se agrega sí o sí → spec 016 (espera
  capturas).
- **Logout** como último paso después de Complete (un clic) + nota "cierre sesión siempre que
  deje la estación": hubo CAPAs por Code1 compartidos y sesiones abiertas. Según ella el MES
  se cierra solo a los 15 min (sin verificar: T9) → spec 015.
- **Decisiones del usuario (2026-10-13):** Resource Setup arranca con equipos precargados (del
  turno o lote anterior) que hay que quitar con Remove All → spec 015. **Grupo de DC
  "Trainer"** (existe en toda la planta, opcional en el MES real pero siempre usado en
  entrenamiento: el nombre de la persona en entrenamiento, solo letras, porque trabaja con el
  Code1 del trainer hasta certificarse) reemplaza el tercer grupo opcional de Redefinity y
  LoFlo → spec 015. Piezas por orden Serialized: 4-5 (hoy 3-5) para que alcancen los tres
  scraps → spec 014. **Mensajes de la guía** aprobados, con notas de **alerta** en rojo en
  Resource Setup (Retrieve y Set Up; Remove All y escanear lo que está en la estación; al
  inicio del turno y en cada cambio de lote), LC de cierre (no aplica en algunas
  operaciones), Search/Start (solo piezas de su orden y estación) y DC (grupo Trainer); y
  mensajes normales en LC de apertura, DC, Log NC y Logout → T7.
- **Menores:** texto mal escrito ("RCI" o similar) en la selección de producto o área (bug);
  lentitud por enlace remoto (Fase 9); los errores del MES son un recuadro rojo arriba y las
  confirmaciones duran 5 s (`docs/ui-reference/general-flow.md`).

#### Contraste con la WI QD001001524 (2026-10-12)
El manual oficial de ejecución de órdenes en MES (Disposables, rev. 27) confirma la secuencia
y la operación de todos los pasos del simulador. Detalle y hallazgos W1-W22:
`docs/ui-reference/wi-produccion-disposables.md`. Lo ya decidido está en "Validación con
técnicos" (arriba): W17 (Complete directo, simplificación aceptada), Tool List descartado,
T10 desde la WI 6.4 y W9 cubierto por la spec 011. Quedan para Ernesto (T6) y el ambiente de
testing (T9):
- **W3:** ¿hay operaciones sin herramientas en Resource Setup? El simulador exige al menos una.
- **W7:** ¿de dónde sale el Line Clearance de cierre? La WI solo lo repite por condiciones
  (cambio de usuario, tiempo, SFC activo, cambio de orden). La evaluación práctica (EP-8) repite las mismas
  condiciones: no la resuelve.
- **W13 (resuelto 2026-10-15):** Assembly Data depende del control del material (Batch,
  Inventory SFC + Batch o Material Document); el simulador siempre usa el Process Lot. Entró
  en la spec 014 (D-46, 2026-10-15) y se recortó el 2026-10-19 (enmienda 6): diferida hasta
  que T6, la trainer o T9 muestren errores con esos datos.
- **W15:** ícono ✔ del grupo de DC guardado (captura pendiente en `inventario-pantallas.md`).
- **W20:** `F_FR_GENERAL` está en el catálogo de scrap de Moldeo, pero en el MES es un código
  de reparación. Lo resuelve la spec 014 (método de cada código en el catálogo).
- Para T4, la WI ya describe los pasos de la reparación rápida (`F_FR`) y la extendida
  (`F_FX`) (W21); faltan las pantallas. La reparación rápida reutiliza el ensamble de
  Assembly para el componente nuevo (Colorado: "follow Scanning to Assemble").
- **Otro site (2026-10-15):** la WI de Colorado Springs D001372587 rev. E es casi igual.
  Separa **Normal Flow** (por SFC; el split es solo de este flujo, con cantidad > 1) y
  **Relaxed Flow** (por cantidad: ventana "Relax Flow NC Logging" con Defect Count, sin
  split); los tres métodos aplican a ambos. Prefijo de la extendida: `F_EX` allá, `F_FX` en
  Costa Rica. Moldeo en el simulador corresponde a Normal Flow con cantidad > 1. No
  versionada en el repo.

### Fase 8.5 — Entrega de noviembre
**Objetivo:** versión de entrenamiento para la segunda semana de noviembre de 2026, con la
identidad del operador y el historial de Assessment confiables (ver "Revisión con Ernesto").
Plan aprobado por el usuario el 2026-10-12 y **reordenado el 2026-10-13** dos veces: tras
la validación con técnicos y tras la trainer; el usuario terminó la 013 antes que T7
(2026-10-13). Orden vigente: **013 ✅ → T7 → 015 → 014 ✅ → 016 ✅ → notas EP-7 → 012 → 017** (T6, 2026-10-19; si las capturas de la 016 se atrasan, las notas EP-7 van primero). Cada tarea con funcionalidad nueva lleva su `/spec` (`docs/process.md`).

| # | Tarea | Tipo | Depende de |
|---|---|---|---|
| 1 | ✅ **T1 · Versión de prueba** en IIS interno (`d60bdfb`): `DEFAULT_PASSWORD` con su valor de siempre (D-18); BD con `00`–`09`, **incluido `05`** (Redefinity y Subensamble 1 de ejemplo: avisar a los testers que son demostración); `customErrors` en Release (parte mínima de AUD-28); reset de testers con `Database/Ops/ResetUserPassword.sql`. Prueba en el servidor real diferida (D-19). Los datos de la prueba se descartan | plan directo | — |
| 2 | ✅ **T2 · Escanear la Shop Order** (2026-10-13, `bf19e54`; plan `011-plan.md`) | spec 011 | — |
| 3 | ✅ **T7 · Mensajes y alertas de la guía de Guided** (2026-10-14) (retroalimentación de la trainer): notas de alerta en rojo en Resource Setup, LC de cierre y Search/Start, y los mensajes aprobados de LC de apertura, DC y Log NC. Las notas de Logout y del grupo Trainer entran con la spec 015. Junto con el bug del texto mal escrito en la selección de área | plan directo | — |
| 4 | ✅ **Cuentas, roles y registro de actividad** (2026-10-14, `e4ef6ae`…`5b9091a`; plan `013-plan.md`): roles excluyentes Operator / Administrator (los admins no entrenan), login de admin aparte con diseño propio, gestión de usuarios (crear, resetear a la contraseña por defecto, desbloquear, borrar con su historial; el rol no se cambia desde la UI, enmienda del 2026-10-13), bloqueo de 15 min tras 5 fallos, último acceso visible y motivo + acción de cada intento. El reclamo de cuenta ajena no se cierra (D-20) | spec 013 | — |
| 5 | ✅ **Ajustes de la trainer** (2026-10-14) (spec aprobada el 2026-10-14): Resource Setup con equipos precargados (generados al abrir el control; guardar con alguno es intento fallido); **Logout** como último paso después de Complete (0.05, sale de Complete; clic simulado → resumen; antes del último paso, intento fallido en Assessment e ignorado en Guided); grupo de DC **`DCGTM_TRAINEE/A`** opcional en todas las áreas con DC (nombre: letras y espacios, formato) | spec 015 | — |
| 6 | ✅ **T4 · Tres métodos de Log NC** (2026-10-19, `b685f1d`…`0654e9d`; plan `014-plan.md`): desecho rápido, reparación rápida y extendida, obligatorios en las áreas con Assembly (LoFlo, Redefinity); Moldeo solo desecho rápido (D-53). Nota de hallazgo con la pieza defectuosa (en Assessment, otra pieza, otro código u otra decisión es intento fallido: error 1 de la trainer), 4-5 piezas por orden Serialized, cantidad del split calificada en Moldeo, reparaciones con los códigos reales `F_FR_GENERAL` y `F_FX_GENERAL` (enmienda 5) y pesos nuevos (Log NC 0.05 × 3, DC 0.25). La E4 (Assembly Data por tipo de material, W13) se recortó y quedó diferida (enmienda 6) | spec 014 | banners provisionales hasta conocer el de la reparación rápida y el de Local Rework (V-3, V-5) |
| 7 | ✅ **T10 · Preparación antes de SAP** (2026-10-19, `50aca9d`…`e1227dd`; spec aprobada el 2026-10-19, `docs/specs/016-preparacion-antes-de-sap.md`; plan `016-plan.md` aprobado el 2026-10-19): paso nuevo "Ingreso a MES" al principio de las guías (script de limpieza → Repositorio → Hora a Hora 3.0 → Recursos MES → login de MES con el diseño SAP NetWeaver), Logout con la limpieza final (EP-3), login de la app único y neutro (enmienda 2 de la spec 013); Ingreso 0.05 y DC −0.05. Flujo en `docs/ui-reference/pre-sap-flow.md` | spec 016 | — |
| 8 | **T3 · Modo práctica.** La Entrega 1 de su plan es el refactor **`ModePolicy`** (concentrar en un objeto lo que hoy decide cada modo: penaliza, fuerza avance, guía visual, pistas; sin cambio de comportamiento, con tests), que se implementa después de aprobar la spec porque ella define qué capacidades expresa. Propuesta para la spec: registra intentos marcados con el modo (se miden sin mezclarse con Assessment), pista por paso con texto de catálogo, sin avance forzado | spec 012 | — |
| 9 | ✅ **T6 · Evaluación práctica → Assessment** (QD001043644, 2026-10-19): contraste y decisiones en `docs/ui-reference/evaluacion-practica-mes.md`. La evaluación es insumo, no se replica: el simulador cubre como mínimo lo que pide. Sin cambios de pesos ni del criterio de aprobación (EP-1, EP-2); W13 sigue diferida (EP-6). Salieron: EP-3 → 016, EP-7 → fila 11, EP-4/EP-5 → fila 10 | análisis | — |
| 10 | **Spec 017 · Fidelidad con la evaluación práctica** (EP-4, EP-5): Resource Setup con REMOVE ALL + Save → Insert New y el Setup Type calificado (con `Material`); errores del MES que el operador debe reconocer: SFC iniciado en otro recurso, componente no requerido, material sensible al tiempo expirado, batch inválido, cantidad insuficiente y DC fuera de límites | spec 017 | capturas de los mensajes reales en el ambiente de testing (T9) |
| 11 | **Notas de comprensión en la guía de Guided** (EP-7): cuándo iniciar y cerrar sesión, riesgo de una sesión abierta, cuándo repetir Resource Setup y Line Clearance, consumir antes del MPI; cada una en su paso, como las de T7. Assessment no las evalúa. Las de login y limpieza de caché, con los pasos de la 016. Plan y textos aprobados el 2026-10-20: ver "Plan de la fila 11" | plan directo | — |

**Plan de la fila 11 · notas EP-7** (aprobado por el usuario el 2026-10-20, con la
verificación de que no repiten lo de otros pasos). La spec 016 ya cubrió el inicio de
sesión (subpaso `info` del Ingreso a MES), cuándo cerrarla y el riesgo de dejarla abierta
(nota del subpaso `logout`). Quedan tres notas normales (no alertas), solo texto en
`GuideScriptBuilder`, en una entrega. No cambian BD, puntaje ni Assessment (no recibe la
guía), y ninguna prueba compara estos textos.

| Paso · subpaso | Texto aprobado | Cambio |
|---|---|---|
| Resource Setup · `open` | "Resource Setup **se repite cada vez que se sustituye un equipo**: mantenimiento preventivo, correctivo o calibración." | Nota nueva; la alerta de `removeAll` no cambia |
| Line Clearance de apertura · `activities` | "Siempre después de **Resource Setup**: la limpieza física y la del sistema.<br>Se registra de nuevo cuando:<br>• cambia el usuario o la Shop Order,<br>• queda un SFC activo,<br>• vence el tiempo configurado." | Reemplaza la nota de T7 (el cambio de orden queda dentro). El LC de cierre conserva su alerta y no lleva estas condiciones: W7 sigue abierta |
| Assembly · `open` | "**Consuma los componentes antes** de empezar la instrucción del MPI." | Nota nueva; solo en las áreas con Assembly |

Si el tiempo no alcanza, se recorta primero T3 (modo práctica), después T10 y T4 si las
capturas o las pantallas no llegan; nunca la spec 013: sin ella solo el desarrollador administra las cuentas y no se
guarda el registro de actividad que necesita el dashboard.

**Decisiones del usuario (2026-10-12 y 2026-10-13) para la spec 013 y la Fase 10:**
- **D-18:** `DEFAULT_PASSWORD` sigue en `Constants` (versionado) y, en T1, con su valor de
  siempre (corrección del usuario, 2026-10-12: no se cambia).
- **D-20 (2026-10-13, spec 013):** el reclamo de cuenta ajena **no se cierra**: solo
  detección (trainer presente en las sesiones + último acceso visible para el usuario), con
  el riesgo aceptado también en producción. Reemplaza el "obligatorio" que tenía la 013. Se
  revisa si Assessment pasa a usarse como evidencia de la CAPA o si se entrena sin trainer.
- Solo los **administradores** ven resultados y dashboards: un único rol además del de
  operador, sin alcance por área.
- La API de empleados da solo Code1, nombre completo y correo: el área de un operador no sale
  de la API. Si T5 decide escenarios, las métricas por área necesitan un área del usuario
  asignada en la app (a decidir en la spec del dashboard).
- Todos tienen cuenta corporativa (`nombre.apellido@philips.com`). Si se revisa D-20, las
  opciones son un código de activación del trainer, el correo (requiere SMTP y que el
  operador lea el correo) o Windows Authentication (solo si cada operador entrena con su
  propia sesión de Windows).

**No incluye:** dashboard y reportes (Fase 10), nuevas áreas (espera T5), RF-16 (diferido
otra vez el 2026-10-13, aunque lo pide la trainer) y lo descartado el 2026-10-12: Tool List, variantes de Complete y
Data Collection para todas las piezas.

### Fase 9 — Deploy del MVP
**Objetivo:** deploy en servidor interno con IIS: connection string de producción, App Pool,
permisos, prueba de humo. Incluye AUD-28/29.
- **Arranque en frío en IIS** (medido 2026-10-07 en desarrollo): tras reciclar el App Pool,
  el primer request compila el markup (3-10 s por carpeta; ~18 s todo el sitio). Resolver al
  publicar: precompilar el sitio y configurar el App Pool con `startMode="AlwaysRunning"`,
  `preloadEnabled="true"` (Application Initialization) e idle timeout 0.
**No incluye:** CI/CD automatizado, monitoreo avanzado.

---

## Historial

### Fase 0 — Setup ✅
**Objetivo:** proyecto Web Forms creado, N-Tier scaffolded, build verde.
**No incluye:** lógica de negocio, UI funcional, datos reales.

### Fase 1 — Auth ✅
**Objetivo:** `Login.aspx` + `ChangePassword.aspx` con el flujo completo de primer acceso
(API de empleados → `Operator` local → PBKDF2 → FormsAuth).
**No incluye:** dashboard, GuidedTest, catálogo de áreas, pasos de entrenamiento.

### Fase 2 — Dominio y catálogo de R&C ✅
**Objetivo:** auditoría del legacy de R&C + catálogo real en `06_RCMoldeoCatalog.sql`:
`Products` (R&C), `Areas` (Moldeo), `NCCodes` (30 códigos), `DataCollectionParameters` (15:
ID molde + 10 parámetros MSS + verificación MSS + fecha kerosene + 2 first-shot). El esquema
de `DataCollectionParameters` sumó `DataType` y `DisplayOrder`; `MinValue`/`MaxValue` quedaron
nullable (rangos pendientes del MSS D000281359).
**No incluye:** UI, motor de validación, `TrainingGuideService`, pantallas.
**Deuda de proceso:** se saltó la aprobación de la spec 002 antes de implementar.

### Fase 3 — Motor de TrainingGuide ✅
**Objetivo:** `TrainingGuideRepository` (un SP con dos result sets puebla `Steps`),
`TrainingGuideService.BuildValidatorForGuide` y `StepValidator` con `StepValidationContext`
inyectado (Area + NCCodes + DCParams), con validación para todos los `StepType`. Repositorios
`Area`, `NCCode`, `DataCollectionParameter` y sus SPs. Guía real de Moldeo en
`07_RCMoldeoGuide.sql`.
*Superado después:* la LC Moldeo era un paso propio de la guía (con pesos 0.04/0.03/0.03 en
3 instancias de LC) y Assembly se validaba contra `ExpectedComponentCount`. Hoy el checklist
va dentro de Search, Assembly usa el BOM (spec 008) y los pesos están en `CLAUDE.md`.
**No incluye:** gate-keeping real, UI, tests de `ScoreCalculator`.

### Fase 4 — TrainingSession y modos ✅
**Objetivo:** gate-keeping real Guided vs. Assessment en `TrainingSessionService`, `Abandon`,
puntaje en tiempo real. `StartSession.aspx` (Product → Area → Guía → Modo) y un
`GuidedTest.aspx` genérico por paso. Spec `003-training-session-y-modos.md` (aprobada
2026-09-04).
**No incluye:** UserControls por tipo de paso, tests de `ScoreCalculator`, LoFlo.

### Fase 5 — ScoreCalculator: cobertura de tests ✅ (2026-10-07)
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

### Fase 6 — UserControls por tipo de paso ✅ (2026-09-24)
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

### Ajustes tras la Fase 6 y spec 008 ✅ (2026-09-25 a 2026-09-28)
- **Assessment real (S1 + S4, sin spec):** el 50% del puntaje nunca podía fallar porque los
  controles rechazaban las decisiones antes de enviarlas. Se separó formato de decisión (hoy
  `CLAUDE.md` → "Qué cuenta como intento"); el 70% pasó a `ScoreCalculator.PassingScore`; los
  banners de guardado solo aparecen si el servidor aceptó el paso.
- **S2, secuencia en Assessment (sin spec):** reglas cerradas con el usuario el 2026-09-25,
  hoy en `CLAUDE.md` → "Qué cuenta como intento".
- **Spec 008 (5 entregas, `008-plan.md`):** el motor soporta áreas Serialized: 3-5 piezas
  con SFC propio, Browse Work List y POD con multi-selección, `[N SELECTED]`, Start directo
  sin checklist (`Areas.RequiresStartChecklist`); Assembly con el BOM (se eliminó
  `ExpectedComponentCount`); DC por pieza con grupos obligatorios (Moldeo exige su grupo de
  fechas de expiración); Log NC y Complete multi-fila. Pendiente diferido en la spec: recorrido
  pieza por pieza. Verificado: Redefinity de punta a punta y regresión de Moldeo.
- **Checklist de la spec 006 cerrado (2026-09-29):** el punto 6 (volver a guardar Resource
  Setup) se resolvió por análisis de código (`BtnSave_Click` con `if (!Visible) return;` + el
  guard de sender); el 5 y el 7 los cubrió la spec 008.

### Fase 6.5 — Fidelidad de UI/UX con el SAP MES real ✅ (2026-09-29 a 2026-10-02)
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

### Auditoría, spec 009 y auditoría final ✅ (2026-10-01 a 2026-10-06)
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

### Fase 6.6 — Overlays guiados ✅ (2026-10-07)
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

### Reorganización de la documentación (2026-10-07, sin spec)
Cada regla en un solo lugar. `CLAUDE.md` quedó con solo reglas vigentes y un mapa de
documentos (de 37,5 KB a 22,9 KB); el proceso pasó a `docs/process.md` y los mecanismos a
`docs/architecture.md`; el roadmap quedó como estado + historial; el glosario, solo
definiciones. Se quitaron los pesos copiados en `docs/ui-reference/` (estaban desalineados
de la BD). La memoria pasó de 10 a 4 entradas (perfil, reporte semanal, `/btw` y dónde
anotar cada cosa).

### Limpieza de scripts SQL (2026-10-07, sin spec)
Mismos 8 archivos y numeración (decisión del usuario). Los SPs pasan a `CREATE OR ALTER` sin
los `DROP` de SPs ya eliminados. Cada catálogo se declara una vez como tabla de valores y se
actualiza o inserta en orden (los Ids no cambian), todo dentro de una transacción. Las guías
se cargan con `spTrainingGuideSync` (nuevo, en `02`), que actualiza los pesos y aborta si no
suman 1.00. Se corrigió un bug: los literales sin `N` guardaban "Ω" como "O". Se probó en
LocalDB: la instalación limpia y la re-ejecución sobre una BD con sesiones dan los mismos
datos e Ids que los scripts anteriores. Reglas: `docs/process.md` → "Base de datos".

### Arreglo de rendimiento del arranque ✅ (2026-10-07, sin spec)
`optimizeCompilations` en `Web.config` (Login tras un rebuild: de ~4 s a ~1,7 s); la lista de
empleados se precarga al arrancar y se refresca en segundo plano (`docs/architecture.md` →
"API de empleados"); `/bundles` y `/Content` accesibles sin login (Login y ChangePassword
recibían el HTML del Login en vez de sus JS). B-1: una lista vacía de la API no reemplaza la
última. Pendiente para la Fase 9: el arranque en frío en IIS.

### Login SAP NetWeaver ✅ (2026-10-08, sin spec)
Login y Cambio de contraseña replican el login SAP NetWeaver real (captura `Login SAP
NETWEAVER.png`) en un `Auth.Master` propio que hereda `AntiXsrfMasterPage`; `StartSession`
sigue con `Site.Master`. La foto y el logo de SAP están recortados de la captura; sin
Bootstrap. Decisiones del usuario: los errores del servidor van en SweetAlert, como el
legacy, y Cambio de contraseña tiene Cancel, que cierra la sesión. Las validaciones no
cambiaron. Cambio de contraseña no tiene captura: usa los textos del cambio de contraseña de
NetWeaver (`docs/ui-reference/inventario-pantallas.md`). Probado por el usuario en el
navegador.

### Ultrareview ✅ (2026-10-08, sin spec)
Por pedido del usuario, `main` se movió (fast-forward) a `25fa64f`, el cierre de la auditoría
final: así `/ultrareview` sin argumentos revisa solo lo no auditado (6.6, rendimiento, Fase 5,
scripts SQL, Fase 7). Un solo hallazgo, *nit*: en Serialized, la guía de Log NC empezaba con
un subpaso "quitar la selección" que siempre se saltaba (el POD ya llega sin selección), y el
contador arrancaba en "Subpaso 2". Se quitó el subpaso.

### Refactor `Operator` → `User` ✅ (2026-10-09 a 2026-10-12, sin spec)
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

### T1 · Preparación de la versión de prueba ✅ (2026-10-12, Fase 8.5, plan directo)
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

### Spec 011 · Escanear la Shop Order ✅ (2026-10-13, Fase 8.5, T2)
Plan `docs/specs/011-plan.md`, tres entregas probadas por el usuario: **E1** (`5976d22`)
carga desde `TxtSFC` (Shop Order o SFC de la orden + Enter, en el cliente como Browse Work
List; Start resuelve lo escrito sin Enter; la BLL valida el SFC de NonSerialized con
`PodNotice.SfcNotInOrder` y el Start de Serialized avisa con `StartWholeOrder`); **E2**
(`1db8c23`) etiqueta simulada con Code 128 (JsBarcode 3.11.6 local) sobre el lado vacío del
pie, y **B-1** (ya existía: después del Start el OK de Browse solo cierra); **E3** guía de
Guided (resalta la etiqueta con `TxtSFC` libre, burbuja de Browse, Start) y la etiqueta
desaparece con el Start. Tres enmiendas de la spec, todas decididas por el usuario
(`1865c03`, `b6846b9`, `2e96361`). Sin cambios de esquema ni SPs.

### Spec 013 · Cuentas, roles y registro de actividad ✅ (2026-10-14, Fase 8.5)
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

### T7 · Mensajes y alertas de la guía ✅ (2026-10-14, Fase 8.5, plan directo)
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

### Spec 015 · Ajustes de la trainer ✅ (2026-10-14, Fase 8.5)
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

### Spec 014 · Tres métodos de Log NC ✅ (2026-10-19, Fase 8.5, T4)
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

### Spec 016 · Preparación antes de SAP ✅ (2026-10-19, Fase 8.5, T10)
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
